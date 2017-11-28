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
            Log.Information("Client {Hash} disconnected: {Target}, {Resolved}, {State}",
                e.Client.GetHashCode(), e.Client.Channel.Target, e.Client.Channel.ResolvedTarget, e.Client.Channel.State);
        }

        private void GrpcCommunicationClientFactory_ClientConnected(object sender, CommunicationClientEventArgs<GrpcCommunicationClient<TClient>> e)
        {
            Log.Information("Client {Hash} connected: {Target}, {Resolved}, {State}",
                e.Client.GetHashCode(), e.Client.Channel.Target, e.Client.Channel.ResolvedTarget, e.Client.Channel.State);
        }

        protected override void AbortClient(GrpcCommunicationClient<TClient> client)
        {
            Log.Information("Abort client {Hash} for: {Target}, {Resolved}, {State}",
                client.GetHashCode(), client.Channel.Target, client.Channel.ResolvedTarget, client.Channel.State);
            client.Channel.ShutdownAsync().Wait();
        }

        protected override async Task<GrpcCommunicationClient<TClient>> CreateClientAsync(string endpoint, CancellationToken cancellationToken)
        {
            var channel = new Channel(endpoint.Replace("http://", string.Empty), ChannelCredentials.Insecure);
            await channel.ConnectAsync().ConfigureAwait(false);
            var client = new GrpcCommunicationClient<TClient>(channel);
            Log.Information("Create client for {Endpoint}: {Hash}", endpoint, client.GetHashCode());
            return client;
        }

        protected override bool ValidateClient(GrpcCommunicationClient<TClient> client) => client.Channel.State == ChannelState.Ready;

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
