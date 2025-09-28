﻿using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)None)]
    public enum AffixPosition
    {
        None,
        Prefix,
        Suffix,
        Visual,
        Ultimate,
        Cosmic,
        Unique,
        Blessing,
        Runeword,
        TeamUp,
        Metadata,
        PetTech1,
        PetTech2,
        PetTech3,
        PetTech4,
        PetTech5,
        RegionAffix,
        Socket1,
        Socket2,
        Socket3,
        NumPositions
    }

    [AssetEnum((int)Fail)]
    public enum DuplicateHandlingBehavior
    {
        Fail,
        Ignore,
        Overwrite,
        Append,
    }

    [AssetEnum]
    public enum OmegaPageType
    {
        NeuralEnhancement = 0,
        CosmicEntities = 1,
        ArcaneAttunement = 2,
        InterstellarExploration = 3,
        SpecialWeapons = 4,
        ExtraDimensionalTravel = 5,
        Xenobiology = 6,
        RadioactiveOrigins = 7,
        TemporalManipulation = 8,
        Nanotechnology = 9,
        SupernaturalInvestigation = 10,
        HumanAugmentation = 11,
        Psionics = 12,
        MolecularAdjustment = 13,
    }

    [AssetEnum((int)None)]
    public enum InfinityGem
    {
        Mind = 0,
        Power = 1,
        Reality = 2,
        Soul = 3,
        Space = 4,
        Time = 5,
        NumGems = 6,
        None = 7,
    }

    [AssetEnum((int)Popcorn)]
    public enum Rank
    {
        Popcorn,
        Champion,
        Elite,
        MiniBoss,
        Boss,
        Player,
        GroupBoss,
        TeamUp,
        Max
    }

    [AssetEnum((int)Default)]
    public enum HealthBarType
    {
        Default = 0,
        EliteMinion = 1,
        MiniBoss = 2,
        Boss = 3,
        None = 4,
    }

    [AssetEnum((int)Default)]
    public enum OverheadInfoDisplayType
    {
        Default = 0,
        Always = 1,
        Never = 2,
    }

    #endregion

    public class AffixPrototype : Prototype
    {
        public AffixPosition Position { get; protected set; }
        public PrototypePropertyCollection Properties { get; protected set; }
        public LocaleStringId DisplayNameText { get; protected set; }
        public int Weight { get; protected set; }
        public PrototypeId[] TypeFilters { get; protected set; }
        public PropertyPickInRangeEntryPrototype[] PropertyEntries { get; protected set; }
        public AssetId[] Keywords { get; protected set; }
        public DropRestrictionPrototype[] DropRestrictions { get; protected set; }
        public DuplicateHandlingBehavior DuplicateHandlingBehavior { get; protected set; }

        //---

        [DoNotCopy]
        public virtual bool HasBonusPropertiesToApply { get => Properties != null || PropertyEntries != null; }

        [DoNotCopy]
        public bool IsPetTechAffix { get => Position >= AffixPosition.PetTech1 && Position <= AffixPosition.PetTech5; }

        [DoNotCopy]
        public bool IsGemAffix { get => Position >= AffixPosition.Socket1 && Position <= AffixPosition.Socket3; }

        public override void PostProcess()
        {
            base.PostProcess();

            // V48_TODO: Check if there should be anything here
        }

        public virtual bool AllowAttachment(DropFilterArguments args)
        {
            if (DropRestrictions.IsNullOrEmpty())
                return true;

            foreach (DropRestrictionPrototype dropRestrictionProto in DropRestrictions)
            {
                if (dropRestrictionProto.Allow(args) == false)
                    return false;
            }

            return true;
        }

        public bool HasKeywords(AssetId[] keywordsToCheck, bool hasAll = false)
        {
            if (keywordsToCheck.IsNullOrEmpty())
                return true;

            if (Keywords.IsNullOrEmpty())
                return false;

            foreach (AssetId keywordAssetRefToCheck in keywordsToCheck)
            {
                bool found = false;

                foreach (AssetId keywordAssetRef in Keywords)
                {
                    if (keywordAssetRef == keywordAssetRefToCheck)
                    {
                        found = true;
                        break;
                    }
                }

                if (found != hasAll)
                    return found;
            }

            return hasAll;
        }
    }

    public class AffixPowerModifierPrototype : AffixPrototype
    {
        public bool IsForSinglePowerOnly { get; protected set; }
        public EvalPrototype PowerBoostMax { get; protected set; }
        public EvalPrototype PowerGrantRankMax { get; protected set; }
        public PrototypeId PowerKeywordFilter { get; protected set; }
        public EvalPrototype PowerUnlockLevelMax { get; protected set; }
        public EvalPrototype PowerUnlockLevelMin { get; protected set; }
        public EvalPrototype PowerBoostMin { get; protected set; }
        public EvalPrototype PowerGrantRankMin { get; protected set; }
        public PrototypeId PowerProgTableTabRef { get; protected set; }

        //---

        [DoNotCopy]
        public override bool HasBonusPropertiesToApply { get => base.HasBonusPropertiesToApply || PowerBoostMax != null || PowerGrantRankMax != null; }
    }

    public class AffixRegionModifierPrototype : AffixPrototype
    {
        public PrototypeId AffixTable { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        [DoNotCopy]
        public override bool HasBonusPropertiesToApply { get => true; }

        public override bool AllowAttachment(DropFilterArguments args)
        {
            if (base.AllowAttachment(args) == false)
                return false;

            if (args.ItemProto == null)
                return Logger.WarnReturn(false, "AllowAttachment(): args.ItemProto == null");

            if (args.ItemProto is not ItemPrototype itemProto)
                return false;

            PrototypeId portalTargetProtoRef = itemProto.GetPortalTarget();
            RegionPrototype portalTargetProto = portalTargetProtoRef.As<RegionPrototype>();
            return portalTargetProto?.AffixTable == AffixTable;
        }
    }

    public class AffixRegionRestrictedPrototype : AffixPrototype
    {
        public PrototypeId RequiredRegion { get; protected set; }
        public PrototypeId[] RequiredRegionKeywords { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public bool MatchesRegion(Region region)
        {
            if (RequiredRegion != PrototypeId.Invalid && region.PrototypeDataRef == RequiredRegion)
                return true;

            if (RequiredRegionKeywords.HasValue() && region.HasKeywords())
            {
                // Seems to be deprecated in 1.52, but may be useful for older versions
                foreach (PrototypeId keywordProtoRef in RequiredRegionKeywords)
                {
                    if (region.HasKeyword(keywordProtoRef.As<KeywordPrototype>()))
                        return true;
                }
            }

            return false;
        }
    }

    public class AffixTeamUpPrototype : AffixPrototype
    {
        public bool IsAppliedToOwnerAvatar { get; protected set; }
    }

    public class AffixRunewordPrototype : AffixPrototype
    {
        public PrototypeId Runeword { get; protected set; }
    }

    public class RunewordDefinitionEntryPrototype : Prototype
    {
        public PrototypeId Rune { get; protected set; }
    }

    public class RunewordDefinitionPrototype : Prototype
    {
        public RunewordDefinitionEntryPrototype[] Runes { get; protected set; }
    }

    public class AffixEntryPrototype : Prototype
    {
        public PrototypeId Affix { get; protected set; }
        public PrototypeId Power { get; protected set; }
        public PrototypeId Avatar { get; protected set; }

        //---

        [DoNotCopy]
        public virtual int LevelRequirement { get => 0; }
    }

    public class LeveledAffixEntryPrototype : AffixEntryPrototype
    {
        public int LevelRequired { get; protected set; }
        public LocaleStringId LockedDescriptionText { get; protected set; }

        //--

        [DoNotCopy]
        public override int LevelRequirement { get => LevelRequired; }
    }

    public class AffixDisplaySlotPrototype : Prototype
    {
        public AssetId[] AffixKeywords { get; protected set; }
        public LocaleStringId DisplayText { get; protected set; }
    }

    public class ModPrototype : Prototype
    {
        public LocaleStringId TooltipTitle { get; protected set; }
        public AssetId UIIcon { get; protected set; }
        public LocaleStringId TooltipDescription { get; protected set; }
        public PrototypePropertyCollection Properties { get; protected set; }     // Property list, should this be a property collection?
        public PrototypeId[] PassivePowers { get; protected set; }
        public PrototypeId Type { get; protected set; }
        public int RanksMax { get; protected set; }
        public CurveId RankCostCurve { get; protected set; }
        public ModDisableByBasePrototype[] DisableBy { get; protected set; }    // V48
        public PrototypeId TooltipTemplateCurrentRank { get; protected set; }
        public EvalPrototype[] EvalOnCreate { get; protected set; }
        public PrototypeId TooltipTemplateNextRank { get; protected set; }
        public PropertySetEntryPrototype[] PropertiesForTooltips { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public int GetRanksMax()
        {
            Curve curve = RankCostCurve.AsCurve();

            if (RanksMax > 0 && curve != null)
                return Math.Min(curve.MaxPosition, RanksMax);

            if (RanksMax > 0)
                return RanksMax;

            if (curve != null)
                return curve.MaxPosition;

            return 0;
        }

        public void RunEvalOnCreate(Entity entity, PropertyCollection indexProperties, PropertyCollection modProperties)
        {
            if (EvalOnCreate.HasValue())
            {
                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                evalContext.SetVar_PropertyCollectionPtr(EvalContext.Default, modProperties);
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, entity.Properties);
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Var1, indexProperties);

                foreach (EvalPrototype evalProto in EvalOnCreate)
                {
                    bool curEvalSucceeded = Eval.RunBool(evalProto, evalContext);
                    if (curEvalSucceeded == false)
                        Logger.Warn($"RunEvalOnCreate(): The following EvalOnCreate Eval in a mod failed:\nEval: [{evalProto.ExpressionString}]\n Mod: [{this}]");
                }
            }

            if (PropertiesForTooltips.HasValue())
            {
                foreach (PropertySetEntryPrototype propEntryProto in PropertiesForTooltips)
                {
                    if (propEntryProto.Value == null)
                    {
                        Logger.Warn("RunEvalOnCreate(): propEntryProto.Value == null");
                        continue;
                    }

                    using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                    evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, entity.Properties);
                    evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Var1, indexProperties);

                    PropertyInfo propertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propEntryProto.Prop.Enum);

                    switch (propertyInfo.DataType)
                    {
                        case PropertyDataType.Boolean:
                            Properties[propEntryProto.Prop] = Eval.RunBool(propEntryProto.Value, evalContext);
                            break;

                        case PropertyDataType.Real:
                            Properties[propEntryProto.Prop] = Eval.RunFloat(propEntryProto.Value, evalContext);
                            break;

                        case PropertyDataType.Integer:
                            Properties[propEntryProto.Prop] = Eval.RunInt(propEntryProto.Value, evalContext);
                            break;

                        default:
                            Logger.Warn("The following Mod has a built-in PropertySetEntry with a property that is not an int/float/bool prop, which doesn't work!\n" +
                                $"Mod: [{this}]\nProperty: [{propertyInfo.PropertyName}]");
                            break;
                    }
                }
            }
        }
    }

    public class ModTypePrototype : Prototype
    {
        public PrototypeId AggregateProperty { get; protected set; }
        public PrototypeId TempProperty { get; protected set; }
        public PrototypeId BaseProperty { get; protected set; }
        public PrototypeId CurrencyIndexProperty { get; protected set; }
        public CurveId CurrencyCurve { get; protected set; }
        public bool UseCurrencyIndexAsValue { get; protected set; }
    }

    public class ModGlobalsPrototype : Prototype
    {
        public PrototypeId RankModType { get; protected set; }
        public PrototypeId SkillModType { get; protected set; }
        public PrototypeId EnemyBoostModType { get; protected set; }
        public PrototypeId PvPUpgradeModType { get; protected set; }
        public PrototypeId TalentModType { get; protected set; }
        public PrototypeId OmegaBonusModType { get; protected set; }
        public PrototypeId OmegaHowToTooltipTemplate { get; protected set; }
    }

    public class SkillPrototype : ModPrototype
    {
        public CurveId DamageBonusByRank { get; protected set; }
    }

    public class TalentSetPrototype : ModPrototype
    {
        public LocaleStringId UITitle { get; protected set; }
        public PrototypeId[] Talents { get; protected set; }
    }

    public class TalentPrototype : ModPrototype
    {
    }

    public class OmegaBonusPrototype : ModPrototype
    {
        public PrototypeId[] Prerequisites { get; protected set; }
        public int UIHexIndex { get; protected set; }
    }

    public class OmegaBonusSetPrototype : ModPrototype
    {
        public LocaleStringId UITitle { get; protected set; }
        public PrototypeId[] OmegaBonuses { get; protected set; }
        public OmegaPageType UIPageType { get; protected set; }
        public bool Unlocked { get; protected set; }
        public AssetId UIColor { get; protected set; }
        public AssetId UIBackgroundImage { get; protected set; }
    }

    public class RankPrototype : ModPrototype
    {
        public Rank Rank { get; protected set; }
        public HealthBarType HealthBarType { get; protected set; }
        public LootRollModifierPrototype[] LootModifiers { get; protected set; }
        public LootDropEventType LootTableParam { get; protected set; }
        public OverheadInfoDisplayType OverheadInfoDisplayType { get; protected set; }
        public PrototypeId[] Keywords { get; protected set; }

        //---

        [DoNotCopy]
        public bool IsRankBoss { get => Rank == Rank.Boss || Rank == Rank.GroupBoss; }

        [DoNotCopy]
        public bool IsRankChampionOrEliteOrMiniBoss { get => Rank == Rank.Champion || Rank == Rank.Elite || Rank == Rank.MiniBoss; }

        [DoNotCopy]
        public bool IsRankBossOrMiniBoss { get => IsRankBoss || Rank == Rank.MiniBoss; }

        private KeywordsMask _keywordsMask;

        public static PrototypeId DoOverride(PrototypeId rankRef, PrototypeId rankOverride)
        {
            var rankProto = rankRef.As<RankPrototype>();
            var rankOverrideProto = rankOverride.As<RankPrototype>();
            return DoOverride(rankProto, rankOverrideProto).DataRef;
        }

        public static RankPrototype DoOverride(RankPrototype rankProto, RankPrototype rankOverrideProto)
        {
            if (rankProto == null) return rankOverrideProto;
            if (rankOverrideProto == null) return rankProto;
            if (rankProto.Rank < rankOverrideProto.Rank) return rankOverrideProto;
            return rankProto;
        }

        public override void PostProcess()
        {
            base.PostProcess();
            _keywordsMask = KeywordPrototype.GetBitMaskForKeywordList(Keywords);
        }

        public bool HasKeyword(KeywordPrototype keywordProto)
        {
            return keywordProto != null && KeywordPrototype.TestKeywordBit(_keywordsMask, keywordProto);
        }
    }

    public class EnemyBoostSetPrototype : Prototype
    {
        public PrototypeId[] Modifiers { get; protected set; }

        public bool Contains(PrototypeId affixRef)
        {
            return Modifiers.HasValue() ? Modifiers.Contains(affixRef) : false;
        }
    }

    public class EnemyBoostPrototype : ModPrototype
    {
        public PrototypeId ActivePower { get; protected set; }
        public bool ShowVisualFX { get; protected set; }
        public bool DisableForControlledAgents { get; protected set; }
        public bool CountsAsAffixSlot { get; protected set; }
    }

    public class DifficultyRankEntryPrototype : Prototype
    {
        public RankAffixEntryPrototype[] Affixes { get; protected set; }
        public PrototypeId Rank { get; protected set; }
        public int Weight { get; protected set; }

        public RankAffixEntryPrototype GetAffixSlot(int slot)
        {
            if (Affixes.HasValue() && slot >= 0 && slot < Affixes.Length)
                return Affixes[slot];
            return null;
        }

        public int GetMaxAffixes()
        {
            return Affixes.HasValue() ? Affixes.Length : 0;
        }
    }

    public class RarityPrototype : Prototype
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public PrototypeId DowngradeTo { get; protected set; }
        public PrototypeId TextStyle { get; protected set; }
        public CurveId Weight { get; protected set; }
        public LocaleStringId DisplayNameText { get; protected set; }
        public int BroadcastToPartyLevelMax { get; protected set; }
        public AffixEntryPrototype[] AffixesBuiltIn { get; protected set; }
        public int ItemLevelBonus { get; protected set; }

        [DoNotCopy]
        public int Tier { get; private set; }

        public override void PostProcess()
        {
            base.PostProcess();

            Tier = 1;

            PrototypeId downgrade = DowngradeTo;
            while (downgrade != PrototypeId.Invalid)
            {
                RarityPrototype descendant = GameDatabase.GetPrototype<RarityPrototype>(downgrade);

                if (descendant == null)
                {
                    Logger.Warn("PostProcess(): descendant == null");
                    break;
                }

                downgrade = descendant.DowngradeTo;
                Tier++;
            }
        }

        public float GetWeight(int level)
        {
            Curve curve = CurveDirectory.Instance.GetCurve(Weight);
            if (curve == null) return Logger.WarnReturn(0f, "GetWeight(): curve == null");

            return curve.GetAt(Math.Clamp(level, curve.MinPosition, curve.MaxPosition));
        }
    }

    // V48

    public class ModDisableByBasePrototype : Prototype
    {
    }

    public class ModDisableByMissionRequirementPrototype : ModDisableByBasePrototype
    {
        public PrototypeId Mission { get; protected set; }
    }

    public class ModDisableByModSelectedPrototype : ModDisableByBasePrototype
    {
        public PrototypeId Mod { get; protected set; }
    }

    public class ModDisableByModTypeRequirementPrototype : ModDisableByBasePrototype
    {
        public PrototypeId ModType { get; protected set; }
        public int RanksMin { get; protected set; }
    }

    public class ModDisableBySetPointRequirementPrototype : ModDisableByBasePrototype
    {
        public PrototypeId TalentSet { get; protected set; }
        public int PointsRequired { get; protected set; }
    }

    public class ModDisableByUniqueRequirementPrototype : ModDisableByBasePrototype
    {
        public PrototypeId UniqueSet { get; protected set; }
    }

    public class ModUniqueSetPrototype : Prototype
    {
        public PrototypeId[] Set { get; protected set; }
    }
}
