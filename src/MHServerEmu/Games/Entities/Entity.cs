using System.Text;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Entities
{
    public class Entity
    {
        public EntityBaseData BaseData { get; set; }
        public ulong RegionId { get; set; } = 0;

        public AoiNetworkPolicyValues ReplicationPolicy { get; set; }
        public ReplicatedPropertyCollection PropertyCollection { get; set; }

        protected EntityFlags Flags;
        public EntityPrototype EntityPrototype { get => GameDatabase.GetPrototype<EntityPrototype>(BaseData.PrototypeId); }

        public Entity(EntityBaseData baseData, ByteString archiveData)
        {
            BaseData = baseData;
            CodedInputStream stream = CodedInputStream.CreateInstance(archiveData.ToByteArray());
            Decode(stream);
        }

        // Base data is required for all entities, so there's no parameterless constructor
        public Entity(EntityBaseData baseData) { BaseData = baseData; }

        public Entity(EntityBaseData baseData, AoiNetworkPolicyValues replicationPolicy, ReplicatedPropertyCollection propertyCollection)
        {
            BaseData = baseData;
            ReplicationPolicy = replicationPolicy;
            PropertyCollection = propertyCollection;
        }

        protected virtual void Decode(CodedInputStream stream)
        {
            ReplicationPolicy = (AoiNetworkPolicyValues)stream.ReadRawVarint32();
            PropertyCollection = new(stream);
        }

        public virtual void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint32((uint)ReplicationPolicy);
            PropertyCollection.Encode(stream);
        }

        public ByteString Serialize()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);
                Encode(cos);
                cos.Flush();
                return ByteString.CopyFrom(ms.ToArray());
            }
        }

        public NetMessageEntityCreate ToNetMessageEntityCreate()
        {
            return NetMessageEntityCreate.CreateBuilder()
                .SetBaseData(BaseData.Serialize())
                .SetArchiveData(Serialize())
                .Build();
        }

        protected virtual void BuildString(StringBuilder sb)
        {
            sb.AppendLine($"ReplicationPolicy: {ReplicationPolicy}");
            sb.AppendLine($"PropertyCollection: {PropertyCollection}");
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            BuildString(sb);
            return sb.ToString();
        }

        public virtual void Destroy()
        {
            throw new NotImplementedException();
        }

        public virtual bool IsDormant() => Flags.HasFlag(EntityFlags.Dormant);
        public bool IsDead() => Flags.HasFlag(EntityFlags.IsDead);
        public bool HasMovementPreventionStatus() => Flags.HasFlag(EntityFlags.HasMovementPreventionStatus);
        public bool IsControlledEntity() => Flags.HasFlag(EntityFlags.AIMasterAvatar);
        public bool IsConfused() => Flags.HasFlag(EntityFlags.Confused);
        public bool IsMesmerized() => Flags.HasFlag(EntityFlags.Mesmerized);
        public bool IsMissionCrossEncounterHostilityOk() => Flags.HasFlag(EntityFlags.MissionXEncounterHostilityOk);
        public bool IgnoreMissionOwnerForTargeting() => Flags.HasFlag(EntityFlags.IgnoreMissionOwnerForTargeting);
        public bool IsSimulated() => Flags.HasFlag(EntityFlags.IsSimulated);
        public bool IsUntargetable() => Flags.HasFlag(EntityFlags.Untargetable);
        public bool IsUnaffectable() => Flags.HasFlag(EntityFlags.Unaffectable) || Flags.HasFlag(EntityFlags.TutorialInvulnerable);
        public bool IsNeverAffectedByPowers() => Flags.HasFlag(EntityFlags.IsNeverAffectedByPowers);
        public bool HasAITargetableOverride() => Flags.HasFlag(EntityFlags.AITargetableOverride);
        public bool HasAIControlPowerLock() => Flags.HasFlag(EntityFlags.AIControlPowerLock);
        public bool IsInKnockback() => Flags.HasFlag(EntityFlags.Knockback);
        public bool IsInKnockdown() => Flags.HasFlag(EntityFlags.Knockdown);
        public bool IsInKnockup() => Flags.HasFlag(EntityFlags.Knockup);
        public bool IsImmobilized() => Flags.HasFlag(EntityFlags.Immobilized) || Flags.HasFlag(EntityFlags.ImmobilizedParam);
        public bool IsImmobilizedByHitReact() => Flags.HasFlag(EntityFlags.ImmobilizedByHitReact);
        public bool IsSystemImmobilized() => Flags.HasFlag(EntityFlags.SystemImmobilized) || Flags.HasFlag(EntityFlags.TutorialImmobilized);
        public bool IsStunned() => Flags.HasFlag(EntityFlags.Stunned) || Flags.HasFlag(EntityFlags.StunnedByHitReact);
        public bool NPCAmbientLock() => Flags.HasFlag(EntityFlags.NPCAmbientLock);
        public bool IsInPowerLock() => Flags.HasFlag(EntityFlags.PowerLock);
        public bool NoCollide() => Flags.HasFlag(EntityFlags.NoCollide);
        public bool HasNoCollideException() => Flags.HasFlag(EntityFlags.HasNoCollideException);
        public bool IsIntangible() => Flags.HasFlag(EntityFlags.Intangible);
        public bool HasPowerUserOverride() => Flags.HasFlag(EntityFlags.PowerUserOverrideID);
        public bool IsMissilePlayerOwned() => Flags.HasFlag(EntityFlags.MissileOwnedByPlayer);
        public bool HasMissionPrototype() => Flags.HasFlag(EntityFlags.HasMissionPrototype);
        public bool IsAttachedToEntity() => Flags.HasFlag(EntityFlags.AttachedToEntityId);
        public bool IsHotspot() => Flags.HasFlag(EntityFlags.IsHotspot);
        public bool IsCollidableHotspot() => Flags.HasFlag(EntityFlags.IsCollidableHotspot);
        public bool IsReflectingHotspot() => Flags.HasFlag(EntityFlags.IsReflectingHotspot);
        public bool HasPowerImmunity() => Flags.HasFlag(EntityFlags.ImmuneToPower);
        public bool HasClusterPrototype() => Flags.HasFlag(EntityFlags.ClusterPrototype);
        public bool HasEncounterResourcePrototype() => Flags.HasFlag(EntityFlags.EncounterResource);
        public bool IgnoreNavi() => Flags.HasFlag(EntityFlags.IgnoreNavi);        
        public bool IsInTutorialPowerLock() => Flags.HasFlag(EntityFlags.TutorialPowerLock);
        
    }

    [Flags]
    public enum EntityFlags : ulong
    {
        Dormant = 1ul << 0,
        IsDead = 1ul << 1,
        HasMovementPreventionStatus = 1ul << 2,
        AIMasterAvatar = 1ul << 3,
        Confused = 1ul << 4,
        Mesmerized = 1ul << 5,
        MissionXEncounterHostilityOk = 1ul << 6,
        IgnoreMissionOwnerForTargeting = 1ul << 7,
        IsSimulated = 1ul << 8,
        Untargetable = 1ul << 9,
        Unaffectable = 1ul << 10,
        IsNeverAffectedByPowers = 1ul << 11,
        AITargetableOverride = 1ul << 12,
        AIControlPowerLock = 1ul << 13,
        Knockback = 1ul << 14,
        Knockdown = 1ul << 15,
        Knockup = 1ul << 16,
        Immobilized = 1ul << 17,
        ImmobilizedParam = 1ul << 18,
        ImmobilizedByHitReact = 1ul << 19,
        SystemImmobilized = 1ul << 20,
        Stunned = 1ul << 21,
        StunnedByHitReact = 1ul << 22,
        NPCAmbientLock = 1ul << 23,
        PowerLock = 1ul << 24,
        NoCollide = 1ul << 25,
        HasNoCollideException = 1ul << 26,
        Intangible = 1ul << 27,
        PowerUserOverrideID = 1ul << 28,
        MissileOwnedByPlayer = 1ul << 29,
        HasMissionPrototype = 1ul << 30,
        Flag31 = 1ul << 31,
        Flag32 = 1ul << 32,
        Flag33 = 1ul << 33,
        AttachedToEntityId = 1ul << 34,
        IsHotspot = 1ul << 35,
        IsCollidableHotspot = 1ul << 36,
        IsReflectingHotspot = 1ul << 37,
        ImmuneToPower = 1ul << 38,
        ClusterPrototype = 1ul << 39,
        EncounterResource = 1ul << 40,
        IgnoreNavi = 1ul << 41,
        TutorialImmobilized = 1ul << 42,
        TutorialInvulnerable = 1ul << 43,
        TutorialPowerLock = 1ul << 44,
    }
}
