# Connection Lifecycle: Design Rationale

A port owns a single connection to its external system, and that connection has a life of its own: it is opened at startup, used throughout operation, and closed at teardown. This page is about *when* the connection is opened and closed, and *why* a closed one can come back. It is the counterpart to [Client lifecycle manager](client-lifecycle-manager.md), which is about the interface that does the acquiring and releasing. Here the subject is the timeline, not the seam.

---

## Disconnect is reversible; dispose is terminal

There are two ways a connection ends, and they differ in one respect: whether the port can come back afterwards.

`DisconnectAsync` stops the background work and releases the client, but it leaves the validated, strongly-typed model in memory: the device node tree, the flattened data points, the poll-frequency groups. A later `ConnectAsync` simply re-acquires a client and resumes. This is the path a reconnection takes.

`DisposeAsync` is terminal. It disconnects, releases the resources the subclass owns, and ends the port's life. A disposed port is not meant to come back.

The split exists because the two kinds of state have very different costs and lifetimes. The validated model is derived from configuration once, at construction, and it does not change when a connection drops, so re-running the mapping and validation to recover from a network blip would be wasted work. So disconnect throws away only what is tied to the *live connection* (the client itself, the polling timers or the queue processor) and keeps everything that was derived from *configuration*. Dispose is the only operation that tears down the configuration-derived state as well, because it is the only one that means the port is finished.

Connection state is tracked implicitly by whether the port currently holds a client. Connecting when already connected is a no-op, and disconnecting when already disconnected is a no-op. Both log and return rather than failing. That makes the two operations idempotent, so the managed engine can call them defensively without having to track the port's state itself.

---

## Keeping the connection operational is the implementation's job

The extension library has no automated reconnect. `ConnectAsync` acquires a client once, and the base classes keep using that same instance for every read or write until the port is torn down. They never release and re-acquire it in response to a failure, and there is no watchdog that notices a dead connection and recycles it.

What the library does provide is resilience around *operations*, not recovery of the *connection*. An incoming port wraps each polling group in a pipeline with a timeout and a circuit breaker; an outgoing port retries the head of its queue with backoff. That keeps the port from hammering a struggling device and lets it resume cleanly once calls start succeeding again. None of it re-opens a connection, though. It just keeps calling the same client.

The distinction worth being precise about is that the base class cannot tell a dropped connection apart from any other transient failure. A read that fails because the socket is gone and a read that fails because the device was briefly busy arrive the same way, as an exception from the client's `ReadAsync` or `WriteAsync`, and the resilience pipeline treats them identically. The base class has no signal that says "reconnect me" as opposed to "try again in a moment", so it does neither and simply retries against the same client.

That places the responsibility for keeping the connection operational on the implementation. Because the library keeps calling the same acquired client, that client has to heal its own transport: a dropped socket or an expired session must be re-established inside the client so the next `ReadAsync` or `WriteAsync` can succeed (see [re-establishing a dropped transport](client-lifecycle-manager.md#re-establishing-a-dropped-transport-is-the-clients-own-job)). How a given failure is handled is therefore the client's own call. An ordinary transient hiccup just needs the next attempt to go through; a genuine disconnect needs the transport rebuilt before the next attempt can work. The resilience pipeline gives the client room to do either, retrying without tearing anything down. If the client cannot recover on its own, the affected reads or writes keep failing and the port makes no progress until the connection is restored by other means.

The one reconnect the library does model is the coarse one: a `DisconnectAsync` followed by a `ConnectAsync`, which releases the client and acquires a fresh one. The base classes never trigger this themselves; it is driven from outside the port, by the managed engine. Deciding that a connection is dead enough to recycle wholesale, rather than merely briefly unavailable, is a policy judgement that depends on the protocol and the deployment, so it lives with the engine, which has the context to make it, rather than being baked into the base class.

---

## A failed startup never leaks a live connection

Connection-establishment failures, such as an unreachable host or a rejected authentication, surface from `AcquireConnectedAsync` as `ConnectionFailureException` and propagate to the caller. Nothing was acquired, so there is nothing to clean up.

The more subtle case is when the connection succeeds but the configuration does not hold up against the live device. Here the base class has a connected client in hand when it discovers the problem, so before it throws `InvalidConfigurationException` it calls `DisconnectAsync` to release that client. A startup that fails for any reason leaves the port disconnected, never holding a connection it has no intention of using. See [Configuration verification](configuration-verification.md) for why that check runs where it does.

---

## Related

- [Client lifecycle manager](client-lifecycle-manager.md): the interface that acquires and releases the connection, and why it is a separate concern
- [Configuration verification](configuration-verification.md): the optional startup check that runs against the live connection
- [Overview](../Overview.md): the four lifecycle phases and the "surface errors early" principle in full
- [Incoming data port](../incoming/incoming-data-port.md) · [Outgoing data port](../outgoing/outgoing-data-port.md): the connect / disconnect diagrams for each direction