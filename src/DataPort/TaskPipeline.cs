using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.Suite.DataPort;

public class TaskPipeline<T>(int maxRetrys, Func<int, TimeSpan> getNextDelay, T outcomeWhereToRetry)
{
    public async ValueTask<T> ExecuteAsync(Func<CancellationToken, ValueTask<T>> callback, CancellationToken cancellationToken = default)
    {
        T result;

        for (var i = 0; i < maxRetrys; i++)
        {
            await Task.Delay(getNextDelay(i), cancellationToken);

            result = await callback(cancellationToken);

            if (result is not null && !result.Equals(outcomeWhereToRetry))
            {
                return result;
            }
        }

        return outcomeWhereToRetry;
    }
}
