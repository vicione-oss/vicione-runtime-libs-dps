using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel;

namespace ViciOne.Suite.System.Runtime;

internal class InternalLink<TSource, TValue, TDestination> : InternalLink, IInternalLink<TSource, TValue, TDestination>
{
    public InternalLink()
    {
        Destination = new();
        Source = new();
    }

    public new ConnectorOutput<TDestination> Destination
    {
        get => (ConnectorOutput<TDestination>)base.Destination;
        set => base.Destination = value;
    }

    IConnector<TDestination> ILinkWithDestination<TValue, TDestination>.Destination => Destination;

    public new ConnectorInput<TSource> Source
    {
        get => (ConnectorInput<TSource>)base.Source;
        set => base.Source = value;
    }

    IConnector<TSource> ILinkWithSource<TSource>.Source => Source;
}
