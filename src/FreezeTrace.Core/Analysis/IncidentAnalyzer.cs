using FreezeTrace.Core.Models;

namespace FreezeTrace.Core.Analysis;

public sealed class IncidentAnalyzer
{
    public IReadOnlyList<IncidentFinding> Analyze(
        IReadOnlyList<TelemetrySample> samples,
        IReadOnlyList<WindowsEventRecord>? events = null)
    {
        if (samples.Count == 0 && (events is null || events.Count == 0))
            return [];

        var findings = new List<IncidentFinding>();

        if (samples.Count > 0)
        {
            AddMemoryFinding(samples, findings);
            AddCpuFinding(samples, findings);
            AddNetworkFinding(samples, findings);
        }

        if (events is { Count: > 0 })
            AddWindowsEventFindings(events, findings);

        return findings;
    }

    private static void AddWindowsEventFindings(
        IReadOnlyList<WindowsEventRecord> events,
        ICollection<IncidentFinding> findings)
    {
        var whea = events
            .Where(x => x.Provider.Contains("WHEA", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Timestamp)
            .ToArray();

        if (whea.Length > 0)
        {
            findings.Add(new IncidentFinding(
                "hardware-whea",
                "Windows reported a hardware error",
                FindingConfidence.High,
                whea.Take(4)
                    .Select(x => $"{x.Timestamp:O} — {x.Provider} event {x.EventId}: {Compact(x.Message)}")
                    .ToArray(),
                ["A WHEA event identifies a hardware/firmware error path, but the exact failing component still requires the event payload and repeated-pattern analysis."]));
        }

        var graphics = events
            .Where(IsGraphicsEvent)
            .OrderBy(x => x.Timestamp)
            .ToArray();

        if (graphics.Length > 0)
        {
            findings.Add(new IncidentFinding(
                "graphics-stack",
                "Graphics driver or display stack interruption",
                graphics.Any(x => x.EventId == 4101) ? FindingConfidence.High : FindingConfidence.Medium,
                graphics.Take(5)
                    .Select(x => $"{x.Timestamp:O} — {x.Provider} event {x.EventId}: {Compact(x.Message)}")
                    .ToArray(),
                ["A graphics event close to the incident is strong correlation, but it does not by itself prove whether the root cause is the driver, GPU, application, power delivery, or another component."]));
        }

        var hangs = events
            .Where(x => x.EventId == 1002 || x.Provider.Equals("Application Hang", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Timestamp)
            .ToArray();

        if (hangs.Length > 0)
        {
            findings.Add(new IncidentFinding(
                "application-hang",
                "Application stopped responding",
                FindingConfidence.High,
                hangs.Take(4)
                    .Select(x => $"{x.Timestamp:O} — {Compact(x.Message)}")
                    .ToArray(),
                ["An application hang is a symptom. FreezeTrace still needs surrounding CPU, GPU, storage, memory and driver evidence to determine the likely cause."]));
        }

        var crashes = events
            .Where(x => x.EventId == 1000 || x.Provider.Equals("Application Error", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Timestamp)
            .ToArray();

        if (crashes.Length > 0)
        {
            findings.Add(new IncidentFinding(
                "application-crash",
                "Application crash recorded by Windows",
                FindingConfidence.High,
                crashes.Take(4)
                    .Select(x => $"{x.Timestamp:O} — {Compact(x.Message)}")
                    .ToArray(),
                ["The crash event confirms that a process failed, but the faulting module and exception code must be correlated with the rest of the incident before assigning root cause."]));
        }

        var kernelPower = events
            .Where(x => x.EventId == 41 && x.Provider.Contains("Kernel-Power", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Timestamp)
            .ToArray();

        if (kernelPower.Length > 0)
        {
            findings.Add(new IncidentFinding(
                "unexpected-shutdown",
                "Previous unexpected shutdown detected",
                FindingConfidence.Low,
                kernelPower.Take(2)
                    .Select(x => $"{x.Timestamp:O} — Kernel-Power event 41.")
                    .ToArray(),
                ["Kernel-Power event 41 is recorded after Windows detects that the previous shutdown was not clean. It is evidence of an abnormal shutdown, not the root cause itself."]));
        }
    }

    private static bool IsGraphicsEvent(WindowsEventRecord e)
    {
        if (e.EventId == 4101)
            return true;

        string[] providers = ["Display", "nvlddmkm", "amdkmdag", "amdwddmg", "igfx", "Intel Graphics"];
        return providers.Any(p => e.Provider.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    private static string Compact(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "No formatted message available.";

        var compact = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return compact.Length <= 220 ? compact : compact[..220] + "…";
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
