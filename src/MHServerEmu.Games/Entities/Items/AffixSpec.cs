using Gazillion;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties.Evals;

namespace MHServerEmu.Games.Entities.Items
{
    public class AffixSpec : ISerialize
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private PrototypeId _scopeProtoRef;
        private int _seed;

        public AffixPrototype AffixProto { get; set; }
        public PrototypeId ScopeProtoRef { get => _scopeProtoRef; set => _scopeProtoRef = value; }
        public int Seed { get => _seed; set => _seed = value; }

        public bool IsValid { get => AffixProto != null && _seed != 0; }

        public AffixSpec() { }

        public AffixSpec(AffixPrototype affixProto, PrototypeId scopeProtoRef, int seed)
        {
            AffixProto = affixProto;
            _scopeProtoRef = scopeProtoRef;
            _seed = seed;
        }

        public AffixSpec(AffixSpec other)
        {
            AffixProto = other.AffixProto;
            _scopeProtoRef = other._scopeProtoRef;
            _seed = other._seed;
        }

        public AffixSpec(NetStructAffixSpec protobuf)
        {
            AffixProto = GameDatabase.GetPrototype<AffixPrototype>((PrototypeId)protobuf.AffixProtoRef);
            _scopeProtoRef = (PrototypeId)protobuf.ScopeProtoRef;
            _seed = protobuf.Seed;
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            PrototypeId affixProtoRef = AffixProto != null ? AffixProto.DataRef : PrototypeId.Invalid;
            success &= Serializer.Transfer(archive, ref affixProtoRef);

            if (archive.IsUnpacking)
                AffixProto = affixProtoRef.As<AffixPrototype>();

            success &= Serializer.Transfer(archive, ref _scopeProtoRef);
            success &= Serializer.Transfer(archive, ref _seed);
            return success;
        }

        public NetStructAffixSpec ToProtobuf()
        {
            return NetStructAffixSpec.CreateBuilder()
                .SetAffixProtoRef((ulong)AffixProto.DataRef)
                .SetScopeProtoRef((ulong)_scopeProtoRef)
                .SetSeed(_seed)
                .Build();
        }

        public override string ToString()
        {
            return $"AffixSpec [{AffixProto}] scope=[{_scopeProtoRef.GetName()}] seed=[{_seed}]";
        }

        /// <summary>
        /// Rolls this <see cref="AffixSpec"/> using the provided data.
        /// </summary>
        public MutationResults RollAffix(GRandom random, PrototypeId rollFor, ItemSpec itemSpec,
            Picker<AffixPrototype> affixPicker, HashSet<ScopedAffixRef> affixSet)
        {
            AffixPrototype prevAffixProto = AffixProto;
            PrototypeId prevScopeProtoRef = _scopeProtoRef;

            MutationResults result = MutationResults.None;

            while (affixPicker.PickRemove(out AffixPrototype pickedAffixProto))
            {
                if (pickedAffixProto == null)
                {
                    Logger.Warn("RollAffix(): affixProto == null");
                    continue;
                }

                AffixProto = pickedAffixProto;
                result |= SetScope(random, rollFor, itemSpec, affixSet, BehaviorOnPowerMatch.Ignore);

                if (result.HasFlag(MutationResults.Error) == false)
                {
                    // Skip duplicate affixes
                    if (affixSet.Contains(new(AffixProto.DataRef, _scopeProtoRef)))
                        continue;

                    // Roll seed for this affix
                    _seed = random.Next(1, int.MaxValue);

                    // Remember this affix / scope combo so we don't get it again
                    affixSet.Add(new(AffixProto.DataRef, _scopeProtoRef));

                    // Re-add affix to the pool if needed
                    if (pickedAffixProto.DuplicateHandlingBehavior == DuplicateHandlingBehavior.Append)
                        affixPicker.Add(pickedAffixProto, pickedAffixProto.Weight);

                    result |= MutationResults.AffixChange;
                    break;
                }
                else
                {
                    // Clean up if failed to set scope
                    AffixProto = prevAffixProto;
                    ScopeProtoRef = prevScopeProtoRef;
                }
            }

            // Final validation
            if (IsValid == false)
                result |= MutationResults.Error;

            return result;
        }

