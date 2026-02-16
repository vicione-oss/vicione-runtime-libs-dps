using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.Suite.DataPort.Extensions.Exceptions;

namespace ViciOne.Suite.DataPort.Extensions.Client;

/// <summary>
/// Manages connection lifecycle for DataPort clients.
/// Abstracts connection patterns for both incoming and outgoing data ports.
/// </summary>
/// <typeparam name="TClient">Client type (IReadClient or IWriteClient or derived)</typeparam>
/// <typeparam name="TClientInformation">The information required to uniquely identify and create a client.</typeparam>
public interface IClientLifecycleManager<TClient, in TClientInformation>
    where TClientInformation : IClientInformation
{
    /// <summary>
    /// Acquires a connected client.
    /// </summary>
    /// <param name="clientInformation">An identifier for which device we want to create the connection for.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Connected client instance</returns>
    /// <exception cref="ConnectionFailureException">Thrown when connection cannot be established</exception>
    /// <exception cref="OperationCanceledException">Thrown when operation is canceled</exception>
    ValueTask<TClient> AcquireConnectedAsync(TClientInformation clientInformation, CancellationToken cancellationToken);

    /// <summary>
    /// Releases the client connection.
    /// </summary>
    /// <param name="clientInformation">An identifier for which device we to release the connection.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous release operation</returns>
    ValueTask ReleaseAsync(TClientInformation clientInformation, CancellationToken cancellationToken);
}
