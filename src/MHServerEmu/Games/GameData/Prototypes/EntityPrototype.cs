using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class EntityPrototype : Prototype
    {
        public LocaleStringId DisplayName { get; }
        public LocaleStringId DisplayNameInformal { get; }
        public LocaleStringId DisplayNameShort { get; }
        public Prototype[] EvalOnCreate { get; }                // R list Eval/Eval.defaults
        public StringId IconPath { get; }                       // A Entity/Types/EntityIconPathType.type
        public StringId IconPathHiRes { get; }                  // A Entity/Types/EntityIconPathType.type
        public StringId IconPathTooltipHeader { get; }          // A Entity/Types/EntityIconPathType.type
        public long LifespanMS { get; }
        public PrototypePropertyCollection Properties { get; }  // Populated from mixins?
        public bool ReplicateToOwner { get; }
        public bool ReplicateToParty { get; }
        public bool ReplicateToProximity { get; }
        public bool ReplicateToDiscovered { get; }
        public bool ReplicateToTrader { get; }
        public Prototype[] Inventories { get; }                 // R list Entity/Inventory/EntityInventoryAssignment.defaults
    }

    public class WorldEntityPrototype : EntityPrototype
    {
    }
}
