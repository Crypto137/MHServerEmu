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
        private readonly Dictionary<(PrototypeId WidgetRef, PrototypeId ContextRef), UISyncData> _data = new();

        public Region Region { get => Owner as Region; }
        public Game Game { get; }
        public IUIDataProviderOwner Owner { get; }

        public UIDataProvider(Game game, IUIDataProviderOwner owner)
        {
            Game = game;
            Owner = owner;
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            foreach (var kvp in _data)
            {
                string widgetName = GameDatabase.GetFormattedPrototypeName(kvp.Key.WidgetRef);
                string contextName = GameDatabase.GetFormattedPrototypeName(kvp.Key.ContextRef);
                sb.AppendLine($"_data[{widgetName}][{contextName}]: {kvp.Value}");
            }

            return sb.ToString();
        }

        public void Deallocate()
        {
            foreach (UISyncData widget in _data.Values)
                widget?.Deallocate();

            _data.Clear();
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            uint numWidgets = (uint)_data.Count;
            success &= Serializer.Transfer(archive, ref numWidgets);

            if (archive.IsPacking)
            {
                foreach (var kvp in _data)
                {
                    (PrototypeId widgetRef, PrototypeId contextRef) = kvp.Key;
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

        public T GetWidget<T>(PrototypeId widgetRef, PrototypeId contextRef = PrototypeId.Invalid) where T: UISyncData
        {
            if (_data.TryGetValue((widgetRef, contextRef), out UISyncData widget) == false)
                widget = AllocateUIWidget(widgetRef, contextRef);

            return widget as T;
        }

        public void DeleteWidget(PrototypeId widgetRef, PrototypeId contextRef = PrototypeId.Invalid)
        {
            if (_data.Remove((widgetRef, contextRef), out UISyncData widget))
                widget.Deallocate();

            Region region = Region;
            if (region == null)
                return;

            NetMessageUISyncDataRemove message = NetMessageUISyncDataRemove.CreateBuilder()
                .SetUiSyncDataProtoId((ulong)widgetRef)
                .SetContextProtoId((ulong)contextRef)
                .Build();

            Game?.NetworkManager.SendMessageToInterested(message, region);
        }

        public void OnUpdateUI(UISyncData uiSyncData)
        {
            Region region = Region;
            if (region == null)
                return;

            ByteString buffer;
            using (Archive archive = new(ArchiveSerializeType.Replication, (ulong)AOINetworkPolicyValues.AllChannels))
            {
                uiSyncData.Serialize(archive);
                buffer = archive.ToByteString();
            }

            NetMessageUISyncDataUpdate message = NetMessageUISyncDataUpdate.CreateBuilder()
                .SetUiSyncDataProtoId((ulong)uiSyncData.WidgetRef)
                .SetContextProtoId((ulong)uiSyncData.ContextRef)
                .SetBuffer(buffer)
                .Build();

            Game?.NetworkManager.SendMessageToInterested(message, region);
        }

        public void OnEntityTracked(WorldEntity worldEntity, PrototypeId widgetRef)
        {
            if (!Verify.IsNotNull(worldEntity)) return;
            if (!Verify.IsTrue(widgetRef != PrototypeId.Invalid)) return;

            MetaGameDataPrototype metaGameProto = widgetRef.As<MetaGameDataPrototype>();
            if (metaGameProto == null)
                return;

            UISyncData uiSyncData = FindWidget(worldEntity, widgetRef);
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

        public void OnWidgetButtonResult(NetMessageWidgetButtonResult widgetButtonResult)
        {
            PrototypeId widgetRef = (PrototypeId)widgetButtonResult.WidgetRefId;
            PrototypeId contextRef = (PrototypeId)widgetButtonResult.WidgetContextRefId;

            UIWidgetButton button = GetWidget<UIWidgetButton>(widgetRef, contextRef);
            if (!Verify.IsNotNull(button)) return;

            button.DoCallback(widgetButtonResult.PlayerGuid, widgetButtonResult.Result);
        }

        /// <summary>
        /// Creates a <see cref="UISyncData"/> instance of the appropriate subtype for the specified widget prototype.
        /// </summary>
        private UISyncData AllocateUIWidget(PrototypeId widgetRef, PrototypeId contextRef)
        {
            if (!Verify.IsTrue(widgetRef != PrototypeId.Invalid)) return null;
            if (!Verify.IsTrue(_data.ContainsKey((widgetRef, contextRef)) == false)) return null;

            MetaGameDataPrototype metaGameDataProto = widgetRef.As<MetaGameDataPrototype>();
            if (!Verify.IsNotNull(metaGameDataProto)) return null;

            UISyncData uiSyncData = metaGameDataProto switch
            {
                UIWidgetButtonPrototype             => new UIWidgetButton(this, widgetRef, contextRef),
                UIWidgetEntityIconsPrototype        => new UIWidgetEntityIconsSyncData(this, widgetRef, contextRef),
                UIWidgetGenericFractionPrototype    => new UIWidgetGenericFraction(this, widgetRef, contextRef),
                UIWidgetMissionTextPrototype        => new UIWidgetMissionText(this, widgetRef, contextRef),
                UIWidgetReadyCheckPrototype         => new UIWidgetReadyCheck(this, widgetRef, contextRef),
                _ => null
            };

            if (!Verify.IsNotNull(uiSyncData, $"Trying to allocate widget of the base type, use UIWidgetEntityIconsSyncData, UIWidgetGenericFraction, or UIWidgetMissionText. WIDGETREF={widgetRef.GetNameFormatted()}"))
                return null;

            _data.Add((widgetRef, contextRef), uiSyncData);
            return uiSyncData;
        }

        private UISyncData UpdateOrCreateUIWidget(PrototypeId widgetRef, PrototypeId contextRef, Archive archive)
        {
            if (_data.TryGetValue((widgetRef, contextRef), out UISyncData uiData) == false)
                uiData = AllocateUIWidget(widgetRef, contextRef);

            uiData.Serialize(archive);
            uiData.UpdateUI();

            return uiData;
        }

        private UISyncData FindWidget(WorldEntity worldEntity, PrototypeId widgetRef)
        {
            if (_data.TryGetValue((widgetRef, PrototypeId.Invalid), out UISyncData widget))
                return widget;

            if (_data.TryGetValue((widgetRef, worldEntity.MissionPrototype), out widget))
                return widget;

            foreach (var kvp in _data)
            {
                if (kvp.Key.WidgetRef == widgetRef)
                    return kvp.Value;
            }

            return null;
        }
    }
}
