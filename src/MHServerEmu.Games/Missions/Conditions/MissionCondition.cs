using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionCondition : ISerialize, IMissionConditionOwner
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public Mission Mission { get; private set; }
        public IMissionConditionOwner Owner { get; private set; }
        public MissionConditionPrototype Prototype { get; private set; }
        public Region Region { get => Mission.Region; }
        public Game Game { get => Mission.Game; }
        public bool EventsRegistered { get; protected set; }
        public bool IsReseting { get; private set; }

        protected int ConditionIndex;

        public MissionCondition(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype)
        {
            Mission = mission;
            Owner = owner;
            Prototype = prototype;
            ConditionIndex = -1;
        }

        public virtual bool Serialize(Archive archive) => false;

        public static MissionCondition CreateCondition(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype conditionProto)
        {
            if (MissionManager.Debug) Logger.Debug($"CreateCondition {GameDatabase.GetFormattedPrototypeName(mission.PrototypeDataRef)} {conditionProto.GetType().Name}");
            return conditionProto.AllocateCondition(mission, owner);
        }

        public virtual void Destroy() { }

        public bool Reset()
        {
            IsReseting = true;
            bool result = OnReset();
            IsReseting = false;
            return result;
        }

        public bool EvaluateEntityFilter(EntityFilterPrototype entityFilter, WorldEntity entity)
        {
            if (entity == null || entityFilter == null) return false;
            return entityFilter.Evaluate(entity, new(Mission.PrototypeDataRef));
        }

        public virtual bool IsCompleted() => false;
        public virtual void SetCompleted() { }
        public virtual bool OnReset() => false;
        public virtual bool Initialize(int conditionIndex) => true;
        public virtual void RegisterEvents(Region region) { }
        public virtual void UnRegisterEvents(Region region) { }
        public virtual void StoreConditionState(PropertyCollection properties, PropertyEnum propEnum, byte index) { }
        public virtual void RestoreConditionState(PropertyCollection properties, PropertyEnum propEnum, byte index) { }
        public virtual bool EvaluateOnReset() => false;
        public virtual bool GetCompletionCount(ref long currentCount, ref long requiredCount, bool isRequired) => false;
        public virtual void OnUpdateCondition(MissionCondition condition) => Owner.OnUpdateCondition(this);

        protected void OnUpdate()
        {
            Owner.OnUpdateCondition(this);
            if (IsCompleted())
                OnConditionCompleted();
        }

        public virtual bool OnConditionCompleted()
        {
            var storyNotification = Prototype.StoryNotification;
            if (storyNotification != null)
            {
                List<Player> participants = ListPool<Player>.Instance.Get();
                if (Mission.GetParticipants(participants))
                {
                    foreach (var player in participants)
                        player.SendStoryNotification(storyNotification, Mission.PrototypeDataRef);
                }
                ListPool<Player>.Instance.Return(participants);
            }

            return Owner.OnConditionCompleted();
        }
    }
}
