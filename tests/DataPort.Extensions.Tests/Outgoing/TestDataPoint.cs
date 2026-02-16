using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

namespace ViciOne.Suite.DataPort.Extension.Tests.Outgoing;

/// <summary>
/// Minimal test-only data point configuration.
/// </summary>
#pragma warning disable CA1515 // types are used by NSubstitute and cannot be avoided
public class TestDataPoint : IDataPoint
#pragma warning restore CA1515 // types are used by NSubstitute and cannot be avoided
{
    public required string ChannelName { get; init; }
    public string? Description { get; init; }
    public Channels Channels { get; init; } = new([], []);
    public string AddressString { get; } = "default";
    public string DataTypeString { get; } = "default";
}
