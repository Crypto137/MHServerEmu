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
            ChannelProtoId = stream.ReadPrototypeEnum(PrototypeEnumType.All);
            IsSubscribed = boolDecoder.ReadBool(stream);
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

                cos.WritePrototypeEnum(ChannelProtoId, PrototypeEnumType.All);
                boolEncoder.WriteBuffer(cos);   // IsSubscribed

                cos.Flush();
                return ms.ToArray();
            }
        }

        public NetStructChatChannelFilterState ToNetStruct() => NetStructChatChannelFilterState.CreateBuilder().SetChannelProtoId(ChannelProtoId).SetIsSubscribed(IsSubscribed).Build();

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"ChannelProtoId: {GameDatabase.GetPrototypeName(ChannelProtoId)}");
            sb.AppendLine($"IsSubscribed: {IsSubscribed}");
            return sb.ToString();
        }
    }
}
