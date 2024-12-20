using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using tgv_core.extensions;
using tgv_core.imp;

namespace tgv_core.api;

public interface IExtensionFactoryInternal
{
    /// <summary>
    /// Create or refresh extension.
    /// Called on regular route chain
    /// </summary>
    /// <param name="ctx">HTTP request</param>
    /// <returns></returns>
    Task FillContext(Context ctx);
}

public interface IExtensionProvider<T> : IExtensionFactoryInternal, IAsyncEnumerable<(Context, T)>
{
    /// <summary>
    /// Provide extension from HTTP request.
    /// </summary>
    /// <param name="context">HTTP request</param>
    /// <returns></returns>
    Task<T?> GetOrCreate(Context context);
}

/// <summary>
/// Simple extension implementation. 
/// </summary>
/// <typeparam name="TKey">Key type</typeparam>
/// <typeparam name="TExtension">Any addition payload class</typeparam>
public abstract class ExtensionFactory<TExtension, TKey> : IExtensionProvider<TExtension>
    where TExtension : class
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// <see cref="GetKey"/> => pending tasks to track context generation
    /// </summary>
    private readonly ConcurrentDictionary<TKey, Task<TExtension?>> _tasks = new();

    /// <summary>
    /// <see cref="GetKey"/> -> context storage
    /// </summary>
    private readonly ConcurrentDictionary<TKey, TExtension?> _payloads = new();

    /// <summary>
    /// <see cref="GetKey"/> -> cache policy storage
    /// </summary>
    private readonly ConcurrentDictionary<TKey, CachePolicy<TExtension>> _policies = new();

    /// <summary>
    /// <see cref="GetKey"/> => context reference storage
    /// </summary>
    private readonly ConcurrentDictionary<TKey, WeakReference<Context>> _refs = new();

    /// <summary>
    /// return all active extensions
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async IAsyncEnumerator<(Context, TExtension)> GetAsyncEnumerator(
        CancellationToken cancellationToken = new CancellationToken())
    {
        // local copy of keys
        var allKeys = _payloads.Keys.ToList();

        foreach (var key in allKeys)
        {
            // payload is not valid
            if (!_payloads.TryGetValue(key, out var payload) || payload == null)
                continue;

            // HTTP context is not existing
            if (!_refs.TryGetValue(key, out var reference) || !reference.TryGetTarget(out var context) ||
                context == null)
                continue;

            // checking if context expired due to policies
            if (await CheckPolicies(key, payload, context)) yield return (context, payload);

            // remove key otherwise
            else RemoveKey(key);
        }
    }

    /// <summary>
    /// Returns uniq key for context.
    /// Different implementation may use session ID / user name / traceID / etc.
    /// Calls rapidly, good perfomace required
    /// </summary>
    /// <param name="context">HTTP request</param>
    protected abstract TKey? GetKey(Context context);
    
    /// <summary>
    /// Returns uniq key for extension. Should match with <see cref="GetKey(Context)"/>
    /// </summary>
    /// <param name="context">Extension object</param>
    protected abstract TKey GetKey(TExtension context);

    /// <summary>
    /// Creates new context for HTTP request. Called once for generated key (<see cref="GetKey(Context)"/>)
    /// </summary>
    /// <param name="context">HTTP request</param>
    /// <param name="key">Generated key</param>
    /// <returns></returns>
    protected abstract Task<TExtension?> GetOrCreateInternal(Context context, TKey key);

    /// <summary>
    /// Creates default cache policy for payload. <p/>
    /// <p/>
    /// For example: StoreType.Assign2Context stores item with context and destory it with context the same time
    ///  StoreType.Custom uses validating function
    /// </summary>
    /// <param name="context">HTTP request</param>
    /// <param name="payload">Created additional context</param>
    /// <returns></returns>
    protected virtual CachePolicy<TExtension>? CreateCachePolicy(Context context, TExtension payload) => new(StoreType.Assign2Context);

    /// <summary>
    /// Creates context from current request
    /// </summary>
    /// <param name="context">HTTP request</param>
    public async Task<TExtension?> GetOrCreate(Context context)
    {
        TExtension? extension = default;
        CachePolicy<TExtension>? policy = default;
        TKey? key = GetKey(context);

        // error handling
        if (key == null) return null;

        // if any task pending exists
        if (_tasks.TryGetValue(key, out var task))
            return await task;

        if (_payloads.TryGetValue(key, out extension))
        {
            if (extension == null || await CheckPolicies(key, extension, context))
            {
                return OnResolved(key, context, extension);
            }

            context.Logger.Debug("Context expired, created new one");
        }

        RemoveKey(key);

        try
        {
            // creating pending task to avoid multiple requests
            task = _tasks[key] = GetOrCreateInternal(context, key);
            // waiting for result
            extension = await task;
        }
        catch (Exception e)
        {
            context.Logger.Error("Error during context calculation: {e}", e);
            RemoveKey(key);
            return OnResolved(key, context, null);
        }
        finally
        {
            // remove penging task from storage
            _tasks.TryRemove(key, out _);
        }
        
        if (extension != null)
        {
            policy = CreateCachePolicy(context, extension);
            key = GetKey(extension);
        }
        
        _ = Add(key, extension, policy, context);
        return OnResolved(key, context, extension);
    }

    /// <summary>
    /// Checking current context storage policies
    /// </summary>
    /// <param name="key">Comparable key</param>
    /// <param name="context">Already existing additional context</param>
    /// <param name="http">HTTP request</param>
    /// <returns>True - can use provided context. <p/>
    /// False - must create new context</returns>
    protected virtual async Task<bool> CheckPolicies(TKey key, TExtension context, Context http)
    {
        if (!_policies.TryGetValue(key, out var policy)) return true;

        // assotiated with context only
        if (policy.StoreType == StoreType.Assign2Context)
        {
            // same reference exists
            return _refs.TryGetValue(key, out var reference)
                   && reference.TryGetTarget(out var target)
                   && ReferenceEquals(target, http);
        }

        // should store whole application
        if (policy.StoreType == StoreType.Custom)
        {
            return policy?.IsAlive?.Invoke(http, context) == true;
        }

        return false;
    }

    /// <summary>
    /// Removes key from all storages
    /// </summary>
    /// <param name="key">comparable key</param>
    protected virtual bool RemoveKey(TKey key)
    {
        bool res = _tasks.TryRemove(key, out _);
        if (_payloads.TryRemove(key, out _)) res = true; 
        if (_policies.TryRemove(key, out _)) res = true; 
        if (_refs.TryRemove(key, out _)) res = true;

        if (res) AfterRemoval(key);
        
        return res;
    }

    /// <summary>
    /// Clear all data
    /// </summary>
    protected void Clear()
    {
        _tasks.Clear();
        _payloads.Clear();
        _policies.Clear();
        _refs.Clear();
    }

    protected virtual async Task<bool> Add(TKey key, TExtension? context = null, CachePolicy<TExtension>? policy = null, Context? http = null)
    {
        // removing pending task
        _tasks.TryRemove(key, out _);

        // checking policies
        if (context != null && http != null && !await CheckPolicies(key, context, http))
        {
            RemoveKey(key);
            return false;
        }

        _payloads[key] = context;

        if (policy != null)
            _policies[key] = policy;
        else
            _policies.TryRemove(key, out _);

        if (http != null)
            _refs[key] = new WeakReference<Context>(http);
        else
            _refs.TryRemove(key, out _);

        AfterAdd(key, context, policy, http);
        return true;
    }

    /// <summary>
    /// Called after extension was added
    /// </summary>
    /// <param name="key">Comparable key</param>
    /// <param name="context">Extension context</param>
    /// <param name="policy">Cache policy</param>
    /// <param name="http">HTTP request</param>
    protected virtual void AfterAdd(TKey key, TExtension? context = null, CachePolicy<TExtension>? policy = null,
        Context? http = null)
    {
        
    }

    /// <summary>
    /// Called after removal
    /// </summary>
    /// <param name="key">Comparable key</param>
    protected virtual void AfterRemoval(TKey key)
    {
        
    }

    /// <summary>
    /// Called after extension object was resolved for HTTP request.
    /// Can be used to enrich HTTP context with cookies
    /// </summary>
    /// <param name="key">Comparable key</param>
    /// <param name="http">HTTP request</param>
    /// <param name="context">Extension object</param>
    /// <returns></returns>
    protected virtual TExtension? OnResolved(TKey key, Context http, TExtension? context) => context;

    Task IExtensionFactoryInternal.FillContext(Context ctx) => GetOrCreate(ctx);
}