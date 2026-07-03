using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using ViciOne.Core.Dataflow.DataModel.Generation;
using ViciOne.Core.Dataflow.Pooling;
using ViciOne.Engine.DefaultPoolings;
using ViciOne.Engine.DefaultValidators;
using ViciOne.Suite.System;
using ViciOne.Suite.System.Contracts;

namespace Benchmarks;

internal sealed class DataPortFunctionBlockBuilder
{
    private readonly PoolingFactory _poolingFactory;
    private readonly FunctionBlockDesign _design;
    private readonly List<(string Input, string Output)> _nodeLinks = [];
    private readonly Dictionary<string, (ITransferMode TransferMode, Pooling Pooling, bool MarkAsChangedIfNotEqual)> _outputConfigurations = [];

    public DataPortFunctionBlockBuilder()
    {
        _poolingFactory = new(
        [
            typeof(AndPooling).Assembly,
            typeof(RangeValidator<>).Assembly,
        ]);
        _design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesigns(RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesigns([typeof(DataPort).Assembly]), _poolingFactory).Single();
    }

    public DataPortFunctionBlockBuilder AddInput<T>(string name, PoolingStrategy poolingStrategy, IAggregatingPooling<T>? aggregatingPooling = null)
    {
        ConnectorDesignInputEditor<T> connectorDesignInputEditor = new(name, _poolingFactory);
        connectorDesignInputEditor.SetDefaultCrossSourcePoolingStrategy(poolingStrategy, aggregatingPooling);
        connectorDesignInputEditor.SetDefaultPerSourcePoolingStrategy(poolingStrategy, aggregatingPooling);
        _design.ProcessDataInputs.Add(connectorDesignInputEditor.Design);

        return this;
    }

    public DataPortFunctionBlockBuilder AddOutput<T>(string name, ITransferMode transferMode, PoolingStrategy poolingStrategy, IAggregatingPooling<T>? aggregatingPooling = null, bool markAsChangedIfNotEqual = false)
    {
        ConnectorDesignOutputEditor<T> connectorDesignOutputEditor = new(name);
        _design.ProcessDataOutputs.Add(connectorDesignOutputEditor.Design);
        Pooling pooling = new()
        {
            CrossSourcePoolingStrategy = poolingStrategy,
            CrossSourceAggregatingPooling = aggregatingPooling,
            PerSourcePoolingStrategy = poolingStrategy,
            PerSourceAggregatingPooling = aggregatingPooling,
        };
        _outputConfigurations.Add(name, (transferMode, pooling, markAsChangedIfNotEqual));

        return this;
    }

    public DataPortFunctionBlockBuilder AddNodeLink(string input, string output)
    {
        _nodeLinks.Add((input, output));
        return this;
    }

    public FunctionBlock Build()
    {
        var editor = new FunctionBlockEditor(_design);
        var inputs = editor.FunctionBlock.ProcessDataInputs.ToDictionary(i => i.Design.Name, i => i.Id);
        var outputs = editor.FunctionBlock.ProcessDataOutputs.ToDictionary(o => o.Design.Name, o => o.Id);

        List<NodeLink> nodeLinks = [];

        foreach (var (input, output) in _nodeLinks)
            nodeLinks.Add(new() { InputConnectorId = inputs[input], OutputConnectorId = outputs[output], });

        Dictionary<Guid, OutputConfiguration> outputConfigurations = [];
        Dictionary<Guid, ITransferMode> connectorTransferModes = [];

        foreach (var (name, configuration) in _outputConfigurations)
        {
            var outputId = outputs[name];
            outputConfigurations.Add(outputId, new() { Pooling = configuration.Pooling, MarkAsChangedIfNotEqual = configuration.MarkAsChangedIfNotEqual, });
            connectorTransferModes.Add(outputId, configuration.TransferMode);
        }

        editor.FunctionBlock.Settings.First(s => s.Design.Name == nameof(DataPort.Settings.NodeLinks)).Value = nodeLinks;
        editor.FunctionBlock.Settings.First(s => s.Design.Name == nameof(DataPort.Settings.OutputConfigurations)).Value = outputConfigurations;
        editor.FunctionBlock.Settings.First(s => s.Design.Name == nameof(DataPort.Settings.TransferModes)).Value = connectorTransferModes;

        return editor.FunctionBlock;
    }
}
