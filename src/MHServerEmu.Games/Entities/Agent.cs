using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Behavior;
using MHServerEmu.Games.Behavior.StaticAI;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Entities.Locomotion;
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
        public AgentPrototype AgentPrototype { get => Prototype as AgentPrototype; }
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

        public bool HasPowerPreventionStatus { get; internal set; }

        public Agent(Game game) : base(game) { }

        public override bool Initialize(EntitySettings settings)
        {
            var agentProto = GameDatabase.GetPrototype<AgentPrototype>(settings.EntityRef);
            if (agentProto == null) return false;
            if (agentProto.Locomotion.Immobile == false) Locomotor = new();

            // GetPowerCollectionAllocateIfNull()
            base.Initialize(settings);

            // Agents (team-ups and players) need an invloc to be recognized as belonging to the player
            BaseData.FieldFlags |= EntityCreateMessageFlags.HasInvLoc;

            // InitPowersCollection
            InitLocomotor(settings.LocomotorHeightOverride);

            return true;
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

            if (AIController != null)
            {
                AIController.OnAIEnteredWorld();
                ActivateAI();
            }
        }

        public void ActivateAI()
        {
            if (AIController == null) return;
            BehaviorBlackboard blackboard = AIController.Blackboard;
            if (blackboard.PropertyCollection[PropertyEnum.AIStartsEnabled])
                AIController.SetIsEnabled(true);
            blackboard.SpawnOffset = (SpawnSpec != null) ? SpawnSpec.Transform.Translation : Vector3.Zero;
            if (IsInWorld)
                AIController.OnAIActivated();
        }

        public override void OnKilled(WorldEntity killer, KillFlags killFlags, WorldEntity directKiller)
        {
            // TODO other events

            if (AIController != null)
            {
                AIController.OnAIKilled();
                AIController.SetIsEnabled(false);
            }

            EndAllPowers(false);

            Locomotor locomotor = Locomotor;
            if (locomotor != null)
            {
                locomotor.Stop();
                locomotor.SetMethod(LocomotorMethod.Default, 0.0f);
            }

            base.OnKilled(killer, killFlags, directKiller);
        }

        public void Think()
        {
            AIController?.Think();
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

        public int GetPowerRank(PrototypeId powerRef)
        {
            if (powerRef == PrototypeId.Invalid) return 0;
            return Properties[PropertyEnum.PowerRankCurrentBest, powerRef];
        }

        public void SetDormant(bool dormant)
        {
            if (IsDormant != dormant)
            {
                if (dormant == false)
                {
                    AgentPrototype prototype = AgentPrototype;
                    if (prototype == null) return;
                    if (prototype.WakeRandomStartMS > 0 && IsControlledEntity == false)
                        ScheduleRandomWakeStart(prototype.WakeRandomStartMS);
                    else
                        Properties[PropertyEnum.Dormant] = dormant;
                }
                else
                    Properties[PropertyEnum.Dormant] = dormant;
            }
        }

        private void ScheduleRandomWakeStart(int wakeRandomStartMS)
        {
            throw new NotImplementedException();
        }

        public InventoryResult CanEquip(Item item, ref PropertyEnum propertyRestriction)
        {
            // TODO
            return InventoryResult.Success;     // Bypass property restrictions
        }

        protected override bool InitInventories(bool populateInventories)
        {
            // TODO
            return base.InitInventories(populateInventories);
        }

        internal IsInPositionForPowerResult IsInPositionForPower(Power power, WorldEntity target, Vector3 targetPosition)
        {
            throw new NotImplementedException();
        }

        internal PowerUseResult CanActivatePower(Power power, ulong targetId, Vector3 targetPosition, ulong flags = 0, ulong itemSourceId = 0)
        {
            throw new NotImplementedException();
        }
    }

    public enum IsInPositionForPowerResult
    {
        Error,
        Success,
        NotClearLocation,
        OutOfRange,
        NoPowerLOS
    }
}
