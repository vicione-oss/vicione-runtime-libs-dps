using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

/// <summary>
/// Represents a group of data points that share the same polling frequency.
/// </summary>
/// <typeparam name="TPollingDataPointType">The type of polling data points in this group, must implement <see cref="IPollingDataPoint"/>.</typeparam>
/// <remarks>
/// Polling groups allow efficient batching of read operations for data points that need to be
/// polled at the same interval. This reduces the number of connections and improves throughput
/// when communicating with external systems.
/// </remarks>
[SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
public interface IDataPointPollingGroup<TPollingDataPointType> where TPollingDataPointType : IPollingDataPoint
{
    /// <summary>
    /// Gets or initializes the polling frequency for all data points in this group.
    /// </summary>
    PollFrequency PollFrequency { get; init; }

    /// <summary>
    /// Gets or initializes the collection of data points that belong to this polling group.
    /// </summary>
    IReadOnlyList<TPollingDataPointType> DataPoints { get; init; }
}
