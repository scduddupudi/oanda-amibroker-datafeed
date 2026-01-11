// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DataSource.cs" company="Clartix LLC">
//   Copyright © 2026 Sanjay DUDDUPUDI, Clartix LLC. See LICENSE.txt
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace AmiBroker.Plugin
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using System.Collections.Concurrent;
    using System.Net;
    using System.Net.Http;
    using System.Collections.Generic;

    using System.Globalization;
    using Models;
    using AmiBroker.Plugin.Oanda;

    // using AmiBroker.Plugin.Oanda; // Ensure this namespace matches your DTOs file

    public class DataSource
    {
        public DataSource(string databasePath, IntPtr mainWnd)
        {


            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                if (args.Name.StartsWith("Newtonsoft.Json"))
                {
                    string folder = System.IO.Path.GetDirectoryName(typeof(DataSource).Assembly.Location);
                    string path = System.IO.Path.Combine(folder, "Newtonsoft.Json.dll");
                    if (System.IO.File.Exists(path)) return System.Reflection.Assembly.LoadFrom(path);
                }
                return null;
            };

            this.DatabasePath = databasePath;
            this.MainWnd = mainWnd;
            this.Broker = Activator.CreateInstance(Type.GetTypeFromProgID("Broker.Application", true));

            // Optional: Close other instances logic preserved from your file
            if (this.Broker.ActiveDocument == null)
            {
                var processes = Process.GetProcesses().Where(x => x.ProcessName.Contains("AmiBroker") && x.MainWindowHandle != this.MainWnd);
                foreach (var proc in processes)
                {
                    MessageBox.Show("Please close AmiBroker application with Process ID: " + proc.Id, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    proc.WaitForExit();
                }
                this.Broker = Activator.CreateInstance(Type.GetTypeFromProgID("Broker.Application", true));
            }
        }

        public string DatabasePath { get; set; }
        public IntPtr MainWnd { get; private set; }
        public dynamic Broker { get; private set; }

        // OANDA config
        private const string ACCESS_TOKEN = "fb0d80816a87fdc8f117b8bec3ba79ae-b44803b4fcdf2cb9d0f910638a32051d";
        private const string BASE_URL = "https://api-fxpractice.oanda.com/v3"; // Will be replaced with prod

        // Cache: key = ticker|periodicity, value = last downloaded bars
        private static readonly ConcurrentDictionary<string, Quotation[]> _cache =
            new ConcurrentDictionary<string, Quotation[]>();

        // Throttle to avoid repeated downloads
        private static readonly ConcurrentDictionary<string, DateTime> _lastFetchUtc =
            new ConcurrentDictionary<string, DateTime>();

        private static readonly TimeSpan MIN_FETCH_INTERVAL = TimeSpan.FromSeconds(3);

        private static readonly HttpClient _client = CreateClient();

        private static HttpClient CreateClient()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            var c = new HttpClient(handler);
            c.DefaultRequestHeaders.Add("Authorization", "Bearer " + ACCESS_TOKEN);
            c.DefaultRequestHeaders.Add("Accept", "application/json");
            c.DefaultRequestHeaders.Add("Accept-Datetime-Format", "RFC3339");
            return c;
        }

        private static string MakeKey(string ticker, Periodicity p)
            => $"{ticker.ToUpperInvariant().Trim()}|{(int)p}";

        public Quotation[] GetQuotes(string ticker, Periodicity periodicity, int limit, Quotation[] existingQuotes)
        {
            string key = MakeKey(ticker, periodicity);

            // Throttle: if we have cached data and AB has data, return empty to keep existing bars
            if (existingQuotes != null && existingQuotes.Length > 0 &&
                _cache.TryGetValue(key, out var cached) && cached != null && cached.Length > 0)
            {
                if (_lastFetchUtc.TryGetValue(key, out var last) &&
                    (DateTime.UtcNow - last) < MIN_FETCH_INTERVAL)
                {
                    return new Quotation[0];
                }
            }

            _lastFetchUtc[key] = DateTime.UtcNow;

            // Always request last 5000 bars (or less if user set limit smaller)
            int count = Math.Min(Math.Max(1, limit), 5000);

            try
            {
                // FIX: Pass enum directly, do not cast to int
                var bars = FetchLastBarsFromOanda(ticker, periodicity, count);

                if (bars != null && bars.Length > 0)
                {
                    _cache[key] = bars;
                    return bars;
                }
                return new Quotation[0];
            }
            catch (Exception ex)
            {
                Debug.WriteLine("GetQuotes error: " + ex.Message);
                return new Quotation[0];
            }
        }

        // FIX: Change signature to accept Periodicity enum
        private Quotation[] FetchLastBarsFromOanda(string ticker, Periodicity periodicity, int count)
        {
            string symbol = ToOandaSymbol(ticker);
            string granularity = ToOandaGranularity(periodicity); // FIX: Pass enum

            string url = $"{BASE_URL}/instruments/{symbol}/candles?price=M&granularity={granularity}&count={count}";
            Debug.WriteLine("OANDA URL: " + url);

            // FIX: Use GetAsync to check status code and log body on error
            using (var resp = _client.GetAsync(url).Result)
            {
                string body = resp.Content.ReadAsStringAsync().Result;

                if (!resp.IsSuccessStatusCode)
                {
                    Debug.WriteLine("OANDA HTTP ERROR " + (int)resp.StatusCode + ": " + body);
                    return new Quotation[0];
                }

                var root = Newtonsoft.Json.JsonConvert.DeserializeObject<OandaResponse>(body);

                if (root?.candles == null || root.candles.Count == 0)
                    return new Quotation[0];

                var list = new List<Quotation>(root.candles.Count);

                foreach (var candle in root.candles)
                {
                    if (!candle.complete) continue;

                    DateTime dt = DateTime.Parse(candle.time, null, DateTimeStyles.RoundtripKind);

                    list.Add(new Quotation
                    {
                        DateTime = new AmiDate(dt.ToLocalTime()),
                        Open = float.Parse(candle.mid.o, CultureInfo.InvariantCulture),
                        High = float.Parse(candle.mid.h, CultureInfo.InvariantCulture),
                        Low = float.Parse(candle.mid.l, CultureInfo.InvariantCulture),
                        Price = float.Parse(candle.mid.c, CultureInfo.InvariantCulture),
                        Volume = (float)candle.volume
                    });
                }

                return list.OrderBy(x => x.DateTime).ToArray();
            }
        }

        private string ToOandaSymbol(string ticker)
        {
            string s = ticker.ToUpperInvariant().Trim();
            if (s.Contains("_")) return s;
            if (s.Length == 6) return s.Insert(3, "_");   // EURUSD -> EUR_USD
            return s; // fallback
        }

        
        private string ToOandaGranularity(Periodicity p)
        {
            switch (p)
            {
              
                case Periodicity.OneSecond: return "S5";
                case Periodicity.OneMinute: return "M1";
                case Periodicity.FiveMinutes: return "M5";
                case Periodicity.FifteenMinutes: return "M15";
                case Periodicity.OneHour: return "H1";
                case Periodicity.EndOfDay: return "D";

                // Fallback for any unknown value
                default: return "M1";
            }
        }

    }
}
