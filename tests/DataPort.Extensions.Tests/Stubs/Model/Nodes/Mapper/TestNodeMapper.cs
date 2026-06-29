using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.DataPoints.Int.Mapping;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.DataPoints.String.Mapping;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.Device;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.Device.Mapping;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.Folders.Mapping;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.Mapper;

internal static class TestNodeMapper
{
    public static TypedNodeMapper<TestCommunication, TestDeviceNode> Instance() =>
        new(
            new TestDeviceNodeMapper(),
            [new TestGroupNodeMapper(), new TestFolderNodeMapper()],
            [new IntNodeMapper(), new StringNodeMapper()]);
}
