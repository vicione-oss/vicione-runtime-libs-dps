using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.Suite.DataPort.Extensions.Exceptions;

namespace ViciOne.Suite.DataPort.Extensions.Incoming;

/// <summary>
/// Manages connection lifecycle for IncomingDataPort clients.
/// Abstracts connection patterns.
/// </summary>
/// <typeparam name="TClient">Client type (IClient or derived)</typeparam>
/// <remarks>
/// <para>
/// This interface provides a unified abstraction for different connection management strategies:
/// </para>
/// <para>
/// <b>Implementation Notes:</b>
/// </para>
/// </remarks>
public interface IConnectionManager<TClient> : IAsyncDisposable
    where TClient : class
{
    /// <summary>
    /// Acquires a connected client.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Connected client instance</returns>
    /// <exception cref="ConnectionFailureException">Thrown when connection cannot be established</exception>
    /// <exception cref="OperationCanceledException">Thrown when operation is canceled</exception>
    Task<TClient> AcquireConnectedAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Releases the client connection.
    /// </summary>
    /// <param name="client">Client instance to release</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous release operation</returns>
    Task ReleaseAsync(TClient client, CancellationToken cancellationToken);
}
