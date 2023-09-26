using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;

namespace MHServerEmu.GameServer.UI.Widgets
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

        public override byte[] Encode(BoolEncoder boolEncoder)
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                WriteParentFields(cos);

                cos.WriteRawVarint64((ulong)Callbacks.Length);
                for (int i = 0; i < Callbacks.Length; i++)
                    cos.WriteRawVarint64(Callbacks[i]);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            WriteParentString(sb);
            for (int i = 0; i < Callbacks.Length; i++) sb.AppendLine($"Callback{i}: {Callbacks[i]}");
            return sb.ToString();
        }
    }
}
