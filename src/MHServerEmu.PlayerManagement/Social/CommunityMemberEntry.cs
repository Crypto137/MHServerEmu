using Gazillion;
using MHServerEmu.Core.System.Time;

namespace MHServerEmu.PlayerManagement.Social
{
    public class CommunityMemberEntry
    {
        private CommunityMemberBroadcast.Builder _broadcastBuilder = CommunityMemberBroadcast.CreateBuilder();
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        private CommunityMemberAvatarSlot.Builder _avatarSlotBuilder = CommunityMemberAvatarSlot.CreateBuilder();
#endif

        private bool _hasUpToDateBroadcast = false;
        private CommunityMemberBroadcast _cachedBroadcast = null;

        public ulong PlayerDbId { get => _broadcastBuilder.MemberPlayerDbId; }

        public CommunityMemberEntry(ulong playerDbId, string currentPlayerName)
        {
            _broadcastBuilder.SetMemberPlayerDbId(playerDbId);
            _broadcastBuilder.SetCurrentPlayerName(currentPlayerName);

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            // Need to have a valid level for members to show up in the guild tab (WHY???)
            _avatarSlotBuilder.SetLevel(1);
#else
            _broadcastBuilder.SetCurrentCharacterLevel(1);  // not sure if this is needed for 1.48
#endif
        }

        public CommunityMemberBroadcast GetBroadcast()
        {
            if (_hasUpToDateBroadcast == false)
            {
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
                _broadcastBuilder.ClearSlots();
                _broadcastBuilder.AddSlots(_avatarSlotBuilder);
#endif
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

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        public bool SetCurrentDifficultyRefId(ulong currentDifficultyRefId)
        {
            if (_broadcastBuilder.CurrentDifficultyRefId == currentDifficultyRefId)
                return false;

            _broadcastBuilder.SetCurrentDifficultyRefId(currentDifficultyRefId);
            _hasUpToDateBroadcast = false;
            return true;
        }
#endif

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        public bool SetAvatarRefId(ulong avatarRefId)
        {
            if (_avatarSlotBuilder.AvatarRefId == avatarRefId)
                return false;

            _avatarSlotBuilder.SetAvatarRefId(avatarRefId);
            _hasUpToDateBroadcast = false;
            return true;
        }
#else
        public bool SetAvatarRefId(ulong avatarRefId)
        {
            if (_broadcastBuilder.CurrentAvatarRefId == avatarRefId)
                return false;

            _broadcastBuilder.SetCurrentAvatarRefId(avatarRefId);
            _hasUpToDateBroadcast = false;
            return true;
        }
#endif

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        public bool SetCostumeRefId(ulong costumeRefId)
        {
            if (_avatarSlotBuilder.CostumeRefId == costumeRefId)
                return false;

            _avatarSlotBuilder.SetCostumeRefId(costumeRefId);
            _hasUpToDateBroadcast = false;
            return true;
        }
#else
        public bool SetCostumeRefId(ulong costumeRefId)
        {
            if (_broadcastBuilder.CurrentCostumeRefId == costumeRefId)
                return false;

            _broadcastBuilder.SetCurrentCostumeRefId(costumeRefId);
            _hasUpToDateBroadcast = false;
            return true;
        }
#endif

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        public bool SetLevel(uint level)
        {
            if (_avatarSlotBuilder.Level == level)
                return false;

            _avatarSlotBuilder.SetLevel(level);
            _hasUpToDateBroadcast = false;
            return true;
        }
#else
        public bool SetLevel(uint level)
        {
            if (_broadcastBuilder.CurrentCharacterLevel == level)
                return false;

            _broadcastBuilder.SetCurrentCharacterLevel(level);
            _hasUpToDateBroadcast = false;
            return true;
        }
#endif

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        public bool SetPrestigeLevel(uint prestigeLevel)
        {
            if (_avatarSlotBuilder.PrestigeLevel == prestigeLevel)
                return false;

            _avatarSlotBuilder.SetPrestigeLevel(prestigeLevel);
            _hasUpToDateBroadcast = false;
            return true;
        }
#else
        public bool SetPrestigeLevel(uint prestigeLevel)
        {
            if (_broadcastBuilder.CurrentPrestigeLevel == prestigeLevel)
                return false;

            _broadcastBuilder.SetCurrentPrestigeLevel(prestigeLevel);
            _hasUpToDateBroadcast = false;
            return true;
        }
#endif

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
