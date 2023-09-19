using System.Text;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.GameData;

namespace MHServerEmu.GameServer.Entities.Options
{
    public class ChatChannelFilter
    {
        public ulong ChannelProtoId { get; set; }
        public bool IsSubscribed { get; set; }

        public ChatChannelFilter(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            ChannelProtoId = stream.ReadPrototypeId(PrototypeEnumType.All);
            if (boolDecoder.IsEmpty) boolDecoder.SetBits(stream.ReadRawByte());
            IsSubscribed = boolDecoder.ReadBool();
        }

        public ChatChannelFilter(ulong channelProtoId, bool isSubscribed)
        {
            ChannelProtoId = channelProtoId;
            IsSubscribed = isSubscribed;
        }

        public ChatChannelFilter(NetStructChatChannelFilterState netStruct)
        {
            ChannelProtoId = netStruct.ChannelProtoId;
            IsSubscribed = netStruct.IsSubscribed;
        }

        public byte[] Encode(BoolEncoder boolEncoder)
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WritePrototypeId(ChannelProtoId, PrototypeEnumType.All);

                byte bitBuffer = boolEncoder.GetBitBuffer();             // IsSubscribed
                if (bitBuffer != 0) cos.WriteRawByte(bitBuffer);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public NetStructChatChannelFilterState ToNetStruct() => NetStructChatChannelFilterState.CreateBuilder().SetChannelProtoId(ChannelProtoId).SetIsSubscribed(IsSubscribed).Build();

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"ChannelProtoId: {GameDatabase.GetPrototypePath(ChannelProtoId)}");
            sb.AppendLine($"IsSubscribed: {IsSubscribed}");
            return sb.ToString();
        }
    }
}
