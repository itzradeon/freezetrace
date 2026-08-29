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

var buffer = new RingBuffer<TelemetrySample>(preIncidentSeconds / sampleIntervalSeconds);
var collector = new WindowsSystemCollector();
var recorder = new IncidentRecorder();
using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cts.Cancel();
};

Console.WriteLine("""
FreezeTrace Agent — early MVP

S  Save an incident (keeps last 2 minutes + 15 seconds after trigger)
Q  Quit

Telemetry remains in memory until an incident is saved.
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
                $"FreezeTrace | CPU {sample.CpuUsagePercent:F0}% | RAM {sample.MemoryUsagePercent:F0}% | samples {buffer.Count}/{buffer.Capacity}";
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
        await Task.Delay(100, cts.Token).ContinueWith(_ => { });
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
    Console.WriteLine($"[{DateTime.Now:T}] Incident triggered. Capturing {postIncidentSeconds}s after the event...");

    var after = new List<TelemetrySample>();
    var end = DateTimeOffset.UtcNow.AddSeconds(postIncidentSeconds);

    while (DateTimeOffset.UtcNow < end && !cts.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
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

    try
    {
        var path = await recorder.SaveAsync("manual", merged, cts.Token);
        Console.WriteLine($"Incident saved: {path}");
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
