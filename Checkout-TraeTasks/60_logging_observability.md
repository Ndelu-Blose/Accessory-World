# 60 — Logging & Observability



---


- Use **structured logging** with event ids (`OrderCreated`, `StockReserved`).
- Correlate with **Order.PublicId** in each log line.
- Emit **metrics**: checkout latency, ITN processing time, failures.
