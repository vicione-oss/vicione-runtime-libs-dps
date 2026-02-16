using System;
using System.Collections.Generic;
using System.Threading;
using AwesomeAssertions;

#pragma warning disable CA1515
namespace ViciOne.Suite.DataPort.Extensions.Testing.Assertions;

/// <summary>
/// Extension methods for eventual assertions in tests.
/// Provides assertions that wait for conditions to eventually become true within a timeout.
/// </summary>
public static class EventualAssertions
{
    /// <summary>
    /// Default timeout in milliseconds for test assertions.
    /// </summary>
    public const int DefaultTestTimeoutMs = 5000;

    /// <summary>
    /// Creates an eventual value wrapper that defers evaluation until assertion time.
    /// </summary>
    /// <typeparam name="T">The type of value being evaluated.</typeparam>
    /// <param name="getValue">Function that retrieves the current value.</param>
    /// <param name="timeoutMs">The timeout in milliseconds. Defaults to <see cref="DefaultTestTimeoutMs"/> (5 seconds).</param>
    /// <returns>An eventual value wrapper for fluent assertions.</returns>
    /// <example>
    /// Usage:
    /// <code>
    /// Eventually(() => callCount).Should().Be(21, "retries should complete");
    /// Eventually(() => status).Should().Be("complete", "operation should finish");
    /// Eventually(() => items.Count, timeoutMs: 10000).Should().BeGreaterThan(5, "items should be added");
    /// </code>
    /// </example>
    public static EventualValue<T> Eventually<T>(Func<T> getValue, int timeoutMs = DefaultTestTimeoutMs)
        => new(getValue, timeoutMs);
}

/// <summary>
/// Wrapper for a value that should eventually meet an assertion condition.
/// </summary>
/// <typeparam name="T">The type of value being evaluated.</typeparam>
public sealed class EventualValue<T>(Func<T> getValue, int timeoutMs)
{
    /// <summary>
    /// Creates an assertion context that will poll until the condition is met.
    /// </summary>
    /// <returns>An assertion context for fluent assertions.</returns>
    public EventualValueAssertions<T> Should() => new(getValue, timeoutMs);
}

/// <summary>
/// Assertion methods for eventual values that should meet specific conditions.
/// </summary>
/// <typeparam name="T">The type of value being asserted.</typeparam>
public sealed class EventualValueAssertions<T>(Func<T> getValue, int timeoutMs)
{
    /// <summary>
    /// Asserts that the value eventually equals the expected value.
    /// </summary>
    /// <param name="expected">The expected value.</param>
    /// <param name="message">The assertion message if the condition is not met within the timeout.</param>
    public void Be(T expected, string message)
    {
        var conditionMet = SpinWait.SpinUntil(
            () => EqualityComparer<T>.Default.Equals(getValue(), expected),
            TimeSpan.FromMilliseconds(timeoutMs));

        conditionMet.Should().BeTrue(message);
        getValue().Should().Be(expected);
    }

    /// <summary>
    /// Asserts that the value eventually satisfies a custom condition.
    /// </summary>
    /// <param name="condition">The condition the value should satisfy.</param>
    /// <param name="message">The assertion message if the condition is not met within the timeout.</param>
    public void Match(Func<T, bool> condition, string message)
    {
        var conditionMet = SpinWait.SpinUntil(
            () => condition(getValue()),
            TimeSpan.FromMilliseconds(timeoutMs));

        conditionMet.Should().BeTrue(message);
    }
}

/// <summary>
/// Extension methods for eventual boolean assertions.
/// </summary>
public static class EventualBooleanAssertions
{
    /// <summary>
    /// Asserts that the boolean value eventually becomes true.
    /// </summary>
    /// <param name="assertions">The assertion context.</param>
    /// <param name="message">The assertion message if the condition is not met within the timeout.</param>
    public static void BeTrue(this EventualValueAssertions<bool> assertions, string message) =>
        assertions.Be(true, message);

    /// <summary>
    /// Asserts that the boolean value eventually becomes false.
    /// </summary>
    /// <param name="assertions">The assertion context.</param>
    /// <param name="message">The assertion message if the condition is not met within the timeout.</param>
    public static void BeFalse(this EventualValueAssertions<bool> assertions, string message) =>
        assertions.Be(false, message);
}
