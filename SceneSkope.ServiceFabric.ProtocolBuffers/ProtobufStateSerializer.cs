using Google.Protobuf;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Diagnostics;
using System.IO;

namespace SceneSkope.ServiceFabric.ProtocolBuffers
{
    public static class ProtobufStateSerializer
    {
        private static void VerifySerializerCreatedOnStartup(StatefulService serviceBase)
        {
            var constructors = serviceBase.GetType().GetConstructors();
            var currentStackTrace = new StackTrace();
            for (var i = currentStackTrace.FrameCount - 1; i >= 0; i--)
            {
                var frame = currentStackTrace.GetFrame(i);
                var method = frame.GetMethod();
                for (var j = 0; j < constructors.Length; j++)
                {
                    var constructor = constructors[j];
                    if (constructor.MethodHandle == method.MethodHandle)
                    {
                        return;
                    }
                }
            }
            throw new InvalidOperationException("Serializer is not created from service constructor");
        }

        public static VerifiedStateManager RegisterProtobufStateSerializer<T>(this StatefulService service)
            where T : class, IMessage<T>, new()
        {
            VerifySerializerCreatedOnStartup(service);
            var verifiedStateManager = new VerifiedStateManager(service.StateManager);
            return verifiedStateManager.RegisterProtobufStateSerializer<T>();
        }

        public static VerifiedStateManager RegisterProtobufStateSerializer<T>(this VerifiedStateManager verifiedStateManager)
            where T : class, IMessage<T>, new()
        {
            verifiedStateManager.StateManager.TryAddStateSerializer(new ProtobufStateSerializer<T>());
            return verifiedStateManager;
        }

        public class VerifiedStateManager
        {
            public IReliableStateManager StateManager { get; }

            internal VerifiedStateManager(IReliableStateManager stateManager)
            {
                StateManager = stateManager;
            }
        }
    }

    internal class ProtobufStateSerializer<T> : IStateSerializer<T>
        where T : class, IMessage<T>, new()
    {
        public T Read(BinaryReader binaryReader) => Read(new T(), binaryReader);

        public T Read(T baseValue, BinaryReader binaryReader)
        {
            var length = binaryReader.ReadInt32();
            var buffer = binaryReader.ReadBytes(length);
            var value = baseValue ?? new T();
            value.MergeFrom(buffer);
            return value;
        }

        public void Write(T value, BinaryWriter binaryWriter)
        {
            var buffer = value.ToByteArray();
            binaryWriter.Write(buffer.Length);
            binaryWriter.Write(buffer);
        }

        public void Write(T baseValue, T targetValue, BinaryWriter binaryWriter) => Write(targetValue, binaryWriter);
    }
}
