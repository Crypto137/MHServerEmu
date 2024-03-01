using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

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

    }
}
