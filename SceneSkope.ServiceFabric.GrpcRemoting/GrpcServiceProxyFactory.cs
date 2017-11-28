using Grpc.Core;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Serilog;
using System;

namespace SceneSkope.ServiceFabric.GrpcRemoting
{
    public class GrpcServiceProxyFactory<TClient> where TClient : ClientBase<TClient>
    {
        public ServicePartitionResolver ServicePartitionResolver { get; }
        public GrpcCommunicationClientFactory<TClient> CommunicationClientFactory { get; }
        public Uri ServiceUri { get; }

        public ILogger Log { get; }

        public GrpcServiceProxyFactory(ILogger logger, ServicePartitionResolver servicePartitionResolver = null, Uri serviceUri = null, string traceId = null)
        {
            Log = logger;
            ServicePartitionResolver = servicePartitionResolver ?? ServicePartitionResolver.GetDefault();
            ServiceUri = serviceUri;
            CommunicationClientFactory = new GrpcCommunicationClientFactory<TClient>(logger, null, ServicePartitionResolver, traceId);
        }

        public ServicePartitionClient<GrpcCommunicationClient<TClient>> CreateProxy(ServicePartitionKey partitionKey = null,
            TargetReplicaSelector targetReplicaSelector = TargetReplicaSelector.Default, string listenerName = null, OperationRetrySettings retrySettings = null) =>
            CreateProxy(ServiceUri, partitionKey, targetReplicaSelector, listenerName, retrySettings);

        public ServicePartitionClient<GrpcCommunicationClient<TClient>> CreateProxy(Uri serviceUri = null, ServicePartitionKey partitionKey = null,
            TargetReplicaSelector targetReplicaSelector = TargetReplicaSelector.Default, string listenerName = null, OperationRetrySettings retrySettings = null)
        {
            var realServiceUri = serviceUri ?? ServiceUri ?? throw new ArgumentNullException("A service URI must be specified");
            Log.Information("Create proxy for {Uri} {@Key}", realServiceUri, partitionKey);
            return new ServicePartitionClient<GrpcCommunicationClient<TClient>>(CommunicationClientFactory, realServiceUri, partitionKey,
                targetReplicaSelector, listenerName, retrySettings);
        }
    }
}
