using System.Text;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Powers.Conditions;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions.ObjectiveGraphs;

// This was previously used for our packet parsing functionality, which we no longer need. I am leaving this here just for reference.

#if false
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

        public static void ParseRegionChangeArchiveData(Archive archive, StringBuilder sb)
        {
            sb.AppendLine("properties:");
            ParseReplicatedPropertyCollection(archive, sb);

            sb.AppendLine("missionManager:");
            ParseMissionManager(archive, sb);

            sb.AppendLine("uiDataProvider:");
            ParseUIDataProvider(archive, sb);

            sb.AppendLine("objectiveGraph:");
            ParseObjectiveGraph(archive, sb);
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

            if (fieldFlags.HasFlag(ConditionSerializationFlags.CreatorIsOwner) == false)
            {
                ulong creatorId = 0;
                Serializer.Transfer(archive, ref creatorId);
                sb.AppendLine($"creatorId: {creatorId}");
            }

            if (fieldFlags.HasFlag(ConditionSerializationFlags.CreatorIsUltimateCreator) == false)
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

            if (fieldFlags.HasFlag(ConditionSerializationFlags.HasOwnerAssetRefOverride))
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

            if (fieldFlags.HasFlag(ConditionSerializationFlags.HasUpdateIntervalOverride))
            {
                int updateIntervalMS = 0;
                Serializer.Transfer(archive, ref updateIntervalMS);
                sb.AppendLine($"updateIntervalMS: {updateIntervalMS}");
            }

            sb.Append("properties: ");
            ParseReplicatedPropertyCollection(archive, sb);

            if (fieldFlags.HasFlag(ConditionSerializationFlags.HasCancelOnFlagsOverride))
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

        private static void ParseMissionManager(Archive archive, StringBuilder sb)
        {
            PrototypeId avatarPrototypeRef = PrototypeId.Invalid;
            Serializer.Transfer(archive, ref avatarPrototypeRef);
            sb.AppendLine($"\tavatarPrototypeRef: {avatarPrototypeRef.GetName()}");

            // Missions
            ulong numMissions = 0;
            Serializer.Transfer(archive, ref numMissions);
            for (ulong i = 0; i < numMissions; i++)
            {
                ulong guid = 0;
                Serializer.Transfer(archive, ref guid);
                PrototypeId protoRef = GameDatabase.GetDataRefByPrototypeGuid((PrototypeGuid)guid);

                sb.AppendLine($"\tmissions[{protoRef.GetNameFormatted()}]:");
                ParseMission(archive, sb);
            }

            // Legendary mission blacklist
            int numBlacklistCategories = 0;
            Serializer.Transfer(archive, ref numBlacklistCategories);

            sb.AppendLine("\tlegendaryMissionBlacklist:");
            for (int i = 0; i < numBlacklistCategories; i++)
            {
                ulong categoryGuid = 0;
                Serializer.Transfer(archive, ref categoryGuid);
                PrototypeId categoryProtoRef = GameDatabase.GetDataRefByPrototypeGuid((PrototypeGuid)categoryGuid);

                sb.AppendLine($"\t\tlegendaryMissionBlacklist[{categoryProtoRef.GetName()}]");

                ulong numCategoryMissions = 0;
                Serializer.Transfer(archive, ref numCategoryMissions);
                for (ulong j = 0; j < numCategoryMissions; j++)
                {
                    ulong categoryMissionGuid = 0;
                    Serializer.Transfer(archive, ref categoryMissionGuid);
                    PrototypeId categoryMissionProtoRef = GameDatabase.GetDataRefByPrototypeGuid((PrototypeGuid)categoryMissionGuid);

                    sb.AppendLine($"\t\t\t{categoryMissionProtoRef.GetName()}");
                }
            }
        }

        private static void ParseMission(Archive archive, StringBuilder sb)
        {
            int state = 0;
            Serializer.Transfer(archive, ref state);
            sb.AppendLine($"\t\tstate: {(MissionState)state}");

            TimeSpan timeExpireCurrentState = TimeSpan.Zero;
            Serializer.Transfer(archive, ref timeExpireCurrentState);
            string expireTime = timeExpireCurrentState != TimeSpan.Zero ? Clock.GameTimeToDateTime(timeExpireCurrentState).ToString() : "0";
            sb.AppendLine($"\t\ttimeExpireCurrentState: {expireTime}");

            PrototypeId prototypeDataRef = PrototypeId.Invalid;
            Serializer.Transfer(archive, ref prototypeDataRef);
            sb.AppendLine($"\t\tprototypeDataRef: {prototypeDataRef.GetName()}");

            int lootSeed = 0;
            Serializer.Transfer(archive, ref lootSeed);
            sb.AppendLine($"\t\tlootSeed: {lootSeed}");

            sb.AppendLine("\t\tobjectives:");
            ParseMissionObjectives(archive, sb);

            sb.Append("\t\tparticipants: ");
            ulong numParticipants = 0;
            Serializer.Transfer(archive, ref numParticipants);
            for (ulong i = 0; i < numParticipants; i++)
            {
                ulong participantId = 0;
                Serializer.Transfer(archive, ref participantId);
                sb.Append($"{participantId} ");
            }
            sb.AppendLine();

            bool isSuspended = false;
            Serializer.Transfer(archive, ref isSuspended);
            sb.AppendLine($"\t\tisSuspended: {isSuspended}");
        }

        private static void ParseMissionObjectives(Archive archive, StringBuilder sb)
        {
            ulong numObjectives = 0;
            Serializer.Transfer(archive, ref numObjectives);

            for (ulong i = 0; i < numObjectives; i++)
            {
                byte index = 0;
                Serializer.Transfer(archive, ref index);
                sb.Append($"\t\t\tobjectives[{index}]: ");

                Serializer.Transfer(archive, ref index);    // index is serialized twice in a row

                int state = 0;
                Serializer.Transfer(archive, ref state);

                TimeSpan objectiveStateExpireTime = TimeSpan.Zero;
                Serializer.Transfer(archive, ref objectiveStateExpireTime);
                string expireTime = objectiveStateExpireTime != TimeSpan.Zero ? Clock.GameTimeToDateTime(objectiveStateExpireTime).ToString() : "0";

                uint numInteractions = 0;   // this is always zero in all of our packets
                Serializer.Transfer(archive, ref numInteractions);
                for (uint j = 0; j < numInteractions; j++)
                {
                    ulong entityId = 0;
                    Serializer.Transfer(archive, ref entityId);
                    ulong regionId = 0;
                    Serializer.Transfer(archive, ref regionId);
                }

                ushort currentCount = 0;
                Serializer.Transfer(archive, ref currentCount);
                ushort requiredCount = 0;
                Serializer.Transfer(archive, ref requiredCount);
                ushort failCurrentCount = 0;
                Serializer.Transfer(archive, ref failCurrentCount);
                ushort failRequiredCount = 0;
                Serializer.Transfer(archive, ref failRequiredCount);

                sb.AppendLine($"state={(MissionObjectiveState)state}, expireTime={expireTime}, numInteractions={numInteractions}, count={currentCount}/{requiredCount}, failCount={failCurrentCount}/{failRequiredCount}");
            }
        }

        private static void ParseUIDataProvider(Archive archive, StringBuilder sb)
        {
            uint numWidgets = 0;
            Serializer.Transfer(archive, ref numWidgets);

            for (uint i = 0; i < numWidgets; i++)
            {
                PrototypeId widgetRef = PrototypeId.Invalid;
                Serializer.Transfer(archive, ref widgetRef);
                PrototypeId contextRef = PrototypeId.Invalid;
                Serializer.Transfer(archive, ref contextRef);

                sb.AppendLine($"\tdata[({widgetRef.GetName()}, {contextRef.GetName()})]:");
                ParseUISyncData(archive, sb, widgetRef);
            }
        }

        private static void ParseUISyncData(Archive archive, StringBuilder sb, PrototypeId widgetRef)
        {
            int numAreas = 0;
            Serializer.Transfer(archive, ref numAreas);

            sb.AppendLine($"\t\tareas:");
            for (int i = 0; i < numAreas; i++)
            {
                PrototypeId areaProtoRef = 0;
                Serializer.Transfer(archive, ref areaProtoRef);
                sb.AppendLine($"\t\t\tareas[{i}]: {areaProtoRef.GetName()}");
            }

            Prototype widgetProto = GameDatabase.GetPrototype<Prototype>(widgetRef);

            switch (widgetProto)
            {
                case UIWidgetButtonPrototype:           ParseUIWidgetButton(archive, sb); break;
                case UIWidgetEntityIconsPrototype:      ParseUIWidgetEntityIcons(archive, sb); break;
                case UIWidgetGenericFractionPrototype:  ParseUIWidgetGenericFraction(archive, sb); break;
                case UIWidgetMissionTextPrototype:      ParseUIWidgetMissionText(archive, sb); break;
                case UIWidgetReadyCheckPrototype:       ParseUIWidgetReadyCheck(archive, sb); break;

                default: throw new NotImplementedException();
            }
        }

        private static void ParseUIWidgetButton(Archive archive, StringBuilder sb)
        {
            throw new NotImplementedException();
        }

        private static void ParseUIWidgetEntityIcons(Archive archive, StringBuilder sb)
        {
            throw new NotImplementedException();
        }

        private static void ParseUIWidgetGenericFraction(Archive archive, StringBuilder sb)
        {
            int currentCount = 0;
            Serializer.Transfer(archive, ref currentCount);
            int totalCount = 0;
            Serializer.Transfer(archive, ref totalCount);
            sb.AppendLine($"\t\tcount: {currentCount} / {totalCount}");

            long timeStart = 0;
            Serializer.Transfer(archive, ref timeStart);
            sb.AppendLine($"\t\ttimeStart: {(timeStart != 0 ? Clock.GameTimeMillisecondsToDateTime(timeStart) : 0)}");

            long timeEnd = 0;
            Serializer.Transfer(archive, ref timeEnd);
            sb.AppendLine($"\t\ttimeEnd: {(timeEnd != 0 ? Clock.GameTimeMillisecondsToDateTime(timeEnd) : 0)}");

            bool timePaused = false;
            Serializer.Transfer(archive, ref timePaused);
            sb.AppendLine($"\t\ttimePaused: {timePaused}");
        }

        private static void ParseUIWidgetMissionText(Archive archive, StringBuilder sb)
        {
            ulong missionName = 0;
            Serializer.Transfer(archive, ref missionName);
            sb.AppendLine($"\t\tmissionName: {missionName}");

            ulong missionObjectiveName = 0;
            Serializer.Transfer(archive, ref missionObjectiveName);
            sb.AppendLine($"\t\tmissionObjectiveName: {missionObjectiveName}");
        }

        private static void ParseUIWidgetReadyCheck(Archive archive, StringBuilder sb)
        {
            throw new NotImplementedException();
        }

        private static void ParseObjectiveGraph(Archive archive, StringBuilder sb)
        {
            sb.AppendLine("\tnodes:");

            uint numNodes = 0;
            Serializer.Transfer(archive, ref numNodes);
            for (uint i = 0; i < numNodes; i++)
            {
                ulong id = 0;
                Serializer.Transfer(archive, ref id);
                sb.AppendLine($"\t\tid: {id}");

                Vector3 position = Vector3.Zero;
                Serializer.Transfer(archive, ref position);
                sb.AppendLine($"\t\tposition: {position}");

                sb.AppendLine("\t\tareas:");
                ulong numAreas = 0;
                Serializer.Transfer(archive, ref numAreas);
                for (ulong j = 0; j < numAreas; j++)
                {
                    ulong area = 0;
                    Serializer.Transfer(archive, ref area);
                    sb.AppendLine($"\t\t\tareas[{j}]: {area}");
                }

                sb.AppendLine("\t\tcells:");
                ulong numCells = 0;
                Serializer.Transfer(archive, ref numCells);
                for (ulong j = 0; j < numCells; j++)
                {
                    ulong cell = 0;
                    Serializer.Transfer(archive, ref cell);
                    sb.AppendLine($"\t\t\tcells[{j}]: {cell}");
                }

                uint type = 0;
                Serializer.Transfer(archive, ref type);
                sb.AppendLine($"\t\ttype: {(ObjectiveGraphType)type}");

                uint index = 0;
                Serializer.Transfer(archive, ref index);
                sb.AppendLine($"\t\tindex: {index}");

            }

            sb.AppendLine("\tconnections:");

            uint numConnections = 0;
            Serializer.Transfer(archive, ref numConnections);
            for (uint i = 0; i < numConnections; i++)
            {
                uint nodeIndex0 = 0;
                Serializer.Transfer(archive, ref nodeIndex0);
                uint nodeIndex1 = 0;
                Serializer.Transfer(archive, ref nodeIndex1);
                float distance = 0f;
                Serializer.Transfer(archive, ref distance);

                sb.AppendLine($"\t\tconnections[{i}]: [{nodeIndex0}] <- {distance}f -> [{nodeIndex1}]");
            }
        }
    }
}
#endif
