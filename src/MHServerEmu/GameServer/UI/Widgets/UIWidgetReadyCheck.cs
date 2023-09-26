using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Common.Extensions;

namespace MHServerEmu.GameServer.UI.Widgets
{
    public class UIWidgetReadyCheck : UISyncData
    {
        public PlayerReadyState[] PlayerReadyStates { get; set; }

        public UIWidgetReadyCheck(ulong widgetR, ulong contextR, ulong[] areas, CodedInputStream stream) : base(widgetR, contextR, areas)
        {
            PlayerReadyStates = new PlayerReadyState[stream.ReadRawVarint64()];
            for (int i = 0; i < PlayerReadyStates.Length; i++)
                PlayerReadyStates[i] = new(stream);
        }

        public override byte[] Encode(BoolEncoder boolEncoder)
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                WriteParentFields(cos);

                cos.WriteRawVarint64((ulong)PlayerReadyStates.Length);
                for (int i = 0; i < PlayerReadyStates.Length; i++)
                    cos.WriteRawBytes(PlayerReadyStates[i].Encode());

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            WriteParentString(sb);
            for (int i = 0; i < PlayerReadyStates.Length; i++) sb.AppendLine($"PlayerReadyState{i}: {PlayerReadyStates[i]}");
            return sb.ToString();
        }
    }

    public class PlayerReadyState
    {
        public ulong Index { get; set; }
        public string PlayerName { get; set; }
        public int ReadyCheck { get; set; }

        public PlayerReadyState(CodedInputStream stream)
        {
            Index = stream.ReadRawVarint64();
            PlayerName = stream.ReadRawString();
            ReadyCheck = stream.ReadRawInt32();
        }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint64(Index);
                cos.WriteRawString(PlayerName);
                cos.WriteRawInt32(ReadyCheck);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Index: {Index}");
            sb.AppendLine($"PlayerName: {PlayerName}");
            sb.AppendLine($"ReadyCheck: {ReadyCheck}");
            return sb.ToString();
        }
    }
}
