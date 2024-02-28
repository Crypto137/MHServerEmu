using MHServerEmu.Common.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Dialog
{
    public class InteractionManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private Dictionary<PrototypeId, InteractionData> _interationMap;
        private Dictionary<PrototypeId, ExtraMissionData> _missionMap;
        private List<InteractionOption> _options;

        public InteractionManager()
        {
            _interationMap = new();
            _missionMap = new();
            _options = new();
        }

        public void Initialize()
        {
            foreach (var missionRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy(typeof(MissionPrototype), 
                PrototypeIterateFlags.NoAbstract | PrototypeIterateFlags.ApprovedOnly))
            {
                MissionPrototype missionProto = GameDatabase.GetPrototype<MissionPrototype>(missionRef);
                if (missionProto == null) continue;
                GetInteractionDataFromMissionPrototype(missionProto);
            }

            foreach (var kvp in _missionMap)
            {
                var missionData = kvp.Value;
                if (missionData == null) continue;
                if (missionData.CompleteOptions.Any())
                {
                    HashSet<PrototypeId> contexts = new (missionData.Contexts);
                    if (contexts.Any())
                    {
                        foreach (var completeOption in missionData.CompleteOptions)
                        {
                            if (!contexts.Any())
                            {
                                Logger.Warn($"Unable to link option to mission. MISSION={GameDatabase.GetFormattedPrototypeName(missionData.MissionRef)} OPTION={completeOption.ToString()}");
                                continue;
                            }
                            BindOptionToMap(completeOption, contexts);
                            missionData.PlayerHUDShowObjs |= (completeOption.MissionProto.PlayerHUDShowObjs == true);
                        }
                    }
                }
            }

            foreach (var uiWidgetRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy(typeof(MetaGameDataPrototype), 
                PrototypeIterateFlags.NoAbstract | PrototypeIterateFlags.ApprovedOnly))
            {
                if (uiWidgetRef == PrototypeId.Invalid) continue;
                GetInteractionDataFromUIWidgetPrototype(uiWidgetRef);
            }

            foreach (var metaStateRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy(typeof(MetaStatePrototype), 
                PrototypeIterateFlags.NoAbstract | PrototypeIterateFlags.ApprovedOnly))
            {
                if (metaStateRef == PrototypeId.Invalid) continue;
                GetInteractionDataFromMetaStatePrototype(metaStateRef);
            }

            foreach (var kvp in _interationMap)
                kvp.Value?.Sort();
        }

        private void BindOptionToMap(InteractionOption option, HashSet<PrototypeId> contexts)
        {
            throw new NotImplementedException();
        }

        private void GetInteractionDataFromMetaStatePrototype(PrototypeId metaStateRef)
        {
            throw new NotImplementedException();
        }

        private void GetInteractionDataFromUIWidgetPrototype(PrototypeId uiWidgetRef)
        {
            throw new NotImplementedException();
        }

        private void GetInteractionDataFromMissionPrototype(MissionPrototype missionProto)
        {
            throw new NotImplementedException();
        }
    }

    public class InteractionData
    {
        private List<InteractionOption> _options;
        public int OptionFlags { get; set; }

        public void Sort()
        {
            _options.Sort((a, b) => a.CompareTo(b));
        }

        public InteractionData()
        {
            _options = new();
            OptionFlags = 0;
        }
    }

    public class ExtraMissionData
    {      
        public PrototypeId MissionRef { get; set; }
        public SortedSet<BaseMissionOption> Options { get; set; }
        public SortedSet<PrototypeId> Contexts { get; set; }
        public SortedSet<BaseMissionOption> CompleteOptions { get; set; }
        public bool PlayerHUDShowObjs { get; set; }

        public ExtraMissionData(PrototypeId missionRef)
        {
            MissionRef = missionRef;
            Options = new();
            Contexts = new();
            CompleteOptions = new();
            PlayerHUDShowObjs = true;
        }
    }

}
