# Common Exceptions

**Namespace**: `ViciOne.Suite.DataPort.Extensions.Exceptions`

> **Beyond the API summaries.** The generated [API reference](api/index.md) gives each exception's
> meaning, constructors, and members. This page adds the *flow disposition*: where each exception is
> raised, where it is caught, and whether your implementation is expected to handle it.

### [ConnectionFailureException](xref:ViciOne.Suite.DataPort.Extensions.Exceptions.ConnectionFailureException)

Raised by `IClientLifecycleManager.AcquireConnectedAsync` implementations, and by `IReadClient.ReadAsync`
when the connection drops mid-read. Surfaces to the `ConnectAsync` caller; there is no automatic retry at
the data-port level.

### [DataRetrievalException](xref:ViciOne.Suite.DataPort.Extensions.Exceptions.DataRetrievalException)

Raised by `IReadClient.ReadAsync` implementations when a read fails. It propagates through the group's
polling resilience pipeline; an unabsorbed failure is logged by the periodic polling job and the group
keeps polling on the next tick.

### [DataConversionException](xref:ViciOne.Suite.DataPort.Extensions.Exceptions.DataConversionException)

Raised by protocol value-conversion code. Constructed through the factory methods listed on the API page
rather than public constructors.

### [InvalidConfigurationException](xref:ViciOne.Suite.DataPort.Extensions.Exceptions.InvalidConfigurationException)

Raised during construction (typed-node mapping and validation) and at startup (configuration
verification). Surfaces to the constructor or the `ConnectAsync` caller.

### [MaxRetriesExceededException](xref:ViciOne.Suite.DataPort.Extensions.Exceptions.MaxRetriesExceededException)

Thrown by the outgoing `QueueProcessor` when the active `IRetryPolicy` declines a further retry (e.g.
`BoundedRetryPolicy` after its limit); carries `AttemptCount`. Caught internally by the `QueueProcessor`
— never surfaced to `SendAsync` callers.

### [QueueBackpressureException](xref:ViciOne.Suite.DataPort.Extensions.Exceptions.QueueBackpressureException)

Raised internally when a retrying write item is displaced from the queue head (DropOldest overflow during
retry). Caught inside the outgoing queue processor — never surfaced to `SendAsync` callers.
