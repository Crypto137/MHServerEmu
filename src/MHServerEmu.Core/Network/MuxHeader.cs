using System.Runtime.InteropServices;

namespace MHServerEmu.Core.Network
{
    public enum MuxCommand : byte
    {
        Invalid,
        Connect,
        ConnectAck,
        Disconnect,
        ConnectWithData,
        Data
    }

    /// <summary>
    /// Data header for communicating over a mux channel.
    /// </summary>
    public readonly struct MuxHeader
    {
        // See CoreNetworkChannel::CreateMuxDataHeader() in the client for reference

        public const int Size = 6;

        public readonly ushort MuxId;
        public readonly int DataSize;
        public readonly MuxCommand Command;

        private MuxHeader(ushort muxId, int dataSize, MuxCommand command)
        {
            if ((dataSize & dataSize & 0xFF000000) != 0)
                throw new("Invalid data size. Must not exceed 24 bits.");

            MuxId = muxId;
            DataSize = dataSize;
            Command = command;
        }

        /// <summary>
        /// Constructs a <see cref="MuxHeader"/> from the provided data.
        /// </summary>
        public static MuxHeader FromData(ushort muxId, int dataSize, MuxCommand command)
        {
            return new(muxId, dataSize, command);
        }

        /// <summary>
        /// Reads a <see cref="MuxHeader"/> from the provided <see cref="Stream"/>.
        /// </summary>
        public static MuxHeader FromStream(Stream stream)
        {
            // Reading bytes individually is faster than using MemoryMarshal or Span<byte> (as of .NET 8)
            ushort muxId = (ushort)(stream.ReadByte() | (stream.ReadByte() << 8));
            int dataSize = stream.ReadByte() | (stream.ReadByte() << 8) | (stream.ReadByte() << 16);
            MuxCommand command = (MuxCommand)stream.ReadByte();

            return new(muxId, dataSize, command);
        }

        /// <summary>
        /// Writes this <see cref="MuxHeader"/> to the provided <see cref="Stream"/>.
        /// </summary>
        public void WriteTo(Stream stream)
        {
            // Writing bytes to a ulong and reinterpret casting it to a Span<byte> is faster than writing bytes individually (as of .NET 8)

            // Pack all of our data
            ulong bits = MuxId;
            bits |= (ulong)DataSize << 16;
            bits |= (ulong)Command << 40;

            // Reinterpret cast packed data as a byte span
            Span<byte> bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref bits, 1));
            
            // Write the bytes we need to the stream
            stream.Write(bytes[..Size]);
        }
    }
}
