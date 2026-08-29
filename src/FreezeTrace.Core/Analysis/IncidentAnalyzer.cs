using FreezeTrace.Core.Models;

namespace FreezeTrace.Core.Analysis;

public sealed class IncidentAnalyzer
{
    public IReadOnlyList<IncidentFinding> Analyze(IReadOnlyList<TelemetrySample> samples)
    {
        if (samples.Count == 0)
            return [];

        var findings = new List<IncidentFinding>();
        AddMemoryFinding(samples, findings);
        AddCpuFinding(samples, findings);
        AddNetworkFinding(samples, findings);

        return findings;
    }

    private static void AddMemoryFinding(
        IReadOnlyList<TelemetrySample> samples,
        ICollection<IncidentFinding> findings)
    {
        var peak = samples.Max(x => x.MemoryUsagePercent);
        var minAvailable = samples.Min(x => x.AvailableMemoryBytes);

        if (peak < 92)
            return;

        var evidence = new List<string>
        {
            $"Peak memory usage reached {peak:F1}%."
        };

        if (minAvailable < 1_500_000_000)
            evidence.Add($"Available memory fell to {FormatBytes(minAvailable)}.");

        findings.Add(new IncidentFinding(
            "memory",
            "Memory pressure",
            minAvailable < 750_000_000 ? FindingConfidence.High : FindingConfidence.Medium,
            evidence,
            []));
    }

    private static void AddCpuFinding(
        IReadOnlyList<TelemetrySample> samples,
        ICollection<IncidentFinding> findings)
    {
        var hot = samples.Count(x => x.CpuUsagePercent >= 95);
        if (hot < 5)
            return;

        findings.Add(new IncidentFinding(
            "cpu",
            "Sustained CPU saturation",
            hot >= 15 ? FindingConfidence.High : FindingConfidence.Medium,
            [$"CPU usage was at or above 95% for {hot} sampled seconds."],
            []));
    }

    private static void AddNetworkFinding(
        IReadOnlyList<TelemetrySample> samples,
        ICollection<IncidentFinding> findings)
    {
        if (samples.Count < 2)
            return;

        var last = samples[^1];
        var previous = samples[^2];

        if (last.NetworkBytesReceived < previous.NetworkBytesReceived ||
            last.NetworkBytesSent < previous.NetworkBytesSent)
        {
            findings.Add(new IncidentFinding(
                "network",
                "Network interface counter reset",
                FindingConfidence.Low,
                ["Network byte counters reset between the two most recent samples."],
                ["A counter reset can also happen when an adapter reconnects or changes."]));
        }
    }

    private static string FormatBytes(ulong value)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var size = (double)value;
        var unit = 0;

        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:F1} {units[unit]}";
    }
}
