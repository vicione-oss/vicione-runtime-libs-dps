using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine;

namespace ViciOne.Suite.System.Runtime;

internal class PasstroughProcessor(IOutputLinkGroup outputLinkGroup, IConnectorValueTransferInterceptor defaultConnectorValueTransferInterceptor)
    : IInputProcessor
{
    public void ProcessInput(IRuntimeConnector _)
        => outputLinkGroup.TransferValueToLinks(null, defaultConnectorValueTransferInterceptor);
}
