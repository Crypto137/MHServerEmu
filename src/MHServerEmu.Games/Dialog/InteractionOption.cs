using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;

namespace MHServerEmu.Games.Dialog
{
    public class InteractionOption : IComparable<InteractionOption>
    {
        public int Priority { get; protected set; }
        public InteractionMethod MethodEnum { get; protected set; }
        public MissionOptionTypeFlags OptionType { get; protected set; }
        public EntityFilterWrapper EntityFilterWrapper { get; protected set; }
        public EntityTrackingFlag EntityTrackingFlags { get; protected set; }
        public InteractionOptimizationFlags OptimizationFlags { get; set; }

        public InteractionOption()
        {
            Priority = 50;            
            EntityFilterWrapper = new();
            OptionType = MissionOptionTypeFlags.None; 
            MethodEnum = InteractionMethod.None;
            EntityTrackingFlags = EntityTrackingFlag.None;
            OptimizationFlags = InteractionOptimizationFlags.None;
        }

        public int CompareTo(InteractionOption other)
        {
            return Priority.CompareTo(other.Priority);
        }

        public virtual EntityTrackingFlag InterestedInEntity(EntityTrackingContextMap2 map, WorldEntity entity, SortedSet<InteractionOption> checkList)
        {
            checkList.Add(this);
            return EntityTrackingFlag.None;
        }
    }
}
