using Westermo.GraphX.Controls.Controls;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

/// <summary>
/// Tests for the object pooling infrastructure used to reduce GC pressure.
/// </summary>
public class ObjectPoolTests
{
    [Test]
    public async Task ListPool_RentsEmptyList()
    {
        var list = ListPool<int>.Rent();
        
        await Assert.That(list).IsNotNull();
        await Assert.That(list.Count).IsEqualTo(0);
        
        ListPool<int>.Return(list);
    }

    [Test]
    public async Task ListPool_ReturnedListIsClearedOnReuse()
    {
        var list1 = ListPool<int>.Rent();
        list1.Add(1);
        list1.Add(2);
        list1.Add(3);
        ListPool<int>.Return(list1);
        
        var list2 = ListPool<int>.Rent();
        
        // Should be empty even if it's the same instance
        await Assert.That(list2.Count).IsEqualTo(0);
        
        ListPool<int>.Return(list2);
    }

    [Test]
    public async Task ListPool_CanRentMultipleLists()
    {
        var list1 = ListPool<int>.Rent();
        var list2 = ListPool<int>.Rent();
        var list3 = ListPool<int>.Rent();
        
        await Assert.That(list1).IsNotNull();
        await Assert.That(list2).IsNotNull();
        await Assert.That(list3).IsNotNull();
        
        // They should be different instances when pool is empty
        await Assert.That(list1).IsNotEqualTo(list2);
        await Assert.That(list2).IsNotEqualTo(list3);
        
        ListPool<int>.Return(list1);
        ListPool<int>.Return(list2);
        ListPool<int>.Return(list3);
    }

    [Test]
    public async Task DictionaryPool_RentsEmptyDictionary()
    {
        var dict = DictionaryPool<string, int>.Rent();
        
        await Assert.That(dict).IsNotNull();
        await Assert.That(dict.Count).IsEqualTo(0);
        
        DictionaryPool<string, int>.Return(dict);
    }

    [Test]
    public async Task DictionaryPool_ReturnedDictionaryIsClearedOnReuse()
    {
        var dict1 = DictionaryPool<string, int>.Rent();
        dict1["a"] = 1;
        dict1["b"] = 2;
        DictionaryPool<string, int>.Return(dict1);
        
        var dict2 = DictionaryPool<string, int>.Rent();
        
        await Assert.That(dict2.Count).IsEqualTo(0);
        
        DictionaryPool<string, int>.Return(dict2);
    }

    [Test]
    public async Task DictionaryPool_CanRentMultipleDictionaries()
    {
        var dict1 = DictionaryPool<string, int>.Rent();
        var dict2 = DictionaryPool<string, int>.Rent();
        
        await Assert.That(dict1).IsNotNull();
        await Assert.That(dict2).IsNotNull();
        await Assert.That(dict1).IsNotEqualTo(dict2);
        
        DictionaryPool<string, int>.Return(dict1);
        DictionaryPool<string, int>.Return(dict2);
    }

    [Test]
    public async Task ListPool_ReusesReturnedInstance()
    {
        var list1 = ListPool<int>.Rent();
        var originalCapacity = list1.Capacity;
        
        // Grow the list to increase capacity
        for (int i = 0; i < 100; i++)
            list1.Add(i);
        
        var grownCapacity = list1.Capacity;
        ListPool<int>.Return(list1);
        
        var list2 = ListPool<int>.Rent();
        
        // Should reuse the same instance with its grown capacity
        await Assert.That(list2.Capacity).IsEqualTo(grownCapacity);
        
        ListPool<int>.Return(list2);
    }

    [Test]
    public async Task ListPool_RentAfterReturn_ReturnsValidList()
    {
        // Rent and return a list
        var list1 = ListPool<int>.Rent();
        list1.Add(42);
        ListPool<int>.Return(list1);
        
        // Verify we can rent again and get a cleared list
        var list2 = ListPool<int>.Rent();
        await Assert.That(list2).IsNotNull();
        await Assert.That(list2.Count).IsEqualTo(0);
        ListPool<int>.Return(list2);
    }

    [Test]
    public async Task DictionaryPool_RentAfterReturn_ReturnsValidDictionary()
    {
        // Rent and return a dictionary
        var dict1 = DictionaryPool<string, int>.Rent();
        dict1["key"] = 42;
        DictionaryPool<string, int>.Return(dict1);
        
        // Verify we can rent again and get a cleared dictionary
        var dict2 = DictionaryPool<string, int>.Rent();
        await Assert.That(dict2).IsNotNull();
        await Assert.That(dict2.Count).IsEqualTo(0);
        DictionaryPool<string, int>.Return(dict2);
    }

    [Test]
    public async Task ListPool_WorksWithReferenceTypes()
    {
        var list = ListPool<string>.Rent();
        list.Add("hello");
        list.Add("world");
        
        await Assert.That(list.Count).IsEqualTo(2);
        
        ListPool<string>.Return(list);
        
        var list2 = ListPool<string>.Rent();
        await Assert.That(list2.Count).IsEqualTo(0);
        
        ListPool<string>.Return(list2);
    }

    [Test]
    public async Task DictionaryPool_WorksWithComplexTypes()
    {
        var dict = DictionaryPool<int, List<string>>.Rent();
        dict[1] = ["a", "b"];
        dict[2] = ["c"];
        
        await Assert.That(dict.Count).IsEqualTo(2);
        
        DictionaryPool<int, List<string>>.Return(dict);
        
        var dict2 = DictionaryPool<int, List<string>>.Rent();
        await Assert.That(dict2.Count).IsEqualTo(0);
        
        DictionaryPool<int, List<string>>.Return(dict2);
    }
}
