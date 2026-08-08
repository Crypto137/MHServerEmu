using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Social.Parties;

namespace MHServerEmu.Games.Regions
{
    // Relevant protobufs:
    // CommonMessages.proto - [ChangeRegionRequestHeader, NetStructRegionLocation, NetStructRegionOrigin, NetStructTransferParams, NetStructRegionTarget]
    // PlayerMgrToGameServer.proto - [GameAndRegionForPlayer]

    public sealed class TeleporterPool : GenericPool<Teleporter> { }

    /// <summary>
    /// Provides API for initiating teleports from gameplay code.
    /// </summary>
    public class Teleporter : IPoolable
    {
        public Player Player { get; private set; }
        public TeleportContextEnum Context { get; private set; }

        public Transition TransitionEntity { get; set; }

        // Additional region creation data (see NetStructCreateRegionParams), used primarily for Danger Room and bonus level (Cow/Doop) regions
        public int Level { get; set; }
        public bool Cheat { get; set; }
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        public PrototypeId DifficultyTierRef { get; set; }
#endif
        public int EndlessLevel { get; set; }
        public int Seed { get; set; }
        public ulong ParentRegionId { get; set; }
        public PrototypeId RequiredItemProtoRef { get; set; }
        public ulong RequiredItemEntityId { get; set; }
        public NetStructPortalInstance AccessPortal { get; set; }
        public List<PrototypeId> Affixes { get; private set; }
        public int PlayerDeaths { get; set; }
        public ulong DangerRoomScenarioItemDbGuid { get; set; }
        public PrototypeId ItemRarity { get; set; }
        public PropertyCollection Properties { get; private set; }
        public PrototypeId DangerRoomScenarioRef { get; set; }

        public Teleporter() { }     // Use pooling instead of this constructor

        public void ResetForPool()
        {
            Player = default;
            Context = default;

            TransitionEntity = default;

            Level = default;
            Cheat = default;
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            DifficultyTierRef = default;
#endif
            EndlessLevel = default;
            Seed = default;
            ParentRegionId = default;
            RequiredItemProtoRef = default;
            RequiredItemEntityId = default;
            AccessPortal = default;

            if (Verify.IsNotNull(Affixes))
            {
                ListPool<PrototypeId>.Return(Affixes);
                Affixes = null;
            }

            PlayerDeaths = default;
            DangerRoomScenarioItemDbGuid = default;
            ItemRarity = default;

            if (Verify.IsNotNull(Properties))
            {
                PropertyCollectionPool.Return(Properties);
                Properties = default;
            }

            DangerRoomScenarioRef = default;
        }

        public void Initialize(Player player, TeleportContextEnum context)
        {
            Player = player;
            Context = context;

            Affixes = ListPool<PrototypeId>.Get();
            Properties = PropertyCollectionPool.Get();
        }

        public void SetAccessPortal(Transition accessPortalEntity)
        {
            if (!Verify.IsNotNull(accessPortalEntity)) return;
            if (!Verify.IsTrue(accessPortalEntity.IsInWorld)) return;

            ParentRegionId = accessPortalEntity.Region.Id;

            var portalInstanceBuilder = NetStructPortalInstance.CreateBuilder()
                .SetEntityDbId(accessPortalEntity.DatabaseUniqueId)
                .SetLocation(accessPortalEntity.RegionLocation.ToProtobuf());

            ulong ownerDbId = accessPortalEntity.Properties[PropertyEnum.RestrictedToPlayerGuidParty];
            if (ownerDbId != 0)
                portalInstanceBuilder.SetOwnerPlayerDbId(ownerDbId).SetBoundToOwner(true);

            AccessPortal = portalInstanceBuilder.Build();
        }

        public void CopyEndlessRegionData(Region region, bool incrementEndlessLevel)
        {
            RegionPrototype regionProto = region.Prototype;
            if (!Verify.IsNotNull(regionProto)) return;

            if (!Verify.IsTrue(regionProto.HasEndlessTheme(), $"Region [{regionProto}] is not an endless region"))
                return;

            RegionSettings settings = region.Settings;

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            DifficultyTierRef = settings.DifficultyTierRef;
#endif

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
        }

