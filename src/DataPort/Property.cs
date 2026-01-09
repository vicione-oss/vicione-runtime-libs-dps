using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ViciOne.Suite.DataPort;

public class Property
{
    [JsonConverter(typeof(TypeNameHandlingConverter))]
    public required object Value { get; init; }
    public List<string> Channels { get; init; } = [];
}
