using Gazillion;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;

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

        public MutationResults RollAffix(GRandom random, PrototypeId rollFor, ItemSpec itemSpec,
            Picker<AffixPrototype> affixPicker, HashSet<ScopedAffixRef> affixSet)
        {
            Logger.Debug("RollAffix()");

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
                result |= SetScope(random, rollFor, itemSpec, affixSet, BehaviorOnPowerMatch.Behavior0);

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

        public MutationResults SetScope(GRandom random, PrototypeId rollFor, ItemSpec itemSpec,
            HashSet<ScopedAffixRef> affixSet, BehaviorOnPowerMatch behaviorOnPowerMatch)
        {
            if (AffixProto == null)
                return Logger.WarnReturn(MutationResults.Error, "SetScope(): AffixProto == null");

            if (AffixProto is AffixPowerModifierPrototype affixPowerModifierProto)
            {
                if (affixPowerModifierProto.IsForSinglePowerOnly)
                    return SetAffixScopePower(random, rollFor, itemSpec, affixSet, behaviorOnPowerMatch);

                return SetAffixPowerForPowerGroupBonus(affixPowerModifierProto, rollFor, affixSet);
            }
            else if (AffixProto is AffixRegionModifierPrototype affixRegionModifierProto)
            {
                return SetAffixScopeRegionAffix(random, itemSpec, affixSet);
            }

            return MutationResults.None;
        }

        private MutationResults SetAffixScopePower(GRandom random, PrototypeId rollFor, ItemSpec itemSpec,
            HashSet<ScopedAffixRef> affixSet, BehaviorOnPowerMatch behaviorOnPowerMatch)
        {
            Logger.Warn("SetAffixScopePower()");
            return MutationResults.None;
        }

        private MutationResults SetAffixPowerForPowerGroupBonus(AffixPowerModifierPrototype affixPowerModifierProto,
            PrototypeId rollFor, HashSet<ScopedAffixRef> affixSet)
        {
            Logger.Warn("SetAffixPowerForPowerGroupBonus()");

            if (affixPowerModifierProto.IsForSinglePowerOnly)
                return Logger.WarnReturn(MutationResults.Error, "SetAffixPowerForPowerGroupBonus(): affixPowerModifierProto.IsForSinglePowerOnly");

            if (affixPowerModifierProto.PowerKeywordFilter != PrototypeId.Invalid)
            {
                // Keyword scope (applies to powers that match the keyword filter)

                // These affixes don't use scope param, so we check for invalid scope
                if (affixSet.Contains(new(affixPowerModifierProto.DataRef, PrototypeId.Invalid)))
                    return MutationResults.Error | MutationResults.ErrorReasonPowerGroup;

                if (_scopeProtoRef != PrototypeId.Invalid)
                {
                    _scopeProtoRef = PrototypeId.Invalid;
                    return MutationResults.Changed;
                }
            }
            else if (affixPowerModifierProto.PowerProgTableTabRef != PrototypeId.Invalid)
            {
                // Power tab scope (applies to powers in the specified power tab)
                
                if (rollFor == PrototypeId.Invalid)
                {
                    return Logger.WarnReturn(MutationResults.Error, "SetAffixPowerForPowerGroupBonus(): Trying to SetAffixPower() for a power mod affix that grants a bonus " +
                        $"to power progression table page, but there is not rollFor avatar!\n[{affixPowerModifierProto}]");
                }

                var avatarProto = rollFor.As<AvatarPrototype>();
                if (avatarProto == null) return Logger.WarnReturn(MutationResults.Error, "SetAffixPowerForPowerGroupBonus(): avatarProto == null");

                var powerProgTableTabRefProto = affixPowerModifierProto.PowerProgTableTabRef.As<PowerProgTableTabRefPrototype>();
                if (powerProgTableTabRefProto == null) return Logger.WarnReturn(MutationResults.Error, "SetAffixPowerForPowerGroupBonus(): powerProgTableTabRefProto == null");

                if (avatarProto.GetPowerProgressionTableAtIndex(powerProgTableTabRefProto.PowerProgTableTabIndex) == null)
                {
                    if (avatarProto.ApprovedForUse())
                    {
                        Logger.Warn($"SetAffixPowerForPowerGroupBonus(): Trying to SetAffixPower() for a power mod affix that grants a bonus to " +
                            "power progression table page for a shipping avatar, but the avatar does not have a power progression table " +
                            $"at the specified index!\nAvatar: [{avatarProto}]\nAffix: [{affixPowerModifierProto}]\nIndex: [{powerProgTableTabRefProto.PowerProgTableTabIndex}]");
                    }

                    return MutationResults.Error | MutationResults.ErrorReasonPowerGroup;
                }

                // NOTE: The client checks for invalid scope rather than rollFor, which may be a bug
                if (affixSet.Contains(new(affixPowerModifierProto.DataRef, PrototypeId.Invalid)))
                    return MutationResults.Error | MutationResults.ErrorReasonPowerGroup;

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

        private MutationResults SetAffixScopeRegionAffix(GRandom random, ItemSpec itemSpec, HashSet<ScopedAffixRef> affixSet)
        {
            Logger.Warn("SetAffixScopeRegionAffix()");

            // TODO: Cache this?
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
