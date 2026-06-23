using System;
using System.Collections.Generic;
using System.Reflection;
using ViciOne.Core.Contracts;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel;
using System.Linq;
using ViciOne.Suite.System.Contracts;
using ViciOne.Suite.System.Runtime;

namespace ViciOne.Suite.System;

#nullable enable
#pragma warning disable CA1034 // Geschachtelte Typen dürfen nicht sichtbar sein
[FunctionBlockDesign("c7390e0f-761d-40f6-9112-a31216eac2c7", "ebfa0e55-e61b-482b-b74f-f6a18cb162ec", "7896227b-8946-4efd-8bf4-bd564fe28c7f")]
[ValidRunModes(RunMode.Cyclic, RunMode.Change)]
[DefaultRunMode(RunMode.Change)]
public partial class DataPort : RuntimeFunctionBlock<DataPort>
{
    public new SettingsDataPort Settings { get; } = new SettingsDataPort();

    [SettingDesigns]
    public class SettingsDataPort
    {
        [SettingDesign("a70d582d-7a17-4361-843f-299af86dcb97", "03132d07-ac96-4fbe-a008-d38f4eafc930", "ca7dd196-0051-4661-b836-a363b0f22ef4")]
        public IRuntimeSetting<List<NodeLink>>? NodeLinks { get; set; }

        [SettingDesign("a6b043fb-f21e-48b1-be5a-595adc18dc08", "314eafa5-a192-47e9-8354-07d3b7165131", "e5cb9bdf-e700-4d6c-a789-52770db98ecd")]
        public IRuntimeSetting<Dictionary<Guid, OutputConfiguration>>? OutputConfigurations { get; set; }

        [SettingDesign("f73cc185-43ab-4b77-817a-eda924625b5b", "f64400a8-2857-4d30-856c-570fc25d1ff2", "f2022716-88ba-4780-bbe8-9ce46451926c")]
        public IRuntimeSetting<Dictionary<Guid, ITransferMode>>? TransferModes { get; set; }
    }

    protected override void InitializeConnectors()
    {
        if (Settings.OutputConfigurations?.Value is null)
            throw new InvalidOperationException("Output poolings must be set.");

        var createInputInstanceMethodDefinition = typeof(DataPort).GetMethod(nameof(CreateInputConnector), BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException();
        var createPoolableOutputConnectorDefinition = typeof(DataPort).GetMethod(nameof(CreatePoolableOutputConnector), BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException();

        InitializeRuntimeConnectors(FunctionBlock.ProcessDataInputs, createInputInstanceMethodDefinition, c => new[] { c, }, ProcessDataInputs);
        InitializeRuntimeConnectors(FunctionBlock.ProcessDataOutputs,
            createPoolableOutputConnectorDefinition,
            c =>
            {
                var configuration = Settings.OutputConfigurations.Value[c.Id];
                return new object[] { c, configuration.Pooling, configuration.MarkAsChangedIfNotEqual, };
            },
            ProcessDataOutputs);
    }

    private static void InitializeRuntimeConnectors(IEnumerable<IConnector> connectors, MethodInfo createMethodDefinition, Func<IConnector,object[]> getCtorParameters, IList<IRuntimeConnector> runtimeConnectors)
    {
        Dictionary<Type, MethodInfo> createMethodCache = new();

        foreach (var connector in connectors)
        {
            var connectorType = connector.GetType();

            if (createMethodCache.TryGetValue(connectorType, out var createMethod) is false)
            {
                var valueType = DataTypeCompatibilityValidator.DetermineValueType(connectorType);
                createMethod = createMethodDefinition.MakeGenericMethod(valueType);
                createMethodCache.Add(connectorType, createMethod);
            }

            var instance = createMethod.Invoke(null, getCtorParameters(connector)) as IRuntimeConnector
                ?? throw new InvalidOperationException("Cannot create runtime connector instance.");

            runtimeConnectors.Add(instance);
        }

        createMethodCache.Clear();
    }

    private static RuntimeConnector<TValue> CreateInputConnector<TValue>(IConnector<TValue> connector)
        => new RuntimeConnector<TValue>(connector, true);

    private static RuntimeConnector<TValue> CreatePoolableOutputConnector<TValue>(IConnector connector, Pooling pooling, bool markAsChangedOnlyIfNotEqual)
    {
        var typedSourceConnector = connector as ConnectorOutput<TValue> ?? throw new InvalidOperationException();
        typedSourceConnector.MarkAsChangedOnlyIfNotEqual = markAsChangedOnlyIfNotEqual;
        PoolableConnectorOutput<TValue> poolableConnector = new()
        {
            Id = connector.Id,
            Design = (ConnectorDesignOutput<TValue>)typedSourceConnector.Design,
            MarkAsChangedOnlyIfNotEqual = connector.MarkAsChangedOnlyIfNotEqual,
            Validators = typedSourceConnector.Validators,
            Value = typedSourceConnector.Value,
            CrossSourcePoolingStrategy = pooling.CrossSourcePoolingStrategy,
            CrossSourceAggregatingPooling = pooling.CrossSourceAggregatingPooling,
            PerSourcePoolingStrategy = pooling.PerSourcePoolingStrategy,
            PerSourceAggregatingPooling = pooling.PerSourceAggregatingPooling,
        };
        return new RuntimeConnector<TValue>(poolableConnector, true);
    }

    protected override void ValidateConnectors()
    {
        if (FunctionBlock.ProcessDataInputs.Select(c => c.Id).SequenceEqual(ProcessDataInputs.Select(x => x.Connector.Id)) == false)
            throw new InvalidOperationException("There are differences between inputs of design and implementation.");

        if (FunctionBlock.ProcessDataOutputs.Select(c => c.Id).SequenceEqual(ProcessDataOutputs.Select(x => x.Connector.Id)) == false)
            throw new InvalidOperationException("There are differences between outputs of design and implementation.");
    }
}
#pragma warning restore CA1034 // Geschachtelte Typen dürfen nicht sichtbar sein
#nullable restore
