using System;
using AwesomeAssertions;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class AwsToMqttConverter_ToMqtt
{
    [Fact]
    public void Should_translate_broker_nodes()
    {
        Node brokerNode = new()
        {
            Id = Guid.NewGuid(),
            DesignId = AwsIotNodeDesignId.Broker,
            Name = "Broker",
        };
        var valueOneNodeId = Guid.NewGuid();
        Node valueOneNode = new()
        {
            Id = valueOneNodeId,
            ParentId = brokerNode.Id,
            Name = "value1",
            DesignId = AwsIotNodeDesignId.Topic,
            TransferredChannels = { valueOneNodeId.ToString(), },
            AffectedChannels = { valueOneNodeId.ToString(), },
            ValueType = typeof(int),
            Properties = new()
            {
                { MqttNodeProperties.Retain, new() { Value = true } },
                { MqttNodeProperties.Serializer, new() { Value = (byte)2 } },
            },
        };
        var groupNodeId = Guid.NewGuid();
        var valueTwoNodeId = Guid.NewGuid();
        Node valueTwoNode = new()
        {
            Id = valueTwoNodeId,
            ParentId = groupNodeId,
            Name = "value2",
            DesignId = AwsIotNodeDesignId.Topic,
            TransferredChannels = { valueTwoNodeId.ToString(), },
            AffectedChannels = { valueTwoNodeId.ToString(), },
            ValueType = typeof(int),
        };
        Node groupNode = new()
        {
            Id = groupNodeId,
            ParentId = brokerNode.Id,
            Name = "group",
            DesignId = AwsIotNodeDesignId.Topic,
            TransferredChannels = [valueTwoNodeId.ToString(),],
        };
        AwsIotDataPortCommunication amazonCommunication = new()
        {
            Nodes = [brokerNode, valueOneNode, valueTwoNode, groupNode,],
        };

        var mqttCommunication = amazonCommunication.ToMqtt(string.Empty, false);

        mqttCommunication.Nodes.Should().HaveCount(3);
        mqttCommunication.Nodes.Should().ContainEquivalentOf(new Node()
        {
            Id = valueOneNodeId,
            Name = "value1",
            ParentId = null,
            DesignId = MqttNodeDesignId.Topic,
            ValueType = typeof(int),
            AffectedChannels = { valueOneNodeId.ToString(), },
            TransferredChannels = { valueOneNodeId.ToString(), },
            Properties = new()
            {
                { MqttNodeProperties.Retain, new() { Value = true} },
                { MqttNodeProperties.Serializer, new() { Value = (byte)2 } },
            },
        });
        mqttCommunication.Nodes.Should().ContainEquivalentOf(new Node()
        {
            Id = valueTwoNodeId,
            Name = "value2",
            ParentId = groupNodeId,
            DesignId = MqttNodeDesignId.Topic,
            ValueType = typeof(int),
            AffectedChannels = { valueTwoNodeId.ToString(), },
            TransferredChannels = { valueTwoNodeId.ToString(), },
        });
        mqttCommunication.Nodes.Should().ContainEquivalentOf(new Node()
        {
            Id = groupNodeId,
            Name = "group",
            ParentId = null,
            DesignId = MqttNodeDesignId.Topic,
            TransferredChannels = { valueTwoNodeId.ToString(), },
        });
    }

    [Fact]
    public void Should_translate_thing_nodes()
    {
        Node thingsNode = new()
        {
            Id = Guid.NewGuid(),
            DesignId = AwsIotNodeDesignId.Things,
        };
        Node thingNode = new()
        {
            Id = Guid.NewGuid(),
            ParentId = thingsNode.Id,
            Name = "thing",
            DesignId = AwsIotNodeDesignId.Thing,
        };
        var reportedValueClassicShadowNodeId = Guid.NewGuid();
        var desiredValueClassicShadowNodeId = Guid.NewGuid();
        Node classicShadowNode = new()
        {
            Id = Guid.NewGuid(),
            ParentId = thingNode.Id,
            Name = "ClassicShadow",
            DesignId = AwsIotNodeDesignId.ClassicDeviceShadow,
            TransferredChannels = { reportedValueClassicShadowNodeId.ToString(), desiredValueClassicShadowNodeId.ToString(), }
        };
        var reportedValueNamedShadowNodeId = Guid.NewGuid();
        var desiredValueNamedShadowNodeId = Guid.NewGuid();
        Node namedShadowNode = new()
        {
            Id = Guid.NewGuid(),
            ParentId = thingNode.Id,
            Name = "MyShadow",
            DesignId = AwsIotNodeDesignId.NamedDeviceShadow,
            TransferredChannels = { reportedValueNamedShadowNodeId.ToString(), desiredValueNamedShadowNodeId.ToString(), }
        };
        Node reportedClassicShadowNode = new()
        {
            Id = Guid.NewGuid(),
            ParentId = classicShadowNode.Id,
            Name = "reported",
            DesignId = AwsIotNodeDesignId.Reported,
        };
        Node desiredClassicShadowNode = new()
        {
            Id = Guid.NewGuid(),
            ParentId = classicShadowNode.Id,
            Name = "desired",
            DesignId = AwsIotNodeDesignId.Desired,
        };
        Node groupClassicShadowNode = new()
        {
            Id = Guid.NewGuid(),
            ParentId = reportedClassicShadowNode.Id,
            Name = "group",
            DesignId = AwsIotNodeDesignId.JsonStructure,
        };
        Node reportedValueClassicShadowNode = new()
        {
            Id = reportedValueClassicShadowNodeId,
            ParentId = groupClassicShadowNode.Id,
            Name = "value",
            DesignId = AwsIotNodeDesignId.JsonStructure,
            AffectedChannels = { reportedValueClassicShadowNodeId.ToString(), },
            ValueType = typeof(int),
        };
        Node desiredValueClassicShadowNode = new()
        {
            Id = desiredValueClassicShadowNodeId,
            ParentId = desiredClassicShadowNode.Id,
            Name = "value",
            DesignId = AwsIotNodeDesignId.JsonStructure,
            AffectedChannels = { desiredValueClassicShadowNodeId.ToString(), },
            ValueType = typeof(int),
        };
        Node reportedNamedShadowNode = new()
        {
            Id = Guid.NewGuid(),
            ParentId = namedShadowNode.Id,
            Name = "reported",
            DesignId = AwsIotNodeDesignId.Reported,
        };
        Node reportedValueNamedShadowNode = new()
        {
            Id = reportedValueNamedShadowNodeId,
            ParentId = reportedNamedShadowNode.Id,
            Name = "value",
            DesignId = AwsIotNodeDesignId.JsonStructure,
            AffectedChannels = { reportedValueNamedShadowNodeId.ToString(), },
            ValueType = typeof(int),
        };
        Node desiredNamedShadowNode = new()
        {
            Id = Guid.NewGuid(),
            ParentId = namedShadowNode.Id,
            Name = "desired",
            DesignId = AwsIotNodeDesignId.Desired,
        };
        Node desiredValueNamedShadowNode = new()
        {
            Id = desiredValueNamedShadowNodeId,
            ParentId = desiredNamedShadowNode.Id,
            Name = "value",
            DesignId = AwsIotNodeDesignId.JsonStructure,
            AffectedChannels = { desiredValueNamedShadowNodeId.ToString(), },
            ValueType = typeof(int),
        };

        AwsIotDataPortCommunication amazonCommunication = new()
        {
            Nodes = [thingsNode, thingNode, classicShadowNode, namedShadowNode, reportedClassicShadowNode, desiredClassicShadowNode, groupClassicShadowNode,
                reportedValueClassicShadowNode, desiredValueClassicShadowNode, reportedNamedShadowNode, reportedValueNamedShadowNode, desiredNamedShadowNode, desiredValueNamedShadowNode, ],
        };

        var mqttCommunication = amazonCommunication.ToMqtt(string.Empty, false);

        mqttCommunication.Nodes.Should().HaveCount(13);
        mqttCommunication.Nodes.Should().ContainEquivalentOf(new Node()
        {
            Id = classicShadowNode.Id,
            Name = "$aws/things/thing/shadow/update",
            ParentId = null,
            DesignId = MqttNodeDesignId.Topic,
            TransferredChannels = { reportedValueClassicShadowNode.Id.ToString(), desiredValueClassicShadowNode.Id.ToString(), },
        });
        mqttCommunication.Nodes.Should().ContainEquivalentOf(new Node()
        {
            Id = namedShadowNode.Id,
            Name = "$aws/things/thing/shadow/name/MyShadow/update",
            ParentId = null,
            DesignId = MqttNodeDesignId.Topic,
            TransferredChannels = { reportedValueNamedShadowNode.Id.ToString(), desiredValueNamedShadowNode.Id.ToString(), },
        });
        var classicStateNode = mqttCommunication.Nodes.Should().ContainEquivalentOf(new Node()
        {
            Name = "state",
            ParentId = classicShadowNode.Id,
            DesignId = MqttNodeDesignId.Topic,
        }, n => n.Excluding(e => e.Id)).Which;
        mqttCommunication.Nodes.Should().ContainEquivalentOf(new Node()
        {
            Id = reportedClassicShadowNode.Id,
            Name = "reported",
            ParentId = classicStateNode.Id,
            DesignId = MqttNodeDesignId.Topic,
        });
        mqttCommunication.Nodes.Should().ContainEquivalentOf(new Node()
        {
            Id = groupClassicShadowNode.Id,
            Name = "group",
            ParentId = reportedClassicShadowNode.Id,
            DesignId = MqttNodeDesignId.Topic,
        });
        mqttCommunication.Nodes.Should().ContainEquivalentOf(new Node()
        {
            Id = reportedValueClassicShadowNode.Id,
            Name = "value",
            ParentId = groupClassicShadowNode.Id,
            DesignId = MqttNodeDesignId.Topic,
            AffectedChannels = { reportedValueClassicShadowNode.Id.ToString(), },
            ValueType = typeof(int),
        });
        mqttCommunication.Nodes.Should().ContainEquivalentOf(new Node()
        {
            Id = desiredClassicShadowNode.Id,
            Name = "desired",
            ParentId = classicStateNode.Id,
            DesignId = MqttNodeDesignId.Topic,
        });
        mqttCommunication.Nodes.Should().ContainEquivalentOf(new Node()
        {
            Id = desiredValueClassicShadowNode.Id,
            Name = "value",
            ParentId = desiredClassicShadowNode.Id,
            DesignId = MqttNodeDesignId.Topic,
            AffectedChannels = { desiredValueClassicShadowNode.Id.ToString(), },
            ValueType = typeof(int),
        });
        var namedStateNode = mqttCommunication.Nodes.Should().ContainEquivalentOf(new Node()
        {
            Name = "state",
            ParentId = namedShadowNode.Id,
            DesignId = MqttNodeDesignId.Topic,
        }, n => n.Excluding(e => e.Id)).Which;
        mqttCommunication.Nodes.Should().ContainEquivalentOf(new Node()
        {
            Id = reportedNamedShadowNode.Id,
            Name = "reported",
            ParentId = namedStateNode.Id,
            DesignId = MqttNodeDesignId.Topic,
        });
        mqttCommunication.Nodes.Should().ContainEquivalentOf(new Node()
        {
            Id = reportedValueNamedShadowNode.Id,
            Name = "value",
            ParentId = reportedNamedShadowNode.Id,
            DesignId = MqttNodeDesignId.Topic,
            AffectedChannels = { reportedValueNamedShadowNode.Id.ToString(), },
            ValueType = typeof(int),
        });
        mqttCommunication.Nodes.Should().ContainEquivalentOf(new Node()
        {
            Id = desiredNamedShadowNode.Id,
            Name = "desired",
            ParentId = namedStateNode.Id,
            DesignId = MqttNodeDesignId.Topic,
        });
        mqttCommunication.Nodes.Should().ContainEquivalentOf(new Node()
        {
            Id = desiredValueNamedShadowNode.Id,
            Name = "value",
            ParentId = desiredNamedShadowNode.Id,
            DesignId = MqttNodeDesignId.Topic,
            AffectedChannels = { desiredValueNamedShadowNode.Id.ToString(), },
            ValueType = typeof(int),
        });
    }

    [Fact]
    public void Should_translate_communication()
    {
        AwsIotDataPortCommunication amazonCommunication = new()
        {
            CleanSession = true,
            CertificateFile = "certificate",
            PrivateKeyFile = "privateKey",
            ClientId = "clientId",
            QualityOfService = 1,
            SessionExpiryInterval = 60,
            WillTopic = "willTopic",
            WillMessage = "willMessage",
            WillRetain = true,
            EndpointPort = 1234,
            ProtocolVersion = 1,
            SslProtocol = 1,
            MaxPendingMessages = 1,
            DisableCertificateValidation = true,
        };

        var mqttCommunication = amazonCommunication.ToMqtt("local", false);

        mqttCommunication.CleanSession.Should().BeTrue();
        mqttCommunication.CertificateFile.Should().Be("certificate");
        mqttCommunication.CertificatePrivateKeyFile.Should().Be("privateKey");
        mqttCommunication.ClientId.Should().Be("clientId");
        mqttCommunication.QualityOfService.Should().Be(1);
        mqttCommunication.SessionExpiryInterval.Should().Be(60);
        mqttCommunication.WillTopic.Should().Be("willTopic");
        mqttCommunication.WillMessage.Should().Be("willMessage");
        mqttCommunication.WillRetain.Should().BeTrue();
        mqttCommunication.Host.Should().Be("local");
        mqttCommunication.Port.Should().Be(1234);
        mqttCommunication.ProtocolVersion.Should().Be(1);
        mqttCommunication.SslProtocol.Should().Be(1);
        mqttCommunication.DefaultSerializer.Should().Be(1);
        mqttCommunication.MaxPendingMessages.Should().Be(1);
        mqttCommunication.DisableCertificateValidation.Should().BeTrue();
    }
}
