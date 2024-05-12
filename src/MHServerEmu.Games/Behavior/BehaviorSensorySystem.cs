using MHServerEmu.Games.Entities;

namespace MHServerEmu.Games.Behavior
{
    public class BehaviorSensorySystem
    {
        private AIController _pAIController;

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

        public void UpdateAvatarSensory()
        {
            throw new NotImplementedException();
        }

        private void HandleInterrupts()
        {
            throw new NotImplementedException();
        }

        private void IsLeashingDistanceMet()
        {
            throw new NotImplementedException();
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
    }
}
