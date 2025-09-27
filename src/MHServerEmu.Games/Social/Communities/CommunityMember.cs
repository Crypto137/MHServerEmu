using System.Collections;
using System.Text;
using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;

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
    public enum CommunityMemberUpdateOptions
    {
        None            = 0,
        NewlyCreated    = 1 << 0,
        Circle          = 1 << 1,
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
        SecondaryPlayer = 1 << 12,

        AvatarSlotBits = AvatarRef | CostumeRef | Level | PrestigeLevel
    }

    public class CommunityMember : ISerialize
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private ulong _dbId;
        private PrototypeId _regionRef;
        private PrototypeId _difficultyRef;

        private long _lastLogoutTimeAsFileTimeUtc = 0;

        private PrototypeId _avatarRef;
        private PrototypeId _costumeRef;
        private int _characterLevel;
        private int _prestigeLevel;

        private CommunityMemberOnlineStatus _isOnline;
        private string _playerName = string.Empty;
        private readonly BitArray _systemCircles = new((int)CircleId.NumCircles);

        public Community Community { get; }

        public ulong DbId { get => _dbId; }
        public PrototypeId RegionRef { get => _regionRef; }
        public PrototypeId AvatarRef { get => _avatarRef; }
        public PrototypeId CostumeRef { get => _costumeRef; }
        public int CharacterLevel { get => _characterLevel; }
        public int PrestigeLevel { get => _prestigeLevel; }
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

            if (archive.IsPersistent == false)
            {
                success &= Serializer.Transfer(archive, ref _regionRef);
                success &= Serializer.Transfer(archive, ref _difficultyRef);

                // V48_TODO: Fix this

                int isOnline = (int)_isOnline;
                success &= Serializer.Transfer(archive, ref isOnline);
                _isOnline = (CommunityMemberOnlineStatus)isOnline;

                success &= Serializer.Transfer(archive, ref _playerName);
            }

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
                    if (archiveCircleId == -1)
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

        public string GetName()
        {
            return _playerName;
        }

        public void SetName(string name)
        {
            _playerName = name;
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

        public bool IsIgnored()
        {
            CommunityCircle ignoreCircle = Community.GetCircle(CircleId.__Ignore);
            if (ignoreCircle == null) return Logger.WarnReturn(false, "IsIgnored(): ignoreCircle == null");
            return IsInCircle(ignoreCircle);
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

        public bool CanReceiveBroadcast(CommunityBroadcastFlags filterFlags = CommunityBroadcastFlags.All)
        {
            foreach (CommunityCircle circle in Community.IterateCircles(this))
            {
                if (circle.IsIgnored)
                    return false;
            }

            CommunityBroadcastFlags flags = GetBroadcastFlags();
            return (flags & filterFlags) != 0;
        }

        public CommunityBroadcastFlags GetBroadcastFlags()
        {
            CommunityBroadcastFlags flags = CommunityBroadcastFlags.None;

            foreach (CommunityCircle circle in Community.IterateCircles(this))
                flags |= circle.BroadcastFlags;

            return flags;
        }

        /// <summary>
        /// Updates the state of this <see cref="CommunityMember"/> with new data from a <see cref="CommunityMemberBroadcast"/> instance.
        /// Returns <see cref="CommunityMemberUpdateOptions"/> that specifies the fields that were updated.
        /// </summary>
        public CommunityMemberUpdateOptions ReceiveBroadcast(CommunityMemberBroadcast broadcast, bool sendUpdate)
        {
            CommunityMemberUpdateOptions updateOptions = CommunityMemberUpdateOptions.None;

            if (broadcast.HasCurrentRegionRefId)
            {
                // CommunityMember::updateRegionRef()
                PrototypeId newRegionRef = (PrototypeId)broadcast.CurrentRegionRefId;

                if (RegionRef != newRegionRef)
                {
                    _regionRef = newRegionRef;
                    updateOptions |= CommunityMemberUpdateOptions.RegionRef;
                }
                    
            }

            if (broadcast.HasIsOnline)
            {
                CommunityMemberOnlineStatus isOnlineBefore = _isOnline;
                _isOnline = broadcast.IsOnline == 1 ? CommunityMemberOnlineStatus.Online : CommunityMemberOnlineStatus.Offline;

                if (isOnlineBefore != _isOnline)
                    updateOptions |= CommunityMemberUpdateOptions.IsOnline;
            }

            if (broadcast.HasLastLogoutTimeAsFileTimeUtc)
            {
                long oldLastLogoutTime = _lastLogoutTimeAsFileTimeUtc;
                _lastLogoutTimeAsFileTimeUtc = broadcast.LastLogoutTimeAsFileTimeUtc;

                if (oldLastLogoutTime != _lastLogoutTimeAsFileTimeUtc)
                    updateOptions |= CommunityMemberUpdateOptions.LastLogoutTime;
            }

            if (broadcast.HasCurrentAvatarRefId)
            {
                PrototypeId newAvatarRef = (PrototypeId)broadcast.CurrentAvatarRefId;

                if (_avatarRef != newAvatarRef)
                {
                    _avatarRef = newAvatarRef;
                    updateOptions |= CommunityMemberUpdateOptions.AvatarRef;
                }
            }

            if (broadcast.HasCurrentCostumeRefId)
            {
                PrototypeId newCostumeRef = (PrototypeId)broadcast.CurrentCostumeRefId;

                if (_costumeRef != newCostumeRef)
                {
                    _costumeRef = newCostumeRef;
                    updateOptions |= CommunityMemberUpdateOptions.CostumeRef;
                }
            }

            if (broadcast.HasCurrentCharacterLevel)
            {
                int newCharacterLevel = (int)broadcast.CurrentCharacterLevel;

                if (_characterLevel != newCharacterLevel)
                {
                    _characterLevel = newCharacterLevel;
                    updateOptions |= CommunityMemberUpdateOptions.Level;
                }
            }

            if (broadcast.HasCurrentPrestigeLevel)
            {
                int newPrestigeLevel = (int)broadcast.CurrentPrestigeLevel;

                if (_prestigeLevel != newPrestigeLevel)
                {
                    _prestigeLevel = newPrestigeLevel;
                    updateOptions |= CommunityMemberUpdateOptions.PrestigeLevel;
                }
            }

            if (broadcast.HasCurrentPlayerName)
            {
                if (_playerName != broadcast.CurrentPlayerName)
                {
                    _playerName = broadcast.CurrentPlayerName;
                    updateOptions |= CommunityMemberUpdateOptions.Name;
                }
            }

            // Notify circles of member changes
            if (updateOptions != CommunityMemberUpdateOptions.None)
            {
                foreach (CommunityCircle circle in Community.IterateCircles(this))
                    circle.OnMemberReceivedBroadcast(this, updateOptions);
            }

            // Relay this broadcast to the client
            if (sendUpdate && updateOptions != 0)
                SendUpdateToOwner(updateOptions);

            return updateOptions;
        }

        public CommunityMemberUpdateOptions ClearData()
        {
            CommunityMemberUpdateOptions updateOptions = CommunityMemberUpdateOptions.None;

            if (RegionRef != PrototypeId.Invalid)
            {
                _regionRef = PrototypeId.Invalid;
                updateOptions |= CommunityMemberUpdateOptions.RegionRef;
            }

            if (AvatarRef != PrototypeId.Invalid)
            {
                _avatarRef = PrototypeId.Invalid;
                updateOptions |= CommunityMemberUpdateOptions.AvatarRef;
            }

            if (CostumeRef != PrototypeId.Invalid)
            {
                _costumeRef = PrototypeId.Invalid;
                updateOptions |= CommunityMemberUpdateOptions.CostumeRef;
            }

            if (CharacterLevel != 0)
            {
                _characterLevel = 0;
                updateOptions |= CommunityMemberUpdateOptions.Level;
            }

            if (PrestigeLevel != 0)
            {
                _prestigeLevel = 0;
                updateOptions |= CommunityMemberUpdateOptions.PrestigeLevel;
            }

            return updateOptions;
        }

        /// <summary>
        /// Sends a <see cref="NetMessageModifyCommunityMember"/> to the owner <see cref="Player"/> containing
        /// data specified by provided <see cref="CommunityMemberUpdateOptions"/>.
        /// </summary>
        public void SendUpdateToOwner(CommunityMemberUpdateOptions updateOptions)
        {
            // Early out if there is nothing to update
            if (updateOptions == CommunityMemberUpdateOptions.None)
                return;

            NetMessageModifyCommunityMember.Builder messageBuilder = NetMessageModifyCommunityMember.CreateBuilder();

            // Broadcast
            CommunityMemberBroadcast.Builder broadcastBuilder = CommunityMemberBroadcast.CreateBuilder()
                .SetMemberPlayerDbId(DbId);

            if (updateOptions.HasFlag(CommunityMemberUpdateOptions.RegionRef))
                broadcastBuilder.SetCurrentRegionRefId((ulong)RegionRef);

            if (updateOptions.HasFlag(CommunityMemberUpdateOptions.AvatarRef))
                broadcastBuilder.SetCurrentAvatarRefId((ulong)AvatarRef);

            if (updateOptions.HasFlag(CommunityMemberUpdateOptions.CostumeRef))
                broadcastBuilder.SetCurrentCostumeRefId((ulong)CostumeRef);

            if (updateOptions.HasFlag(CommunityMemberUpdateOptions.Level))
                broadcastBuilder.SetCurrentCharacterLevel((uint)CharacterLevel);

            if (updateOptions.HasFlag(CommunityMemberUpdateOptions.PrestigeLevel))
                broadcastBuilder.SetCurrentPrestigeLevel((uint)PrestigeLevel);

            if (updateOptions.HasFlag(CommunityMemberUpdateOptions.IsOnline))
                broadcastBuilder.SetIsOnline((int)IsOnline);

            if (updateOptions.HasFlag(CommunityMemberUpdateOptions.Name))
                broadcastBuilder.SetCurrentPlayerName(GetName());

            if (IsOnline != CommunityMemberOnlineStatus.Online && updateOptions.HasFlag(CommunityMemberUpdateOptions.LastLogoutTime))
                broadcastBuilder.SetLastLogoutTimeAsFileTimeUtc(_lastLogoutTimeAsFileTimeUtc);

            // We don't care about secondary players on PC

            messageBuilder.SetBroadcast(broadcastBuilder.Build());

            // PlayerName
            if (updateOptions.HasFlag(CommunityMemberUpdateOptions.NewlyCreated))
                messageBuilder.SetPlayerName(GetName());

            // SystemCirclesBitSets
            if (updateOptions.HasFlag(CommunityMemberUpdateOptions.Circle))
            {
                // TODO: Switch to GBitArray and use underlying words directly?
                ulong circleBits = 0;
                for (CircleId circle = 0; circle < CircleId.NumCircles; circle++)
                {
                    int i = (int)circle;
                    if (_systemCircles[i])
                        circleBits |= 1ul << i;
                }

                messageBuilder.SetSystemCirclesBitSet(circleBits);
            }

            Community.Owner.SendMessage(messageBuilder.Build());
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            sb.AppendLine($"{nameof(_playerName)}: {_playerName}");
            sb.AppendLine($"{nameof(_dbId)}: 0x{_dbId:X}");
            sb.AppendLine($"{nameof(_regionRef)}: {GameDatabase.GetPrototypeName(_regionRef)}");
            sb.AppendLine($"{nameof(_difficultyRef)}: {GameDatabase.GetPrototypeName(_difficultyRef)}");

            // V48_TODO

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
