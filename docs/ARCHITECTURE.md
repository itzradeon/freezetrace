# FreezeTrace Architecture

## Product definition

FreezeTrace is a Windows incident recorder.

Its unit of work is not a benchmark session and not an individual metric. It is an **incident**:

```text
pre-incident telemetry
        +
trigger
        +
post-incident telemetry
        +
system events
        =
incident bundle
```

## Components

### FreezeTrace.Core

Platform-agnostic domain logic:

- telemetry models
- circular buffer
- incident model
- analyzer contracts
- deterministic detection rules

It should remain easy to unit test.

### FreezeTrace.Agent

Windows background collection process.

Responsibilities:

- collect metrics
- keep the rolling buffer
- handle manual / automatic triggers
- save incident bundles
- eventually run ETW sessions and event subscriptions

### FreezeTrace.UI (planned)

Desktop app.

Responsibilities:

- incident list
- synchronized timeline
- findings / evidence
- comparison across incidents
- export / anonymization
- settings

The UI must not be required for collection.

## Data flow

```text
Collectors
   │
   ├── System
   ├── Network
   ├── Processes
   ├── Event Log       (planned)
   ├── ETW             (planned)
   ├── PresentMon      (planned)
   └── Hardware        (planned)
   │
   v
TelemetrySample
   │
   v
RingBuffer<T>
   │
   ├── normal samples expire
   │
   └── trigger
        │
        v
IncidentRecorder
        │
        ├── pre-trigger samples
        ├── post-trigger samples
        ├── machine metadata
        └── findings
```

## Sampling strategy

Not every signal needs the same frequency.

### Slow metrics

Target: ~1 Hz.

Examples:

- CPU usage
- memory
- temperatures
- clocks
- disk throughput
- network throughput

### Event streams

Event-driven.

Examples:

- process start/stop
- app crash
- display driver reset
- WHEA
- network interface state

### High-resolution traces

Enabled selectively / circularly.

Examples:

- ETW
- frame presentation
- DPC / ISR
- storage latency

## Incident window

Default target:

```text
-120 seconds ───── trigger ───── +15 seconds
```

The post-trigger window matters because Windows may emit diagnostic events after the visible symptom.

## Analyzer

The analyzer should produce hypotheses:

```text
Finding
├── category
├── title
├── confidence
├── evidence[]
└── counterEvidence[]
```

Bad:

> The AMD driver caused the freeze.

Better:

> Graphics-stack interruption is the strongest hypothesis (high confidence) because frame delivery stopped, GPU clocks collapsed and a display-driver event occurred in the same window.

## Performance budget

Long-term target while idle:

- CPU: < 1% typical
- RAM: < 150 MB
- Disk writes: near-zero during normal operation
- No continuous packet payload capture

Measurements should be benchmarked in CI or release testing as the project matures.

## Storage

Normal high-frequency data lives in memory.

Persistent storage occurs when:

- an incident is triggered
- an automatic detector fires
- the user explicitly starts a recording session

SQLite may be introduced for incident indexing while raw high-frequency streams can use compact binary blobs.

## Privacy model

Local-first.

A collector must answer:

1. Why is this data necessary?
2. Could it contain personal information?
3. Can it be anonymized?
4. Does it need to be on by default?

## Future extension points

```csharp
public interface ITelemetryCollector
{
    string Name { get; }
    ValueTask CollectAsync(TelemetrySampleBuilder builder, CancellationToken ct);
}

public interface IIncidentRule
{
    IncidentFinding? Evaluate(Incident incident);
}
```
