using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ViciOne.ManagedEngine.ExternalCommunication;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class RelationalDatabase_
{
    [Fact]
    public async Task Publishes_values_Async()
    {
        IEnumerable<RowData> data = [];
        using TestDatabase database = new();
        database.ValuesPublished += d => data = d;

        await database.ConnectAsync(CancellationToken.None);
        database.Send(
        [
            new()
            {
                Channel = "name",
                Value = "Test",
            },
            new()
            {
                Channel = "value",
                Value = 23,
            },
        ]);
        await database.DisconnectAsync(CancellationToken.None);

        data.Should().ContainSingle().Which.Columns.Should().HaveCount(2).And.SatisfyRespectively(
            c =>
            {
                c.Key.Name.Should().Be("name");
                c.Value.Should().Be("Test");
            },
            c =>
            {
                c.Key.Name.Should().Be("props");
                JsonNode.Parse(c.Value.Should().BeOfType<string>().Which)!["value"]!.GetValue<int>().Should().Be(23);
            });
    }

    internal sealed record TestCommunication : RelationalDatabaseCommunication
    { }

    internal sealed class TestDatabase : RelationalDatabase
    {
        private static readonly Node s_idNode = new()
        {
            Id = Guid.NewGuid(),
            DesignId = RelationalDatabaseNodeDesignId.ColumnNodeId,
            Name = "ID",
            ValueType = typeof(int),
        };
        private static readonly Node s_nameNode = new()
        {
            Id = Guid.NewGuid(),
            DesignId = RelationalDatabaseNodeDesignId.ColumnNodeId,
            Name = "name",
            ValueType = typeof(string),
            AffectedChannels = ["name"],
            TransferredChannels = ["name"],
        };
        private static readonly Node s_propsNode = new()
        {
            Id = Guid.NewGuid(),
            DesignId = RelationalDatabaseNodeDesignId.ColumnNodeId,
            Name = "props",
            TransferredChannels = ["value"],
        };
        private static readonly Node s_valueNode = new()
        {
            Id = Guid.NewGuid(),
            ParentId = s_propsNode.Id,
            DesignId = RelationalDatabaseNodeDesignId.JsonStructureNodeId,
            Name = "value",
            ValueType = typeof(int),
            AffectedChannels = ["value"],
        };

        public event Action<IEnumerable<RowData>>? ValuesPublished;

        public TestDatabase() : base(CreateCommunication(), Substitute.For<ILogger>())
        { }

        private static TestCommunication CreateCommunication()
            => new()
            {
                Nodes = [s_idNode, s_nameNode, s_propsNode, s_valueNode,],
            };

        protected override Task<IReadOnlyCollection<Database>> GetExistingStructureAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<Database>>(
            [
                new("default",
                [
                    new("default",
                    [
                        new("default",
                        [
                            new("ID", "int", true, false, true),
                            new("name", "string", false, false, false),
                            new("props", "json", false, false, false),
                        ]),
                    ]),
                ]),
            ]);

        protected override IReadOnlyCollection<Database> GetExpectedStructure(IReadOnlyCollection<INode> nodes)
            =>
            [
                new("default",
                [
                    new("default",
                    [
                        new("default",
                        [
                            new("ID", "int", true, false, true),
                            new("name", "string", false, false, false),
                            new("props", "json", false, false, false),
                        ]),
                    ]),
                ]),
            ];

        protected override Dictionary<Guid, ColumnInfo> CreateColumnInfos(IReadOnlyCollection<INode> nodes)
            => new()
            {
                { s_idNode.Id, new ColumnInfo(new ("default", "default", "default"), "ID", false, false, typeof(Guid)) },
                { s_nameNode.Id, new ColumnInfo(new ("default", "default", "default"), "name", false, false, typeof(string)) },
                { s_propsNode.Id, new ColumnInfo(new ("default", "default", "default"), "props", true, false, typeof(string)) },
            };

        protected override ValueTask<bool> PublishAsync(IEnumerable<RowData> data, CancellationToken cancellationToken)
        {
            ValuesPublished?.Invoke(data);
            return ValueTask.FromResult(true);
        }

        protected override Task OpenConnectionAsync(IReadOnlyCollection<ColumnInfo> columnInfos, CancellationToken cancellationToken) => Task.CompletedTask;
        protected override Task CloseConnectionAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        protected override Task CreateMissingStructureAsync(IReadOnlyCollection<Database> missingTables, IReadOnlyCollection<Database> missingDatabases, IReadOnlyCollection<Database> missingSchemas, CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}

public class ContainsInappropriateSign
{
    [Theory]
    [InlineData("MyColumn")]
    [InlineData("Main.MyColumn")]
    [InlineData("Main.MyColumn-Sub")]
    [InlineData("Hello+World#2303")]
    public void Returns_false_if_value_is_appropriate(string value)
        => RelationalDatabase.ContainsInappropriateSign(value).Should().BeFalse();

    [Theory]
    [InlineData("\"MyColumn\"")]
    [InlineData("'MyColumn'")]
    [InlineData("Hello\n")]
    [InlineData("Hello\r")]
    [InlineData("Hello\tWorld")]
    [InlineData("\\Hello")]
    [InlineData("$Hello")]
    [InlineData("%Hello")]
    [InlineData("Hello World")]
    [InlineData("Hello\b")]
    [InlineData("Hello\0")]
    [InlineData("Hello,")]
    [InlineData("Hello;")]
    public void Returns_true_if_value_is_inappropriate(string value)
        => RelationalDatabase.ContainsInappropriateSign(value).Should().BeTrue();
}

public class ValidateNodeNames
{
    [Fact]
    public void Throws_with_names_and_ids()
    {
        var nodes = new Node[]
        {
            new()
            {
                Id = Guid.Parse("bc6ba8c1-dfe7-492c-aacd-9497d10f5a7a"),
                Name = "Test'",
            },
            new()
            {
                Id = Guid.Parse("5ac4ed19-8ba1-43f3-b042-79b6457b98d2"),
                Name = "Hello\n",
            },
        };
        var act = () => RelationalDatabase.ValidateNodeNames(nodes);

        act.Should().Throw<InvalidOperationException>().WithMessage($"*names*inappropriate*Test'*bc6ba8c1-dfe7-492c-aacd-9497d10f5a7a*Hello\n*5ac4ed19-8ba1-43f3-b042-79b6457b98d2*");
    }
}

public class ComposeMessage
{
    [Fact]
    public void Acceptance()
    {
        Table users = new("users", [new("ID", string.Empty, true, false, false), new("name", string.Empty, false, false, false), new("age", string.Empty, false, false, false),]);
        Schema schema = new("public", [users,]);
        var databases = new Database[]
        {
            new("mydb", [schema,]),
        };

        var message = RelationalDatabase.ComposeMessage(databases);

        message.Should().Match("*changes*expect*mydb.public.users.ID*mydb.public.users.name*mydb.public.users.age*");
    }
}

public class PrepareRowData
{
    [Fact]
    public void Can_handle_json_structure()
    {
        Node columnNode = new()
        {
            Id = Guid.NewGuid(),
            Name = "JSON column node"
        };
        Node apartmentNode = new()
        {
            Id = Guid.NewGuid(),
            ParentId = columnNode.Id,
            Name = "apartment node"
        };
        Node kitchenNode = new()
        {
            Id = Guid.NewGuid(),
            ParentId = apartmentNode.Id,
            Name = "kitchen node"
        };
        Node livingRoomNode = new()
        {
            Id = Guid.NewGuid(),
            ParentId = apartmentNode.Id,
            Name = "living room node"
        };
        TableInfo table = new("default", "default", "default");
        ColumnInfo column = new(table, "JSON column node", true, false, null);
        Dictionary<string, ColumnInfo> channelColumns = new()
        {
            { kitchenNode.Id.ToString(), column },
            { livingRoomNode.Id.ToString(), column },
        };
        Dictionary<string, INode[]> routes = new()
        {
            { kitchenNode.Id.ToString(), [apartmentNode, kitchenNode] },
            { livingRoomNode.Id.ToString(), [apartmentNode, livingRoomNode] },
        };
        List<ExternalValue> values =
        [
            new()
            {
                Channel = kitchenNode.Id.ToString(),
                Value = 1,
            },
            new()
            {
                Channel = livingRoomNode.Id.ToString(),
                Value = 2,
            },
        ];

        var data = RelationalDatabase.PrepareRowData(values, channelColumns, routes).ToArray();

        data.Should().ContainSingle().Which.Columns.Should().ContainSingle().Which.Value.Should().Be("""
        {
          "apartment node": {
            "kitchen node": 1,
            "living room node": 2
          },
          "$meta": {
            "apartment node": {
              "kitchen node": "System.Int32, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
              "living room node": "System.Int32, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e"
            }
          }
        }
        """);
    }

    [Theory]
    [MemberData(nameof(GetSpecialSingleValues))]
    [SuppressMessage("Usage", "xUnit1045:Avoid using TheoryData type arguments that might not be serializable",
        Justification = "Die zu testenden Objekte sind zu unterschiedlich um sie auf einen serialisierbaren Typ zu reduzieren")]
    public void Can_handle_single_value(object? value, object? expected, bool isUnknown)
    {
        Node columnNode = new()
        {
            Id = Guid.NewGuid(),
            Name = "JSON column node"
        };
        TableInfo table = new("default", "default", "default");
        ColumnInfo column = new(table, "JSON column node", false, isUnknown, null);
        Dictionary<string, ColumnInfo> channelColumns = new()
        {
            { columnNode.Id.ToString(), column },
        };
        List<ExternalValue> values =
        [
            new()
            {
                Channel = columnNode.Id.ToString(),
                Value = value,
            },
        ];

        var data = RelationalDatabase.PrepareRowData(values, channelColumns, []).ToArray();

        data.Should().ContainSingle().Which.Columns.Should().ContainSingle().Which.Value.Should().Be(expected);
    }

    private sealed class Test
    {
        public int TestValue { get; set; }
    }

    public static TheoryData<object?, object?, bool> GetSpecialSingleValues()
        => new()
        {
            { null, DBNull.Value, false },
            { 23, 23, false },
            { new Test { TestValue = 2, }, "{\"TestValue\":2}", true },
        };
}
