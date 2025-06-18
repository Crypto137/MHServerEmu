using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Loot
{
    // NOTE: This is needed only for LootMutateAffixesPrototype (at least in 1.52)

    public class AffixPickerTable
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Picker<AffixPrototype>[] _pickers = new Picker<AffixPrototype>[(int)AffixPosition.NumPositions];

        public AffixPickerTable()
        {
        }

        public bool Initialize(uint mask, GRandom random)
        {
            for (AffixPosition affixPos = 0; affixPos < AffixPosition.NumPositions; affixPos++)
            {
                int i = (int)affixPos;

                if ((mask & (1u << i)) == 0)
                    continue;

                _pickers[i] = new(random);
            }

            return true;
        }

        public Picker<AffixPrototype> GetPicker(int affixPos)
        {
            if (affixPos < 0) return Logger.WarnReturn<Picker<AffixPrototype>>(null, "GetPicker(): affixPos < 0");
            if (affixPos >= _pickers.Length) return Logger.WarnReturn<Picker<AffixPrototype>>(null, "GetPicker(): affixPos >= _pickers.Length");

            return _pickers[affixPos];
        }

        public Picker<AffixPrototype> GetPicker(AffixPosition affixPos)
        {
            return GetPicker((int)affixPos);
        }
    }
}
