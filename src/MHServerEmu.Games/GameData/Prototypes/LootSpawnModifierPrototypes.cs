using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Loot;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class LootLocationModifierPrototype : Prototype
    {
        //---

        public virtual void Apply(LootLocationData lootLocationData)
        {
        }
    }

    public class LootSearchRadiusPrototype : LootLocationModifierPrototype
    {
        public float MinRadius { get; protected set; }
        public float MaxRadius { get; protected set; }

        //---

        public override void Apply(LootLocationData lootLocationData)
        {
            base.Apply(lootLocationData);

            lootLocationData.MinRadius = MinRadius;
            lootLocationData.MaxRadius = MaxRadius;
        }
    }

    public class LootBoundsOverridePrototype : LootLocationModifierPrototype
    {
        public float Radius { get; protected set; }

        //--

        public override void Apply(LootLocationData lootLocationData)
        {
            base.Apply(lootLocationData);

            lootLocationData.Bounds.Radius = Radius;
        }
    }

    public class LootLocationOffsetPrototype : LootLocationModifierPrototype
    {
        public float Offset { get; protected set; }

        //---

        public override void Apply(LootLocationData lootLocationData)
        {
            base.Apply(lootLocationData);

            WorldEntity sourceEntity = lootLocationData.SourceEntity;
            if (sourceEntity == null || sourceEntity.IsInWorld == false)
                return;

            Vector3 offset = lootLocationData.Position - sourceEntity.RegionLocation.Position;
            if (Vector3.IsNearZero2D(offset))
                lootLocationData.Offset = Vector3.Zero;
            else
                lootLocationData.Offset = Vector3.Normalize(offset) * Offset;
        }
    }

    public class DropInPlacePrototype : LootLocationModifierPrototype
    {
        public bool Check { get; protected set; }

        public override void Apply(LootLocationData lootLocationData)
        {
            base.Apply(lootLocationData);

            lootLocationData.DropInPlace = Check;
        }
    }
}
