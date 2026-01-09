using System.Collections.Generic;
using System.Linq;

namespace ViciOne.Suite.DataPort;

internal static class InfluxDbComparer
{
    public static bool TryGetMissingStructure(IReadOnlyCollection<Bucket> current, IReadOnlyCollection<Bucket> expected, out List<Bucket> missingBuckets)
    {
        missingBuckets = [];

        foreach (var expectedBucket in expected)
        {
            var currentBucket = current.FirstOrDefault(o => o.Name == expectedBucket.Name);

            if (currentBucket is null)
            {
                missingBuckets.Add(expectedBucket);
                continue;
            }
        }

        return missingBuckets.Count > 0;
    }
}
