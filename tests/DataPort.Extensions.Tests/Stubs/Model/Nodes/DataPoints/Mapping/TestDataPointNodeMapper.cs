using FluentValidation.Results;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.DataPoints.Mapping;

internal sealed class TestDataPointNodeMapper : IDataPointNodeMapper<TestDataPointNodeWithProperties>
{
    public string TargetLinkedNodeTypeId => "DataPoint";

    public TestDataPointNodeWithProperties Map(LinkedNode node) => new(node)
    {
        DataValue = node.GetRequiredPropertyValue<string>("DataValue"),
        Timestamp = node.GetRequiredPropertyValue<long>("Timestamp")
    };

    public ValidationResult Validate(LinkedNode linkedNode) => new();
}
