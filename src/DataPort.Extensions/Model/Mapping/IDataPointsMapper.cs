using System.Collections.Generic;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes;

namespace ViciOne.Suite.DataPort.Extensions.Model.Mapping;

/// <summary>
/// Maps configuration nodes to data points.
/// </summary>
/// <typeparam name="TDataPoint">The type of data point to create.</typeparam>
/// <typeparam name="TRootNode">The type of root configuration node.</typeparam>
/// <typeparam name="TDataPortCommunication">The type of communication configuration.</typeparam>
public interface IDataPointsMapper<in TRootNode, TDataPortCommunication, TDataPoint>
    where TRootNode : IRootConfigurationNode<TDataPortCommunication>
    where TDataPortCommunication : DataPortCommunication
    where TDataPoint : IDataPoint
{
    /// <summary>
    /// Converts a root configuration node into a flat list of data points.
    /// </summary>
    /// <param name="deviceNode">The root configuration node to map.</param>
    /// <returns>A list of data points.</returns>
    List<TDataPoint> ToDataPoints(TRootNode deviceNode);
}
