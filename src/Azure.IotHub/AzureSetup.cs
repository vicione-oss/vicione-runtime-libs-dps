using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using AuthenticationMethodFactory = Microsoft.Azure.Devices.Client.AuthenticationMethodFactory;

namespace ViciOne.Suite.DataPort;

internal static class AzureSetup
{
    internal static async Task<IDictionary<Guid, VirtualDeviceClient>> CreateDeviceClients(AzureIotHubDataPortCommunication communication, IDeviceClientPool deviceClientPool, CancellationToken cancellationToken)
    {
        AzureIotHubDataPortProperties properties = new(communication);
        var registrySharedAccessSignature = new SharedAccessSignatureBuilder()
        {
            Target = properties.Hostname,
            KeyName = properties.SharedAccessSignatureKeyName,
            Key = properties.SharedAccessSignatureKey,
        }.ToSignature();
        using var registryManager = RegistryManager.Create(properties.Hostname, new AzureSasCredential(registrySharedAccessSignature));
        Dictionary<Guid, VirtualDeviceClient> deviceClients = [];

        foreach (var node in communication.Nodes.Where(n => n.DesignId == AzureIotHubNodeDesignId.Device))
        {
            var deviceId = node.Name;
            var device = await registryManager.GetDeviceAsync(deviceId, cancellationToken).ConfigureAwait(false);
            device ??= await registryManager.AddDeviceAsync(new(deviceId), cancellationToken).ConfigureAwait(false);

            var deviceClient = await deviceClientPool.Register(properties.Hostname, deviceId, AuthenticationMethodFactory.CreateAuthenticationWithRegistrySymmetricKey(deviceId, device.Authentication.SymmetricKey.PrimaryKey), cancellationToken);
            deviceClients.Add(node.Id, deviceClient);
        }

        return deviceClients;
    }
}
