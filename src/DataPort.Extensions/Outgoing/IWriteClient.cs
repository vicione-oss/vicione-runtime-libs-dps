using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing;

/// <summary>
/// Protocol-agnostic interface for writing data point values.
/// Implemented by I*Client keeps Common layer independent.
/// </summary>
/// <typeparam name="TDataPointValue">Data point value type</typeparam>
public interface IWriteClient<TDataPointValue>
{
    /// <summary>
    /// Writes a batch of data point values to the device.
    /// </summary>
    /// <param name="values">Values to write</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async task</returns>
    ValueTask WriteAsync(IReadOnlyList<TDataPointValue> values, CancellationToken cancellationToken);
}
