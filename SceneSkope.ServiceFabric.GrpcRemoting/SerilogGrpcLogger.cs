using Grpc.Core.Logging;
using System;

namespace SceneSkope.ServiceFabric.GrpcRemoting
{
    public class SerilogGrpcLogger : global::Grpc.Core.Logging.ILogger
    {
        public Serilog.ILogger Log { get; }

        public SerilogGrpcLogger(Serilog.ILogger logger)
        {
            Log = logger;
        }

#pragma warning disable Serilog004 // Constant MessageTemplate verifier
        public void Debug(string message) => Log.Debug(message);

        public void Debug(string format, params object[] formatArgs) => Log.Debug(format, formatArgs);

        public void Error(string message) => Log.Error(message);

        public void Error(string format, params object[] formatArgs) => Log.Error(format, formatArgs);

        public void Error(Exception exception, string message) => Log.Error(exception, message);

        public ILogger ForType<T>() => new SerilogGrpcLogger(Log.ForContext<T>());

        public void Info(string message) => Log.Information(message);

        public void Info(string format, params object[] formatArgs) => Log.Information(format, formatArgs);

        public void Warning(string message) => Log.Warning(message);

        public void Warning(string format, params object[] formatArgs) => Log.Information(format, formatArgs);

        public void Warning(Exception exception, string message) => Log.Warning(exception, message);
#pragma warning restore Serilog004 // Constant MessageTemplate verifier

    }
}
