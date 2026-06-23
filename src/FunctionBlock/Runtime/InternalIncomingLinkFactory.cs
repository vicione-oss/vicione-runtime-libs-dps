using System;
using System.Reflection;
using ViciOne.Core.Contracts;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Engine;

namespace ViciOne.Suite.System.Runtime;

internal class InternalIncomingLinkFactory : GenericFactory
{
    public InternalIncomingLinkFactory() : base(GetGenericCreateMethod()) { }

    public ExternalIncomingLink Create(Connector destination)
        => Create<ExternalIncomingLink>([destination,],
            DataTypeCompatibilityValidator.DetermineLinkValueType(destination),
            DataTypeCompatibilityValidator.DetermineLinkValueType(destination));

    private static InternalIncomingLink<TValue, TDestination> CreateLink<TValue, TDestination>(ConnectorOutput<TDestination> destination)
        => new()
        {
            Id = Guid.NewGuid(),
            Destination = destination,
        };

    private static MethodInfo GetGenericCreateMethod()
        => typeof(InternalIncomingLinkFactory).GetMethod(nameof(CreateLink), BindingFlags.NonPublic | BindingFlags.Static) ?? throw new InvalidOperationException();
}
