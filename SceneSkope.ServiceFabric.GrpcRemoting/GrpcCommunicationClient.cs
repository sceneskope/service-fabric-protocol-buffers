using Grpc.Core;
using Microsoft.ServiceFabric.Services.Communication.Client;
using System;
using System.Fabric;
using static SceneSkope.ServiceFabric.GrpcRemoting.ChannelCache;

namespace SceneSkope.ServiceFabric.GrpcRemoting
{
    public class GrpcCommunicationClient<TClient> : ICommunicationClient
        where TClient : ClientBase<TClient>
    {
        public Guid Id { get; } = Guid.NewGuid();
        public TClient Client { get; }
        public ChannelEntry ChannelEntry { get; }
        public string ConnectionAddress { get; }

        internal GrpcCommunicationClient(string connectionAddress, ChannelEntry channelEntry, TClient client)
        {
            Client = client;
            ChannelEntry = channelEntry;
            ConnectionAddress = connectionAddress;
        }

        public ResolvedServicePartition ResolvedServicePartition { get; set; }
        public string ListenerName { get; set; }
        public ResolvedServiceEndpoint Endpoint { get; set; }
    }
}
