﻿using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Regions.Maps;

namespace MHServerEmu.Games.Network
{
    // Unused bool field names from 1.24: initConditionComponent, startFullInWorldHierarchyUpdate
    [Flags]
    public enum EntityCreateMessageFlags : uint
    {
        None = 0,
        HasPositionAndOrientation   = 1 << 0,
        HasActivePowerPrototypeRef  = 1 << 1,
        IsNewOnServer               = 1 << 2,
        HasSourceEntityId           = 1 << 3,
        HasSourcePosition           = 1 << 4,
        HasNonProximityInterest     = 1 << 5,
        HasInvLoc                   = 1 << 6,
        HasInvLocPrev               = 1 << 7,
        HasDbId                     = 1 << 8,
        HasAvatarWorldInstanceId    = 1 << 9,
        OverrideSnapToFloorOnSpawn  = 1 << 10,
        HasBoundsScaleOverride      = 1 << 11,
        IsClientEntityHidden        = 1 << 12,
        Flag13                      = 1 << 13,  // Unused
        HasAttachedEntities         = 1 << 14,
        IgnoreNavi                  = 1 << 15
    }

    [Flags]
    public enum EnterGameWorldMessageFlags : uint
    {
        None = 0,
        HasAvatarWorldInstanceId    = 1 << 0,
        IsNewOnServer               = 1 << 1,
        IsClientEntityHidden        = 1 << 2,
        HasAttachedEntities         = 1 << 3
    }

    /// <summary>
    /// A helper class for building archive protobufs introduced in version 1.25. 
    /// </summary>
    public static class ArchiveMessageBuilder
    {
        public static NetMessageEntityCreate BuildEntityCreateMessage(Entity entity, AOINetworkPolicyValues interestPolicies, bool includeInvLoc, EntitySettings settings = null)
        {
            ByteString baseData = null;
            ByteString archiveData = null;

            // Base data
            using (Archive archive = new Archive(ArchiveSerializeType.Replication, (ulong)interestPolicies))
            {
                // Build flags
                EntityCreateMessageFlags fieldFlags = EntityCreateMessageFlags.None;
                LocomotionMessageFlags locoFieldFlags = LocomotionMessageFlags.None;

                // The client assumes interest to be proximity-based unless flagged otherwise
                if (interestPolicies != AOINetworkPolicyValues.AOIChannelProximity)
                    fieldFlags |= EntityCreateMessageFlags.HasNonProximityInterest;

                // Add database guid for players
                if (entity is Player)
                    fieldFlags |= EntityCreateMessageFlags.HasDbId;

                // Add world instance id for avatars
                Avatar avatar = entity as Avatar;
                if (avatar != null)
                    fieldFlags |= EntityCreateMessageFlags.HasAvatarWorldInstanceId;

                // Apply world entity specific flags
                Vector3 position = Vector3.Zero;
                Orientation orientation = Orientation.Zero;
                LocomotionState locomotionState = null;
                PrototypeId activePowerPrototypeRef = PrototypeId.Invalid;

                if (entity is WorldEntity worldEntity)
                {
                    // Make sure that the world entity in proximity is in world, because items in other players' inventories
                    // (e.g. equipment) are also considered to be in proximity.
                    if (interestPolicies.HasFlag(AOINetworkPolicyValues.AOIChannelProximity) && worldEntity.IsInWorld)
                    {
                        fieldFlags |= EntityCreateMessageFlags.HasPositionAndOrientation;

                        position = worldEntity.RegionLocation.Position;
                        orientation = worldEntity.RegionLocation.Orientation;

                        if (orientation.Pitch != 0f || orientation.Roll != 0f)
                            locoFieldFlags |= LocomotionMessageFlags.HasFullOrientation;

                        locomotionState = worldEntity.Locomotor?.LocomotionState;

                        locoFieldFlags |= LocomotionState.GetFieldFlags(locomotionState, null, true);
                    }

                    //activePowerPrototypeRef = worldEntity.ActivePowerRef;     // TODO
                    if (activePowerPrototypeRef != PrototypeId.Invalid)
                        fieldFlags |= EntityCreateMessageFlags.HasActivePowerPrototypeRef;

                    // TODO: worldEntity.Physics.HasAttachedEntities();

                    if (worldEntity.ShouldSnapToFloorOnSpawn != worldEntity.WorldEntityPrototype.SnapToFloorOnSpawn)
                        fieldFlags |= EntityCreateMessageFlags.OverrideSnapToFloorOnSpawn;
                }             

                // Apply flags from settings
                if (settings != null)
                {
                    if (settings.OptionFlags.HasFlag(EntitySettingsOptionFlags.IsNewOnServer))
                        fieldFlags |= EntityCreateMessageFlags.IsNewOnServer;

                    if (settings.SourceEntityId != Entity.InvalidId)
                        fieldFlags |= EntityCreateMessageFlags.HasSourceEntityId;

                    if (settings.SourcePosition != Vector3.Zero)
                        fieldFlags |= EntityCreateMessageFlags.HasSourcePosition;

                    if (settings.BoundsScaleOverride != 1f)
                        fieldFlags |= EntityCreateMessageFlags.HasBoundsScaleOverride;

                    if (settings.OptionFlags.HasFlag(EntitySettingsOptionFlags.IsClientEntityHidden))
                        fieldFlags |= EntityCreateMessageFlags.IsClientEntityHidden;

                    if (settings.IgnoreNavi)
                        fieldFlags |= EntityCreateMessageFlags.IgnoreNavi;
                }

                // Include inventory location based on inventory visibility
                if (includeInvLoc)
                {
                    fieldFlags |= EntityCreateMessageFlags.HasInvLoc;

                    // We need a second null check for settings here because we send invLocPrev only if we have a current invLoc
                    if (settings != null && settings.InventoryLocationPrevious.Equals(entity.InventoryLocation) == false)
                        fieldFlags |= EntityCreateMessageFlags.HasInvLocPrev;
                }

                // Serialize
                ulong entityId = entity.Id;
                Serializer.Transfer(archive, ref entityId);

                PrototypeId entityPrototypeRef = entity.PrototypeDataRef;
                Serializer.TransferPrototypeEnum<EntityPrototype>(archive, ref entityPrototypeRef);

                uint fieldFlagsRaw = (uint)fieldFlags;
                Serializer.Transfer(archive, ref fieldFlagsRaw);

                uint locoFieldFlagsRaw = (uint)locoFieldFlags;
                Serializer.Transfer(archive, ref locoFieldFlagsRaw);

                if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasNonProximityInterest))
                {
                    uint rawInterestPolicies = (uint)interestPolicies;
                    Serializer.Transfer(archive, ref rawInterestPolicies);
                }

