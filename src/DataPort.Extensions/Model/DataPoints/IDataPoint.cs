using System.Linq;

namespace ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

/// <summary>
/// Defines the base contract for a data point, representing a single addressable data element
/// that can be read from or written to an external system.
/// </summary>
/// <remarks>
/// Data points are the fundamental unit of data exchange in the extension framework.
/// Each data point has an address (protocol-specific), a data type, and associated channels
/// for routing and filtering purposes.
/// </remarks>
public interface IDataPoint
{
    /// <summary>
    /// Gets or initializes the channel configuration for this data point.
    /// </summary>
    Channels Channels { get; init; }

    /// <summary>
    /// Determines whether this data point contains the specified transferred channel.
    /// </summary>
    /// <param name="channel">The channel identifier to search for.</param>
    /// <returns><c>true</c> if the data point contains the specified transferred channel; otherwise, <c>false</c>.</returns>
    bool ContainsTransferredChannel(string channel) =>
        Channels.TransferredChannels.Any(transferredChannel => transferredChannel.Value == channel);

    /// <summary>
    /// Gets a human-readable string representation of this data point's address.
    /// </summary>
    /// <remarks>
    /// The format of this string is protocol-specific and is primarily used for logging and diagnostics.
    /// </remarks>
    DataPointIdentifier Identifier { get; }

    /// <summary>
    /// Gets a human-readable string representation of this data point's data type.
    /// </summary>
    /// <remarks>
    /// The format of this string is protocol-specific and is primarily used for logging and diagnostics.
    /// </remarks>
    DataTypeName DataTypeName { get; }
}

/// <summary>
/// A strongly-typed identifier for a data point.
/// </summary>
/// <param name="Value">The raw string value of the identifier.</param>
public record DataPointIdentifier(string Value);

/// <summary>
/// A strongly-typed name for a data point's data type.
/// </summary>
/// <param name="Value">The raw string representation of the data type name.</param>
public record DataTypeName(string Value);
