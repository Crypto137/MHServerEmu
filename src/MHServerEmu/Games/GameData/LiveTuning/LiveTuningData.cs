using Gazillion;
using MHServerEmu.Common.Helpers;
using MHServerEmu.Common.Logging;

namespace MHServerEmu.Games.GameData.LiveTuning
{
    public class LiveTuningData
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly List<LiveTuningSetting> _settingList;

        public int Count { get => _settingList.Count; }

        /// <summary>
        /// Initializes a new empty LiveTuningData instance.
        /// </summary>
        public LiveTuningData()
        {
            _settingList = new();
        }

        /// <summary>
        /// Initializes a new LiveTuningData instance from the specified JSON file.
        /// </summary>
        public LiveTuningData(string jsonPath)
        {
            _settingList = FileHelper.DeserializeJson<List<LiveTuningSetting>>(jsonPath);
            foreach (var setting in _settingList)
                Logger.Trace(setting.ToString());
        }
        
        /// <summary>
        /// Generates an update message from the current live tuning data.
        /// </summary>
        public NetMessageLiveTuningUpdate ToNetMessageLiveTuningUpdate()
        {
            return NetMessageLiveTuningUpdate.CreateBuilder()
                .AddRangeTuningTypeKeyValueSettings(_settingList.Select(setting => setting.ToNetStructProtoEnumValue()))
                .Build();
        }

        public void PrintTuningVars()
        {
            foreach (LiveTuningSetting setting in _settingList)
                Logger.Trace(setting.ToString());
        }
    }
}
