using MHServerEmu.Common;

namespace MHServerEmu.Networking
{
    class ServerPacket
    {
        // Set ClientPacket for information on the structure
        private ushort _muxId;
        private MuxCommand _muxCommand;

        private byte[] _body = new byte[] { };

        public byte[] Data
        {
            get
            {
                using (MemoryStream memoryStream = new())
                {
                    using (BinaryWriter binaryWriter = new(memoryStream))
                    {
                        binaryWriter.Write(_muxId);
                        binaryWriter.Write(_body.Length.ToUInt24ByteArray());
                        binaryWriter.Write((byte)_muxCommand);
                        binaryWriter.Write(_body);
                        return memoryStream.ToArray();
                    }
                }
            }
        }

        public ServerPacket(ushort muxId, MuxCommand command)
        {
            _muxId = muxId;
            _muxCommand = command;
        }

        public void WriteMessage(byte messageId, byte[] message, bool addExtraByte)
        {
            using (MemoryStream memoryStream = new())
            {
                using (BinaryWriter binaryWriter = new(memoryStream))
                {
                    binaryWriter.Write(messageId);

                    if (addExtraByte) binaryWriter.Write(Convert.ToByte(1));   // NetMessageInitialTimeSync needs an extra byte here for some reason
                    binaryWriter.Write(Convert.ToByte(message.Length));

                    binaryWriter.Write(message);

                    _body = memoryStream.ToArray();
                }
            }
        }
    }
}
