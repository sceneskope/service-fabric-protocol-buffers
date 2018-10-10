using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Services.Communication.Client;

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
                        Log.LogInformation(exceptionInformation.Exception, "Throwing: {Exception}", exceptionInformation.Exception.Message);
                        result = new ExceptionHandlingThrowResult();
                        return true;

                    case StatusCode.Unavailable:
                    case StatusCode.Unknown:
                    case StatusCode.Cancelled:
                        Log.LogInformation(exceptionInformation.Exception, "Not transient exception: {Exception}, Retry {@Retry}", exceptionInformation.Exception.Message, retrySettings);
                        result = new ExceptionHandlingRetryResult(exceptionInformation.Exception, false, retrySettings, int.MaxValue);
                        return true;

                    default:
                        Log.LogInformation(exceptionInformation.Exception, "Unknown exception: {Exception}, Retry {@Retry}", exceptionInformation.Exception.Message, retrySettings);
                        result = new ExceptionHandlingThrowResult();
                        return true;
                }
            }
            else
            {
                Log.LogInformation(exceptionInformation.Exception, "Throwing: {Exception}", exceptionInformation.Exception.Message);
                result = new ExceptionHandlingThrowResult();
                return true;
            }
        }
    }
}
