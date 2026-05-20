using System.Collections.Frozen;
using System.Collections.Generic;
using ViciOne.ManagedEngine.ExternalCommunication;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing.ChannelResolution;

/// <summary>
/// Resolves incoming channel names to their corresponding data points for writing.
/// Pre-builds a reverse index from <see cref="IDataPoint.Channels"/> at construction
/// time, providing O(1) resolution per channel.
/// </summary>
/// <typeparam name="TDataPoint">The data point type.</typeparam>
internal sealed class ChannelResolver<TDataPoint> where TDataPoint : IDataPoint
{
    private readonly FrozenDictionary<string, TDataPoint> _channelIndex;

    public ChannelResolver(IEnumerable<TDataPoint> dataPoints)
    {
        var channelIndex = new Dictionary<string, TDataPoint>();
        foreach (var dataPoint in dataPoints)
        {
            foreach (var channel in dataPoint.Channels.TransferredChannels)
                channelIndex[channel.Value] = dataPoint;
        }

        _channelIndex = channelIndex.ToFrozenDictionary();
    }

    private TDataPoint? Resolve(ExternalValue externalValue) => _channelIndex.GetValueOrDefault(externalValue.Channel);

    public ChannelResolutions<TDataPoint> Resolve(IReadOnlyCollection<ExternalValue> externalValues)
    {
        var resolved = new List<ResolvedChannel<TDataPoint>>(externalValues.Count);
        var unresolved = new List<UnresolvedChannel>();

        foreach (var externalValue in externalValues)
        {
            var dataPoint = Resolve(externalValue);
            if (dataPoint is not null)
                resolved.Add(new ResolvedChannel<TDataPoint>(externalValue, dataPoint));
            else
                unresolved.Add(new UnresolvedChannel(externalValue));
        }

        return new ChannelResolutions<TDataPoint>(resolved, unresolved);
    }
}
