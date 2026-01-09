using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using ViciOne.Suite.DataPort;

namespace Benchmarks;

[MemoryDiagnoser]
public class NodeExtensionGetSuccessorRoutesBenchmark
{
    private Node? _node;
    private readonly List<Node> _nodes = [];

    [Params(100)]
    public int Width { get; set; }
    [Params(100)]
    public int Depth { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _node = new() { Id = Guid.NewGuid(), };
        _nodes.Add(_node);

        for (var width = 0; width < Width; width++)
        {
            var parentId = _node.Id;

            for (var depth = 0; depth < Depth; depth++)
            {
                Node node = new()
                {
                    Id = Guid.NewGuid(),
                    ParentId = parentId,
                };

                _nodes.Add(node);
                parentId = node.Id;
            }
        }
    }

    [GlobalCleanup]
    public void Cleanup()
        => _nodes.Clear();

    [Benchmark(Baseline = true)]
    public IReadOnlyCollection<IReadOnlyCollection<INode>> GetSuccessorRoutes()
        => _node!.GetSuccessorRoutes(_nodes.CreateNodeParentsCache());

    [Benchmark]
    public IReadOnlyCollection<IReadOnlyCollection<INode>> GetSuccessorRoutesExperimental()
        => _node!.GetSuccessorRoutes(_nodes.CreateNodeParentsCache());
}
