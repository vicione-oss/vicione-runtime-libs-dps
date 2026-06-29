# Client Lifecycle Manager: Design Rationale

`IClientLifecycleManager` is the interface both base classes use to acquire and release protocol connections. It is separate from the interfaces responsible for protocol I/O (`IReadClient` for incoming, `IWriteClient` for outgoing).

---

## Why lifecycle management is a separate concern

Connection management (acquiring, releasing, reconnecting, holding pool references) evolves independently of protocol I/O semantics, and the read or write client should never need to know how it was created.

How a connection is managed varies by protocol and deployment, and the differences have nothing to do with reading or writing:

- **Fresh per use**: open a connection on `AcquireConnectedAsync` and close it on `ReleaseAsync`. The simplest option, and the right one when a connection is cheap to establish and carries no session state worth keeping.
- **Shared session**: open one long-lived session and hand the same instance to every acquisition. This suits protocols where establishing a session is expensive, such as an authenticated OPC-UA session, and a single port owns it.
- **Pooled**: borrow from a pool shared across several ports, reference-counted, with the pool owning the physical connections. This bounds how many real connections a busy endpoint has to carry.

The lifecycle manager encapsulates whichever of these a protocol chooses, so the choice never leaks into the I/O path.

This separation enables pool-backed implementations without touching the I/O path. A lifecycle manager that wraps a shared pool simply increments a reference count on `AcquireConnectedAsync` and decrements it on `ReleaseAsync`. From the data port's perspective, it called `AcquireConnectedAsync` and got a client. Whether a TCP connection was opened at that moment or an existing one was handed over is an implementation detail that stays inside the lifecycle manager.

A typical pool-backed layering puts an adapter between the port and a shared pool singleton. The adapter implements `IClientLifecycleManager` and translates acquire/release into reference-count changes on the pool, which owns the physical connections:

![Pool adapter layering: IncomingDataPortBase / OutgoingDataPortBase depend on IClientLifecycleManager, implemented by the PoolConnectionManager adapter, which delegates acquire/release to a shared ConnectionPool singleton that manages the physical Protocol SDK connections to the remote device](diagrams/pool-adapter.svg)

Because the pool is shared, the adapter must close the physical connection only when its reference count reaches zero, and it must **not** dispose the pool from within a single port's teardown, because that would tear the connection out from under every other port using it. `IClientLifecycleManager` deliberately does not extend `IAsyncDisposable` for this reason: the manager's own lifetime is owned externally, not by the ports that borrow from it.

### Thread safety is the manager's responsibility

The base classes may call `AcquireConnectedAsync` and `ReleaseAsync` concurrently from different threads. A reconnection can begin while a release is still in flight, for example. The interface does not prescribe how a manager stays safe under that, because the right approach depends on what the manager wraps: a fresh-per-use manager has almost no shared state to guard, while a pool-backed one has reference counts and bookkeeping that several ports touch at once. A pooled implementation typically holds a lock only over that in-memory state and keeps the expensive I/O, the actual TCP connect or disconnect, outside it, so one slow connect never stalls the other callers.

---

## Re-establishing a dropped transport is the client's own job

`AcquireConnectedAsync` is where connection-establishment failures surface, such as an unreachable host or a failed authentication. Once a connection is up, re-establishing a transport that has *genuinely dropped* is expected of the protocol client itself; it is part of the interface's implicit contract, rather than something the base library generalizes or documents today.

The base library leans on this: it has no reconnect of its own and keeps calling the same client through its resilience pipeline, so the client is what has to bring a dropped transport back. See [keeping the connection operational is the implementation's job](connection-lifecycle.md#keeping-the-connection-operational-is-the-implementations-job).

---

## Incoming: lifecycle manager and read client are separate types

For incoming ports, `IClientLifecycleManager` and `IReadClient` are different interfaces on potentially different types.

The split reflects that read semantics and connection semantics evolve for different reasons. A new read optimisation, such as batching registers or changing timeouts, shouldn't require touching the lifecycle manager. A change in connection pooling strategy shouldn't require touching the read client.

---

## Outgoing: the write client doubles as the connection handle

For outgoing ports, `TWriteClient` is both the type returned by `IClientLifecycleManager.AcquireConnectedAsync` and the object used for writing. It must implement `IWriteClient<TDataPointValue>`.

For write-only ports, the connection and the client are effectively the same thing. The outgoing port's background queue processor is the sole consumer of the write client, and there is no scenario where the base class holds a connection handle separately from the write client. A separate "connection handle" alongside the write client would be an interface split that doesn't reflect a real separation of concerns.

---

## Related

- [Connection lifecycle](connection-lifecycle.md): when the connection is acquired and released, and why disconnect is reversible but dispose is terminal
- [Configuration verification](configuration-verification.md): the optional startup check that runs against the connection this manager hands over
- [Incoming data port](../incoming/incoming-data-port.md): polling model and where the lifecycle manager fits
- [Outgoing data port](../outgoing/outgoing-data-port.md): queue model and where the lifecycle manager fits
- [Interfaces reference](../../reference/interfaces.md): `IClientLifecycleManager` contract
