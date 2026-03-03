using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Opc.Ua;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class OpcUaServer_VerifyPassword_Logging
{
    private static OpcUaServer CreateServer(TestLogger<IOpcUaServer> logger, string user = "admin", string password = "secret")
    {
        OpcUaServerDataPortCommunication communication = new()
        {
            User = user,
            Password = password,
            UserAuthenticationType = 1,
            Nodes = [],
        };

        return new OpcUaServer(communication, logger);
    }

    private static UserNameIdentityToken CreateToken(string user, string password) => new()
    {
        UserName = user,
        DecryptedPassword = password,
    };

    [Fact]
    public void Logs_warning_on_failed_login()
    {
        TestLogger<IOpcUaServer> logger = new();
        using var server = CreateServer(logger);
        var token = CreateToken("admin", "wrong");

        var act = () => server.VerifyPassword(token, "session-1");

        act.Should().Throw<ServiceResultException>();
        logger.Entries.Should().ContainSingle(e =>
            e.LogLevel == LogLevel.Warning &&
            e.Message.Contains("Failed authentication attempt") &&
            e.Message.Contains("admin") &&
            e.Message.Contains("session-1"));
    }

    [Fact]
    public void Logs_information_on_successful_login()
    {
        TestLogger<IOpcUaServer> logger = new();
        using var server = CreateServer(logger);
        var token = CreateToken("admin", "secret");

        server.VerifyPassword(token, "session-2");

        logger.Entries.Should().ContainSingle(e =>
            e.LogLevel == LogLevel.Information &&
            e.Message.Contains("authenticated successfully") &&
            e.Message.Contains("admin") &&
            e.Message.Contains("session-2"));
    }
}

public class OpcUaServer_HandleAnonymousAccess_Logging
{
    [Fact]
    public void Logs_warning_on_anonymous_access()
    {
        TestLogger<IOpcUaServer> logger = new();
        OpcUaServerDataPortCommunication communication = new()
        {
            UserAuthenticationType = 0,
            Nodes = [],
        };
        using OpcUaServer server = new(communication, logger);

        server.HandleAnonymousAccess("session-3");

        logger.Entries.Should().ContainSingle(e =>
            e.LogLevel == LogLevel.Warning &&
            e.Message.Contains("Anonymous access used") &&
            e.Message.Contains("session-3"));
    }
}

public class OpcUaServer_LogInsecureConfigurationWarnings
{
    [Fact]
    public void Logs_warning_when_anonymous_access_is_configured()
    {
        TestLogger<IOpcUaServer> logger = new();
        OpcUaServerDataPortCommunication communication = new()
        {
            UserAuthenticationType = 0,
            Nodes = [],
        };
        using OpcUaServer server = new(communication, logger);

        server.LogInsecureConfigurationWarnings();

        logger.Entries.Should().ContainSingle(e =>
            e.LogLevel == LogLevel.Warning &&
            e.Message.Contains("anonymous access enabled") &&
            e.Message.Contains("IEC 62443"));
    }

    [Fact]
    public void Logs_warning_when_auto_accept_untrusted_certificates_is_enabled()
    {
        TestLogger<IOpcUaServer> logger = new();
        OpcUaServerDataPortCommunication communication = new()
        {
            UserAuthenticationType = 1,
            AutoAcceptUntrustedCertificates = true,
            Nodes = [],
        };
        using OpcUaServer server = new(communication, logger);

        server.LogInsecureConfigurationWarnings();

        logger.Entries.Should().ContainSingle(e =>
            e.LogLevel == LogLevel.Warning &&
            e.Message.Contains("AutoAcceptUntrustedCertificates=true") &&
            e.Message.Contains("man-in-the-middle"));
    }
}
