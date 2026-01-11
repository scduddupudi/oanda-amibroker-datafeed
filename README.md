OANDA AmiBroker Datafeed Plugin
This plugin provides a real‑time and historical market data feed for AmiBroker, powered by the official OANDA v20 REST API. It enables traders to stream intraday quotes, fetch historical candlesticks, and perform automated backfills directly inside AmiBroker without relying on third‑party bridges.

🔌 What This Plugin Does
Connects to OANDA’s v20 REST API

Streams real‑time tick and bar data into AmiBroker

Supports intraday, EOD, and historical backfill

Uses OANDA’s official CandlestickGranularity definitions
Reference: https://developer.oanda.com/rest-live-v20/instrument-df/#CandlestickGranularity

Designed for stability, low latency, and compatibility with custom trading systems

📊 Supported Features
Real‑time quote updates for all major forex pairs

Backfill of up to 5,000 bars per request (OANDA limit)

Configurable granularity (S5, M1, M5, H1, D, W, etc.)

Automatic handling of session restarts and reconnections

Clean integration with AmiBroker’s RT plugin architecture

🧩 Use Cases
Building automated trading systems in AmiBroker

Running real‑time forex dashboards

Performing intraday and EOD analysis using OANDA data

Maintaining a lightweight, broker‑native datafeed without external dependencies
