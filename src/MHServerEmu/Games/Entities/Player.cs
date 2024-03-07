using System.Text;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Common;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Achievements;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Options;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Regions.MatchQueues;
using MHServerEmu.Games.Social.Communities;
using MHServerEmu.Games.Social.Guilds;
using MHServerEmu.PlayerManagement.Accounts;
using MHServerEmu.PlayerManagement.Accounts.DBModels;

namespace MHServerEmu.Games.Entities
{
    // Avatar index for console versions that have local coop, mostly meaningless on PC.
    public enum PlayerAvatarIndex       
    {
        Primary,
        Secondary,
        Count
    }

    // NOTE: These badges and their descriptions are taken from an internal build dated June 2015 (most likely version 1.35).
    // They are not fully implemented and they may be outdated for our version 1.52.
    public enum AvailableBadges
    {
        CanGrantBadges = 1,         // User can grant badges to other users
        SiteCommands,               // User can run the site commands (player/regions lists, change to specific region etc)
        CanBroadcastChat,           // User can send a chat message to all players
        AllContentAccess,           // User has access to all content in the game
        CanLogInAsAnotherAccount,   // User has ability to log in as another account
        CanDisablePersistence,      // User has ability to play without saving
        PlaytestCommands,           // User can always use commands that are normally only available during a playtest (e.g. bug)
        CsrUser,                    // User can perform Customer Service Representative commands
        DangerousCheatAccess,       // User has access to some especially dangerous cheats
        NumberOfBadges
    }

    public class Player : Entity, IMissionManagerOwner
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private ReplicatedVariable<string> _playerName;
        private ulong[] _consoleAccountIds = new ulong[(int)PlayerAvatarIndex.Count];
        private ReplicatedVariable<string> _secondaryPlayerName;

        private ulong _guildId;
        private string _guildName;
        private GuildMembership _guildMembership;

        private SortedSet<AvailableBadges> _badges;

        public MissionManager MissionManager { get; set; }
        public ReplicatedPropertyCollection AvatarProperties { get; set; }
        public ulong ShardId { get; set; }
        public MatchQueueStatus MatchQueueStatus { get; set; }

        // NOTE: EmailVerified and AccountCreationTimestamp are set in NetMessageGiftingRestrictionsUpdate that
        // should be sent in the packet right after logging in. NetMessageGetCurrencyBalanceResponse should be
        // sent along with it.
        public bool EmailVerified { get; set; }
        public TimeSpan AccountCreationTimestamp { get; set; }  // UnixTime

        public ReplicatedVariable<ulong> PartyId { get; set; }
        public Community Community { get; set; }
        public bool UnkBool { get; set; }
        public PrototypeId[] StashInventories { get; set; }
        public GameplayOptions GameplayOptions { get; set; }
        public AchievementState AchievementState { get; set; }
        public StashTabOption[] StashTabOptions { get; set; }

        public Player(EntityBaseData baseData, ByteString archiveData) : base(baseData, archiveData) { }

        // note: this is ugly
        public Player(EntityBaseData baseData, AOINetworkPolicyValues replicationPolicy, ReplicatedPropertyCollection properties,
            MissionManager missionManager, ReplicatedPropertyCollection avatarProperties,
            ulong shardId, ReplicatedVariable<string> playerName, ReplicatedVariable<string> secondaryPlayerName,
            MatchQueueStatus matchQueueStatus, bool emailVerified, TimeSpan accountCreationTimestamp, ReplicatedVariable<ulong> partyId,
            Community community, bool unkBool, PrototypeId[] stashInventories, SortedSet<AvailableBadges> badges,
            GameplayOptions gameplayOptions, AchievementState achievementState, StashTabOption[] stashTabOptions) : base(baseData)
        {
            ReplicationPolicy = replicationPolicy;
            Properties = properties;

            MissionManager = missionManager;
            AvatarProperties = avatarProperties;
            ShardId = shardId;
            _playerName = playerName;
            _secondaryPlayerName = secondaryPlayerName;
            MatchQueueStatus = matchQueueStatus;
            EmailVerified = emailVerified;
            AccountCreationTimestamp = accountCreationTimestamp;
            PartyId = partyId;
            Community = community;
            UnkBool = unkBool;
            StashInventories = stashInventories;
            _badges = badges;
            GameplayOptions = gameplayOptions;
            AchievementState = achievementState;
            StashTabOptions = stashTabOptions;
        }

