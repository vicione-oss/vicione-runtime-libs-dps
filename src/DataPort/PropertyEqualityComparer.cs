using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.Suite.DataPort;

public class PropertyEqualityComparer<TElement, TProperty>(Func<TElement, TProperty> propertySelector) : IEqualityComparer<TElement> where TProperty : notnull
{
    private readonly EqualityComparer<TProperty> _equalityComparer = EqualityComparer<TProperty>.Default;

    public bool Equals(TElement? x, TElement? y)
    {
        if (x is null && y is null)
            return true;
        if (x is null || y is null)
            return false;

        return _equalityComparer.Equals(propertySelector(x), propertySelector(y));
    }

    public int GetHashCode([DisallowNull] TElement obj)
        => _equalityComparer.GetHashCode(propertySelector(obj));
}
