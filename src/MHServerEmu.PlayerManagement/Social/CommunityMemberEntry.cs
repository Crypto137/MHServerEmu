using Gazillion;

namespace MHServerEmu.PlayerManagement.Social
{
    public class CommunityMemberEntry
    {
        private CommunityMemberBroadcast.Builder _broadcastBuilder = CommunityMemberBroadcast.CreateBuilder();

        private bool _hasUpToDateBroadcast = false;
        private CommunityMemberBroadcast _cachedBroadcast = null;

        public ulong PlayerDbId { get => _broadcastBuilder.MemberPlayerDbId; }

        public CommunityMemberEntry(ulong playerDbId)
        {
            _broadcastBuilder.SetMemberPlayerDbId(playerDbId);
        }

        public CommunityMemberBroadcast GetBroadcast()
        {
            if (_hasUpToDateBroadcast == false)
            {
                _cachedBroadcast = _broadcastBuilder.Build();
                _hasUpToDateBroadcast = true;
            }

            return _cachedBroadcast;
        }

        public bool SetIsOnline(int isOnline)
        {
            if (isOnline == _broadcastBuilder.IsOnline)
                return false;

            _broadcastBuilder.SetIsOnline(isOnline);
            _hasUpToDateBroadcast = false;
            return true;
        }
    }
}
