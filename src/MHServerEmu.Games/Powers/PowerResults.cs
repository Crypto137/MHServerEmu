using System.Runtime.InteropServices;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Powers.Conditions;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Powers
{
    public class PowerResults : PowerEffectsPacket
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private List<Condition> _conditionAddList;
        private List<ulong> _conditionRemoveList;

        private DamageForClient _damageForClient = new();

        public float HealingForClient { get; set; }
        public AssetId PowerAssetRefOverride { get; private set; }
        public PowerResultFlags Flags { get; set; }
        public ulong TransferToId { get; set; }

        public Vector3 TeleportPosition { get; set; }
        public Vector3 KnockbackSourcePosition { get; set; }

        public PowerActivationSettings ActivationSettings { get; set; }

        public IReadOnlyList<Condition> ConditionAddList { get => _conditionAddList != null ? _conditionAddList : Array.Empty<Condition>(); }
        public IReadOnlyList<ulong> ConditionRemoveList { get => _conditionRemoveList != null ? _conditionRemoveList : Array.Empty<ulong>(); }

        public bool IsBlocked { get => Flags.HasFlag(PowerResultFlags.Blocked); }
        public bool IsDodged { get => Flags.HasFlag(PowerResultFlags.Dodged); }
        public bool IsAvoided { get => Flags.HasFlag(PowerResultFlags.Dodged) || Flags.HasFlag(PowerResultFlags.Unaffected); }

        public void Init(ulong powerOwnerId, ulong ultimateOwnerId, ulong targetId, Vector3 powerOwnerPosition,
            PowerPrototype powerProto, AssetId powerAssetRefOverride, bool isHostile)
        {
            PowerOwnerId = powerOwnerId;
            UltimateOwnerId = ultimateOwnerId;
            TargetId = targetId;
            PowerOwnerPosition = powerOwnerPosition;
            PowerPrototype = powerProto;
            PowerAssetRefOverride = powerAssetRefOverride;

            SetFlag(PowerResultFlags.Hostile, isHostile);
        }

        public override void Clear()
        {
            base.Clear();

            _damageForClient.Clear();

            ClearConditionInstances();
            _conditionRemoveList?.Clear();

            TeleportPosition = default;
            KnockbackSourcePosition = default;

            ActivationSettings = default;

            HealingForClient = default;
            PowerAssetRefOverride = default;
            Flags = default;
            TransferToId = default;
        }

        public void ClearConditionInstances()
        {
            if (_conditionAddList == null)
                return;

            foreach (Condition condition in _conditionAddList)
            {
                if (condition.IsInPool == false && condition.IsInCollection == false)
                    ConditionCollection.DeleteCondition(condition);
            }

            _conditionAddList.Clear();
        }

        public bool HasDamageForClient()
        {
            foreach (float damage in _damageForClient)
            {
                if (damage > 0f)
                    return true;
            }

            return false;
        }

        public float GetDamageForClient(DamageType damageType)
        {
            if (damageType < DamageType.Physical || damageType >= DamageType.NumDamageTypes)
                return 0f;

            return _damageForClient[(int)damageType];
        }

        public float GetTotalDamageForClient()
        {
            float totalDamage = 0f;
            foreach (float damage in _damageForClient)
                totalDamage += damage;
            return totalDamage;
        }

        public void SetDamageForClient(DamageType damageType, float value)
        {
            if (damageType < DamageType.Physical || damageType >= DamageType.NumDamageTypes)
                return;

            _damageForClient[(int)damageType] = value;
        }

        public bool TestFlag(PowerResultFlags flag)
        {
            return (Flags & flag) != PowerResultFlags.None;
        }

        public void SetFlag(PowerResultFlags flag, bool value)
        {
            if (value)
                Flags |= flag;
            else
                Flags &= ~flag;
        }

        public bool AddConditionToAdd(Condition condition)
        {
            if (condition == null) return Logger.WarnReturn(false, "AddConditionToAdd(): condition == null");
            _conditionAddList ??= new();
            _conditionAddList.Add(condition);
            return true;
        }

        public bool AddConditionToRemove(ulong conditionId)
        {
            _conditionRemoveList ??= new();
            _conditionRemoveList.Add(conditionId);
            return true;
        }

        public bool HasMeaningfulResults()
        {
            foreach (var kvp in Properties)
            {
                // Skip meaningless properties
                switch (kvp.Key.Enum)
                {
                    case PropertyEnum.CreatorEntityAssetRefBase:
                    case PropertyEnum.CreatorEntityAssetRefCurrent:
                    case PropertyEnum.NoExpOnDeath:
                    case PropertyEnum.NoLootDrop:
                    case PropertyEnum.ProcRecursionDepth:
                    case PropertyEnum.SetTargetLifespanMS:
                        continue;
                }

                return true;
            }

            if (_conditionAddList?.Count > 0 || _conditionRemoveList?.Count > 0)
                return true;

            if ((Flags & PowerResultFlags.HasResultsFlags) != 0)
                return true;

            return false;
        }

        public bool ShouldSendToClient()
        {
            if (GetTotalDamageForClient() > 0f || HealingForClient > 0f)
                return true;

            if (HasVisuals())
                return true;

            if ((Flags & PowerResultFlags.SendToClientFlags) != 0)
                return true;

            return false;
        }

        public bool HasVisuals()
        {
            if (PowerAssetRefOverride != AssetId.Invalid)
                return true;

            PowerPrototype powerProto = PowerPrototype;
            if (powerProto == null)
                return false;

            return powerProto.HUDMessage != PrototypeId.Invalid || powerProto.PowerUnrealClass != AssetId.Invalid;
        }

        public bool IsAtMaxRecursionDepth()
        {
            const int MaxRecursionDepth = 3;

            PowerPrototype powerProto = PowerPrototype;
            if (powerProto == null)
                return false;

            return powerProto.PowerCategory == PowerCategoryType.ProcEffect && Properties[PropertyEnum.ProcRecursionDepth] >= MaxRecursionDepth;
        }

        /// <summary>
        /// Specialized struct to avoid allocating a <see cref="float"/> array for each <see cref="PowerResults"/> instance.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct DamageForClient
        {
            [FieldOffset(0)]
            public float Physical;
            [FieldOffset(4)]
            public float Energy;
            [FieldOffset(8)]
            public float Mental;

            public float this[int index] { get => AsSpan()[index]; set => AsSpan()[index] = value; }

            public Span<float> AsSpan()
            {
                return MemoryMarshal.CreateSpan(ref Physical, (int)DamageType.NumDamageTypes);
            }

            public void Clear()
            {
                AsSpan().Clear();
            }

            public Span<float>.Enumerator GetEnumerator()
            {
                return AsSpan().GetEnumerator();
            }
        }
    }
}
