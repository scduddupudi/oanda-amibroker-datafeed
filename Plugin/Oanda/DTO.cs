using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmiBroker.Plugin.Oanda
{
    public class OandaResponse
    {
        public List<OandaCandle> candles { get; set; }
    }

    public class OandaCandle
    {
        public bool complete { get; set; }
        public int volume { get; set; }
        public string time { get; set; }
        public OandaOHLC mid { get; set; }
    }

    public class OandaOHLC
    {
        public string o { get; set; }
        public string h { get; set; }
        public string l { get; set; }
        public string c { get; set; }
    }

   
}
