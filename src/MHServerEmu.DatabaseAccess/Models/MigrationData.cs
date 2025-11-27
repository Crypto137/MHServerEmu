using Gazillion;

namespace MHServerEmu.DatabaseAccess.Models
{
    public class MigrationData
    {
        // Store everything here as ulong, PropertyCollection will sort it out game-side
        private readonly Dictionary<ulong, List<(ulong, ulong)>> _properties = new(32);

        public bool SkipNextUpdate { get; set; }

        public bool IsFirstLoad { get; set; } = true;

        public List<(ulong, ulong)> WorldView { get; } = new();
        public byte[] MatchQueueStatus { get; set; }
        public List<CommunityMemberBroadcast> CommunityStatus { get; } = new();

        public MigrationData() { }

        public List<(ulong, ulong)> GetOrCreatePropertyList(ulong entityDbId)
        {
            if (_properties.TryGetValue(entityDbId, out List<(ulong, ulong)> list) == false)
            {
                list = new();
                _properties.Add(entityDbId, list);
            }

            return list;
        }

        public void RemovePropertyList(ulong entityDbId)
        {
            _properties.Remove(entityDbId);
        }

        public void Reset()
        {
            SkipNextUpdate = false;

            IsFirstLoad = true;

            // Properties for summoned entities need to be migrated, and these have arbitrary runtime dbIds, so just clear everything.
            _properties.Clear();
            
            WorldView.Clear();
            MatchQueueStatus = null;
            CommunityStatus.Clear();
        }
    }
}
