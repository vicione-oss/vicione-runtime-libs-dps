using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Extensions;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Runtime;
using ViciOne.Engine.DefaultPoolings;
using ViciOne.Suite.System.Contracts;
using ViciOne.Suite.System.Runtime;
using Xunit;

namespace ViciOne.Suite.System;

public sealed class DataPort_General
{
    [Fact]
    public void Does_nothing_if_disabled()
    {
        using FunctionBlockExecutionContext<DataPort> context = new(a => new DataflowInfoBuilder(a)
            .AddInput("ValueIn1", PoolingStrategy.Aggregate, new MaxPooling<int>())
            .AddOutput("ValueOut1", new OnChangedTransferMode(), PoolingStrategy.Aggregate, new MaxPooling<int>())
            .AddNodeLink("ValueIn1", "ValueOut1")
            .CreateDataflowInfo());
        var value = context.GetOutputValue<int>("ValueOut1");
        context.ChangeFunctionBlockEnabled(false);
        context.ChangeInputValue("ValueIn1", 100);

        context.ProcessCycle();

        context.GetOutputValue<int>("ValueOut1").Should().Be(value);
    }

    [Fact]
    public void Does_not_set_output_when_value_is_null()
    {
        using FunctionBlockExecutionContext<DataPort> context = new(a => new DataflowInfoBuilder(a)
            .AddInput<object>("ValueIn1", PoolingStrategy.Queue)
            .AddOutput<object>("ValueOut1", new OnChangedTransferMode(), PoolingStrategy.Queue)
            .AddNodeLink("ValueIn1", "ValueOut1")
            .CreateDataflowInfo());

        context.ChangeInputValue("ValueIn1", 10);
        context.ProcessCycle();
        context.GetOutput("ValueOut1").Value.Should().Be(10);

        context.ChangeInputValue("ValueIn1", null!);
        context.ProcessCycle();
        context.GetOutput("ValueOut1").Value.Should().Be(10);
    }

    [Fact]
    public void Does_not_set_output_in_first_cycle_with_unchanged_values()
    {
        var now = DateTime.UtcNow;

        // executor calls ProcessCycle once during initialization
        using FunctionBlockExecutionContext<DataPort> context = new(a => new DataflowInfoBuilder(a)
            .AddInput<double>("ValueIn1", PoolingStrategy.Aggregate, new AveragePooling())
            .AddOutput<double>("ValueOut1", new OnChangedTransferMode(), PoolingStrategy.Aggregate, new AveragePooling())
            .AddNodeLink("ValueIn1", "ValueOut1")
            .AddInput<DateTime>("ValueIn2", PoolingStrategy.Queue)
            .AddOutput<DateTime>("ValueOut2", new OnChangedTransferMode(), PoolingStrategy.Queue)
            .AddNodeLink("ValueIn2", "ValueOut2")
            .CreateDataflowInfo());

        context.GetOutput("ValueOut1").Time.Should().BeBefore(now);
        context.GetOutput("ValueOut2").Time.Should().BeBefore(now);
    }

    [Fact]
    public void Does_set_output_in_first_cycle_with_changed_values()
    {
        var now = DateTime.UtcNow;

        using FunctionBlockExecutionContext<DataPort> context = new(a => new DataflowInfoBuilder(a)
            .AddInput<double>("ValueIn1", PoolingStrategy.Aggregate, new AveragePooling())
            .AddOutput<double>("ValueOut1", new OnChangedTransferMode(), PoolingStrategy.Aggregate, new AveragePooling())
            .AddNodeLink("ValueIn1", "ValueOut1")
            .AddInput<DateTime>("ValueIn2", PoolingStrategy.Queue)
            .AddOutput<DateTime>("ValueOut2", new OnChangedTransferMode(), PoolingStrategy.Queue)
            .AddNodeLink("ValueIn2", "ValueOut2")
            .CreateDataflowInfo(), true);

        context.ChangeInputValue("ValueIn1", 10d);
        context.ChangeInputValue("ValueIn2", DateTime.UtcNow);

        context.ProcessCycle();

        context.GetOutput("ValueOut1").Time.Should().BeAfter(now);
        context.GetOutput("ValueOut2").Time.Should().BeAfter(now);
    }
}

