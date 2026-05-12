using AwesomeAssertions;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class HttpServerPropertiesTests_BaseAddress
{
    [Theory]
    [MemberData(nameof(Conversion))]
    public void Getter_returns_value(string baseAddress)
    {
        HttpServerDataPortProperties properties = new(new() { BaseAddress = baseAddress, });

        properties.BaseAddress.Should().Be(baseAddress);
    }

    [Theory]
    [MemberData(nameof(Conversion))]
    public void Setter_sets_value(string baseAddress)
    {
        HttpServerDataPortCommunication communication = new();

        _ = new HttpServerDataPortProperties(communication)
        {
            BaseAddress = baseAddress,
        };

        communication.BaseAddress.Should().Be(baseAddress);
    }

    public static TheoryData<string> Conversion() => new()
    {
        { string.Empty },
        { "api.sample.io" },
    };
}

public class HttpServerPropertiesTests_AuthenticationType
{
    [Theory]
    [MemberData(nameof(Conversion))]
    public void Getter_returns_value(byte authenticationType)
    {
        HttpServerDataPortProperties properties = new(new() { AuthenticationType = authenticationType, });

        properties.AuthenticationType.Should().Be((AuthenticationType)authenticationType);
    }

    [Theory]
    [MemberData(nameof(Conversion))]
    public void Setter_sets_value(byte authenticationType)
    {
        HttpServerDataPortCommunication communication = new();

        _ = new HttpServerDataPortProperties(communication)
        {
            AuthenticationType = (AuthenticationType)authenticationType,
        };

        communication.AuthenticationType.Should().Be(authenticationType);
    }

    public static TheoryData<byte> Conversion() => new()
    {
        { (byte)AuthenticationType.Basic },
        { (byte)AuthenticationType.None },
    };
}

public class HttpServerPropertiesTests_BearerToken
{
    [Theory]
    [MemberData(nameof(Conversion))]
    public void Getter_returns_value(string bearerToken)
    {
        HttpServerDataPortProperties properties = new(new() { BearerToken = bearerToken, });

        properties.BearerToken.Should().Be(bearerToken);
    }

    [Theory]
    [MemberData(nameof(Conversion))]
    public void Setter_sets_value(string bearerToken)
    {
        HttpServerDataPortCommunication communication = new();

        _ = new HttpServerDataPortProperties(communication)
        {
            BearerToken = bearerToken,
        };

        communication.BearerToken.Should().Be(bearerToken);
    }

    public static TheoryData<string> Conversion() => new()
    {
        { string.Empty },
        { "test" },
    };
}

public class HttpServerPropertiesTests_Username
{
    [Theory]
    [MemberData(nameof(Conversion))]
    public void Getter_returns_value(string username)
    {
        HttpServerDataPortProperties properties = new(new() { Username = username, });

        properties.Username.Should().Be(username);
    }

    [Theory]
    [MemberData(nameof(Conversion))]
    public void Setter_sets_value(string username)
    {
        HttpServerDataPortCommunication communication = new();

        _ = new HttpServerDataPortProperties(communication)
        {
            Username = username,
        };

        communication.Username.Should().Be(username);
    }

    public static TheoryData<string> Conversion() => new()
    {
        { string.Empty },
        { "test" },
    };
}

public class HttpServerPropertiesTests_Password
{
    [Theory]
    [MemberData(nameof(Conversion))]
    public void Getter_returns_value(string password)
    {
        HttpServerDataPortProperties properties = new(new() { Password = password, });

        properties.Password.Should().Be(password);
    }

    [Theory]
    [MemberData(nameof(Conversion))]
    public void Setter_sets_value(string password)
    {
        HttpServerDataPortCommunication communication = new();

        _ = new HttpServerDataPortProperties(communication)
        {
            Password = password,
        };

        communication.Password.Should().Be(password);
    }

    public static TheoryData<string> Conversion() => new()
    {
        { string.Empty },
        { "test" },
    };
}

public class HttpServerPropertiesTests_CertificateFilePassword
{
    [Theory]
    [MemberData(nameof(Conversion))]
    public void Getter_returns_value(string certificateFilePassword)
    {
        HttpServerDataPortProperties properties = new(new() { CertificateFilePassword = certificateFilePassword, });

        properties.CertificateFilePassword.Should().Be(certificateFilePassword);
    }

    [Theory]
    [MemberData(nameof(Conversion))]
    public void Setter_sets_value(string certificateFilePassword)
    {
        HttpServerDataPortCommunication communication = new();

        _ = new HttpServerDataPortProperties(communication)
        {
            CertificateFilePassword = certificateFilePassword,
        };

        communication.CertificateFilePassword.Should().Be(certificateFilePassword);
    }

    public static TheoryData<string> Conversion() => new()
    {
        { string.Empty },
        { "test" },
    };
}

public class HttpServerPropertiesTests_CertificateFile
{
    [Theory]
    [MemberData(nameof(Conversion))]
    public void Getter_returns_value(string certificateFile)
    {
        HttpServerDataPortProperties properties = new(new() { CertificateFile = certificateFile, });

        properties.CertificateFile.Should().Be(certificateFile);
    }

    [Theory]
    [MemberData(nameof(Conversion))]
    public void Setter_sets_value(string certificateFile)
    {
        HttpServerDataPortCommunication communication = new();

        _ = new HttpServerDataPortProperties(communication)
        {
            CertificateFile = certificateFile,
        };

        communication.CertificateFile.Should().Be(certificateFile);
    }

    public static TheoryData<string> Conversion() => new()
    {
        { string.Empty },
        { "test" },
    };
}

public class HttpServerPropertiesTests_CertificatePrivateKeyFile
{
    [Theory]
    [MemberData(nameof(Conversion))]
    public void Getter_returns_value(string certificatePrivateKeyFile)
    {
        HttpServerDataPortProperties properties = new(new() { CertificatePrivateKeyFile = certificatePrivateKeyFile, });

        properties.CertificatePrivateKeyFile.Should().Be(certificatePrivateKeyFile);
    }

    [Theory]
    [MemberData(nameof(Conversion))]
    public void Setter_sets_value(string certificatePrivateKeyFile)
    {
        HttpServerDataPortCommunication communication = new();

        _ = new HttpServerDataPortProperties(communication)
        {
            CertificatePrivateKeyFile = certificatePrivateKeyFile,
        };

        communication.CertificatePrivateKeyFile.Should().Be(certificatePrivateKeyFile);
    }

    public static TheoryData<string> Conversion() => new()
    {
        { string.Empty },
        { "test" },
    };
}
