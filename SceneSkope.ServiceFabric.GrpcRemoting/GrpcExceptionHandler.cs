using Grpc.Core;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Serilog;

namespace SceneSkope.ServiceFabric.GrpcRemoting
{
    public class GrpcExceptionHandler : IExceptionHandler
    {
        public ILogger Log { get; }

        public GrpcExceptionHandler(ILogger logger)
        {
            Log = logger;
        }

        public bool TryHandleException(ExceptionInformation exceptionInformation, OperationRetrySettings retrySettings, out ExceptionHandlingResult result)
        {
            if (exceptionInformation.Exception is RpcException rpcEx)
            {
                switch (rpcEx.Status.StatusCode)
                {
                    case StatusCode.Unavailable when rpcEx.Status.Detail == "Endpoint read failed":
                        Log.Information(exceptionInformation.Exception, "Throwing: {Exception}", exceptionInformation.Exception.Message);
                        result = null;
                        return false;

                    case StatusCode.Cancelled:
                    case StatusCode.Unavailable:
                        Log.Warning(exceptionInformation.Exception, "Not transient exception: {Exception}, Retry {@Retry}", exceptionInformation.Exception.Message, retrySettings);
                        result = new ExceptionHandlingRetryResult(exceptionInformation.Exception, false, retrySettings, int.MaxValue);
                        return true;

                    default:
                        Log.Information(exceptionInformation.Exception, "Transient exception: {Exception}, Retry {@Retry}", exceptionInformation.Exception.Message, retrySettings);
                        result = new ExceptionHandlingRetryResult(exceptionInformation.Exception, true, retrySettings, int.MaxValue);
                        return true;
                }
            }
            else
            {
                Log.Information(exceptionInformation.Exception, "Throwing: {Exception}", exceptionInformation.Exception.Message);
                result = null;
                return false;
            }
        }
    }
}
