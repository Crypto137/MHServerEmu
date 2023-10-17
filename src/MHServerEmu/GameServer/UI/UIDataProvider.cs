using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.GameData;
using MHServerEmu.GameServer.UI.Widgets;

namespace MHServerEmu.GameServer.UI
{
    public class UIDataProvider
    {
        public UISyncData[] UISyncData { get; set; }

        public UIDataProvider(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            UISyncData = new UISyncData[stream.ReadRawVarint32()];
            for (int i = 0; i < UISyncData.Length; i++)
            {
                ulong widgetR = stream.ReadPrototypeId(PrototypeEnumType.All);
                ulong contextR = stream.ReadPrototypeId(PrototypeEnumType.All);

                ulong[] areas = new ulong[stream.ReadRawInt32()];
                for (int j = 0; j < areas.Length; j++)
                    areas[j] = stream.ReadPrototypeId(PrototypeEnumType.All);

                string className = GameDatabase.DataDirectory.GetPrototypeBlueprint(widgetR).RuntimeBinding;

                switch (className)
                {
                    case "UIWidgetButtonPrototype":
                        UISyncData[i] = new UIWidgetButton(widgetR, contextR, areas, stream);
                        break;

                    case "UIWidgetEntityIconsSyncDataPrototype":
                        UISyncData[i] = new UIWidgetEntityIconsSyncData(widgetR, contextR, areas, stream, boolDecoder);
                        break;

                    case "UIWidgetGenericFractionPrototype":
                        UISyncData[i] = new UIWidgetGenericFraction(widgetR, contextR, areas, stream, boolDecoder);
                        break;

                    case "UIWidgetMissionTextPrototype":
                        UISyncData[i] = new UIWidgetMissionText(widgetR, contextR, areas, stream);
                        break;

                    case "UIWidgetReadyCheckPrototype":
                        UISyncData[i] = new UIWidgetReadyCheck(widgetR, contextR, areas, stream);
                        break;

                    default:
                        throw new($"Unsupported UISyncData type {className}.");
                }
            }
        }

        public virtual byte[] Encode(BoolEncoder boolEncoder)
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint32((uint)UISyncData.Length);
                foreach (UISyncData data in UISyncData)
                    cos.WriteRawBytes(data.Encode(boolEncoder));

                cos.Flush();
                return ms.ToArray();
            }
        }

        public void EncodeBools(BoolEncoder boolEncoder)
        {
            foreach (UISyncData data in UISyncData)
                data.EncodeBools(boolEncoder);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            for (int i = 0; i < UISyncData.Length; i++) sb.AppendLine($"UISyncData{i}: {UISyncData[i]}");
            return sb.ToString();
        }
    }
}
