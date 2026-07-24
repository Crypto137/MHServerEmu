using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.LiveTuning;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PublicEventPrototype : Prototype
    {
        public bool DefaultEnabled { get; protected set; }
        public LocaleStringId Name { get; protected set; }
        public PrototypeId[] Teams { get; protected set; }
        public AssetId PanelName { get; protected set; }

        //---

        [DoNotCopy]
        public int PublicEventPrototypeEnumValue { get; private set; }

        public override void PostProcess()
        {
            base.PostProcess();

            PublicEventPrototypeEnumValue = GetEnumValueFromBlueprint(LiveTuningData.GetPublicEventBlueprintDataRef());

            if (Teams.HasValue())
            {
                foreach (PrototypeId teamDataRef in Teams)
                {
                    if (!Verify.IsTrue(teamDataRef != PrototypeId.Invalid))
                        continue;

                    PublicEventTeamPrototype teamProto = teamDataRef.As<PublicEventTeamPrototype>();
                    if (!Verify.IsNotNull(teamProto))
                        continue;

                    if (!Verify.IsTrue(teamProto.PublicEventRef == PrototypeId.Invalid))
                        continue;

                    teamProto.PublicEventRef = DataRef;
                }
            }
        }

        public int GetEventInstance()
        {
            return (int)LiveTuningManager.GetLivePublicEventTuningVar(this, PublicEventTuningVar.ePETV_EventInstance);
        }
    }

    public class PublicEventTeamPrototype : Prototype
    {
        public LocaleStringId Name { get; protected set; }

        //---

        [DoNotCopy]
        public PrototypeId PublicEventRef { get; set; }
    }
}
