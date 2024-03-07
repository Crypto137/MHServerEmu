using MHServerEmu.Common.Logging;

namespace MHServerEmu.Games.Social.Communities
{
    // Seems like user type never ended up being implemented
    public enum CircleType
    {
        None,
        System,
        User
    }

    // We currenly use SystemCircle as circleId since there are no user circles anyway
    public enum SystemCircle
    {
        // The names below are written to serialization archives, do not change
        __None,
        __Friends,
        __Ignore,
        __Nearby,
        __Party,
        __Guild,
        NumCircles
    }

    /// <summary>
    /// Represents a category of <see cref="CommunityMember"/> instances.
    /// </summary>
    public class CommunityCircle
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private static readonly CommunityCirclePrototype[] Prototypes = new CommunityCirclePrototype[]
        {
            new(SystemCircle.__None,    false,  false,  false,  false,  false,  false,  false,  false, 0,   false,  CommunityCirclePrototypeFlags.None),
            new(SystemCircle.__Friends, true,   true,   false,  false,  false,  false,  false,  false, 96,  true,   CommunityCirclePrototypeFlags.Flag1),
            new(SystemCircle.__Ignore,  true,   true,   false,  true,   true,   false,  false,  false, 128, false,  CommunityCirclePrototypeFlags.None),
            new(SystemCircle.__Nearby,  false,  false,  false,  false,  true,   false,  false,  false, 0,   false,  CommunityCirclePrototypeFlags.Flag0),
            new(SystemCircle.__Party,   false,  true,   false,  false,  false,  false,  true,   false, 0,   false,  CommunityCirclePrototypeFlags.Flag1),
            new(SystemCircle.__Guild,   false,  false,  false,  false,  false,  false,  true,   false, 0,   false,  CommunityCirclePrototypeFlags.Flag2),
        };

        public Community Community { get; }
        public string Name { get; }
        public SystemCircle Id { get; }
        public CircleType Type { get; }

        public bool IsPersistent { get => GetPrototype().IsPersistent; }
        public bool IsMigrated { get => GetPrototype().IsMigrated; }
        public bool RestrictsIgnore { get => GetPrototype().RestrictsIgnore; }
        public bool NotifyOnline { get => GetPrototype().NotifyOnline; }

        /// <summary>
        /// Constructs a new <see cref="CommunityCircle"/> instance.
        /// </summary>
        public CommunityCircle(Community community, string name, SystemCircle id, CircleType type)
        {
            Community = community;
            Name = name;
            Id = id;
            Type = type;
        }

        public bool ShouldArchiveTo(/* Archive archive */)
        {
            // TODO: Archive::IsReplication(), Archive::IsPersistent(), CommunityCircle::IsPersistent(), Archive::IsMigration(), CommunityCircle:IsMigrated()
            return true;
        }

        public void OnMemberReceivedBroadcast(CommunityMember member, CommunityMemberUpdateOptionBits updateOptionBits)
        {
            // todo: send client updates when members change here
        }

        public override string ToString() => Name;

        /// <summary>
        /// Returns the <see cref="CommunityCirclePrototype"/> instance for this <see cref="CommunityCircle"/>/
        /// </summary>
        private CommunityCirclePrototype GetPrototype()
        {
            foreach (CommunityCirclePrototype prototype in Prototypes)
            {
                if (prototype.Id == Id)
                    return prototype;
            }

            Logger.Warn($"GetPrototype(): Prototype for id {Id} not found");
            return Prototypes[0];
        }
    }
}