        /// <summary>
        /// Assigns a scope parameter for this <see cref="AffixSpec"/> if needed.
        /// </summary>
        public MutationResults SetScope(GRandom random, PrototypeId rollFor, ItemSpec itemSpec,
            HashSet<ScopedAffixRef> affixSet, BehaviorOnPowerMatch behaviorOnPowerMatch)
        {
            if (AffixProto == null)
                return Logger.WarnReturn(MutationResults.Error, "SetScope(): AffixProto == null");

            if (AffixProto is AffixPowerModifierPrototype powerAffixProto)
            {
                if (powerAffixProto.IsForSinglePowerOnly)
                    return SetAffixScopePower(random, rollFor, itemSpec, affixSet, behaviorOnPowerMatch);

                return SetAffixPowerForPowerGroupBonus(powerAffixProto, rollFor, affixSet);
            }
            else if (AffixProto is AffixRegionModifierPrototype regionAffixProto)
            {
                return SetAffixScopeRegionAffix(random, itemSpec, affixSet);
            }

            return MutationResults.None;
        }

        /// <summary>
        /// Assigns a scope parameter for affixes that affects a single power (e.g. +x rank for power).
        /// </summary>
        private MutationResults SetAffixScopePower(GRandom random, PrototypeId rollFor, ItemSpec itemSpec,
            HashSet<ScopedAffixRef> affixSet, BehaviorOnPowerMatch behaviorOnPowerMatch)
        {
            // Validate
            if (AffixProto == null)
                return Logger.WarnReturn(MutationResults.Error, "SetAffixScopePower(): AffixProto == null");

            if (AffixProto is not AffixPowerModifierPrototype powerAffixProto)
                return Logger.WarnReturn(MutationResults.Error, "SetAffixScopePower(): AffixProto is not AffixPowerModifierPrototype powerAffixProto");

            if (powerAffixProto.IsForSinglePowerOnly == false)
                return Logger.WarnReturn(MutationResults.Error, "SetAffixScopePower(): powerAffixProto.IsForSinglePowerOnly == false");

            var avatarProto = rollFor.As<AvatarPrototype>();
            if (avatarProto == null)
                return Logger.WarnReturn(MutationResults.Error, "SetAffixScopePower(): avatarProto == null");

            // Run evals
            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetVar_Int(EvalContext.Var1, itemSpec.ItemLevel);

            int powerUnlockLevelMin = Eval.RunInt(powerAffixProto.PowerUnlockLevelMin, evalContext);
            int powerUnlockLevelMax = Eval.RunInt(powerAffixProto.PowerUnlockLevelMax, evalContext);
            powerUnlockLevelMax = Math.Max(powerUnlockLevelMin, powerUnlockLevelMax);

            List<PowerProgressionEntryPrototype> powerProgEntries = ListPool<PowerProgressionEntryPrototype>.Instance.Get();
            if (avatarProto.GetPowersUnlockedAtLevel(powerProgEntries, powerUnlockLevelMax, true) == false)
            {
                ListPool<PowerProgressionEntryPrototype>.Instance.Return(powerProgEntries);
                return MutationResults.Error | MutationResults.ErrorReasonAffixScopePower;
            }

            // Build scope picker
            Picker<PrototypeId> scopePicker = new(random);

            foreach (PowerProgressionEntryPrototype entryProto in powerProgEntries)
            {
                // Entry validation
                if (entryProto.PowerAssignment == null)
                {
                    Logger.Warn("SetAffixScopePower(): entryProto.PowerAssignment == null");
                    continue;
                }

                if (entryProto.PowerAssignment.Ability == PrototypeId.Invalid)
                {
                    Logger.Warn("SetAffixScopePower(): entryProto.PowerAssignment.Ability == PrototypeId.Invalid");
                    continue;
                }

                var powerProto = entryProto.PowerAssignment.Ability.As<PowerPrototype>();
                if (powerProto == null)
                {
                    Logger.Warn("SetAffixScopePower(): powerProto == null");
                    continue;
                }

                // Skip irrelevant entries
                if (Power.IsUltimatePower(powerProto))
                    continue;

                if (Power.IsTalentPower(powerProto))
                    continue;

                if (Power.IsTravelPower(powerProto))
                    continue;

                if (entryProto.IsTrait)
                    continue;

                if (affixSet.Contains(new(powerAffixProto.DataRef, powerProto.DataRef)))
                    continue;

                if (powerAffixProto.PowerKeywordFilter != PrototypeId.Invalid)
                {
                    KeywordPrototype keywordProto = powerAffixProto.PowerKeywordFilter.As<KeywordPrototype>();

                    if (powerProto.HasKeyword(keywordProto) == false)
                        continue;
                }

                if (entryProto.Antirequisites.HasValue())
                {
                    if (powerAffixProto.PowerGrantRankMax != null)
                    {
                        EvalPrototype eval = powerAffixProto.PowerGrantRankMax;
                        LoadFloatPrototype loadFloatEval = eval as LoadFloatPrototype;
                        LoadIntPrototype loadIntEval = eval as LoadIntPrototype;

                        if ((loadFloatEval == null && loadIntEval == null) ||
                            (loadFloatEval != null && loadFloatEval.Value > 0f) ||
                            (loadIntEval != null && loadIntEval.Value > 0))
                        {
                            continue;
                        }
                    }
                }

                Curve maxRankCurve = entryProto.MaxRankForPowerAtCharacterLevel.AsCurve();
                if (maxRankCurve == null)
                {
                    Logger.Warn("SetAffixScopePower(): maxRankCurve == null");
                    continue;
                }

                if (maxRankCurve.GetIntAt(maxRankCurve.MaxPosition) <= 1)
                    continue;

                if (powerProto.DataRef == _scopeProtoRef)
                {
                    if (behaviorOnPowerMatch == BehaviorOnPowerMatch.Cancel)
                    {
                        ListPool<PowerProgressionEntryPrototype>.Instance.Return(powerProgEntries);
                        return MutationResults.None;
                    }
                    
                    if (behaviorOnPowerMatch == BehaviorOnPowerMatch.Skip)
                        continue;
                }

                scopePicker.Add(powerProto.DataRef, 1);
            }

            ListPool<PowerProgressionEntryPrototype>.Instance.Return(powerProgEntries);

            // Pick affix scope
            PrototypeId scopeProtoRefBefore = _scopeProtoRef;
            if (scopePicker.Empty() || scopePicker.Pick(out _scopeProtoRef) == false)
                return MutationResults.Error | MutationResults.ErrorReasonAffixScopePower;

            if (_scopeProtoRef == scopeProtoRefBefore)
                return MutationResults.None;

            return MutationResults.Changed;
        }