        protected override void Decode(CodedInputStream stream)
        {
            base.Decode(stream);

            BoolDecoder boolDecoder = new();

            MissionManager = new(stream, boolDecoder);
            AvatarProperties = new(stream);

            ShardId = stream.ReadRawVarint64();

            _playerName = new(stream);
            _consoleAccountIds[0] = stream.ReadRawVarint64();
            _consoleAccountIds[1] = stream.ReadRawVarint64();
            _secondaryPlayerName = new(stream);

            MatchQueueStatus = new(stream);
            MatchQueueStatus.SetOwner(this);

            EmailVerified = boolDecoder.ReadBool(stream);
            AccountCreationTimestamp = Clock.UnixTimeMicrosecondsToTimeSpan(stream.ReadRawInt64());

            PartyId = new(stream);

            GuildMember.SerializeReplicationRuntimeInfo(stream, boolDecoder, ref _guildId, ref _guildName, ref _guildMembership);

            // There is a string here that is always empty and is immediately discarded after reading, purpose unknown
            string emptyString = stream.ReadRawString();
            if (emptyString != string.Empty)
                Logger.Warn($"Decode(): emptyString is not empty!");

            Community = new(this);
            Community.Initialize();
            bool hasCommunityData = boolDecoder.ReadBool(stream);
            if (hasCommunityData) Community.Decode(stream);

            UnkBool = boolDecoder.ReadBool(stream);

            StashInventories = new PrototypeId[stream.ReadRawVarint64()];
            for (int i = 0; i < StashInventories.Length; i++)
                StashInventories[i] = stream.ReadPrototypeEnum<Prototype>();

            _badges = new();
            ulong numBadges = stream.ReadRawVarint64();
            for (ulong i = 0; i < numBadges; i++)
                _badges.Add((AvailableBadges)stream.ReadRawVarint32());

            GameplayOptions = new(stream, boolDecoder);

            AchievementState = new(stream);

            StashTabOptions = new StashTabOption[stream.ReadRawVarint64()];
            for (int i = 0; i < StashTabOptions.Length; i++)
                StashTabOptions[i] = new(stream);
        }

        public override void Encode(CodedOutputStream stream)
        {
            base.Encode(stream);

            // Prepare bool encoder
            BoolEncoder boolEncoder = new();

            MissionManager.EncodeBools(boolEncoder);

            boolEncoder.EncodeBool(EmailVerified);
            boolEncoder.EncodeBool(_guildId != GuildMember.InvalidGuildId);
            boolEncoder.EncodeBool(true);   // hasCommunity TODO: Check archive's replication policy and send community only to owners
            boolEncoder.EncodeBool(UnkBool);

            GameplayOptions.EncodeBools(boolEncoder);

            boolEncoder.Cook();

            // Encode
            MissionManager.Encode(stream, boolEncoder);
            AvatarProperties.Encode(stream);

            stream.WriteRawVarint64(ShardId);
            _playerName.Encode(stream);
            stream.WriteRawVarint64(_consoleAccountIds[0]);
            stream.WriteRawVarint64(_consoleAccountIds[1]);
            _secondaryPlayerName.Encode(stream);
            MatchQueueStatus.Encode(stream);
            boolEncoder.WriteBuffer(stream);   // EmailVerified
            stream.WriteRawInt64(AccountCreationTimestamp.Ticks / 10);
            PartyId.Encode(stream);
            GuildMember.SerializeReplicationRuntimeInfo(stream, boolEncoder, ref _guildId, ref _guildName, ref _guildMembership);
            stream.WriteRawString(string.Empty);    // Mysterious always empty throwaway string

            boolEncoder.WriteBuffer(stream);   // hasCommunity
            Community.Encode(stream);

            boolEncoder.WriteBuffer(stream);   // UnkBool

            stream.WriteRawVarint64((ulong)StashInventories.Length);
            foreach (PrototypeId stashInventory in StashInventories) stream.WritePrototypeEnum<Prototype>(stashInventory);

            stream.WriteRawVarint64((ulong)_badges.Count);
            foreach (AvailableBadges badge in _badges)
                stream.WriteRawVarint32((uint)badge);

            GameplayOptions.Encode(stream, boolEncoder);

            AchievementState.Encode(stream);

            stream.WriteRawVarint64((ulong)StashTabOptions.Length);
            foreach (StashTabOption option in StashTabOptions) option.Encode(stream);
        }

