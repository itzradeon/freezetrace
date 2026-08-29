# FreezeTrace

**FreezeTrace is an open-source flight recorder for Windows PCs.**

When a game stutters, an app hangs, the network drops, or Windows briefly freezes, FreezeTrace keeps a short rolling history of lightweight system telemetry so you can save **what happened just before the problem**.

> Current release line: **v0.2.x — Windows events + low-impact safeguards**

## Why FreezeTrace?

Most diagnostic tools show metrics. FreezeTrace focuses on **incidents**:

1. Continuously collect lightweight telemetry into bounded in-memory buffers.
2. When something goes wrong, press `S`.
3. Preserve the two minutes before the incident and a short window after it.
4. Correlate selected Windows events with telemetry.
5. Produce hypotheses with evidence and counter-evidence instead of pretending correlation is proof.

FreezeTrace is intended to be **local-first, privacy-first, low-overhead, and open source**.

## v0.2.0 capabilities

- Windows agent (`FreezeTrace.Agent`)
- 120-sample circular telemetry buffer
- 256-event bounded Windows-event buffer
- CPU usage through native Windows APIs
- RAM usage / available memory
- aggregate network byte counters
- foreground process name
- top-process memory snapshot cached for 5 seconds
- per-sample collector-duration measurement
- manual incident capture (`S`)
- 120 seconds before + 15 seconds after the trigger
- JSON incident export
- Windows Event Log subscriptions for:
  - Application Error / crash (`1000`)
  - Application Hang (`1002`)
  - Kernel-Power (`41`)
  - Display / graphics reset (`4101`)
  - WHEA-Logger events
  - selected AMD / NVIDIA display-driver providers
  - NetworkProfile connect / disconnect (`10000` / `10001`)
- structured EventData extraction for WHEA and application failures
- event `RecordId` capture and incident deduplication
- deterministic incident findings for memory, CPU, graphics, WHEA, app crash/hang, network disconnect and unexpected shutdown
- GitHub Actions build/tests

## Low-impact policy

FreezeTrace must not become the performance issue it is trying to diagnose.

v0.2.0 therefore deliberately uses conservative defaults:

- CPU/RAM/network sampling: ~1 Hz
- top-process enumeration: once every 5 seconds, cached between refreshes
- Event Log collection: event-driven and narrowly filtered
- no continuous disk writes
- no ETW/WPR yet
- no packet capture
- no process dumps
- no screenshots
- no high-frequency hardware polling
- no automatic incident persistence
- no large WHEA `RawData` in the always-on buffer

See [`docs/PERFORMANCE.md`](docs/PERFORMANCE.md).

## Usage

Run the agent and press:

- `S` — save an incident
- `Q` — quit

An incident is stored under:

```text
%LOCALAPPDATA%\FreezeTrace\incidents\<incident-id>\incident.json
```

Normal monitoring remains in memory and does not create incident files until you explicitly press `S`.

## Example finding

```text
Graphics driver or display stack interruption
Confidence: High

Evidence
- 2026-08-30T00:42:18Z — Display event 4101

Counter-evidence
- The event strongly correlates with the incident, but does not prove whether
  the root cause is the driver, GPU, application, power delivery, or another component.
```

Kernel-Power event 41 is intentionally treated as evidence of an abnormal previous shutdown, **not** as the root cause itself.

## Architecture

```text
                  Windows
                     │
       ┌─────────────┴─────────────┐
       │                           │
  1 Hz telemetry             Event Log watchers
       │                           │
       v                           v
120-sample ring buffer      256-event ring buffer
       │                           │
       └─────────────┬─────────────┘
                     │
                manual trigger
                     │
              -120 s / +15 s
                     │
                     v
              IncidentAnalyzer
                     │
                     v
                incident.json
```

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

## Build from source

### Requirements

- Windows 10/11
- .NET 8 SDK or newer

```powershell
git clone https://github.com/itzradeon/freezetrace.git
cd freezetrace
dotnet restore FreezeTrace.sln
dotnet build FreezeTrace.sln --configuration Release
dotnet test FreezeTrace.sln --configuration Release
```

Run:

```powershell
dotnet run --project src/FreezeTrace.Agent/FreezeTrace.Agent.csproj
```

## Principles

- **Evidence first** — show why a hypothesis was produced.
- **No fake certainty** — correlation is not automatically causation.
- **Low overhead** — diagnostics must not become the performance problem.
- **Bounded memory** — always-on buffers have fixed capacities.
- **Local by default** — telemetry stays on the machine.
- **No packet contents / keystrokes / screenshots by default.**
- **Composable collectors** — ETW, PresentMon and hardware sensors can be added independently later.
- **Exportable incidents** — make support reports easy to share safely.

## Next major data sources

- LibreHardwareMonitor
- PresentMon
- ETW / WPR
- DPC / ISR timing
- Storage latency
- DNS / gateway / packet-loss probes

## Roadmap

See [`docs/ROADMAP.md`](docs/ROADMAP.md).

## Contributing

Issues, ideas, collectors, detection rules and UI work are welcome.

Read [`CONTRIBUTING.md`](CONTRIBUTING.md) before opening a pull request.

## Security & privacy

See [`SECURITY.md`](SECURITY.md).

## License

MIT — see [`LICENSE`](LICENSE).
