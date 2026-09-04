using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AwesomeAssertions;
using Benchmarks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine;
using ViciOne.Suite.System.Contracts;
using Xunit;

namespace ViciOne.Suite.System.Runtime;

public sealed class ConnectionFactory_Constructor
{
    [Fact]
    public void Throws_when_node_links_are_not_set()
    {
        using var dataPort = new DataPort();

        var act = FluentActions.Invoking(() => _ = new ConnectionFactory(dataPort));

        act.Should().Throw<InvalidOperationException>().WithMessage("Node links setting must be set.");
    }

    [Fact]
    public void Throws_when_transfer_modes_are_not_set()
    {
        using var dataPort = new DataPort();
        var nodeLinksSetting = Substitute.For<IRuntimeSetting<List<NodeLink>>>();
        nodeLinksSetting.Value.Returns([]);
        dataPort.Settings.NodeLinks = nodeLinksSetting;

        var act = FluentActions.Invoking(() => _ = new ConnectionFactory(dataPort));

        act.Should().Throw<InvalidOperationException>().WithMessage("Transfer modes must be set.");
    }
}

public sealed class ConnectionFactory_CreateInputProcessors
{
    private readonly ILogger _logger = Substitute.For<ILogger>();
    private readonly IConnectorValueTransferInterceptor _interceptor = Substitute.For<IConnectorValueTransferInterceptor>();
    private readonly DefaultUniqueNameProvider _uniqueNameProvider = new(new Dictionary<Guid, string>());

    [Fact]
    public void Creates_passtrough_processor_for_direct_link()
    {
        using var dataPort = CreateInitializedDataPort(b => b
            .AddInput<int>("ValueIn1", PoolingStrategy.Queue)
            .AddOutput<int>("ValueOut1", new OnChangedTransferMode(), PoolingStrategy.Queue)
            .AddNodeLink("ValueIn1", "ValueOut1"));

        ConnectionFactory factory = new(dataPort);
        var processors = factory.CreateInputProcessors(_interceptor);

        var inputId = ((IRuntimeFunctionBlock)dataPort).ProcessDataInputs.Single().Connector.Id;
        processors.Should().ContainKey(inputId)
            .WhoseValue.Should().BeOfType<PasstroughProcessor>();
    }

    [Fact]
    public void Returns_empty_when_there_are_no_links()
    {
        using var dataPort = CreateInitializedDataPort(b => b
            .AddInput<int>("ValueIn1", PoolingStrategy.Queue));

        ConnectionFactory factory = new(dataPort);
        var processors = factory.CreateInputProcessors(_interceptor);

        processors.Should().BeEmpty();
    }

    [Fact]
    public void Creates_single_passtrough_processor_for_input_linked_to_multiple_outputs()
    {
        using var dataPort = CreateInitializedDataPort(b => b
            .AddInput<int>("ValueIn1", PoolingStrategy.Queue)
            .AddOutput<int>("ValueOut1", new OnChangedTransferMode(), PoolingStrategy.Queue)
            .AddOutput<int>("ValueOut2", new OnChangedTransferMode(), PoolingStrategy.Queue)
            .AddNodeLink("ValueIn1", "ValueOut1")
            .AddNodeLink("ValueIn1", "ValueOut2"));
        var inputId = ((IRuntimeFunctionBlock)dataPort).ProcessDataInputs.Single().Connector.Id;

        ConnectionFactory factory = new(dataPort);
        var processors = factory.CreateInputProcessors(_interceptor);

        processors.Should().ContainSingle()
            .Which.Value.Should().BeOfType<PasstroughProcessor>();
    }

    private DataPort CreateDataPort(Func<DataPortFunctionBlockBuilder, DataPortFunctionBlockBuilder> configure)
    {
        var functionBlock = configure(new DataPortFunctionBlockBuilder()).Build();
        DataPort dataPort = new();
        dataPort.Setup(functionBlock, null, [], _uniqueNameProvider, _logger);
        return dataPort;
    }

    private DataPort CreateInitializedDataPort(Func<DataPortFunctionBlockBuilder, DataPortFunctionBlockBuilder> configure)
    {
        var dataPort = CreateDataPort(configure);
        dataPort.InitializeI();
        return dataPort;
    }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated via reflection")]
    private sealed class Test
    {
        public int MyProperty { get; set; }
        public int MyOtherProperty { get; set; }
        internal int InternalProperty { get; set; }
        public int PropertyWithoutGetter { set => MyProperty = value; }
        public int PropertyWithoutSetter { get; }
    }
}
