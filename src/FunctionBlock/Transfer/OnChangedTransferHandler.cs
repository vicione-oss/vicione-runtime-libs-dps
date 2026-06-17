using ViciOne.Engine;
using ViciOne.Suite.System.Runtime;

namespace ViciOne.Suite.System.Transfer;

internal class OnChangedTransferHandler : ITransferHandler
{
    private readonly IIncomingInputLinkGroup _linkGroup;

    internal OnChangedTransferHandler(IIncomingInputLinkGroup linkGroup)
        => _linkGroup = linkGroup;

    public void ProcessCycle()
        => _linkGroup.PoolIfChanged();
}
