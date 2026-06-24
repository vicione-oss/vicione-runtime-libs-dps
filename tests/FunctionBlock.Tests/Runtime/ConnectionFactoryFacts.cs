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
    private readonly ILogger _logger = Substitute.For<ILogger>();

    [Fact]
    public void Throws_when_node_links_are_not_set()
    {
        using var dataPort = new DataPort();

        var act = FluentActions.Invoking(() => _ = new ConnectionFactory(dataPort, _logger));

        act.Should().Throw<InvalidOperationException>().WithMessage("Node links setting must be set.");
    }

    [Fact]
    public void Throws_when_transfer_modes_are_not_set()
    {
        using var dataPort = new DataPort();
        var nodeLinksSetting = Substitute.For<IRuntimeSetting<List<NodeLink>>>();
        nodeLinksSetting.Value.Returns([]);
        dataPort.Settings.NodeLinks = nodeLinksSetting;

        var act = FluentActions.Invoking(() => _ = new ConnectionFactory(dataPort, _logger));

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

        ConnectionFactory factory = new(dataPort, _logger);
        var processors = factory.CreateInputProcessors(_interceptor, out var composers);

        var inputId = ((IRuntimeFunctionBlock)dataPort).ProcessDataInputs.Single().Connector.Id;
        processors.Should().ContainKey(inputId)
            .WhoseValue.Should().BeOfType<PasstroughProcessor>();
        composers.Should().BeEmpty();
    }

    [Fact]
    public void Creates_extract_processor_for_extraction_link()
    {
        using var dataPort = CreateInitializedDataPort(b => b
            .AddInput<Test>("ValueIn1", PoolingStrategy.Queue)
            .AddOutput<int>("ValueOut1", new OnChangedTransferMode(), PoolingStrategy.Queue)
            .AddNodeLink("ValueIn1", "ValueOut1", nameof(Test.MyProperty), NodeLinkModification.Extract));
        var inputId = ((IRuntimeFunctionBlock)dataPort).ProcessDataInputs.Single().Connector.Id;

        ConnectionFactory factory = new(dataPort, _logger);
        var processors = factory.CreateInputProcessors(_interceptor, out var composers);

        processors.Should().ContainKey(inputId)
            .WhoseValue.Should().BeOfType<ExtractProcessor>();
        composers.Should().BeEmpty();
    }

    [Fact]
    public void Throws_for_non_public_property_for_extraction_link()
    {
        using var dataPort = CreateDataPort(b => b
            .AddInput<Test>("ValueIn1", PoolingStrategy.Queue)
            .AddOutput<int>("ValueOut1", new OnChangedTransferMode(), PoolingStrategy.Queue)
            .AddNodeLink("ValueIn1", "ValueOut1", nameof(Test.InternalProperty), NodeLinkModification.Extract));

        var act = dataPort.Invoking(x => x.InitializeI());

        act.Should().Throw<InvalidOperationException>().WithMessage($"*type*{nameof(Test)}*not*contain*public*getter*path*{nameof(Test.InternalProperty)}*");
    }

    [Fact]
    public void Throws_for_property_without_getter_for_extraction_link()
    {
        using var dataPort = CreateDataPort(b => b
            .AddInput<Test>("ValueIn1", PoolingStrategy.Queue)
            .AddOutput<int>("ValueOut1", new OnChangedTransferMode(), PoolingStrategy.Queue)
            .AddNodeLink("ValueIn1", "ValueOut1", nameof(Test.PropertyWithoutGetter), NodeLinkModification.Extract));

        var act = dataPort.Invoking(x => x.InitializeI());

        act.Should().Throw<InvalidOperationException>().WithMessage($"*type*{nameof(Test)}*not*contain*public*getter*path*{nameof(Test.PropertyWithoutGetter)}*");
    }

    [Fact]
    public void Creates_compose_processor_for_composition_link()
    {
        using var dataPort = CreateInitializedDataPort(b => b
            .AddInput<int>("ValueIn1", PoolingStrategy.Queue)
            .AddOutput<Test>("ValueOut1", new OnChangedTransferMode(), PoolingStrategy.Queue)
            .AddNodeLink("ValueIn1", "ValueOut1", nameof(Test.MyProperty), NodeLinkModification.Compose));
        var inputId = ((IRuntimeFunctionBlock)dataPort).ProcessDataInputs.Single().Connector.Id;

        ConnectionFactory factory = new(dataPort, _logger);
        var processors = factory.CreateInputProcessors(_interceptor, out var composers);

        processors.Should().ContainKey(inputId)
            .WhoseValue.Should().BeOfType<ComposeProcessor>();
        composers.Should().ContainSingle().Which.Should().BeSameAs(processors[inputId]);
    }

    [Fact]
    public void Throws_for_non_public_property_for_compose_link()
    {
        using var dataPort = CreateDataPort(b => b
            .AddInput<int>("ValueIn1", PoolingStrategy.Queue)
            .AddOutput<Test>("ValueOut1", new OnChangedTransferMode(), PoolingStrategy.Queue)
            .AddNodeLink("ValueIn1", "ValueOut1", nameof(Test.InternalProperty), NodeLinkModification.Compose));

        var act = dataPort.Invoking(x => x.InitializeI());

        act.Should().Throw<InvalidOperationException>().WithMessage($"*type*{nameof(Test)}*not*contain*public*setter*path*{nameof(Test.InternalProperty)}*");
    }

    [Fact]
    public void Throws_for_property_without_setter_for_compose_link()
    {
        using var dataPort = CreateDataPort(b => b
            .AddInput<int>("ValueIn1", PoolingStrategy.Queue)
            .AddOutput<Test>("ValueOut1", new OnChangedTransferMode(), PoolingStrategy.Queue)
            .AddNodeLink("ValueIn1", "ValueOut1", nameof(Test.PropertyWithoutSetter), NodeLinkModification.Compose));

        var act = dataPort.Invoking(x => x.InitializeI());

        act.Should().Throw<InvalidOperationException>().WithMessage($"*type*{nameof(Test)}*not*contain*public*setter*path*{nameof(Test.PropertyWithoutSetter)}*");
    }

    [Fact]
    public void Returns_empty_when_there_are_no_links()
    {
        using var dataPort = CreateInitializedDataPort(b => b
            .AddInput<int>("ValueIn1", PoolingStrategy.Queue));

        ConnectionFactory factory = new(dataPort, _logger);
        var processors = factory.CreateInputProcessors(_interceptor, out var composers);

        processors.Should().BeEmpty();
        composers.Should().BeEmpty();
    }

    [Fact]
    public void Creates_all_processors_for_mixed_links()
    {
        using var dataPort = CreateInitializedDataPort(b => b
            .AddInput<int>("ValueIn1", PoolingStrategy.Queue)
            .AddOutput<int>("ValueOut1", new OnChangedTransferMode(), PoolingStrategy.Queue)
            .AddNodeLink("ValueIn1", "ValueOut1")
            .AddInput<Test>("ValueIn2", PoolingStrategy.Queue)
            .AddOutput<int>("ValueOut2", new OnChangedTransferMode(), PoolingStrategy.Queue)
            .AddNodeLink("ValueIn2", "ValueOut2", nameof(Test.MyProperty), NodeLinkModification.Extract)
            .AddInput<int>("ValueIn3", PoolingStrategy.Queue)
            .AddOutput<Test>("ValueOut3", new OnChangedTransferMode(), PoolingStrategy.Queue)
            .AddNodeLink("ValueIn3", "ValueOut3", nameof(Test.MyProperty), NodeLinkModification.Compose));
        var inputs = ((IRuntimeFunctionBlock)dataPort).ProcessDataInputs.ToDictionary(c => c.Connector.Design.Name, c => c.Connector.Id);

        ConnectionFactory factory = new(dataPort, _logger);
        var processors = factory.CreateInputProcessors(_interceptor, out var composers);

        processors.Should().HaveCount(3);
        processors[inputs["ValueIn1"]].Should().BeOfType<PasstroughProcessor>();
        processors[inputs["ValueIn2"]].Should().BeOfType<ExtractProcessor>();
        processors[inputs["ValueIn3"]].Should().BeOfType<ComposeProcessor>();
        composers.Should().ContainSingle();
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

        ConnectionFactory factory = new(dataPort, _logger);
        var processors = factory.CreateInputProcessors(_interceptor, out var composers);

        processors.Should().ContainSingle()
            .Which.Value.Should().BeOfType<PasstroughProcessor>();
        composers.Should().BeEmpty();
    }

    [Fact]
    public void Adds_same_composer_for_each_input_of_a_composition_group()
    {
        using var dataPort = CreateInitializedDataPort(b => b
            .AddInput<int>("ValueIn1", PoolingStrategy.Queue)
            .AddInput<int>("ValueIn2", PoolingStrategy.Queue)
            .AddOutput<Test>("ValueOut1", new OnChangedTransferMode(), PoolingStrategy.Queue)
            .AddNodeLink("ValueIn1", "ValueOut1", nameof(Test.MyProperty), NodeLinkModification.Compose)
            .AddNodeLink("ValueIn2", "ValueOut1", nameof(Test.MyOtherProperty), NodeLinkModification.Compose));
        var inputs = ((IRuntimeFunctionBlock)dataPort).ProcessDataInputs.ToDictionary(c => c.Connector.Design.Name, c => c.Connector.Id);

        ConnectionFactory factory = new(dataPort, _logger);
        var processors = factory.CreateInputProcessors(_interceptor, out var composers);

        processors.Should().HaveCount(2);
        processors[inputs["ValueIn1"]].Should().BeSameAs(composers[0]);
        processors[inputs["ValueIn2"]].Should().BeSameAs(composers[0]);
        composers.Should().ContainSingle();
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
