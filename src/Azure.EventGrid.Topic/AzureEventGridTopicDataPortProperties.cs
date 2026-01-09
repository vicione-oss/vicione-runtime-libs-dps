namespace ViciOne.Suite.DataPort;

internal sealed class AzureEventGridTopicDataPortProperties(AzureEventGridTopicDataPortCommunication communication)
{
    internal string TopicEndpoint
    {
        get => communication.TopicEndpoint;
        set => communication.TopicEndpoint = value;
    }

    internal string AccessKey
    {
        get => communication.AccessKey;
        set => communication.AccessKey = value;
    }

    internal int MaxPendingMessages
    {
        get => communication.MaxPendingMessages;
        set => communication.MaxPendingMessages = value;
    }

    internal int BulkSize
    {
        get => communication.BulkSize;
        set => communication.BulkSize = value;
    }
}
