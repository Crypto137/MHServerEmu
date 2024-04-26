

namespace MHServerEmu.Games.Behavior.ProceduralAI
{
    public class ProceduralAI
    {
        private AIController _AIController;

        public void StopOwnerLocomotor()
        {
            var agent = _AIController.Owner;
            if (agent != null)
                if (agent.IsInWorld)
                    agent.Locomotor?.Stop();
        }
    }
}
