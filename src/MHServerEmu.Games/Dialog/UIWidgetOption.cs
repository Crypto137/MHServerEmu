using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.Dialog
{
    public class UIWidgetOption : InteractionOption
    {
        public PrototypeId UIWidgetRef { get; set; }
        public UIWidgetEntityIconsPrototype Proto { get; set; } 

        public UIWidgetOption() 
        {
            OptimizationFlags |= InteractionOptimizationFlags.Hint;
        }

        public override EntityTrackingFlag InterestedInEntity(EntityTrackingContextMap2 map, WorldEntity entity, SortedSet<InteractionOption> checkList)
        {
            MetaGameDataPrototype metaGameDataProto = GameDatabase.GetPrototype<MetaGameDataPrototype>(UIWidgetRef);
            var trackingFlag = EntityTrackingFlag.None;
            if (metaGameDataProto == null) return trackingFlag;

            if (metaGameDataProto is UIWidgetEntityIconsPrototype uiWidgetEntityIconsProto && uiWidgetEntityIconsProto.Entities.HasValue())
                foreach (var uiWidgetEntryProto in uiWidgetEntityIconsProto.Entities)
                    if (uiWidgetEntryProto != null && uiWidgetEntryProto.Filter != null && uiWidgetEntryProto.Filter.Evaluate(entity, new()))
                    {
                        map.Insert(UIWidgetRef, EntityTrackingFlag.HUD);
                        trackingFlag |= EntityTrackingFlag.HUD;
                    }

            return trackingFlag;
        }
    }
}
