using System.Text;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.Network.Parsing
{
    public static class ArchiveParser
    {
        public static void ParseEntityCreateBaseData(Archive archive, StringBuilder sb, out ulong entityId, out PrototypeId entityPrototypeRef)
        {
            entityId = 0;
            Serializer.Transfer(archive, ref entityId);
            sb.AppendLine($"entityId: {entityId}");

            entityPrototypeRef = 0;
            Serializer.TransferPrototypeEnum<EntityPrototype>(archive, ref entityPrototypeRef);
            sb.AppendLine($"entityPrototypeRef: {GameDatabase.GetPrototypeName(entityPrototypeRef)}");

            uint fieldFlagsRaw = 0;
            Serializer.Transfer(archive, ref fieldFlagsRaw);
            var fieldFlags = (EntityCreateMessageFlags)fieldFlagsRaw;
            sb.AppendLine($"fieldFlags: {fieldFlags}");

            uint locoFieldFlagsRaw = 0;
            Serializer.Transfer(archive, ref locoFieldFlagsRaw);
            var locoFieldFlags = (LocomotionMessageFlags)locoFieldFlagsRaw;
            sb.AppendLine($"locoFieldFlags: {locoFieldFlags}");

            uint interestPolicies = 1;  // Proximity
            if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasNonProximityInterest))
                Serializer.Transfer(archive, ref interestPolicies);
            sb.AppendLine($"interestPolicies: {(AOINetworkPolicyValues)interestPolicies}");

            if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasAvatarWorldInstanceId))
            {
                uint avatarWorldInstanceId = 0;
                Serializer.Transfer(archive, ref avatarWorldInstanceId);
                sb.AppendLine($"avatarWorldInstanceId: {avatarWorldInstanceId}");
            }

            if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasDbId))
            {
                ulong dbId = 0;
                Serializer.Transfer(archive, ref dbId);
                sb.AppendLine($"dbId: 0x{dbId:X}");
            }

            if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasPositionAndOrientation))
            {
                Vector3 position = Vector3.Zero;
                Serializer.TransferVectorFixed(archive, ref position, 3);
                sb.AppendLine($"position: {position}");

                bool yawOnly = locoFieldFlags.HasFlag(LocomotionMessageFlags.HasFullOrientation) == false;
                Orientation orientation = Orientation.Zero;
                Serializer.TransferOrientationFixed(archive, ref orientation, yawOnly, 6);
                sb.AppendLine($"orientation: {orientation}");
            }

            if (locoFieldFlags.HasFlag(LocomotionMessageFlags.NoLocomotionState) == false)
                ParseLocomotionState(archive, locoFieldFlags, sb);

            if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasBoundsScaleOverride))
            {
                float boundsScaleOverride = 0f;
                Serializer.TransferFloatFixed(archive, ref boundsScaleOverride, 8);
                sb.AppendLine($"boundsScaleOverride: {boundsScaleOverride}f");
            }

            if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasSourceEntityId))
            {
                ulong sourceEntityId = 0;
                Serializer.Transfer(archive, ref sourceEntityId);
                sb.AppendLine($"sourceEntityId: {sourceEntityId}");
            }

            if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasSourcePosition))
            {
                Vector3 sourcePosition = Vector3.Zero;
                Serializer.TransferVectorFixed(archive, ref sourcePosition, 3);
                sb.AppendLine($"sourcePosition: {sourcePosition}");
            }

            if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasActivePowerPrototypeRef))
            {
                PrototypeId activePowerPrototypeRef = 0;
                Serializer.TransferPrototypeEnum<PowerPrototype>(archive, ref activePowerPrototypeRef);
                sb.AppendLine($"activePowerPrototypeRef: {GameDatabase.GetPrototypeName(activePowerPrototypeRef)}");
            }

            if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasInvLoc))
            {
                sb.Append("invLoc: ");
                ParseInventoryLocation(archive, sb);
            }

            if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasInvLocPrev))
            {
                sb.Append("invLocPrev: ");
                ParseInventoryLocation(archive, sb);
            }

            if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasAttachedEntities))
            {
                ulong numAttachedEntities = 0;
                Serializer.Transfer(archive, ref numAttachedEntities);

                for (ulong i = 0; i < numAttachedEntities; i++)
                {
                    ulong attachedEntityId = 0;
                    Serializer.Transfer(archive, ref attachedEntityId);
                    sb.AppendLine($"attachedEntities[{i}]: {attachedEntityId}");
                }
            }
        }

        public static void ParseLocomotionStateUpdate(Archive archive, StringBuilder sb)
        {
            ulong entityId = 0;
            Serializer.Transfer(archive, ref entityId);
            sb.AppendLine($"entityId: {entityId}");

            uint flagsRaw = 0;
            Serializer.Transfer(archive, ref flagsRaw);
            var fieldFlags = (LocomotionMessageFlags)flagsRaw;
            sb.AppendLine($"fieldFlags: {fieldFlags}");

            if (fieldFlags.HasFlag(LocomotionMessageFlags.HasEntityPrototypeRef))
            {
                PrototypeId entityPrototypeRef = 0;
                Serializer.TransferPrototypeEnum<EntityPrototype>(archive, ref entityPrototypeRef);
                sb.AppendLine($"entityPrototypeRef: {GameDatabase.GetPrototypeName(entityPrototypeRef)}");
            }

            Vector3 position = Vector3.Zero;
            Serializer.TransferVectorFixed(archive, ref position, 3);
            sb.AppendLine($"position: {position}");

            bool yawOnly = fieldFlags.HasFlag(LocomotionMessageFlags.HasFullOrientation) == false;
            Orientation orientation = Orientation.Zero;
            Serializer.TransferOrientationFixed(archive, ref orientation, yawOnly, 6);
            sb.AppendLine($"orientation: {orientation}");

            if (fieldFlags.HasFlag(LocomotionMessageFlags.NoLocomotionState) == false)
                ParseLocomotionState(archive, fieldFlags, sb);
        }

        public static void ParseActivatePower(Archive archive, StringBuilder sb)
        {
            uint flagsRaw = 0;
            Serializer.Transfer(archive, ref flagsRaw);
            var fieldFlags = (ActivatePowerMessageFlags)flagsRaw;
            sb.AppendLine($"fieldFlags: {fieldFlags}");

            ulong userEntityId = 0;
            Serializer.Transfer(archive, ref userEntityId);
            sb.AppendLine($"userEntityId: {userEntityId}");

            if (fieldFlags.HasFlag(ActivatePowerMessageFlags.TargetIsUser) == false)
            {
                ulong targetEntityId = 0;
                Serializer.Transfer(archive, ref targetEntityId);
                sb.AppendLine($"targetEntityId: {targetEntityId}");
            }

            PrototypeId powerPrototypeRef = 0;
            Serializer.TransferPrototypeEnum<PowerPrototype>(archive, ref powerPrototypeRef);
            sb.AppendLine($"powerPrototypeRef: {GameDatabase.GetPrototypeName(powerPrototypeRef)}");

            if (fieldFlags.HasFlag(ActivatePowerMessageFlags.HasTriggeringPowerPrototypeRef))
            {
                PrototypeId triggeringPowerPrototypeRef = 0;
                Serializer.TransferPrototypeEnum<PowerPrototype>(archive, ref triggeringPowerPrototypeRef);
                sb.AppendLine($"triggeringPowerPrototypeRef: {GameDatabase.GetPrototypeName(triggeringPowerPrototypeRef)}");
            }

            Vector3 userPosition = Vector3.Zero;
            archive.TransferVectorFixed(ref userPosition, 2);
            sb.AppendLine($"userPosition: {userPosition}");

            if (fieldFlags.HasFlag(ActivatePowerMessageFlags.HasTargetPosition))
            {
                Vector3 targetOffset = Vector3.Zero;
                archive.TransferVectorFixed(ref targetOffset, 2);
                sb.AppendLine($"targetPosition: {userPosition + targetOffset}");
            }

            if (fieldFlags.HasFlag(ActivatePowerMessageFlags.HasMovementTime))
            {
                uint movementTimeMS = 0;
                Serializer.Transfer(archive, ref movementTimeMS);
                sb.AppendLine($"movementTimeMS: {movementTimeMS}");
            }

            if (fieldFlags.HasFlag(ActivatePowerMessageFlags.HasVariableActivationTime))
            {
                uint variableActivationTimeMS = 0;
                Serializer.Transfer(archive, ref variableActivationTimeMS);
                sb.AppendLine($"variableActivationTimeMS: {variableActivationTimeMS}");
            }

            if (fieldFlags.HasFlag(ActivatePowerMessageFlags.HasPowerRandomSeed))
            {
                uint powerRandomSeed = 0;
                Serializer.Transfer(archive, ref powerRandomSeed);
                sb.AppendLine($"powerRandomSeed: {powerRandomSeed}");
            }

            if (fieldFlags.HasFlag(ActivatePowerMessageFlags.HasFXRandomSeed))
            {
                uint fxRandomSeed = 0;
                Serializer.Transfer(archive, ref fxRandomSeed);
                sb.AppendLine($"fxRandomSeed: {fxRandomSeed}");
            }
        }

        public static void ParsePowerResult(Archive archive, StringBuilder sb)
        {
            uint fieldFlagsRaw = 0;
            Serializer.Transfer(archive, ref fieldFlagsRaw);
            var fieldFlags = (PowerResultMessageFlags)fieldFlagsRaw;
            sb.AppendLine($"fieldFlags: {fieldFlags}");

            PrototypeId powerPrototypeRef = 0;
            Serializer.TransferPrototypeEnum<PowerPrototype>(archive, ref powerPrototypeRef);
            sb.AppendLine($"powerPrototypeRef: {GameDatabase.GetPrototypeName(powerPrototypeRef)}");

            ulong targetEntityId = 0;
            Serializer.Transfer(archive, ref targetEntityId);
            sb.AppendLine($"targetEntityId: {targetEntityId}");

            if (fieldFlags.HasFlag(PowerResultMessageFlags.IsSelfTarget) == false && fieldFlags.HasFlag(PowerResultMessageFlags.NoPowerOwnerEntityId) == false)
            {
                ulong powerOwnerEntityId = 0;
                Serializer.Transfer(archive, ref powerOwnerEntityId);
                sb.AppendLine($"powerOwnerEntityId: {powerOwnerEntityId}");
            }

            if (fieldFlags.HasFlag(PowerResultMessageFlags.UltimateOwnerIsPowerOwner) == false && fieldFlags.HasFlag(PowerResultMessageFlags.NoUltimateOwnerEntityId))
            {
                ulong ultimateOwnerEntityId = 0;
                Serializer.Transfer(archive, ref ultimateOwnerEntityId);
                sb.AppendLine($"ultimateOwnerEntityId: {ultimateOwnerEntityId}");
            }

            if (fieldFlags.HasFlag(PowerResultMessageFlags.HasResultFlags))
            {
                ulong resultFlags = 0;
                Serializer.Transfer(archive, ref resultFlags);
                sb.AppendLine($"resultFlags: {(PowerResultFlags)resultFlags}");
            }

            if (fieldFlags.HasFlag(PowerResultMessageFlags.HasDamagePhysical))
            {
                uint damagePhysical = 0;
                Serializer.Transfer(archive, ref damagePhysical);
                sb.AppendLine($"damagePhysical: {damagePhysical}");
            }

            if (fieldFlags.HasFlag(PowerResultMessageFlags.HasDamageEnergy))
            {
                uint damageEnergy = 0;
                Serializer.Transfer(archive, ref damageEnergy);
                sb.AppendLine($"damageEnergy: {damageEnergy}");
            }

            if (fieldFlags.HasFlag(PowerResultMessageFlags.HasDamageMental))
            {
                uint damageMental = 0;
                Serializer.Transfer(archive, ref damageMental);
                sb.AppendLine($"damageMental: {damageMental}");
            }

            if (fieldFlags.HasFlag(PowerResultMessageFlags.HasHealing))
            {
                uint healing = 0;
                Serializer.Transfer(archive, ref healing);
                sb.AppendLine($"healing: {healing}");
            }

            if (fieldFlags.HasFlag(PowerResultMessageFlags.HasPowerAssetRefOverride))
            {
                ulong powerAssetRefOverride = 0;
                Serializer.Transfer(archive, ref powerAssetRefOverride);
                sb.AppendLine($"powerAssetRefOverride: {powerAssetRefOverride}");
            }

            if (fieldFlags.HasFlag(PowerResultMessageFlags.HasPowerOwnerPosition))
            {
                Vector3 powerOwnerPosition = Vector3.Zero;
                Serializer.Transfer(archive, ref powerOwnerPosition);
                sb.AppendLine($"powerOwnerPosition: {powerOwnerPosition}");
            }

            if (fieldFlags.HasFlag(PowerResultMessageFlags.HasTransferToEntityId))
            {
                ulong transferToEntityId = 0;
                Serializer.Transfer(archive, ref transferToEntityId);
                sb.AppendLine($"transferToEntityId: {transferToEntityId}");
            }
        }

        public static void ParseEntityEnterGameWorld(Archive archive, StringBuilder sb)
        {
            ulong entityId = 0;
            Serializer.Transfer(archive, ref entityId);
            sb.AppendLine($"entityId: {entityId}");

            uint flags = 0;
            Serializer.Transfer(archive, ref flags);
            var locoFieldFlags = (LocomotionMessageFlags)(flags & 0xFFF);
            var extraFieldFlags = (EnterGameWorldMessageFlags)(flags >> 12);
            sb.AppendLine($"locoFieldFlags: {locoFieldFlags}");
            sb.AppendLine($"extraFieldFlags: {extraFieldFlags}");

            if (locoFieldFlags.HasFlag(LocomotionMessageFlags.HasEntityPrototypeRef))
            {
                PrototypeId entityPrototypeRef = 0;
                Serializer.TransferPrototypeEnum<EntityPrototype>(archive, ref entityPrototypeRef);
                sb.AppendLine($"entityPrototypeRef: {GameDatabase.GetPrototypeName(entityPrototypeRef)}");
            }

            Vector3 position = Vector3.Zero;
            archive.TransferVectorFixed(ref position, 3);
            sb.AppendLine($"position: {position}");

            bool yawOnly = locoFieldFlags.HasFlag(LocomotionMessageFlags.HasFullOrientation) == false;
            Orientation orientation = Orientation.Zero;
            archive.TransferOrientationFixed(ref orientation, yawOnly, 6);
            sb.AppendLine($"orientation: {orientation}");

            if (locoFieldFlags.HasFlag(LocomotionMessageFlags.NoLocomotionState) == false)
                ParseLocomotionState(archive, locoFieldFlags, sb);

            if (extraFieldFlags.HasFlag(EnterGameWorldMessageFlags.HasAvatarWorldInstanceId))
            {
                uint avatarWorldInstanceId = 0;
                Serializer.Transfer(archive, ref avatarWorldInstanceId);
                sb.AppendLine($"avatarWorldInstanceId: {avatarWorldInstanceId}");
            }

            if (extraFieldFlags.HasFlag(EnterGameWorldMessageFlags.HasAttachedEntities))
            {
                ulong numAttachedEntities = 0;
                Serializer.Transfer(archive, ref numAttachedEntities);

                for (ulong i = 0; i < numAttachedEntities; i++)
                {
                    ulong attachedEntityId = 0;
                    Serializer.Transfer(archive, ref attachedEntityId);
                    sb.AppendLine($"attachedEntities[{i}]: {attachedEntityId}");
                }
            }
        }

        public static void ParseAddCondition(Archive archive, StringBuilder sb)
        {
            ulong entityId = 0;
            Serializer.Transfer(archive, ref entityId);
            sb.AppendLine($"entityId: {entityId}");

            ParseCondition(archive, sb);
        }

        private static void ParseLocomotionState(Archive archive, LocomotionMessageFlags flags, StringBuilder sb)
        {
            if (flags.HasFlag(LocomotionMessageFlags.HasLocomotionFlags))
            {
                ulong locomotionFlags = 0;
                Serializer.Transfer(archive, ref locomotionFlags);
                sb.AppendLine($"locomotionFlags: {(LocomotionFlags)locomotionFlags}");
            }

            if (flags.HasFlag(LocomotionMessageFlags.HasMethod))
            {
                uint method = 0;
                Serializer.Transfer(archive, ref method);
                sb.AppendLine($"method: {(LocomotorMethod)method}");
            }

            if (flags.HasFlag(LocomotionMessageFlags.HasMoveSpeed))
            {
                float moveSpeed = 0f;
                archive.TransferFloatFixed(ref moveSpeed, 0);
                sb.AppendLine($"moveSpeed: {moveSpeed}f");
            }

            if (flags.HasFlag(LocomotionMessageFlags.HasHeight))
            {
                uint height = 0;
                Serializer.Transfer(archive, ref height);
                sb.AppendLine($"height: {height}");
            }

            if (flags.HasFlag(LocomotionMessageFlags.HasFollowEntityId))
            {
                ulong followEntityId = 0;
                Serializer.Transfer(archive, ref followEntityId);
                sb.AppendLine($"followEntityId: {followEntityId}");
            }

            if (flags.HasFlag(LocomotionMessageFlags.HasFollowEntityRange))
            {
                float followEntityRangeStart = 0f;
                Serializer.Transfer(archive, ref followEntityRangeStart);
                sb.AppendLine($"followEntityRangeStart: {followEntityRangeStart}");

                float followEntityRangeEnd = 0f;
                Serializer.Transfer(archive, ref followEntityRangeEnd);
                sb.AppendLine($"followEntityRangeEnd: {followEntityRangeEnd}");
            }

            if (flags.HasFlag(LocomotionMessageFlags.UpdatePathNodes))
            {
                uint pathGoalNodeIndex = 0;
                Serializer.Transfer(archive, ref pathGoalNodeIndex);
                sb.AppendLine($"pathGoalNodeIndex: {pathGoalNodeIndex}");

                uint pathNodeCount = 0;
                Serializer.Transfer(archive, ref pathNodeCount);
                sb.AppendLine($"pathNodeCount: {pathNodeCount}");

                if (pathNodeCount > 0)
                {
                    Vector3 previousVertex = Vector3.Zero;
                    for (uint i = 0; i < pathNodeCount; i++)
                    {
                        sb.Append($"pathNodes[{i}]: ");
                        previousVertex = ParseNaviPathNode(archive, previousVertex, sb);
                    }
                }
            }
        }

        private static Vector3 ParseNaviPathNode(Archive archive, Vector3 previousVertex, StringBuilder sb)
        {
            Vector3 offset = Vector3.Zero;
            archive.TransferVectorFixed(ref offset, 3);
            Vector3 vertex = previousVertex + offset;


            int vertexSideRadius = 0;
            Serializer.Transfer(archive, ref vertexSideRadius);

            NaviSide vertexSide = NaviSide.Point;
            float radius = 0f;

            if (vertexSideRadius < 0)
            {
                vertexSide = NaviSide.Left;
                radius = -vertexSideRadius;
            }
            else if (vertexSideRadius > 0)
            {
                vertexSide = NaviSide.Right;
                radius = vertexSideRadius;
            }

            sb.AppendLine($"vertex={vertex}, vertexSide={vertexSide}, radius={radius}");

            return vertex;
        }

        private static void ParseInventoryLocation(Archive archive, StringBuilder sb)
        {
            ulong containerId = 0;
            Serializer.Transfer(archive, ref containerId);

            PrototypeId inventoryRef = 0;
            Serializer.TransferPrototypeEnum<InventoryPrototype>(archive, ref inventoryRef);
            string inventoryRefName = GameDatabase.GetPrototypeName(inventoryRef);

            uint slot = 0;
            Serializer.Transfer(archive, ref slot);

            sb.AppendLine($"containerId={containerId}, inventoryRef={inventoryRefName}, slot={slot}");
        }

        private static void ParseCondition(Archive archive, StringBuilder sb)
        {
            uint fieldFlagsRaw = 0;
            Serializer.Transfer(archive, ref fieldFlagsRaw);
            var fieldFlags = (ConditionSerializationFlags)fieldFlagsRaw;
            sb.AppendLine($"fieldFlags: {fieldFlags}");

            ulong conditionId = 0;
            Serializer.Transfer(archive, ref conditionId);
            sb.AppendLine($"conditionId: {conditionId}");

            if (fieldFlags.HasFlag(ConditionSerializationFlags.NoCreatorId) == false)
            {
                ulong creatorId = 0;
                Serializer.Transfer(archive, ref creatorId);
                sb.AppendLine($"creatorId: {creatorId}");
            }

            if (fieldFlags.HasFlag(ConditionSerializationFlags.NoUltimateCreatorId) == false)
            {
                ulong ultimateCreatorId = 0;
                Serializer.Transfer(archive, ref ultimateCreatorId);
                sb.AppendLine($"ultimateCreatorId: {ultimateCreatorId}");
            }

            if (fieldFlags.HasFlag(ConditionSerializationFlags.NoConditionPrototypeRef) == false)
            {
                PrototypeId conditionPrototypeRef = 0;
                Serializer.Transfer(archive, ref conditionPrototypeRef);
                sb.AppendLine($"conditionPrototypeRef: {GameDatabase.GetPrototypeName(conditionPrototypeRef)}");
            }

            if (fieldFlags.HasFlag(ConditionSerializationFlags.NoCreatorPowerPrototypeRef) == false)
            {
                PrototypeId creatorPowerPrototypeRef = 0;
                Serializer.Transfer(archive, ref creatorPowerPrototypeRef);
                sb.AppendLine($"creatorPowerPrototypeRef: {GameDatabase.GetPrototypeName(creatorPowerPrototypeRef)}");
            }

            if (fieldFlags.HasFlag(ConditionSerializationFlags.HasCreatorPowerIndex))
            {
                uint creatorPowerIndex = 0;
                Serializer.Transfer(archive, ref creatorPowerIndex);
                sb.AppendLine($"creatorPowerIndex: {creatorPowerIndex}");
            }

            if (fieldFlags.HasFlag(ConditionSerializationFlags.OwnerAssetRefOverride))
            {
                ulong ownerAssetRef = 0;
                Serializer.Transfer(archive, ref ownerAssetRef);
                sb.AppendLine($"ownerAssetRef: {ownerAssetRef}");
            }

            long startTime = 0;
            Serializer.Transfer(archive, ref startTime);
            sb.AppendLine($"startTime: {startTime}");

            if (fieldFlags.HasFlag(ConditionSerializationFlags.HasPauseTime))
            {
                long pauseTime = 0;
                Serializer.Transfer(archive, ref pauseTime);
                sb.AppendLine($"pauseTime: {pauseTime}");
            }

            if (fieldFlags.HasFlag(ConditionSerializationFlags.HasDuration))
            {
                long duration = 0;
                Serializer.Transfer(archive, ref duration);
                sb.AppendLine($"duration: {duration}");
            }

            if (fieldFlags.HasFlag(ConditionSerializationFlags.UpdateIntervalOverride))
            {
                int updateIntervalMS = 0;
                Serializer.Transfer(archive, ref updateIntervalMS);
                sb.AppendLine($"updateIntervalMS: {updateIntervalMS}");
            }

            sb.Append("properties: ");
            ParseReplicatedPropertyCollection(archive, sb);

            if (fieldFlags.HasFlag(ConditionSerializationFlags.CancelOnFlagsOverride))
            {
                uint cancelOnFlags = 0;
                Serializer.Transfer(archive, ref cancelOnFlags);
                sb.AppendLine($"cancelOnFlags: {(ConditionCancelOnFlags)cancelOnFlags}");
            }
        }

        private static void ParseReplicatedPropertyCollection(Archive archive, StringBuilder sb)
        {
            ReplicatedPropertyCollection properties = new();
            properties.Serialize(archive);
            sb.AppendLine(properties.ToString());
        }
    }
}
