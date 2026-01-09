using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using ViciOne.ManagedEngine.ExternalCommunication;
using ViciOne.Suite.DataPort;

namespace Benchmarks;

[ParamsExpansionColumn<TestData>(nameof(TestData))]
[ThroughputExpansionColumn(nameof(TestData), nameof(TestData.Buckets), nameof(TestData.Measurements), nameof(TestData.FieldsAndTags), nameof(TestData.RecordsPerField))]
public class InfluxDbDataPortOutgoingBenchmark : IDisposable
{
    private InfluxDbDataPortOutgoing? _communication;
    private readonly ILoggerFactory _loggerFactory;
    private List<List<ExternalValue>>? _data;
    private bool _disposedValue;

    public InfluxDbDataPortOutgoingBenchmark()
    {
        _loggerFactory = LoggerFactory
            .Create(logging => logging.AddConsole());
        TestData = null!;
    }

    public static IEnumerable<TestData> Data =>
    [
        new TestData
        {
            Buckets = 1,
            Measurements = 10,
            Tags = 5,
            Fields = 5,
            RecordsPerField = 4000,
            BulkSize = 5000,
        }
    ];

    [ParamsSource(nameof(Data))]
    public TestData TestData { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var nodes = InfluxDbTreeBuilder.CreateTree(TestData);
        _data = InfluxDbTreeBuilder.CreateTestData(nodes, TestData.RecordsPerField);
        var communication = new InfluxDbDataPortCommunication()
        {
            Nodes = nodes
        };
        _ = new InfluxDbDataPortProperties(communication)
        {
            Organization = "",
            Url = "",
            Token = "",
            BulkSize = TestData.BulkSize,
            MinimumPublishInterval = TimeSpan.FromSeconds(10),
        };
        _communication = new(
            communication, _loggerFactory.CreateLogger<InfluxDbDataPortOutgoing>());
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _communication?.Dispose();
        _data?.Clear();
    }

    [Benchmark]
    [SuppressMessage("Naming", "CA1707:Bezeichner dürfen keine Unterstriche enthalten")]
    public async Task InfluxDb_External_Outgoing_Async()
    {
        await _communication!.ConnectAsync(CancellationToken.None);
        foreach (var data in _data!)
            await _communication.SendAsync(0, data, CancellationToken.None);
        await _communication.DisconnectAsync(CancellationToken.None);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _communication?.Dispose();
                _data?.Clear();
                _loggerFactory.Dispose();
            }
            _disposedValue = true;
        }
    }
}
