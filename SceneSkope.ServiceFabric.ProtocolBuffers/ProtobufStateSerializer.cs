using Google.Protobuf;
using Microsoft.ServiceFabric.Data;
using System.IO;

namespace SceneSkope.ServiceFabric.ProtocolBuffers
{
    public class ProtobufStateSerializer<T> : IStateSerializer<T>
        where T : class, IMessage<T>, new()
    {
        public T Read(BinaryReader binaryReader) => Read(new T(), binaryReader);

        public T Read(T baseValue, BinaryReader binaryReader)
        {
            var value = baseValue ?? new T();
            value.MergeFrom(binaryReader.BaseStream);
            return value;
        }

        public void Write(T value, BinaryWriter binaryWriter)
        {
            value.WriteTo(binaryWriter.BaseStream);
        }

        public void Write(T baseValue, T targetValue, BinaryWriter binaryWriter) => Write(targetValue, binaryWriter);
    }
}
