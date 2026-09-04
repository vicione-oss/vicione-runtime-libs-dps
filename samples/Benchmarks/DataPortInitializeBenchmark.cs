using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Engine;
using ViciOne.Suite.System;
using ViciOne.Suite.System.Contracts;

namespace Benchmarks;

[SuppressMessage("Maintainability", "CA1515:Erwägen Sie, öffentliche Typen intern zu machen.", Justification = "Sonst nicht von BenchmarkDotnet aufrufbar")]
public class DataPortInitializeBenchmark : IDisposable
{
    private DataPort? _runtimeFunctionBlockWithDirectLinks;
    private IFunctionBlock? _functionBlockWithDirectLinks;
    private readonly DefaultUniqueNameProvider _uniqueNameProvider = new(new Dictionary<Guid, string>());
    private readonly FakeLogger<DataPort> _logger = new FakeLogger<DataPort>().ConfigureLogLevel(LogLevel.Warning);
    private bool _disposed;

    [Params(10_000)]
    public int Links { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        DataPortFunctionBlockBuilder builder = new();

        for (var i = 0; i < Links; i++)
        {
            var input = $"ValueIn{i}";
            var output = $"ValueOut{i}";
            builder.AddInput<double>(input, PoolingStrategy.Queue);
            builder.AddOutput<double>(output, new OnChangedTransferMode(), PoolingStrategy.Queue);
            builder.AddNodeLink(input, output);
        }

        _functionBlockWithDirectLinks = builder.Build();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _runtimeFunctionBlockWithDirectLinks = new DataPort();
        _runtimeFunctionBlockWithDirectLinks.Setup(_functionBlockWithDirectLinks!, null, [], _uniqueNameProvider, _logger);
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        _runtimeFunctionBlockWithDirectLinks!.Dispose();
        _runtimeFunctionBlockWithDirectLinks = null;

        if (_logger.Collector.Count > 0)
            throw new InvalidOperationException("Unexpected warnings or errors.");
    }

    [Benchmark(Baseline = true)]
    [SuppressMessage("Naming", "CA1707:Bezeichner dürfen keine Unterstriche enthalten")]
    public void InitializeI_with_direct_links()
        => _runtimeFunctionBlockWithDirectLinks!.InitializeI();

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
            _runtimeFunctionBlockWithDirectLinks?.Dispose();

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
