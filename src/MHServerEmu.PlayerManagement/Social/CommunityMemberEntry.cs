using Gazillion;
using MHServerEmu.Core.System.Time;

namespace MHServerEmu.PlayerManagement.Social
{
    public class CommunityMemberEntry
    {
        private CommunityMemberBroadcast.Builder _broadcastBuilder = CommunityMemberBroadcast.CreateBuilder();
        private CommunityMemberAvatarSlot.Builder _avatarSlotBuilder = CommunityMemberAvatarSlot.CreateBuilder();

        private bool _hasUpToDateBroadcast = false;
        private CommunityMemberBroadcast _cachedBroadcast = null;

        public ulong PlayerDbId { get => _broadcastBuilder.MemberPlayerDbId; }

        public CommunityMemberEntry(ulong playerDbId, string currentPlayerName)
        {
            _broadcastBuilder.SetMemberPlayerDbId(playerDbId);
            _broadcastBuilder.SetCurrentPlayerName(currentPlayerName);
        }

        public CommunityMemberBroadcast GetBroadcast()
        {
            if (_hasUpToDateBroadcast == false)
            {
                _broadcastBuilder.ClearSlots();
                _broadcastBuilder.AddSlots(_avatarSlotBuilder);
                _cachedBroadcast = _broadcastBuilder.Build();
                _hasUpToDateBroadcast = true;
            }

            return _cachedBroadcast;
        }

        public bool SetCurrentRegionRefId(ulong currentRegionRefId)
        {
            if (_broadcastBuilder.CurrentRegionRefId == currentRegionRefId)
                return false;

            _broadcastBuilder.SetCurrentRegionRefId(currentRegionRefId);
            _hasUpToDateBroadcast = false;
            return true;
        }

        public bool SetCurrentDifficultyRefId(ulong currentDifficultyRefId)
        {
            if (_broadcastBuilder.CurrentDifficultyRefId == currentDifficultyRefId)
                return false;

            _broadcastBuilder.SetCurrentDifficultyRefId(currentDifficultyRefId);
            _hasUpToDateBroadcast = false;
            return true;
        }

        public bool SetAvatarRefId(ulong avatarRefId)
        {
            if (_avatarSlotBuilder.AvatarRefId == avatarRefId)
                return false;

            _avatarSlotBuilder.SetAvatarRefId(avatarRefId);
            _hasUpToDateBroadcast = false;
            return true;
        }

        public bool SetCostumeRefId(ulong costumeRefId)
        {
            if (_avatarSlotBuilder.CostumeRefId == costumeRefId)
                return false;

            _avatarSlotBuilder.SetCostumeRefId(costumeRefId);
            _hasUpToDateBroadcast = false;
            return true;
        }

        public bool SetLevel(uint level)
        {
            if (_avatarSlotBuilder.Level == level)
                return false;

            _avatarSlotBuilder.SetLevel(level);
            _hasUpToDateBroadcast = false;
            return true;
        }

        public bool SetPrestigeLevel(uint prestigeLevel)
        {
            if (_avatarSlotBuilder.PrestigeLevel == prestigeLevel)
                return false;

            _avatarSlotBuilder.SetPrestigeLevel(prestigeLevel);
            _hasUpToDateBroadcast = false;
            return true;
        }

        public bool SetCurrentPlayerName(string currentPlayerName)
        {
            if (_broadcastBuilder.CurrentPlayerName.Equals(currentPlayerName, StringComparison.Ordinal))
                return false;

            _broadcastBuilder.SetCurrentPlayerName(currentPlayerName);
            _hasUpToDateBroadcast = false;
            return true;
        }

        public bool SetIsOnline(bool isOnline)
        {
            int isOnlineValue = isOnline ? 1 : 0;
            if (_broadcastBuilder.IsOnline == isOnlineValue)
                return false;

            _broadcastBuilder.SetIsOnline(isOnlineValue);
            _hasUpToDateBroadcast = false;
            return true;
        }

        public bool SetLastLogoutTime(TimeSpan lastLogoutTime)
        {
            long lastLogoutTimeAsFileTimeUtc = Clock.UnixTimeToFileTimeUtc(lastLogoutTime);
            if (_broadcastBuilder.LastLogoutTimeAsFileTimeUtc == lastLogoutTimeAsFileTimeUtc)
                return false;

            _broadcastBuilder.SetLastLogoutTimeAsFileTimeUtc(lastLogoutTimeAsFileTimeUtc);
            _hasUpToDateBroadcast = false;
            return true;
        }
    }
}
