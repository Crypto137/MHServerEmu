using System.Text;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Social.Communities
{
    public enum CommunityMemberOnlineStatus
    {
        Invalid,
        Online,
        Offline
    }

    /// <summary>
    /// Flags that are set when processing <see cref="CommunityMemberBroadcast"/>.
    /// </summary>
    [Flags]
    public enum CommunityMemberUpdateOptionBits
    {
        None            = 0,
        Flag0           = 1 << 0,
        Flag1           = 1 << 1,
        RegionRef       = 1 << 2,
        AvatarRef       = 1 << 3,
        CostumeRef      = 1 << 4,
        Level           = 1 << 5,
        IsOnline        = 1 << 6,
        Flag7           = 1 << 7,
        Name            = 1 << 8,
        PrestigeLevel   = 1 << 9,
        LastLogoutTime  = 1 << 10,
        DifficultyRef   = 1 << 11,
        SecondaryPlayer = 1 << 12
    }

    public class CommunityMember
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private long _lastLogoutTimeAsFileTimeUtc = 0;
        private AvatarSlotInfo[] _slots = new AvatarSlotInfo[0];
        private string _playerName = string.Empty;
        private string _secondaryPlayerName = string.Empty;
        private ulong[] _consoleAccountIds = new ulong[(int)PlayerAvatarIndex.Count];

        public Community Community { get; }

        public ulong DbId { get; private set; }

        public PrototypeId RegionRef { get; private set; }
        public PrototypeId DifficultyRef { get; private set; }
        public CommunityMemberOnlineStatus IsOnline { get; private set; }
        public int[] ArchiveCircleIds { get; set; }

        public int NumCircles { get => ArchiveCircleIds.Length; }

        public CommunityMember(Community community, ulong playerDbId, string playerName)
        {
            Community = community;
            DbId = playerDbId;
            _playerName = playerName;
        }

        public void Decode(CodedInputStream stream)
        {
            RegionRef = stream.ReadPrototypeEnum<Prototype>();
            DifficultyRef = stream.ReadPrototypeEnum<Prototype>();

            byte numSlots = stream.ReadRawByte();
            Array.Resize(ref _slots, numSlots);
            for (byte i = 0; i < numSlots; i++)
            {
                PrototypeId avatarRef = stream.ReadPrototypeEnum<Prototype>();
                PrototypeId costumeRef = stream.ReadPrototypeEnum<Prototype>();
                int avatarLevel = stream.ReadRawInt32();
                int prestigeLevel = stream.ReadRawInt32();

                _slots[i] = new(avatarRef, costumeRef, avatarLevel, prestigeLevel);
            }

            IsOnline = (CommunityMemberOnlineStatus)stream.ReadRawInt32();

            _playerName = stream.ReadRawString();
            _secondaryPlayerName = stream.ReadRawString();
            _consoleAccountIds[0] = stream.ReadRawVarint64();
            _consoleAccountIds[1] = stream.ReadRawVarint64();

            ArchiveCircleIds = new int[stream.ReadRawInt32()];
            for (int i = 0; i < ArchiveCircleIds.Length; i++)
                ArchiveCircleIds[i] = stream.ReadRawInt32();
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WritePrototypeEnum<Prototype>(RegionRef);
            stream.WritePrototypeEnum<Prototype>(DifficultyRef);

            stream.WriteRawByte((byte)_slots.Length);
            foreach (AvatarSlotInfo slot in _slots)
            {
                stream.WritePrototypeEnum<Prototype>(slot.AvatarRef);
                stream.WritePrototypeEnum<Prototype>(slot.CostumeRef);
                stream.WriteRawInt32(slot.Level);
                stream.WriteRawInt32(slot.PrestigeLevel);
            }

            stream.WriteRawInt32((int)IsOnline);
            stream.WriteRawString(_playerName);
            stream.WriteRawString(_secondaryPlayerName);
            stream.WriteRawVarint64(_consoleAccountIds[0]);
            stream.WriteRawVarint64(_consoleAccountIds[1]);

            stream.WriteRawInt32(ArchiveCircleIds.Length);
            foreach (int circleId in ArchiveCircleIds)
                stream.WriteRawInt32(circleId);
        }

        public string GetName(PlayerAvatarIndex avatarIndex = PlayerAvatarIndex.Primary)
        {
            if (avatarIndex == PlayerAvatarIndex.Secondary)
                return _secondaryPlayerName;

            return _playerName;
        }

        public void SetName(string name, PlayerAvatarIndex avatarIndex = PlayerAvatarIndex.Primary)
        {
            if (avatarIndex == PlayerAvatarIndex.Secondary)
                _secondaryPlayerName = name;
            else
                _playerName = name;
        }

        public ulong GetConsoleAccountId(PlayerAvatarIndex avatarIndex = PlayerAvatarIndex.Primary)
        {
            if ((avatarIndex >= PlayerAvatarIndex.Primary && avatarIndex < PlayerAvatarIndex.Count) == false)
                return Logger.WarnReturn(0ul, "GetConsoleAccountId(): avatarIndex out of range");

            return _consoleAccountIds[(int)avatarIndex];
        }

        public bool SetConsoleAccountId(ulong consoleAccountId, PlayerAvatarIndex avatarIndex = PlayerAvatarIndex.Primary)
        {
            if ((avatarIndex >= PlayerAvatarIndex.Primary && avatarIndex < PlayerAvatarIndex.Count) == false)
                return Logger.WarnReturn(false, "SetConsoleAccountId(): avatarIndex out of range");

            _consoleAccountIds[(int)avatarIndex] = consoleAccountId;
            return true;
        }

        public AvatarSlotInfo GetAvatarSlotInfo(PlayerAvatarIndex avatarIndex = PlayerAvatarIndex.Primary)
        {
            int index = (int)avatarIndex;

            if (index >= 0 && index < _slots.Length)
                return _slots[index];

            return null;
        }

        public bool ShouldArchiveTo(/* Archive archive */)
        {
            // iterate community circles to determine this
            return true;
        }

        public CommunityMemberUpdateOptionBits ReceiveBroadcast(CommunityMemberBroadcast broadcast)
        {
            CommunityMemberUpdateOptionBits updateOptionBits = CommunityMemberUpdateOptionBits.None;

            if (broadcast.HasCurrentRegionRefId)
            {
                PrototypeId newRegionRef = (PrototypeId)broadcast.CurrentRegionRefId;
                PrototypeId oldRegionRef = UpdateRegionRef(newRegionRef);

                if (newRegionRef != oldRegionRef)
                    updateOptionBits |= CommunityMemberUpdateOptionBits.RegionRef;
            }

            if (broadcast.HasCurrentDifficultyRefId)
            {
                PrototypeId newDifficultyRef = (PrototypeId)broadcast.CurrentDifficultyRefId;
                PrototypeId oldAvatarRef = UpdateDifficultyRef(newDifficultyRef);

                if (newDifficultyRef != oldAvatarRef)
                    updateOptionBits |= CommunityMemberUpdateOptionBits.DifficultyRef;
            }

            if (broadcast.HasIsOnline)
            {
                CommunityMemberOnlineStatus oldIsOnline = IsOnline;
                IsOnline = broadcast.IsOnline == 1 ? CommunityMemberOnlineStatus.Online : CommunityMemberOnlineStatus.Offline;

                if (oldIsOnline != CommunityMemberOnlineStatus.Online && IsOnline == CommunityMemberOnlineStatus.Online)
                    updateOptionBits |= CommunityMemberUpdateOptionBits.IsOnline;
            }

            if (broadcast.HasLastLogoutTimeAsFileTimeUtc)
            {
                long oldLastLogoutTime = _lastLogoutTimeAsFileTimeUtc;
                _lastLogoutTimeAsFileTimeUtc = broadcast.LastLogoutTimeAsFileTimeUtc;

                if (oldLastLogoutTime != _lastLogoutTimeAsFileTimeUtc)
                    updateOptionBits |= CommunityMemberUpdateOptionBits.LastLogoutTime;
            }

            if (broadcast.SlotsCount > 0)
            {
                // Number of avatars changed
                while (_slots.Length > broadcast.SlotsCount)
                    updateOptionBits |= CommunityMemberUpdateOptionBits.AvatarRef;

                Array.Resize(ref _slots, broadcast.SlotsCount);

                for (int i = 0; i < broadcast.SlotsCount; i++)
                {
                    // Create a new slot if needed
                    if (_slots.ElementAtOrDefault(i) == null)
                        _slots[i] = new();

                    // Get slot info from the broadcast
                    var slot = broadcast.SlotsList[i];

                    if (slot.HasAvatarRefId)
                    {
                        PrototypeId oldAvatarRef = _slots[i].AvatarRef;
                        PrototypeId newAvatarRef = (PrototypeId)slot.AvatarRefId;

                        if (oldAvatarRef != newAvatarRef)
                        {
                            _slots[i].AvatarRef = newAvatarRef;
                            updateOptionBits |= CommunityMemberUpdateOptionBits.AvatarRef;
                        }
                    }

                    if (slot.HasCostumeRefId)
                    {
                        PrototypeId oldCostumeRef = _slots[i].CostumeRef;
                        PrototypeId newCostumeRef = (PrototypeId)slot.CostumeRefId;

                        if (oldCostumeRef != newCostumeRef)
                        {
                            _slots[i].CostumeRef = newCostumeRef;
                            updateOptionBits |= CommunityMemberUpdateOptionBits.CostumeRef;
                        }
                    }

                    if (slot.HasLevel)
                    {
                        if (_slots[i].Level != slot.Level)
                        {
                            _slots[i].Level = (int)slot.Level;
                            updateOptionBits |= CommunityMemberUpdateOptionBits.Level;
                        }
                    }

                    if (slot.HasPrestigeLevel)
                    {
                        if (_slots[i].PrestigeLevel != slot.PrestigeLevel)
                        {
                            _slots[i].PrestigeLevel = (int)slot.PrestigeLevel;
                            updateOptionBits |= CommunityMemberUpdateOptionBits.PrestigeLevel;
                        }
                    }

                    // slot.OnlineId is ignored for some reason
                    if (slot.HasOnlineId)
                        Logger.Warn($"ReceiveBroadcast(): HasOnlineId {slot.OnlineId}");
                }
            }

            if (broadcast.HasCurrentPlayerName)
            {
                if (_playerName != broadcast.CurrentPlayerName)
                {
                    _playerName = broadcast.CurrentPlayerName;
                    updateOptionBits |= CommunityMemberUpdateOptionBits.Name;
                }
            }

            if (broadcast.HasSecondaryPlayerName)
            {
                if (_secondaryPlayerName != broadcast.SecondaryPlayerName)
                {
                    _secondaryPlayerName = broadcast.SecondaryPlayerName;
                    updateOptionBits |= CommunityMemberUpdateOptionBits.SecondaryPlayer;
                }
            }

            if (broadcast.HasSecondaryConsoleAccountId)
            {
                if (GetConsoleAccountId(PlayerAvatarIndex.Secondary) != broadcast.ConsoleAccountId)
                {
                    SetConsoleAccountId(broadcast.SecondaryConsoleAccountId, PlayerAvatarIndex.Secondary);
                    updateOptionBits |= CommunityMemberUpdateOptionBits.SecondaryPlayer;
                }
            }

            // iterate and update circles here

            return updateOptionBits;
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            sb.AppendLine($"{nameof(_playerName)}: {_playerName}");
            sb.AppendLine($"{nameof(DbId)}: 0x{DbId:X}");
            sb.AppendLine($"{nameof(RegionRef)}: {GameDatabase.GetPrototypeName(RegionRef)}");
            sb.AppendLine($"{nameof(DifficultyRef)}: {GameDatabase.GetPrototypeName(DifficultyRef)}");

            for (int i = 0; i < _slots.Length; i++)
                sb.AppendLine($"Slot{i}: {_slots[i]}");

            sb.AppendLine($"{nameof(IsOnline)}: {IsOnline}");

            sb.AppendLine($"{nameof(_secondaryPlayerName)}: {_secondaryPlayerName}");
            sb.AppendLine($"{nameof(_consoleAccountIds)}[0]: {_consoleAccountIds[0]}");
            sb.AppendLine($"{nameof(_consoleAccountIds)}[1]: {_consoleAccountIds[1]}");

            for (int i = 0; i < ArchiveCircleIds.Length; i++)
                sb.AppendLine($"ArchiveCircleId{i}: {ArchiveCircleIds[i]}");

            return sb.ToString();
        }

        /// <summary>
        /// Updates the current region ref and returns the old one.
        /// </summary>
        private PrototypeId UpdateRegionRef(PrototypeId regionRef)
        {
            PrototypeId oldRef = RegionRef;
            RegionRef = regionRef;
            return oldRef;
        }

        /// <summary>
        /// Updates the current difficulty ref and returns the old one.
        /// </summary>
        private PrototypeId UpdateDifficultyRef(PrototypeId difficultyRef)
        {
            PrototypeId oldRef = DifficultyRef;
            DifficultyRef = difficultyRef;
            return oldRef;
        }
    }
}
