using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using ViciOne.ManagedEngine.ExternalCommunication;
using ViciOne.Suite.DataPort;

namespace Benchmarks;

[ThroughputColumn(nameof(Batches), nameof(CountPerBatch))]
public class HttpClientDataPortOutgoingBenchmark : IDisposable
{
    private HttpClientDataPortOutgoing? _communication;
    private bool _disposedValue;
    private readonly ILoggerFactory _loggerFactory;
    private readonly List<List<ExternalValue>> _data = [];

    public HttpClientDataPortOutgoingBenchmark()
        => _loggerFactory = LoggerFactory
            .Create(logging => logging.AddConsole());

    [GlobalSetup]
    public void Setup()
    {
        List<Node> nodes = [];

        for (var i = 0; i < CountPerBatch; i++)
        {
            var nodeId = Guid.NewGuid();
            Node node = new()
            {
                Id = nodeId,
                DesignId = HttpClientNodeDesignId.Path,
                Name = $"counter/receive/{i}",
                TransferredChannels = { nodeId.ToString(), },
                AffectedChannels = { nodeId.ToString(), },
                ValueType = typeof(string),
                Properties =
                {
                    { "HttpMethod", new() { Value = 1, } },
                }
            };
            nodes.Add(node);
        }

        for (var i = 0; i < Batches; i++)
        {
            _data.Add([.. nodes.Select(n => new ExternalValue
            {
                Channel = n.Id.ToString(),
                Value = Guid.NewGuid().ToString(),
            })]);
        }

        HttpClientDataPortCommunication communication = new()
        {
            Nodes = nodes,
        };
        _ = new HttpClientDataPortProperties(communication)
        {
            BaseAddress = "http://localhost:5000/",
        };
        _communication = new(communication, _loggerFactory.CreateLogger<HttpClientDataPortOutgoing>());
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _communication?.Dispose();
        _data.Clear();
    }

    [Params(10)]
    public int CountPerBatch { get; set; }
    [Params(1000)]
    public int Batches { get; set; }

    [Benchmark]
    [SuppressMessage("Naming", "CA1707:Bezeichner dürfen keine Unterstriche enthalten")]
    public async Task Http_External_Outgoing_Async()
    {
        await _communication!.ConnectAsync(CancellationToken.None);
        foreach (var partition in _data)
            await _communication.SendAsync(0, partition, CancellationToken.None);
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
                _loggerFactory.Dispose();
            }
            _disposedValue = true;
        }
    }
}
