# Security Policy

FreezeTrace captures diagnostic telemetry. Privacy and integrity therefore matter.

## Supported versions

FreezeTrace is currently pre-release. Only the latest main branch is supported.

## Reporting a vulnerability

Please do not publish exploit details in a public issue before maintainers have had a chance to investigate.

Until a dedicated security contact exists, open a GitHub Security Advisory on the repository.

## Data collection policy

FreezeTrace should not capture by default:

- Keystrokes
- Clipboard content
- Packet payloads
- Browser history / page contents
- Screenshots
- File contents
- Credentials or secrets

Exports should minimize or anonymize:

- Usernames
- Hostnames
- Local paths
- IP addresses
- MAC addresses

Collectors that introduce potentially sensitive telemetry must be explicit and opt-in.
