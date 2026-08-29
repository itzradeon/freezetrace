using System.Diagnostics.Eventing.Reader;
using System.Xml.Linq;
using FreezeTrace.Core.Buffers;
using FreezeTrace.Core.Models;

namespace FreezeTrace.Agent.Collectors;

internal sealed class WindowsEventCollector : IDisposable
{
    private const int StartupLookbackSeconds = 120;
    private const int MaxEventFields = 32;
    private const int MaxFieldLength = 512;

    private readonly RingBuffer<WindowsEventRecord> _buffer;
    private readonly List<EventLogWatcher> _watchers = [];
    private bool _started;

    public WindowsEventCollector(RingBuffer<WindowsEventRecord> buffer)
    {
        _buffer = buffer;
    }

    public void Start()
    {
        if (_started)
            return;

        _started = true;

        TryStartLog("Application", ApplicationQuery);
        TryStartLog("System", SystemQuery);
        TryStartLog("Microsoft-Windows-NetworkProfile/Operational", NetworkProfileQuery, optional: true);
    }

    private void TryStartLog(string logName, string xpath, bool optional = false)
    {
        try
        {
            SeedRecentEvents(logName, xpath);
            StartWatcher(logName, xpath);
        }
        catch (EventLogNotFoundException)
        {
            if (!optional)
                Console.Error.WriteLine($"FreezeTrace could not open Windows log '{logName}'.");
        }
        catch (UnauthorizedAccessException)
        {
            if (!optional)
                Console.Error.WriteLine($"FreezeTrace does not have permission to read Windows log '{logName}'.");
        }
        catch (EventLogException ex)
        {
            if (!optional)
                Console.Error.WriteLine($"FreezeTrace Event Log error ({logName}): {ex.Message}");
        }
    }

    private void StartWatcher(string logName, string xpath)
    {
        var query = new EventLogQuery(logName, PathType.LogName, xpath);
        var watcher = new EventLogWatcher(query);

        watcher.EventRecordWritten += (_, args) =>
        {
            try
            {
                if (args.EventException is not null || args.EventRecord is null)
                    return;

                using var record = args.EventRecord;
                var item = ConvertRecord(logName, record);
                if (item is not null)
                    _buffer.Add(item);
            }
            catch
            {
                // Event collection is best-effort and must never disturb the host system.
            }
        };

        watcher.Enabled = true;
        _watchers.Add(watcher);
    }

    private void SeedRecentEvents(string logName, string xpath)
    {
        var cutoff = DateTimeOffset.UtcNow.AddSeconds(-StartupLookbackSeconds);
        var query = new EventLogQuery(logName, PathType.LogName, xpath)
        {
            ReverseDirection = true
        };

        using var reader = new EventLogReader(query);
        while (true)
        {
            using var record = reader.ReadEvent();
            if (record is null)
                break;

            var timestamp = record.TimeCreated is { } time
                ? new DateTimeOffset(time).ToUniversalTime()
                : DateTimeOffset.UtcNow;

            if (timestamp < cutoff)
                break;

            var item = ConvertRecord(logName, record);
            if (item is not null)
                _buffer.Add(item);
        }
    }

    private static WindowsEventRecord? ConvertRecord(string logName, EventRecord record)
    {
        if (record.TimeCreated is null)
            return null;

        return new WindowsEventRecord(
            new DateTimeOffset(record.TimeCreated.Value).ToUniversalTime(),
            logName,
            record.ProviderName ?? "Unknown",
            record.Id,
            record.Level,
            GetLevelName(record.Level),
            Message: null,
            record.RecordId,
            ExtractNamedEventData(record));
    }

    private static IReadOnlyDictionary<string, string>? ExtractNamedEventData(EventRecord record)
    {
        try
        {
            var document = XDocument.Parse(record.ToXml(), LoadOptions.None);
            var root = document.Root;
            if (root is null)
                return null;

            var ns = root.Name.Namespace;
            var dataElements = root
                .Descendants(ns + "EventData")
                .Elements(ns + "Data");

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var unnamed = 0;

            foreach (var element in dataElements)
            {
                if (result.Count >= MaxEventFields)
                    break;

                var name = (string?)element.Attribute("Name");
                if (string.IsNullOrWhiteSpace(name))
                    name = $"Data{unnamed++}";

                // WHEA RawData can be very large. Keep it out of the always-on buffer.
                if (name.Equals("RawData", StringComparison.OrdinalIgnoreCase))
                    continue;

                var value = Limit(element.Value, MaxFieldLength);
                if (!string.IsNullOrWhiteSpace(value))
                    result[name] = value;
            }

            return result.Count == 0 ? null : result;
        }
        catch
        {
            return null;
        }
    }

    private static string Limit(string value, int maxLength)
    {
        if (value.Length <= maxLength)
            return value;

        return value[..maxLength] + "…";
    }

    private static string? GetLevelName(byte? level) => level switch
    {
        1 => "Critical",
        2 => "Error",
        3 => "Warning",
        4 => "Information",
        5 => "Verbose",
        _ => null
    };

    public void Dispose()
    {
        foreach (var watcher in _watchers)
        {
            try
            {
                watcher.Enabled = false;
                watcher.Dispose();
            }
            catch
            {
                // Shutdown must remain best-effort.
            }
        }

        _watchers.Clear();
    }

    private const string ApplicationQuery =
        "*[System[(EventID=1000 or EventID=1002)]]";

    private const string SystemQuery =
        "*[System[(EventID=41 or EventID=4101) " +
        "or Provider[@Name='Microsoft-Windows-WHEA-Logger'] " +
        "or Provider[@Name='Display'] " +
        "or Provider[@Name='nvlddmkm'] " +
        "or Provider[@Name='amdkmdag'] " +
        "or Provider[@Name='amdwddmg']]]";

    private const string NetworkProfileQuery =
        "*[System[(EventID=10000 or EventID=10001)]]";
}
