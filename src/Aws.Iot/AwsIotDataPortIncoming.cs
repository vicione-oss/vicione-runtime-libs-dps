using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.ManagedEngine.ExternalCommunication;

namespace ViciOne.Suite.DataPort;

public sealed class AwsIotDataPortIncoming(AwsIotDataPortCommunication communication, ILoggerFactory loggerFactory, AssemblyLoadContext context)
    : IExternalIncomingCommunication<AwsIotDataPortCommunication>, IDisposable
{
    private readonly AwsIotDataPortProperties _properties = new(communication);
    private readonly Node[] _thingNodes = [.. communication.Nodes.Where(n => n.DesignId == AwsIotNodeDesignId.Thing)];

    private MqttDataPortIncoming? _mqttIncoming;

    public event Action<IReadOnlyCollection<ExternalValue>>? Received;

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (_mqttIncoming is null)
        {
            using AmazonClient amazonClient = new(communication);
            await amazonClient.EnsureThings(_thingNodes, _properties.CertificateId, cancellationToken);
            var endpointAddress = await amazonClient.GetEndpointAddress(_properties, cancellationToken);

            _mqttIncoming = new(communication.ToMqtt(endpointAddress, true), loggerFactory, loggerFactory.CreateLogger<MqttDataPortIncoming>(), context);
            _mqttIncoming.Received += PassthroughValues;
            await _mqttIncoming.ConnectAsync(cancellationToken);
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        if (_mqttIncoming is null)
            return;

        await _mqttIncoming.DisconnectAsync(cancellationToken);
        _mqttIncoming.Received -= PassthroughValues;
        _mqttIncoming = null;
    }

    public void Dispose()
        => _mqttIncoming?.Dispose();

    private void PassthroughValues(IReadOnlyCollection<ExternalValue> values)
        => Received?.Invoke(values);
}
