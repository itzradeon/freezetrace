# Contributing to FreezeTrace

Thanks for helping build FreezeTrace.

## What we want contributions for

- New low-overhead telemetry collectors
- Detection / correlation rules
- Incident serialization and anonymization
- ETW integration
- PresentMon integration
- Windows Event Log / WHEA parsing
- Tests
- Documentation
- Desktop UI / timeline visualization

## Ground rules

1. Diagnostic conclusions must be explainable.
2. Do not label correlation as proven causation.
3. Collect only data necessary for diagnostics.
4. New sensitive data sources must be opt-in and documented.
5. Avoid dependencies that add large idle overhead.
6. Prefer small, testable collectors.

## Development

```powershell
dotnet restore
dotnet build
dotnet test
```

## Pull requests

A PR should include:

- What problem it solves
- Why the approach was chosen
- Performance/privacy impact
- Tests where practical
- Screenshots for UI changes

## Commit style

Conventional-style prefixes are encouraged:

```text
feat:
fix:
docs:
test:
refactor:
perf:
chore:
```
