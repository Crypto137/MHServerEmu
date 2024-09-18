using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Dialog
{
    public class StoryWarpOption : InteractionOption
    {
        public StoryWarpOption()
        {
            Priority = 50;
            MethodEnum = InteractionMethod.StoryWarp;
            IndicatorType = HUDEntityOverheadIcon.StoryWarp;
        }
    }
}
