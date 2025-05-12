using MHServerEmu.Core.Config;

namespace MHServerEmu.Games.GameData
{
    public class GameDataConfig : ConfigContainer
    {
        public bool LoadAllPrototypes { get; private set; } = false;
        public bool UseEquipmentSlotTableCache { get; private set; } = false;
        public bool EnablePatchManager { get; private set; } = true;
    }
}
