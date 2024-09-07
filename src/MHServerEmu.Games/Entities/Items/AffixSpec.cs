using Gazillion;
using MHServerEmu.Core.Collections;
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
            return MutationResults.None;
        }
    }
}
