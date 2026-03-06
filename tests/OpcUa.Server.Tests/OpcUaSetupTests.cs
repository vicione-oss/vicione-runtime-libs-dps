using AwesomeAssertions;
using Opc.Ua;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class OpcUaSetup_CreateConfiguration_TrustedPeerCertificates
{
    [Fact]
    public void TrustedPeerCertificates_is_configured_when_TrustedCertificatesStoreType_is_set()
    {
        OpcUaServerDataPortCommunication communication = new()
        {
            TrustedCertificatesStoreType = 2, // Directory
            TrustedCertificatesStorePath = "/trusted/peer/certs",
        };
        OpcUaServerDataPortProperties properties = new(communication);

        var configuration = OpcUaSetup.CreateConfiguration(properties);

        configuration.SecurityConfiguration.TrustedPeerCertificates.Should().NotBeNull();
        configuration.SecurityConfiguration.TrustedPeerCertificates.StoreType.Should().Be(CertificateStoreType.Directory);
        configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath.Should().Be("/trusted/peer/certs");
    }

    [Fact]
    public void TrustedIssuerCertificates_is_not_set_by_TrustedCertificatesStoreType()
    {
        OpcUaServerDataPortCommunication communication = new()
        {
            TrustedCertificatesStoreType = 2, // Directory
            TrustedCertificatesStorePath = "/trusted/peer/certs",
            TrustedIssuerCertificatesStoreType = 0, // null
        };
        OpcUaServerDataPortProperties properties = new(communication);

        var configuration = OpcUaSetup.CreateConfiguration(properties);

        configuration.SecurityConfiguration.TrustedIssuerCertificates.StorePath.Should().BeNullOrEmpty();
    }
}

public class OpcUaSetup_CreateConfiguration_TrustedIssuerCertificates
{
    [Fact]
    public void TrustedIssuerCertificates_is_configured_when_TrustedIssuerCertificatesStoreType_is_set()
    {
        OpcUaServerDataPortCommunication communication = new()
        {
            TrustedIssuerCertificatesStoreType = 1, // X509Store
            TrustedIssuerCertificatesStorePath = "/trusted/issuer/certs",
        };
        OpcUaServerDataPortProperties properties = new(communication);

        var configuration = OpcUaSetup.CreateConfiguration(properties);

        configuration.SecurityConfiguration.TrustedIssuerCertificates.Should().NotBeNull();
        configuration.SecurityConfiguration.TrustedIssuerCertificates.StoreType.Should().Be(CertificateStoreType.X509Store);
        configuration.SecurityConfiguration.TrustedIssuerCertificates.StorePath.Should().Be("/trusted/issuer/certs");
    }

    [Fact]
    public void Both_TrustedPeerCertificates_and_TrustedIssuerCertificates_are_configured_independently()
    {
        OpcUaServerDataPortCommunication communication = new()
        {
            TrustedCertificatesStoreType = 2, // Directory
            TrustedCertificatesStorePath = "/trusted/peer/certs",
            TrustedIssuerCertificatesStoreType = 1, // X509Store
            TrustedIssuerCertificatesStorePath = "/trusted/issuer/certs",
        };
        OpcUaServerDataPortProperties properties = new(communication);

        var configuration = OpcUaSetup.CreateConfiguration(properties);

        configuration.SecurityConfiguration.TrustedPeerCertificates.StoreType.Should().Be(CertificateStoreType.Directory);
        configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath.Should().Be("/trusted/peer/certs");
        configuration.SecurityConfiguration.TrustedIssuerCertificates.StoreType.Should().Be(CertificateStoreType.X509Store);
        configuration.SecurityConfiguration.TrustedIssuerCertificates.StorePath.Should().Be("/trusted/issuer/certs");
    }
}
