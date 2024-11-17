using MHServerEmu.Games.Regions.Maps;

namespace MHServerEmu.Games.Network
{
    /// <summary>
    /// Contains player data that needs to persist on migration (going from region to region), but not when logging out.
    /// </summary>
    public class MigrationData
    {
        private readonly Dictionary<ulong, MapDiscoveryData> _mapDiscoveryDict = new();

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
