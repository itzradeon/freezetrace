using FreezeTrace.Agent;
using FreezeTrace.Agent.Collectors;
using FreezeTrace.Core.Buffers;
using FreezeTrace.Core.Models;

if (!OperatingSystem.IsWindows())
{
    Console.Error.WriteLine("FreezeTrace.Agent currently requires Windows.");
    return 1;
}

const int sampleIntervalSeconds = 1;
const int preIncidentSeconds = 120;
const int postIncidentSeconds = 15;
const int eventBufferCapacity = 256;

var buffer = new RingBuffer<TelemetrySample>(preIncidentSeconds / sampleIntervalSeconds);
var eventBuffer = new RingBuffer<WindowsEventRecord>(eventBufferCapacity);
var collector = new WindowsSystemCollector();
using var eventCollector = new WindowsEventCollector(eventBuffer);
var recorder = new IncidentRecorder();
using var cts = new CancellationTokenSource();

eventCollector.Start();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cts.Cancel();
};

Console.WriteLine("""
FreezeTrace Agent — v0.2.0

S  Save an incident (keeps last 2 minutes + 15 seconds after trigger)
Q  Quit

FreezeTrace is local-first. Telemetry and selected Windows events stay in bounded memory buffers until an incident is explicitly saved.
Automatic incident writes are intentionally disabled in v0.2.0 to avoid disrupting foreground workloads.
""");

var samplingTask = Task.Run(async () =>
{
    while (!cts.IsCancellationRequested)
    {
        try
        {
            var sample = collector.Collect();
            buffer.Add(sample);

            Console.Title =
                $"FreezeTrace | CPU {sample.CpuUsagePercent:F0}% | RAM {sample.MemoryUsagePercent:F0}% | samples {buffer.Count}/{buffer.Capacity} | events {eventBuffer.Count}";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Collector error: {ex.Message}");
        }

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(sampleIntervalSeconds), cts.Token);
        }
        catch (OperationCanceledException)
        {
            break;
        }
    }
}, cts.Token);

while (!cts.IsCancellationRequested)
{
    if (!Console.KeyAvailable)
    {
        try
        {
            await Task.Delay(100, cts.Token);
        }
        catch (OperationCanceledException)
        {
            break;
        }

        continue;
    }

    var key = Console.ReadKey(intercept: true).Key;

    if (key == ConsoleKey.Q)
    {
        cts.Cancel();
        break;
    }

    if (key != ConsoleKey.S)
        continue;

    var before = buffer.Snapshot();
    var beforeEvents = eventBuffer.Snapshot();
    var triggeredAt = DateTimeOffset.UtcNow;

    Console.WriteLine($"[{DateTime.Now:T}] Incident triggered. Capturing {postIncidentSeconds}s after the event...");

    var after = new List<TelemetrySample>();
    var end = DateTimeOffset.UtcNow.AddSeconds(postIncidentSeconds);

    while (DateTimeOffset.UtcNow < end && !cts.IsCancellationRequested)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
        }
        catch (OperationCanceledException)
        {
            break;
        }

        var snapshot = buffer.Snapshot();
        if (snapshot.Count > 0)
            after.Add(snapshot[^1]);
    }

    var merged = before
        .Concat(after)
        .GroupBy(x => x.Timestamp)
        .Select(g => g.First())
        .OrderBy(x => x.Timestamp)
        .ToArray();

    var eventWindowStart = triggeredAt.AddSeconds(-preIncidentSeconds);
    var eventWindowEnd = triggeredAt.AddSeconds(postIncidentSeconds + 2);

    var mergedEvents = beforeEvents
        .Concat(eventBuffer.Snapshot())
        .Where(x => x.Timestamp >= eventWindowStart && x.Timestamp <= eventWindowEnd)
        .GroupBy(x => x.RecordId is long recordId
            ? $"{x.LogName}:{recordId}"
            : $"{x.Timestamp.UtcTicks}:{x.LogName}:{x.Provider}:{x.EventId}")
        .Select(g => g.First())
        .OrderBy(x => x.Timestamp)
        .ToArray();

    try
    {
        var path = await recorder.SaveAsync("manual", merged, mergedEvents, cts.Token);
        Console.WriteLine($"Incident saved: {path}");
        Console.WriteLine($"Included {mergedEvents.Length} relevant Windows event(s).");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Could not save incident: {ex.Message}");
    }
}

try
{
    await samplingTask;
}
catch (OperationCanceledException)
{
    // Expected during shutdown.
}

return 0;
