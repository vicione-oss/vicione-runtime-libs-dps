using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing.Utilities;

/// <summary>
/// Builds O(1) channel lookup dictionaries for fast channel→datapoint mapping.
/// </summary>
public static class ChannelLookupBuilder
{
    /// <summary>
    /// Builds an immutable dictionary mapping channels to data points.
    /// </summary>
    /// <typeparam name="TDataPoint">Data point type</typeparam>
    /// <param name="dataPoints">Collection of data points</param>
    /// <param name="channelSelector">Function to extract channels from a data point</param>
    /// <returns>Read-only dictionary with channel keys and data point values</returns>
    public static IReadOnlyDictionary<string, TDataPoint> BuildLookup<TDataPoint>(
        IEnumerable<TDataPoint> dataPoints,
        Func<TDataPoint, IEnumerable<string>> channelSelector)
    {
        var map = new Dictionary<string, TDataPoint>();

        foreach (var dataPoint in dataPoints)
        {
            foreach (var channel in channelSelector(dataPoint))
            {
                map[channel] = dataPoint;
            }
        }

        return new ReadOnlyDictionary<string, TDataPoint>(map);
    }
}