        public bool TeleportToTarget(PrototypeId targetProtoRef)
        {
            if (CanTeleport() == false)
                return false;

            RegionConnectionTargetPrototype targetProto = targetProtoRef.As<RegionConnectionTargetPrototype>();
            if (!Verify.IsNotNull(targetProto)) return false;

            // V52_NOTE: The data for 1.52 doesn't specify the correct difficulty tiers in SurturRaidRegionBand,
            // which causes the cosmic difficulty to be clamped to red. Resolve the target region here to avoid this.
            RegionPrototype currentRegionProto = Player.GetRegion()?.Prototype;
            if (!Verify.IsNotNull(currentRegionProto)) return false;

            RegionPrototype destRegionProto = targetProto.Region.As<RegionPrototype>();
            if (!Verify.IsNotNull(destRegionProto)) return false;

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
            if (CanTeleport() == false)
                return false;

            Region region = Player.GetRegion();
            if (!Verify.IsNotNull(region)) return false;

            RegionPrototype destinationRegionProto = regionProtoRef.As<RegionPrototype>();
            if (!Verify.IsNotNull(destinationRegionProto)) return false;

            // Fix endless data if needed
            if (destinationRegionProto.HasEndlessTheme() && EndlessLevel <= 0)
            {
                if (region.PrototypeDataRef == destinationRegionProto.DataRef)
                    CopyEndlessRegionData(region, false);
                else
                    EndlessLevel = 1;
            }

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            // Keep difficulty consistent for teleports that are expected to be local (e.g. resurrect, Surtur raid teleport).
            if (DifficultyTierRef == PrototypeId.Invalid)
            {
                switch (Context)
                {
                    case TeleportContextEnum.TeleportContext_Mission:
                    case TeleportContextEnum.TeleportContext_Power:
                    case TeleportContextEnum.TeleportContext_Resurrect:
                        DifficultyTierRef = region.DifficultyTierRef;
                        break;
                }
            }

            // Clamp target region's difficulty to the available range
            DifficultyTierRef = Player.GetDifficultyTierForRegion(regionProtoRef, DifficultyTierRef);
#endif

            if (IsLocalTeleport(region, destinationRegionProto))
            {
                return TeleportToLocalTarget(areaProtoRef, cellProtoRef, entityProtoRef);
            }
            else
            {
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
                if (Player.CanEnterRegion(regionProtoRef, DifficultyTierRef, false) == false)
                    return false;
#else
                if (Player.CanEnterRegion(regionProtoRef, false) == false)
                    return false;
#endif

                if (destinationRegionProto.IsQueueRegion)
                    return BeginTeleportToQueueTarget(regionProtoRef);

                return TeleportToRemoteTarget(regionProtoRef, areaProtoRef, cellProtoRef, entityProtoRef);
            }
        }

        public bool TeleportToRegionLocation(ulong regionId, Vector3 position)
        {
            Player.PlayerConnection.BeginRegionTransfer(PrototypeId.Invalid);

            ChangeRegionRequestHeader header = BuildChangeRegionRequestHeader();
            NetStructRegionLocation destLocation = NetStructRegionLocation.CreateBuilder()
                .SetRegionId(regionId)
                .SetPosition(position.ToNetStructPoint3())
                .Build();

            ServiceMessage.ChangeRegionRequest message = new(header, destLocation);
            ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, message);

            return true;
        }

#if GAME_VERSION_1_48
        public bool TeleportToWaypoint(PrototypeId waypointProtoRef, PrototypeId regionOverrideProtoRef)
#else
        public bool TeleportToWaypoint(PrototypeId waypointProtoRef, PrototypeId regionOverrideProtoRef, PrototypeId difficultyProtoRef)
#endif
        {
            if (CanTeleport() == false)
                return false;

            WaypointPrototype waypointProto = waypointProtoRef.As<WaypointPrototype>();
            if (!Verify.IsNotNull(waypointProto)) return false;

            RegionConnectionTargetPrototype targetProto = waypointProto.Destination.As<RegionConnectionTargetPrototype>();
            if (!Verify.IsNotNull(targetProto)) return false;

#if !GAME_VERSION_1_48
            DifficultyTierRef = difficultyProtoRef;
#endif

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
            if (!Verify.IsNotNull(transition)) return false;

            TransitionPrototype transitionProto = transition.TransitionPrototype;
            if (!Verify.IsNotNull(transitionProto)) return false;

            Vector3 targetPos = transition.RegionLocation.Position;
            Orientation targetRot = transition.RegionLocation.Orientation;
            targetPos += transitionProto.CalcSpawnOffset(targetRot);

            ChangePositionResult result = Player.CurrentAvatar.ChangeRegionPosition(targetPos, targetRot, ChangePositionFlags.Teleport);
            return result == ChangePositionResult.PositionChanged || result == ChangePositionResult.Teleport;
        }

