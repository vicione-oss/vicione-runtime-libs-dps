using System;
using System.Threading.Tasks;
using ViciOne.Suite.DataPort.Extensions.Testing.Assertions;
using Xunit.Sdk;

namespace ViciOne.Suite.DataPort.Extensions.Testing.Tests.Assertions;

public class EventualAssertionsTests
{
    [Fact]
    public async Task Eventually_Be_Succeeds_WhenValueBecomesExpected()
    {
        // Arrange
        var counter = 0;
        var task = Task.Run(async () =>
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);
            counter = 42;
        }, TestContext.Current.CancellationToken);

        // Act
        var act = () => EventualAssertions.Eventually(() => counter, timeoutMs: 1000)
            .Should()
            .Be(42, "counter should eventually be 42");

        // Assert
        act.Should().NotThrow();
        await task;
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public void Eventually_Be_Fails_WhenValueNeverBecomesExpected()
    {
        // Arrange
        var counter = 0;

        // Act
        var act = () => EventualAssertions.Eventually(() => counter, timeoutMs: 100)
            .Should()
            .Be(42, "counter should eventually be 42");

        // Assert
        act.Should().Throw<XunitException>()
            .WithMessage("*counter should eventually be 42*");
    }

    [Fact]
    public void Eventually_Be_Succeeds_Immediately_WhenValueIsAlreadyExpected()
    {
        // Arrange
        var counter = 42;

        // Act
        var act = () => EventualAssertions.Eventually(() => counter, timeoutMs: 100)
            .Should()
            .Be(42, "counter should be 42");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task Eventually_Match_Succeeds_WhenConditionBecomesTrue()
    {
        // Arrange
        var counter = 0;
        var task = Task.Run(async () =>
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);
            counter = 50;
        }, TestContext.Current.CancellationToken);

        // Act
        var act = () => EventualAssertions.Eventually(() => counter, timeoutMs: 1000)
            .Should()
            .Match(x => x > 40, "counter should eventually be greater than 40");

        // Assert
        act.Should().NotThrow();
        await task;
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public void Eventually_Match_Fails_WhenConditionNeverBecomesTrue()
    {
        // Arrange
        var counter = 10;

        // Act
        var act = () => EventualAssertions.Eventually(() => counter, timeoutMs: 100)
            .Should()
            .Match(x => x > 40, "counter should eventually be greater than 40");

        // Assert
        act.Should().Throw<XunitException>()
            .WithMessage("*counter should eventually be greater than 40*");
    }

    [Fact]
    public async Task Eventually_BeTrue_Succeeds_WhenBooleanBecomesTrue()
    {
        // Arrange
        var flag = false;
        var task = Task.Run(async () =>
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);
            flag = true;
        }, TestContext.Current.CancellationToken);

        // Act
        var act = () => EventualAssertions.Eventually(() => flag, timeoutMs: 1000)
            .Should()
            .BeTrue("flag should eventually be true");

        // Assert
        act.Should().NotThrow();
        await task;
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task Eventually_BeFalse_Succeeds_WhenBooleanBecomesFalse()
    {
        // Arrange
        var flag = true;
        var task = Task.Run(async () =>
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);
            flag = false;
        }, TestContext.Current.CancellationToken);

        // Act
        var act = () => EventualAssertions.Eventually(() => flag, timeoutMs: 1000)
            .Should()
            .BeFalse("flag should eventually be false");

        // Assert
        act.Should().NotThrow();
        await task;
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public void Eventually_UsesDefaultTimeout_WhenNotSpecified()
    {
        // Arrange
        var counter = 0;
        var startTime = DateTime.UtcNow;

        // Act
        try
        {
            EventualAssertions.Eventually(() => counter)
                .Should()
                .Be(42, "should use default timeout");
        }
        catch (XunitException)
        {
            // Expected to fail
        }

        var elapsed = DateTime.UtcNow - startTime;

        // Assert
        elapsed.TotalMilliseconds.Should().BeGreaterThan(EventualAssertions.DefaultTestTimeoutMs - 500);
    }

    [Fact]
    public async Task Eventually_WorksWithStrings()
    {
        // Arrange
        var status = "pending";
        var task = Task.Run(async () =>
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);
            status = "complete";
        }, TestContext.Current.CancellationToken);

        // Act
        var act = () => EventualAssertions.Eventually(() => status, timeoutMs: 1000)
            .Should()
            .Be("complete", "status should eventually be complete");

        // Assert
        act.Should().NotThrow();
        await task;
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task Eventually_WorksWithCustomTypes()
    {
        // Arrange
        var result = new TestResult { Value = 0 };
        var task = Task.Run(async () =>
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);
            result.Value = 100;
        }, TestContext.Current.CancellationToken);

        // Act
        var act = () => EventualAssertions.Eventually(() => result.Value, timeoutMs: 1000)
            .Should()
            .Match(x => x == 100, "result value should eventually be 100");

        // Assert
        act.Should().NotThrow();
        await task;
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    private class TestResult
    {
        public int Value { get; set; }
    }
}
