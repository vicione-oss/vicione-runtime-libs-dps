using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Engine;
using ViciOne.Suite.System.Contracts;
using ViciOne.Suite.System.Runtime;

namespace ViciOne.Suite.System.Transfer;

[ValidRunModes(RunMode.Cyclic)]
internal class CyclicTransferHandler : ITransferHandler
{
    private readonly IIncomingInputLinkGroup _linkGroup;

    private readonly uint _interval;
    private ulong _cycle = 1;

    internal CyclicTransferHandler(CyclicTransferMode transferMode, IIncomingInputLinkGroup linkGroup)
    {
        _interval = transferMode.Interval;
        _linkGroup = linkGroup;
    }

    public void ProcessCycle()
    {
        if (_cycle++ % _interval == 0)
            _linkGroup.Pool();
    }
}
