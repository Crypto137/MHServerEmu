using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Navi
{
    public class ContentFlagCounts
    {
        public sbyte AddWalk { get; set; }
        public sbyte RemoveWalk { get; set; }
        public sbyte AddFly { get; set; }
        public sbyte RemoveFly { get; set; }
        public sbyte AddPower { get; set; }
        public sbyte RemovePower { get; set; }
        public sbyte AddSight { get; set; }
        public sbyte RemoveSight { get; set; }

        public static int Count { get; } = 8;

        public sbyte this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return AddWalk;
                    case 1: return RemoveWalk;
                    case 2: return AddFly;
                    case 3: return RemoveFly;
                    case 4: return AddPower;
                    case 5: return RemovePower;
                    case 6: return AddSight;
                    case 7: return RemoveSight;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
            set
            {
                switch (index)
                {
                    case 0: AddWalk = value; break;
                    case 1: RemoveWalk = value; break;
                    case 2: AddFly = value; break;
                    case 3: RemoveFly = value; break;
                    case 4: AddPower = value; break;
                    case 5: RemovePower = value; break;
                    case 6: AddSight = value; break;
                    case 7: RemoveSight = value; break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }

        public void Set(ContentFlagCounts other)
        {
            AddWalk = other.AddWalk;
            RemoveWalk = other.RemoveWalk;
            AddFly = other.AddFly;
            RemoveFly = other.RemoveFly;
            AddPower = other.AddPower;
            RemovePower = other.RemovePower;
            AddSight = other.AddSight;
            RemoveSight = other.RemoveSight;
        }
    }

    public class NaviEdgePathingFlags
    {
        public readonly ContentFlagCounts[] ContentFlagCounts;

        public NaviEdgePathingFlags()
        {
            ContentFlagCounts = new ContentFlagCounts[2];
            // Clear();
        }

        public NaviEdgePathingFlags(NaviContentFlags[] flags0, NaviContentFlags[] flags1)
        {
            ContentFlagCounts = new ContentFlagCounts[2];
            NaviContentFlags flag0 = NaviContentFlags.None;
            NaviContentFlags flag1 = NaviContentFlags.None;
            foreach (var flag in flags0) flag0 |= flag;
            foreach (var flag in flags1) flag1 |= flag;
            SetContentFlags(flag0, flag1);
        }

        public void SetContentFlags(NaviContentFlags flag0, NaviContentFlags flag1)
        {
            for (int flagIndex = 0; flagIndex < Navi.ContentFlagCounts.Count; flagIndex++)
            {
                ContentFlagCounts[0][flagIndex] = (sbyte)(((int)flag0 >> flagIndex) & 1);
                ContentFlagCounts[1][flagIndex] = (sbyte)(((int)flag1 >> flagIndex) & 1);
            }
        }

        public void Clear()
        {
            for (int flagIndex = 0; flagIndex < Navi.ContentFlagCounts.Count; flagIndex++)
            {
                ContentFlagCounts[0][flagIndex] = 0;
                ContentFlagCounts[1][flagIndex] = 0;
            }
        }

        public void Clear(int side)
        {
            for (int flagIndex = 0; flagIndex < Navi.ContentFlagCounts.Count; flagIndex++)
                ContentFlagCounts[side][flagIndex] = 0;
        }

        public NaviContentFlags GetContentFlagsForSide(int side)
        {
            NaviContentFlags contentFlags = NaviContentFlags.None;
            for (int flagIndex = 0; flagIndex < Navi.ContentFlagCounts.Count; flagIndex++)
                if (ContentFlagCounts[side][flagIndex] > 0)
                    contentFlags |= (NaviContentFlags)(1 << flagIndex);
            return contentFlags;
        }

        public void Merge(NaviEdgePathingFlags other, bool flipEdgePathFlags)
        {
            int side0 = flipEdgePathFlags ? 0 : 1;
            int side1 = flipEdgePathFlags ? 1 : 0;
            for (int flagIndex = 0; flagIndex < Navi.ContentFlagCounts.Count; flagIndex++)
            {
                ContentFlagCounts[0][flagIndex] += other.ContentFlagCounts[side0][flagIndex];
                ContentFlagCounts[1][flagIndex] += other.ContentFlagCounts[side1][flagIndex];
            }
        }
    }
}
