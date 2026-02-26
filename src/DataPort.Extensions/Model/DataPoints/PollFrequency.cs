using System;

#pragma warning disable CA2225
namespace ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

/// <summary>
/// Represents the polling frequency for reading a data point from an external system.
/// </summary>
public readonly record struct PollFrequency(TimeSpan Value)
{
    /// <summary>
    /// Implicitly converts a TimeSpan to a PollFrequency.
    /// </summary>
    /// <param name="value">The TimeSpan value to convert.</param>
    public static implicit operator PollFrequency(TimeSpan value) => new(value);

    /// <summary>
    /// Implicitly converts a PollFrequency to a TimeSpan.
    /// </summary>
    /// <param name="frequency">The PollFrequency to convert.</param>
    public static implicit operator TimeSpan(PollFrequency frequency) => frequency.Value;
}
