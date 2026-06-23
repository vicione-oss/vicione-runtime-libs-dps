using System.Text.Json.Serialization;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel;

namespace ViciOne.Suite.System.Runtime;

internal class PoolableConnectorOutput<TValue> : ConnectorOutput<TValue>, IPoolable
{
    [JsonConverter(typeof(TypeNameHandlingConverter))]
    public IAggregatingPooling? CrossSourceAggregatingPooling { get; init; }
    public required PoolingStrategy CrossSourcePoolingStrategy { get; init; }
    [JsonConverter(typeof(TypeNameHandlingConverter))]
    public IAggregatingPooling? PerSourceAggregatingPooling { get; init; }
    public required PoolingStrategy PerSourcePoolingStrategy { get; init; }
}
