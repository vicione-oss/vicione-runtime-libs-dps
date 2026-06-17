using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Suite.System.Runtime;

public sealed class ConcatArraysPooling<TValue> : IAggregatingPooling<TValue[]>
{
    public string Description => "Concatenates all current input instances.";
    public string Name => "Concat arrays pooling";

    public TValue[]? Aggregate(IEnumerable<TValue[]?> values)
        => [.. values
            .Where(e => e is not null)
            .SelectMany(e => e!)];
}