public sealed class DataPort_LinkGroups : IDisposable
{
    private readonly FunctionBlockExecutionContext<DataPort> _context = new(a => new DataflowInfoBuilder(a)
        .SetRunMode(RunMode.Cyclic)
        .AddInput("ValueIn1", PoolingStrategy.Aggregate, new MaxPooling<int>())
        .AddInput("ValueIn2", PoolingStrategy.Aggregate, new MaxPooling<double>())
        .AddInput("ValueIn3", PoolingStrategy.Aggregate, new MaxPooling<double>())
        .AddOutput("ValueOut1", new CyclicTransferMode(2), PoolingStrategy.Aggregate, new MaxPooling<int>())
        .AddOutput("ValueOut2", new CyclicTransferMode(3), PoolingStrategy.Aggregate, new MaxPooling<int>())
        .AddOutput<double>("ValueOut3", new OnChangedTransferMode(), PoolingStrategy.Aggregate, new AveragePooling())
        .AddNodeLink("ValueIn1", "ValueOut1")
        .AddNodeLink("ValueIn1", "ValueOut2")
        .AddNodeLink("ValueIn2", "ValueOut3")
        .AddNodeLink("ValueIn3", "ValueOut3")
        .CreateDataflowInfo());
    public void Dispose() => _context.Dispose();

    [Fact]
    public void Output_link_groups_works_properly()
    {
        _context.ChangeInputValue("ValueIn1", 1);
        _context.ProcessCycle();
        _context.GetOutputValue<int>("ValueOut1").Should().Be(1);
        _context.GetOutputValue<int>("ValueOut2").Should().Be(0);

        _context.ChangeInputValue("ValueIn1", 2);
        _context.ProcessCycle();
        _context.GetOutputValue<int>("ValueOut1").Should().Be(1);
        _context.GetOutputValue<int>("ValueOut2").Should().Be(2);

        _context.ChangeInputValue("ValueIn1", 3);
        _context.ProcessCycle();
        _context.GetOutputValue<int>("ValueOut1").Should().Be(3);
        _context.GetOutputValue<int>("ValueOut2").Should().Be(2);
    }

    [Fact]
    public void Input_link_groups_works_properly()
    {
        _context.ChangeInputValue("ValueIn2", 2d);
        _context.ChangeInputValue("ValueIn3", 8d);
        _context.ProcessCycle();
        _context.GetOutputValue<double>("ValueOut3").Should().Be(5);
    }
}

public sealed class DataPort_TransferModes_OnChanged : IDisposable
{
    private readonly FunctionBlockExecutionContext<DataPort> _context = new(a => new DataflowInfoBuilder(a)
        .AddInput("ValueIn1", PoolingStrategy.Aggregate, new MaxPooling<int>())
        .AddOutput("ValueOut1", new OnChangedTransferMode(), PoolingStrategy.Aggregate, new MaxPooling<int>())
        .AddNodeLink("ValueIn1", "ValueOut1")
        .AddInput("ValueIn2", PoolingStrategy.Aggregate, new MaxPooling<int>())
        .AddOutput("ValueOut2", new OnChangedTransferMode(), PoolingStrategy.Aggregate, new MaxPooling<int>())
        .AddNodeLink("ValueIn2", "ValueOut2")
        .CreateDataflowInfo());
    public void Dispose() => _context.Dispose();

    [Fact]
    public void Does_set_output()
    {
        var value = _context.GetOutputValue<int>("ValueOut1");
        _context.ChangeInputValue("ValueIn1", 100);

        _context.ProcessCycle();

        _context.GetOutputValue<int>("ValueOut1").Should().NotBe(value);
    }

