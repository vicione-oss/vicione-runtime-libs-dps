using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.Suite.DataPort.Extensions.Client;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Client;

internal sealed class TestClientLifecycleManager(Func<TestClientInformation, TestClient>? clientFactory = null)
    : IClientLifecycleManager<TestClient, TestClientInformation>
{
    private readonly Dictionary<TestClientInformation, TestClient> _existingClients = [];
    public int AcquireCallCount { get; private set; }
    public int ReleaseCallCount { get; private set; }

    public ValueTask<TestClient> AcquireConnectedAsync(TestClientInformation clientInformation,
        CancellationToken cancellationToken)
    {
        AcquireCallCount++;
        var client = _existingClients.GetValueOrDefault(clientInformation);
        if (client is not null) return new ValueTask<TestClient>(client);

        client = clientFactory?.Invoke(clientInformation) ?? new TestClient();
        _existingClients.Add(clientInformation, client);
        return new ValueTask<TestClient>(client);
    }

    public ValueTask ReleaseAsync(TestClientInformation clientInformation, CancellationToken cancellationToken)
    {
        ReleaseCallCount++;
        _existingClients.Remove(clientInformation);
        return ValueTask.CompletedTask;
    }
}
