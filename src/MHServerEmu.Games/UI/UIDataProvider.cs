using System.Text;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.UI.Widgets;

namespace MHServerEmu.Games.UI
{
    public class UIDataProvider : ISerialize
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<(PrototypeId, PrototypeId), UISyncData> _dataDict = new();

        public Game Game { get; }
        public Region Region { get; }

        public UIDataProvider() { }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            uint numWidgets = (uint)_dataDict.Count;
            success &= Serializer.Transfer(archive, ref numWidgets);

            if (archive.IsPacking)
            {
                foreach (var kvp in _dataDict)
                {
                    PrototypeId widgetRef = kvp.Key.Item1;
                    PrototypeId contextRef = kvp.Key.Item2;
                    success &= Serializer.Transfer(archive, ref widgetRef);
                    success &= Serializer.Transfer(archive, ref contextRef);
                    success &= kvp.Value.Serialize(archive);
                }
            }
            else
            {
                for (uint i = 0; i < numWidgets; i++)
                {
                    PrototypeId widgetRef = PrototypeId.Invalid;
                    PrototypeId contextRef = PrototypeId.Invalid;
                    success &= Serializer.Transfer(archive, ref widgetRef);
                    success &= Serializer.Transfer(archive, ref contextRef);
                    success &= UpdateOrCreateUIWidget(widgetRef, contextRef, archive) != null;
                }
            }

            return success;
        }

        public void Decode(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            ulong numWidgets = stream.ReadRawVarint64();

            for (ulong i = 0; i < numWidgets; i++)
            {
                PrototypeId widgetRef = stream.ReadPrototypeRef<Prototype>();
                PrototypeId contextRef = stream.ReadPrototypeRef<Prototype>();
                OLD_UpdateOrCreateUIWidget(widgetRef, contextRef, stream, boolDecoder);
            }
        }

        public void Encode(CodedOutputStream stream, BoolEncoder boolEncoder)
        {
            stream.WriteRawVarint64((ulong)_dataDict.Count);
            foreach (var kvp in _dataDict)
            {
                stream.WritePrototypeRef<Prototype>(kvp.Key.Item1);
                stream.WritePrototypeRef<Prototype>(kvp.Key.Item2);
                kvp.Value.Encode(stream, boolEncoder);
            }
        }

        public void EncodeBools(BoolEncoder boolEncoder)
        {
            foreach (UISyncData uiData in _dataDict.Values)
                uiData.EncodeBools(boolEncoder);
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            foreach (var kvp in _dataDict)
            {
                string widgetName = GameDatabase.GetFormattedPrototypeName(kvp.Key.Item1);
                string contextName = GameDatabase.GetFormattedPrototypeName(kvp.Key.Item2);
                sb.AppendLine($"_dataDict[{widgetName}][{contextName}]: {kvp.Value}");
            }

            return sb.ToString();
        }

        public T GetWidget<T>(PrototypeId widgetRef, PrototypeId contextRef) where T: UISyncData
        {
            if (_dataDict.TryGetValue((widgetRef, contextRef), out UISyncData widget) == false)
                widget = AllocateUIWidget(widgetRef, contextRef);

            return widget as T;
        }

        public void DeleteWidget(PrototypeId widgetRef, PrototypeId contextRef)
        {
            if (_dataDict.Remove((widgetRef, contextRef)) == false)
                Logger.Warn($"DeleteWidget(): Widget not found, widgetRef={widgetRef}, contextRef={contextRef}");

            // todo: send a NetMessageUISyncDataRemove to clients when a widget is removed server-side
        }

        public void DeleteWidget(NetMessageUISyncDataRemove removeMessage)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a <see cref="UISyncData"/> instance of the appropriate subtype for the specified widget prototype.
        /// </summary>
        private UISyncData AllocateUIWidget(PrototypeId widgetRef, PrototypeId contextRef)
        {
            if (widgetRef == PrototypeId.Invalid)
                return Logger.WarnReturn<UISyncData>(null, "AllocateUIWidget(): widgetRef == PrototypeId.Invalid");

            if (_dataDict.ContainsKey((widgetRef, contextRef)))
                return Logger.WarnReturn<UISyncData>(null, $"AllocateUIWidget(): Widget already exists, widgetRef={widgetRef}, contextRef={contextRef}");

            MetaGameDataPrototype metaGameDataPrototype = GameDatabase.GetPrototype<MetaGameDataPrototype>(widgetRef);

            if (metaGameDataPrototype == null)
                return Logger.WarnReturn<UISyncData>(null, "AllocateUIWidget(): metaGameDataPrototype == null");

            UISyncData uiSyncData = metaGameDataPrototype switch
            {
                UIWidgetButtonPrototype             => new UIWidgetButton(this, widgetRef, contextRef),
                UIWidgetEntityIconsPrototype        => new UIWidgetEntityIconsSyncData(this, widgetRef, contextRef),
                UIWidgetGenericFractionPrototype    => new UIWidgetGenericFraction(this, widgetRef, contextRef),
                UIWidgetMissionTextPrototype        => new UIWidgetMissionText(this, widgetRef, contextRef),
                UIWidgetReadyCheckPrototype         => new UIWidgetReadyCheck(this, widgetRef, contextRef),
                _ => null
            };

            if (uiSyncData == null)
                return Logger.WarnReturn<UISyncData>(null, "AllocateUIWidget(): Trying to allocate widget of the base type");

            _dataDict.Add((widgetRef, contextRef), uiSyncData);
            return uiSyncData;
        }

        private UISyncData UpdateOrCreateUIWidget(PrototypeId widgetRef, PrototypeId contextRef, Archive archive)
        {
            if (_dataDict.TryGetValue((widgetRef, contextRef), out UISyncData uiData) == false)
                uiData = AllocateUIWidget(widgetRef, contextRef);

            uiData.Serialize(archive);
            uiData.UpdateUI();

            return uiData;
        }


        private UISyncData OLD_UpdateOrCreateUIWidget(PrototypeId widgetRef, PrototypeId contextRef, CodedInputStream stream, BoolDecoder boolDecoder)
        {
            if (_dataDict.TryGetValue((widgetRef, contextRef), out UISyncData uiData) == false)
                uiData = AllocateUIWidget(widgetRef, contextRef);

            uiData.Decode(stream, boolDecoder);
            uiData.UpdateUI();

            return uiData;
        }
    }
}
