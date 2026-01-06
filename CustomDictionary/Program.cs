using System;
using System.Dynamic;
using System.Runtime.CompilerServices;


public class Program
{
  public interface ICustomDictionary<TKey, TValue>
  {
    public void Add(TKey key, TValue value);
    public bool TryGetValue(TKey key, out TValue value);
    public bool RemoveKey(TKey key);
  }


  internal class Entry<TKey, TValue>
  {
    public TKey Key;
    public TValue Value;
    public Entry<TKey, TValue>? Next;

    public Entry(TKey key, TValue value)
    {
      Key = key;
      Value = value;
    }
  }
  

  public class CustomDictionary <TKey, TValue> : ICustomDictionary<TKey, TValue>
  {
    private Entry<TKey, TValue>?[] _buckets;
    private int _count;
    private const float LoadFactor = 0.75f;

    public CustomDictionary(int cap = 16){
      _buckets = new Entry<TKey, TValue>[cap];
    }

    public int GetBucketIndex(TKey key)
    {
      var hashcode = key!.GetHashCode();
      return Math.Abs(hashcode)% _buckets.Length;
    }

   void Add(TKey key, TValue value)
    {
      int index = GetBucketIndex(key);
        var entry = _buckets[index];
         while (entry != null)
        {
            if (entry.Key!.Equals(key))
                throw new ArgumentException("Duplicate key");

            entry = entry.Next;
        }

        var newEntry = new Entry<TKey, TValue>(key, value)
        {
            Next = _buckets[index]
        };

        _buckets[index] = newEntry;
        _count++;

        if (_count >= _buckets.Length * LoadFactor)
            Resize();
    }
    bool TryGetValue(TKey key, out TValue value)
    {
      int index = GetBucketIndex(key); 
      var entry = _buckets[index];

      while (entry != null)
      {
        if (entry.Key!.Equals(key))
        {
          value = entry.Value;
          return true;
        }
        entry = entry.Next;
      }

      value = default;
      return false;

    }
    bool RemoveKey(TKey key)
    {
      int index = GetBucketIndex(key); 
      Entry<TKey, TValue>? previous = null;
      var entry = _buckets[index];

      while(entry!= null)
      {
        if(entry.Key!.Equals(key))
        {
          if(previous == null)
          {
            _buckets[index] = entry.Next;
          }
          else
          {
            previous.Next = entry.Next;
          }
          _count--;
          return true;
        }
        previous = entry;
        entry = entry.Next;
      }

      return false;
    }

    private void Resize()
    {
        var oldBuckets = _buckets;
        _buckets = new Entry<TKey, TValue>[oldBuckets.Length * 2];
        _count = 0;

        foreach (var head in oldBuckets)
        {
            var entry = head;
            while (entry != null)
            {
                Add(entry.Key, entry.Value);
                entry = entry.Next;
            }
        }
    }

    void ICustomDictionary<TKey, TValue>.Add(TKey key, TValue value)
    {
      Add(key, value);
    }

    bool ICustomDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value)
    {
      return TryGetValue(key, out value);
    }

    bool ICustomDictionary<TKey, TValue>.RemoveKey(TKey key)
    {
      return RemoveKey(key);
    }
  }
  public static void Main()
    {
      ICustomDictionary<string, int> dict = new CustomDictionary<string, int>();
      dict.Add("one", 1);
      dict.Add("two", 2);
      dict.Add("three", 3);

      if (dict.TryGetValue("two", out int value))
      {
        Console.WriteLine($"Key: two, Value: {value}");
      }

      dict.RemoveKey("two");

      if (!dict.TryGetValue("two", out value))
      {
        Console.WriteLine("Key 'two' not found.");
      }
    }
}
