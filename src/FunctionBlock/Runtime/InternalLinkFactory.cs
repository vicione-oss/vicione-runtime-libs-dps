using System;
using System.Reflection;
using ViciOne.Core.Contracts;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Engine;

namespace ViciOne.Suite.System.Runtime;

internal class InternalLinkFactory : GenericFactory
{
    public InternalLinkFactory() : base(GetGenericCreateMethod()) { }

    public InternalLink Create(Connector source, Connector destination)
        => Create<InternalLink>([source, destination,],
            DataTypeCompatibilityValidator.DetermineValueType(source), // ignore poolable
            DataTypeCompatibilityValidator.DetermineLinkValueType(destination));

    private static InternalLink<TSource, TSource, TDestination> CreateLink<TSource, TDestination>(ConnectorInput<TSource> source, ConnectorOutput<TDestination> destination)
        => new()
        {
            Id = Guid.NewGuid(),
            Source = source,
            Destination = destination,
        };

    private static MethodInfo GetGenericCreateMethod()
        => typeof(InternalLinkFactory).GetMethod(nameof(CreateLink), BindingFlags.NonPublic | BindingFlags.Static) ?? throw new InvalidOperationException();
}
