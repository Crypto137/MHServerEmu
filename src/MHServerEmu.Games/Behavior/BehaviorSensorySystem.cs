using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Generators.Population;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Behavior
{
    public class BehaviorSensorySystem
    {
        private AIController _pAIController;
        public List<ulong> PotentialAllyTargetIds { get; private set; }
        public List<ulong> PotentialHostileTargetIds { get; private set; }

        private float _leashDistanceSq;
        public bool CanLeash { get; set; }
        public BehaviorInterruptType Interrupt { get; set; }

        public BehaviorSensorySystem()
        {
            PotentialAllyTargetIds = new();
            PotentialHostileTargetIds = new();
            _leashDistanceSq = 3000.0f * 3000.0f; // Default LeashDistance in PopulationObject
        }

        public void Sense()
        {
            var agent = _pAIController.Owner;
            if (agent != null)
            {
                UpdateAvatarSensory();
                if (agent.IsDormant == false)
                {
                    IsLeashingDistanceMet();
                    HandleInterrupts();
                }
            }
        }

        public void Initialize(AIController aIController, BehaviorProfilePrototype profile, SpawnSpec spec)
        {
            _pAIController = aIController;
            CanLeash = profile.CanLeash;
            if (spec != null)
                _leashDistanceSq = MathHelper.Square(spec.LeashDistance);
        }

        private bool IsLeashingDistanceMet()
        {
            if (CanLeash)
            {
                Agent ownerAgent = _pAIController?.Owner;
                if (ownerAgent == null) return false;
                BehaviorBlackboard blackboard = _pAIController.Blackboard;
                if (_leashDistanceSq > 0.0f && Vector3.DistanceSquared2D(blackboard.SpawnPoint, ownerAgent.RegionLocation.Position) > _leashDistanceSq) 
                {
                    blackboard.PropertyCollection[PropertyEnum.AIIsLeashing] = true;
                    return true;
                }
            }
            return false;
        }

        public void UpdateAvatarSensory()
        {
            if (_pAIController == null) return;
            Agent ownerAgent = _pAIController.Owner;
            if (ownerAgent == null) return;

            BehaviorBlackboard blackboard = _pAIController.Blackboard;
            AgentPrototype prototype = ownerAgent.AgentPrototype;
            if (prototype == null) return;

            bool ownerIsDormant = ownerAgent.IsDormant;
            float hostilePlayersNearbyCheckRange = blackboard.PropertyCollection[PropertyEnum.AIHostilePlayersNearbyCheckRange];
            float wakeRange = prototype.WakeRange;
            float returnToDormantRange = prototype.ReturnToDormantRange;
            float maxRange;

            if (ownerIsDormant)
            {
                if (Segment.IsNearZero(wakeRange) || wakeRange < 0.0f) return;
                maxRange = wakeRange;
            }
            else
            {
                if ((Segment.IsNearZero(returnToDormantRange) || returnToDormantRange < 0.0f) 
                    && (Segment.IsNearZero(hostilePlayersNearbyCheckRange) || hostilePlayersNearbyCheckRange < 0.0f)) return;
                maxRange = Math.Max(returnToDormantRange, hostilePlayersNearbyCheckRange);
            }

            Vector3 position = ownerAgent.RegionLocation.Position;
            int numHostileNearby = 0;
            bool returnToDormant = returnToDormantRange > 0f;

            if (maxRange > 0)
            {
                Region ownersRegion = ownerAgent.Region;
                if (ownersRegion == null) return;

                foreach (Avatar avatar in ownersRegion.IterateAvatarsInVolume(new Sphere(position, maxRange)))
                {
                    if (avatar == null || avatar.IsInWorld == false) continue;

                    Vector3 avatarPosition = avatar.RegionLocation.Position;
                    float distToAvatarSq = Vector3.DistanceSquared(position, avatarPosition);

                    if (ownerIsDormant)
                    {
                        if (wakeRange > 0 && distToAvatarSq <= wakeRange * wakeRange)
                        {
                            ownerAgent.SetDormant(false);
                            List<WorldEntity> entities = SpawnGroup.GetEntities(ownerAgent, SpawnGroupEntityQueryFilterFlags.All);
                            foreach (WorldEntity entity in entities)
                                if (entity is Agent groupAgent && ownerAgent != groupAgent && groupAgent.IsDormant)
                                    groupAgent.SetDormant(false);
                            break;
                        }
                    }
                    else if (returnToDormant && distToAvatarSq <= returnToDormantRange * returnToDormantRange)
                    {
                        returnToDormant = false;
                        if (Segment.IsNearZero(hostilePlayersNearbyCheckRange) || hostilePlayersNearbyCheckRange < 0f)
                            break;
                    }

                    if (hostilePlayersNearbyCheckRange > 0.0f 
                        && distToAvatarSq <= hostilePlayersNearbyCheckRange * hostilePlayersNearbyCheckRange
                        && Combat.ValidTarget(ownerAgent.Game, ownerAgent, avatar, CombatTargetType.Hostile, false))
                            numHostileNearby++;
                }

                if (ownerIsDormant == false && returnToDormant)
                    ownerAgent.SetDormant(true);

                if (numHostileNearby > 0)
                    blackboard.PropertyCollection[PropertyEnum.AINumHostilePlayersNearby] = numHostileNearby;
            }
        }

        private void HandleInterrupts()
        {
            if (Interrupt != BehaviorInterruptType.None)
            {
                if (_pAIController == null) return;
                _pAIController.ProcessInterrupts(Interrupt);
                ClearInterrupts();
            }
        }

        private void ClearInterrupts()
        {
            Interrupt = BehaviorInterruptType.None;
        }

        public WorldEntity GetCurrentTarget()
        {
            if (_pAIController == null) return null;

            ulong targetId = _pAIController.Blackboard.PropertyCollection[PropertyEnum.AIRawTargetEntityID];
            if (targetId != 0)
            {
                Entity targetEntity = _pAIController.Game.EntityManager.GetEntity<Entity>(targetId);
                if (targetEntity == null) return null;
                if (targetEntity is WorldEntity targetWorldEntity)
                    return targetWorldEntity;
            }
            return null;
        }

        public bool ShouldSense()
        {
            if (_pAIController == null) return false;
            BehaviorBlackboard blackboard = _pAIController.Blackboard;
            Game game = _pAIController.Game;
            if (game == null)  return false;

            if (game.GetCurrentTime().TotalMilliseconds > blackboard.PropertyCollection[PropertyEnum.AINextSensoryUpdate])
            {
                blackboard.PropertyCollection[PropertyEnum.AINextSensoryUpdate] = (long)game.RealGameTime.TotalMilliseconds + 1000;
                return true;
            }
            return false;
        }

        public void ValidateCurrentTarget(CombatTargetType targetType)
        {
            if (_pAIController == null) return;
            BehaviorBlackboard blackboard = _pAIController.Blackboard;
            Agent agent = _pAIController.Owner;
            if (agent == null) return;
            if (blackboard.PropertyCollection[PropertyEnum.AIRawTargetEntityID] != 0)
            {
                WorldEntity target = GetCurrentTarget();
                if (Combat.ValidTarget(agent.Game, agent, target, targetType, true) == false)
                    _pAIController.ResetCurrentTargetState();
            }
        }

        public void NotifyAlliesOnTargetAquired()
        {
            Agent ownerAgent = _pAIController.Owner;
            if (ownerAgent == null) return;

            List<WorldEntity> allies = GetPopulationGroup();
            foreach (var entity in allies)
            {
                if (entity is not Agent ally) continue;
                AIController brain = ally.AIController;
                if (brain == null) continue;
                if (brain.Blackboard.PropertyCollection[PropertyEnum.AIRawTargetEntityID] == 0)
                {
                    brain.SetTargetEntity(GetCurrentTarget());
                    brain.Senses.Interrupt = BehaviorInterruptType.Alerted;
                }
            }
        }

        private List<WorldEntity> GetPopulationGroup()
        {
            List<WorldEntity> populationGroup = new ();
            if (_pAIController != null)
            {
                Agent ownerAgent = _pAIController.Owner;
                ownerAgent?.SpawnSpec?.Group?.GetEntities(out populationGroup, SpawnGroupEntityQueryFilterFlags.All);
            }
            return populationGroup;
        }

        public void NotifyAlliesOnOwnerKilled()
        {
            Agent ownerAgent = _pAIController.Owner;
            if (ownerAgent == null) return;

            List<WorldEntity> allies = GetPopulationGroup();
            foreach (var entity in allies)
            {
                if (entity is not Agent ally) continue;
                AIController brain = ally.AIController;
                if (brain == null) continue;                
                brain.Senses?.OnLeaderDeath(ownerAgent);
            }
        }

        public void OnLeaderDeath(Agent leader)
        {
            Interrupt = BehaviorInterruptType.AllyDeath;
            if (_pAIController == null) return;
            _pAIController.OnAILeaderDeath();

            var collection = _pAIController.Blackboard?.PropertyCollection;
            if (collection != null && leader != null && collection[PropertyEnum.AILeaderID] == leader.Id)
                collection[PropertyEnum.AILeaderID] = 0;
        }

        internal void NotifyAlliesOnTargetAcquired()
        {
            throw new NotImplementedException();
        }
    }

    [Flags]
    public enum BehaviorInterruptType
    {
        None = 0,
        Alerted = 1 << 0,
        AllyDeath = 1 << 1,
        CollisionWithTarget = 1 << 2,
        Command = 1 << 3,
        Defeated = 1 << 4,
        ForceIdle = 1 << 5,
        InitialBranch = 1 << 6,
        LeashDistanceMet = 1 << 7,
        NoTarget = 1 << 8,
        TargetSighted = 1 << 9,
        Override = 1 << 10,
    }
}
