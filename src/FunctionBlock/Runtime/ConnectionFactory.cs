using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using ViciOne.Core.Contracts;
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
    private readonly List<NodeLink> _linksWithExtraction;
    private readonly List<NodeLink> _linksWithComposition;
    private readonly Dictionary<Guid, IInputProcessor> _inputProcessors = [];
    private readonly Dictionary<IRuntimeConnector, List<IRuntimeLink>> _runtimeLinks = [];
    private readonly ILogger _logger;

    public ConnectionFactory(DataPort dataPort, ILogger logger)
    {
        if (dataPort.Settings.NodeLinks?.Value is null)
            throw new InvalidOperationException("Node links setting must be set.");

        if (dataPort.Settings.TransferModes?.Value is null)
            throw new InvalidOperationException("Transfer modes must be set.");

        var fb = (IRuntimeFunctionBlock)dataPort;

        _inputs = fb.ProcessDataInputs.ToDictionary(c => c.Connector.Id, c => c);
        _outputs = fb.ProcessDataOutputs.ToDictionary(c => c.Connector.Id, c => c);
        _connectedConnectors = fb.ProcessDataInputs.Concat(fb.ProcessDataOutputs).ToDictionary(c => c.Connector.Id, c => c);

        ClassifyLinks(dataPort.Settings.NodeLinks.Value, out _directLinks, out _linksWithExtraction, out _linksWithComposition);
        _logger = logger;
    }

    private static void ClassifyLinks(List<NodeLink> links, out List<NodeLink> directLinks, out List<NodeLink> linksWithExtraction, out List<NodeLink> linksWithComposition)
    {
        directLinks = [];
        linksWithExtraction = [];
        linksWithComposition = [];

        foreach (var link in links)
        {
            switch (link.Modification)
            {
                case NodeLinkModification.Extract:
                    linksWithExtraction.Add(link);
                    break;
                case NodeLinkModification.Compose:
                    linksWithComposition.Add(link);
                    break;
                default:
                    directLinks.Add(link);
                    break;
            }
        }
    }

    internal readonly Dictionary<Guid, IInputProcessor> CreateInputProcessors(IConnectorValueTransferInterceptor defaultConnectorValueTransferInterceptor, out List<ComposeProcessor> composers)
    {
        CreateDirectLinks(defaultConnectorValueTransferInterceptor);
        CreateLinksWithExtraction();
        CreateLinksWithComposition(out composers);

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

    private readonly void CreateLinksWithExtraction()
    {
        using InternalIncomingLinkFactory internalIncomingLinkFactory = new();
        using RuntimeLinkFactory runtimeLinkFactory = new();
        using TypeAnalyzer typeAnalyzer = new(_logger);

        foreach (var nodeLink in _linksWithExtraction)
        {
            var typeInfo = typeAnalyzer.AnalyzeType(DataTypeCompatibilityValidator.DetermineValueType(_inputs[nodeLink.InputConnectorId].Connector.Design.ConnectorType)).First(e => e.Path == nodeLink.PropertyPath);
            var output = _outputs[nodeLink.OutputConnectorId];
            var linkDefinition = internalIncomingLinkFactory.Create((Connector)output.Connector);
            var runtimeLink = runtimeLinkFactory.Create(linkDefinition, new Dictionary<Guid, IRuntimeConnector>());
            var getProperty = typeInfo.PropertyGetter ?? throw new InvalidOperationException("Property getter cannot be null.");

            _inputProcessors.Add(nodeLink.InputConnectorId, new ExtractProcessor(getProperty, runtimeLink));
            _runtimeLinks.AddOrUpdate(output, runtimeLink);
        }
    }

    private readonly void CreateLinksWithComposition(out List<ComposeProcessor> composers)
    {
        using InternalIncomingLinkFactory internalIncomingLinkFactory = new();
        using RuntimeLinkFactory runtimeLinkFactory = new();
        using TypeAnalyzer typeAnalyzer = new(_logger);

        composers = [];

        foreach (var group in _linksWithComposition.GroupBy(l => l.OutputConnectorId))
        {
            var output = _outputs[group.Key];
            var type = DataTypeCompatibilityValidator.DetermineValueType(output.Connector.Design.ConnectorType);
            var linkDefinition = internalIncomingLinkFactory.Create((Connector)output.Connector);
            var runtimeLink = runtimeLinkFactory.Create(linkDefinition, new Dictionary<Guid, IRuntimeConnector>());
            var typeInfo = typeAnalyzer.AnalyzeType(type);

            ComposeProcessor composer = new(type, group.ToDictionary(l => l.InputConnectorId, l => typeInfo.First(e => e.Path == l.PropertyPath).PropertySetter ?? throw new InvalidOperationException("Property setter cannot be null.")), runtimeLink);
            foreach (var link in group)
                _inputProcessors.Add(link.InputConnectorId, composer);
            _runtimeLinks.AddOrUpdate(output, runtimeLink);
            composers.Add(composer);
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
