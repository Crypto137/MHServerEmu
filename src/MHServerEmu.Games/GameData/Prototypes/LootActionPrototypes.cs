using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Loot.Visitors;

namespace MHServerEmu.Games.GameData.Prototypes
{
    // LootActionPrototype is a wrapper for LootNodePrototype with additional selection logic

    public class LootActionPrototype : LootNodePrototype
    {
        public LootNodePrototype Target { get; protected set; }

        //---

        public override void Visit<T>(ref T visitor)
        {
            base.Visit(ref visitor);

            Target?.Visit(ref visitor);
        }

        protected LootRollResult SelectTarget(LootRollSettings settings, IItemResolver resolver)
        {
            if (Target == null)
                return LootRollResult.NoRoll;

            return Target.Select(settings, resolver);
        }
    }

    public class LootActionFirstTimePrototype : LootActionPrototype
    {
        public bool FirstTime { get; protected set; }

        //---

        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            if (resolver.Flags.HasFlag(LootResolverFlags.FirstTime) == false)
                return LootRollResult.NoRoll;

            return SelectTarget(settings, resolver);
        }
    }

    public class LootActionLoopOverAvatarsPrototype : LootActionPrototype
    {
        //---

        // NOTE: This is used only in two test tables in 1.52, but it's pretty easy to do,
        // so I'm implementing it in case we need it for other versions.
        // - Loot/Tables/Test/TestUniques41to100.prototype
        // - Loot/Tables/Test/TestUniquesAll.prototype

        private static readonly Logger Logger = LogManager.CreateLogger();

        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            LootRollResult result = LootRollResult.NoRoll;

            using LootRollSettings modifiedSettings = ObjectPoolManager.Instance.Get<LootRollSettings>();
            modifiedSettings.Set(settings);
            modifiedSettings.ForceUsable = true;

            foreach (PrototypeId avatarProtoRef in DataDirectory.Instance.IteratePrototypesInHierarchy<AvatarPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                AvatarPrototype avatarProto = avatarProtoRef.As<AvatarPrototype>();
                if (avatarProto == null)
                {
                    Logger.Warn("Roll(): avatarProto == null");
                    continue;
                }

                // NOTE: This will skip disabled heroes
                if (avatarProto.ShowInRosterIfLocked == false)
                    continue;

                if (avatarProto.IsLiveTuningEnabled() == false)
                    continue;

                modifiedSettings.UsableAvatar = avatarProto;
                result |= SelectTarget(modifiedSettings, resolver);
            }

            return result;
        }
    }
}
