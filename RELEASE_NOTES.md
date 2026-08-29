# FreezeTrace v0.2.0

FreezeTrace v0.2.0 expands the first flight-recorder MVP with selected Windows Event Log correlation while deliberately keeping the always-on collector conservative.

## Highlights

- Application Error (`1000`) and Application Hang (`1002`) capture
- WHEA event capture with bounded structured EventData extraction
- Display / graphics-reset correlation including event `4101`
- Kernel-Power `41` handled as evidence of an abnormal shutdown, not automatically as root cause
- NetworkProfile connect/disconnect events (`10000` / `10001`)
- event RecordId capture and incident deduplication
- structured application crash evidence such as application, module and exception code when Windows provides it
- top-process enumeration reduced to once every 5 seconds with cached results
- per-sample collector-duration measurement
- fixed-size 120-sample telemetry buffer and 256-event Windows-event buffer
- no background incident disk writes
- no continuous ETW, packet capture, dumps or high-frequency hardware polling in this release

## Privacy / performance design

Normal operation remains local and memory-backed. An incident file is written only when the user explicitly presses `S`.

Large WHEA `RawData` payloads are excluded from the always-on buffer, event field counts and lengths are capped, and Windows provider message formatting is avoided in the continuous event collector.

## Known limitations

- this release has not yet been benchmarked across a broad set of real consumer Windows PCs;
- AMD/NVIDIA/Intel Event Log coverage can vary by driver version;
- hardware temperatures, clocks and VRAM are planned for v0.3;
- PresentMon gaming telemetry is planned for v0.4;
- ETW/DPC/ISR/storage-latency tracing is intentionally deferred until stronger overhead controls are implemented.

Because real-machine overhead validation is still pending, v0.2.0 is published as a **pre-release**.
