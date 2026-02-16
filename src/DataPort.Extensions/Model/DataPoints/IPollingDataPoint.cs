using System.Diagnostics.CodeAnalysis;

namespace ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

/// <summary>
/// Extends <see cref="IDataPoint"/> with polling-specific configuration for data points
/// that are periodically read from an external system.
/// </summary>
/// <remarks>
/// Polling data points are used in incoming data ports where data is actively fetched
/// at regular intervals rather than being pushed by the external system.
/// </remarks>
[SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
public interface IPollingDataPoint : IDataPoint
{
    /// <summary>
    /// Gets or initializes the frequency at which this data point should be polled.
    /// </summary>
    PollFrequency PollFrequency { get; init; }
}
