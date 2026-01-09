using ViciOne.ManagedEngine.Communication;

namespace ViciOne.Suite.DataPort;

[Communication("httpserverdataport")]
public sealed record class HttpServerDataPortCommunication : DataPortCommunication
{
    public string BaseAddress { get; set; } = string.Empty;
    public byte AuthenticationType { get; set; }
    public string BearerToken { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string CertificateFilePassword { get; set; } = string.Empty;
    public string CertificateFile { get; set; } = string.Empty;
    public string CertificatePrivateKeyFile { get; set; } = string.Empty;
}
