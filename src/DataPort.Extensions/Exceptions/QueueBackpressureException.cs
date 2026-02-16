using System;

namespace ViciOne.Suite.DataPort.Extensions.Exceptions;

/// <summary>
/// Exception thrown when a queue operation must be abandoned due to backpressure.
/// Indicates that the queue is full and new items are being added faster than they can be processed.
/// </summary>
/// <remarks>
/// <para>
/// This exception is thrown when a peeked queue item is detected to be no longer at the head
/// during retry processing. This occurs when the queue fills up during retries (backpressure condition),
/// and the queue's overflow strategy (DropOldest or DropNewest) removes the item being retried.
/// Continuing to retry a superseded item would block processing of newer items.
/// </para>
/// <para>
/// The typical handling strategy uses the Peek/MarkItemAsRead pattern:
/// <list type="number">
/// <item><description>Peek at the queue head without removing it</description></item>
/// <item><description>Attempt to process the item with retries</description></item>
/// <item><description>During retry, check if item is still at head (IsItemStillHead)</description></item>
/// <item><description>If not at head, throw QueueBackpressureException</description></item>
/// <item><description>Catch the exception, log the dropped item, and continue with next item</description></item>
/// <item><description>On success, mark item as read to remove it from queue</description></item>
/// </list>
/// </para>
/// <para>
/// This approach ensures that the system remains responsive and newer data can be processed,
/// trading off guaranteed delivery for system stability under load. The Peek/MarkItemAsRead pattern
/// is retry-safe because it only removes items that were successfully processed or explicitly abandoned.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var item = await queue.PeekAsync(cancellationToken);
/// if (item is null) continue;
///
/// try
/// {
///     await PublishWithRetryAsync(item, cancellationToken); // Includes IsItemStillHead check
///     await queue.MarkItemAsReadAsync(item, cancellationToken); // Success - remove item
/// }
/// catch (QueueBackpressureException)
/// {
///     logger.LogWarning("Item dropped due to backpressure");
///     // Item already removed from queue by overflow strategy, continue to next
/// }
/// </code>
/// </example>
#pragma warning disable CA1032 // Implement standard exception constructors - Exception requires specific parameters
public class QueueBackpressureException : Exception
{
}
