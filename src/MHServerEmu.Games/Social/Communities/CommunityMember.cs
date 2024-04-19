using System.Collections;
using System.Text;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
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

    public class CommunityMember : ISerialize
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private ulong _dbId;
        private PrototypeId _regionRef;
        private PrototypeId _difficultyRef;

        private long _lastLogoutTimeAsFileTimeUtc = 0;
        private AvatarSlotInfo[] _slots = Array.Empty<AvatarSlotInfo>();
        private CommunityMemberOnlineStatus _isOnline;
        private string _playerName = string.Empty;
        private string _secondaryPlayerName = string.Empty;
        private readonly BitArray _systemCircles = new((int)CircleId.NumCircles);

        private readonly ulong[] _consoleAccountIds = new ulong[(int)PlayerAvatarIndex.Count];

        public Community Community { get; }

        public ulong DbId { get => _dbId; }
        public PrototypeId RegionRef { get => _regionRef; }
        public PrototypeId DifficultyRef { get => _difficultyRef; }
        public CommunityMemberOnlineStatus IsOnline { get => _isOnline; }

        public CommunityMember(Community community, ulong playerDbId, string playerName)
        {
            Community = community;
            _dbId = playerDbId;
            _playerName = playerName;
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            // archive.IsPersistent == false
            success &= Serializer.Transfer(archive, ref _regionRef);
            success &= Serializer.Transfer(archive, ref _difficultyRef);

            byte numSlots = 0;
            if (archive.IsPacking)
            {
                if (_slots.Length >= byte.MaxValue)
                    return Logger.ErrorReturn(false, $"Serialize(): numSlots overflow {_slots.Length}");
                numSlots = (byte)_slots.Length;
            }

            success &= Serializer.Transfer(archive, ref numSlots);

            if (archive.IsUnpacking)
                Array.Resize(ref _slots, numSlots);

            for (int i = 0; i < numSlots; i++)
            {
                // Slight deviation from the client: we implemented ISerialize for AvatarSlotInfo to make this a bit cleaner
                if (_slots[i] == null)
                    _slots[i] = new();

                success &= Serializer.Transfer(archive, ref _slots[i]);
            }

            int isOnline = (int)_isOnline;
            success &= Serializer.Transfer(archive, ref isOnline);
            _isOnline = (CommunityMemberOnlineStatus)isOnline;

            success &= Serializer.Transfer(archive, ref _playerName);
            success &= Serializer.Transfer(archive, ref _secondaryPlayerName);
            success &= Serializer.Transfer(archive, ref _consoleAccountIds[0]);
            success &= Serializer.Transfer(archive, ref _consoleAccountIds[1]);

            int numCircles = 0;
            if (archive.IsPacking)
            {
                foreach (CommunityCircle circle in Community.IterateCircles(this))
                {
                    if (circle.ShouldArchiveTo(archive))
                        numCircles++;
                }
            }

            success &= Serializer.Transfer(archive, ref numCircles);

            if (archive.IsPacking)
            {
                foreach (CommunityCircle circle in Community.IterateCircles(this))
                {
                    if (circle.ShouldArchiveTo(archive) == false) continue;

                    int archiveCircleId = Community.CircleManager.GetArchiveCircleId(circle);
                    if (archiveCircleId == 1)
                        return Logger.ErrorReturn(false, $"Serialize(): Invalid archive circle id returned for circle in archive. circle={circle}");

                    success &= Serializer.Transfer(archive, ref archiveCircleId);
                }
            }
            else
            {
                for (int i = 0; i < numCircles; i++)
                {
                    int archiveCircleId = 0;
                    success &= Serializer.Transfer(archive, ref archiveCircleId);

                    CommunityCircle circle = Community.CircleManager.GetCircleByArchiveCircleId(archiveCircleId);
                    if (circle == null)
                        return Logger.ErrorReturn(false, $"Serialize(): Circle not found when reading member. archiveCircleId=0x{archiveCircleId:X}, member={this}, community={Community}");

                    SetBitForCircle(_systemCircles, circle, true);
                }
            }

            return success;
        }

        public bool Decode(CodedInputStream stream)
        {
            _regionRef = stream.ReadPrototypeRef<Prototype>();
            _difficultyRef = stream.ReadPrototypeRef<Prototype>();

            byte numSlots = stream.ReadRawByte();
            Array.Resize(ref _slots, numSlots);
            for (byte i = 0; i < numSlots; i++)
            {
                PrototypeId avatarRef = stream.ReadPrototypeRef<Prototype>();
                PrototypeId costumeRef = stream.ReadPrototypeRef<Prototype>();
                int avatarLevel = stream.ReadRawInt32();
                int prestigeLevel = stream.ReadRawInt32();

                _slots[i] = new(avatarRef, costumeRef, avatarLevel, prestigeLevel);
            }

            _isOnline = (CommunityMemberOnlineStatus)stream.ReadRawInt32();

            _playerName = stream.ReadRawString();
            _secondaryPlayerName = stream.ReadRawString();
            _consoleAccountIds[0] = stream.ReadRawVarint64();
            _consoleAccountIds[1] = stream.ReadRawVarint64();

            int numCircles = stream.ReadRawInt32();
            for (int i = 0; i < numCircles; i++)
            {
                int archiveCircleId = stream.ReadRawInt32();
                CommunityCircle circle = Community.CircleManager.GetCircleByArchiveCircleId(archiveCircleId);
                if (circle == null)
                    return Logger.ErrorReturn(false, $"Decode(): Circle not found when reading member. archiveCircleId=0x{archiveCircleId:X}, member={this}, community={Community}");

                SetBitForCircle(_systemCircles, circle, true);
            }

            return true;
        }

        public bool Encode(CodedOutputStream stream)
        {
            stream.WritePrototypeRef<Prototype>(RegionRef);
            stream.WritePrototypeRef<Prototype>(DifficultyRef);

            stream.WriteRawByte((byte)_slots.Length);
            foreach (AvatarSlotInfo slot in _slots)
            {
                stream.WritePrototypeRef<Prototype>(slot.AvatarRef);
                stream.WritePrototypeRef<Prototype>(slot.CostumeRef);
                stream.WriteRawInt32(slot.Level);
                stream.WriteRawInt32(slot.PrestigeLevel);
            }

            stream.WriteRawInt32((int)IsOnline);
            stream.WriteRawString(_playerName);
            stream.WriteRawString(_secondaryPlayerName);
            stream.WriteRawVarint64(_consoleAccountIds[0]);
            stream.WriteRawVarint64(_consoleAccountIds[1]);

            // The whole circle system is such a mess lol
            int numCircles = 0;
            foreach (CommunityCircle circle in Community.IterateCircles(this))
            {
                if (circle.ShouldArchiveTo(/* archive */))
                    numCircles++;
            }
            stream.WriteRawInt32(numCircles);

            foreach (CommunityCircle circle in Community.IterateCircles(this))
            {
                if (circle.ShouldArchiveTo(/* archive */) == false) continue;

                int archiveCircleId = Community.CircleManager.GetArchiveCircleId(circle);
                if (archiveCircleId == 1)
                    return Logger.ErrorReturn(false, $"Encode(): Invalid archive circle id returned for circle in archive. circle={circle}");

                stream.WriteRawInt32(archiveCircleId);
            }

            return true;
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

        public bool ShouldArchiveTo(Archive archive)
        {
            foreach (CommunityCircle circle in Community.IterateCircles(this))
            {
                if (circle.ShouldArchiveTo(archive))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if this <see cref="CommunityMember"/> is in the provided <see cref="CommunityCircle"/>.
        /// </summary>
        public bool IsInCircle(CommunityCircle circle)
        {
            int circleId = (int)circle.Id;
            if ((circleId >= 0 && circleId < _systemCircles.Length) == false)
                return Logger.WarnReturn(false, $"IsInCircle(): Invalid circle id for bitset. circle={circle}");

            return _systemCircles[circleId];
        }

        /// <summary>
        /// Returns the number of <see cref="CommunityCircle"/> instances this <see cref="CommunityMember"/> is in.
        /// </summary>
        /// <returns></returns>
        public int NumCircles()
        {
            int numCircles = 0;
            for (int i = 0; i < _systemCircles.Count; i++)
                if (_systemCircles[i]) numCircles++;
            return numCircles;
        }

        /// <summary>
        /// Sets the bit value for the provided <see cref="CommunityCircle"/>.
        /// </summary>
        public bool AddRemoveFromCircle(bool add, CommunityCircle circle)
        {
            // This method is pretty much just a wrapper around SetBitForCircle since user circles never got implemented.
            if (circle.Type != CircleType.System)
                return Logger.WarnReturn(false, $"AddRemoveFromCircle(): Only system circles are supported. add={add}, circle={circle}, member={this}");

            return SetBitForCircle(_systemCircles, circle, add);
        }

        /// <summary>
        /// Updates the state of this <see cref="CommunityMember"/> with new data from a <see cref="CommunityMemberBroadcast"/> instance.
        /// Returns <see cref="CommunityMemberUpdateOptionBits"/> that specifies the fields that were updated.
        /// </summary>
        public CommunityMemberUpdateOptionBits ReceiveBroadcast(CommunityMemberBroadcast broadcast)
        {
            CommunityMemberUpdateOptionBits updateOptionBits = CommunityMemberUpdateOptionBits.None;

            if (broadcast.HasCurrentRegionRefId)
            {
                // CommunityMember::updateRegionRef()
                PrototypeId newRegionRef = (PrototypeId)broadcast.CurrentRegionRefId;

                if (RegionRef != newRegionRef)
                {
                    _regionRef = newRegionRef;
                    updateOptionBits |= CommunityMemberUpdateOptionBits.RegionRef;
                }
                    
            }

            if (broadcast.HasCurrentDifficultyRefId)
            {
                // CommunityMember::updateDifficultyRef()
                PrototypeId newDifficultyRef = (PrototypeId)broadcast.CurrentDifficultyRefId;

                if (DifficultyRef != newDifficultyRef)
                {
                    _difficultyRef = newDifficultyRef;
                    updateOptionBits |= CommunityMemberUpdateOptionBits.DifficultyRef;
                }  
            }

            if (broadcast.HasIsOnline)
            {
                CommunityMemberOnlineStatus oldIsOnline = IsOnline;
                _isOnline = broadcast.IsOnline == 1 ? CommunityMemberOnlineStatus.Online : CommunityMemberOnlineStatus.Offline;

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

            // Notify circles of member changes
            if (updateOptionBits != CommunityMemberUpdateOptionBits.None)
            {
                foreach (CommunityCircle circle in Community.IterateCircles(this))
                    circle.OnMemberReceivedBroadcast(this, updateOptionBits);
            }

            return updateOptionBits;
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            sb.AppendLine($"{nameof(_playerName)}: {_playerName}");
            sb.AppendLine($"{nameof(_dbId)}: 0x{_dbId:X}");
            sb.AppendLine($"{nameof(_regionRef)}: {GameDatabase.GetPrototypeName(_regionRef)}");
            sb.AppendLine($"{nameof(_difficultyRef)}: {GameDatabase.GetPrototypeName(_difficultyRef)}");

            for (int i = 0; i < _slots.Length; i++)
                sb.AppendLine($"{nameof(_slots)}[{i}]: {_slots[i]}");

            sb.AppendLine($"{nameof(_isOnline)}: {_isOnline}");
            sb.AppendLine($"{nameof(_secondaryPlayerName)}: {_secondaryPlayerName}");
            sb.AppendLine($"{nameof(_consoleAccountIds)}[0]: {_consoleAccountIds[0]}");
            sb.AppendLine($"{nameof(_consoleAccountIds)}[1]: {_consoleAccountIds[1]}");

            sb.Append($"{nameof(_systemCircles)}: ");
            for (int i = 0; i < _systemCircles.Count; i++)
                if (_systemCircles[i]) sb.Append((CircleId)i).Append(' ');
            sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>
        /// Sets the bit value for the provided <see cref="CommunityCircle"/>.
        /// </summary>
        private bool SetBitForCircle(BitArray bitSet, CommunityCircle circle, bool value)
        {
            int circleId = (int)circle.Id;
            if ((circleId >= 0 && circleId < bitSet.Length) == false)
                return Logger.WarnReturn(false, $"SetBitForCircle(): Invalid circle id for bitset. value={value}, circle={circle}, member={this}");

            bitSet[circleId] = value;
            return true;
        }
    }
}
