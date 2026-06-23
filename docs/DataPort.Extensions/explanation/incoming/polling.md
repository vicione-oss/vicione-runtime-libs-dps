# Polling Jobs: Design Rationale

The `Incoming.Polling` namespace takes an instruction like "read this group of data points every 250 ms" and turns it into a background loop that keeps running for months. `IncomingDataPortBase` settles the questions of what gets polled and how often. Everything past that point is this subsystem: the timers that fire, the resilience that wraps each read, the choreography that starts and stops the loops, and the handling that stops one bad read from taking a whole group down.

This page is about why that machinery is split and shaped the way it is. If you want the broader case for polling by group, read [Incoming data port](incoming-data-port.md) first. If you want build steps and the exact resilience numbers, the [incoming data port how-to](../../how-to/incoming-dataport/incoming-data-port.md) has them.

---

## Three layers, each with one responsibility

Polling is built from three types stacked on top of one another, kept deliberately apart:

- **`PollingJobOrchestrator`** owns the fleet. It creates one job per polling group, starts them all, and later tears them all down together. It is the only layer that knows about the group list, the read client, and the shared cancellation that stops everything at once.
- **`DataPointGroupPollingJob`** is a thin adapter that binds one group and the read client into a concrete job. It answers only the two questions that are protocol-aware: what a single tick does, where `PerformJob` calls `IReadClient.ReadAsync`, and what to call the job in the logs, where `GetJobName` produces the label. Everything else it inherits.
- **`PeriodicJob<TJobInput, TJobOutput>`** is the generic engine underneath. It runs an arbitrary unit of work on a cadence, pushes each result through a resilience pipeline, and hands the output to a callback. It knows nothing about data points, reading, or protocols.

Each layer changes for its own reasons, which is the point of keeping them separate. Managing a set of jobs and shutting them down cleanly is logistics. Deciding what a single poll does is a five-line protocol binding. Running a task on a timer without letting failures pile up is a general problem with nothing protocol-specific about it. Collapse all three into one class and the generic timer loop ends up tangled with read-client details, no piece can be tested on its own, and swapping any one of them means touching the others.

Visibility follows the same line. `PeriodicJob` and `DefaultResiliencePipeline` are public; the orchestrator and the per-group adapter are internal. That split is deliberate. An implementation that does not inherit from `IncomingDataPortBase` can still reach for the generic periodic job and get the same resilience behaviour, yet it never sees the wiring that only makes sense inside the incoming data port.

---

## Overlapping executions are allowed, not prevented

Every tick launches its iteration and does not wait for it. The loop kicks off the work and goes straight back to waiting for the next tick:

```csharp
while (await timer.WaitForNextTickAsync(token))
    _ = ExecuteJobIteration();   // not awaited
```

This is the decision that shapes everything else at runtime. Suppose the loop did await each read. A single slow response would then delay the next tick by however long that read took, and the poll frequency you configured would quietly become whichever is slower, your interval or the device. A group set to 100 ms would stretch to 100 ms plus read latency, and on a sluggish device that lag would keep accumulating. Firing each iteration on its own avoids the problem entirely: the timer keeps a fixed schedule no matter how long any read runs.

The obvious worry is that a slow device could pile up an ever-growing stack of overlapping reads. What prevents that is the resilience timeout rather than a lock. Each iteration runs inside a pipeline whose timeout defaults to 110% of the interval, so no iteration outlives much more than a single period. That caps the overlap at roughly two iterations at any moment. Slow groups, where the timeout works out shorter than the interval, never overlap at all. The overlap that does occur is a bounded consequence of separating the schedule from the work, not an accident.

The bill comes due at shutdown. `StopPeriodicExecution` waits for the timer loop, but not for the iterations already launched before cancellation. Those wind down on their own once the cancellation token trips, which means an output callback can still fire for a moment after stop has returned. We chose to live with that. Tracking and awaiting every in-flight iteration would buy a guarantee a polling consumer has no use for, since a stray value arriving just after disconnect does no harm.

---

## Resilience is per group and tuned to its cadence

