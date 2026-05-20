using FluentValidation.Results;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.DataPoints;

/// <summary>
/// Shared base for data point node mappers. Parses the common poll-frequency property and supplies a
/// passing default validation, leaving subclasses to declare their target id and build the typed node.
/// </summary>
internal abstract class DataPointNodeMapperBase<TNode> : IDataPointNodeMapper<TNode>
    where TNode : IDataPointNode
{
    public abstract string TargetLinkedNodeTypeId { get; }

    public TNode Map(LinkedNode node) => CreateNode(node, ReadPollFrequency(node));

    public virtual ValidationResult Validate(LinkedNode linkedNode) => new();

    protected abstract TNode CreateNode(LinkedNode node, PollFrequency pollFrequency);

    private static PollFrequency ReadPollFrequency(LinkedNode node)
        => PollFrequency.FromMilliseconds(node.GetRequiredPropertyValue<int>(DataPointNodeBase.PollFrequencyPropertyId));
}
