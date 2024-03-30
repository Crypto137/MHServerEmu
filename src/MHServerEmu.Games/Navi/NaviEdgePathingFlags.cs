
namespace MHServerEmu.Games.Navi
{
    [Flags]
    public enum PathFlags
    {
        None = 0,
        Walk = 1 << 0,
        Fly = 1 << 1,
        Power = 1 << 2,
        Sight = 1 << 3,
        TallWalk = 1 << 4,
    }

    [Flags]
    public enum NaviContentFlags
    {
        None = 0,
        AddWalk = 1 << 0,
        RemoveWalk = 1 << 1,
        AddFly = 1 << 2,
        RemoveFly = 1 << 3,
        AddPower = 1 << 4,
        RemovePower = 1 << 5,
        AddSight = 1 << 6,
        RemoveSight = 1 << 7
    }
    public enum NaviContentTags
    {
        None = 0,
        OpaqueWall = 1,
        TransparentWall = 2,
        Blocking = 3,
        NoFly = 4,
        Walkable = 5,
        Obstacle = 6
    }

    public interface IContainsPathFlagsCheck
    {
        bool CanBypassCheck(); // Inverted return
        bool PathingFlagsCheck(PathFlags pathingFlags);
    }

    public class DefaultContainsPathFlagsCheck : IContainsPathFlagsCheck
    {
        public PathFlags PathFlags { get; set; }

        public DefaultContainsPathFlagsCheck(PathFlags pathFlags)
        {
            PathFlags = pathFlags;
        }

        public bool CanBypassCheck() 
        {
            return PathFlags != PathFlags.None;
        }

        public bool PathingFlagsCheck(PathFlags pathingFlags)
        {
            return pathingFlags.HasFlag(PathFlags);
        }
    }

    public class ContentFlagCounts // TODO: optimize it
    {
        public int AddWalk { get; set; }
        public int RemoveWalk { get; set; }
        public int AddFly { get; set; }
        public int RemoveFly { get; set; }
        public int AddPower { get; set; }
        public int RemovePower { get; set; }
        public int AddSight { get; set; }
        public int RemoveSight { get; set; }

        public static int Count { get; } = 8;      
        
        public ContentFlagCounts()
        {
        }        
        
        public ContentFlagCounts(ContentFlagCounts flagCounts)
        {
            Set(flagCounts);
        }

        public uint GetHash()
        {
            uint hash = 2166136261;

            hash = (hash ^ (uint)AddWalk) * 16777619;
            hash = (hash ^ (uint)RemoveWalk) * 16777619;
            hash = (hash ^ (uint)AddFly) * 16777619;
            hash = (hash ^ (uint)RemoveFly) * 16777619;
            hash = (hash ^ (uint)AddPower) * 16777619;
            hash = (hash ^ (uint)RemovePower) * 16777619;
            hash = (hash ^ (uint)AddSight) * 16777619;
            hash = (hash ^ (uint)RemoveSight) * 16777619;

            return hash;
        }

        public int this[int index]
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

        public void Clear()
        {
            AddWalk = 0;
            RemoveWalk = 0;
            AddFly = 0;
            RemoveFly = 0;
            AddPower = 0;
            RemovePower = 0;
            AddSight = 0;
            RemoveSight = 0;
        }

        public static NaviContentFlags ToContentFlags(ContentFlagCounts flagCounts)
        {
            NaviContentFlags contentFlags = NaviContentFlags.None;
            for (int flagIndex = 0; flagIndex < Count; flagIndex++)
                if (flagCounts[flagIndex] > 0)
                    contentFlags |= (NaviContentFlags)(1 << flagIndex);
            return contentFlags;
        }

        public override string ToString()
        {
            int[] array = new int[Count];
            for (int flagIndex = 0; flagIndex < Count; flagIndex++)
                array[flagIndex] = this[flagIndex];
            return string.Join(" ", array);
        }
    }