Every iteration runs through a Polly `ResiliencePipeline`. By default that pipeline is `DefaultResiliencePipeline`, a timeout wrapped around a circuit breaker, with every constant computed from the group's own poll interval rather than hard-coded. The actual numbers live in the [how-to's resilience table](../../how-to/incoming-dataport/incoming-data-port.md#5-optional-customize-polling-resilience). What this section explains is why they scale with the interval at all.

Think about what a single fixed timeout would have to do. It would have to serve both a 100 ms group and an hourly one, and no one value can. Anything generous enough for the slow group lets the fast group fall hopelessly behind before the timeout ever fires. Pinning the timeout to 110% of the interval says what we actually mean: a poll should finish inside its own period, give or take a little jitter. A read that runs past its period is already behind, so timing it out frees the slot for the next attempt. An absolute cap of 30 seconds keeps the proportional rule from turning an hourly group's timeout into something absurd.

The circuit breaker scales for the same reason. Its sampling window, the span of history it judges health on, and its break duration, the time it waits before probing again, are both multiples of the interval. A fast group gathers enough calls to form an opinion quickly and recovers quickly. A slow group gets a proportionally longer memory, though a cap stops that window from growing without limit. The breaker earns its place because outages in the field are rarely instantaneous. When a device drops, reading at full cadence only produces a stream of fast failures. So the breaker watches the failure rate, stops trying for a while once it crosses the threshold, and then probes on its own to check whether the device is back. A returning device is picked up without anyone intervening. A small jitter on the break duration keeps those recovery probes from landing exactly on a tick boundary.

One detail matters more than it first looks. Each group gets its own pipeline instance, because the orchestrator calls the factory once per group as it builds the jobs. Every group therefore has independent breaker state. An hourly group can sit with its breaker wide open while a 100 ms group keeps reading as though nothing were wrong. Parts of the same device often differ in availability, and a shared breaker would let the flakiest subsystem drag down the rest.

None of this is fixed policy. It is only the default. `IncomingDataPortBase` builds each group's pipeline through one method, `CreatePollingResiliencePipeline(TDataPointGroup)`, and the stock implementation returns `DefaultResiliencePipeline` tuned to the group it was handed. An override plugs into the very same method, so there is never a second code path to hold in your head, and there is no way to end up with no pipeline at all, because the seam always returns one. [Step 5 of the how-to](../../how-to/incoming-dataport/incoming-data-port.md#5-optional-customize-polling-resilience) shows how to override it.

---

## A failed read is absorbed as data, not propagated as a fault

When `PerformJob` throws, the iteration catches the exception, logs it as a transient warning, and lets the loop go back to waiting for the next tick. The full stack trace stays at debug level, so a device that flaps every few seconds does not bury the log. A failed read never stops its group, and it never raises a `Received` event. Publishing nothing is the right call, because half a reading is worse than none. Cancellation travels its own path and is not counted as an error, since cancelling is simply how a job is meant to stop.

This is what turns the phrase "transient failures are expected, not exceptional" into real behaviour. The timer loop only ends on cancellation or on a genuinely unexpected fatal error. Anything a single read can throw stays boxed inside that one iteration. Put that together with the per-group circuit breaker and a group will absorb one failed read quietly, ease off when reads keep failing, and resume by itself when the device comes back. No code above the polling layer has to be involved.

---

## Lifecycle and teardown

Two methods on the orchestrator drive the whole fleet. `Start` builds every job first and starts them only once they all exist, so a slow start in one group does not hold up constructing the others. `DisposeAsync` cancels a single shared `CancellationTokenSource`, the one every job links its own token to, and then disposes all the jobs together. A single cancellation therefore stops every group at once, and disposal waits for the timer loops to wind down in parallel rather than one after another.

Time comes in through `TimeProvider` instead of straight off the system clock. Production uses `TimeProvider.System`. Tests use a `FakeTimeProvider`, which lets a test trigger a poll tick by advancing virtual time rather than sleeping through real intervals. The [how-to's testing section](../../how-to/incoming-dataport/incoming-data-port.md#test-the-polling-loop) walks through the pattern. The fire-and-forget caveat from the shutdown discussion applies here too: advancing the clock only starts the tick, and the iteration still runs asynchronously, so a test has to assert eventually rather than on the next line.

---

## Observability is structured and categorised

Neither the generic job nor the orchestrator logs through ad-hoc calls. Both go through dedicated, source-generated message classes: `PeriodicJobLogMessages`, which is public, and `PollingJobOrchestratorLogs`, which is internal. The periodic-job events fall into fixed event-ID ranges, one band for job lifecycle, one for per-iteration events, and one for resilience events such as circuit-breaker transitions and timeouts. For a port that runs unattended for months, that structure pays off operationally. An operator can filter to resilience events alone to see whether a breaker is opening, or to iteration events to confirm that reads are still landing, without sifting through everything else.

---

## Related explanation

- [Incoming data port](incoming-data-port.md) makes the higher-level case for polling by group and for building resilience into the base class.
- [Overview](../Overview.md) places polling within the data-port lifecycle, in the operation steady state.
- [Outgoing data port](../outgoing/outgoing-data-port.md) is the write-side counterpart, where a background queue solves the mirror-image problem.

## Related how-to and reference

- The [incoming data port how-to](../../how-to/incoming-dataport/incoming-data-port.md) covers building a port, the resilience-settings table, overriding the pipeline, and testing the loop.
- [Resilience components](../../reference/resilience.md) documents `DefaultResiliencePipeline` and the retry utilities.
- The generated API carries [`PeriodicJob`](xref:ViciOne.Suite.DataPort.Extensions.Incoming.Polling.Job.PeriodicJob%602) and [`DefaultResiliencePipeline`](xref:ViciOne.Suite.DataPort.Extensions.Incoming.Polling.Job.DefaultResiliencePipeline).
