using System.Collections.Generic;
using System.Linq;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.DataPort;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.DataPort.DataPoints.Int;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.DataPort.DataPoints.String;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.DataPoints.Int;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.DataPoints.String;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.Device;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;
using ViciOne.Suite.DataPort.Extensions.Model.Mapping;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Mapping;

/// <summary>
/// Test mapper that extracts data points from node structure and uses normal grouping logic.
/// </summary>
internal sealed class TestDataPointGroupsMapper
    : IDataPointGroupsMapper<TestDataPoint, TestDataPointGroup, TestDeviceNode, TestCommunication>
{
    public IReadOnlyList<TestDataPoint> ToDataPoints(TestDeviceNode deviceNode)
        =>
        [
            .. deviceNode.DataPointNodes
                .Select(MapToDataPoint)
                .OfType<TestDataPoint>()
        ];

    // Maps each node type to its matching strongly-typed data point; unknown node types are ignored.
    private static TestDataPoint? MapToDataPoint(IDataPointNode node) => node switch
    {
        StringNode stringNode => new StringDataPoint
        {
            Channels = stringNode.Channels,
            PollFrequency = stringNode.PollFrequency
        },
        IntNode intNode => new IntDataPoint
        {
            Channels = intNode.Channels,
            PollFrequency = intNode.PollFrequency
        },
        _ => null
    };

    public TestDataPointGroup CreateGroup(PollFrequency pollFrequency, IReadOnlyList<TestDataPoint> dataPoints)
        => new()
        {
            DataPoints = dataPoints,
            PollFrequency = pollFrequency
        };
}
