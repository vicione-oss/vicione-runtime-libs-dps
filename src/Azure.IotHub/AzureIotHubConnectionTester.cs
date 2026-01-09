using System;
using System.Threading.Tasks;
using Azure;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;

namespace ViciOne.Suite.DataPort;

public sealed class AzureIotHubConnectionTester : IDataPortConnectionTester<AzureIotHubDataPortCommunication>
{
    public static async Task TestConnectionAsync(AzureIotHubDataPortCommunication communication)
    {
        AzureIotHubDataPortProperties properties = new(communication);
        try
        {
            var registrySharedAccessSignature = new SharedAccessSignatureBuilder()
            {
                Target = properties.Hostname,
                KeyName = properties.SharedAccessSignatureKeyName,
                Key = properties.SharedAccessSignatureKey,
            }.ToSignature();

            using var registryManager = RegistryManager.Create(properties.Hostname, new AzureSasCredential(registrySharedAccessSignature));

            var queryRequest = registryManager.CreateQuery("select * from devices");
            _ = await queryRequest.GetNextAsJsonAsync();
        }
        catch (Exception ex)
        {
            throw new DataPortConnectionFailedException("Failed to connect to Azure.IotHub.", ex);
        }
    }
}
