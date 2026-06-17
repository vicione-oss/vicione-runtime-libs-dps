using System.Collections.Generic;

namespace ViciOne.Suite.System;

internal static class DictionaryExtensions
{
    internal static void AddOrUpdate<TKey, TValue>(this Dictionary<TKey, List<TValue>> dictionary, TKey key, TValue value) where TKey : notnull
    {
        if (dictionary.TryGetValue(key, out var runtimeLinks))
            runtimeLinks.Add(value);
        else
            dictionary[key] = [value,];
    }
}
