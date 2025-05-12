namespace MHServerEmu.Core.Logging
{
    /// <summary>
    /// <see cref="LoggingLevel"/> is used to filter log output by severity level.
    /// </summary>
    public enum LoggingLevel
    {
        Trace,
        Debug,
        Info,
        Warn,
        Error,
        Fatal
    }

    /// <summary>
    /// <see cref="LogChannels"/> are used to filter log output by source.
    /// </summary>
    [Flags]
    public enum LogChannels : ulong
    {
        None,
        General     = 1ul << 0,

        // Add channels here to enable them by default
        Default     = General,
        All         = unchecked((ulong)-1L)
    }

    /// <summary>
    /// <see cref="LogCategory"/> is used to split log output.
    /// </summary>
    public enum LogCategory
    {
        Common,
        Chat,
        MTXStore,
        NumCategories
    }
}
