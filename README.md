# FreezeTrace

**FreezeTrace is an open-source flight recorder for Windows PCs.**

When a game stutters, an app hangs, the network drops, or Windows briefly freezes, FreezeTrace keeps a short rolling history of system telemetry so you can save **what happened just before the problem**.

> Status: early MVP / contributor-ready scaffold.

## Why FreezeTrace?

Most diagnostic tools show metrics. FreezeTrace focuses on **incidents**:

1. Continuously collect lightweight telemetry into a circular in-memory buffer.
2. When something goes wrong, press a key / trigger an incident.
3. Preserve the minutes before the incident and a short window after it.
4. Correlate signals and events.
5. Produce a human-readable explanation with evidence and confidence.

FreezeTrace is intended to be **local-first, privacy-first, and open source**.

## Current MVP

The first MVP contains:

- Windows agent (`FreezeTrace.Agent`)
- Circular in-memory telemetry buffer
- CPU usage sampling through Windows APIs
- RAM usage
- Network byte counters
- Foreground process / top memory processes
- Manual incident capture
- Post-incident capture window
- JSON incident export
- Basic rule-based incident analysis
- Unit tests for the ring buffer
- GitHub Actions CI
- Architecture, roadmap, security and contribution docs

### MVP interaction

Run the agent and press:

- `S` — save an incident
- `Q` — quit

An incident is stored under:

```text
%LOCALAPPDATA%\FreezeTrace\incidents\<incident-id>\
```

## Example incident

```text
Incident 2026-08-30T00:42:18Z
Window: -120s / +15s

Likely finding:
  Memory pressure

Confidence:
  Medium

Evidence:
  - RAM usage exceeded 92%
  - Available memory fell below 1.5 GB
```

The goal is **not** to pretend correlation is causation. Findings are hypotheses backed by evidence.

## Architecture

```text
┌───────────────────────────────────────┐
│          FreezeTrace.Agent             │
│                                       │
│  Collectors ──> Ring Buffer           │
│                    │                  │
│                    ├─ manual trigger  │
│                    └─ detectors       │
│                         │             │
│                         v             │
│                   IncidentRecorder    │
└─────────────────────────┬─────────────┘
                          │
                          v
                 Local incident bundle
                          │
                    Analyzer Engine
                          │
                          v
                  Desktop UI (planned)
```

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

## Build

### Requirements

- Windows 10/11
- .NET 8 SDK or newer

```powershell
git clone https://github.com/itzradeon/freezetrace.git
cd freezetrace
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

Run:

```powershell
dotnet run --project src/FreezeTrace.Agent/FreezeTrace.Agent.csproj
```

## Principles

- **Evidence first** — show why a hypothesis was produced.
- **No fake certainty** — correlation is not automatically causation.
- **Low overhead** — diagnostics must not become the performance problem.
- **Local by default** — telemetry stays on the machine.
- **No packet contents / keystrokes / screenshots by default.**
- **Composable collectors** — ETW, PresentMon, WHEA and hardware sensors can be added independently.
- **Exportable incidents** — make support reports easy to share safely.

## Planned data sources

- ETW / WPR
- Windows Event Log
- WHEA
- PresentMon
- LibreHardwareMonitor
- DPC / ISR timing
- Storage latency
- DNS / gateway / packet-loss probes
- Application crash and hang detection

## Roadmap

See [`docs/ROADMAP.md`](docs/ROADMAP.md).

## Contributing

Issues, ideas, collectors, detection rules and UI work are welcome.

Read [`CONTRIBUTING.md`](CONTRIBUTING.md) before opening a pull request.

## Security & privacy

See [`SECURITY.md`](SECURITY.md).

## License

MIT — see [`LICENSE`](LICENSE).