    [Fact]
    public void Does_not_set_output()
    {
        var value = _context.GetOutput("ValueOut1").Time;
        _context.ChangeInputValue("ValueIn2", 100);

        _context.ProcessCycle();

        _context.GetOutput("ValueOut1").Time.Should().Be(value);
    }
}

public sealed class DataPort_TransferModes_Cyclic : IDisposable
{
    private readonly FunctionBlockExecutionContext<DataPort> _context = new(a => new DataflowInfoBuilder(a)
        .SetRunMode(RunMode.Cyclic)
        .AddInput("ValueIn1", PoolingStrategy.Aggregate, new MaxPooling<int>())
        .AddOutput("ValueOut1", new CyclicTransferMode(3), PoolingStrategy.Aggregate, new MaxPooling<int>())
        .AddNodeLink("ValueIn1", "ValueOut1")
        .CreateDataflowInfo());
    public void Dispose() => _context.Dispose();

    [Fact]
    public void Does_set_output()
    {
        _context.ChangeInputValue("ValueIn1", 50);
        _context.ProcessCycle();

        _context.ChangeInputValue("ValueIn1", 100);
        _context.ProcessCycle();

        _context.GetOutputValue<int>("ValueOut1").Should().Be(100);
    }

    [Fact]
    public void Does_set_same_value_again()
    {
        _context.ChangeInputValue("ValueIn1", 50);
        _context.ProcessCycle();
        _context.ProcessCycle();

        _context.GetOutputValue<int>("ValueOut1").Should().Be(50);
        var time = _context.GetOutput("ValueOut1").Time;

        _context.ProcessCycle();
        _context.ProcessCycle();
        _context.ProcessCycle();

        _context.GetOutputValue<int>("ValueOut1").Should().Be(50);
        _context.GetOutput("ValueOut1").Time.Should().NotBe(time);
    }

    [Fact]
    public void Does_not_set_output()
    {
        _context.ChangeInputValue("ValueIn1", 50);
        _context.ProcessCycle();

        _context.GetOutputValue<int>("ValueOut1").Should().Be(0);
    }
}

public sealed class DataPort_TransferModes_Periodic : IDisposable
{
    private readonly FunctionBlockExecutionContext<DataPort> _context = new(a => new DataflowInfoBuilder(a)
        .AddInput("ValueIn1", PoolingStrategy.Aggregate, new MaxPooling<int>())
        .AddOutput("ValueOut1", new PeriodicTransferMode(250.Milliseconds()), PoolingStrategy.Aggregate, new MaxPooling<int>())
        .AddNodeLink("ValueIn1", "ValueOut1")
        .CreateDataflowInfo());
    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Acceptance_Async()
    {
        _context.ChangeInputValue("ValueIn1", 50);
        _context.ProcessCycle();

        _context.ChangeInputValue("ValueIn1", 100);
        _context.ProcessCycle();

        _context.GetOutputValue<int>("ValueOut1").Should().Be(0);

        await Task.Delay(250, TestContext.Current.CancellationToken);

        _context.ProcessCycle();

        _context.GetOutputValue<int>("ValueOut1").Should().Be(100);
    }

    [Fact]
    public async Task Does_set_same_value_again_Async()
    {
        _context.ChangeInputValue("ValueIn1", 100);
        _context.ProcessCycle();
        await Task.Delay(250, TestContext.Current.CancellationToken);
        _context.ProcessCycle();

        _context.GetOutputValue<int>("ValueOut1").Should().Be(100);
        var time = _context.GetOutput("ValueOut1").Time;

        await Task.Delay(250, TestContext.Current.CancellationToken);
        _context.ProcessCycle();
        _context.GetOutputValue<int>("ValueOut1").Should().Be(100);
        _context.GetOutput("ValueOut1").Time.Should().NotBe(time);

    }
}

