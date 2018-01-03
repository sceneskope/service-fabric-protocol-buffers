using Grpc.Core;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace SceneSkope.ServiceFabric.GrpcRemoting
{
    public class GrpcCommunicationClientCache<TKey, TClient> where TClient : ClientBase<TClient>
    {
        private ILogger Log { get; }

        private class Entry
        {
            public long LastUsedTicks;
            public ServicePartitionClient<GrpcCommunicationClient<TClient>> Client { get; }
            public Entry(ServicePartitionClient<GrpcCommunicationClient<TClient>> client)
            {
                Client = client;
            }
        }

        private readonly ConcurrentDictionary<TKey, Entry> _cache = new ConcurrentDictionary<TKey, Entry>();
        private readonly Func<TKey, ServicePartitionKey> _keyConverter;
        private readonly GrpcServiceProxyFactory<TClient> _factory;

        public GrpcCommunicationClientCache(ILogger logger, GrpcServiceProxyFactory<TClient> factory, Func<TKey, ServicePartitionKey> keyConverter)
        {
            Log = logger;
            _factory = factory;
            _keyConverter = keyConverter;
        }

        public ServicePartitionClient<GrpcCommunicationClient<TClient>> GetClient(TKey key)
        {
            var entry = _cache.GetOrAdd(key, CreateEntry);
            entry.LastUsedTicks = DateTime.UtcNow.Ticks;
            return entry.Client;
        }

        private Entry CreateEntry(TKey key)
        {
            Log.Debug("Create new entry for {@Key}", key);
            var partitionKey = _keyConverter(key);
            var client = _factory.CreateProxy(partitionKey);
            return new Entry(client);
        }

        public async Task ExecuteAsync(TKey key, Func<TClient, Task> handler)
        {
            var entry = _cache.GetOrAdd(key, CreateEntry);
            try
            {
                await entry.Client.InvokeWithRetryAsync(c => handler(c.Client)).ConfigureAwait(false);
                entry.LastUsedTicks = DateTime.UtcNow.Ticks;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error invoking: {Exception}", ex.Message);
                _cache.TryRemove(key, out var _);
                throw;
            }
        }

        public async Task<TRet> ExecuteAsync<TRet>(TKey key, Func<TClient, Task<TRet>> handler)
        {
            var entry = _cache.GetOrAdd(key, CreateEntry);
            try
            {
                var result = await entry.Client.InvokeWithRetryAsync(c => handler(c.Client)).ConfigureAwait(false);
                entry.LastUsedTicks = DateTime.UtcNow.Ticks;
                return result;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error invoking: {Exception}", ex.Message);
                _cache.TryRemove(key, out var _);
                throw;
            }
        }
    }
}
