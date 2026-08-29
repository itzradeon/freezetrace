namespace FreezeTrace.Core.Models;

public sealed record MachineMetadata(
    string OsDescription,
    string OsArchitecture,
    string ProcessArchitecture,
    string FrameworkDescription,
    int ProcessorCount);