public sealed class DataPort_AggregatingPooling_MaxPooling : IDisposable
{
    private readonly FunctionBlockExecutionContext<DataPort> _context = new(a => new DataflowInfoBuilder(a)
        .SetRunMode(RunMode.Cyclic)
        .AddInput("ValueIn1", PoolingStrategy.Aggregate, new MaxPooling<int>())
        .AddOutput("ValueOut1", new CyclicTransferMode(4), PoolingStrategy.Aggregate, new MaxPooling<int>())
        .AddNodeLink("ValueIn1", "ValueOut1")
        .CreateDataflowInfo());
    public void Dispose() => _context.Dispose();

    [Fact]
    public void Does_set_output_to_max_value()
    {
        _context.ChangeInputValue("ValueIn1", 10);
        _context.ProcessCycle();
        _context.ChangeInputValue("ValueIn1", 100);
        _context.ProcessCycle();
        _context.ChangeInputValue("ValueIn1", 25);
        _context.ProcessCycle();

        _context.GetOutputValue<int>("ValueOut1").Should().Be(100);
    }
}

public sealed class DataPort_AggregatingPooling_MinPooling : IDisposable
{
    private readonly FunctionBlockExecutionContext<DataPort> _context = new(a => new DataflowInfoBuilder(a)
        .SetRunMode(RunMode.Cyclic)
        .AddInput("ValueIn1", PoolingStrategy.Aggregate, new MinPooling<int>())
        .AddOutput("ValueOut1", new CyclicTransferMode(4), PoolingStrategy.Aggregate, new MinPooling<int>())
        .AddNodeLink("ValueIn1", "ValueOut1")
        .CreateDataflowInfo());
    public void Dispose() => _context.Dispose();

    [Fact]
    public void Does_set_output_to_min_value()
    {
        _context.ChangeInputValue("ValueIn1", -1);
        _context.ProcessCycle();
        _context.ChangeInputValue("ValueIn1", 100);
        _context.ProcessCycle();
        _context.ChangeInputValue("ValueIn1", 25);
        _context.ProcessCycle();

        _context.GetOutputValue<int>("ValueOut1").Should().Be(-1);
    }
}

public sealed class DataPort_AggregatingPooling_AveragePooling : IDisposable
{
    private readonly FunctionBlockExecutionContext<DataPort> _context = new(a => new DataflowInfoBuilder(a)
        .SetRunMode(RunMode.Cyclic)
        .AddInput("ValueIn1", PoolingStrategy.Aggregate, (IAggregatingPooling<double>)new AveragePooling())
        .AddOutput("ValueOut1", new CyclicTransferMode(2), PoolingStrategy.Aggregate, (IAggregatingPooling<double>)new AveragePooling())
        .AddNodeLink("ValueIn1", "ValueOut1")
        .CreateDataflowInfo());
    public void Dispose() => _context.Dispose();

    [Fact]
    public void Does_set_output_to_average_value()
    {
        _context.ChangeInputValue("ValueIn1", 100d);
        _context.ChangeInputValue("ValueIn1", 200d);

        _context.ProcessCycle();

        _context.GetOutputValue<double>("ValueOut1").Should().Be(150d);
    }
}

public sealed class DataPort_ConcatPooling : IDisposable
{
    private readonly FunctionBlockExecutionContext<DataPort> _context = new(a => new DataflowInfoBuilder(a)
        .SetRunMode(RunMode.Cyclic)
        .AddInput<int[]>("ValueIn1", PoolingStrategy.Concat)
        .AddOutput("ValueOut1", new CyclicTransferMode(3), PoolingStrategy.Aggregate, new ConcatArraysPooling<int>())
        .AddNodeLink("ValueIn1", "ValueOut1")
        .CreateDataflowInfo());
    public void Dispose() => _context.Dispose();

    [Fact]
    public void Acceptance()
    {
        _context.ChangeInputValue("ValueIn1", 10);
        _context.ProcessCycle();
        _context.ChangeInputValue("ValueIn1", 20);
        _context.ProcessCycle();

        _context.GetOutputValue<int[]>("ValueOut1").Should().HaveCount(2).And.Contain(10).And.Contain(20);
    }
}