        public bool TeleportToPlayer(ulong playerDbId)
        {
            if (!Verify.IsTrue(playerDbId != 0)) return false;

            // See if we can do a local teleport
            Avatar otherAvatar = Player.Game.EntityManager.GetEntityByDbGuid<Player>(playerDbId)?.CurrentAvatar;
            if (otherAvatar != null && otherAvatar.IsInWorld && otherAvatar.Region == Player.GetRegion())
            {
                Vector3 position = otherAvatar.RegionLocation.Position;
                if (Avatar.AdjustStartPositionIfNeeded(otherAvatar.Region, ref position))
                {
                    ChangePositionResult result = Player.CurrentAvatar.ChangeRegionPosition(position, null, ChangePositionFlags.Teleport);
                    if (result == ChangePositionResult.PositionChanged || result == ChangePositionResult.Teleport)
                        return true;
                }
            }

            // Do a remote teleport
            Player.PlayerConnection.BeginRegionTransfer(PrototypeId.Invalid);

            ChangeRegionRequestHeader header = BuildChangeRegionRequestHeader();
            ServiceMessage.ChangeRegionRequest message = new(header, playerDbId);
            ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, message);

            return true;
        }

        public static void DebugTeleportToTarget(Player player, PrototypeId targetProtoRef, PrototypeId difficultyTierRef = PrototypeId.Invalid)
        {
            using var teleporterHandle = TeleporterPool.Get(out Teleporter teleporter);
            teleporter.Initialize(player, TeleportContextEnum.TeleportContext_Debug);
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            teleporter.DifficultyTierRef = difficultyTierRef;
#endif
            teleporter.TeleportToTarget(targetProtoRef);
        }

        private bool TeleportToLocalTarget(PrototypeId areaProtoRef, PrototypeId cellProtoRef, PrototypeId entityProtoRef)
        {
            Region region = Player.GetRegion();
            if (!Verify.IsNotNull(region)) return false;

            Vector3 position = Vector3.Zero;
            Orientation orientation = Orientation.Zero;

            bool locationFound = region.FindTargetLocation(ref position, ref orientation, areaProtoRef, cellProtoRef, entityProtoRef);
            if (!Verify.IsTrue(locationFound, $"Failed to find location for local target [area={areaProtoRef.GetName()}, cell={cellProtoRef.GetName()}, entity={entityProtoRef.GetName()}] in region [{region}]"))
                return false;

            if (Player.CurrentAvatar.Area?.PrototypeDataRef != areaProtoRef)
                region.PlayerBeginTravelToAreaEvent.Invoke(new(Player, areaProtoRef));

            Player.SendMessage(NetMessageOneTimeSnapCamera.DefaultInstance);    // Disables camera interpolation for movement

            ChangePositionResult result = Player.CurrentAvatar.ChangeRegionPosition(position, orientation, ChangePositionFlags.Teleport);
            return result == ChangePositionResult.PositionChanged || result == ChangePositionResult.Teleport;
        }

        private bool BeginTeleportToQueueTarget(PrototypeId regionProtoRef)
        {
            RegionPrototype destinationRegionProto = regionProtoRef.As<RegionPrototype>();
            if (!Verify.IsNotNull(destinationRegionProto)) return false;

            if (Player.MatchQueueStatus.IsOwnerInQueue())
            {
                Player.SendBannerMessage(GameDatabase.UIGlobalsPrototype.MessageAlreadyInQueue);
                return false;
            }

            RegionPrototype currentRegionProto = Player.GetRegion()?.Prototype;
            if (currentRegionProto != null && currentRegionProto.IsQueueRegion)
            {
                Player.SendBannerMessage(GameDatabase.UIGlobalsPrototype.MessageCantQueueInQueueRegion);
                return false;
            }

            Party party = Player.GetParty();
            if (party != null && party.IsLeader(Player) == false)
            {
                Player.SendBannerMessage(GameDatabase.UIGlobalsPrototype.MessageOnlyPartyLeaderCanQueue);
                return false;
            }

            // Queue up straight away if there is nothing to choose (queue bypass is not allowed and we are not in a party).
            if (destinationRegionProto.AllowsQueueBypass == false && party == null)
            {
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
                Player.SendRegionRequestQueueCommandToPlayerManager(regionProtoRef, DifficultyTierRef, RegionRequestQueueCommandVar.eRRQC_AddToQueueSolo);
#else
                Player.SendRegionRequestQueueCommandToPlayerManager(regionProtoRef, PrototypeId.Invalid, RegionRequestQueueCommandVar.eRRQC_AddToQueueSolo);
#endif
                return true;
            }

            // Ask the player to choose whether to queue solo or not.
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            Player.SendMatchQueueUpdate(Player.DatabaseUniqueId, regionProtoRef, DifficultyTierRef, 0,
                RegionRequestQueueUpdateVar.eRRQ_SelectQueueMethod, Player.GetName());
#else
            Player.SendMatchQueueUpdate(Player.DatabaseUniqueId, regionProtoRef, PrototypeId.Invalid, 0,
                RegionRequestQueueUpdateVar.eRRQ_SelectQueueMethod, Player.GetName());
#endif

            return true;
        }

        private bool TeleportToRemoteTarget(PrototypeId regionProtoRef, PrototypeId areaProtoRef, PrototypeId cellProtoRef, PrototypeId entityProtoRef)
        {
            // NOTE: CreateRegionParams need to be built before we call BeginRegionTransfer() so that we have access to avatar's location to use as a region origin.
            ChangeRegionRequestHeader header = BuildChangeRegionRequestHeader();
            NetStructRegionTarget destTarget = NetStructRegionTarget.CreateBuilder()
                .SetRegionProtoId((ulong)regionProtoRef)
                .SetAreaProtoId((ulong)areaProtoRef)
                .SetCellProtoId((ulong)cellProtoRef)
                .SetEntityProtoId((ulong)entityProtoRef)
                .Build();
            NetStructCreateRegionParams createRegionParams = BuildCreateRegionParams();

            Player.PlayerConnection.BeginRegionTransfer(regionProtoRef);

            ServiceMessage.ChangeRegionRequest message = new(header, destTarget, createRegionParams);
            ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, message);

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

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            // DifficultyTier
            if (DifficultyTierRef != PrototypeId.Invalid && currentRegion.DifficultyTierRef != DifficultyTierRef)
                return false;
