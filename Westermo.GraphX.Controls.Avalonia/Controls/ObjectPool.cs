using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Westermo.GraphX.Controls.Controls;

/// <summary>
/// Simple object pool for reducing allocations in hot paths.
/// Thread-safe for concurrent access.
/// </summary>
/// <typeparam name="T">Type of objects to pool</typeparam>
public sealed class SimplePool<T> where T : class, new()
{
    private readonly ConcurrentBag<T> _pool = new();
    private readonly int _maxSize;

    public SimplePool(int maxSize = 100)
    {
        _maxSize = maxSize;
    }

    public T Rent()
    {
        return _pool.TryTake(out var item) ? item : new T();
    }

    public void Return(T item)
    {
        if (_pool.Count < _maxSize)
        {
            _pool.Add(item);
        }
    }
}

/// <summary>
/// Pooled list that can be rented and returned to reduce allocations.
/// </summary>
public static class ListPool<T>
{
    private static readonly SimplePool<List<T>> Pool = new();

    public static List<T> Rent()
    {
        var list = Pool.Rent();
        list.Clear();
        return list;
    }

    public static void Return(List<T> list)
    {
        list.Clear();
        Pool.Return(list);
    }
}

/// <summary>
/// Pooled dictionary that can be rented and returned to reduce allocations.
/// </summary>
public static class DictionaryPool<TKey, TValue> where TKey : notnull
{
    private static readonly SimplePool<Dictionary<TKey, TValue>> Pool = new();

    public static Dictionary<TKey, TValue> Rent()
    {
        var dict = Pool.Rent();
        dict.Clear();
        return dict;
    }

    public static void Return(Dictionary<TKey, TValue> dict)
    {
        dict.Clear();
        Pool.Return(dict);
    }
}
