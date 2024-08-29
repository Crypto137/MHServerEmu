using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Dialog
{
    public class InteractionOption
    {
        public static readonly Logger Logger = LogManager.CreateLogger();
        public int Priority { get; protected set; }
        public InteractionMethod MethodEnum { get; protected set; }
        public HUDEntityOverheadIcon IndicatorType { get; protected set; }
        public MissionOptionTypeFlags OptionType { get; protected set; }
        public EntityFilterWrapper EntityFilterWrapper { get; protected set; }
        public EntityTrackingFlag EntityTrackingFlags { get; protected set; }
        public InteractionOptimizationFlags OptimizationFlags { get; set; }
        public PrototypeId RegionFilterRef { get; protected set; }
        public PrototypeId AreaFilterRef { get; protected set; }
        public PrototypeId MissionFilterRef { get; protected set; }
        public LocaleStringId FailureReasonText { get; private set; }

        public InteractionOption()
        {
            Priority = 50;            
            EntityFilterWrapper = new();
            OptionType = MissionOptionTypeFlags.None; 
            MethodEnum = InteractionMethod.None;
            IndicatorType = HUDEntityOverheadIcon.None;
            EntityTrackingFlags = EntityTrackingFlag.None;
            OptimizationFlags = InteractionOptimizationFlags.None;
        }

        public int SortPriority(InteractionOption other)
        {
            return Priority.CompareTo(other.Priority);
        }

        public virtual EntityTrackingFlag InterestedInEntity(EntityTrackingContextMap map, WorldEntity entity, HashSet<InteractionOption> checkList)
        {
            checkList.Add(this);
            return EntityTrackingFlag.None;
        }

        public virtual bool IsCurrentlyAvailable(EntityDesc interacteeDesc, WorldEntity localInteractee, WorldEntity interactor, InteractionFlags interactionFlags)
        {
            return true;
        }

        public bool Evaluate(EntityDesc interacteeDesc, WorldEntity interactor, InteractionFlags interactionFlags, 
            ref InteractionMethod outInteractions, ref InteractData outInteractData)
        {
            if (interactor == null) return false;

            WorldEntity localInteractee = interacteeDesc.GetEntity<WorldEntity>(interactor.Game);
            bool isAvailable = IsCurrentlyAvailable(interacteeDesc, localInteractee, interactor, interactionFlags);
            FillOutputData(isAvailable, ref outInteractions, ref outInteractData, localInteractee, interactor);

            return isAvailable;
        }

        public virtual void FillOutputData(bool isAvailable, ref InteractionMethod outInteractions, ref InteractData outInteractData, WorldEntity localInteractee, WorldEntity interactor)
        {
            if (isAvailable)
                outInteractions |= MethodEnum;
            if (outInteractData != null)
            {
                if (isAvailable)
                    InteractionManager.TrySetIndicatorTypeAndMapOverrideWithPriority(localInteractee, ref outInteractData.IndicatorType, ref outInteractData.MapIconOverrideRef, IndicatorType);                
                else if (FailureReasonText != LocaleStringId.Blank)
                    outInteractData.FailureReasonText = FailureReasonText;
            }
        }
    }
}
