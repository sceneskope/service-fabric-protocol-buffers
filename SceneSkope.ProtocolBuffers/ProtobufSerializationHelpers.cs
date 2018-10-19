using Google.Protobuf;
using System;

namespace SceneSkope.ProtocolBuffers
{
    public static class ProtobufSerializationHelpers
    {
        public static ArraySegment<byte> SerializeTo<T>(T message, byte[] buffer)
            where T : IMessage<T>
        {
            var stream = new CodedOutputStream(buffer);
            message.WriteTo(stream);
            return new ArraySegment<byte>(buffer, 0, (int)stream.Position);
        }

        public static bool TryDeserialize<T>(in ArraySegment<byte> buffer, out T value, out Exception exception)
            where T : IMessage<T>, new()
        {
            try
            {
                value = new T();
                value.MergeFrom(buffer.Array, buffer.Offset, buffer.Count);
                exception = default;
                return true;
            }
            catch (Exception ex)
            {
                value = default;
                exception = ex;
                return false;
            }
        }
    }
}
