# Roadmap

## v0.1 — Flight recorder foundation

- [x] Repository foundation
- [x] Core telemetry model
- [x] Circular ring buffer
- [x] Windows CPU / memory sampling
- [x] Network counters
- [x] Process memory snapshot
- [x] Manual incident trigger
- [x] Pre/post incident window
- [x] JSON export
- [x] Initial deterministic analyzer
- [x] Unit tests / CI
- [ ] Validate on Windows hardware
- [ ] Measure idle overhead

## v0.2 — Windows events

- [x] Event Log subscription
- [x] Application crash / hang events
- [x] Kernel-Power event 41 correlation
- [x] Display-driver events
- [x] WHEA event detection
- [ ] WHEA payload/component parser
- [ ] Network adapter state events
- [x] Incident event timeline
- [ ] Automatic incident triggers from critical events
- [ ] Validate event subscriptions on AMD / NVIDIA / Intel systems

## v0.3 — Hardware telemetry

- [ ] LibreHardwareMonitor integration
- [ ] CPU temperature / clocks / power
- [ ] GPU temperature / clocks / power
- [ ] VRAM
- [ ] Storage health signals
- [ ] Sensor availability capabilities

## v0.4 — Gaming telemetry

- [ ] PresentMon integration
- [ ] FPS
- [ ] frame time
- [ ] present mode
- [ ] dropped / delayed frames
- [ ] automatic severe-stutter trigger
- [ ] game/session detection

## v0.5 — ETW

- [ ] Circular ETW session
- [ ] DPC / ISR
- [ ] disk I/O latency
- [ ] process/thread events
- [ ] memory pressure
- [ ] save ETL alongside incident
- [ ] low-overhead profiles

## v0.6 — Correlation engine

- [ ] Finding score system
- [x] evidence / counter-evidence foundation
- [ ] CPU stall rule
- [x] graphics-stack event rule
- [x] memory-pressure rule
- [ ] disk-stall rule
- [ ] network-drop rule
- [ ] driver-reset multi-signal rule
- [ ] confidence calibration

## v0.7 — Incident similarity

- [ ] Incident fingerprints
- [ ] Similarity scoring
- [ ] Incident clusters
- [ ] Common-factor extraction
- [ ] First-seen / last-seen tracking

## v0.8 — Desktop UI

- [ ] Incident inbox
- [ ] synchronized telemetry timeline
- [ ] finding detail view
- [ ] compare incidents
- [ ] sensor settings
- [ ] tray mode
- [ ] global hotkey

## v0.9 — Sharing

- [ ] `.freezetrace` bundle format
- [ ] anonymizer
- [ ] standalone HTML report
- [ ] support-mode export
- [ ] import shared incident

## 1.0

- [ ] Stable incident schema
- [ ] Tested upgrade path
- [ ] Performance baseline
- [ ] Privacy audit
- [ ] Signed Windows release
- [ ] Documentation site
