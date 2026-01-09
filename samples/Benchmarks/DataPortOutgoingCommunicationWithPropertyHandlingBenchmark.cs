using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using ViciOne.ManagedEngine.ExternalCommunication;
using ViciOne.Suite.DataPort;

namespace Benchmarks;

public class DataPortOutgoingCommunicationWithPropertyHandlingBenchmark
{
    private readonly List<ExternalValue> _externalValues = [];
    private IExternalOutgoingCommunication<BenchmarkCommunication>? _provider;

    [Params(100_000)]
    public int Nodes { get; set; }
    [Params(10)]
    public int ProcessValuesPerNode { get; set; }
    [Params(10)]
    public int PropertyValuesPerNode { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        List<Node> nodes = [];

        for (var i = 0; i < Nodes; i++)
        {
            nodes.Add(new()
            {
                Id = Guid.NewGuid(),
                Properties = Enumerable.Range(0, PropertyValuesPerNode)
                    .Select(p => ($"property-{i}-{p}".ToString(), new Property { Value = 0, Channels = { $"property-{i}-{p}", } }))
                    .ToDictionary(e => e.Item1, e => e.Item2),
                AffectedChannels = [.. Enumerable.Range(0, ProcessValuesPerNode).Select(e => $"value-{i}-{e}")],
            });
        }

        foreach (var node in nodes)
        {
            foreach (var channel in node.AffectedChannels)
                _externalValues.Add(new() { Channel = channel, Value = 0, });

            foreach (var (_, property) in node.Properties)
                _externalValues.Add(new() { Channel = property.Channels[0], Value = 0, });
        }

        _provider = new BenchmarkOutgoingCommunication(new() { Nodes = nodes, });
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _externalValues.Clear();
        _provider = null;
    }

    [Benchmark]
    public Task SendAsync()
        => _provider!.SendAsync(0, _externalValues, CancellationToken.None);
}

public sealed record class BenchmarkCommunication : DataPortCommunication;

public class BenchmarkOutgoingCommunication(BenchmarkCommunication communication) : DataPortOutgoingCommunicationWithPropertyHandling<BenchmarkCommunication>(communication)
{
    public override Task ConnectAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
    public override Task DisconnectAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

    public override Task SendAsync(ulong engineCycle, IReadOnlyCollection<ExternalValue> processValues, IReadOnlyCollection<ExternalValue> propertyValues, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
