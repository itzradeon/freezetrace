using FreezeTrace.Core.Analysis;
using FreezeTrace.Core.Models;

namespace FreezeTrace.Core.Tests;

public sealed class IncidentAnalyzerEventTests
{
    [Fact]
    public void WheaEventProducesHighConfidenceHardwareFinding()
    {
        var analyzer = new IncidentAnalyzer();
        var events = new[]
        {
            new WindowsEventRecord(
                DateTimeOffset.UtcNow,
                "System",
                "Microsoft-Windows-WHEA-Logger",
                18,
                2,
                "Error",
                "A fatal hardware error has occurred.")
        };

        var findings = analyzer.Analyze([], events);

        var finding = Assert.Single(findings);
        Assert.Equal("hardware-whea", finding.Id);
        Assert.Equal(FindingConfidence.High, finding.Confidence);
    }

    [Fact]
    public void Display4101ProducesGraphicsFinding()
    {
        var analyzer = new IncidentAnalyzer();
        var events = new[]
        {
            new WindowsEventRecord(
                DateTimeOffset.UtcNow,
                "System",
                "Display",
                4101,
                3,
                "Warning",
                "Display driver stopped responding and has recovered.")
        };

        var findings = analyzer.Analyze([], events);

        var finding = Assert.Single(findings);
        Assert.Equal("graphics-stack", finding.Id);
        Assert.Equal(FindingConfidence.High, finding.Confidence);
    }

    [Fact]
    public void KernelPower41IsNotPresentedAsRootCause()
    {
        var analyzer = new IncidentAnalyzer();
        var events = new[]
        {
            new WindowsEventRecord(
                DateTimeOffset.UtcNow,
                "System",
                "Microsoft-Windows-Kernel-Power",
                41,
                1,
                "Critical",
                null)
        };

        var findings = analyzer.Analyze([], events);

        var finding = Assert.Single(findings);
        Assert.Equal("unexpected-shutdown", finding.Id);
        Assert.Equal(FindingConfidence.Low, finding.Confidence);
        Assert.Contains(finding.CounterEvidence, x => x.Contains("not the root cause", StringComparison.OrdinalIgnoreCase));
    }
}
