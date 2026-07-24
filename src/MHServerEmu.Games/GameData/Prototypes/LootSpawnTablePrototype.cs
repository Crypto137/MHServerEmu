using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Loot;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class LootLocationNodePrototype : Prototype
    {
        public short Weight { get; protected set; }
        public LootLocationModifierPrototype[] Modifiers { get; protected set; }

        //---

        public virtual void Roll(LootLocationData lootLocationData)
        {
            if (Modifiers.IsNullOrEmpty())
                return;

            foreach (LootLocationModifierPrototype modProto in Modifiers)
                modProto.Apply(lootLocationData);
        }
    }

    public class LootLocationTablePrototype : LootLocationNodePrototype
    {
        public LootLocationNodePrototype[] Choices { get; protected set; }

        //---

        public override void Roll(LootLocationData lootLocationData)
        {
            base.Roll(lootLocationData);

            if (!Verify.IsTrue(Choices.HasValue(), $"LootSpawnTable has no Choices! {this}"))
                return;

            LootLocationNodePrototype pick;

            if (Choices.Length == 1)
            {
                // If have only one possible choice, just pick it straight away instead of initializing a picker
                pick = Choices[0];
            }
            else
            {
                // Pick one of multiple choices
                Picker<LootLocationNodePrototype> possibleNodes = new(lootLocationData.Game.Random);
                foreach (LootLocationNodePrototype choiceProto in Choices)
                    possibleNodes.Add(choiceProto, choiceProto.Weight);

                if (!Verify.IsTrue(possibleNodes.Empty() == false, $"No LootNodePrototypes to pick from! LootSpawnTable: {this}"))
                    return;

                pick = possibleNodes.Pick();
            }

            pick.Roll(lootLocationData);
        }
    }
}