    public class ContentFlags
    {
        public static PathFlags ToPathFlags(NaviContentFlags contentFlags)
        {
            PathFlags pathFlags = 0;
            if (contentFlags.HasFlag(NaviContentFlags.AddWalk) && contentFlags.HasFlag(NaviContentFlags.RemoveWalk) == false)
                pathFlags |= PathFlags.Walk;
            if (contentFlags.HasFlag(NaviContentFlags.AddFly) && contentFlags.HasFlag(NaviContentFlags.RemoveFly) == false)
                pathFlags |= PathFlags.Fly;
            if (contentFlags.HasFlag(NaviContentFlags.AddPower) && contentFlags.HasFlag(NaviContentFlags.RemovePower) == false)
                pathFlags |= PathFlags.Power;
            if (contentFlags.HasFlag(NaviContentFlags.AddSight) && contentFlags.HasFlag(NaviContentFlags.RemoveSight) == false)
                pathFlags |= PathFlags.Sight;
            if (pathFlags.HasFlag(PathFlags.Walk | PathFlags.Fly))
                pathFlags |= PathFlags.TallWalk;

            return pathFlags;
        }
    }

    public class NaviEdgePathingFlags
    {
        public ContentFlagCounts[] ContentFlagCounts;

        public NaviEdgePathingFlags()
        {
            InitContentFlagCounts();
        }

        public NaviEdgePathingFlags(NaviContentFlags[] flags0, NaviContentFlags[] flags1)
        {
            InitContentFlagCounts();
            NaviContentFlags flag0 = NaviContentFlags.None;
            NaviContentFlags flag1 = NaviContentFlags.None;
            foreach (var flag in flags0) flag0 |= flag;
            foreach (var flag in flags1) flag1 |= flag;
            SetContentFlags(flag0, flag1);
        }

        public NaviEdgePathingFlags(NaviEdgePathingFlags pathingFlags)
        {
            InitContentFlagCounts();
            if (pathingFlags != null)
            {
               ContentFlagCounts[0].Set(pathingFlags.ContentFlagCounts[0]);
               ContentFlagCounts[1].Set(pathingFlags.ContentFlagCounts[1]);
            }
        }

        private void InitContentFlagCounts()
        {
            ContentFlagCounts = new ContentFlagCounts[2];
            ContentFlagCounts[0] = new();
            ContentFlagCounts[1] = new();
        }

        public void SetContentFlags(NaviContentFlags flag0, NaviContentFlags flag1)
        {
            for (int flagIndex = 0; flagIndex < Navi.ContentFlagCounts.Count; flagIndex++)
            {
                ContentFlagCounts[0][flagIndex] = ((int)flag0 >> flagIndex) & 1;
                ContentFlagCounts[1][flagIndex] = ((int)flag1 >> flagIndex) & 1;
            }
        }

        public void Clear()
        {
            ContentFlagCounts[0].Clear();
            ContentFlagCounts[1].Clear();
        }

        public void Clear(int side)
        {
            ContentFlagCounts[side].Clear();
        }

        public NaviContentFlags GetContentFlagsForSide(int side)
        {
            return Navi.ContentFlagCounts.ToContentFlags(ContentFlagCounts[side]);
        }

        public void Merge(NaviEdgePathingFlags other, bool flip)
        {
            int side0 = flip ? 1 : 0;
            int side1 = flip ? 0 : 1;
            for (int flagIndex = 0; flagIndex < Navi.ContentFlagCounts.Count; flagIndex++)
            {
                ContentFlagCounts[0][flagIndex] += other.ContentFlagCounts[side0][flagIndex];
                ContentFlagCounts[1][flagIndex] += other.ContentFlagCounts[side1][flagIndex];
            }
        }

        public override string ToString()
        {
            return $"[0][{ContentFlagCounts[0]}] [1][{ContentFlagCounts[1]}]";
        }

        public uint GetHash()
        {
            uint hash = 2166136261;
            hash = (hash ^ ContentFlagCounts[0].GetHash()) * 16777619;
            hash = hash ^ ContentFlagCounts[1].GetHash();
            return hash;
        }
    }
}
