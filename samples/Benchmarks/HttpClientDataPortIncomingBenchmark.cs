using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using ViciOne.ManagedEngine.ExternalCommunication;
using ViciOne.Suite.DataPort;

namespace Benchmarks;

public class HttpClientDataPortIncomingBenchmark : IDisposable
{
    private HttpClientDataPortIncoming? _communication;
    private bool _disposedValue;
    private readonly ILoggerFactory _loggerFactory;
    private readonly List<ExternalValue> _data = [];

    public HttpClientDataPortIncomingBenchmark()
        => _loggerFactory = LoggerFactory
            .Create(logging => logging.AddConsole());

    [GlobalSetup]
    public void Setup()
    {
        List<Node> nodes = [];

        for (var i = 0; i < Nodes; i++)
        {
            var nodeId = Guid.NewGuid();
            Node node = new()
            {
                Id = nodeId,
                DesignId = HttpClientNodeDesignId.Path,
                Name = $"counter/poll",
                TransferredChannels = { nodeId.ToString(), },
                AffectedChannels = { nodeId.ToString(), },
                ValueType = typeof(int),
                Properties =
                {
                    { "HttpMethod", new() { Value = 0, } },
                }
            };
            nodes.Add(node);
        }

        HttpClientDataPortCommunication communication = new()
        {
            Nodes = nodes,
        };
        _ = new HttpClientDataPortProperties(communication)
        {
            BaseAddress = "http://localhost:5000/",
            PollInterval = TimeSpan.FromSeconds(1),
        };
        _communication = new(communication, _loggerFactory.CreateLogger<HttpClientDataPortIncoming>(), AssemblyLoadContext.Default);
        _communication.Received += _data.AddRange;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _communication?.Dispose();
        _data.Clear();
    }

    [Params(10)]
    public int Nodes { get; set; }

    [Benchmark]
    [SuppressMessage("Naming", "CA1707:Bezeichner dürfen keine Unterstriche enthalten")]
    public async Task Http_External_Incoming_Async()
    {
        await _communication!.ConnectAsync(CancellationToken.None);
        await Task.Delay(10_000);
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
