using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class SuperCellEntryPrototype : Prototype
    {
        public sbyte X { get; protected set; }
        public sbyte Y { get; protected set; }
        public AssetId Cell { get; protected set; }
        public AssetId[] Alts { get; protected set; }

        //---

        [DoNotCopy]
        public Point2 Offset { get => new(X, Y); }

        public PrototypeId PickCell(GRandom random, List<PrototypeId> list)
        {
            if (Alts.IsNullOrEmpty())
                return GameDatabase.GetDataRefByAsset(Cell);

            Picker<PrototypeId> picker = new(random);

            if (Cell != 0)
            {
                PrototypeId cellRef = GameDatabase.GetDataRefByAsset(Cell);
                if (cellRef != 0)
                    picker.Add(cellRef);
            }

            foreach (AssetId alt in Alts)
            {
                PrototypeId altRef = GameDatabase.GetDataRefByAsset(alt);
                if (!Verify.IsTrue(altRef != PrototypeId.Invalid))
                    continue;

                bool isUnique = true;
                foreach (PrototypeId item in list)
                {
                    if (altRef == item)
                    {
                        isUnique = false;
                        break;
                    }
                }

                if (isUnique)
                    picker.Add(altRef);
            }

            PrototypeId pickedCell = PrototypeId.Invalid;
            if (Verify.IsTrue(picker.Empty() == false))
                picker.Pick(out pickedCell);

            return pickedCell;
        }
    }

    public class SuperCellPrototype : Prototype
    {
        public SuperCellEntryPrototype[] Entries { get; protected set; }

        //---

        [DoNotCopy]
        public Point2 Max { get; private set; }

        public override void PostProcess()
        {
            base.PostProcess();

            Max = new(-1, -1);

            if (Entries.HasValue())
            {
                foreach (SuperCellEntryPrototype superCellEntry in Entries)
                {
                    if (superCellEntry != null)
                        Max = new(Math.Max(Max.X, superCellEntry.X), Math.Max(Max.Y, superCellEntry.Y));
                }
            }
        }

        public bool ContainsCell(PrototypeId cellRef)
        {
            if (Entries.HasValue())
            {
                foreach (SuperCellEntryPrototype entryProto in Entries)
                {
                    if (entryProto != null && GameDatabase.GetDataRefByAsset(entryProto.Cell) == cellRef)
                        return true;
                }
            }

            return false;
        }
    }
}
