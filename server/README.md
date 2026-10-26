# Colón Quest — Server

FastAPI backend: AI game master, negotiation, image cache, cost guard and Class Mode telemetry.

See the [root README](../README.md) for setup commands and [CONTRIBUTING.md](../CONTRIBUTING.md) for conventions.

## Layout

```
app/
  main.py        # FastAPI app factory and router wiring
  config.py      # Settings loaded from environment variables
  api/           # HTTP routers (one module per resource)
tests/           # pytest suite
```
