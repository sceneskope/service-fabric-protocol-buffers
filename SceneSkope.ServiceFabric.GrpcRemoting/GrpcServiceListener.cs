using Grpc.Core;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Serilog;
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

        public GrpcServiceListener(StatefulServiceContext context, ILogger logger, IEnumerable<ServerServiceDefinition> services)
        {
            Context = context;
            Log = logger;
            Services = services;
        }

        public void Abort()
        {
            Log.Information("Aborting server");
            StopServerAsync().Wait();
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            Log.Information("Closing server");
            return StopServerAsync();
        }

        private Server _server;

        public async Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            var serviceEndpoint = Context.CodePackageActivationContext.GetEndpoint("GrpcServiceEndpoint");
            var port = serviceEndpoint.Port;
            var host = FabricRuntime.GetNodeContext().IPAddressOrFQDN;

            Log.Information("Starting gRPC server on http://{Host}:{Port}", host, port);
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
                Log.Error(ex, "Error starting server: {Exception}", ex.Message);
                await StopServerAsync().ConfigureAwait(false);

                throw;
            }
        }

        private async Task StopServerAsync()
        {
            try
            {
                await _server?.ShutdownAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to shutdown server: {Exception}", ex.Message);
            }
        }
    }
}
