using System.Buffers;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// Contains a serialized <see cref="IMessage"/>.
    /// </summary>
    public readonly struct MessageBuffer
    {
        public const int MaxSize = 2048;     // Client messages should be small
        public const uint InvalidMessageId = unchecked((uint)-1);

        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly ArrayPool<byte> BufferPool = ArrayPool<byte>.Create();

        private readonly byte[] _buffer;
        private readonly int _length;

        public uint MessageId { get; }

        /// <summary>
        /// Reads a <see cref="MessageBuffer"/> from the provided <see cref="Stream"/>.
        /// </summary>
        public MessageBuffer(Stream stream)
        {
            try
            {
                MessageId = CodedInputStream.ReadRawVarint32(stream);

                _length = (int)CodedInputStream.ReadRawVarint32(stream);
                if (_length > MaxSize)
                    throw new Exception($"Message length {_length} exceeded the max allowed length of {MaxSize}.");

                _buffer = BufferPool.Rent(_length);
                stream.Read(_buffer, 0, _length);
            }
            catch (Exception e)
            {
                MessageId = InvalidMessageId;

                _length = 0;
                _buffer = null;
                
                Logger.ErrorException(e, "Failed to read MessageBuffer");
            }
        }

        /// <summary>
        /// Deserializes this <see cref="MessageBuffer"/> as an <see cref="IMessage"/> using the <typeparamref name="T"/> protocol. Returns <see langword="null"/> if deserialization failed.
        /// </summary>
        /// <remarks>
        /// Because <see cref="Deserialize{T}"/> uses pooled buffers, it should only ever be called once for each <see cref="MessageBuffer"/> instance.
        /// </remarks>
        public IMessage Deserialize<T>() where T: Enum
        {
            if (_buffer == null) return Logger.WarnReturn<IMessage>(null, $"Deserialize(): _buffer == null");

            try
            {
                CodedInputStream cis = CodedInputStream.CreateInstance(_buffer, 0, _length);
                var parse = ProtocolDispatchTable.Instance.GetParseMessageDelegate(typeof(T), MessageId);
                return parse(cis);
            }
            catch (Exception e)
            {
                Logger.ErrorException(e, $"{nameof(Deserialize)}");
                return null;
            }
            finally
            {
                BufferPool.Return(_buffer);
            }
        }

        /// <summary>
        /// Deserializes this <see cref="MessageBuffer"/> as a <see cref="NetMessageReadyForGameJoin"/>. Returns <see langword="null"/> if deserialization failed.
        /// </summary>
        /// <remarks>
        /// Because <see cref="DeserializeReadyForGameJoin"/> uses pooled buffers, it should only ever be called once for each <see cref="MessageBuffer"/> instance.
        /// </remarks>
        public NetMessageReadyForGameJoin DeserializeReadyForGameJoin()
        {
            // NetMessageReadyForGameJoin contains a bug where wipesDataIfMismatchedInDb is marked as required but the client
            // doesn't include it. To avoid an exception we build a partial message from the data we receive.
            try
            {
                CodedInputStream cis = CodedInputStream.CreateInstance(_buffer, 0, _length);
                return NetMessageReadyForGameJoin.CreateBuilder().MergeFrom(cis).BuildPartial();
            }
            catch (Exception e)
            {
                Logger.ErrorException(e, $"{nameof(DeserializeReadyForGameJoin)}");
                return null;
            }
            finally
            {
                BufferPool.Return(_buffer);
            }
        }
    }
}
