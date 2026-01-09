using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.IoT;
using Amazon.IoT.Model;

namespace ViciOne.Suite.DataPort;

internal class AmazonClient : IAmazonClient, IDisposable
{
    private readonly AmazonIoTClient _amazonIoTClient;

    public AmazonClient(AwsIotDataPortCommunication communication)
    {
        AwsIotDataPortProperties properties = new(communication);
        _amazonIoTClient = new AmazonIoTClient(properties.AccessKey, properties.SecretAccessKey, properties.Region);
    }

    public async Task<string> GetEndpointAddress(CancellationToken cancellationToken)
    {
        try
        {
            var describeEndpointResponse = await _amazonIoTClient.DescribeEndpointAsync(new DescribeEndpointRequest { EndpointType = "iot:Data-ATS" }, cancellationToken);

            if (describeEndpointResponse.HttpStatusCode != System.Net.HttpStatusCode.OK)
                throw new InvalidOperationException($"Failed to describe endpoint. Status code: {describeEndpointResponse.HttpStatusCode}");

            return describeEndpointResponse.EndpointAddress;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("Failed to get endpoint address.", exception);
        }
    }

    public async Task<string> GetCertificateArn(string certificateId, CancellationToken cancellationToken)
    {
        try
        {
            var describeCertificateResponse = await _amazonIoTClient.DescribeCertificateAsync(certificateId, cancellationToken);

            if (describeCertificateResponse.HttpStatusCode != System.Net.HttpStatusCode.OK)
                throw new InvalidOperationException($"Failed to describe certificate. Status code: {describeCertificateResponse.HttpStatusCode}");

            return describeCertificateResponse.CertificateDescription.CertificateArn;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to get ARN for certificate '{certificateId}'.", exception);
        }
    }

    public async Task<IReadOnlyCollection<string>> GetThings(CancellationToken cancellationToken)
    {
        try
        {
            var listThingsResponse = await _amazonIoTClient.ListThingsAsync(cancellationToken);

            if (listThingsResponse.HttpStatusCode != System.Net.HttpStatusCode.OK)
                throw new InvalidOperationException($"Failed to list things. Status code: {listThingsResponse.HttpStatusCode}");

            return [.. listThingsResponse.Things.Select(t => t.ThingName)];
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("Failed to list things.", exception);
        }
    }

    public async Task CreateThing(string thingName, CancellationToken cancellationToken)
    {
        try
        {
            var createThingResponse = await _amazonIoTClient.CreateThingAsync(new CreateThingRequest { ThingName = thingName }, cancellationToken);

            if (createThingResponse.HttpStatusCode != System.Net.HttpStatusCode.OK)
                throw new InvalidOperationException($"Failed to create thing '{thingName}'. Status code: {createThingResponse.HttpStatusCode}");
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to create thing '{thingName}'.", exception);
        }
    }

    public async Task<IReadOnlyCollection<string>> GetCertificateThings(string principal, CancellationToken cancellationToken)
    {
        try
        {
            var listPrincipalThingsResponse = await _amazonIoTClient.ListPrincipalThingsAsync(principal, cancellationToken);

            if (listPrincipalThingsResponse.HttpStatusCode != System.Net.HttpStatusCode.OK)
                throw new InvalidOperationException($"Failed to list principal things. Status code: {listPrincipalThingsResponse.HttpStatusCode}");

            return listPrincipalThingsResponse.Things;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to list things for principal '{principal}'.", exception);
        }
    }

    public async Task AttachCertificateToThing(string thingName, string principal, CancellationToken cancellationToken)
    {
        try
        {
            var attachThingPrincipalResponse = await _amazonIoTClient.AttachThingPrincipalAsync(thingName, principal, cancellationToken);

            if (attachThingPrincipalResponse.HttpStatusCode != System.Net.HttpStatusCode.OK)
                throw new InvalidOperationException($"Failed to attach thing '{thingName}' to principal '{principal}'. Status code: {attachThingPrincipalResponse.HttpStatusCode}");
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to attach thing '{thingName}' to principal '{principal}'.", exception);
        }
    }

    public void Dispose()
        => _amazonIoTClient.Dispose();
}
