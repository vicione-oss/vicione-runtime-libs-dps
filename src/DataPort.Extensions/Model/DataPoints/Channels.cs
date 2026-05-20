using System.Collections.Generic;
using System.Linq;

namespace ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

/// <summary>
/// Represents the channel configuration for a data point, containing both affected and transferred channels.
/// </summary>
/// <param name="Affected">The collection of channels that are affected by this data point's operations.</param>
/// <param name="Transferred">The collection of channels through which data is actively transferred.</param>
public record struct Channels(
    IReadOnlyList<AffectedChannel> Affected,
    IReadOnlyList<TransferredChannel> Transferred)
{
    /// <summary>
    /// Creates a list of <see cref="AffectedChannel"/> instances from the given channel names.
    /// </summary>
    /// <param name="channelNames">The names of the affected channels.</param>
    /// <returns>A list of <see cref="AffectedChannel"/> instances, one per supplied name.</returns>
    public static IReadOnlyList<AffectedChannel> AffectedChannels(params string[] channelNames) =>
        [.. channelNames.Select(name => new AffectedChannel(name))];

    /// <summary>
    /// Creates a list of <see cref="TransferredChannel"/> instances from the given channel names.
    /// </summary>
    /// <param name="channelNames">The names of the transferred channels.</param>
    /// <returns>A list of <see cref="TransferredChannel"/> instances, one per supplied name.</returns>
    public static IReadOnlyList<TransferredChannel> TransferredChannels(params string[] channelNames) =>
        [.. channelNames.Select(name => new TransferredChannel(name))];
}

/// <summary>
/// Represents a channel that is affected by a data point's operations but may not actively transfer data.
/// </summary>
/// <param name="Value">The unique identifier or name of the affected channel.</param>
public readonly record struct AffectedChannel(string Value);

/// <summary>
/// Represents a channel through which data is actively transferred.
/// </summary>
/// <param name="Value">The unique identifier or name of the transferred channel.</param>
public readonly record struct TransferredChannel(string Value);
