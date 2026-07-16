using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Tables;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class AlliancePrototype : Prototype
    {
        public PrototypeId[] HostileTo { get; protected set; }
        public PrototypeId[] FriendlyTo { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public AlliancePrototype WhileConfused { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public AlliancePrototype WhileControlled { get; protected set; }

        //---

        [DoNotCopy]
        public int EnumValue { get; private set; }

        public override void PostProcess()
        {
            base.PostProcess();

            EnumValue = 0;

            BlueprintId blueprintRef = GameDatabase.DataDirectory.GetPrototypeBlueprintDataRef(DataRef);
            if (!Verify.IsTrue(blueprintRef != BlueprintId.Invalid, "Unable to find the Alliance Blueprint."))
                return;

            EnumValue = GameDatabase.DataDirectory.GetPrototypeEnumValue(DataRef, blueprintRef);
        }

        public static bool IsHostileToPlayerAlliance(AlliancePrototype allianceProto)
        {
            if (allianceProto == null)
                return false;

            if (!Verify.IsNotNull(GameDatabase.GlobalsPrototype)) return false;
            if (!Verify.IsNotNull(GameDatabase.GlobalsPrototype.PlayerAlliance)) return false;

            return GameDatabase.GlobalsPrototype.PlayerAlliance.IsHostileTo(allianceProto);
        }

        public bool IsFriendlyTo(AlliancePrototype allianceProto)
        {
            if (allianceProto == null)
                return false;

            return GameDataTables.Instance.AllianceTable.IsFriendlyTo(this, allianceProto);
        }

        public bool IsHostileTo(AlliancePrototype allianceProto)
        {
            if (allianceProto == null)
                return false;

            return GameDataTables.Instance.AllianceTable.IsHostileTo(this, allianceProto);
        }
    }
}
