using System.Collections.Generic;
using ViciOne.ManagedEngine.ExternalCommunication;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing.ChannelResolution;

internal readonly record struct ChannelResolutions<TDataPoint>(
    IReadOnlyList<ResolvedChannel<TDataPoint>> ResolvedChannels,
    IReadOnlyList<UnresolvedChannel> UnresolvedChannels)
    where TDataPoint : IDataPoint;

/// <summary>
/// Represents a resolved pair of an external value and the data point its channel was matched to.
/// </summary>
/// <param name="ExternalValue">The external value whose channel was resolved.</param>
/// <param name="DataPoint">The data point the channel resolved to.</param>
public readonly record struct ResolvedChannel<TDataPoint>(ExternalValue ExternalValue, TDataPoint DataPoint);

/// <summary>
/// Represents an external value whose channel could not be resolved to any configured data point.
/// </summary>
/// <param name="ExternalValue">The external value that could not be resolved.</param>
public readonly record struct UnresolvedChannel(ExternalValue ExternalValue);
