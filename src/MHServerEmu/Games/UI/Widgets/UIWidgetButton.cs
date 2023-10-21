using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;

namespace MHServerEmu.Games.UI.Widgets
{
    public class UIWidgetButton : UISyncData
    {
        public ulong[] Callbacks { get; set; }      // PlayerGuid

        public UIWidgetButton(ulong widgetR, ulong contextR, ulong[] areas, CodedInputStream stream) : base(widgetR, contextR, areas)
        {
            Callbacks = new ulong[stream.ReadRawVarint64()];
            for (int i = 0; i < Callbacks.Length; i++)
                Callbacks[i] = stream.ReadRawVarint64();
        }

        public override void Encode(CodedOutputStream stream, BoolEncoder boolEncoder)
        {
            base.Encode(stream, boolEncoder);

            stream.WriteRawVarint64((ulong)Callbacks.Length);
            for (int i = 0; i < Callbacks.Length; i++)
                stream.WriteRawVarint64(Callbacks[i]);
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            for (int i = 0; i < Callbacks.Length; i++) sb.AppendLine($"Callback{i}: {Callbacks[i]}");
        }
    }
}
