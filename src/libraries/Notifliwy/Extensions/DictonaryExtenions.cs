using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Notifliwy.Extensions;

internal static class DictionaryExtensions
{
    /// <inheritdoc cref="ConcurrentDictionary{TKey,TValue}.AddOrUpdate(TKey,System.Func{TKey,TValue},System.Func{TKey,TValue,TValue})"/>
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