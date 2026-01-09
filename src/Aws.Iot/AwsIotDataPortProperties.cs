using Amazon;

namespace ViciOne.Suite.DataPort;

internal sealed class AwsIotDataPortProperties(AwsIotDataPortCommunication communication)
{
    internal string AccessKey
    {
        get => communication.AccessKey;
        set => communication.AccessKey = value;
    }

    internal string SecretAccessKey
    {
        get => communication.SecretAccessKey;
        set => communication.SecretAccessKey = value;
    }

    internal ushort EndpointPort
    {
        get => communication.EndpointPort;
        set => communication.EndpointPort = value;
    }

    internal RegionEndpoint Region
    {
        get => RegionEndpoint.GetBySystemName(communication.Region);
        set => communication.Region = value.SystemName;
    }

    internal string CertificateId
    {
        get => communication.CertificateId;
        set => communication.CertificateId = value;
    }

    internal string? EndpointAddress
    {
        get => communication.EndpointAddress;
        set => communication.EndpointAddress = value;
    }
}
