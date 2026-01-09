using ViciOne.ManagedEngine.Communication;

namespace ViciOne.Suite.DataPort;

[Communication("httpclientdataport")]
public sealed record class HttpClientDataPortCommunication : DataPortCommunication
{
    public string BaseAddress { get; set; } = string.Empty;
    public int? PollInterval { get; set; }
    public byte AuthenticationType { get; set; }
    public string BearerToken { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
