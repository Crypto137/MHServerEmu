using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Generators.Population;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Behavior
{
    public class BehaviorSensorySystem
    {
        private AIController _pAIController;
        public List<ulong> PotentialAllyTargetIds { get; private set; }
        public List<ulong> PotentialHostileTargetIds { get; private set; }

        private float _leashDistanceSq;
        public bool CanLeash { get; private set; }
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        internal bool ShouldSense()
        {
            throw new NotImplementedException();
        }

        internal void ValidateCurrentTarget(CombatTargetType targetType)
        {
            throw new NotImplementedException();
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
                    brain.Senses.Interrupt =BehaviorInterruptType.Alerted;
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
