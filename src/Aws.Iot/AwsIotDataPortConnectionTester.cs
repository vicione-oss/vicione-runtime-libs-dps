using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.Suite.DataPort;

public sealed class AwsIotDataPortConnectionTester : IDataPortConnectionTester<AwsIotDataPortCommunication>
{
    public static async Task TestConnectionAsync(AwsIotDataPortCommunication communication)
    {
        AwsIotDataPortProperties properties = new(communication);

        try
        {
            using var amazonClient = new AmazonClient(communication);
            var endpointAddress = await amazonClient.GetEndpointAddress(properties, CancellationToken.None);

            await amazonClient.GetCertificateArn(properties.CertificateId, CancellationToken.None);
            await MqttConnectionTester.TestConnectionAsync(communication.ToMqtt(endpointAddress, false));
        }
        catch (Exception ex)
        {
            throw new DataPortConnectionFailedException("Failed to connect to AWS IoT.", ex);
        }
    }
}