        /// <summary>
        /// Initializes this <see cref="Player"/> from data contained in the provided <see cref="DBAccount"/>.
        /// </summary>
        public void InitializeFromDBAccount(DBAccount account)
        {
            // Adjust properties
            foreach (var accountAvatar in account.Avatars.Values)
            {
                PropertyParam enumValue = Property.ToParam(PropertyEnum.AvatarLibraryCostume, 1, (PrototypeId)accountAvatar.Prototype);
                var avatarPrototype = (PrototypeId)accountAvatar.Prototype;

                // Set library costumes according to account data
                Properties[PropertyEnum.AvatarLibraryCostume, 0, avatarPrototype] = (PrototypeId)accountAvatar.Costume;

                // Set avatar levels to 60
                // Note: setting this to above level 60 sets the prestige level as well
                Properties[PropertyEnum.AvatarLibraryLevel, 0, avatarPrototype] = 60;

                // Clean up team ups
                Properties[PropertyEnum.AvatarLibraryTeamUp, 0, avatarPrototype] = PrototypeId.Invalid;

                // Unlock start avatars
                var avatarUnlock = (AvatarUnlockType)(int)Properties[PropertyEnum.AvatarUnlock, enumValue];
                if (avatarUnlock == AvatarUnlockType.Starter)
                    Properties[PropertyEnum.AvatarUnlock, avatarPrototype] = (int)AvatarUnlockType.Type3;
            }

            // TODO: Set this after creating all avatar entities via a NetMessageSetProperty in the same packet
            Properties[PropertyEnum.PlayerMaxAvatarLevel] = 60;

            _playerName.Value = account.PlayerName;    // Used for highlighting your name in leaderboards

            // Todo: send this separately in NetMessageGiftingRestrictionsUpdate on login
            Properties[PropertyEnum.LoginCount] = 1075;
            EmailVerified = true;
            AccountCreationTimestamp = Clock.DateTimeToUnixTime(new(2023, 07, 16, 1, 48, 0));   // First GitHub commit date

            // Clear community
            Community.Shutdown();
            Community.Initialize();

            #region Hardcoded social tab easter eggs
            Community.AddMember(1, "DavidBrevik", CircleId.__Friends);
            Community.ReceiveMemberBroadcast(CommunityMemberBroadcast.CreateBuilder().SetMemberPlayerDbId(1).SetIsOnline(1)
                .SetCurrentRegionRefId(12735255224807267622).SetCurrentDifficultyRefId((ulong)DifficultyTier.Normal)
                .AddSlots(CommunityMemberAvatarSlot.CreateBuilder().SetAvatarRefId(15769648016960461069).SetCostumeRefId(4881398219179434365).SetLevel(60).SetPrestigeLevel(6))
                .Build());

            Community.AddMember(2, "TonyStark", CircleId.__Friends);
            Community.ReceiveMemberBroadcast(CommunityMemberBroadcast.CreateBuilder().SetMemberPlayerDbId(2).SetIsOnline(1)
                .SetCurrentRegionRefId((ulong)RegionPrototypeId.NPEAvengersTowerHUBRegion).SetCurrentDifficultyRefId((ulong)DifficultyTier.Normal)
                .AddSlots(CommunityMemberAvatarSlot.CreateBuilder().SetAvatarRefId(421791326977791218).SetCostumeRefId(7150542631074405762).SetLevel(60).SetPrestigeLevel(5))
                .Build());

            Community.AddMember(3, "Doomsaw", CircleId.__Friends);
            Community.ReceiveMemberBroadcast(CommunityMemberBroadcast.CreateBuilder().SetMemberPlayerDbId(3).SetIsOnline(1)
                .AddSlots(CommunityMemberAvatarSlot.CreateBuilder().SetAvatarRefId(17750839636937086083).SetCostumeRefId(14098108758769669917).SetLevel(60).SetPrestigeLevel(6))
                .Build());

            Community.AddMember(4, "PizzaTime", CircleId.__Friends);
            Community.ReceiveMemberBroadcast(CommunityMemberBroadcast.CreateBuilder().SetMemberPlayerDbId(4).SetIsOnline(1)
                .AddSlots(CommunityMemberAvatarSlot.CreateBuilder().SetAvatarRefId(9378552423541970369).SetCostumeRefId(6454902525769881598).SetLevel(60).SetPrestigeLevel(5))
                .Build());

            Community.AddMember(5, "RogueServerEnjoyer", CircleId.__Friends);
            Community.ReceiveMemberBroadcast(CommunityMemberBroadcast.CreateBuilder().SetMemberPlayerDbId(5).SetIsOnline(1)
                .AddSlots(CommunityMemberAvatarSlot.CreateBuilder().SetAvatarRefId(1660250039076459846).SetCostumeRefId(9447440487974639491).SetLevel(60).SetPrestigeLevel(3))
                .Build());

            Community.AddMember(6, "WhiteQueenXOXO", CircleId.__Friends);
            Community.ReceiveMemberBroadcast(CommunityMemberBroadcast.CreateBuilder().SetMemberPlayerDbId(6).SetIsOnline(1)
                .AddSlots(CommunityMemberAvatarSlot.CreateBuilder().SetAvatarRefId(412966192105395660).SetCostumeRefId(12724924652099869123).SetLevel(60).SetPrestigeLevel(4))
                .Build());

            Community.AddMember(7, "AlexBond", CircleId.__Friends);
            Community.ReceiveMemberBroadcast(CommunityMemberBroadcast.CreateBuilder().SetMemberPlayerDbId(7).SetIsOnline(1)
                .AddSlots(CommunityMemberAvatarSlot.CreateBuilder().SetAvatarRefId(9255468350667101753).SetCostumeRefId(16813567318560086134).SetLevel(60).SetPrestigeLevel(2))
                .Build());

            Community.AddMember(8, "Crypto137", CircleId.__Friends);
            Community.ReceiveMemberBroadcast(CommunityMemberBroadcast.CreateBuilder().SetMemberPlayerDbId(8).SetIsOnline(1)
                .AddSlots(CommunityMemberAvatarSlot.CreateBuilder().SetAvatarRefId(421791326977791218).SetCostumeRefId(1195778722002966150).SetLevel(60).SetPrestigeLevel(2))
                .Build());

            Community.AddMember(9, "yn01", CircleId.__Friends);
            Community.ReceiveMemberBroadcast(CommunityMemberBroadcast.CreateBuilder().SetMemberPlayerDbId(9).SetIsOnline(1)
                .AddSlots(CommunityMemberAvatarSlot.CreateBuilder().SetAvatarRefId(12534955053251630387).SetCostumeRefId(14506515434462517197).SetLevel(60).SetPrestigeLevel(2))
                .Build());

            Community.AddMember(10, "Gazillion", CircleId.__Friends);
            Community.ReceiveMemberBroadcast(CommunityMemberBroadcast.CreateBuilder().SetMemberPlayerDbId(10).SetIsOnline(0).Build());

            Community.AddMember(11, "FriendlyLawyer", CircleId.__Nearby);
            Community.ReceiveMemberBroadcast(CommunityMemberBroadcast.CreateBuilder().SetMemberPlayerDbId(11).SetIsOnline(1)
                .AddSlots(CommunityMemberAvatarSlot.CreateBuilder().SetAvatarRefId(12394659164528645362).SetCostumeRefId(2844257346122946366).SetLevel(99).SetPrestigeLevel(1))
                .Build());
            #endregion

            // Add all badges to admin accounts
            if (account.UserLevel == AccountUserLevel.Admin)
            {
                for (var badge = AvailableBadges.CanGrantBadges; badge < AvailableBadges.NumberOfBadges; badge++)
                    AddBadge(badge);
            }

            AchievementState = account.Player.AchievementState;
            AchievementState.SetAchievementProgress(2, new(1, Clock.UnixTime));
        }

