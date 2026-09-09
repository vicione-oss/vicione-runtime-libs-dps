# Resilience Components

> **Beyond the API summaries.** The generated [API reference](api/index.md) carries each type's members
> and the backoff algorithm. This page adds how the retry, backoff, and resilience-pipeline utilities
> relate — which implementation is the default and the production-vs-test seams — and points to the *why*
> in [Incoming data port](../explanation/incoming/incoming-data-port.md#resilience-is-built-in-not-optional)
> and [Outgoing data port](../explanation/outgoing/outgoing-data-port.md#infiniteretrypolicy-is-the-default).

## Components

### [RetryBackoffCalculator](xref:ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing.Retry.RetryBackoffCalculator)

Stateless exponential/linear backoff calculator used by the outgoing queue processor.

### [IDelayProvider](xref:ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing.IDelayProvider)

The retry-delay seam passed to the `OutgoingDataPortBase` constructor. Implemented by
[`DefaultDelayProvider`](xref:ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing.DefaultDelayProvider)
for production and
[`InstantDelayProvider`](xref:ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing.InstantDelayProvider)
to skip real waits in tests.

### [IRetryPolicy](xref:ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing.Retry.IRetryPolicy)

The retry-decision contract used by the outgoing queue processor, supplied through the `RetryPolicy`
property. Implemented by
[`InfiniteRetryPolicy`](xref:ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing.Retry.InfiniteRetryPolicy)
(the default) and
[`BoundedRetryPolicy`](xref:ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing.Retry.BoundedRetryPolicy).

→ Why infinite retry is the default and when to bound it:
[Outgoing data port](../explanation/outgoing/outgoing-data-port.md#infiniteretrypolicy-is-the-default)

→ Why the write path uses this instead of a Polly pipeline:
[Outgoing data port](../explanation/outgoing/outgoing-data-port.md#why-the-write-path-does-not-use-polly)

### [DefaultResiliencePipeline](xref:ViciOne.Suite.DataPort.Extensions.Incoming.Polling.Job.DefaultResiliencePipeline)

The Polly timeout + circuit-breaker pipeline returned by
`IncomingDataPortBase.CreatePollingResiliencePipeline(TDataPointGroup)` by default, tuned per group's poll
frequency. Every setting is derived from the group's poll interval:

| Strategy | Setting |
|----------|---------|
| Timeout | 110% of poll interval, max 30 s |
| Circuit breaker opens | 50% failure rate over 10 calls |
| Sampling window | 10× poll interval, max 5 min |
| Break duration | 2× poll interval (500 ms – 2 min) + 50 ms |

→ Why these scale with the interval: [Polling jobs](../explanation/incoming/polling.md).
→ How to override it: [incoming how-to — Customize polling resilience](../how-to/incoming-dataport/incoming-data-port.md#5-optional-customize-polling-resilience)
