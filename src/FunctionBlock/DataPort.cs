using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Engine;
using ViciOne.Suite.System.Contracts;
using ViciOne.Suite.System.Runtime;
using ViciOne.Suite.System.Transfer;

namespace ViciOne.Suite.System;

public partial class DataPort
{
    private readonly List<ITransferHandler> _transferHandlers = [];
    private readonly DefaultConnectorValueTransferInterceptor _defaultConnectorValueTransferInterceptor = new();
    private Dictionary<Guid, IInputProcessor> _inputProcessors = [];
    private bool _disposed;

    internal IList<IRuntimeConnector> ProcessDataInputConnectors => ProcessDataInputs;

    internal void Process()
    {
        ProcessInputs();
        ProcessHandlers();
    }

    protected override void ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
    {
        if (FunctionBlockEnabled)
            Process();
    }

    public override void InitializeI()
    {
        base.InitializeI();

        if (Settings.TransferModes?.Value is null)
            throw new InvalidOperationException("Transfer modes must be set.");

        ConnectionFactory connectionFactory = new(this);
        _inputProcessors = connectionFactory.CreateInputProcessors(_defaultConnectorValueTransferInterceptor);
        var linkGroups = connectionFactory.CreateInputLinkGroups();

        foreach (var (connectorId, transferMode) in Settings.TransferModes.Value)
            _transferHandlers.Add(CreateTransferHandler(transferMode, linkGroups[connectorId]));
    }

    private ITransferHandler CreateTransferHandler(ITransferMode transferMode, IIncomingInputLinkGroup linkGroup)
    {
        ITransferHandler handler = transferMode switch
        {
            OnChangedTransferMode => new OnChangedTransferHandler(linkGroup),
            CyclicTransferMode m => new CyclicTransferHandler(m, linkGroup),
            PeriodicTransferMode m => new PeriodicTransferHandler(this, m, linkGroup),
            _ => throw new NotSupportedException($"Transfer mode '{transferMode.GetType().Name}' is not supported."),
        };

        if (RunModeIsNotValid(handler, RunMode))
            throw new InvalidOperationException($"The transfer handler '{handler.GetType()}' cannot be executed with run mode '{RunMode}'.");

        return handler;
    }

    private static bool RunModeIsNotValid(ITransferHandler handler, RunMode runMode)
    {
        var validRunModes = handler.GetType().GetCustomAttribute<ValidRunModesAttribute>()?.ValidRunModes;
        return validRunModes?.Contains(runMode) is false;
    }

    private void ProcessInputs()
    {
        foreach (var connector in ProcessDataInputs)
        {
            if (connector.Changed && connector.Value is not null)
                _inputProcessors[connector.Connector.Id].ProcessInput(connector);
        }
    }

    private void ProcessHandlers()
    {
        foreach (var handler in _transferHandlers)
            handler.ProcessCycle();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!_disposed)
        {
            if (disposing)
            {
                DisposeTransferHandlers();
                _transferHandlers.Clear();
                _inputProcessors.Clear();
                _defaultConnectorValueTransferInterceptor.Dispose();
            }

            _disposed = true;
        }

        void DisposeTransferHandlers()
        {
            foreach (var handler in _transferHandlers)
            {
                if (handler is IDisposable disposable)
                    disposable.Dispose();
                else if (handler is IAsyncDisposable asyncDisposable)
                    asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }
    }
}
