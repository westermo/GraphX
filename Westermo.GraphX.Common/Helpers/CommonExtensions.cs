using System;
using System.Collections.Generic;

namespace Westermo.GraphX.Common
{
    public static class CommonExtensions
    {
        public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            dictionary[key] = value;
        }

        public static void ForEach<T>(this IEnumerable<T> list, Action<T> func)
        {
            foreach (var item in list)
                func(item);
        }
    }
}