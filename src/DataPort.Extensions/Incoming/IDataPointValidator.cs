using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using ViciOne.Suite.DataPort.Extensions.Exceptions;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

namespace ViciOne.Suite.DataPort.Extensions.Incoming;

/// <summary>
/// Validates data points against device configuration.
/// Optional hook - default implementation is no-op.
/// Symbolic overrides to validate against TIA Portal symbol table.
/// </summary>
/// <typeparam name="TDataPoint">Data point configuration type</typeparam>
/// <remarks>
/// <para>
/// This interface provides an optional validation hook for implementations that need runtime
/// validation of data point configurations against the target system.
/// </para>
/// <para>
/// The base class calls this via virtual ValidateConnectionAsync() method during connection establishment.
/// </para>
/// </remarks>
public interface IDataPointValidator<TDataPoint>
    where TDataPoint : IDataPoint
{
    /// <summary>
    /// Validates data points against device configuration.
    /// Throws ConnectionFailureException if validation fails.
    /// </summary>
    /// <typeparam name="TClient">Client type for device communication</typeparam>
    /// <param name="client">Connected client instance</param>
    /// <param name="dataPoints">Data points to validate</param>
    /// <param name="logger">Logger for validation diagnostics</param>
    /// <exception cref="ConnectionFailureException">Thrown when validation fails (e.g., symbol not found in TIA Portal)</exception>
    void Validate<TClient>(
        TClient client,
        IReadOnlyList<TDataPoint> dataPoints,
        ILogger logger)
        where TClient : class;
}

/// <summary>
/// Default no-op validator for implementations without runtime validation.
/// </summary>
/// <typeparam name="TDataPoint">Data point configuration type</typeparam>
/// <remarks>
/// This implementation is used where validation is not required
/// </remarks>
public sealed class NoOpDataPointValidator<TDataPoint> : IDataPointValidator<TDataPoint>
    where TDataPoint : IDataPoint
{
    /// <summary>
    /// No-op validation
    /// </summary>
    /// <typeparam name="TClient">Client type</typeparam>
    /// <param name="client">Connected client (unused)</param>
    /// <param name="dataPoints">Data points (unused)</param>
    /// <param name="logger">Logger (unused)</param>
    public void Validate<TClient>(TClient client, IReadOnlyList<TDataPoint> dataPoints, ILogger logger)
        where TClient : class
    {
        // No validation required
    }
}
