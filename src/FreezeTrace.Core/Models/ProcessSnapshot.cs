namespace FreezeTrace.Core.Models;

public sealed record ProcessSnapshot(
    int ProcessId,
    string Name,
    long WorkingSetBytes);
