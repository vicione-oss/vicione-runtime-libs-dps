using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime.Execution;
using ViciOne.Engine;
using ViciOne.Suite.System;
using ViciOne.Suite.System.Contracts;

namespace Benchmarks;

[SuppressMessage("Maintainability", "CA1515:Erwägen Sie, öffentliche Typen intern zu machen.", Justification = "Sonst nicht von BenchmarkDotnet aufrufbar")]
public class DataPortProcessCycleBenchmark : IDisposable
{
    private readonly Random _random = new(100);
    private readonly DefaultUniqueNameProvider _uniqueNameProvider = new(new Dictionary<Guid, string>());
    private readonly FakeLogger<DataPort> _logger = new FakeLogger<DataPort>().ConfigureLogLevel(LogLevel.Warning);
    private DataPort? _runtimeFunctionBlockWithDirectLinks;
    private DataPort? _runtimeFunctionBlockWithCompositionLinks;
    private DataPort? _runtimeFunctionBlockWithExtractionLinks;
    private List<IRuntimeConnector<double>> _inputConnectorsWithDirectLink = [];
    private List<IRuntimeConnector<double>> _inputConnectorsWithCompositionLink = [];
    private List<IRuntimeConnector<Point>> _inputConnectorsWithExtractionLink = [];
    private bool _disposed;

    [Params(25_000)]
    public int Links { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        var assemblies = AssemblyLoader.CreateRelevantAssemblies(typeof(DataPort).Assembly.Location);
        SetupFunctionBlock(
            (b, i) =>
            {
                var input = $"ValueIn{i}";
                var output = $"ValueOut{i}";
                b.AddInput<double>(input, PoolingStrategy.Queue);
                b.AddOutput<double>(output, new OnChangedTransferMode(), PoolingStrategy.Queue);
                b.AddNodeLink(input, output);
            },
            out _runtimeFunctionBlockWithDirectLinks,
            out _inputConnectorsWithDirectLink);
        SetupFunctionBlock(
            (b, i) =>
            {
                var input = $"ValueIn{i}";
                var output = $"ValueOut{i}";
                b.AddInput<double>(input, PoolingStrategy.Queue);
                b.AddOutput<Point>(output, new OnChangedTransferMode(), PoolingStrategy.Queue);
                b.AddNodeLink(input, output, nameof(Point.Value), NodeLinkModification.Compose);
            },
            out _runtimeFunctionBlockWithCompositionLinks,
            out _inputConnectorsWithCompositionLink);
        SetupFunctionBlock(
            (b, i) =>
            {
                var input = $"ValueIn{i}";
                var output = $"ValueOut{i}";
                b.AddInput<Point>(input, PoolingStrategy.Queue);
                b.AddOutput<double>(output, new OnChangedTransferMode(), PoolingStrategy.Queue);
                b.AddNodeLink(input, output, nameof(Point.Value), NodeLinkModification.Extract);
            },
            out _runtimeFunctionBlockWithExtractionLinks,
            out _inputConnectorsWithExtractionLink);
    }

    private void SetupFunctionBlock<T>(Action<DataPortFunctionBlockBuilder, int> createLink, out DataPort runtimeFunctionBlock, out List<IRuntimeConnector<T>> inputs)
    {
        DataPortFunctionBlockBuilder builder = new();

        for (var i = 0; i < Links; i++)
            createLink(builder, i);

        var functionBlock = builder.Build();
        runtimeFunctionBlock = new DataPort();
        runtimeFunctionBlock.Setup(functionBlock, null, [], _uniqueNameProvider, _logger);
        runtimeFunctionBlock.InitializeI();
        runtimeFunctionBlock.InitializeII();
        inputs = [.. runtimeFunctionBlock.ProcessDataInputConnectors.Cast<IRuntimeConnector<T>>()];
    }

    [IterationSetup]
    [SuppressMessage("Security", "CA5394:Keine unsichere Zufälligkeitsstufe verwenden")]
    public void IterationSetup()
    {
        var value = _random.NextDouble();

        foreach (var input in _inputConnectorsWithDirectLink)
            input.SetValueIfNotEqual(value);

        foreach (var input in _inputConnectorsWithCompositionLink)
            input.SetValueIfNotEqual(value);

        foreach (var input in _inputConnectorsWithExtractionLink)
            input.SetValueIfNotEqual(new() { Value = value, });
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        if (_logger.Collector.Count > 0)
            throw new InvalidOperationException("Unexpected warnings or errors.");
    }

    [Benchmark(Baseline = true)]
    [SuppressMessage("Naming", "CA1707:Bezeichner dürfen keine Unterstriche enthalten")]
    public void ProcessCycle_with_direct_links()
        => _runtimeFunctionBlockWithDirectLinks!.Process();

    [Benchmark]
    [SuppressMessage("Naming", "CA1707:Bezeichner dürfen keine Unterstriche enthalten")]
    public void ProcessCycle_with_composition_links()
        => _runtimeFunctionBlockWithCompositionLinks!.Process();

    [Benchmark]
    [SuppressMessage("Naming", "CA1707:Bezeichner dürfen keine Unterstriche enthalten")]
    public void ProcessCycle_with_extraction_links()
        => _runtimeFunctionBlockWithExtractionLinks!.Process();

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _inputConnectorsWithDirectLink.Clear();
            _inputConnectorsWithCompositionLink.Clear();
            _inputConnectorsWithExtractionLink.Clear();
            _runtimeFunctionBlockWithDirectLinks?.Dispose();
            _runtimeFunctionBlockWithCompositionLinks?.Dispose();
            _runtimeFunctionBlockWithExtractionLinks?.Dispose();
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
