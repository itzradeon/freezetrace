using FreezeTrace.Core.Analysis;
using FreezeTrace.Core.Models;

namespace FreezeTrace.Core.Tests;

public sealed class IncidentAnalyzerEventTests
{
    [Fact]
    public void WheaEventProducesHardwareFindingWithStructuredEvidence()
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
                null,
                42,
                new Dictionary<string, string>
                {
                    ["ErrorSource"] = "3",
                    ["ErrorType"] = "9",
                    ["ApicId"] = "0",
                    ["MCABank"] = "5"
                })
        };

        var findings = analyzer.Analyze([], events);

        var finding = Assert.Single(findings);
        Assert.Equal("hardware-whea", finding.Category);
        Assert.Equal(FindingConfidence.High, finding.Confidence);
        Assert.Contains(finding.Evidence, x => x.Contains("MCABank=5", StringComparison.Ordinal));
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
                null)
        };

        var findings = analyzer.Analyze([], events);

        var finding = Assert.Single(findings);
        Assert.Equal("graphics-stack", finding.Category);
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
        Assert.Equal("unexpected-shutdown", finding.Category);
        Assert.Equal(FindingConfidence.Low, finding.Confidence);
        Assert.Contains(finding.CounterEvidence, x => x.Contains("not the root cause", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void NetworkProfileDisconnectProducesNetworkFinding()
    {
        var analyzer = new IncidentAnalyzer();
        var events = new[]
        {
            new WindowsEventRecord(
                DateTimeOffset.UtcNow,
                "Microsoft-Windows-NetworkProfile/Operational",
                "Microsoft-Windows-NetworkProfile",
                10001,
                4,
                "Information",
                null,
                100,
                new Dictionary<string, string>
                {
                    ["Name"] = "Home LAN"
                })
        };

        var findings = analyzer.Analyze([], events);

        var finding = Assert.Single(findings);
        Assert.Equal("network-disconnect", finding.Category);
        Assert.Equal(FindingConfidence.Medium, finding.Confidence);
        Assert.Contains(finding.Evidence, x => x.Contains("Home LAN", StringComparison.Ordinal));
    }

    [Fact]
    public void NetworkProfileConnectAloneDoesNotProduceFailureFinding()
    {
        var analyzer = new IncidentAnalyzer();
        var events = new[]
        {
            new WindowsEventRecord(
                DateTimeOffset.UtcNow,
                "Microsoft-Windows-NetworkProfile/Operational",
                "Microsoft-Windows-NetworkProfile",
                10000,
                4,
                "Information",
                null)
        };

        var findings = analyzer.Analyze([], events);

        Assert.Empty(findings);
    }

    [Fact]
    public void ApplicationCrashUsesStructuredFaultDetails()
    {
        var analyzer = new IncidentAnalyzer();
        var events = new[]
        {
            new WindowsEventRecord(
                DateTimeOffset.UtcNow,
                "Application",
                "Application Error",
                1000,
                2,
                "Error",
                null,
                55,
                new Dictionary<string, string>
                {
                    ["AppName"] = "game.exe",
                    ["ModuleName"] = "driver.dll",
                    ["ExceptionCode"] = "0xc0000005"
                })
        };

        var findings = analyzer.Analyze([], events);

        var finding = Assert.Single(findings);
        Assert.Equal("application-crash", finding.Category);
        Assert.Contains(finding.Evidence, x => x.Contains("driver.dll", StringComparison.Ordinal));
        Assert.Contains(finding.Evidence, x => x.Contains("0xc0000005", StringComparison.Ordinal));
    }
}
