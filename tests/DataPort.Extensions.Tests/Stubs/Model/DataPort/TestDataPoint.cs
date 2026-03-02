using System;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.DataPort;

/// <summary>
/// Minimal test-only data point configuration supporting both incoming (polling) and outgoing data ports.
/// </summary>
internal class TestDataPoint : IPollingDataPoint
{
    public string? Description { get; init; }
    public Channels Channels { get; init; } = new([], []);
    public DataPointIdentifier Identifier { get; } = new(string.Empty);
    public DataTypeName DataTypeName { get; } = new(string.Empty);
    public PollFrequency PollFrequency { get; init; } = new(TimeSpan.FromMilliseconds(100));
}
