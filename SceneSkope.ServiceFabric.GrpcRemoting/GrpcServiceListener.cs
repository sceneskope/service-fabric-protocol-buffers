using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using System;
using System.Collections.Generic;
using System.Fabric;
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
        public IEnumerable<ServerServiceDefinition> Services { get; }

        public StatefulServiceContext Context { get; }

        public ILogger Log { get; }

        public string EndpointName { get; }

        public GrpcServiceListener(StatefulServiceContext context, ILogger logger, IEnumerable<ServerServiceDefinition> services,
            string endpointName = "GrpcServiceEndpoint")
        {
            Context = context;
            Log = logger;
            Services = services;
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

        public async Task<string> OpenAsync(CancellationToken cancellationToken)
        {
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
                var server = new Server
                {
                    Ports = { new ServerPort(host, port, ServerCredentials.Insecure) }
                };
                foreach (var service in Services)
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
        }
    }
}
