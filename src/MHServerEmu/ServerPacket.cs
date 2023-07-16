using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu
{
    class ServerPacket
    {
        // Set ClientPacket for information on the structure
        private ushort _muxId;
        private byte _byte3 = 0x00;
        private byte _byte4 = 0x00;
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
                        binaryWriter.Write(Convert.ToByte(_body.Length));
                        binaryWriter.Write(_byte3);
                        binaryWriter.Write(_byte4);
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

        public void WriteMessage(byte messageId, byte[] message, long timestamp = 0)
        {
            using (MemoryStream memoryStream = new())
            {
                using (BinaryWriter binaryWriter = new(memoryStream))
                {
                    binaryWriter.Write(messageId);

                    if (timestamp != 0)
                    {
                        // This doesn't work
                        binaryWriter.Write(Convert.ToByte(message.Length));
                        binaryWriter.Write(timestamp);
                    }
                    else
                    {
                        binaryWriter.Write(Convert.ToByte(message.Length));
                    }

                    binaryWriter.Write(message);

                    _body = memoryStream.ToArray();
                }
            }
        }
    }
}
