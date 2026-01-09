namespace ViciOne.Suite.DataPort;

internal sealed class HttpServerDataPortProperties(HttpServerDataPortCommunication communication)
{
    internal string BaseAddress
    {
        get => communication.BaseAddress;
        set => communication.BaseAddress = value;
    }

    internal AuthenticationType AuthenticationType
    {
        get => (AuthenticationType)communication.AuthenticationType;
        set => communication.AuthenticationType = (byte)value;
    }

    internal string BearerToken
    {
        get => communication.BearerToken;
        set => communication.BearerToken = value;
    }

    internal string Username
    {
        get => communication.Username;
        set => communication.Username = value;
    }

    internal string Password
    {
        get => communication.Password;
        set => communication.Password = value;
    }

    internal string CertificateFilePassword
    {
        get => communication.CertificateFilePassword;
        set => communication.CertificateFilePassword = value;
    }

    internal string CertificateFile
    {
        get => communication.CertificateFile;
        set => communication.CertificateFile = value;
    }

    internal string CertificatePrivateKeyFile
    {
        get => communication.CertificatePrivateKeyFile;
        set => communication.CertificatePrivateKeyFile = value;
    }
}
