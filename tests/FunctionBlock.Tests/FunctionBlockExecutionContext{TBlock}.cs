using System;
using System.Linq;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime.Execution;

namespace ViciOne.Core.Runtime;

internal sealed class FunctionBlockExecutionContext<TBlock> : IDisposable where TBlock : RuntimeFunctionBlock<TBlock>, new()
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly FakeLogger<FunctionBlockExecutionContext<TBlock>> _logger;
    private readonly FunctionBlockExecutor _blockExecutor;

    internal FunctionBlockExecutionContext() : this(DataflowGenerator.GenerateDataflow) { }

    internal FunctionBlockExecutionContext(Func<RelevantAssemblies, DataflowInfo> generateDataflow, bool manualFirstCycle = false)
    {
        var assemblies = AssemblyLoader.CreateRelevantAssemblies(typeof(TBlock).Assembly.Location);
        var dataflowInfo = generateDataflow(assemblies);
        _logger = new();
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(default!).ReturnsForAnyArgs(_logger);
        _blockExecutor = new FunctionBlockExecutor(dataflowInfo, assemblies.AllAssemblies, _loggerFactory);
        _blockExecutor.StartEngine();
        _blockExecutor.EngineState.Should().Be(EngineState.Starting, "engine was started");

        if (!manualFirstCycle)
            ProcessCycle();
    }

    internal void ChangeFunctionBlockEnabled(bool enabled)
        => _blockExecutor.ChangeInputValue(GetInput(SystemConnectorNames.FunctionBlockEnabledName), enabled);

    internal void ChangeInputValue(string connectorName, object newValue)
        => _blockExecutor.ChangeInputValue(GetInput(connectorName), newValue);

    internal void ChangePersistentVariableValue(string variableName, object newValue)
        => _blockExecutor.ChangePersistentVariableValue(GetPersistentVariable(variableName), newValue);

    internal void ChangeSettingValue(string settingName, object newValue)
        => _blockExecutor.ChangeSettingValue(GetSetting(settingName), newValue);

    public void Dispose()
    {
        _blockExecutor?.Dispose();
        _loggerFactory?.Dispose();
    }

    internal IConnector GetInput(string connectorName)
        => _blockExecutor.GetInputs().First(x => string.Equals(x.Design.Name, connectorName, StringComparison.Ordinal));

    internal Validity GetInputValidity(string connectorName)
        => _blockExecutor.GetInputValues().First(x => string.Equals(x.Key.Design.Name, connectorName, StringComparison.Ordinal)).Value.Validity;

    internal TValue? GetInputValue<TValue>(string connectorName)
        => (TValue?)_blockExecutor.GetInputValues().First(x => string.Equals(x.Key.Design.Name, connectorName, StringComparison.Ordinal)).Value.Value;

    internal Validity GetOutputValidity(string connectorName)
        => _blockExecutor.GetOutputValues().First(x => string.Equals(x.Key.Design.Name, connectorName, StringComparison.Ordinal)).Value.Validity;

    internal TValue? GetOutputValue<TValue>(string connectorName)
        => (TValue?)_blockExecutor.GetOutputValues().First(x => string.Equals(x.Key.Design.Name, connectorName, StringComparison.Ordinal)).Value.Value;

    internal ChangedEntry GetOutput(string connectorName)
        => _blockExecutor.GetOutputValues().First(x => string.Equals(x.Key.Design.Name, connectorName, StringComparison.Ordinal)).Value;

    internal IPersistentVariable GetPersistentVariable(string variableName)
        => _blockExecutor.GetPersistentVariables().First(x => string.Equals(x.Design.Name, variableName, StringComparison.Ordinal));

    internal TValue? GetPersistentVariableValue<TValue>(string variableName)
        => (TValue?)_blockExecutor.GetPersistentVariableValues().First(x => string.Equals(x.Key.Design.Name, variableName, StringComparison.Ordinal)).Value.Value;

    internal ISetting GetSetting(string settingName)
        => _blockExecutor.GetSettings().First(x => string.Equals(x.Design.Name, settingName, StringComparison.Ordinal));

    internal TValue? GetSettingValue<TValue>(string settingName)
        => (TValue?)_blockExecutor.GetSettingValues().First(x => string.Equals(x.Key.Design.Name, settingName, StringComparison.Ordinal)).Value.Value;

    internal void ProcessCycle(bool allowErrors = false)
    {
        _blockExecutor.ProcessCycle();
        if (!allowErrors)
            AssertNoErrorsAreLogged();
    }

    internal void AssertNoErrorsAreLogged()
    {
        using (var scope = new AssertionScope())
        {
            var records = _logger.Collector.GetSnapshot();
            records.Should().NotContain(static x => x.Level >= LogLevel.Error);
            scope.AppendTracing(
                string.Join(Environment.NewLine,
                    records.Where(static x => x.Level >= LogLevel.Error).Select(x => x.Message)));
        }
    }
}
