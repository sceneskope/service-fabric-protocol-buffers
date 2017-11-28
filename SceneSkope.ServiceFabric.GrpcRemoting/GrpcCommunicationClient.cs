using Grpc.Core;
using Microsoft.ServiceFabric.Services.Communication.Client;
using System;
using System.Fabric;

namespace SceneSkope.ServiceFabric.GrpcRemoting
{
    public class GrpcCommunicationClient<TClient> : ICommunicationClient
        where TClient : ClientBase<TClient>
    {
        public TClient Client { get; }
        public Channel Channel { get; }

        internal GrpcCommunicationClient(Channel channel)
        {
            Client = (TClient)Activator.CreateInstance(typeof(TClient), channel);
            Channel = channel;
        }

        public ResolvedServicePartition ResolvedServicePartition { get; set; }
        public string ListenerName { get; set; }
        public ResolvedServiceEndpoint Endpoint { get; set; }
    }
}
