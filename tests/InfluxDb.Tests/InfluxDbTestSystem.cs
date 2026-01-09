using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Testcontainers.InfluxDb;
using Xunit;

namespace ViciOne.Suite.DataPort;

[SuppressMessage("Maintainability", "CA1515:Erwägen Sie, öffentliche Typen intern zu machen.")]
public sealed class InfluxDbTestSystem : IAsyncLifetime
{
    private const string Token = nameof(InfluxDbTestSystem);
    private readonly InfluxDbContainer _container = new InfluxDbBuilder().WithAdminToken(Token).Build();

    internal InfluxDbDataPortCommunication Communication { get; } = new();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        Communication.Organization = InfluxDbBuilder.DefaultOrganization;
        Communication.Token = Token;
        Communication.Url = _container.GetAddress();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
