
using MHServerEmu.Games.Powers;

namespace MHServerEmu.Games.Entities
{
    public class ConditionCollection : List<Condition>
    {
        public bool HasANegativeStatusEffectCondition()
        {
            foreach (Condition condition in this)
                if (condition != null && condition.IsANegativeStatusEffect()) return true;
            return false;
        }
    }
}
