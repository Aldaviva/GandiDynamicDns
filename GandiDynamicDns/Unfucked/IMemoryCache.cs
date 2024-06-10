using System.Runtime.Caching;

// ReSharper disable InconsistentNaming

namespace GandiDynamicDns.Unfucked;

public interface IMemoryCache<T> where T: notnull {

    T GetOrAdd(string key, Func<T> valueCreator, CacheItemPolicy? policy = default, string? regionName = null);

    T GetOrAdd(string key, Func<T> valueCreator, TimeSpan evictAfterCreation, string? regionName = null);

    Task<T> GetOrAdd(string key, Func<Task<T>> valueCreator, CacheItemPolicy? policy = null, string? regionName = null);

    Task<T> GetOrAdd(string key, Func<Task<T>> valueCreator, TimeSpan evictAfterCreation, string? regionName = null);

    IEnumerator<T> GetEnumerator();

    bool Add(string key, T value, DateTimeOffset absoluteExpiration, string? regionName = null);

    bool Add(string key, T value, CacheItemPolicy policy, string? regionName = null);

    bool Add(CacheItem item, CacheItemPolicy policy);

    IDictionary<string, T>? GetValues(string regionName, params string[] keys);

    IDictionary<string, T> GetValues(IEnumerable<string> keys, string? regionName = null);

    CacheEntryChangeMonitor CreateCacheEntryChangeMonitor(IEnumerable<string> keys, string? regionName = null);

    void Dispose();

    long Trim(int percent);

    bool Contains(string key, string? regionName = null);

    T AddOrGetExisting(string key, T value, DateTimeOffset absoluteExpiration, string? regionName = null);

    CacheItem AddOrGetExisting(CacheItem item, CacheItemPolicy policy);

    T AddOrGetExisting(string key, T value, CacheItemPolicy policy, string? regionName = null);

    T? Get(string key, string? regionName = null);

    CacheItem GetCacheItem(string key, string? regionName = null);

    void Set(string key, T value, DateTimeOffset absoluteExpiration, string? regionName = null);

    void Set(CacheItem item, CacheItemPolicy policy);

    void Set(string key, T value, CacheItemPolicy policy, string? regionName = null);

    T? Remove(string key, string? regionName = null);

    T? Remove(string key, CacheEntryRemovedReason reason, string? regionName = null);

    long GetCount(string? regionName = null);

    long GetLastSize(string? regionName = null);

    long CacheMemoryLimit { get; }

    DefaultCacheCapabilities DefaultCacheCapabilities { get; }

    string Name { get; }

    long PhysicalMemoryLimit { get; }

    TimeSpan PollingInterval { get; }

    T? this[string key] { get; set; }

}