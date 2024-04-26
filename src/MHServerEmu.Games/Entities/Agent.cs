using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.System;
using MHServerEmu.Games.Entities.PowerCollections;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Entities
{
    public class Agent : WorldEntity
    {
        public AgentPrototype AgentPrototype { get => EntityPrototype as AgentPrototype; }
        public override bool IsTeamUpAgent => AgentPrototype is AgentTeamUpPrototype;

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

        // New
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
            ConditionCollection.AddCondition(condition);

            PowerCollection.AssignPower(startPowerRef, new());
            
            return true;
        }

        public bool AppendOnStartActions(PrototypeId targetRef)
        {
            if (GameDatabase.InteractionManager.GetStartAction(BaseData.PrototypeId, targetRef, out MissionActionEntityPerformPowerPrototype action))
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

        // Old
        public Agent(EntityBaseData baseData, ByteString archiveData) : base(baseData, archiveData) { }

        public Agent(EntityBaseData baseData) : base(baseData) { }
    }
}
