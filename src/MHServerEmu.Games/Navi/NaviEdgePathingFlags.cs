using System.Runtime.InteropServices;

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
        BlackOutZone = 1 << 5,
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
        public bool CanBypassCheck(); // Inverted return
        public bool PathingFlagsCheck(PathFlags pathingFlags);
    }

    public readonly struct DefaultContainsPathFlagsCheck : IContainsPathFlagsCheck
    {
        private readonly PathFlags _pathFlags;

        public DefaultContainsPathFlagsCheck(PathFlags pathFlags)
        {
            _pathFlags = pathFlags;
        }

        public bool CanBypassCheck() 
        {
            return _pathFlags != PathFlags.None;
        }

        public bool PathingFlagsCheck(PathFlags pathingFlags)
        {
            return pathingFlags.HasFlag(_pathFlags);
        }
    }

    public readonly struct WalkPathFlagsCheck : IContainsPathFlagsCheck
    {
        public bool CanBypassCheck() => true;
        public bool PathingFlagsCheck(PathFlags pathingFlags) => pathingFlags.HasFlag(PathFlags.Walk);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ContentFlagCounts
    {
        public const int Count = 8;

        public int AddWalk;
        public int RemoveWalk;
        public int AddFly;
        public int RemoveFly;
        public int AddPower;
        public int RemovePower;
        public int AddSight;
        public int RemoveSight;
        
        public ContentFlagCounts()
        {
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

        public ulong GetHash64()
        {
            ulong hash = 14695981039346656037;

            hash = (hash ^ (uint)AddWalk) * 1099511628211;
            hash = (hash ^ (uint)RemoveWalk) * 1099511628211;
            hash = (hash ^ (uint)AddFly) * 1099511628211;
            hash = (hash ^ (uint)RemoveFly) * 1099511628211;
            hash = (hash ^ (uint)AddPower) * 1099511628211;
            hash = (hash ^ (uint)RemovePower) * 1099511628211;
            hash = (hash ^ (uint)AddSight) * 1099511628211;
            hash = (hash ^ (uint)RemoveSight) * 1099511628211;

            return hash;
        }

        public int this[int index] { get => AsSpan()[index]; set => AsSpan()[index] = value; }

        public Span<int> AsSpan()
        {
            // Do some MemoryMarshal hackery to represent this struct as an int span
            return MemoryMarshal.CreateSpan(ref AddWalk, Count);
        }

        public void Clear()
        {
            AsSpan().Clear();
        }

        public NaviContentFlags ToContentFlags()
        {
            NaviContentFlags contentFlags = NaviContentFlags.None;
            for (int flagIndex = 0; flagIndex < Count; flagIndex++)
                if (this[flagIndex] > 0)
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
        public ContentFlagCounts[] ContentFlagCounts = new ContentFlagCounts[2];

        public NaviEdgePathingFlags()
        {
        }

        public NaviEdgePathingFlags(NaviContentFlags[] flags0, NaviContentFlags[] flags1)
        {
            NaviContentFlags flag0 = NaviContentFlags.None;
            NaviContentFlags flag1 = NaviContentFlags.None;
            foreach (var flag in flags0) flag0 |= flag;
            foreach (var flag in flags1) flag1 |= flag;
            SetContentFlags(flag0, flag1);
        }

        public NaviEdgePathingFlags(NaviEdgePathingFlags pathingFlags)
        {
            if (pathingFlags != null)
            {
               ContentFlagCounts[0] = pathingFlags.ContentFlagCounts[0];
               ContentFlagCounts[1] = pathingFlags.ContentFlagCounts[1];
            }
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
            return ContentFlagCounts[side].ToContentFlags();
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

        public ulong GetHash64()
        {
            ulong hash = 14695981039346656037;
            hash = (hash ^ ContentFlagCounts[0].GetHash64()) * 1099511628211;
            hash = hash ^ ContentFlagCounts[1].GetHash64();
            return hash;
        }
    }
}
