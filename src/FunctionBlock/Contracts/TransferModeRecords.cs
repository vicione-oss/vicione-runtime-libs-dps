using System;

namespace ViciOne.Suite.System.Contracts;

public record OnChangedTransferMode : ITransferMode { }

public record PeriodicTransferMode : ITransferMode
{
    public PeriodicTransferMode(TimeSpan interval)
        => Interval = interval;

    public TimeSpan Interval { get; init; }
}

public record CyclicTransferMode : ITransferMode
{
    public CyclicTransferMode(uint interval)
        => Interval = interval;

    public uint Interval { get; init; }
}