        /// <summary>
        /// Assigns a scope parameter for affixes that affect a power group (e.g. +x rank for area / tab1 powers).
        /// </summary>
        private MutationResults SetAffixPowerForPowerGroupBonus(AffixPowerModifierPrototype powerAffixProto,
            PrototypeId rollFor, HashSet<ScopedAffixRef> affixSet)
        {
            if (powerAffixProto.IsForSinglePowerOnly)
                return Logger.WarnReturn(MutationResults.Error, "SetAffixPowerForPowerGroupBonus(): affixPowerModifierProto.IsForSinglePowerOnly");

            if (powerAffixProto.PowerKeywordFilter != PrototypeId.Invalid)
            {
                // Keyword scope (applies to powers that match the keyword filter)

                // These affixes don't use scope param, so we check for invalid scope
                if (affixSet.Contains(new(powerAffixProto.DataRef, PrototypeId.Invalid)))
                    return MutationResults.Error | MutationResults.ErrorReasonAffixScopePowerGroup;

                if (_scopeProtoRef != PrototypeId.Invalid)
                {
                    _scopeProtoRef = PrototypeId.Invalid;
                    return MutationResults.Changed;
                }
            }
            else if (powerAffixProto.PowerProgTableTabRef != PrototypeId.Invalid)
            {
                // Power tab scope (applies to powers in the specified power tab)
                
                if (rollFor == PrototypeId.Invalid)
                {
                    return Logger.WarnReturn(MutationResults.Error, "SetAffixPowerForPowerGroupBonus(): Trying to SetAffixPower() for a power mod affix that grants a bonus " +
                        $"to power progression table page, but there is not rollFor avatar!\n[{powerAffixProto}]");
                }

                var avatarProto = rollFor.As<AvatarPrototype>();
                if (avatarProto == null) return Logger.WarnReturn(MutationResults.Error, "SetAffixPowerForPowerGroupBonus(): avatarProto == null");

                var powerProgTableTabRefProto = powerAffixProto.PowerProgTableTabRef.As<PowerProgTableTabRefPrototype>();
                if (powerProgTableTabRefProto == null) return Logger.WarnReturn(MutationResults.Error, "SetAffixPowerForPowerGroupBonus(): powerProgTableTabRefProto == null");

                if (avatarProto.GetPowerProgressionTableAtIndex(powerProgTableTabRefProto.PowerProgTableTabIndex) == null)
                {
                    if (avatarProto.ApprovedForUse())
                    {
                        Logger.Warn($"SetAffixPowerForPowerGroupBonus(): Trying to SetAffixPower() for a power mod affix that grants a bonus to " +
                            "power progression table page for a shipping avatar, but the avatar does not have a power progression table " +
                            $"at the specified index!\nAvatar: [{avatarProto}]\nAffix: [{powerAffixProto}]\nIndex: [{powerProgTableTabRefProto.PowerProgTableTabIndex}]");
                    }

                    return MutationResults.Error | MutationResults.ErrorReasonAffixScopePowerGroup;
                }

                // NOTE: The client checks for invalid scope rather than rollFor, which may be a bug
                if (affixSet.Contains(new(powerAffixProto.DataRef, PrototypeId.Invalid)))
                    return MutationResults.Error | MutationResults.ErrorReasonAffixScopePowerGroup;

                if (_scopeProtoRef != rollFor)
                {
                    _scopeProtoRef = rollFor;
                    return MutationResults.Changed;
                }
            }
            else
            {
                // Global scope (applies to all powers)
                if (_scopeProtoRef != PrototypeId.Invalid)
                {
                    _scopeProtoRef = PrototypeId.Invalid;
                    return MutationResults.Changed;
                }
            }

            return MutationResults.None;
        }