public sealed class DataPort_QueuePooling : IDisposable
{
    private readonly FunctionBlockExecutionContext<DataPort> _context = new(a => new DataflowInfoBuilder(a)
        .SetRunMode(RunMode.Cyclic)
        .AddInput<int>("ValueIn1", PoolingStrategy.Queue)
        .AddOutput<int>("ValueOut1", new CyclicTransferMode(1), PoolingStrategy.Queue)
        .AddNodeLink("ValueIn1", "ValueOut1")
        .CreateDataflowInfo());
    public void Dispose() => _context.Dispose();

    [Fact]
    public void Acceptance()
    {
        _context.ChangeInputValue("ValueIn1", 10);
        _context.ChangeInputValue("ValueIn1", 20);
        _context.ProcessCycle();
        _context.GetOutputValue<int>("ValueOut1").Should().Be(10);

        _context.ProcessCycle();
        _context.GetOutputValue<int>("ValueOut1").Should().Be(20);
    }
}

public sealed class DataPort_MarkAsChangedIfNotEqual_True : IDisposable
{
    private readonly FunctionBlockExecutionContext<DataPort> _context = new(a => new DataflowInfoBuilder(a)
        .SetRunMode(RunMode.Cyclic)
        .AddInput("ValueIn1", PoolingStrategy.Aggregate, new MaxPooling<int>())
        .AddInput("ValueIn2", PoolingStrategy.Aggregate, new MaxPooling<int>())
        .AddOutput("ValueOut1", new OnChangedTransferMode(), PoolingStrategy.Aggregate, new MaxPooling<int>(), false)
        .AddNodeLink("ValueIn1", "ValueOut1")
        .AddNodeLink("ValueIn2", "ValueOut1")
        .CreateDataflowInfo());
    public void Dispose() => _context.Dispose();

    [Fact]
    public void Changes_output_to_same_value()
    {
        _context.ChangeInputValue("ValueIn1", 10);
        _context.ChangeInputValue("ValueIn2", 10);
        _context.ProcessCycle();

        var out1 = _context.GetOutput("ValueOut1");

        _context.ChangeInputValue("ValueIn1", 1);
        _context.ProcessCycle();

        var out2 = _context.GetOutput("ValueOut1");

        out1.Value.Should().Be(10);
        out2.Value.Should().Be(10);
        out1.Time.Should().NotBe(out2.Time);
    }
}

public sealed class DataPort_MarkAsChangedIfNotEqual_False : IDisposable
{
    private readonly FunctionBlockExecutionContext<DataPort> _context = new(a => new DataflowInfoBuilder(a)
        .SetRunMode(RunMode.Cyclic)
        .AddInput("ValueIn1", PoolingStrategy.Aggregate, new MaxPooling<int>())
        .AddInput("ValueIn2", PoolingStrategy.Aggregate, new MaxPooling<int>())
        .AddOutput("ValueOut1", new OnChangedTransferMode(), PoolingStrategy.Aggregate, new MaxPooling<int>(), true)
        .AddNodeLink("ValueIn1", "ValueOut1")
        .AddNodeLink("ValueIn2", "ValueOut1")
        .CreateDataflowInfo());
    public void Dispose() => _context.Dispose();

    [Fact]
    public void Does_not_change_output_value()
    {
        _context.ChangeInputValue("ValueIn1", 10);
        _context.ChangeInputValue("ValueIn2", 10);
        _context.ProcessCycle();

        var out1 = _context.GetOutput("ValueOut1");

        _context.ChangeInputValue("ValueIn1", 1);
        _context.ProcessCycle();

        var out2 = _context.GetOutput("ValueOut1");

        out1.Value.Should().Be(10);
        out2.Value.Should().Be(10);
        out1.Time.Should().Be(out2.Time);
    }
}
