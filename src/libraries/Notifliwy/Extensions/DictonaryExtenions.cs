using System;
using System.Collections.Generic;

namespace Notifliwy.Extensions;

internal static class DictionaryExtensions
{
    public static void AddOrUpdate<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        TKey key,
        Func<TKey, TValue> addValueFactory,
        Func<TKey, TValue, TValue> updateValueFactory)
    {
        if (dictionary.TryGetValue(key, out var existingValue))
        {
            var newValue = updateValueFactory(key, existingValue);
            dictionary[key] = newValue;
        }
        else
        {
            var newValue = addValueFactory(key);
            dictionary.Add(key, newValue);
        }
    }
}