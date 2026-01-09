using ViciOne.ManagedEngine.Communication;

namespace ViciOne.Suite.DataPort;

[Communication("influxdbdataport")]
public sealed record class InfluxDbDataPortCommunication : DataPortCommunication
{
    public string Token { get; set; } = string.Empty;
#pragma warning disable CA1056 // URI-ähnliche Eigenschaften dürfen keine Zeichenfolgen sein. Influx nutzt einen string.
    public string Url { get; set; } = string.Empty;
#pragma warning restore CA1056 // URI-ähnliche Eigenschaften dürfen keine Zeichenfolgen sein
    public string Organization { get; set; } = string.Empty;
    public int BulkSize { get; set; } = 10_000;
    public uint MinimumPublishIntervalInMs { get; set; } = 3_000;
}
