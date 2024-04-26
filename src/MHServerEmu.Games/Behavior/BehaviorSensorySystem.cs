
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

        private void UpdateAvatarSensory()
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
    }
}
