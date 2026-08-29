namespace FreezeTrace.Core.Models;

public sealed record WindowsEventRecord(
    DateTimeOffset Timestamp,
    string LogName,
    string Provider,
    int EventId,
    byte? Level,
    string? LevelName,
    string? Message,
    long? RecordId = null,
    IReadOnlyDictionary<string, string>? Data = null);
