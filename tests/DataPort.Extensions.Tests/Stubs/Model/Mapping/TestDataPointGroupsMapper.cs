using System.Collections.Generic;
using System.Linq;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.DataPort;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.DataPoints;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.Device;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;
using ViciOne.Suite.DataPort.Extensions.Model.Mapping;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Mapping;

/// <summary>
/// Test mapper that extracts data points from node structure and uses normal grouping logic.
/// </summary>
internal sealed class TestDataPointGroupsMapper
    : IDataPointGroupsMapper<TestDataPoint, TestDataPointGroup, TestDeviceNode, TestCommunication>
{
    public IReadOnlyList<TestDataPoint> ToDataPoints(TestDeviceNode deviceNode) =>
    [
        .. deviceNode.DataPointNodes.OfType<TestDataPointNode>()
            .Select(dataPointNode => new TestDataPoint
            {
                Channels = dataPointNode.Channels,
                PollFrequency = dataPointNode.PollFrequency
            })
    ];

    public TestDataPointGroup CreateGroup(PollFrequency pollFrequency, IReadOnlyList<TestDataPoint> dataPoints) =>
        new()
        {
            DataPoints = dataPoints,
            PollFrequency = pollFrequency
        };
}
