using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using NSubstitute;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class AmazonClientExtensions_GetEndpointAddress
{
    [Fact]
    public async Task Retrieve_endpoint_from_amazon()
    {
        var amazonClient = Substitute.For<IAmazonClient>();
        amazonClient.GetEndpointAddress(Arg.Any<CancellationToken>()).Returns("amazon");
        AwsIotDataPortProperties properties = new(new())
        {
            EndpointAddress = null,
        };

        var endpointAddress = await amazonClient.GetEndpointAddress(properties, TestContext.Current.CancellationToken);

        endpointAddress.Should().Be("amazon");
    }

    [Fact]
    public async Task Use_endpoint_from_configuration_if_it_exists()
    {
        var amazonClient = Substitute.For<IAmazonClient>();
        amazonClient.GetEndpointAddress(Arg.Any<CancellationToken>()).Returns("amazon");
        AwsIotDataPortProperties properties = new(new())
        {
            EndpointAddress = "local",
        };

        var endpointAddress = await amazonClient.GetEndpointAddress(properties, TestContext.Current.CancellationToken);

        endpointAddress.Should().Be("local");
    }
}

public class AmazonClientExtensions_EnsureThings
{
    [Fact]
    public async Task Creates_things()
    {
        var communication = new AwsIotDataPortCommunication
        {
            Nodes =
            [
                new()
                {
                    DesignId = AwsIotNodeDesignId.Thing,
                    Name = "thing1",
                }
            ]
        };
        var amazonClient = Substitute.For<IAmazonClient>();
        amazonClient.GetCertificateArn("1234", Arg.Any<CancellationToken>()).Returns("arn:certificate-123");

        await amazonClient.EnsureThings(communication.Nodes, "1234", TestContext.Current.CancellationToken);

        await amazonClient.Received().CreateThing("thing1", Arg.Any<CancellationToken>());
        await amazonClient.Received().AttachCertificateToThing("thing1", "arn:certificate-123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Attaches_missing_certificate_to_thing()
    {
        var communication = new AwsIotDataPortCommunication
        {
            Nodes =
            [
                new()
                {
                    DesignId = AwsIotNodeDesignId.Thing,
                    Name = "thing1",
                }
            ]
        };
        var amazonClient = Substitute.For<IAmazonClient>();
        amazonClient.GetCertificateArn("1234", Arg.Any<CancellationToken>()).Returns("arn:certificate-123");
        amazonClient.GetThings(Arg.Any<CancellationToken>()).Returns(["thing1"]);

        await amazonClient.EnsureThings(communication.Nodes, "1234", TestContext.Current.CancellationToken);

        await amazonClient.DidNotReceive().CreateThing("thing1", Arg.Any<CancellationToken>());
        await amazonClient.Received().AttachCertificateToThing("thing1", "arn:certificate-123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Does_nothing_if_thing_with_certificate_exists()
    {
        var communication = new AwsIotDataPortCommunication
        {
            Nodes =
            [
                new()
                {
                    DesignId = AwsIotNodeDesignId.Thing,
                    Name = "thing1",
                }
            ]
        };
        var amazonClient = Substitute.For<IAmazonClient>();
        amazonClient.GetCertificateArn("1234", Arg.Any<CancellationToken>()).Returns("arn:certificate-123");
        amazonClient.GetThings(Arg.Any<CancellationToken>()).Returns(["thing1"]);
        amazonClient.GetCertificateThings("arn:certificate-123", Arg.Any<CancellationToken>()).Returns(["thing1"]);

        await amazonClient.EnsureThings(communication.Nodes, "1234", TestContext.Current.CancellationToken);

        await amazonClient.DidNotReceive().CreateThing("thing1", Arg.Any<CancellationToken>());
        await amazonClient.DidNotReceive().AttachCertificateToThing("thing1", "arn:certificate-123", Arg.Any<CancellationToken>());
    }
}
