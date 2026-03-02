using System.Collections.Generic;
using System.Linq;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes;

namespace ViciOne.Suite.DataPort.Extensions.Model.Mapping;

/// <summary>
/// Maps configuration nodes to grouped polling data points.
/// Extends the base mapper with grouping functionality for incoming data ports.
/// </summary>
/// <typeparam name="TDataPoint">The type of polling data point.</typeparam>
/// <typeparam name="TDataPointGroup">The type of data point group.</typeparam>
/// <typeparam name="TRootNode">The type of root configuration node.</typeparam>
/// <typeparam name="TDataPortCommunication">The type of communication configuration.</typeparam>
public interface IDataPointGroupsMapper<TDataPoint, TDataPointGroup, in TRootNode, TDataPortCommunication>
    : IDataPointsMapper<TRootNode, TDataPortCommunication, TDataPoint>
    where TDataPoint : IPollingDataPoint
    where TRootNode : IRootConfigurationNode<TDataPortCommunication>
    where TDataPortCommunication : DataPortCommunication
    where TDataPointGroup : IDataPointPollingGroup<TDataPoint>
{
    /// <summary>
    /// Converts a root configuration node into grouped data points.
    /// Default implementation calls ToDataPoints and then Group.
    /// </summary>
    /// <param name="deviceNode">The root configuration node to map.</param>
    /// <returns>A list of data point groups.</returns>
    IReadOnlyList<TDataPointGroup> ToDataPointGroups(TRootNode deviceNode) => Group(ToDataPoints(deviceNode));

    /// <summary>
    /// Groups a flat list of data points by their poll frequency.
    /// </summary>
    /// <param name="dataPoints">The data points to group.</param>
    /// <returns>A list of data point groups.</returns>
    IReadOnlyList<TDataPointGroup> Group(IReadOnlyList<TDataPoint> dataPoints) =>
    [
        .. dataPoints
            .GroupBy(dp => dp.PollFrequency)
            .Select(group => CreateGroup(group.Key, [.. group]))
    ];

    /// <summary>
    /// Creates a single data point group with the specified poll frequency and data points.
    /// </summary>
    /// <param name="pollFrequency">The poll frequency for the group.</param>
    /// <param name="dataPoints">The data points in the group.</param>
    /// <returns>A new data point group.</returns>
    TDataPointGroup CreateGroup(PollFrequency pollFrequency, IReadOnlyList<TDataPoint> dataPoints);
}
