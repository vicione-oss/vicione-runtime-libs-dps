using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.Suite.DataPort;

internal static class AwsToMqttConverter
{
    internal static MqttDataPortCommunication ToMqtt(this AwsIotDataPortCommunication amazonCommunication, string endpointAddress, bool incoming)
        => new()
        {
            Nodes = TranslateNodes(amazonCommunication, incoming),
            Pooling = false,
            ProtocolVersion = amazonCommunication.ProtocolVersion,
            Protocol = (byte)MqttProtocol.Tcp,
            Host = endpointAddress,
            Port = amazonCommunication.EndpointPort,
            SslProtocol = amazonCommunication.SslProtocol,
            CertificateFile = amazonCommunication.CertificateFile,
            CertificatePrivateKeyFile = amazonCommunication.PrivateKeyFile,
            ClientId = amazonCommunication.ClientId,
            QualityOfService = amazonCommunication.QualityOfService,
            CleanSession = amazonCommunication.CleanSession,
            SessionExpiryInterval = amazonCommunication.SessionExpiryInterval,
            WillTopic = amazonCommunication.WillTopic,
            WillMessage = amazonCommunication.WillMessage,
            WillRetain = amazonCommunication.WillRetain,
            DefaultSerializer = 1, // JSON,
            DisableCertificateValidation = amazonCommunication.DisableCertificateValidation,
            MaxPendingMessages = amazonCommunication.MaxPendingMessages,
        };

    private static List<Node> TranslateNodes(AwsIotDataPortCommunication amazonCommunication, bool incoming)
    {
        var linkedNodes = LinkedNodeFactory.Create(amazonCommunication.Nodes);
        var brokerNodes = GetBrokerNodes(linkedNodes);
        var thingNodes = GetThingNodes(linkedNodes);
        var mqttNodes = brokerNodes.Where(n => n.DesignId == AwsIotNodeDesignId.Topic).Select(BrokerNodeToMqttNode).ToList();
        Dictionary<Guid, Node> stateNodes = [];

        foreach (var node in thingNodes)
        {
            switch (node.DesignId)
            {
                case AwsIotNodeDesignId.JsonStructure:
                    mqttNodes.Add(JsonStructureNodeToMqttNode(node));
                    break;
                case AwsIotNodeDesignId.ClassicDeviceShadow:
                    mqttNodes.Add(ClassicShadowNodeToMqttNode(node, incoming));
                    break;
                case AwsIotNodeDesignId.NamedDeviceShadow:
                    mqttNodes.Add(NamedShadowNodeToMqttNode(node, incoming));
                    break;
                case AwsIotNodeDesignId.Reported:
                    mqttNodes.AddRange(ReportedNodeToMqttNode(node, stateNodes));
                    break;
                case AwsIotNodeDesignId.Desired:
                    mqttNodes.AddRange(DesiredNodeToMqttNode(node, stateNodes));
                    break;
            }
        }

        return mqttNodes;
    }

    private static LinkedNode[] GetBrokerNodes(List<LinkedNode> linkedNodes)
    {
        var brokerNode = linkedNodes.FirstOrDefault(n => n.DesignId == AwsIotNodeDesignId.Broker);
        var brokerNodes = brokerNode?.GetAllChildren().ToArray() ?? [];
        return brokerNodes;
    }

    private static LinkedNode[] GetThingNodes(List<LinkedNode> linkedNodes)
    {
        var thingsNode = linkedNodes.FirstOrDefault(n => n.DesignId == AwsIotNodeDesignId.Things);
        var thingNodes = thingsNode?.GetAllChildren().ToArray() ?? [];
        return thingNodes;
    }

    private static Node BrokerNodeToMqttNode(LinkedNode node)
        => new()
        {
            Id = node.Id,
            Name = node.Name,
            ParentId = node.Parent?.DesignId == AwsIotNodeDesignId.Broker ? null : node.ParentId,
            DesignId = MqttNodeDesignId.Topic,
            ValueType = node.ValueType,
            AffectedChannels = node.AffectedChannels,
            TransferredChannels = node.TransferredChannels,
            Properties = node.Properties,
        };

    private static Node ClassicShadowNodeToMqttNode(LinkedNode node, bool incoming)
        => new()
        {
            Id = node.Id,
            Name = $"$aws/things/{node.Parent?.Name}/shadow/update{(incoming ? "/accepted" : "")}",
            ParentId = null,
            DesignId = MqttNodeDesignId.Topic,
            AffectedChannels = node.AffectedChannels,
            TransferredChannels = node.TransferredChannels,
        };

    private static Node NamedShadowNodeToMqttNode(LinkedNode node, bool incoming)
        => new()
        {
            Id = node.Id,
            Name = $"$aws/things/{node.Parent?.Name}/shadow/name/{node.Name}/update{(incoming ? "/accepted" : "")}",
            ParentId = null,
            DesignId = MqttNodeDesignId.Topic,
            AffectedChannels = node.AffectedChannels,
            TransferredChannels = node.TransferredChannels,
        };

    private static IEnumerable<Node> ReportedNodeToMqttNode(LinkedNode node, Dictionary<Guid, Node> stateNodes)
    {
        if (node.ParentId is null)
            throw new InvalidOperationException("The reported node must have a parent.");
        if (!stateNodes.TryGetValue(node.ParentId.Value, out var stateNode))
        {
            stateNode = new()
            {
                Id = Guid.NewGuid(),
                Name = "state",
                ParentId = node.ParentId,
                DesignId = MqttNodeDesignId.Topic,
            };
            stateNodes[node.ParentId.Value] = stateNode;
            yield return stateNode;
        }

        yield return new()
        {
            Id = node.Id,
            Name = "reported",
            ParentId = stateNode.Id,
            DesignId = MqttNodeDesignId.Topic,
            ValueType = node.ValueType,
            AffectedChannels = node.AffectedChannels,
            TransferredChannels = node.TransferredChannels,
        };
    }

    private static IEnumerable<Node> DesiredNodeToMqttNode(LinkedNode node, Dictionary<Guid, Node> stateNodes)
    {
        if (node.ParentId is null)
            throw new InvalidOperationException("The desired node must have a parent.");
        if (!stateNodes.TryGetValue(node.ParentId.Value, out var stateNode))
        {
            stateNode = new()
            {
                Id = Guid.NewGuid(),
                Name = "state",
                ParentId = node.ParentId,
                DesignId = MqttNodeDesignId.Topic,
            };
            stateNodes[node.ParentId.Value] = stateNode;
            yield return stateNode;
        }

        yield return new()
        {
            Id = node.Id,
            Name = "desired",
            ParentId = stateNode.Id,
            DesignId = MqttNodeDesignId.Topic,
            ValueType = node.ValueType,
            AffectedChannels = node.AffectedChannels,
            TransferredChannels = node.TransferredChannels,
        };
    }

    private static Node JsonStructureNodeToMqttNode(LinkedNode node)
        => new()
        {
            Id = node.Id,
            Name = node.Name,
            ParentId = node.Parent?.DesignId == AwsIotNodeDesignId.Things ? null : node.ParentId,
            DesignId = MqttNodeDesignId.Topic,
            ValueType = node.ValueType,
            AffectedChannels = node.AffectedChannels,
            TransferredChannels = node.TransferredChannels,
        };
}
