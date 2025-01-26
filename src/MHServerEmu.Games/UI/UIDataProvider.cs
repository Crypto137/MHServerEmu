using System.Text;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Dialog;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.UI.Widgets;

namespace MHServerEmu.Games.UI
{
    public class UIDataProvider : ISerialize
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<(PrototypeId, PrototypeId), UISyncData> _dataDict = new();

        public Region Region { get => Owner as Region; }
        public Game Game { get; }
        public IUIDataProviderOwner Owner { get; }

        public UIDataProvider(Game game, IUIDataProviderOwner owner)
        {
            Game = game;
            Owner = owner;
        }

        public void Deallocate()
        {
            foreach (var widget in _dataDict.Values)
                widget?.Deallocate();

            _dataDict.Clear();
        }

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

        public T GetWidget<T>(PrototypeId widgetRef, PrototypeId contextRef = PrototypeId.Invalid) where T: UISyncData
        {
            if (_dataDict.TryGetValue((widgetRef, contextRef), out UISyncData widget) == false)
                widget = AllocateUIWidget(widgetRef, contextRef);

            return widget as T;
        }

        public void DeleteWidget(PrototypeId widgetRef, PrototypeId contextRef = PrototypeId.Invalid)
        {
            if (_dataDict.Remove((widgetRef, contextRef), out UISyncData widget))
                widget.Deallocate();

            var region = Region;
            if (region == null) return;

            var message = NetMessageUISyncDataRemove.CreateBuilder()
                .SetUiSyncDataProtoId((ulong)widgetRef)
                .SetContextProtoId((ulong)contextRef)
                .Build();

            Game?.NetworkManager.SendMessageToInterested(message, region);
        }

        public void OnUpdateUI(UISyncData uiSyncData)
        {
            var region = Region;
            if (region == null) return;

            ByteString buffer;
            using (var archive = new Archive(ArchiveSerializeType.Replication, (ulong)AOINetworkPolicyValues.AllChannels))
            {
                uiSyncData.Serialize(archive);
                buffer = archive.ToByteString();
            }

            var message = NetMessageUISyncDataUpdate.CreateBuilder()
                .SetUiSyncDataProtoId((ulong)uiSyncData.WidgetRef)
                .SetContextProtoId((ulong)uiSyncData.ContextRef)
                .SetBuffer(buffer)
                .Build();

            Game?.NetworkManager.SendMessageToInterested(message, region);
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

        public void OnEntityTracked(WorldEntity worldEntity, PrototypeId contextRef)
        {
            var metaGameProto = GameDatabase.GetPrototype<MetaGameDataPrototype>(contextRef);
            if (metaGameProto == null) return;
            var uiSyncData = FindWidget(worldEntity, contextRef);
            uiSyncData?.OnEntityTracked(worldEntity);
        }

        public void OnEntityLifecycle(WorldEntity worldEntity)
        {
            foreach (var kvp in worldEntity.TrackingContextMap)
                if (kvp.Value.HasFlag(EntityTrackingFlag.HUD))
                {
                    var widgetRef = kvp.Key;
                    var uiSyncData = FindWidget(worldEntity, widgetRef);
                    uiSyncData?.OnEntityLifecycle(worldEntity);
                }
        }

        private UISyncData FindWidget(WorldEntity worldEntity, PrototypeId contextRef)
        {
            if (_dataDict.TryGetValue((contextRef, PrototypeId.Invalid), out UISyncData widget)) return widget;
            if (_dataDict.TryGetValue((contextRef, worldEntity.MissionPrototype), out widget)) return widget;
            foreach(var kvp in _dataDict)
                if (kvp.Key.Item1 == contextRef) return kvp.Value;
            return null;
        }

        public void OnWidgetButtonResult(NetMessageWidgetButtonResult widgetButtonResult)
        {
            PrototypeId widgetRef = (PrototypeId)widgetButtonResult.WidgetRefId;
            PrototypeId contextRef = (PrototypeId)widgetButtonResult.WidgetContextRefId;
            var button = GetWidget<UIWidgetButton>(widgetRef, contextRef);
            button?.DoCallback(widgetButtonResult.PlayerGuid, widgetButtonResult.Result);

        }
    }
}
