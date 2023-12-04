using MHServerEmu.Common;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Generators.Prototypes
{

    public class SuperCellEntryPrototype : Prototype
    {
        public sbyte X;
        public sbyte Y;
        public ulong Cell;
        public ulong[] Alts;

        public Point2 Offset { get => new(X, Y); }
        public SuperCellEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SuperCellEntryPrototype), proto); }

        public ulong PickCell(GRandom random, List<ulong> list)
        {
            if (Alts == null)
            {
                return GameDatabase.GetDataRefByAsset(Cell);
            }
            else
            {
                Picker<ulong> picker = new(random);

                if (Cell != 0)
                {
                    ulong cellRef = GameDatabase.GetDataRefByAsset(Cell);
                    if (cellRef != 0) picker.Add(cellRef);
                }

                foreach (ulong alt in Alts)
                {
                    ulong altRef = GameDatabase.GetDataRefByAsset(alt);
                    if (altRef != 0)
                    {
                        bool isUnique = true;
                        foreach (ulong item in list)
                        {
                            if (altRef == item)
                            {
                                isUnique = false;
                                break;
                            }
                        }

                        if (isUnique) picker.Add(altRef);
                    }
                }

                ulong pickCell = 0;
                if (!picker.Empty())
                {
                    picker.Pick(out pickCell);
                }

                return pickCell;

            }
        }

    }

    public class SuperCellPrototype : Prototype
    {
        new public SuperCellEntryPrototype[] Entries;

        public Point2 Max;
        public SuperCellPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SuperCellPrototype), proto); }

        public override void PostProcess()
        {
            base.PostProcess();

            Max = new(-1, -1);

            if (Entries != null)
            {
                foreach (SuperCellEntryPrototype superCellEntry in Entries)
                {
                    if (superCellEntry != null)
                    {
                        Max.X = Math.Max(Max.X, superCellEntry.X);
                        Max.Y = Math.Max(Max.Y, superCellEntry.Y);
                    }
                }
            }
        }

        public bool ContainsCell(ulong cellRef)
        {
            if (Entries != null)
            {
                foreach (var entryProto in Entries)
                {
                    if (entryProto != null && GameDatabase.GetDataRefByAsset(entryProto.Cell) == cellRef)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

    }
}
