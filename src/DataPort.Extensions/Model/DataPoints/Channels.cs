using System.Collections.Generic;

namespace ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

/// <summary>
/// Represents the channel configuration for a data point, containing both affected and transferred channels.
/// </summary>
/// <param name="AffectedChannels">The collection of channels that are affected by this data point's operations.</param>
/// <param name="TransferredChannels">The collection of channels through which data is actively transferred.</param>
public record Channels(
    IReadOnlyList<AffectedChannel> AffectedChannels,
    IReadOnlyList<TransferredChannel> TransferredChannels);

/// <summary>
/// Represents a channel that is affected by a data point's operations but may not actively transfer data.
/// </summary>
/// <param name="Value">The unique identifier or name of the affected channel.</param>
public record AffectedChannel(string Value);

/// <summary>
/// Represents a channel through which data is actively transferred.
/// </summary>
/// <param name="Value">The unique identifier or name of the transferred channel.</param>
public record TransferredChannel(string Value);
