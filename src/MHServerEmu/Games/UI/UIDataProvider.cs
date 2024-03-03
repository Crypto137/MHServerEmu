using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.UI.Widgets;

namespace MHServerEmu.Games.UI
{
    public class UIDataProvider
    {
        public UISyncData[] UISyncData { get; set; }

        public UIDataProvider(UISyncData[] uiSyncData)
        {
            UISyncData = uiSyncData;
        }

        public UIDataProvider(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            UISyncData = new UISyncData[stream.ReadRawVarint32()];
            for (int i = 0; i < UISyncData.Length; i++)
            {
                PrototypeId widgetR = stream.ReadPrototypeEnum<Prototype>();
                PrototypeId contextR = stream.ReadPrototypeEnum<Prototype>();

                PrototypeId[] areas = new PrototypeId[stream.ReadRawInt32()];
                for (int j = 0; j < areas.Length; j++)
                    areas[j] = stream.ReadPrototypeEnum<Prototype>();

                string className = GameDatabase.DataDirectory.GetPrototypeBlueprint(widgetR).RuntimeBindingClassType.Name;

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

        public void Encode(CodedOutputStream stream, BoolEncoder boolEncoder)
        {
            stream.WriteRawVarint32((uint)UISyncData.Length);
            foreach (UISyncData data in UISyncData)
                data.Encode(stream, boolEncoder);
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
