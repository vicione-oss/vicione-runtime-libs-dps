using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using ViciOne.Core.Dataflow.DataModel.Generation;
using ViciOne.Core.Dataflow.Pooling;
using ViciOne.Core.Runtime.Execution;
using ViciOne.Suite.System.Contracts;

namespace ViciOne.Suite.System;

internal sealed class DataflowInfoBuilder
{
    private readonly PoolingFactory _poolingFactory;
    private readonly FunctionBlockDesign _design;
    private readonly List<(string Input, string Output)> _nodeLinks = [];
    private readonly Dictionary<string, (ITransferMode TransferMode, Pooling Pooling, bool MarkAsChangedIfNotEqual)> _outputConfigurations = [];

    internal DataflowInfoBuilder(RelevantAssemblies assemblies)
    {
        _poolingFactory = new(assemblies.AdditionalAssemblies);
        _design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesigns(RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesigns([assemblies.FbAssembly]), _poolingFactory).Single();
    }

    internal DataflowInfoBuilder SetRunMode(RunMode runMode)
    {
        _design.DefaultRunMode = runMode;
        return this;
    }

    internal DataflowInfoBuilder AddInput<T>(string name, PoolingStrategy poolingStrategy, IAggregatingPooling<T>? aggregatingPooling = null)
    {
        ConnectorDesignInputEditor<T> connectorDesignInputEditor = new(name, _poolingFactory);
        connectorDesignInputEditor.SetDefaultCrossSourcePoolingStrategy(poolingStrategy, aggregatingPooling);
        connectorDesignInputEditor.SetDefaultPerSourcePoolingStrategy(poolingStrategy, aggregatingPooling);
        _design.ProcessDataInputs.Add(connectorDesignInputEditor.Design);

        return this;
    }

    internal DataflowInfoBuilder AddInput<T>(string name, object? value, PoolingStrategy poolingStrategy, IAggregatingPooling<T>? aggregatingPooling = null)
    {
        ConnectorDesignInputEditor<T> connectorDesignInputEditor = new(name, _poolingFactory);
        connectorDesignInputEditor.SetDefaultCrossSourcePoolingStrategy(poolingStrategy, aggregatingPooling);
        connectorDesignInputEditor.SetDefaultPerSourcePoolingStrategy(poolingStrategy, aggregatingPooling);
        connectorDesignInputEditor.SetDefaultValue((T?)value);
        _design.ProcessDataInputs.Add(connectorDesignInputEditor.Design);

        return this;
    }

    internal DataflowInfoBuilder AddOutput<T>(string name, ITransferMode transferMode, PoolingStrategy poolingStrategy, IAggregatingPooling<T>? aggregatingPooling = null, bool markAsChangedIfNotEqual = false)
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

    internal DataflowInfoBuilder AddNodeLink(string input, string output)
    {
        _nodeLinks.Add((input, output));
        return this;
    }

    internal DataflowInfo CreateDataflowInfo()
    {
        var dataflowInfo = DataflowGenerator.GenerateDataflow([_design,]);

        List<NodeLink> nodeLinks = [];

        foreach (var (input, output) in _nodeLinks)
        {
            var inputId = dataflowInfo.Inputs.First(i => i.Connector.Design.Name == input).Connector.Id;
            var outputId = dataflowInfo.Outputs.First(o => o.Connector.Design.Name == output).Connector.Id;

            nodeLinks.Add(new() { InputConnectorId = inputId, OutputConnectorId = outputId, });
        }

        Dictionary<Guid, OutputConfiguration> outputConfigurations = [];
        Dictionary<Guid, ITransferMode> connectorTransferModes = [];

        foreach (var (name, configuration) in _outputConfigurations)
        {
            var outputId = dataflowInfo.Outputs.First(o => o.Connector.Design.Name == name).Connector.Id;
            outputConfigurations.Add(outputId, new() { Pooling = configuration.Pooling, MarkAsChangedIfNotEqual = configuration.MarkAsChangedIfNotEqual, });
            connectorTransferModes.Add(outputId, configuration.TransferMode);
        }

        dataflowInfo.Settings.First(s => s.Design.Name == nameof(DataPort.Settings.NodeLinks)).Value = nodeLinks;
        dataflowInfo.Settings.First(s => s.Design.Name == nameof(DataPort.Settings.OutputConfigurations)).Value = outputConfigurations;
        dataflowInfo.Settings.First(s => s.Design.Name == nameof(DataPort.Settings.TransferModes)).Value = connectorTransferModes;

        return dataflowInfo;
    }
}
