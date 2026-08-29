# Changelog

All notable changes to FreezeTrace are documented here.

## [0.2.0] - 2026-08-30

### Added

- Windows Event Log subscriptions for application crashes/hangs, WHEA, graphics resets, Kernel-Power and NetworkProfile connect/disconnect events.
- Structured EventData extraction with bounded field counts and lengths.
- WHEA evidence extraction for fields such as ErrorSource, ErrorType, APIC ID, MCA bank and PCIe/device identifiers when available.
- Application crash evidence for application, module and exception code when available.
- Network-disconnect finding based on NetworkProfile event 10001.
- Event RecordId capture and incident deduplication.
- Per-sample collector-duration measurement.
- Performance policy documentation.
- Automated pre-release workflow with self-contained Windows x64 archive and SHA-256 checksum.

### Changed

- Top-process enumeration now runs once every five seconds and is cached between refreshes.
- Windows event buffer is capped at 256 selected events.
- Continuous Event Log collection avoids provider message formatting.

### Safety / performance

- No background incident disk writes.
- Automatic incident persistence remains disabled.
- Large WHEA RawData is excluded from the always-on buffer.
- ETW, packet capture, dumps and high-frequency hardware monitoring remain deferred.

## [0.1.0] - 2026-08-30

### Added

- Initial Windows flight-recorder agent.
- Circular telemetry buffer.
- CPU, RAM, network and process snapshots.
- Manual incident capture and JSON export.
- Initial deterministic analysis rules.
- Unit tests and GitHub Actions CI.
