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
            Log.Information(exceptionInformation.Exception, "Try handle exception: {Exception}, Retry {@Retry}", exceptionInformation.Exception.Message, retrySettings);
            if (exceptionInformation.Exception is RpcException)
            {
                result = new ExceptionHandlingRetryResult(exceptionInformation.Exception, true, retrySettings, retrySettings.DefaultMaxRetryCount);
                return true;
            }
            else
            {
                result = new ExceptionHandlingThrowResult { ExceptionToThrow = exceptionInformation.Exception };
                return false;
            }
        }
    }
}
