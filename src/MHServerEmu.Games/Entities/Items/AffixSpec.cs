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
            Picker<AffixPrototype> affixPicker, HashSet<(PrototypeId, PrototypeId)> affixSet)
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
                    if (affixSet.Contains((AffixProto.DataRef, _scopeProtoRef)))
                        continue;

                    // Roll seed for this affix
                    _seed = random.Next(1, int.MaxValue);

                    // Remember this affix / scope combo so we don't get it again
                    affixSet.Add((AffixProto.DataRef, _scopeProtoRef));

                    // Readd affix to the pool if needed
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
            HashSet<(PrototypeId, PrototypeId)> affixSet, BehaviorOnPowerMatch behaviorOnPowerMatch)
        {
            Logger.Debug("SetScope()");
            return MutationResults.None;
        }
    }
}
