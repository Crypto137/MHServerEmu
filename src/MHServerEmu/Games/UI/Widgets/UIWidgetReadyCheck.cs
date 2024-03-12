using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.UI.Widgets
{
    public class UIWidgetReadyCheck : UISyncData
    {
        public PlayerReadyState[] PlayerReadyStates { get; set; }

        public UIWidgetReadyCheck(PrototypeId widgetR, PrototypeId contextR, PrototypeId[] areas, CodedInputStream stream) : base(widgetR, contextR, areas)
        {
            PlayerReadyStates = new PlayerReadyState[stream.ReadRawVarint64()];
            for (int i = 0; i < PlayerReadyStates.Length; i++)
                PlayerReadyStates[i] = new(stream);
        }

        public override void Encode(CodedOutputStream stream, BoolEncoder boolEncoder)
        {
            base.Encode(stream, boolEncoder);

            stream.WriteRawVarint64((ulong)PlayerReadyStates.Length);
            for (int i = 0; i < PlayerReadyStates.Length; i++)
                PlayerReadyStates[i].Encode(stream);
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            for (int i = 0; i < PlayerReadyStates.Length; i++) sb.AppendLine($"PlayerReadyState{i}: {PlayerReadyStates[i]}");
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

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64(Index);
            stream.WriteRawString(PlayerName);
            stream.WriteRawInt32(ReadyCheck);
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
