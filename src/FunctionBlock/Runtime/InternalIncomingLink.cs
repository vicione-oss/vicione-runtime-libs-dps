using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel;

namespace ViciOne.Suite.System.Runtime;

internal class InternalIncomingLink<TValue, TDestination> : ExternalIncomingLink, IExternalIncomingLink<TValue, TValue, TDestination>
{
    public InternalIncomingLink()
    {
        Destination = new();
        DefaultValue = default;
    }

    public new ConnectorOutput<TDestination> Destination
    {
        get => (ConnectorOutput<TDestination>)base.Destination;
        set => base.Destination = value;
    }

    IConnector<TDestination> ILinkWithDestination<TValue, TDestination>.Destination => Destination;

    public new TValue? DefaultValue
    {
        get => (TValue?)base.DefaultValue;
        set => base.DefaultValue = value;
    }
}
