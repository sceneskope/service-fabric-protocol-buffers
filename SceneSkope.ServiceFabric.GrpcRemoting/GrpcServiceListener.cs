using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SceneSkope.ServiceFabric.GrpcRemoting
{
    public class GrpcServiceListener : ICommunicationListener
    {
        //
        // Summary:
        //     Services that will be exported by the server once started. Register a service
        //     with this server by adding its definition to this collection.
        public Func<CancellationToken, IEnumerable<ServerServiceDefinition>> ServicesFactory { get; }

        public StatefulServiceContext Context { get; }

        public ILogger Log { get; }

        public string EndpointName { get; }

        public IEnumerable<ChannelOption> ChannelOptions { get; }

        public GrpcServiceListener(StatefulServiceContext context, ILogger logger, Func<CancellationToken, IEnumerable<ServerServiceDefinition>> servicesFactory,
            string endpointName = "GrpcServiceEndpoint") : 
            this(context, logger, servicesFactory, Enumerable.Empty<ChannelOption>(), endpointName)
        { }

        public GrpcServiceListener(StatefulServiceContext context, ILogger logger, Func<CancellationToken, IEnumerable<ServerServiceDefinition>> servicesFactory,
            IEnumerable<ChannelOption> channelOptions,
            string endpointName = "GrpcServiceEndpoint")
        {
            Context = context;
            Log = logger;
            ServicesFactory = servicesFactory;
            ChannelOptions = channelOptions;
            EndpointName = endpointName;
        }

        public void Abort()
        {
            Log.LogDebug("Aborting server");
            StopServerAsync().Wait();
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            Log.LogDebug("Closing server");

            return StopServerAsync();
        }

        private Server _server;
        private CancellationTokenSource _serviceCancellationTokenSource;

        public async Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            _serviceCancellationTokenSource = new CancellationTokenSource();
            var endpoints = Context.CodePackageActivationContext.GetEndpoints();

            if (!endpoints.TryGetValue(EndpointName, out var serviceEndpoint))
            {
                Log.LogCritical("Failed to find endpoint for {EndpointName}", EndpointName);
                throw new ArgumentException($"No endpoint for {EndpointName}");
            }
            var port = serviceEndpoint.Port;
            var host = FabricRuntime.GetNodeContext().IPAddressOrFQDN;

            Log.LogDebug("Starting gRPC server on http://{Host}:{Port}", host, port);
            try
            {
                var server = new Server(ChannelOptions)
                {
                    Ports = { new ServerPort(host, port, ServerCredentials.Insecure) }
                };
                var services = ServicesFactory(_serviceCancellationTokenSource.Token);
                foreach (var service in services)
                {
                    server.Services.Add(service);
                }
                _server = server;
                server.Start();
                return $"http://{host}:{port}";
            }
            catch (Exception ex)
            {
                Log.LogError(ex, "Error starting server: {Exception}", ex.Message);
                await StopServerAsync().ConfigureAwait(false);

                throw;
            }
        }

        private Task StopServerAsync()
        {
            Log.LogDebug("Stopping gRPC server");
            return InternalStopServerAsync();
        }

        private async Task InternalStopServerAsync()
        {
            Log.LogDebug("Really stopping server - or at least trying");
            var serviceCancellationTokenSource = _serviceCancellationTokenSource;
            _serviceCancellationTokenSource = null;
            serviceCancellationTokenSource.Cancel();
            try
            {
                if (_server != null)
                {
                    await _server.KillAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Log.LogError(ex, "Failed to shutdown server: {Exception}", ex.Message);
            }
            Log.LogDebug("Probably shutdown");
            serviceCancellationTokenSource.Dispose();
        }
    }
}
