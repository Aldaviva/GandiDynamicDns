using System.Collections.Specialized;
using System.Runtime.Caching;
using MemoryCache = System.Runtime.Caching.MemoryCache;

// ReSharper disable InconsistentNaming

namespace GandiDynamicDns.Unfucked;

public class MemoryCache<T>(string name = "", NameValueCollection? config = null, bool ignoreConfigSection = false): IMemoryCache<T> where T: notnull {

    private readonly MemoryCache cache = new(name, config, ignoreConfigSection);

    #region New Methods

    public T GetOrAdd(string key, Func<T> valueCreator, CacheItemPolicy? policy = default, string? regionName = null) {
        return Get(key, regionName) ?? AddOrGetExisting(key, valueCreator(), policy ?? new CacheItemPolicy(), regionName);
    }

    public T GetOrAdd(string key, Func<T> valueCreator, TimeSpan evictAfterCreation, string? regionName = null) {
        return GetOrAdd(key, valueCreator, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now + evictAfterCreation }, regionName);
    }

    public async Task<T> GetOrAdd(string key, Func<Task<T>> valueCreator, CacheItemPolicy? policy = null, string? regionName = null) {
        return Get(key, regionName) ?? AddOrGetExisting(key, await valueCreator(), policy ?? new CacheItemPolicy(), regionName);
    }

    public async Task<T> GetOrAdd(string key, Func<Task<T>> valueCreator, TimeSpan evictAfterCreation, string? regionName = null) {
        return await GetOrAdd(key, valueCreator, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now + evictAfterCreation }, regionName);
    }

    #endregion

    #region Delegated

    public IEnumerator<T> GetEnumerator() {
        return ((IEnumerable<T>) cache).GetEnumerator();
    }

    public bool Add(string key, T value, DateTimeOffset absoluteExpiration, string? regionName = null) {
        return cache.Add(key, value, absoluteExpiration, regionName);
    }

    public bool Add(string key, T value, CacheItemPolicy policy, string? regionName = null) {
        return cache.Add(key, value, policy, regionName);
    }

    public IDictionary<string, T>? GetValues(string regionName, params string[] keys) {
        return cache.GetValues(regionName, keys) as IDictionary<string, T>;
    }

    public CacheEntryChangeMonitor CreateCacheEntryChangeMonitor(IEnumerable<string> keys, string? regionName = null) {
        return cache.CreateCacheEntryChangeMonitor(keys, regionName);
    }

    public void Dispose() {
        cache.Dispose();
    }

    public long Trim(int percent) {
        return cache.Trim(percent);
    }

    public bool Contains(string key, string? regionName = null) {
        return cache.Contains(key, regionName);
    }

    public bool Add(CacheItem item, CacheItemPolicy policy) {
        return cache.Add(item, policy);
    }

    public T AddOrGetExisting(string key, T value, DateTimeOffset absoluteExpiration, string? regionName = null) {
        return (T) cache.AddOrGetExisting(key, value, absoluteExpiration, regionName) ?? value;
    }

    public CacheItem AddOrGetExisting(CacheItem item, CacheItemPolicy policy) {
        return cache.AddOrGetExisting(item, policy);
    }

    public T AddOrGetExisting(string key, T value, CacheItemPolicy policy, string? regionName = null) {
        return (T) cache.AddOrGetExisting(key, value, policy, regionName) ?? value;
    }

    public T? Get(string key, string? regionName = null) {
        return (T?) cache.Get(key, regionName);
    }

    public CacheItem GetCacheItem(string key, string? regionName = null) {
        return cache.GetCacheItem(key, regionName);
    }

    public void Set(string key, T value, DateTimeOffset absoluteExpiration, string? regionName = null) {
        cache.Set(key, value, absoluteExpiration, regionName);
    }

    public void Set(CacheItem item, CacheItemPolicy policy) {
        cache.Set(item, policy);
    }

    public void Set(string key, T value, CacheItemPolicy policy, string? regionName = null) {
        cache.Set(key, value, policy, regionName);
    }

    public T? Remove(string key, string? regionName = null) {
        return (T?) cache.Remove(key, regionName);
    }

    public T? Remove(string key, CacheEntryRemovedReason reason, string? regionName = null) {
        return (T?) cache.Remove(key, reason, regionName);
    }

    public long GetCount(string? regionName = null) {
        return cache.GetCount(regionName);
    }

    public long GetLastSize(string? regionName = null) {
        return cache.GetLastSize(regionName);
    }

    public IDictionary<string, T> GetValues(IEnumerable<string> keys, string? regionName = null) {
        return (IDictionary<string, T>) cache.GetValues(keys, regionName);
    }

    public long CacheMemoryLimit => cache.CacheMemoryLimit;

    public DefaultCacheCapabilities DefaultCacheCapabilities => cache.DefaultCacheCapabilities;

    public string Name => cache.Name;

    public long PhysicalMemoryLimit => cache.PhysicalMemoryLimit;

    public TimeSpan PollingInterval => cache.PollingInterval;

    public T? this[string key] {
        get => (T?) cache[key];
        set => cache[key] = value;
    }

    #endregion

}