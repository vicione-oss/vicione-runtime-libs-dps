using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.ManagedEngine.ExternalCommunication;
using ViciOne.Suite.DataPort.Extensions.Exceptions;
using ViciOne.Suite.DataPort.Extensions.Logging;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;
using ViciOne.Suite.DataPort.Extensions.TypedNodes;

namespace ViciOne.Suite.DataPort.Extensions.Incoming;

/// <summary>
/// Base class for IncomingDataPort implementations.
/// Implements Template Method pattern with strategy hooks for connection management.
/// </summary>
/// <typeparam name="TCommunication">Communication configuration type</typeparam>
/// <typeparam name="TDeviceNode">Device node configuration type</typeparam>
/// <typeparam name="TDataPointGroup">Data point group type</typeparam>
/// <typeparam name="TDataPointValue">Read result type with value</typeparam>
/// <typeparam name="TClient">Client type for read operations</typeparam>
/// <remarks>
/// <para><b>Design Patterns:</b></para>
/// <list type="bullet">
/// <item><b>Template Method:</b> ConnectAsync, StartPollingAndPublishingDataPoints, ToExternalValues orchestrate common flow</item>
/// <item><b>Strategy:</b> IConnectionManager abstracts connection patterns</item>
/// <item><b>Hook Method:</b> ValidateConnectionAsync is virtual (no-op default)</item>
/// </list>
/// <para><b>Lifecycle:</b></para>
/// <list type="number">
/// <item>Create instance via constructor</item>
/// <item>Call <see cref="ConnectAsync"/> to acquire connection and start polling</item>
/// <item>Subscribe to <see cref="Received"/> event for data updates</item>
/// <item>Call <see cref="DisconnectAsync"/> to stop polling and release connection (supports reconnection)</item>
/// <item>Call <see cref="DisposeAsync"/> for final cleanup (cannot reconnect after disposal)</item>
/// </list>
/// <para><b>Thread Safety:</b> Lifecycle methods are not thread-safe and should be called from a single thread.</para>
/// </remarks>
public abstract class IncomingDataPortBase<TCommunication, TDeviceNode, TDataPointGroup, TDataPointValue, TClient>
    : IExternalIncomingCommunication<TCommunication>, IAsyncDisposable
    where TCommunication : DataPortCommunication
    where TDeviceNode : IRootConfigurationNode<TCommunication>
    where TDataPointGroup : class
    where TClient : class
{
    private readonly TimeProvider _timeProvider;
    private readonly TDeviceNode _deviceNode;
    private readonly IReadOnlyList<TDataPointGroup> _dataPointGroups;
    private readonly IConnectionManager<TClient> _connectionManager;
    private readonly IDataPointValidator<IDataPoint>? _validator;
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;

    private TClient? _client;
    private List<DataPointGroupPollingJob<TDataPointGroup, TDataPointValue, TClient>> _pollingJobs = [];
    private CancellationTokenSource _cancellationSource;

    /// <summary>
    /// Event raised when new data values are received from the device and converted to external values.
    /// </summary>
    public event Action<IReadOnlyCollection<ExternalValue>>? Received;

    /// <summary>
    /// Initializes a new instance of the IncomingDataPortBase class.
    /// </summary>
    /// <param name="deviceNode">The device configuration node containing connection details.</param>
    /// <param name="dataPointGroups">The groups of data points to poll.</param>
    /// <param name="logger">Logger for data port operations.</param>
    /// <param name="loggerFactory">Factory for creating loggers for sub-components.</param>
    /// <param name="connectionManager">Manager for acquiring and releasing client connections.</param>
    /// <param name="validator">Optional validator for data points.</param>
    /// <param name="timeProvider">Optional time provider for testing purposes.</param>
    protected IncomingDataPortBase(
        TDeviceNode deviceNode,
        IReadOnlyList<TDataPointGroup> dataPointGroups,
        ILogger logger,
        ILoggerFactory loggerFactory,
        IConnectionManager<TClient> connectionManager,
        IDataPointValidator<IDataPoint>? validator = null,
        TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _deviceNode = deviceNode;
        _dataPointGroups = dataPointGroups;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _connectionManager = connectionManager;
        _validator = validator;
        _cancellationSource = new CancellationTokenSource();
    }

    /// <summary>
    /// Creates read client adapter from connected TClient instance.
    /// </summary>
    protected abstract IReadClient<TDataPointGroup, TDataPointValue> CreateReadClient(TClient client);

    /// <summary>
    /// Extracts device address for logging from device node.
    /// </summary>
    protected abstract string GetDeviceAddress(TDeviceNode deviceNode);

    /// <summary>
    /// Gets list of all data points from all groups for validation.
    /// </summary>
    protected abstract IReadOnlyList<IDataPoint> GetAllDataPoints(IReadOnlyList<TDataPointGroup> dataPointGroups);

    /// <summary>
    /// Gets transferred channels count from data point value (implementation-specific).
    /// </summary>
    protected abstract int GetTransferredChannelsCount(TDataPointValue dataPointValue);

    /// <summary>
    /// Adds external values for a single data point (implementation-specific).
    /// </summary>
    protected abstract void AddExternalValuesForDataPoint(
        List<ExternalValue> result,
        TDataPointValue dataPointValue,
        DateTime timestamp);

    /// <summary>
    /// Extracts poll frequency from data point group (implementation-specific).
    /// </summary>
    protected abstract TimeSpan GetPollFrequency(TDataPointGroup group);

    /// <summary>
    /// Template method: Connect to external system and start polling.
    /// Orchestrates connection, validation, and polling job creation.
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        var deviceAddress = GetDeviceAddress(_deviceNode);
        _logger.ConnectingToDevice(deviceAddress);

        // Strategy hook: Acquire connected client (pool or direct)
        _client = await _connectionManager.AcquireConnectedAsync(cancellationToken).ConfigureAwait(false);

        // Optional hook: Validate data points
        await ValidateAsync(cancellationToken).ConfigureAwait(false);

        // Reset cancellation if needed (reconnection scenario)
        if (_cancellationSource.IsCancellationRequested)
        {
            _cancellationSource.Dispose();
            _cancellationSource = new CancellationTokenSource();
        }

        // Template method: Start polling jobs
        StartPollingAndPublishingDataPoints(_cancellationSource.Token);
    }

    /// <summary>
    /// Optional hook for runtime validation (default: no-op).
    /// Symbolic overrides to validate against TIA Portal symbols.
    /// </summary>
    protected virtual async ValueTask ValidateAsync(CancellationToken cancellationToken)
    {
        if (_client == null)
            throw new InvalidOperationException("Client not acquired");

        if (_validator is null)
            return;

        try
        {
            var allDataPoints = GetAllDataPoints(_dataPointGroups);
            _validator.Validate(_client, allDataPoints, _logger);
        }
        catch (ConnectionFailureException)
        {
            await DisconnectAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Template method: Disconnect from external system and stop polling.
    /// </summary>
    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        var deviceAddress = GetDeviceAddress(_deviceNode);
        _logger.DisconnectingFromDevice(deviceAddress);

        await _cancellationSource.CancelAsync().ConfigureAwait(false);
        await StopAllPeriodicJobs().ConfigureAwait(false);

        // Strategy hook: Release connection (pool or direct)
        if (_client != null)
        {
            await _connectionManager.ReleaseAsync(_client, cancellationToken).ConfigureAwait(false);
            _client = null;
        }
    }

    /// <summary>
    /// Creates polling jobs for each data point group.
    /// </summary>
    private void StartPollingAndPublishingDataPoints(CancellationToken cancellation)
    {
        _logger.CreatingPollingJobs(_dataPointGroups.Count);

        ArgumentNullException.ThrowIfNull(_client);
        var readClient = CreateReadClient(_client);

        var jobs = new List<DataPointGroupPollingJob<TDataPointGroup, TDataPointValue, TClient>>(_dataPointGroups.Count);
        for (var i = 0; i < _dataPointGroups.Count; i++)
        {
            jobs.Add(CreatePollingJob(_dataPointGroups[i], readClient, cancellation));
        }

        _pollingJobs = jobs;
        for (var i = 0; i < _pollingJobs.Count; i++)
        {
            _pollingJobs[i].StartPeriodicExecution();
        }

        _logger.PollingJobsCreatedAndStarted(_pollingJobs.Count);
    }

    /// <summary>
    /// Creates a single polling job for a data point group.
    /// </summary>
    private DataPointGroupPollingJob<TDataPointGroup, TDataPointValue, TClient> CreatePollingJob(
        TDataPointGroup group,
        IReadClient<TDataPointGroup, TDataPointValue> readClient,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(group);

        return new DataPointGroupPollingJob<TDataPointGroup, TDataPointValue, TClient>(
            GetPollFrequency(group),
            group,
            readClient,
            PublishDataPointValues,
            timeProvider: _timeProvider,
            logger: _loggerFactory.CreateLogger<DataPointGroupPollingJob<TDataPointGroup, TDataPointValue, TClient>>(),
            externalCancellation: cancellationToken);
    }

    /// <summary>
    /// Publishes data point values as external values via Received event.
    /// </summary>
    private void PublishDataPointValues(IReadOnlyList<TDataPointValue> dataPointValues)
    {
        var externalValues = ToExternalValues(dataPointValues);
        _logger.PublishingExternalValues(externalValues.Count, new FormattedExternalValues(externalValues));
        Received?.Invoke(externalValues);
    }

    /// <summary>
    /// Transforms data point values to external values with timestamp.
    /// Performance-optimized with pre-allocated capacity and indexed loops.
    /// </summary>
    private IReadOnlyList<ExternalValue> ToExternalValues(IReadOnlyList<TDataPointValue> dataPointValues)
    {
        var timestamp = _timeProvider.GetUtcNow().DateTime;

        // Pre-calculate capacity to avoid List resizing
        var capacity = 0;
        for (var i = 0; i < dataPointValues.Count; i++)
            capacity += GetTransferredChannelsCount(dataPointValues[i]);

        var result = new List<ExternalValue>(capacity);

        for (var i = 0; i < dataPointValues.Count; i++)
        {
            var dataPointValue = dataPointValues[i];
            AddExternalValuesForDataPoint(result, dataPointValue, timestamp);
        }

        return result;
    }

    /// <summary>
    /// Stops all polling jobs.
    /// </summary>
    private async Task StopAllPeriodicJobs()
    {
        _logger.StoppingPollingJobs(_pollingJobs.Count);

        var tasks = new Task[_pollingJobs.Count];
        for (var i = 0; i < _pollingJobs.Count; i++)
        {
            tasks[i] = _pollingJobs[i].StopPeriodicExecution();
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
        _logger.PollingJobsStopped();
    }

    /// <summary>
    /// Disposes all polling jobs.
    /// </summary>
    private async Task DisposeAllPeriodicJobs()
    {
        var tasks = new Task[_pollingJobs.Count];
        for (var i = 0; i < _pollingJobs.Count; i++)
        {
            tasks[i] = _pollingJobs[i].DisposeAsync().AsTask();
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting resources asynchronously.
    /// Stops all polling jobs, releases the client connection, and disposes of the connection manager.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _cancellationSource.CancelAsync().ConfigureAwait(false);
        await StopAllPeriodicJobs().ConfigureAwait(false);
        await DisposeAllPeriodicJobs().ConfigureAwait(false);
        _pollingJobs.Clear();

        if (_client != null)
        {
            await _connectionManager.ReleaseAsync(_client, CancellationToken.None).ConfigureAwait(false);
            _client = null;
        }

        await _connectionManager.DisposeAsync().ConfigureAwait(false);
        _cancellationSource.Dispose();
        GC.SuppressFinalize(this);
    }
}
