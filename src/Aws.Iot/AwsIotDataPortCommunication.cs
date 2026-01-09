using ViciOne.ManagedEngine.Communication;

namespace ViciOne.Suite.DataPort;

[Communication("awsiotdataport")]
public sealed record AwsIotDataPortCommunication : DataPortCommunication
{
    public string AccessKey { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string? EndpointAddress { get; set; }
    public ushort EndpointPort { get; set; } = 8883;
    /// <summary>
    /// Find out more about the correct region here <see href="https://docs.aws.amazon.com/en_us/general/latest/gr/iot_device_management.html#iot_device_management_region_data"/>.
    /// </summary>
    public string Region { get; set; } = "eu-central-1";
    public string CertificateId { get; set; } = string.Empty;
    public string CertificateFile { get; set; } = string.Empty;
    public string PrivateKeyFile { get; set; } = string.Empty;
    public byte QualityOfService { get; set; }
    public byte ProtocolVersion { get; set; }
    public byte SslProtocol { get; set; } = 1;
    public string? ClientId { get; set; }
    public bool? DisableCertificateValidation { get; set; }
    public bool? CleanSession { get; set; }
    public uint? SessionExpiryInterval { get; set; }
    public string? WillTopic { get; set; }
    public string? WillMessage { get; set; }
    public bool? WillRetain { get; set; }
    public int? MaxPendingMessages { get; set; }
}
