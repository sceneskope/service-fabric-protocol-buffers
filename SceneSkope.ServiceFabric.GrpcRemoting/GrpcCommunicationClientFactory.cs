using Grpc.Core;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Serilog;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SceneSkope.ServiceFabric.GrpcRemoting
{
    public class GrpcCommunicationClientFactory<TClient> :
        CommunicationClientFactoryBase<GrpcCommunicationClient<TClient>>
        where TClient : ClientBase<TClient>
    {
        private ILogger Log { get; }

        public GrpcCommunicationClientFactory(ILogger logger, IEnumerable<IExceptionHandler> exceptionHandlers = null,
            IServicePartitionResolver servicePartitionResolver = null,
            string traceId = null)
            : base(servicePartitionResolver, GetExceptionHandlers(logger, exceptionHandlers), traceId)
        {
            Log = logger;
            ClientConnected += GrpcCommunicationClientFactory_ClientConnected;
            ClientDisconnected += GrpcCommunicationClientFactory_ClientDisconnected;
        }

        private void GrpcCommunicationClientFactory_ClientDisconnected(object sender, CommunicationClientEventArgs<GrpcCommunicationClient<TClient>> e)
        {
            Log.Debug("Client {Hash} disconnected: {Target}, {Resolved}, {State}",
                e.Client.GetHashCode(), e.Client.Channel.Target, e.Client.Channel.ResolvedTarget, e.Client.Channel.State);
        }

        private void GrpcCommunicationClientFactory_ClientConnected(object sender, CommunicationClientEventArgs<GrpcCommunicationClient<TClient>> e)
        {
            Log.Debug("Client {Hash} connected: {Target}, {Resolved}, {State}",
                e.Client.GetHashCode(), e.Client.Channel.Target, e.Client.Channel.ResolvedTarget, e.Client.Channel.State);
        }

        protected override void AbortClient(GrpcCommunicationClient<TClient> client)
        {
            Log.Debug("Abort client for: {Target}, {Resolved}, {State}",
                client.Channel.Target, client.Channel.ResolvedTarget, client.Channel.State);
            client.Channel.ShutdownAsync().Wait();
        }

        protected override Task<GrpcCommunicationClient<TClient>> CreateClientAsync(string endpoint, CancellationToken cancellationToken)
        {
            Log.Debug("Creating client for {Endpoint}", endpoint);
            var channel = new Channel(endpoint.Replace("http://", string.Empty), ChannelCredentials.Insecure);
            var client = new GrpcCommunicationClient<TClient>(channel);
            Log.Debug("Created client for {Endpoint}: {Hash}", endpoint, client.GetHashCode());
            return Task.FromResult(client);
        }

        protected override bool ValidateClient(GrpcCommunicationClient<TClient> client) => client.Channel.State != ChannelState.Shutdown;

        protected override bool ValidateClient(string endpoint, GrpcCommunicationClient<TClient> client) =>
            client.Channel.Target.Equals(endpoint) && ValidateClient(client);

        private static IEnumerable<IExceptionHandler> GetExceptionHandlers(ILogger logger,
                    IEnumerable<IExceptionHandler> exceptionHandlers)
        {
            var handlers = new List<IExceptionHandler>();
            if (exceptionHandlers != null)
            {
                handlers.AddRange(exceptionHandlers);
            }
            handlers.Add(new GrpcExceptionHandler(logger));
            return handlers;
        }
    }
}
