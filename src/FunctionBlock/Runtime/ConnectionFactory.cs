using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Engine;
using ViciOne.Suite.System.Contracts;

namespace ViciOne.Suite.System.Runtime;

internal readonly ref struct ConnectionFactory
{
    private static readonly DefaultPoolingValidator s_defaultPoolingValidator = new();

    private readonly Dictionary<Guid, IRuntimeConnector> _inputs;
    private readonly Dictionary<Guid, IRuntimeConnector> _outputs;
    private readonly Dictionary<Guid, IRuntimeConnector> _connectedConnectors;
    private readonly List<NodeLink> _directLinks;
    private readonly Dictionary<Guid, IInputProcessor> _inputProcessors = [];
    private readonly Dictionary<IRuntimeConnector, List<IRuntimeLink>> _runtimeLinks = [];

    public ConnectionFactory(DataPort dataPort)
    {
        if (dataPort.Settings.NodeLinks?.Value is null)
            throw new InvalidOperationException("Node links setting must be set.");

        if (dataPort.Settings.TransferModes?.Value is null)
            throw new InvalidOperationException("Transfer modes must be set.");

        var fb = (IRuntimeFunctionBlock)dataPort;

        _inputs = fb.ProcessDataInputs.ToDictionary(c => c.Connector.Id, c => c);
        _outputs = fb.ProcessDataOutputs.ToDictionary(c => c.Connector.Id, c => c);
        _connectedConnectors = fb.ProcessDataInputs.Concat(fb.ProcessDataOutputs).ToDictionary(c => c.Connector.Id, c => c);

        _directLinks = dataPort.Settings.NodeLinks.Value;
    }

    internal readonly Dictionary<Guid, IInputProcessor> CreateInputProcessors(IConnectorValueTransferInterceptor defaultConnectorValueTransferInterceptor)
    {
        CreateDirectLinks(defaultConnectorValueTransferInterceptor);
        return _inputProcessors;
    }

    private readonly void CreateDirectLinks(IConnectorValueTransferInterceptor defaultConnectorValueTransferInterceptor)
    {
        using RuntimeLinkFactory runtimeLinkFactory = new();
        using InternalLinkFactory internalLinkFactory = new();
        using OutputLinkGroupFactory outgoingInputLinkGroupFactory = new();

        foreach (var nodeLinks in _directLinks.GroupBy(l => l.InputConnectorId))
        {
            List<IRuntimeLink> inputLinks = [];
            var inputConnector = _inputs[nodeLinks.Key];

            foreach (var nodeLink in nodeLinks)
            {
                var output = _outputs[nodeLink.OutputConnectorId];
                var linkDefinition = internalLinkFactory.Create((Connector)inputConnector.Connector, (Connector)output.Connector);
                var runtimeLink = runtimeLinkFactory.Create(linkDefinition, _connectedConnectors);

                inputLinks.Add(runtimeLink);
                _runtimeLinks.AddOrUpdate(output, runtimeLink);
            }

            _inputProcessors.Add(inputConnector.Connector.Id, new PasstroughProcessor(outgoingInputLinkGroupFactory.Create(inputConnector, inputLinks), defaultConnectorValueTransferInterceptor));
        }
    }

    public readonly Dictionary<Guid, IIncomingInputLinkGroup> CreateInputLinkGroups()
    {
        using IncomingInputLinkGroupFactory incomingInputLinkGroupFactory = new();
        Dictionary<Guid, IIncomingInputLinkGroup> linkGroups = [];

        foreach (var group in _runtimeLinks)
        {
            var linkGroup = incomingInputLinkGroupFactory.Create(group.Key, group.Value, () => s_defaultPoolingValidator);
            linkGroups.Add(group.Key.Connector.Id, linkGroup);
        }

        return linkGroups;
    }
}
