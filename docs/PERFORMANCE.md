# Performance and user-impact policy

FreezeTrace is a diagnostic tool. It must not become the performance problem it is trying to diagnose.

## v0.2 collection budget

- CPU, RAM, foreground process name and aggregate network counters: approximately once per second.
- Top-process inventory: once every 5 seconds and cached between refreshes.
- Windows Event Log: event-driven subscriptions with narrow XPath filters only.
- Telemetry buffer: 120 samples (about two minutes at the default rate).
- Windows-event buffer: 256 selected events.
- Event payloads: at most 32 named fields, each capped at 512 characters.
- Large WHEA `RawData` payloads are intentionally excluded from the always-on buffer.

## No background disk churn

Normal collection stays in memory. v0.2 writes an incident bundle only after the user explicitly presses `S`.

Automatic incident persistence is intentionally disabled in v0.2.0. A future automatic mode must be opt-in and must include rate limits and cooldowns before it can become a default feature.

## Features intentionally not enabled in v0.2

The following can be useful diagnostically but are deliberately deferred because they require stronger overhead controls:

- continuous ETW/WPR traces;
- packet capture or packet payload inspection;
- continuous process dumps;
- high-frequency sensor polling;
- continuous screenshots;
- automatic crash-dump collection;
- automatic incident writes.

## Event Log safeguards

FreezeTrace subscribes only to selected application, graphics, power, WHEA and network-profile events. Provider message formatting is not performed in the always-on collector; structured XML event data is extracted instead. Optional logs are skipped silently if unavailable.

## Self-observation

Each telemetry sample records `CollectionDurationMilliseconds`. This lets future versions measure real collector cost on user hardware instead of assuming it is negligible.

## Release requirement

A release must pass restore, build and unit tests on the GitHub Actions Windows runner. Hardware validation and idle-overhead measurements remain separate real-machine tasks because CI runners cannot represent consumer gaming PCs accurately.