#endif

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

        private bool CanTeleport()
        {
            if (!Verify.IsNotNull(Player)) return false;

            if (Player.PlayerConnection.HasPendingRegionTransfer)
                return false;

            if (TransitionEntity != null)
            {
                Avatar avatar = Player.CurrentAvatar;
                if (!Verify.IsNotNull(avatar)) return false;

                if (avatar.InInteractRange(TransitionEntity, Dialog.InteractionMethod.Use) == false)
                    return false;
            }

            return true;
        }

        private ChangeRegionRequestHeader BuildChangeRegionRequestHeader()
        {
            return ChangeRegionRequestHeader.CreateBuilder()
                .SetRequestingGameId(Player.Game.Id)
                .SetRequestingPlayerGuid(Player.DatabaseUniqueId)
                .SetOrigin(NetStructRegionOrigin.DefaultInstance)   // We currently don't use this, but it's required in the protobuf.
                .SetType(Context)
                .Build();
        }

        private NetStructCreateRegionParams BuildCreateRegionParams()
        {
            var builder = NetStructCreateRegionParams.CreateBuilder()
                .SetLevel((uint)Level)
                // origin
                .SetCheat(Cheat)
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
                .SetDifficultyTierProtoId((ulong)DifficultyTierRef)
#endif
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
    }
}
