using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using FreezeTrace.Core.Analysis;
using FreezeTrace.Core.Models;

namespace FreezeTrace.Agent;

internal sealed class IncidentRecorder
{
    private readonly IncidentAnalyzer _analyzer = new();

    public async Task<string> SaveAsync(
        string trigger,
        IReadOnlyList<TelemetrySample> samples,
        CancellationToken cancellationToken)
    {
        var id = $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..8]}";
        var findings = _analyzer.Analyze(samples);

        var machine = new MachineMetadata(
            RuntimeInformation.OSDescription,
            RuntimeInformation.OSArchitecture.ToString(),
            RuntimeInformation.ProcessArchitecture.ToString(),
            RuntimeInformation.FrameworkDescription,
            Environment.ProcessorCount);

        var incident = new Incident(
            id,
            DateTimeOffset.UtcNow,
            trigger,
            samples,
            findings,
            machine);

        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FreezeTrace",
            "incidents",
            id);

        Directory.CreateDirectory(root);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var path = Path.Combine(root, "incident.json");
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, incident, options, cancellationToken);

        return path;
    }
}
