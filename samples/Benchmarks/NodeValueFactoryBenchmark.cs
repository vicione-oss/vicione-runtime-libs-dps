using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using BenchmarkDotNet.Attributes;
using ViciOne.Suite.DataPort;

namespace Benchmarks;

[MemoryDiagnoser]
public class NodeValueFactoryBenchmark
{
    private JsonNode? _jObject;
    private Node[] _nodes = [];

    [Params(1, 500)]
    public int Values { get; set; }

    [Params(0, 60)]
    public int Levels { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Node rootNode = new() { Id = Guid.NewGuid(), Name = "root", };
        List<Node> nodes = [rootNode,];
        var parentLevelId = rootNode.Id;
        JsonObject jsonObject = [];
        var currentJsonObject = jsonObject;

        for (var i = 0; i < Levels; i++)
        {
            var levelName = $"level{i}";
            Node levelNode = new() { Id = Guid.NewGuid(), Name = levelName, ParentId = parentLevelId, };

            JsonObject levelJsonObject = [];
            currentJsonObject[levelName] = levelJsonObject;
            currentJsonObject = levelJsonObject;
            parentLevelId = levelNode.Id;

            nodes.Add(levelNode);
        }

        for (var i = 0; i < Values; i++)
        {
            var valueName = $"value{i}";
            Node valueNode = new() { Id = Guid.NewGuid(), Name = valueName, ParentId = parentLevelId, };
            currentJsonObject[valueName] = JsonValue.Create(23);
            nodes.Add(valueNode);
        }

        _jObject = jsonObject;
        _nodes = [.. nodes];
    }

    [Benchmark(Baseline = true)]
    public JsonNode? GetValueJsonNode()
        => NodeValueFactory.GetValueJsonNode(_nodes, _jObject);
}
