namespace ViciOne.Suite.System.Contracts;

public sealed class OutputConfiguration
{
    public required Pooling Pooling { get; init; }
    public bool MarkAsChangedIfNotEqual { get; init; }
}
