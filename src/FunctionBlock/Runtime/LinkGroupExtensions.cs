using System.Linq;
using ViciOne.Engine;

namespace ViciOne.Suite.System.Runtime;

internal static class LinkGroupExtensions
{
    private static readonly EmptyConnectorValueTransferInterceptor s_interceptor = new();
    private static readonly DefaultPoolingValidator s_poolingValidator = new();

    internal static void PoolIfChanged(this IIncomingInputLinkGroup linkGroup)
    {
        if (linkGroup.Links.Any(link => link.Changed) || linkGroup.NeedsFurtherProcessing)
            linkGroup.Pool(s_poolingValidator, s_interceptor);
    }

    internal static void Pool(this IIncomingInputLinkGroup linkGroup)
        => linkGroup.Pool(s_poolingValidator, s_interceptor);
}
