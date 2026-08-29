namespace FreezeTrace.Core.Models;

public sealed record TelemetrySample(
    DateTimeOffset Timestamp,
    double CpuUsagePercent,
    double MemoryUsagePercent,
    ulong AvailableMemoryBytes,
    ulong TotalMemoryBytes,
    long NetworkBytesReceived,
    long NetworkBytesSent,
    string? ForegroundProcess,
    IReadOnlyList<ProcessSnapshot> TopProcesses);
