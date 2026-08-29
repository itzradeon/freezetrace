using System.Diagnostics;
using FreezeTrace.Core.Models;

namespace FreezeTrace.Agent.Collectors;

internal static class ProcessCollector
{
    public static IReadOnlyList<ProcessSnapshot> ReadTopProcesses(int count)
    {
        var snapshots = new List<ProcessSnapshot>();

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                snapshots.Add(new ProcessSnapshot(
                    process.Id,
                    process.ProcessName,
                    process.WorkingSet64));
            }
            catch
            {
                // A process can exit or deny access while being inspected.
            }
            finally
            {
                process.Dispose();
            }
        }

        return snapshots
            .OrderByDescending(x => x.WorkingSetBytes)
            .Take(count)
            .ToArray();
    }
}
