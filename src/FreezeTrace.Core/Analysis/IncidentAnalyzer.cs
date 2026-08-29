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
            AddNetworkCounterFinding(samples, findings);
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
            var confidence = whea.Any(x => x.Level is <= 2)
                ? FindingConfidence.High
                : FindingConfidence.Medium;

            findings.Add(new IncidentFinding(
                "hardware-whea",
                "Windows reported a hardware error",
                confidence,
                whea.Take(4).Select(DescribeWhea).ToArray(),
                ["WHEA identifies a hardware/firmware error path, but FreezeTrace does not claim the exact root cause from a single event. Repeated incidents and hardware telemetry are still needed."]));
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
                graphics.Take(5).Select(DescribeGraphics).ToArray(),
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
                hangs.Take(4).Select(DescribeApplicationHang).ToArray(),
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
                crashes.Take(4).Select(DescribeApplicationCrash).ToArray(),
                ["The crash event confirms that a process failed. A faulting module or exception code is evidence, not automatic proof of the underlying root cause."]));
        }

        var networkDisconnects = events
            .Where(IsNetworkDisconnect)
            .OrderBy(x => x.Timestamp)
            .ToArray();

        if (networkDisconnects.Length > 0)
        {
            findings.Add(new IncidentFinding(
                "network-disconnect",
                "Windows recorded a network disconnect",
                FindingConfidence.Medium,
                networkDisconnects.Take(4).Select(DescribeNetworkDisconnect).ToArray(),
                ["A Windows network-profile disconnect can be caused by the adapter, Wi-Fi/Ethernet link, router, roaming, sleep state, or an intentional network change. It does not by itself prove an ISP outage."]));
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

    private static string DescribeWhea(WindowsEventRecord e)
    {
        var details = JoinData(e, "ErrorSource", "ErrorType", "ApicId", "MCABank", "PrimaryDeviceName", "VendorID", "DeviceID");
        return FormatEvent(e, details);
    }

    private static string DescribeGraphics(WindowsEventRecord e)
    {
        var details = JoinData(e, "DriverName", "DeviceName", "DeviceId", "param1");
        return FormatEvent(e, details);
    }

    private static string DescribeApplicationHang(WindowsEventRecord e)
    {
        var details = JoinData(e, "AppName", "ExeFileName", "ProcessId", "ReportId");
        return FormatEvent(e, details);
    }

    private static string DescribeApplicationCrash(WindowsEventRecord e)
    {
        var details = JoinData(e, "AppName", "ModuleName", "ExceptionCode", "FaultingOffset", "AppPath", "ModulePath");
        return FormatEvent(e, details);
    }

    private static string DescribeNetworkDisconnect(WindowsEventRecord e)
    {
        var details = JoinData(e, "Name", "Description", "Guid", "Type", "State", "Category");
        return FormatEvent(e, details);
    }

    private static string FormatEvent(WindowsEventRecord e, string? details)
    {
        var prefix = $"{e.Timestamp:O} — {e.Provider} event {e.EventId}";
        if (!string.IsNullOrWhiteSpace(details))
            return $"{prefix}: {details}";

        if (!string.IsNullOrWhiteSpace(e.Message))
            return $"{prefix}: {Compact(e.Message)}";

        return prefix + ".";
    }

    private static string? JoinData(WindowsEventRecord e, params string[] keys)
    {
        if (e.Data is null || e.Data.Count == 0)
            return null;

        var values = new List<string>();
        foreach (var key in keys)
        {
            if (e.Data.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                values.Add($"{key}={Compact(value)}");
        }

        return values.Count == 0 ? null : string.Join(", ", values);
    }

    private static bool IsGraphicsEvent(WindowsEventRecord e)
    {
        if (e.EventId == 4101)
            return true;

        string[] providers = ["Display", "nvlddmkm", "amdkmdag", "amdwddmg", "igfx", "Intel Graphics"];
        return providers.Any(p => e.Provider.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsNetworkDisconnect(WindowsEventRecord e) =>
        e.EventId == 10001 &&
        e.Provider.Contains("NetworkProfile", StringComparison.OrdinalIgnoreCase);

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

    private static void AddNetworkCounterFinding(
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
