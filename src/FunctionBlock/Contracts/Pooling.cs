using System.Text.Json.Serialization;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Suite.System.Contracts;

public sealed class Pooling : IPoolable
{
    [JsonConverter(typeof(TypeNameHandlingConverter))]
    public IAggregatingPooling? CrossSourceAggregatingPooling { get; init; }
    public required PoolingStrategy CrossSourcePoolingStrategy { get; init; }
    [JsonConverter(typeof(TypeNameHandlingConverter))]
    public IAggregatingPooling? PerSourceAggregatingPooling { get; init; }
    public required PoolingStrategy PerSourcePoolingStrategy { get; init; }
}
