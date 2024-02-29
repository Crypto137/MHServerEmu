
namespace MHServerEmu.Games.Dialog
{
    [Flags]
    public enum InteractionOptimizationFlags
    {
        Hint = 1 << 0,
        Flag1 = 1 << 1,
        Appearance = 1 << 2,
        Visibility = 1 << 3,
        ActionEntityTarget = 1 << 4,
        ConditionHotspot = 1 << 5,
        ConnectionTargetEnable = 1 << 6
    }

    [Flags]
    public enum InteractionMethod : ulong
    {
        None = 0,        
        Throw = 1L << 3,
        Use = 1L << 4,
        StoryWarp = 1L << 28,
    }

    [Flags]
    public enum InteractionFlags
    {
        None = 0,
    }

    [Flags]
    public enum MissionOptionTypeFlags
    {
        None = 0,
        ActivateCondition = 1 << 0,
        Skip = 1 << 1,
        SkipComplete = 1 << 2,
    }

    [Flags]
    public enum MissionStateFlags
    {
        Invalid = 1 << 0,
        Inactive = 1 << 1,
        Available = 1 << 2,
        Active = 1 << 3,
        Completed = 1 << 4,
        Failed = 1 << 5,
    }

    [Flags]
    public enum MissionObjectiveStateFlags
    {
        Invalid = 1 << 0,
        Available = 1 << 1,
        Active = 1 << 2,
        Completed = 1 << 3,
        Failed = 1 << 4,
        Skipped = 1 << 5,
    }
}
