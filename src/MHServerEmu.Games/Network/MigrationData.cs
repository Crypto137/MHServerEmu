using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Regions.Maps;

namespace MHServerEmu.Games.Network
{
    /// <summary>
    /// Contains player data that needs to persist on migration (going from region to region), but not when logging out.
    /// </summary>
    public class MigrationData
    {
        private readonly Dictionary<(PrototypeId, byte), ulong> _missionObjectivesData = new();
        private readonly Dictionary<ulong, MapDiscoveryData> _mapDiscoveryDict = new();

        public void MigrationObjectiveData((PrototypeId PrototypeDataRef, byte _prototypeIndex) key, bool isPacking, ref ulong activeRegionId)
        {
            if (isPacking == false)
                _missionObjectivesData.TryGetValue(key, out activeRegionId);
            else if (activeRegionId != 0)
                _missionObjectivesData[key] = activeRegionId;
        }

        public void ResetObjective()
        {
            _missionObjectivesData.Clear();
        }

        public void TransferMap(Dictionary<ulong, MapDiscoveryData> ioData, bool isPacking)
        {
            if (isPacking) 
            { 
                _mapDiscoveryDict.Clear();
                foreach (var kvp in ioData) _mapDiscoveryDict[kvp.Key] = kvp.Value; 
            } 
            else
            {
                ioData.Clear();
                foreach (var kvp in _mapDiscoveryDict) ioData[kvp.Key] = kvp.Value; 
            }
        }
    }
}
