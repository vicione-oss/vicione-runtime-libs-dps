using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.Suite.DataPort.Extensions.Exceptions;

namespace ViciOne.Suite.DataPort.Extensions.Incoming.Client;

/// <summary>
/// Protocol-agnostic client for reading dataPoint groups from external systems.
/// </summary>
/// <typeparam name="TDataPointGroup">Data point group type.</typeparam>
/// <typeparam name="TDataPointValue">Read result type with value</typeparam>
/// <remarks>
/// <para>
/// This interface abstracts the protocol-specific read operations for IncomingDataPort implementations.
/// </para>
/// </remarks>
public interface IReadClient<in TDataPointGroup, TDataPointValue>
{
    /// <summary>
    /// Reads data points from the external system for a given data point group.
    /// </summary>
    /// <param name="dataPointGroup">Data point group to read</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Read-only list of data point values with read results</returns>
    /// <exception cref="ConnectionFailureException">Thrown when connection to external system is lost during read operation</exception>
    /// <exception cref="OperationCanceledException">Thrown when operation is cancelled via cancellation token</exception>
    ValueTask<IReadOnlyList<TDataPointValue>> ReadAsync(TDataPointGroup dataPointGroup,
        CancellationToken cancellationToken);
}
