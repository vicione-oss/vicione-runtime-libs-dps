using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using ViciOne.Suite.DataPort.Extensions.Incoming.Client;
using ViciOne.Suite.DataPort.Extensions.Incoming.Polling.Job;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

namespace ViciOne.Suite.DataPort.Extensions.Incoming.Polling;

/// <summary>
/// Polling job for a single data point group.
/// Periodically reads data points and publishes values.
/// </summary>
/// <typeparam name="TDataPointGroup">Data point group type (DataPointGroup or SymbolicDataPointGroup)</typeparam>
/// <typeparam name="TDataPoint">Individual data point type that implements IPollingDataPoint</typeparam>
/// <typeparam name="TDataPointValue">Read result type with value</typeparam>
/// <typeparam name="TReadClient">Client type for connection reference (used for type safety)</typeparam>
/// <remarks>
/// <para>
/// This class provides generic polling functionality for specific implementations.
/// It inherits from PeriodicJob to leverage the common polling infrastructure.
/// </para>
/// </remarks>
internal sealed class DataPointGroupPollingJob<TDataPointGroup, TDataPoint, TDataPointValue, TReadClient>(
    TimeSpan executionInterval,
    TDataPointGroup input,
    TReadClient readClient,
    Action<IReadOnlyList<TDataPointValue>> onJobOutputAvailable,
    ResiliencePipeline<IReadOnlyList<TDataPointValue>>? resiliencePipeline = null,
    TimeProvider? timeProvider = null,
    ILogger<PeriodicJob<TDataPointGroup, IReadOnlyList<TDataPointValue>>>? logger = null,
    CancellationToken? externalCancellation = null)
    : PeriodicJob<TDataPointGroup, IReadOnlyList<TDataPointValue>>(
        executionInterval, input, onJobOutputAvailable,
        resiliencePipeline, timeProvider, logger, externalCancellation)
    where TDataPoint : IPollingDataPoint
    where TDataPointValue : IDataPointValue<TDataPoint>
    where TDataPointGroup : IDataPointPollingGroup<TDataPoint>
    where TReadClient : IReadClient<TDataPointGroup, TDataPointValue>
{
    protected override ValueTask<IReadOnlyList<TDataPointValue>> PerformJob(TDataPointGroup input, CancellationToken token) =>
        readClient.ReadAsync(input, token);

    protected override string GetJobName(TDataPointGroup input) =>
        $"DataPointGroupPolling at [{ExecutionInterval.TotalMilliseconds}ms]";
}
