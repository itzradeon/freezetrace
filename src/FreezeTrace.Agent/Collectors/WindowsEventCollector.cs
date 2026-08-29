using System.Diagnostics.Eventing.Reader;
using FreezeTrace.Core.Buffers;
using FreezeTrace.Core.Models;

namespace FreezeTrace.Agent.Collectors;

internal sealed class WindowsEventCollector : IDisposable
{
    private const int StartupLookbackSeconds = 120;

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

        SeedRecentEvents("Application", ApplicationQuery);
        SeedRecentEvents("System", SystemQuery);

        StartWatcher("Application", ApplicationQuery);
        StartWatcher("System", SystemQuery);
    }

    private void StartWatcher(string logName, string xpath)
    {
        var query = new EventLogQuery(logName, PathType.LogName, xpath);
        var watcher = new EventLogWatcher(query);

        watcher.EventRecordWritten += (_, args) =>
        {
            if (args.EventException is not null)
            {
                Console.Error.WriteLine($"Event Log watcher error ({logName}): {args.EventException.Message}");
                return;
            }

            if (args.EventRecord is null)
                return;

            using var record = args.EventRecord;
            var item = ConvertRecord(logName, record);
            if (item is not null)
                _buffer.Add(item);
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

        string? message;
        try
        {
            message = record.FormatDescription();
        }
        catch
        {
            message = null;
        }

        return new WindowsEventRecord(
            new DateTimeOffset(record.TimeCreated.Value).ToUniversalTime(),
            logName,
            record.ProviderName ?? "Unknown",
            record.Id,
            record.Level,
            record.LevelDisplayName,
            message);
    }

    public void Dispose()
    {
        foreach (var watcher in _watchers)
        {
            watcher.Enabled = false;
            watcher.Dispose();
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
}
