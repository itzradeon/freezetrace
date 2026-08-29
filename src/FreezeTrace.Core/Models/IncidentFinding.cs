namespace FreezeTrace.Core.Models;

public enum FindingConfidence
{
    Low,
    Medium,
    High
}

public sealed record IncidentFinding(
    string Category,
    string Title,
    FindingConfidence Confidence,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string> CounterEvidence);
