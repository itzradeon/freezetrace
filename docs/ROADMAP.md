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

- [ ] Event Log subscription
- [ ] Application crash / hang events
- [ ] Kernel-Power
- [ ] Display-driver events
- [ ] WHEA parser
- [ ] Network adapter state events
- [ ] Incident event timeline

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
- [ ] evidence / counter-evidence
- [ ] CPU stall rule
- [ ] graphics-stack rule
- [ ] memory-pressure rule
- [ ] disk-stall rule
- [ ] network-drop rule
- [ ] driver-reset rule
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
