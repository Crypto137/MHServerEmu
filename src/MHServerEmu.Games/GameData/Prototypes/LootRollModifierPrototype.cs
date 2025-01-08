using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.GameData.Prototypes
{
    /// <summary>
    /// Base class for prototypes that apply modifiers to <see cref="LootRollSettings"/>.
    /// </summary>
    public class LootRollModifierPrototype : Prototype
    {
        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public virtual void Apply(LootRollSettings settings)
        {
            Logger.Warn($"Apply(): Not implemented for modifier type {GetType().Name}");
        }

        public virtual bool IsValidForNode(LootNodePrototype node)
        {
            return true;
        }
    }

    public class LootRollClampLevelPrototype : LootRollModifierPrototype
    {
        public int LevelMin { get; protected set; }
        public int LevelMax { get; protected set; }

        //---

        public override void Apply(LootRollSettings settings)
        {
            if (LevelMin > 0 && settings.Level < LevelMin)
                settings.Level = LevelMin;
            else if (LevelMax > 0 && settings.Level > LevelMax)
                settings.Level = LevelMax;
        }
    }

    public class LootRollRequireLevelPrototype : LootRollModifierPrototype
    {
        public int LevelMin { get; protected set; }
        public int LevelMax { get; protected set; }

        //---

        public override void Apply(LootRollSettings settings)
        {
            if ((LevelMin > 0 && settings.LevelForRequirementCheck < LevelMin) || (LevelMax > 0 && settings.LevelForRequirementCheck > LevelMax))
                settings.DropChanceModifiers |= LootDropChanceModifiers.LevelRestricted;
        }
    }

    public class LootRollMarkSpecialPrototype : LootRollModifierPrototype
    {
        //---

        public override void Apply(LootRollSettings settings)
        {
            settings.DropChanceModifiers |= LootDropChanceModifiers.SpecialItemFind;
        }

        public override bool IsValidForNode(LootNodePrototype node)
        {
            return node is LootTablePrototype;
        }
    }

    public class LootRollUnmarkSpecialPrototype : LootRollModifierPrototype
    {
        //---

        public override void Apply(LootRollSettings settings)
        {
            settings.DropChanceModifiers &= ~LootDropChanceModifiers.SpecialItemFind;
        }

        public override bool IsValidForNode(LootNodePrototype node)
        {
            return node is LootTablePrototype;
        }
    }

    public class LootRollMarkRarePrototype : LootRollModifierPrototype
    {
        //---

        public override void Apply(LootRollSettings settings)
        {
            settings.DropChanceModifiers |= LootDropChanceModifiers.RareItemFind;
        }

        public override bool IsValidForNode(LootNodePrototype node)
        {
            return node is LootTablePrototype;
        }
    }

    public class LootRollUnmarkRarePrototype : LootRollModifierPrototype
    {
        //---

        public override void Apply(LootRollSettings settings)
        {
            settings.DropChanceModifiers &= ~LootDropChanceModifiers.RareItemFind;
        }

        public override bool IsValidForNode(LootNodePrototype node)
        {
            return node is LootTablePrototype;
        }
    }

    public class LootRollOffsetLevelPrototype : LootRollModifierPrototype
    {
        public int LevelOffset { get; protected set; }

        //---

        public override void Apply(LootRollSettings settings)
        {
            settings.Level += LevelOffset;
        }
    }

    public class LootRollOnceDailyPrototype : LootRollModifierPrototype
    {
        public bool PerAccount { get; protected set; }

        //---

        public override void Apply(LootRollSettings settings)
        {
            LootDropChanceModifiers modifiers = LootDropChanceModifiers.CooldownOncePerXHours;
            if (PerAccount) modifiers |= LootDropChanceModifiers.PerAccount;
            settings.DropChanceModifiers |= modifiers;
        }

        public override bool IsValidForNode(LootNodePrototype node)
        {
            return node is LootTablePrototype;
        }
    }

    public class LootRollCooldownOncePerRolloverPrototype : LootRollModifierPrototype
    {
        public bool PerAccount { get; protected set; }

        //---

        public override void Apply(LootRollSettings settings)
        {
            LootDropChanceModifiers modifiers = LootDropChanceModifiers.CooldownOncePerRollover;
            if (PerAccount) modifiers |= LootDropChanceModifiers.PerAccount;
            settings.DropChanceModifiers |= modifiers;
        }

        public override bool IsValidForNode(LootNodePrototype node)
        {
            return node is LootTablePrototype;
        }
    }

    public class LootRollCooldownByChannelPrototype : LootRollModifierPrototype
    {
        public bool PerAccount { get; protected set; }

        //---

        public override void Apply(LootRollSettings settings)
        {
            LootDropChanceModifiers modifiers = LootDropChanceModifiers.CooldownByChannel;
            if (PerAccount) modifiers |= LootDropChanceModifiers.PerAccount;
            settings.DropChanceModifiers |= modifiers;
        }

        public override bool IsValidForNode(LootNodePrototype node)
        {
            return node is LootTablePrototype;
        }
    }

    public class LootRollSetAvatarPrototype : LootRollModifierPrototype
    {
        public PrototypeId Avatar { get; protected set; }

        //---

        public override void Apply(LootRollSettings settings)
        {
            if (Avatar != PrototypeId.Invalid)
            {
                settings.UsableAvatar = Avatar.As<AvatarPrototype>();
                settings.ForceUsable = true;
            }
        }
    }

    public class LootRollSetItemLevelPrototype : LootRollModifierPrototype
    {
        public int Level { get; protected set; }

        //---

        public override void Apply(LootRollSettings settings)
        {
            settings.Level = Level;
            settings.UseLevelVerbatim = true;
        }
    }

    public class LootRollModifyAffixLimitsPrototype : LootRollModifierPrototype
    {
        public AffixPosition Position { get; protected set; }
        public short ModifyMinBy { get; protected set; }
        public short ModifyMaxBy { get; protected set; }
        public PrototypeId Category { get; protected set; }

        //---

        public override void Apply(LootRollSettings settings)
        {
            if (Position != AffixPosition.None)
            {
                settings.AffixLimitMinByPositionModifierDict.TryGetValue(Position, out short currentMinValue);
                settings.AffixLimitMinByPositionModifierDict[Position] = (short)(currentMinValue + ModifyMinBy);

                settings.AffixLimitMaxByPositionModifierDict.TryGetValue(Position, out short currentMaxValue);
                settings.AffixLimitMaxByPositionModifierDict[Position] = (short)(currentMaxValue + ModifyMaxBy);
            }
            else if (Category != PrototypeId.Invalid)
            {
                settings.AffixLimitByCategoryModifierDict.TryGetValue(Category, out short currentValue);
                settings.AffixLimitByCategoryModifierDict[Category] = (short)(currentValue + ModifyMinBy);
            }
        }
    }

    public class LootRollSetRarityPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Choices { get; protected set; }

        //---

        public override void Apply(LootRollSettings settings)
        {
            if (Choices.IsNullOrEmpty())
                return;

            settings.Rarities.Clear();
            foreach (PrototypeId rarityRef in Choices)
                settings.Rarities.Add(rarityRef);
        }
    }

    public class LootRollSetUsablePrototype : LootRollModifierPrototype
    {
        public float Usable { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Usable = Math.Clamp(Usable, 0f, 1f);
        }

        public override void Apply(LootRollSettings settings)
        {
            settings.UsablePercent = Usable;
        }
    }

    public class LootRollUseLevelVerbatimPrototype : LootRollModifierPrototype
    {
        public bool UseLevelVerbatim { get; protected set; }

        //---

        public override void Apply(LootRollSettings settings)
        {
            settings.UseLevelVerbatim = UseLevelVerbatim;
        }
    }

    public class LootRollRequireDifficultyTierPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Choices { get; protected set; }

        //---

        public override void Apply(LootRollSettings settings)
        {
            if (Choices.IsNullOrEmpty() || settings.DifficultyTier == PrototypeId.Invalid || Choices.Contains(settings.DifficultyTier))
                return;

            settings.DropChanceModifiers |= LootDropChanceModifiers.DifficultyTierRestricted;
        }

        public override bool IsValidForNode(LootNodePrototype node)
        {
            return node is LootTablePrototype;
        }
    }

    public class LootRollModifyDropByDifficultyTierPrototype : LootRollModifierPrototype
    {
        public CurveId ModifierCurve { get; protected set; }

        //---

        public override void Apply(LootRollSettings settings)
        {
            if (ModifierCurve == CurveId.Invalid || settings.DifficultyTier == PrototypeId.Invalid)
                return;

            Curve curve = CurveDirectory.Instance.GetCurve(ModifierCurve);
            if (curve == null) return;

            var difficultyTierProto = GameDatabase.GetPrototype<DifficultyTierPrototype>(settings.DifficultyTier);
            DifficultyTier difficultyTierAsset = difficultyTierProto != null ? difficultyTierProto.Tier : DifficultyTier.Green;

            float noDropModifier = curve.GetAt((int)difficultyTierAsset);
            if (Segment.EpsilonTest(noDropModifier, 1f) == false)
            {
                settings.NoDropModifier *= noDropModifier;
                settings.DropChanceModifiers |= LootDropChanceModifiers.DifficultyTierNoDropModified;
            }
        }

        public override bool IsValidForNode(LootNodePrototype node)
        {
            return node is LootTablePrototype;
        }
    }

    public class LootRollRequireConditionKeywordPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Choices { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        private KeywordsMask _conditionKeywordsMask;

        public override void PostProcess()
        {
            base.PostProcess();
            _conditionKeywordsMask = KeywordPrototype.GetBitMaskForKeywordList(Choices);
        }

        public override void Apply(LootRollSettings settings)
        {
            if (Choices.IsNullOrEmpty())
                return;

            if (_conditionKeywordsMask.TestAny(settings.AvatarConditionKeywords))
                return;

            settings.DropChanceModifiers |= LootDropChanceModifiers.ConditionRestricted;
        }

        public override bool IsValidForNode(LootNodePrototype node)
        {
            return node is LootTablePrototype;
        }
    }

    public class LootRollForbidConditionKeywordPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Choices { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        private KeywordsMask _conditionKeywordsMask;

        public override void PostProcess()
        {
            base.PostProcess();
            _conditionKeywordsMask = KeywordPrototype.GetBitMaskForKeywordList(Choices);
        }

        public override void Apply(LootRollSettings settings)
        {
            if (Choices.IsNullOrEmpty())
                return;

            if (_conditionKeywordsMask.TestAny(settings.AvatarConditionKeywords))
                settings.DropChanceModifiers |= LootDropChanceModifiers.ConditionRestricted;
        }

        public override bool IsValidForNode(LootNodePrototype node)
        {
            return node is LootTablePrototype;
        }
    }

    public class LootRollRequireDropperKeywordPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Choices { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        private KeywordsMask _conditionKeywordsMask;

        public override void PostProcess()
        {
            base.PostProcess();
            _conditionKeywordsMask = KeywordPrototype.GetBitMaskForKeywordList(Choices);
        }

        public override void Apply(LootRollSettings settings)
        {
            if (Choices.IsNullOrEmpty())
                return;

            if (_conditionKeywordsMask.TestAny(settings.SourceEntityKeywords))
                return;

            settings.DropChanceModifiers |= LootDropChanceModifiers.DropperRestricted;
        }

        public override bool IsValidForNode(LootNodePrototype node)
        {
            return node is LootTablePrototype;
        }
    }

    public class LootRollForbidDropperKeywordPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Choices { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        private KeywordsMask _conditionKeywordsMask;

        public override void PostProcess()
        {
            base.PostProcess();
            _conditionKeywordsMask = KeywordPrototype.GetBitMaskForKeywordList(Choices);
        }

        public override void Apply(LootRollSettings settings)
        {
            if (Choices.IsNullOrEmpty())
                return;

            if (_conditionKeywordsMask.TestAny(settings.SourceEntityKeywords))
                settings.DropChanceModifiers |= LootDropChanceModifiers.DropperRestricted;
        }

        public override bool IsValidForNode(LootNodePrototype node)
        {
            return node is LootTablePrototype;
        }
    }

    public class LootRollRequireRegionKeywordPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Choices { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        private KeywordsMask _conditionKeywordsMask;

        public override void PostProcess()
        {
            base.PostProcess();
            _conditionKeywordsMask = KeywordPrototype.GetBitMaskForKeywordList(Choices);
        }

        public override void Apply(LootRollSettings settings)
        {
            if (Choices.IsNullOrEmpty())
                return;

            if (_conditionKeywordsMask.TestAny(settings.RegionKeywords))
                return;

            settings.DropChanceModifiers |= LootDropChanceModifiers.RegionRestricted;
        }

        public override bool IsValidForNode(LootNodePrototype node)
        {
            return node is LootTablePrototype;
        }
    }

    public class LootRollForbidRegionKeywordPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Choices { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        private KeywordsMask _conditionKeywordsMask;

        public override void PostProcess()
        {
            base.PostProcess();
            _conditionKeywordsMask = KeywordPrototype.GetBitMaskForKeywordList(Choices);
        }

        public override void Apply(LootRollSettings settings)
        {
            if (Choices.IsNullOrEmpty())
                return;

            if (_conditionKeywordsMask.TestAny(settings.RegionKeywords))
                settings.DropChanceModifiers |= LootDropChanceModifiers.RegionRestricted;
        }

        public override bool IsValidForNode(LootNodePrototype node)
        {
            return node is LootTablePrototype;
        }
    }

    public class LootRollRequireRegionScenarioRarityPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Choices { get; protected set; }

        //---

        public override void Apply(LootRollSettings settings)
        {
            if (Choices.IsNullOrEmpty() || (settings.RegionScenarioRarity != PrototypeId.Invalid && Choices.Contains(settings.RegionScenarioRarity)))
                return;

            settings.DropChanceModifiers |= LootDropChanceModifiers.RegionRestricted;
        }

        public override bool IsValidForNode(LootNodePrototype node)
        {
            return node is LootTablePrototype;
        }
    }

    public class LootRollRequireKillCountPrototype : LootRollModifierPrototype
    {
        public int KillsRequired { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override void Apply(LootRollSettings settings)
        {
            if (KillsRequired <= 0)
                return;

            if ((settings.DropChanceModifiers &
                (LootDropChanceModifiers.KillCountRequirementMet | LootDropChanceModifiers.KillCountRestricted)) != LootDropChanceModifiers.None)
            {
                Logger.Warn($"Apply(): Kill count requirement already set! Multiple RequireKillCount nodes in a single loot table are not supported.");
                return;
            }

            if (settings.KillCount >= KillsRequired)
                settings.DropChanceModifiers |= LootDropChanceModifiers.KillCountRequirementMet;
            else
                settings.DropChanceModifiers |= LootDropChanceModifiers.KillCountRestricted;
        }

        public override bool IsValidForNode(LootNodePrototype node)
        {
            return node is LootTablePrototype;
        }
    }

    public class LootRollRequireWeekdayPrototype : LootRollModifierPrototype
    {
        public Weekday[] Choices { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override void Apply(LootRollSettings settings)
        {
            if (Choices.IsNullOrEmpty())
                return;

            if (settings.UsableWeekday == Weekday.All)
            {
                Logger.Warn("Apply(): Unable to find a valid weekday for loot roll!");
                return;
            }

            if (Choices.Contains(settings.UsableWeekday))
                return;

            settings.DropChanceModifiers |= LootDropChanceModifiers.WeekdayRestricted;
        }

        public override bool IsValidForNode(LootNodePrototype node)
        {
            return node is LootTablePrototype;
        }
    }

    public class LootRollIgnoreCooldownPrototype : LootRollModifierPrototype
    {
        //---

        public override void Apply(LootRollSettings settings)
        {
            settings.DropChanceModifiers |= LootDropChanceModifiers.IgnoreCooldown;
        }
    }

    public class LootRollIgnoreVendorXPCapPrototype : LootRollModifierPrototype
    {
        //---

        public override void Apply(LootRollSettings settings)
        {
            settings.DropChanceModifiers |= LootDropChanceModifiers.IgnoreCap;
        }
    }

    public class LootRollSetRegionAffixTablePrototype : LootRollModifierPrototype
    {
        public PrototypeId RegionAffixTable { get; protected set; }

        //---

        public override void Apply(LootRollSettings settings)
        {
            if (RegionAffixTable == PrototypeId.Invalid)
                return;

            settings.RegionAffixTable = RegionAffixTable;
        }
    }

    public class LootRollIncludeCurrencyBonusPrototype : LootRollModifierPrototype
    {
        //---

        public override void Apply(LootRollSettings settings)
        {
            settings.DropChanceModifiers |= LootDropChanceModifiers.IncludeCurrencyBonus;
        }

        public override bool IsValidForNode(LootNodePrototype node)
        {
            return node is LootDropPrototype || node is LootTablePrototype;
        }
    }

    public class LootRollMissionStateRequiredPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Missions { get; protected set; }
        public MissionState RequiredState { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override void Apply(LootRollSettings settings)
        {
            if (settings.Player == null)
            {
                Logger.Warn("Apply(): player == null");
                return;
            };

            if (Missions.IsNullOrEmpty())
                return;

            foreach (PrototypeId missionProtoRef in Missions)
            {
                Mission mission = MissionManager.FindMissionForPlayer(settings.Player, missionProtoRef);
                MissionState state = mission != null ? mission.State : MissionState.Invalid;

                if (state != RequiredState)
                {
                    settings.DropChanceModifiers |= LootDropChanceModifiers.MissionRestricted;
                    break;
                }
            }
        }
    }
}
