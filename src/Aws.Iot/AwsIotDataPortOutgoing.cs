using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.ManagedEngine.ExternalCommunication;

namespace ViciOne.Suite.DataPort;

public sealed class AwsIotDataPortOutgoing(AwsIotDataPortCommunication communication, ILoggerFactory loggerFactory)
    : IExternalOutgoingCommunication<AwsIotDataPortCommunication>, IDisposable
{
    private readonly AwsIotDataPortProperties _properties = new(communication);
    private readonly Node[] _thingNodes = [.. communication.Nodes.Where(n => n.DesignId == AwsIotNodeDesignId.Thing)];

    private MqttDataPortOutgoing? _mqttOutgoing;

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (_mqttOutgoing is null)
        {
            using AmazonClient amazonClient = new(communication);
            var endpointAddress = await amazonClient.GetEndpointAddress(_properties, cancellationToken);
            await amazonClient.EnsureThings(_thingNodes, _properties.CertificateId, cancellationToken);

            _mqttOutgoing = new(communication.ToMqtt(endpointAddress, false), loggerFactory, loggerFactory.CreateLogger<MqttDataPortOutgoing>());
            await _mqttOutgoing.ConnectAsync(cancellationToken);
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        if (_mqttOutgoing is null)
            return;

        await _mqttOutgoing.DisconnectAsync(cancellationToken);
        _mqttOutgoing = null;
    }

    public void Dispose()
        => _mqttOutgoing?.Dispose();

    public async Task SendAsync(ulong engineCycle, IReadOnlyCollection<ExternalValue> values, CancellationToken cancellationToken)
    {
        if (_mqttOutgoing is null)
            throw new InvalidOperationException("The communication is not connected.");

        await _mqttOutgoing.SendAsync(engineCycle, values, cancellationToken);
    }
}
