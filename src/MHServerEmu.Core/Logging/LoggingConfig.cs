using MHServerEmu.Core.Config;

namespace MHServerEmu.Core.Logging
{
    public class LoggingConfig : ConfigContainer
    {
        public bool EnableLogging { get; private set; } = true;
        public bool SynchronousMode { get; private set; } = false;
        public bool HideSensitiveInformation { get; private set; } = false;

        public bool EnableConsole { get; private set; } = true;
        public bool ConsoleIncludeTimestamps { get; private set; } = true;
        public LoggingLevel ConsoleMinLevel { get; private set; } = LoggingLevel.Trace;
        public LoggingLevel ConsoleMaxLevel { get; private set; } = LoggingLevel.Fatal;
        public string ConsoleChannels { get; private set; } = "+Default";

        public bool EnableFile { get; private set; } = false;
        public bool FileIncludeTimestamps { get; private set; } = true;
        public LoggingLevel FileMinLevel { get; private set; } = LoggingLevel.Trace;
        public LoggingLevel FileMaxLevel { get; private set; } = LoggingLevel.Fatal;
        public string FileChannels { get; private set; } = "+Default";
        public bool FileSplitOutput { get; private set; } = false;

        public LogTargetSettings GetConsoleSettings()
        {
            return new()
            {
                IncludeTimestamps = ConsoleIncludeTimestamps,
                MinimumLevel = ConsoleMinLevel,
                MaximumLevel = ConsoleMaxLevel,
                Channels = ParseChannels(ConsoleChannels)
            };
        }

        public LogTargetSettings GetFileSettings()
        {
            return new()
            {
                IncludeTimestamps = FileIncludeTimestamps,
                MinimumLevel = FileMinLevel,
                MaximumLevel = FileMaxLevel,
                Channels = ParseChannels(FileChannels)
            };
        }

        private LogChannels ParseChannels(string channelString)
        {
            LogChannels channels = LogChannels.None;

            string[] tokens = channelString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (string token in tokens)
            {
                ReadOnlySpan<char> tokenChannelName = token.AsSpan(1, token.Length - 1);
                if (Enum.TryParse(tokenChannelName, true, out LogChannels tokenChannel) == false)
                    continue;

                switch (token[0])
                {
                    case '+':
                        channels |= tokenChannel;
                        break;

                    case '-':
                        channels &= ~tokenChannel;
                        break;
                }
            }

            return channels;
        }
    }
}
