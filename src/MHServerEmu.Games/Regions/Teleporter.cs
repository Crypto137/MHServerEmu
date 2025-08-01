﻿using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Regions
{
    // Relevant protobufs:
    // CommonMessages.proto - [ChangeRegionRequestHeader, NetStructRegionLocation, NetStructRegionOrigin, NetStructTransferParams, NetStructRegionTarget]
    // PlayerMgrToGameServer.proto - [GameAndRegionForPlayer]

    /// <summary>
    /// Provides API for initiating teleports from gameplay code.
    /// </summary>
    public class Teleporter : IPoolable, IDisposable
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public Player Player { get; private set; }
        public TeleportContextEnum Context { get; private set; }

        public Transition TransitionEntity { get; set; }

        // Additional region creation data (see NetStructCreateRegionParams), used primarily for Danger Room and bonus level (Cow/Doop) regions
        public int Level { get; set; }
        public bool Cheat { get; set; }
        public PrototypeId DifficultyTierRef { get; set; }
        public int EndlessLevel { get; set; }
        public int Seed { get; set; }
        public ulong ParentRegionId { get; set; }
        public PrototypeId RequiredItemProtoRef { get; set; }
        public ulong RequiredItemEntityId { get; set; }
        public NetStructPortalInstance AccessPortal { get; set; }
        public List<PrototypeId> Affixes { get; set; }
        public int PlayerDeaths { get; set; }
        public ulong DangerRoomScenarioItemDbGuid { get; set; }
        public PrototypeId ItemRarity { get; set; }
        public PropertyCollection Properties { get; set; }
        public PrototypeId DangerRoomScenarioRef { get; set; }

        public bool IsInPool { get; set; }

        public Teleporter() { }     // Use pooling instead of this constructor

        public void ResetForPool()
        {
            Player = default;
            Context = default;

            TransitionEntity = default;

            Level = default;
            Cheat = default;
            DifficultyTierRef = default;
            EndlessLevel = default;
            Seed = default;
            ParentRegionId = default;
            RequiredItemProtoRef = default;
            RequiredItemEntityId = default;
            AccessPortal = default;
            Affixes = default;
            PlayerDeaths = default;
            DangerRoomScenarioItemDbGuid = default;
            ItemRarity = default;
            Properties = default;
            DangerRoomScenarioRef = default;
        }

        public void Dispose()
        {
            ObjectPoolManager pool = ObjectPoolManager.Instance;

            if (Affixes != null)
                ListPool<PrototypeId>.Instance.Return(Affixes);
            else
                Logger.Warn("Dispose(): Affixes == null");

            if (Properties != null)
                pool.Return(Properties);
            else
                Logger.Warn("Dispose(): Properties == null");

            pool.Return(this);
        }

        public void Initialize(Player player, TeleportContextEnum context)
        {
            Player = player;
            Context = context;

            Affixes = ListPool<PrototypeId>.Instance.Get();
            Properties = ObjectPoolManager.Instance.Get<PropertyCollection>();
        }

        public bool SetAccessPortal(Transition accessPortalEntity)
        {
            if (accessPortalEntity == null) return Logger.WarnReturn(false, "SetAccessPortal(): accessPortalEntity == null");
            if (accessPortalEntity.IsInWorld == false) return Logger.WarnReturn(false, "SetAccessPortal(): accessPortalEntity.IsInWorld == false");

            ParentRegionId = accessPortalEntity.Region.Id;

            var portalInstanceBuilder = NetStructPortalInstance.CreateBuilder()
                .SetEntityDbId(accessPortalEntity.DatabaseUniqueId)
                .SetLocation(accessPortalEntity.RegionLocation.ToProtobuf());

            ulong ownerDbId = accessPortalEntity.Properties[PropertyEnum.RestrictedToPlayerGuidParty];
            if (ownerDbId != 0)
                portalInstanceBuilder.SetOwnerPlayerDbId(ownerDbId).SetBoundToOwner(true);

            AccessPortal = portalInstanceBuilder.Build();
            return true;
        }

        public bool CopyEndlessRegionData(Region region, bool incrementEndlessLevel)
        {
            RegionPrototype regionProto = region.Prototype;
            if (regionProto == null) return Logger.WarnReturn(false, "CopyEndlessRegionData(): regionProto == null");

            if (regionProto.HasEndlessTheme() == false)
                return Logger.WarnReturn(false, $"CopyEndlessRegionData(): Region [{regionProto}] is not an endless region");

            RegionSettings settings = region.Settings;

            DifficultyTierRef = settings.DifficultyTierRef;
            EndlessLevel = settings.EndlessLevel;
            if (incrementEndlessLevel)
                EndlessLevel++;

            Seed = settings.Seed;
            ParentRegionId = settings.ParentRegionId;

            if (settings.AccessPortal != null)
                AccessPortal = NetStructPortalInstance.CreateBuilder().MergeFrom(settings.AccessPortal).Build();

            Affixes.Set(settings.Affixes);
            ItemRarity = settings.ItemRarity;
            DangerRoomScenarioItemDbGuid = settings.DangerRoomScenarioItemDbGuid;

            if (settings.Properties != null)
                Properties.FlattenCopyFrom(settings.Properties, true);
            
            Properties.CopyPropertyRange(region.Properties, PropertyEnum.ScoringEventTimerAccumTimeMS);

            DangerRoomScenarioRef = settings.DangerRoomScenarioRef;

            return true;
        }

        public NetStructCreateRegionParams BuildCreateRegionParams()
        {
            var builder = NetStructCreateRegionParams.CreateBuilder()
                .SetLevel((uint)Level)
                // origin
                .SetCheat(Cheat)
                .SetDifficultyTierProtoId((ulong)DifficultyTierRef)
                .SetEndlessLevel((uint)EndlessLevel)
                // gameStateId
                // matchNumber
                .SetSeed((uint)Seed)
                .SetParentRegionId(ParentRegionId)
                .SetRequiredItemProtoId((ulong)RequiredItemProtoRef)
                .SetRequiredItemEntityId(RequiredItemEntityId)
                // accessPortal
                // affixes
                .SetPlayerDeaths((uint)PlayerDeaths)
                .SetDangerRoomScenarioItemDbGuid(DangerRoomScenarioItemDbGuid)
                .SetItemRarity((ulong)ItemRarity)
                // propertyBuffer
                .SetDangerRoomScenarioR((ulong)DangerRoomScenarioRef);

            if (AccessPortal != null)
                builder.SetAccessPortal(AccessPortal);

            if (Affixes != null)
            {
                foreach (PrototypeId affix in Affixes)
                    builder.AddAffixes((ulong)affix);
            }

            if (Properties != null && Properties.IsEmpty == false)
            {
                using Archive archive = new(ArchiveSerializeType.Replication, (ulong)AOINetworkPolicyValues.AllChannels);
                Properties.Serialize(archive);
                builder.SetPropertyBuffer(archive.ToByteString());
            }

            NetStructRegionOrigin.Builder origin = NetStructRegionOrigin.CreateBuilder();

            Avatar avatar = Player.CurrentAvatar;
            if (avatar != null && avatar.IsInWorld)
                origin.SetLocation(avatar.RegionLocation.ToProtobuf());

            WorldEntity returnTarget = TransitionEntity;
            if (returnTarget != null && returnTarget.IsInWorld)
            {
                origin.SetTarget(NetStructRegionTarget.CreateBuilder()
                    .SetRegionProtoId((ulong)returnTarget.Region.PrototypeDataRef)
                    .SetAreaProtoId((ulong)returnTarget.Area.PrototypeDataRef)
                    .SetCellProtoId((ulong)returnTarget.Cell.PrototypeDataRef)
                    .SetEntityProtoId((ulong)returnTarget.PrototypeDataRef));

                origin.SetTransitionDbId(returnTarget.DatabaseUniqueId);
            }

            builder.SetOrigin(origin);

            return builder.Build();
        }

        public bool TeleportToTarget(PrototypeId targetProtoRef)
        {
            var targetProto = targetProtoRef.As<RegionConnectionTargetPrototype>();
            if (targetProto == null) return Logger.WarnReturn(false, "TeleportToTarget(): targetProto == null");

            // V52_NOTE: The data for 1.52 doesn't specify the correct difficulty tiers in SurturRaidRegionBand,
            // which causes the cosmic difficulty to be clamped to red. Resolve the target region here to avoid this.
            RegionPrototype currentRegionProto = Player.GetRegion()?.Prototype;
            if (currentRegionProto == null) return Logger.WarnReturn(false, "TeleportToTarget(): currentRegionProto == null");

            RegionPrototype destRegionProto = targetProto.Region.As<RegionPrototype>();
            if (destRegionProto == null) return Logger.WarnReturn(false, "TeleportToTarget(): destRegionProto == null");

            PrototypeId regionProtoRef = RegionPrototype.Equivalent(destRegionProto, currentRegionProto)
                ? currentRegionProto.DataRef
                : destRegionProto.DataRef;

            PrototypeId areaProtoRef = targetProto.Area;
            PrototypeId cellProtoRef = GameDatabase.GetDataRefByAsset(targetProto.Cell);
            PrototypeId entityProtoRef = targetProto.Entity;

            return TeleportToTarget(regionProtoRef, areaProtoRef, cellProtoRef, entityProtoRef);
        }

        public bool TeleportToTarget(PrototypeId regionProtoRef, PrototypeId areaProtoRef, PrototypeId cellProtoRef, PrototypeId entityProtoRef)
        {
            Region region = Player.GetRegion();
            if (region == null) return Logger.WarnReturn(false, "TeleportToTarget(): region == null");

            RegionPrototype destinationRegionProto = regionProtoRef.As<RegionPrototype>();
            if (destinationRegionProto == null) return Logger.WarnReturn(false, "TeleportToTarget(): destinationRegionProto == null");

            // Fix endless data if needed
            if (destinationRegionProto.HasEndlessTheme() && EndlessLevel <= 0)
            {
                if (region.PrototypeDataRef == destinationRegionProto.DataRef)
                    CopyEndlessRegionData(region, false);
                else
                    EndlessLevel = 1;
            }

            // Clamp target region's difficulty to the available range
            DifficultyTierRef = Player.GetDifficultyTierForRegion(regionProtoRef, DifficultyTierRef);

            if (IsLocalTeleport(region, destinationRegionProto))
            {
                return TeleportToLocalTarget(areaProtoRef, cellProtoRef, entityProtoRef);
            }
            else
            {
                if (ValidateTargetRegion(regionProtoRef) == false)
                    return false;

                return TeleportToRemoteTarget(regionProtoRef, areaProtoRef, cellProtoRef, entityProtoRef);
            }
        }

        public bool TeleportToRegionLocation(ulong regionId, Vector3 position)
        {
            // Check if we still have the region available
            Region region = Player.Game.RegionManager.GetRegion(regionId, true);
            if (region == null)
            {
                Player.SendRegionTransferFailure(RegionTransferFailure.eRTF_BodyslideRegionUnavailable);
                return false;
            }

            PlayerConnection playerConnection = Player.PlayerConnection;

            // FIXME: Get rid of RegionContext and use NetStructCreateRegionParams to get or create region
            RegionContext regionContext = playerConnection.RegionContext;
            NetStructCreateRegionParams createRegionParams = BuildCreateRegionParams();

            regionContext.CreateRegionParams = createRegionParams;

            playerConnection.TransferParams.DestRegionId = regionId;
            playerConnection.TransferParams.DestRegionProtoRef = region.PrototypeDataRef;
            playerConnection.TransferParams.SetLocation(regionId, position);
            playerConnection.BeginRemoteTeleport();
            return true;
        }

        public bool TeleportToWaypoint(PrototypeId waypointProtoRef, PrototypeId regionOverrideProtoRef, PrototypeId difficultyProtoRef)
        {
            WaypointPrototype waypointProto = waypointProtoRef.As<WaypointPrototype>();
            if (waypointProto == null) return Logger.WarnReturn(false, "TeleportToWaypoint(): waypointProto == null");

            RegionConnectionTargetPrototype targetProto = waypointProto.Destination.As<RegionConnectionTargetPrototype>();
            if (targetProto == null) return Logger.WarnReturn(false, "TeleportToWaypoint(): targetProto == null");

            DifficultyTierRef = difficultyProtoRef;

            PrototypeId regionProtoRef = regionOverrideProtoRef != PrototypeId.Invalid ? regionOverrideProtoRef : targetProto.Region;
            PrototypeId areaProtoRef = targetProto.Area;
            PrototypeId cellProtoRef = GameDatabase.GetDataRefByAsset(targetProto.Cell);
            PrototypeId entityProtoRef = targetProto.Entity;

            return TeleportToTarget(regionProtoRef, areaProtoRef, cellProtoRef, entityProtoRef);
        }

        public bool TeleportToLastTown()
        {
            // Check last town
            PrototypeId targetProtoRef = PrototypeId.Invalid;

            PrototypeId regionProtoRef = Player.Properties[PropertyEnum.LastTownRegionForAccount];
            RegionPrototype regionProto = regionProtoRef.As<RegionPrototype>();
            if (regionProto != null)
                targetProtoRef = regionProto.StartTarget;

            // Use the fallback if no saved last town
            if (targetProtoRef == PrototypeId.Invalid)
                targetProtoRef = GameDatabase.GlobalsPrototype.DefaultStartTargetFallbackRegion;

            TeleportToTarget(targetProtoRef);
            return true;
        }

        public bool TeleportToTransition(ulong entityId)
        {
            Transition transition = Player.Game.EntityManager.GetEntity<Transition>(entityId);
            if (transition == null) return Logger.WarnReturn(false, "TeleportToTransitionEntity(): transition == null");

            TransitionPrototype transitionProto = transition.TransitionPrototype;
            if (transitionProto == null) return Logger.WarnReturn(false, "TeleportToTransitionEntity(): transitionProto == null");

            Vector3 targetPos = transition.RegionLocation.Position;
            Orientation targetRot = transition.RegionLocation.Orientation;
            targetPos += transitionProto.CalcSpawnOffset(targetRot);

            ChangePositionResult result = Player.CurrentAvatar.ChangeRegionPosition(targetPos, targetRot, ChangePositionFlags.Teleport);
            return result == ChangePositionResult.PositionChanged || result == ChangePositionResult.Teleport;
        }

        public static void DebugTeleportToTarget(Player player, PrototypeId targetProtoRef)
        {
            using Teleporter teleporter = ObjectPoolManager.Instance.Get<Teleporter>();
            teleporter.Initialize(player, TeleportContextEnum.TeleportContext_Debug);
            teleporter.TeleportToTarget(targetProtoRef);
        }

        private bool TeleportToLocalTarget(PrototypeId areaProtoRef, PrototypeId cellProtoRef, PrototypeId entityProtoRef)
        {
            Region region = Player.GetRegion();
            if (region == null) return Logger.WarnReturn(false, "TeleportToLocalTarget(): region == null");

            Vector3 position = Vector3.Zero;
            Orientation orientation = Orientation.Zero;

            if (region.FindTargetLocation(ref position, ref orientation, areaProtoRef, cellProtoRef, entityProtoRef) == false)
                return Logger.WarnReturn(false, $"TeleportToLocalTarget(): Failed to find location for local target [area={areaProtoRef.GetName()}, cell={cellProtoRef.GetName()}, entity={entityProtoRef.GetName()}] in region [{region}]");

            if (Player.CurrentAvatar.Area?.PrototypeDataRef != areaProtoRef)
                region.PlayerBeginTravelToAreaEvent.Invoke(new(Player, areaProtoRef));

            Player.SendMessage(NetMessageOneTimeSnapCamera.DefaultInstance);    // Disables camera interpolation for movement

            ChangePositionResult result = Player.CurrentAvatar.ChangeRegionPosition(position, orientation, ChangePositionFlags.Teleport);
            return result == ChangePositionResult.PositionChanged || result == ChangePositionResult.Teleport;
        }

        private bool TeleportToRemoteTarget(PrototypeId regionProtoRef, PrototypeId areaProtoRef, PrototypeId cellProtoRef, PrototypeId entityProtoRef)
        {
            PlayerConnection playerConnection = Player.PlayerConnection;

            // FIXME: Get rid of RegionContext and use NetStructCreateRegionParams to get or create region
            RegionContext regionContext = playerConnection.RegionContext;
            NetStructCreateRegionParams createRegionParams = BuildCreateRegionParams();

            regionContext.CreateRegionParams = createRegionParams;

            playerConnection.TransferParams.DestRegionId = 0;
            playerConnection.TransferParams.DestRegionProtoRef = regionProtoRef;
            playerConnection.TransferParams.SetTarget(regionProtoRef, areaProtoRef, cellProtoRef, entityProtoRef);
            playerConnection.BeginRemoteTeleport();
            return true;
        }

        private bool IsLocalTeleport(Region currentRegion, RegionPrototype destinationRegionProto)
        {
            if (currentRegion == null)
                return false;

            RegionPrototype currentRegionProto = currentRegion.Prototype;

            // RegionPrototype
            if (RegionPrototype.Equivalent(destinationRegionProto, currentRegionProto) == false)
                return false;

            // DifficultyTier
            if (DifficultyTierRef != PrototypeId.Invalid && currentRegion.DifficultyTierRef != DifficultyTierRef)
                return false;

            // EndlessLevel
            if (destinationRegionProto.HasEndlessTheme() && currentRegionProto.HasEndlessTheme() && EndlessLevel != currentRegion.Settings.EndlessLevel)
                return false;

            // Seed
            if (Seed != 0 && currentRegion.RandomSeed != Seed)
                return false;

            // AccessPortal
            if (AccessPortal != null && currentRegion.Settings.OwnerPlayerDbId != AccessPortal.OwnerPlayerDbId)
                return false;

            return true;
        }

        private bool ValidateTargetRegion(PrototypeId regionProtoRef)
        {
            RegionPrototype regionProto = regionProtoRef.As<RegionPrototype>();
            if (regionProto == null) return Logger.WarnReturn(false, "ValidateTargetRegion(): regionProto == null");

            Avatar avatar = Player.CurrentAvatar;
            if (avatar == null) return Logger.WarnReturn(false, "ValidateTargetRegion(): avatar == null");

            // TODO: Add more checks

            if (LiveTuningManager.GetLiveRegionTuningVar(regionProto, RegionTuningVar.eRTV_Enabled) == 0f)
            {
                Player.SendBannerMessage(GameDatabase.UIGlobalsPrototype.MessageRegionDisabledPortalFail.As<BannerMessagePrototype>());
                return false;
            }

            if (regionProto.RunEvalAccessRestriction(Player, avatar, DifficultyTierRef) == false)
            {
                Player.SendBannerMessage(GameDatabase.UIGlobalsPrototype.MessageRegionRestricted.As<BannerMessagePrototype>());
                return false;
            }

            return true;
        }
    }
}
