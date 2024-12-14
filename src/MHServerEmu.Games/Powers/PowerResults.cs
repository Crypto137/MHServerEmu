using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Powers
{
    public class PowerResults : PowerEffectsPacket
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly float[] _damageForClient = new float[(int)DamageType.NumDamageTypes];

        private readonly List<Condition> _conditionAddList = new();
        private readonly List<ulong> _conditionRemoveList = new();

        public float HealingForClient { get; set; }
        public AssetId PowerAssetRefOverride { get; private set; }
        public PowerResultFlags Flags { get; set; }
        public ulong TransferToId { get; set; }

        public IReadOnlyList<Condition> ConditionAddList { get => _conditionAddList; }
        public IReadOnlyList<ulong> ConditionRemoveList { get => _conditionRemoveList; }

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

        public bool HasDamageForClient()
        {
            for (int i = 0; i < _damageForClient.Length; i++)
            {
                if (_damageForClient[i] > 0f)
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
            for (int i = 0; i < _damageForClient.Length; i++)
                totalDamage += _damageForClient[i];
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
            _conditionAddList.Add(condition);
            return true;
        }

        public bool AddConditionToRemove(ulong conditionId)
        {
            _conditionRemoveList.Add(conditionId);
            return true;
        }
    }
}
