namespace FreezeTrace.Core.Models;

public sealed record Incident(
    string Id,
    DateTimeOffset TriggeredAt,
    string Trigger,
    IReadOnlyList<TelemetrySample> Samples,
    IReadOnlyList<WindowsEventRecord> Events,
    IReadOnlyList<IncidentFinding> Findings,
    MachineMetadata Machine);
