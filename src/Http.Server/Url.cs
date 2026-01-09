using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.Suite.DataPort;

internal static class Url
{
    internal static string Combine<TElement>(IEnumerable<TElement> parts, Func<TElement, string> selector) where TElement : notnull
        => string.Join("/", parts.Select(p => selector(p)));
}
