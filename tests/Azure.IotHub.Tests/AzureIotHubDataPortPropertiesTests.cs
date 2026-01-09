using AwesomeAssertions;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class AzureIotHubDataPortProperties_Hostname
{
    [Theory]
    [MemberData(nameof(Conversion))]
    public void Getter_returns_value(string hostname)
    {
        AzureIotHubDataPortProperties properties = new(new() { Hostname = hostname, });

        properties.Hostname.Should().Be(hostname);
    }

    [Theory]
    [MemberData(nameof(Conversion))]
    public void Setter_sets_value(string hostname)
    {
        AzureIotHubDataPortCommunication communication = new();

        _ = new AzureIotHubDataPortProperties(communication)
        {
            Hostname = hostname,
        };

        communication.Hostname.Should().Be(hostname);
    }

    public static TheoryData<string> Conversion() => new()
    {
        { string.Empty },
        { "azure.ms.com" },
    };
}

public class AzureIotHubDataPortProperties_SharedAccessSignatureKey
{
    [Theory]
    [MemberData(nameof(Conversion))]
    public void Getter_returns_value(string sharedAccessSignatureKey)
    {
        AzureIotHubDataPortProperties properties = new(new() { SharedAccessSignatureKey = sharedAccessSignatureKey, });

        properties.SharedAccessSignatureKey.Should().Be(sharedAccessSignatureKey);
    }

    [Theory]
    [MemberData(nameof(Conversion))]
    public void Setter_sets_value(string sharedAccessSignatureKey)
    {
        AzureIotHubDataPortCommunication communication = new();

        _ = new AzureIotHubDataPortProperties(communication)
        {
            SharedAccessSignatureKey = sharedAccessSignatureKey,
        };

        communication.SharedAccessSignatureKey.Should().Be(sharedAccessSignatureKey);
    }

    public static TheoryData<string> Conversion() => new()
    {
        { string.Empty },
        { "Key:12:34" },
    };
}

public class AzureIotHubDataPortProperties_SharedAccessSignatureKeyName
{
    [Theory]
    [MemberData(nameof(Conversion))]
    public void Getter_returns_value(string sharedAccessSignatureKeyName)
    {
        AzureIotHubDataPortProperties properties = new(new() { SharedAccessSignatureKeyName = sharedAccessSignatureKeyName, });

        properties.SharedAccessSignatureKeyName.Should().Be(sharedAccessSignatureKeyName);
    }

    [Theory]
    [MemberData(nameof(Conversion))]
    public void Setter_sets_value(string sharedAccessSignatureKeyName)
    {
        AzureIotHubDataPortCommunication communication = new();

        _ = new AzureIotHubDataPortProperties(communication)
        {
            SharedAccessSignatureKeyName = sharedAccessSignatureKeyName,
        };

        communication.SharedAccessSignatureKeyName.Should().Be(sharedAccessSignatureKeyName);
    }

    public static TheoryData<string> Conversion() => new()
    {
        { string.Empty },
        { "myKey" },
    };
}
