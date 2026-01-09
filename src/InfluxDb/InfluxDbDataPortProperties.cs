using System;

namespace ViciOne.Suite.DataPort;

internal sealed class InfluxDbDataPortProperties(InfluxDbDataPortCommunication communication)
{
    internal string Token
    {
        get => communication.Token;
        set => communication.Token = value;
    }

    internal string Url
    {
        get => communication.Url;
        set => communication.Url = value;
    }

    internal string Organization
    {
        get => communication.Organization;
        set => communication.Organization = value;
    }

    internal int BulkSize
    {
        get => communication.BulkSize;
        set => communication.BulkSize = value;
    }

    internal TimeSpan MinimumPublishInterval
    {
        get => TimeSpan.FromMilliseconds(communication.MinimumPublishIntervalInMs);
        set => communication.MinimumPublishIntervalInMs = Convert.ToUInt32(value.TotalMilliseconds);
    }
}