                if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasAvatarWorldInstanceId))
                {
                    uint avatarWorldInstanceId = avatar.AvatarWorldInstanceId;
                    Serializer.Transfer(archive, ref avatarWorldInstanceId);
                }

                if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasDbId))
                {
                    ulong dbId = entity.DatabaseUniqueId;
                    Serializer.Transfer(archive, ref dbId);
                }

                if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasPositionAndOrientation))
                {
                    Serializer.TransferVectorFixed(archive, ref position, 3);

                    bool yawOnly = locoFieldFlags.HasFlag(LocomotionMessageFlags.HasFullOrientation) == false;
                    Serializer.TransferOrientationFixed(archive, ref orientation, yawOnly, 6);
                }

                if (locoFieldFlags.HasFlag(LocomotionMessageFlags.NoLocomotionState) == false)
                    LocomotionState.SerializeTo(archive, locomotionState, locoFieldFlags);

                if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasBoundsScaleOverride))
                {
                    float boundsScaleOverride = settings.BoundsScaleOverride;
                    Serializer.TransferFloatFixed(archive, ref boundsScaleOverride, 8);
                }

                if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasSourceEntityId))
                {
                    ulong sourceEntityId = settings.SourceEntityId;
                    Serializer.Transfer(archive, ref sourceEntityId);
                }

                if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasSourcePosition))
                {
                    // NOTE: Source position is serialized as a delta from position
                    Vector3 sourcePosition = settings.SourcePosition - position;
                    Serializer.TransferVectorFixed(archive, ref sourcePosition, 3);
                }

                if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasActivePowerPrototypeRef))
                    Serializer.TransferPrototypeEnum<PowerPrototype>(archive, ref activePowerPrototypeRef);

                if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasInvLoc))
                    InventoryLocation.SerializeTo(archive, entity.InventoryLocation);

                if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasInvLocPrev))
                    InventoryLocation.SerializeTo(archive, settings.InventoryLocationPrevious);

                if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasAttachedEntities))
                {
                    List<ulong> attachedEntityList = new();
                    Serializer.Transfer(archive, ref attachedEntityList);
                }

                baseData = archive.ToByteString();
            }

            // Archive data
            using (Archive archive = new Archive(ArchiveSerializeType.Replication, (ulong)interestPolicies))
            {
                Serializer.Transfer(archive, ref entity);
                archiveData = archive.ToByteString();
            }

            return NetMessageEntityCreate.CreateBuilder()
                .SetBaseData(baseData)
                .SetArchiveData(archiveData)
                .Build();
        }

        public static NetMessageLocomotionStateUpdate BuildLocomotionStateUpdateMessage(WorldEntity worldEntity, LocomotionState oldLocomotionState, LocomotionState newLocomotionState,
            bool withPathNodes)
        {
            // Build flags
            LocomotionMessageFlags fieldFlags = LocomotionMessageFlags.None;

            RegionLocation regionLocation = worldEntity.RegionLocation;
            Vector3 position = regionLocation.Position;
            Orientation orientation = regionLocation.Orientation;

            if (orientation.Pitch != 0f || orientation.Yaw != 0f)
                fieldFlags |= LocomotionMessageFlags.HasFullOrientation;

            fieldFlags |= LocomotionState.GetFieldFlags(newLocomotionState, oldLocomotionState, withPathNodes);

            // Serialize
            using Archive archive = new(ArchiveSerializeType.Replication, (ulong)AOINetworkPolicyValues.AOIChannelProximity);

            ulong entityId = worldEntity.Id;
            Serializer.Transfer(archive, ref entityId);

            uint fieldFlagsRaw = (uint)fieldFlags;
            Serializer.Transfer(archive, ref fieldFlagsRaw);

            if (fieldFlags.HasFlag(LocomotionMessageFlags.HasEntityPrototypeRef))
            {
                PrototypeId entityPrototypeRef = worldEntity.PrototypeDataRef;
                Serializer.TransferPrototypeEnum<EntityPrototype>(archive, ref entityPrototypeRef);
            }

            Serializer.TransferVectorFixed(archive, ref position, 3);

            bool yawOnly = fieldFlags.HasFlag(LocomotionMessageFlags.HasFullOrientation) == false;
            Serializer.TransferOrientationFixed(archive, ref orientation, yawOnly, 6);

            LocomotionState.SerializeTo(archive, newLocomotionState, fieldFlags);

            return NetMessageLocomotionStateUpdate.CreateBuilder().SetArchiveData(archive.ToByteString()).Build();
        }

        /// <summary>
        /// Builds <see cref="NetMessageEntityEnterGameWorld"/> for the provided <see cref="WorldEntity"/>.
        /// </summary>
        public static NetMessageEntityEnterGameWorld BuildEntityEnterGameWorldMessage(WorldEntity worldEntity, EntitySettings settings = null)
        {
            // Build flags
            LocomotionMessageFlags locoFieldFlags = LocomotionMessageFlags.None;
            EnterGameWorldMessageFlags extraFieldFlags = EnterGameWorldMessageFlags.None;

            // Position
            RegionLocation regionLocation = worldEntity.RegionLocation;
            Vector3 position = regionLocation.Position;
            Orientation orientation = regionLocation.Orientation;

            if (orientation.Pitch != 0f || orientation.Yaw != 0f)
                locoFieldFlags |= LocomotionMessageFlags.HasFullOrientation;

            // LocomotionState
            LocomotionState locomotionState = worldEntity.Locomotor?.LocomotionState;

            if (locomotionState != null)
                locoFieldFlags |= LocomotionState.GetFieldFlags(locomotionState, null, true);
            else
                locoFieldFlags |= LocomotionState.GetFieldFlags(null, null, false);

            // AvatarWorldInstanceId
            Avatar avatar = worldEntity as Avatar;
            if (avatar != null)
                extraFieldFlags |= EnterGameWorldMessageFlags.HasAvatarWorldInstanceId;

            // Settings flags
            if (settings != null)
            {
                if (settings.OptionFlags.HasFlag(EntitySettingsOptionFlags.IsNewOnServer))
                    extraFieldFlags |= EnterGameWorldMessageFlags.IsNewOnServer;

                if (settings.OptionFlags.HasFlag(EntitySettingsOptionFlags.IsClientEntityHidden))
                    extraFieldFlags |= EnterGameWorldMessageFlags.IsClientEntityHidden;
            }

            // TODO: HasAttachedEntities
            /*
            if (worldEntity.Physics.HasAttachedEntities())
                extraFieldFlags |= EnterGameWorldMessageFlags.HasAttachedEntities;
            */

            // Serialize
            // NOTE: EntityEnterGameWorld always uses AOIChannelProximity
            using Archive archive = new(ArchiveSerializeType.Replication, (ulong)AOINetworkPolicyValues.AOIChannelProximity);

            ulong entityId = worldEntity.Id;
            Serializer.Transfer(archive, ref entityId);

            uint flags = (uint)locoFieldFlags | ((uint)extraFieldFlags << 12);  // 12 - LocoFlagCount
            Serializer.Transfer(archive, ref flags);

            if (locoFieldFlags.HasFlag(LocomotionMessageFlags.HasEntityPrototypeRef))
            {
                PrototypeId entityPrototypeRef = worldEntity.PrototypeDataRef;
                Serializer.TransferPrototypeEnum<EntityPrototype>(archive, ref entityPrototypeRef);
            }

            Serializer.TransferVectorFixed(archive, ref position, 3);

            bool yawOnly = locoFieldFlags.HasFlag(LocomotionMessageFlags.HasFullOrientation) == false;
            Serializer.TransferOrientationFixed(archive, ref orientation, yawOnly, 6);

            if (locoFieldFlags.HasFlag(LocomotionMessageFlags.NoLocomotionState) == false)
                LocomotionState.SerializeTo(archive, locomotionState, locoFieldFlags);

            if (extraFieldFlags.HasFlag(EnterGameWorldMessageFlags.HasAvatarWorldInstanceId))
            {
                uint avatarWorldInstanceId = avatar.AvatarWorldInstanceId;
                Serializer.Transfer(archive, ref avatarWorldInstanceId);
            }

            if (extraFieldFlags.HasFlag(EnterGameWorldMessageFlags.HasAttachedEntities))
            {
                //TODO: worldEntity.Physics.GetAttachedEntities(out List<ulong> attachedEntityList);
                List<ulong> attachedEntityList = new();
                Serializer.Transfer(archive, ref attachedEntityList);
            }

            return NetMessageEntityEnterGameWorld.CreateBuilder().SetArchiveData(archive.ToByteString()).Build();
        }

        /// <summary>
        /// Builds <see cref="NetMessageAddCondition"/> for the provided <see cref="Condition"/> owned by a <see cref="WorldEntity"/>.
        /// </summary>
        public static NetMessageAddCondition BuildAddConditionMessage(WorldEntity owner, Condition condition)
        {
            // NOTE: In all of our packets this uses the default policy. This may be different for older versions of the game
            // where condition replication was heavily based on the ArchiveMessageDispatcher/Handler system.
            using Archive archive = new(ArchiveSerializeType.Replication, (ulong)AOINetworkPolicyValues.DefaultPolicy);

            ulong entityId = owner.Id;
            Serializer.Transfer(archive, ref entityId);

            condition.Serialize(archive, owner);

            return NetMessageAddCondition.CreateBuilder().SetArchiveData(archive.ToByteString()).Build();
        }

        public static NetMessageUpdateMiniMap BuildUpdateMiniMapMessage(LowResMap lowResMap)
        {
            // NOTE: NetMessageUpdateMiniMap always uses the default policy values
            using Archive archive = new(ArchiveSerializeType.Replication, (ulong)AOINetworkPolicyValues.DefaultPolicy);

            Serializer.Transfer(archive, ref lowResMap);

            return NetMessageUpdateMiniMap.CreateBuilder().SetArchiveData(archive.ToByteString()).Build();
        }
    }
}
