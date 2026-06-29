# Manage the connection

**Shared building block — step 4.** Used by both [incoming](../incoming-dataport/incoming-data-port.md)
and [outgoing](../outgoing-dataport/outgoing-data-port.md) ports.

The base classes drive *when* a connection is opened and released, but not *how* — that is yours,
because connection management changes for reasons that have nothing to do with reading or writing
(opened fresh each time, kept alive as a shared session, or borrowed from a pool). You supply two
pieces: a value that identifies which connection to use, and a manager that acquires and releases it.

The worked example is under `tests/DataPort.Extensions.Tests/Stubs/Client/`.

## 1. Define the client information

`IClientInformation` is a marker for the parameters that uniquely identify a connection — host, port,
credentials, whatever your protocol needs to decide *which* device a port talks to. The base class
passes it to the lifecycle manager; two ports with equal client information can share one underlying
connection.

`Stubs/Client/TestClientInformation.cs`:

```csharp
internal record TestClientInformation(string Id = "test") : IClientInformation;
```

A `record` is a good fit: value equality is exactly the "same connection" test the manager needs.

## 2. Implement the lifecycle manager

`IClientLifecycleManager<TClient, TClientInformation>` has two methods:

- `AcquireConnectedAsync(clientInformation, ct)` returns a **connected** client, ready to use.
- `ReleaseAsync(clientInformation, ct)` tears it down or returns it to the pool.

`Stubs/Client/TestClientLifecycleManager.cs` keeps a cache keyed by client information, so repeated
acquires for the same device hand back the same client:

```csharp
internal sealed class TestClientLifecycleManager(Func<TestClientInformation, TestClient>? clientFactory = null)
    : IClientLifecycleManager<TestClient, TestClientInformation>
{
    private readonly Dictionary<TestClientInformation, TestClient> _existingClients = [];

    public ValueTask<TestClient> AcquireConnectedAsync(TestClientInformation info, CancellationToken ct)
    {
        if (_existingClients.TryGetValue(info, out var existing)) return new(existing);
        var client = clientFactory?.Invoke(info) ?? new TestClient();
        _existingClients.Add(info, client);
        return new(client);
        // a real manager opens the protocol connection here, and throws ConnectionFailureException on failure
    }

    public ValueTask ReleaseAsync(TestClientInformation info, CancellationToken ct)
    {
        _existingClients.Remove(info);
        return ValueTask.CompletedTask;
    }
}
```

Throw `ConnectionFailureException` on failure — not a protocol-specific exception — so the base class
and the caller of `ConnectAsync` see a uniform error surface. A failed acquire never leaves a live
connection behind: the base class cleans up before rethrowing.

## The client type differs by direction

`AcquireConnectedAsync` returns `TClient`, and what that client *is* depends on the direction:

- **Incoming**: the manager returns a separate **read client** (`IReadClient`). The lifecycle manager
  and the read client are two different types.
- **Outgoing**: the **write client** (`IWriteClient`) *doubles* as the connection handle — the manager
  returns the same object you write through. There is no separate write-client property.

The example's `TestClient` implements *both* `IReadClient` and `IWriteClient`, so one manager serves
both ports — but in a real implementation the read and write clients are usually distinct. You build
the client itself in the direction-specific guide:
[read client](../incoming-dataport/incoming-data-port.md#1-implement-the-read-client) ·
[write client](../outgoing-dataport/outgoing-data-port.md#1-implement-the-write-client).

## Next

The shared building blocks are done. Continue with a direction:
[incoming](../incoming-dataport/incoming-data-port.md) or
[outgoing](../outgoing-dataport/outgoing-data-port.md). Optionally, add
[configuration verification](configuration-verification.md) first.

## Related

- [Connection lifecycle — design rationale](../../explanation/connection/connection-lifecycle.md) —
  why disconnect is reversible, dispose is terminal, and the library never auto-reconnects
- [Client lifecycle manager — design rationale](../../explanation/connection/client-lifecycle-manager.md) —
  why the manager is separate from the read/write client
- [Interfaces reference](../../reference/interfaces.md#connection-management) — `IClientInformation`,
  `IClientLifecycleManager`
