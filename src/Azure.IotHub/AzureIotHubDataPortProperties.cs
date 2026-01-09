namespace ViciOne.Suite.DataPort;

internal sealed class AzureIotHubDataPortProperties(AzureIotHubDataPortCommunication communication)
{
    internal string Hostname
    {
        get => communication.Hostname;
        set => communication.Hostname = value;
    }

    internal string SharedAccessSignatureKey
    {
        get => communication.SharedAccessSignatureKey;
        set => communication.SharedAccessSignatureKey = value;
    }

    internal string SharedAccessSignatureKeyName
    {
        get => communication.SharedAccessSignatureKeyName;
        set => communication.SharedAccessSignatureKeyName = value;
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
