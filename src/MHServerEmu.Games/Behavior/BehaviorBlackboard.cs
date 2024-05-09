
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Behavior
{
    public class BehaviorBlackboard
    {
        private Agent _owner; 
        public PropertyCollection PropertyCollection { get; internal set; }

        public BehaviorBlackboard(Agent owner)
        {
            _owner = owner;
            PropertyCollection = new ();
        }
    }
}
