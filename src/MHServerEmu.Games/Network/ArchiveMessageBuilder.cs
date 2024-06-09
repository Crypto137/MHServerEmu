using Gazillion;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Powers;

namespace MHServerEmu.Games.Network
{
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
        /// <summary>
        /// Builds <see cref="NetMessageEntityEnterGameWorld"/> for the provided <see cref="WorldEntity"/>.
        /// </summary>
        public static NetMessageEntityEnterGameWorld BuildEntityEnterGameWorldMessage(WorldEntity worldEntity, EntitySettings settings = null)
        {
            // Build flags
            LocomotionMessageFlags locoFieldFlags = LocomotionMessageFlags.None;
            EnterGameWorldMessageFlags extraFieldFlags = EnterGameWorldMessageFlags.None;

            // Position
            // TODO: Use RegionLocation
            Vector3 position = worldEntity.BasePosition;
            Orientation orientation = worldEntity.BaseOrientation;

            if (orientation.Pitch != 0f || orientation.Yaw != 0f)
                locoFieldFlags |= LocomotionMessageFlags.HasFullOrientation;

            // LocomotionState
            // TODO: Get real locomotion state from the entity
            LocomotionState locomotionState = null;

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
    }
}
