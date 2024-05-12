using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System;
using MHServerEmu.Games.Behavior;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Entities.PowerCollections;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Tables;
using MHServerEmu.Games.Generators.Population;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Entities
{
    public class Agent : WorldEntity
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public AIController AIController { get; private set; }
        public AgentPrototype AgentPrototype { get => EntityPrototype as AgentPrototype; }
        public override bool IsTeamUpAgent { get => AgentPrototype is AgentTeamUpPrototype; }

        public override bool IsSummonedPet
        {
            get
            {
                if (this is Missile) return false;
                if (IsTeamUpAgent) return true;
                
                PrototypeId powerRef = Properties[PropertyEnum.CreatorPowerPrototype];
                if (powerRef != PrototypeId.Invalid)
                {
                    var powerProto = GameDatabase.GetPrototype<SummonPowerPrototype>(powerRef);
                    if (powerProto != null)
                        return powerProto.IsPetSummoningPower();
                }

                return false;
            }

        }

        public override bool CanRotate
        {
            get
            {
                Player ownerPlayer = GetOwnerOfType<Player>();
                if ( IsInKnockback || IsInKnockdown || IsInKnockup 
                    || IsImmobilized || IsImmobilizedByHitReact || IsSystemImmobilized 
                    || IsStunned || IsMesmerized ||
                    (ownerPlayer != null && ownerPlayer.IsFullscreenMoviePlaying || ownerPlayer.IsOnLoadingScreen)
                    || NPCAmbientLock)
                    return false;
                return true;
            }
        }

        public Agent(Game game) : base(game) { }

        public override void Initialize(EntitySettings settings)
        {
            var agentProto = GameDatabase.GetPrototype<AgentPrototype>(settings.EntityRef);
            if (agentProto == null) return;
            if (agentProto.Locomotion.Immobile == false) Locomotor = new();

            // GetPowerCollectionAllocateIfNull()
            base.Initialize(settings);
            // InitPowersCollection
            InitLocomotor(settings.LocomotorHeightOverride);
        }

        private bool InitAI(EntitySettings settings)
        {
            var agentPrototype = AgentPrototype;
            if (agentPrototype == null || Game == null || this is Avatar) return false;

            var behaviorProfile = agentPrototype.BehaviorProfile;
            if (behaviorProfile != null && behaviorProfile.Brain != PrototypeId.Invalid)
            {
                AIController = new(Game, this);
                PropertyCollection collection = new ();
                collection[PropertyEnum.AIIgnoreNoTgtOverrideProfile] = Properties[PropertyEnum.AIIgnoreNoTgtOverrideProfile];
                SpawnSpec spec = settings?.SpawnSpec ?? new SpawnSpec();
                return AIController.Initialize(behaviorProfile, spec, collection);
            }
            return false;
        }

        private bool InitLocomotor(float height = 0.0f)
        {
            if (Locomotor != null)
            {
                AgentPrototype agentPrototype = AgentPrototype;
                if (agentPrototype == null) return false;

                Locomotor.Initialize(agentPrototype.Locomotion, this, height);
                Locomotor.SetGiveUpLimits(8.0f, TimeSpan.FromMilliseconds(250));
            }
            return true;
        }

        public override void OnEnteredWorld(EntitySettings settings)
        {
            base.OnEnteredWorld(settings);
            RegionLocation.Cell.EnemySpawn(); // Calc Enemy
            // ActivePowerRef = settings.PowerPrototype

            if (AIController != null) 
            {
                var behaviorProfile = AgentPrototype?.BehaviorProfile;
                if (behaviorProfile == null) return;
                AIController.Initialize(behaviorProfile, null, null);
            }
            else InitAI(settings);

            AIController?.OnAIEnteredWorld();
        }

        public override void OnExitedWorld()
        {
            base.OnExitedWorld();
            AIController?.OnAIExitedWorld();
        }

        public override void AppendStartAction(PrototypeId actionsTarget) // TODO rewrite this
        {
            bool startAction = false;

            if (EntityActionComponent != null && EntityActionComponent.ActionTable.TryGetValue(EntitySelectorActionEventType.OnSimulated, out var actionSet))
                startAction = AppendSelectorActions(actionSet);
            if (startAction == false && actionsTarget != PrototypeId.Invalid)
                AppendOnStartActions(actionsTarget);
        }

        private bool AppendStartPower(PrototypeId startPowerRef)
        {
            if (startPowerRef == PrototypeId.Invalid) return false;
            //Console.WriteLine($"[{Id}]{GameDatabase.GetPrototypeName(startPowerRef)}");

            Condition condition = new();
            condition.InitializeFromPowerMixinPrototype(1, startPowerRef, 0, TimeSpan.Zero);
            condition.StartTime = Clock.GameTime;
            _conditionCollection.AddCondition(condition);

            AssignPower(startPowerRef, new());
            
            return true;
        }

        public bool AppendOnStartActions(PrototypeId targetRef)
        {
            if (GameDatabase.InteractionManager.GetStartAction(BaseData.EntityPrototypeRef, targetRef, out MissionActionEntityPerformPowerPrototype action))
                return AppendStartPower(action.PowerPrototype);
            return false;
        }

        public bool AppendSelectorActions(HashSet<EntitySelectorActionPrototype> actions)
        {
            var action = actions.First();
            if (action.AIOverrides.HasValue())
            {
                int index = Game.Random.Next(0, action.AIOverrides.Length);
                var actionAIOverrideRef = action.AIOverrides[index];
                if (actionAIOverrideRef == PrototypeId.Invalid) return false;
                var actionAIOverride = actionAIOverrideRef.As<EntityActionAIOverridePrototype>();
                if (actionAIOverride != null) return AppendStartPower(actionAIOverride.Power);
            }
            return false;
        }

        public virtual bool HasPowerInPowerProgression(PrototypeId powerRef)
        {
            if (IsTeamUpAgent)
                return GameDataTables.Instance.PowerOwnerTable.GetTeamUpPowerProgressionEntry(PrototypeDataRef, powerRef) != null;

            return false;
        }

        public virtual bool GetPowerProgressionInfo(PrototypeId powerProtoRef, out PowerProgressionInfo info)
        {
            // Note: this implementation is meant only for team-up agents

            info = new();

            if (powerProtoRef == PrototypeId.Invalid)
                return Logger.WarnReturn(false, "GetPowerProgressionInfo(): powerProtoRef == PrototypeId.Invalid");

            var teamUpProto = PrototypeDataRef.As<AgentTeamUpPrototype>();
            if (teamUpProto == null)
                return Logger.WarnReturn(false, "GetPowerProgressionInfo(): teamUpProto == null");

            var powerProgressionEntry = GameDataTables.Instance.PowerOwnerTable.GetTeamUpPowerProgressionEntry(teamUpProto.DataRef, powerProtoRef);
            if (powerProgressionEntry != null)
                info.InitForTeamUp(powerProgressionEntry);
            else
                info.InitNonProgressionPower(powerProtoRef);

            return info.IsValid;
        }

        internal int GetPowerRank(PrototypeId powerRef)
        {
            throw new NotImplementedException();
        }

        // Old
        public Agent(EntityBaseData baseData, ByteString archiveData) : base(baseData, archiveData) { }

        public Agent(EntityBaseData baseData) : base(baseData) { }
    }
}
