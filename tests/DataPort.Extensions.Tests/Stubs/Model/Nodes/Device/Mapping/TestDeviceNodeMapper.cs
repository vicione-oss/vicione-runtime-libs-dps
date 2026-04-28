using FluentValidation.Results;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.Device.Mapping;

internal sealed class TestDeviceNodeMapper
    : IRootConfigurationNodeMapper<TestDeviceNode, TestCommunication>
{
    public TestDeviceNode CreateRootNode(TestCommunication communication) =>
        new(communication.DesignId, communication);

    public ValidationResult Validate(TestCommunication communication) => new();
}
