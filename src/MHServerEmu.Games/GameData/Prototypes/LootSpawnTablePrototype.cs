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

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override void Roll(LootLocationData lootLocationData)
        {
            base.Roll(lootLocationData);

            if (Choices.IsNullOrEmpty())
            {
                Logger.Warn($"Roll(): LootSpawnTable has no Choices! {this}");
                return;
            }

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

                if (possibleNodes.Empty())
                {
                    Logger.Warn($"Roll(): No LootNodePrototypes to pick from! LootSpawnTable: {this}");
                    return;
                }

                pick = possibleNodes.Pick();
            }

            pick.Roll(lootLocationData);
        }
    }
}
