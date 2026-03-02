namespace ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing.Prependable;

/// <summary>
/// Tracks which channel the current cached head came from in PrependableChannelQueue.
/// </summary>
internal enum HeadSource
{
    /// <summary>
    /// No head is currently cached.
    /// </summary>
    None,

    /// <summary>
    /// The cached head came from the priority channel (prepended items).
    /// </summary>
    Priority,

    /// <summary>
    /// The cached head came from the normal channel (appended items).
    /// </summary>
    Normal
}
