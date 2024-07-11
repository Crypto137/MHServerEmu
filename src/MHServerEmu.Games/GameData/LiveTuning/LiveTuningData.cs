using Gazillion;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.GameData.LiveTuning
{
    public class LiveTuningData
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public int ChangeNum { get; set; } = 0;

        public LiveTuningData()
        {

        }

        public void ResetToDefaults()
        {

        }

        public void Copy(LiveTuningData target)
        {

        }

        public void UpdateLiveTuningVar(PrototypeId tuningVarProtoRef, int tuningVarEnum, float tuningVarValue)
        {

        }

        public void UpdateLiveGlobalTuningVar(GlobalTuningVar tuningVarEnum, float tuningVarValue)
        {

        }

        public NetMessageLiveTuningUpdate GetLiveTuningUpdate()
        {
            return NetMessageLiveTuningUpdate.DefaultInstance;
        }
    }
}
