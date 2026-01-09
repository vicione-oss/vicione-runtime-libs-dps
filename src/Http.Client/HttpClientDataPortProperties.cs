using System;

namespace ViciOne.Suite.DataPort;

internal sealed class HttpClientDataPortProperties(HttpClientDataPortCommunication communication)
{
    internal string BaseAddress
    {
        get => communication.BaseAddress;
        set => communication.BaseAddress = value;
    }

    internal TimeSpan? PollInterval
    {
        get => communication.PollInterval is null ? null : TimeSpan.FromMilliseconds(communication.PollInterval.Value);
        set => communication.PollInterval = value.HasValue ? Convert.ToInt32(value.Value.TotalMilliseconds) : null;
    }

    internal AuthenticationType AuthenticationType
    {
        get => (AuthenticationType)communication.AuthenticationType;
        set => communication.AuthenticationType = (byte)value;
    }

    internal string BearerToken
    {
        get => communication.BearerToken;
        set => communication.BearerToken = value;
    }

    internal string Username
    {
        get => communication.Username;
        set => communication.Username = value;
    }

    internal string Password
    {
        get => communication.Password;
        set => communication.Password = value;
    }
}
