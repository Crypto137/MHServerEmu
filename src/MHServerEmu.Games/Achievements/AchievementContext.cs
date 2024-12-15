using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Achievements
{
    public enum EventContextType
    {
        Avatar,
        Item,
        Party,
        Pet,
        Region,
        DifficultyTierMin,
        DifficultyTierMax,
        TeamUp,
        PublicEventTeam
    }

    public struct AchievementContext
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public uint Id { get; set; }
        public long RewardPrototype { get; set; }
        public ScoringEventType EventType { get; set; }
        public List<AchievementEventData> EventData { get; set; }
        public List<AchievementEventContext> EventContext { get; set; }

        public static Prototype GetPrototype(long prototypeGuid)
        {
            if (prototypeGuid == 0) return null;
            PrototypeId protoRef = GameDatabase.GetDataRefByPrototypeGuid((PrototypeGuid)prototypeGuid);
            if (protoRef == PrototypeId.Invalid) return Logger.WarnReturn((Prototype)null, $"GetPrototype Guid {prototypeGuid} have not DataRef");
            Prototype proto = GameDatabase.GetPrototype<Prototype>(protoRef);
            if (proto == null) return Logger.WarnReturn((Prototype)null, $"GetPrototype DataRef {protoRef} have not Prototype");
            return proto;
        }

        public readonly ScoringEventData GetScoringEventData()
        {
            ScoringEventData eventData = new();
            int count = EventData.Count;

            if (count == 0) return eventData;
            eventData.Proto0 = GetPrototype(EventData[0].Prototype);
            eventData.Proto0IncludeChildren = EventData[0].IncludeChildren;

            if (count == 1) return eventData;            
            eventData.Proto1 = GetPrototype(EventData[1].Prototype);
            eventData.Proto1IncludeChildren = EventData[1].IncludeChildren;            

            if (count == 2) return eventData;            
            eventData.Proto2 = GetPrototype(EventData[2].Prototype);
            eventData.Proto2IncludeChildren = EventData[2].IncludeChildren;
            
            return eventData;
        }

        public readonly ScoringEventContext GetScoringEventContext()
        {
            ScoringEventContext eventContext = new();
            foreach (var context in EventContext)
            {
                var prototype = GetPrototype(context.Prototype);
                switch (context.ContextType) {
                    case EventContextType.Region:
                        eventContext.Region = prototype;
                        eventContext.RegionIncludeChildren = context.IncludeChildren;
                        break;
                    case EventContextType.Avatar:
                        eventContext.Avatar = prototype;
                        break;
                    case EventContextType.Item:
                        eventContext.Item = prototype;
                        break;
                    case EventContextType.Party:
                        eventContext.Party = prototype;
                        break;
                    case EventContextType.Pet:
                        eventContext.Pet = prototype;
                        break;
                    case EventContextType.DifficultyTierMin:
                        eventContext.DifficultyTierMin = prototype as DifficultyTierPrototype;
                        break;
                    case EventContextType.DifficultyTierMax:
                        eventContext.DifficultyTierMax = prototype as DifficultyTierPrototype;
                        break;
                    case EventContextType.TeamUp:
                        eventContext.TeamUp = prototype;
                        break;
                    case EventContextType.PublicEventTeam:
                        eventContext.PublicEventTeam = prototype;
                        break;
                }
            }
            return eventContext;
        }
    }

    public struct AchievementEventData
    {
        public long Prototype { get; set; }
        public bool IncludeChildren { get; set; }
    }

    public struct AchievementEventContext
    {
        public EventContextType ContextType { get; set; }
        public long Prototype { get; set; }
        public bool IncludeChildren { get; set; }
    }
}