        /// <summary>
        /// Assigns a scope parameter for region affixes (e.g. affixes on Danger Room scenarios).
        /// </summary>
        private MutationResults SetAffixScopeRegionAffix(GRandom random, ItemSpec itemSpec, HashSet<ScopedAffixRef> affixSet)
        {
            // TODO: Reuse this dictionary?
            Dictionary<RegionAffixCategoryPrototype, int> regionAffixCategoryPickDict = new();
            foreach (PrototypeId regionAffixCategoryProtoRef in DataDirectory.Instance.IteratePrototypesInHierarchy<RegionAffixCategoryPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                var regionAffixCategoryProto = regionAffixCategoryProtoRef.As<RegionAffixCategoryPrototype>();
                regionAffixCategoryPickDict[regionAffixCategoryProto] = 0;
            }

            // Filter out scopes that are already in use or mutually exclusive with existing ones
            HashSet<PrototypeId> scopeFilter = new();
            foreach (ScopedAffixRef scopedAffixRef in affixSet)
            {
                if (AffixProto.DataRef != scopedAffixRef.AffixProtoRef)
                    continue;

                scopeFilter.Add(scopedAffixRef.ScopeProtoRef);

                RegionAffixPrototype regionAffixProto = scopedAffixRef.ScopeProtoRef.As<RegionAffixPrototype>();
                if (regionAffixProto == null)
                {
                    Logger.Warn("SetAffixScopeRegionAffix(): regionAffixProto == null");
                    continue;
                }

                if (regionAffixProto.RestrictsAffixes.HasValue())
                {
                    foreach (PrototypeId restrictedAffixRef in regionAffixProto.RestrictsAffixes)
                        scopeFilter.Add(restrictedAffixRef);
                }

                if (regionAffixProto.Category != PrototypeId.Invalid)
                {
                    var affixCategoryProto = regionAffixProto.Category.As<RegionAffixCategoryPrototype>();
                    regionAffixCategoryPickDict[affixCategoryProto]++;
                }
                else
                {
                    Logger.Warn("SetAffixScopeRegionAffix(): regionAffixProto.Category == PrototypeId.Invalid");
                }
            }

            // Pick categories to use
            List<PrototypeId> essentialCategoryList = new();
            List<PrototypeId> extraCategoryList = new();

            foreach (var kvp in regionAffixCategoryPickDict)
            {
                RegionAffixCategoryPrototype categoryProto = kvp.Key;
                int numPicks = kvp.Value;

                if (numPicks < categoryProto.MinPicks)
                    essentialCategoryList.Add(categoryProto.DataRef);
                else if (numPicks < categoryProto.MaxPicks || categoryProto.MaxPicks == 0)
                    extraCategoryList.Add(categoryProto.DataRef);
            }

            // Prioritize essential categories
            List<PrototypeId> categoryListForPicking = essentialCategoryList.Count != 0
                ? essentialCategoryList
                : extraCategoryList;

            // Validation
            if (AffixProto == null)
                return Logger.WarnReturn(MutationResults.Error, "SetAffixScopeRegionAffix(): AffixProto == null");

            if (AffixProto is not AffixRegionModifierPrototype affixRegionModifierProto)
                return Logger.WarnReturn(MutationResults.Error, "SetAffixScopeRegionAffix(): AffixProto is not AffixRegionModifierPrototype affixRegionModifierProto");

            if (affixRegionModifierProto.AffixTable == PrototypeId.Invalid)
                return Logger.WarnReturn(MutationResults.Error, "SetAffixScopeRegionAffix(): affixRegionModifierProto.AffixTable == PrototypeId.Invalid");

            var regionAffixTableProto = affixRegionModifierProto.AffixTable.As<RegionAffixTablePrototype>();
            if (regionAffixTableProto == null)
                return Logger.WarnReturn(MutationResults.Error, "SetAffixScopeRegionAffix(): regionAffixTableProto == null");

            if (regionAffixTableProto.RegionAffixes.IsNullOrEmpty())
                return Logger.WarnReturn(MutationResults.Error, "SetAffixScopeRegionAffix(): regionAffixTableProto.RegionAffixes.IsNullOrEmpty()");

            // Build affix scope picker
            Picker<PrototypeId> scopePicker = new(random);

            foreach (RegionAffixWeightedEntryPrototype entryProto in regionAffixTableProto.RegionAffixes)
            {
                if (entryProto.Affix == PrototypeId.Invalid)
                {
                    Logger.Warn("SetAffixScopeRegionAffix(): entryProto.Affix == PrototypeId.Invalid");
                    continue;
                }

                if (entryProto.Weight == 0)
                {
                    Logger.Warn("SetAffixScopeRegionAffix(): entryProto.Weight == 0");
                    continue;
                }

                if (scopeFilter.Contains(entryProto.Affix))
                    continue;

                var regionAffixProto = entryProto.Affix.As<RegionAffixPrototype>();

                if (categoryListForPicking.Contains(regionAffixProto.Category) == false)
                    continue;

                if (regionAffixProto.AffixRarityRestrictions != null && regionAffixProto.AffixRarityRestrictions.Contains(itemSpec.RarityProtoRef))
                    continue;

                scopePicker.Add(entryProto.Affix, entryProto.Weight);
            }

            // Pick affix scope
            PrototypeId scopeProtoRefBefore = _scopeProtoRef;
            if (scopePicker.Empty() || scopePicker.Pick(out _scopeProtoRef) == false)
                return MutationResults.Error;

            if (_scopeProtoRef == scopeProtoRefBefore)
                return MutationResults.None;

            return MutationResults.Changed;
        }
    }
}
