using System;
using System.Collections.Generic;

namespace Westermo.GraphX.Common;

public static class CommonExtensions
{
    extension<TKey, TValue>(IDictionary<TKey, TValue> dictionary)
    {
        public void AddOrUpdate(TKey key, TValue value)
        {
            dictionary[key] = value;
        }
    }

    extension<T>(IEnumerable<T> list)
    {
        public void ForEach(Action<T> func)
        {
            foreach (var item in list)
                func(item);
        }
    }
}