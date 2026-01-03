

using System;
using System.Data;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CacheSystem
{
  public interface ICache<TKey, TValue>
  {
    public void Set(TKey key, TValue value, DateTime? expiry = null);
    public bool TryGet(TKey key, out TValue value);
    public void Remove(TKey key);
  }
  public interface IEvicationPolicy<TKey>
  {
    void KeyAccessed(TKey key);
    void KeyAdded(TKey key);
    TKey EvictKey();
    void KeyRemoved(TKey key);
  }

  public class CacheItem<T>
  {
    public T Value { get; set; }
    public DateTime? Expiry { get; set; }

    public CacheItem(T value, DateTime? expiry)
    {
      Value = value;
      if (expiry.HasValue)Expiry = expiry;
    }

    public bool IsExpired() => Expiry.HasValue && Expiry <= DateTime.UtcNow;
  }

  public interface ICachePresistance<TKey, TValue>
  {
    public void Save(Dictionary<TKey, CacheItem<TValue>> store);
    public Dictionary<TKey, CacheItem<TValue>> Load();
  }


  public class InMemoryCache<TKey, TValue> : ICache<TKey, TValue>
  {
    private readonly int _capacity;
    private Dictionary<TKey, CacheItem<TValue>> _store;

    private readonly IEvicationPolicy<TKey> _evicationPolicy;

    public InMemoryCache(int capacity, IEvicationPolicy<TKey> policy){
      _capacity = capacity;
      _store = new Dictionary<TKey, CacheItem<TValue>>();
      _evicationPolicy = policy;
    }

    public void Set(TKey key, TValue value, DateTime? span = null)
    {
      if (_store.ContainsKey(key))
      {
        _evicationPolicy.KeyAccessed(key);
        _store[key] = new CacheItem<TValue>(value, span);
        return;
      }

      if (_store.Count >= _capacity)
      {
        var evictKey = _evicationPolicy.EvictKey();
        _store.Remove(evictKey);
        _evicationPolicy.KeyRemoved(evictKey);

      }

      _store[key] = new CacheItem<TValue>(value, span);
      _evicationPolicy.KeyAdded(key);
        return;
    }
    public bool TryGet(TKey key, out TValue value)
    {
      if (!_store.TryGetValue(key, out CacheItem<TValue> item))
      {        
        value = default;
        return false;
      }
      if (_store[key].IsExpired())
      {
        value = default;
        _store.Remove(key);
        _evicationPolicy.KeyRemoved(key);
        return false;
      }

      value = _store[key].Value;
      _evicationPolicy.KeyAccessed(key);
      return true;
    }
    public void Remove(TKey key)
    {
      if(_store.TryGetValue(key, out CacheItem<TValue> item))
      {
        _store.Remove(key);
        _evicationPolicy.KeyRemoved(key);
      }
    }
  }


  public class LRUevictionPolicy<TKey> : IEvicationPolicy<TKey>
  {
    private readonly LinkedList<TKey> _lruList;
    private readonly Dictionary<TKey, LinkedListNode<TKey>> _nodeMap;
    public LRUevictionPolicy()
    {
      _lruList = new LinkedList<TKey>();
      _nodeMap = new Dictionary<TKey, LinkedListNode<TKey>>();
    }
    public void KeyAccessed(TKey key)
    {
      if (_nodeMap.TryGetValue(key, out LinkedListNode<TKey>? node))
      {
        _lruList.Remove(node);
        _lruList.AddFirst(node);
      }
    }
    public void KeyAdded(TKey key)
    {
      var node = new LinkedListNode<TKey>(key);
      _lruList.AddFirst(node);
      _nodeMap.Add(key, node);
    }
    public TKey EvictKey()
    {
      var lastKey = _lruList.Last!.Value;
      return lastKey;
    }
    public void KeyRemoved(TKey key)
    {
      if (!_nodeMap.TryGetValue(key, out LinkedListNode<TKey> node))
      {
        _lruList.Remove(node);
        _nodeMap.Remove(key);
      }
    }
  }


  public class LFUEvicationPolicy<Tkey> : IEvicationPolicy<Tkey>
  {
    private readonly Dictionary<Tkey, int> _countMap;
    public LFUEvicationPolicy()
    {
      _countMap = new Dictionary<Tkey, int>();
    }
    public Tkey EvictKey()
    {
      return _countMap.OrderBy(t => t.Value).First().Key;
    }

    public void KeyAccessed(Tkey key)
    {
      _countMap[key]++;
    }

    public void KeyAdded(Tkey key)
    {
      _countMap.Add(key, 1);
    }

    public void KeyRemoved(Tkey key)
    {
      _countMap.Remove(key);
    }
  }

  public class FileChachePersistance<Tkey, Tvalue> : ICachePresistance<Tkey, Tvalue>
  {

    private readonly string _filePath;

    public FileChachePersistance(string filePath)
    {
      _filePath = filePath;
    }
    public Dictionary<Tkey, CacheItem<Tvalue>> Load()
    {
      if (!File.Exists(_filePath))
      {
        return new();
      }
      var json = File.ReadAllText(_filePath);
      return JsonSerializer.Deserialize<Dictionary<Tkey, CacheItem<Tvalue>>>(json);
    }

    public void Save(Dictionary<Tkey, CacheItem<Tvalue>> store)
    {
      var text = JsonSerializer.Serialize(store);
      File.WriteAllText(_filePath,text);
    }
  }

  public class CacheSystem
  {
    public static void Main(string[] args)
    {
      ICache<string, string> cache = new InMemoryCache<string, string>(3, new LRUevictionPolicy<string>());

      cache.Set("key1", "value1", DateTime.UtcNow.AddSeconds(5));
      cache.Set("key2", "value2");
      cache.Set("key3", "value3");

      if (cache.TryGet("key1", out string value1))
      {
        Console.WriteLine($"Retrieved key1: {value1}");
      }
      else
      {
        Console.WriteLine("key1 not found or expired");
      }

      cache.Set("key4", "value4"); // This should evict key2 as per LRU policy

      if (cache.TryGet("key2", out string value2))
      {
        Console.WriteLine($"Retrieved key2: {value2}");
      }
      else
      {
        Console.WriteLine("key2 not found or expired");
      }
    }
  }
}