        /// <summary>
        /// Returns the name of the player for the specified <see cref="PlayerAvatarIndex"/>.
        /// </summary>
        public string GetName(PlayerAvatarIndex avatarIndex = PlayerAvatarIndex.Primary)
        {
            if ((avatarIndex >= PlayerAvatarIndex.Primary && avatarIndex < PlayerAvatarIndex.Count) == false)
                Logger.Warn("GetName(): avatarIndex out of range");

            if (avatarIndex == PlayerAvatarIndex.Secondary)
                return _secondaryPlayerName.Value;

            return _playerName.Value;
        }

        /// <summary>
        /// Returns the console account id for the specified <see cref="PlayerAvatarIndex"/>.
        /// </summary>
        public ulong GetConsoleAccountId(PlayerAvatarIndex avatarIndex)
        {
            if ((avatarIndex >= PlayerAvatarIndex.Primary && avatarIndex < PlayerAvatarIndex.Count) == false)
                return 0;

            return _consoleAccountIds[(int)avatarIndex];
        }

        /// <summary>
        /// Add the specified badge to this <see cref="Player"/>. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool AddBadge(AvailableBadges badge) => _badges.Add(badge);

        /// <summary>
        /// Removes the specified badge from this <see cref="Player"/>. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool RemoveBadge(AvailableBadges badge) => _badges.Remove(badge);

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="Player"/> has the specified badge.
        /// </summary>
        public bool HasBadge(AvailableBadges badge) => _badges.Contains(badge);

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            sb.AppendLine($"{nameof(MissionManager)}: {MissionManager}");
            sb.AppendLine($"{nameof(AvatarProperties)}: {AvatarProperties}");
            sb.AppendLine($"{nameof(ShardId)}: {ShardId}");
            sb.AppendLine($"{nameof(_playerName)}: {_playerName}");
            sb.AppendLine($"{nameof(_consoleAccountIds)}[0]: {_consoleAccountIds[0]}");
            sb.AppendLine($"{nameof(_consoleAccountIds)}[1]: {_consoleAccountIds[1]}");
            sb.AppendLine($"{nameof(_secondaryPlayerName)}: {_secondaryPlayerName}");
            sb.AppendLine($"{nameof(MatchQueueStatus)}: {MatchQueueStatus}");
            sb.AppendLine($"{nameof(EmailVerified)}: {EmailVerified}");
            sb.AppendLine($"{nameof(AccountCreationTimestamp)}: {AccountCreationTimestamp}");
            sb.AppendLine($"{nameof(PartyId)}: {PartyId}");

            if (_guildId != GuildMember.InvalidGuildId)
            {
                sb.AppendLine($"{nameof(_guildId)}: {_guildId}");
                sb.AppendLine($"{nameof(_guildName)}: {_guildName}");
                sb.AppendLine($"{nameof(_guildMembership)}: {_guildMembership}");
            }

            sb.AppendLine($"{nameof(Community)}: {Community}");
            sb.AppendLine($"{nameof(UnkBool)}: {UnkBool}");

            for (int i = 0; i < StashInventories.Length; i++)
                sb.AppendLine($"StashInventory{i}: {GameDatabase.GetPrototypeName(StashInventories[i])}");

            if (_badges.Any())
            {
                sb.Append("Badges: ");
                foreach (AvailableBadges badge in _badges)
                    sb.Append(badge.ToString()).Append(' ');
                sb.AppendLine();
            }

            sb.AppendLine($"{nameof(GameplayOptions)}: {GameplayOptions}");
            sb.AppendLine($"{nameof(AchievementState)}: {AchievementState}");

            for (int i = 0; i < StashTabOptions.Length; i++)
                sb.AppendLine($"StashTabOption{i}: {StashTabOptions[i]}");
        }
    }
}
