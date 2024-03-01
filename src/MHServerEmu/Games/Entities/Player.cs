using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Common.Extensions;
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
using MHServerEmu.Games.Social;
using MHServerEmu.PlayerManagement.Accounts.DBModels;

namespace MHServerEmu.Games.Entities
{
    public class Player : Entity, IMissionManagerOwner
    {
        public MissionManager MissionManager { get; set; }
        public ReplicatedPropertyCollection AvatarProperties { get; set; }
        public ulong ShardId { get; set; }
        public ReplicatedVariable<string> Name { get; set; }
        public ulong ConsoleAccountId1 { get; set; }
        public ulong ConsoleAccountId2 { get; set; }
        public ReplicatedVariable<string> UnkName { get; set; }
        public ulong MatchQueueStatus { get; set; }
        public bool EmailVerified { get; set; }
        public ulong AccountCreationTimestamp { get; set; }
        public ReplicatedVariable<ulong> PartyId { get; set; }
        public string UnknownString { get; set; }
        public bool HasGuildInfo { get; set; }
        public GuildMemberReplicationRuntimeInfo GuildInfo { get; set; }
        public bool HasCommunity { get; set; }
        public Community Community { get; set; }
        public bool UnkBool { get; set; }
        public PrototypeId[] StashInventories { get; set; }
        public uint[] AvailableBadges { get; set; }
        public GameplayOptions GameplayOptions { get; set; }
        public AchievementState[] AchievementStates { get; set; }
        public StashTabOption[] StashTabOptions { get; set; }

        public Player(EntityBaseData baseData, ByteString archiveData) : base(baseData, archiveData) { }

        // note: this is ugly
        public Player(EntityBaseData baseData, AoiNetworkPolicyValues replicationPolicy, ReplicatedPropertyCollection properties,
            MissionManager missionManager, ReplicatedPropertyCollection avatarProperties,
            ulong shardId, ReplicatedVariable<string> playerName, ReplicatedVariable<string> unkName,
            ulong matchQueueStatus, bool emailVerified, ulong accountCreationTimestamp, ReplicatedVariable<ulong> partyId,
            Community community, bool unkBool, PrototypeId[] stashInventories, uint[] availableBadges,
            GameplayOptions gameplayOptions, AchievementState[] achievementStates, StashTabOption[] stashTabOptions) : base(baseData)
        {
            ReplicationPolicy = replicationPolicy;
            Properties = properties;

            MissionManager = missionManager;
            AvatarProperties = avatarProperties;
            ShardId = shardId;
            Name = playerName;
            ConsoleAccountId1 = 0;
            ConsoleAccountId2 = 0;
            UnkName = unkName;
            MatchQueueStatus = matchQueueStatus;
            EmailVerified = emailVerified;
            AccountCreationTimestamp = accountCreationTimestamp;
            PartyId = partyId;
            Community = community;
            UnkBool = unkBool;
            StashInventories = stashInventories;
            AvailableBadges = availableBadges;
            GameplayOptions = gameplayOptions;
            AchievementStates = achievementStates;
            StashTabOptions = stashTabOptions;
        }

        protected override void Decode(CodedInputStream stream)
        {
            base.Decode(stream);

            BoolDecoder boolDecoder = new();

            MissionManager = new(stream, boolDecoder);
            AvatarProperties = new(stream);

            ShardId = stream.ReadRawVarint64();
            Name = new(stream);
            ConsoleAccountId1 = stream.ReadRawVarint64();
            ConsoleAccountId2 = stream.ReadRawVarint64();
            UnkName = new(stream);
            MatchQueueStatus = stream.ReadRawVarint64();
            EmailVerified = boolDecoder.ReadBool(stream);
            AccountCreationTimestamp = stream.ReadRawVarint64();

            PartyId = new(stream);

            HasGuildInfo = boolDecoder.ReadBool(stream);
            if (HasGuildInfo) GuildInfo = new(stream);      // GuildMember::SerializeReplicationRuntimeInfo

            UnknownString = stream.ReadRawString();

            HasCommunity = boolDecoder.ReadBool(stream);
            if (HasCommunity) Community = new(stream);

            UnkBool = boolDecoder.ReadBool(stream);

            StashInventories = new PrototypeId[stream.ReadRawVarint64()];
            for (int i = 0; i < StashInventories.Length; i++)
                StashInventories[i] = stream.ReadPrototypeEnum<Prototype>();

            AvailableBadges = new uint[stream.ReadRawVarint64()];
            for (int i = 0; i < AvailableBadges.Length; i++) AvailableBadges[i] = stream.ReadRawVarint32();

            GameplayOptions = new(stream, boolDecoder);

            AchievementStates = new AchievementState[stream.ReadRawVarint64()];
            for (int i = 0; i < AchievementStates.Length; i++)
                AchievementStates[i] = new(stream);

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
            boolEncoder.EncodeBool(HasGuildInfo);
            boolEncoder.EncodeBool(HasCommunity);
            boolEncoder.EncodeBool(UnkBool);

            GameplayOptions.EncodeBools(boolEncoder);

            boolEncoder.Cook();

            // Encode
            MissionManager.Encode(stream, boolEncoder);
            AvatarProperties.Encode(stream);

            stream.WriteRawVarint64(ShardId);
            Name.Encode(stream);
            stream.WriteRawVarint64(ConsoleAccountId1);
            stream.WriteRawVarint64(ConsoleAccountId2);
            UnkName.Encode(stream);
            stream.WriteRawVarint64(MatchQueueStatus);
            boolEncoder.WriteBuffer(stream);   // EmailVerified
            stream.WriteRawVarint64(AccountCreationTimestamp);

            PartyId.Encode(stream);

            boolEncoder.WriteBuffer(stream);   // HasGuildInfo
            if (HasGuildInfo) GuildInfo.Encode(stream);

            stream.WriteRawString(UnknownString);

            boolEncoder.WriteBuffer(stream);   // HasCommunity
            if (HasCommunity) Community.Encode(stream);

            boolEncoder.WriteBuffer(stream);   // UnkBool

            stream.WriteRawVarint64((ulong)StashInventories.Length);
            foreach (PrototypeId stashInventory in StashInventories) stream.WritePrototypeEnum<Prototype>(stashInventory);

            stream.WriteRawVarint64((ulong)AvailableBadges.Length);
            foreach (uint badge in AvailableBadges) stream.WriteRawVarint32(badge);

            GameplayOptions.Encode(stream, boolEncoder);

            stream.WriteRawVarint64((ulong)AchievementStates.Length);
            foreach (AchievementState state in AchievementStates) state.Encode(stream);

            stream.WriteRawVarint64((ulong)StashTabOptions.Length);
            foreach (StashTabOption option in StashTabOptions) option.Encode(stream);
        }

        /// <summary>
        /// Initializes this <see cref="Player"/> from data contained in the provided <see cref="DBAccount"/>.
        /// </summary>
        public void InitializeFromDBAccount(DBAccount account)
        {
            // Adjust properties
            foreach (var accountAvatar in account.Avatars)
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

            // Hardcoded community tab easter eggs
            CommunityMember friend = Community.CommunityMemberList[0];
            friend.MemberName = "DavidBrevik";
            friend.Slots = new AvatarSlotInfo[] { new((PrototypeId)15769648016960461069, (PrototypeId)4881398219179434365, 60, 6) };
            friend.OnlineStatus = CommunityMemberOnlineStatus.Online;
            friend.RegionRef = (PrototypeId)10434222419069901867;
            friend = Community.CommunityMemberList[1];
            friend.OnlineStatus = CommunityMemberOnlineStatus.Online;
            friend.MemberName = "TonyStark";
            friend.Slots = new AvatarSlotInfo[] { new((PrototypeId)421791326977791218, (PrototypeId)7150542631074405762, 60, 5) };
            friend.RegionRef = (PrototypeId)RegionPrototypeId.NPEAvengersTowerHUBRegion;

            Community.CommunityMemberList.Add(new("Doomsaw", 1, 0, 0, new AvatarSlotInfo[] { new((PrototypeId)17750839636937086083, (PrototypeId)14098108758769669917, 60, 6) }, CommunityMemberOnlineStatus.Online, "", new int[] { 0 }));
            Community.CommunityMemberList.Add(new("PizzaTime", 2, 0, 0, new AvatarSlotInfo[] { new((PrototypeId)9378552423541970369, (PrototypeId)6454902525769881598, 60, 5) }, CommunityMemberOnlineStatus.Online, "", new int[] { 0 }));
            Community.CommunityMemberList.Add(new("RogueServerEnjoyer", 3, 0, 0, new AvatarSlotInfo[] { new((PrototypeId)1660250039076459846, (PrototypeId)9447440487974639491, 60, 3) }, CommunityMemberOnlineStatus.Online, "", new int[] { 0 }));
            Community.CommunityMemberList.Add(new("WhiteQueenXOXO", 4, 0, 0, new AvatarSlotInfo[] { new((PrototypeId)412966192105395660, (PrototypeId)12724924652099869123, 60, 4) }, CommunityMemberOnlineStatus.Online, "", new int[] { 0 }));
            Community.CommunityMemberList.Add(new("AlexBond", 5, 0, 0, new AvatarSlotInfo[] { new((PrototypeId)9255468350667101753, (PrototypeId)16813567318560086134, 60, 2) }, CommunityMemberOnlineStatus.Online, "", new int[] { 0 }));
            Community.CommunityMemberList.Add(new("Crypto137", 6, 0, 0, new AvatarSlotInfo[] { new((PrototypeId)421791326977791218, (PrototypeId)1195778722002966150, 60, 2) }, CommunityMemberOnlineStatus.Online, "", new int[] { 0 }));
            Community.CommunityMemberList.Add(new("yn01", 7, 0, 0, new AvatarSlotInfo[] { new((PrototypeId)12534955053251630387, (PrototypeId)14506515434462517197, 60, 2) }, CommunityMemberOnlineStatus.Online, "", new int[] { 0 }));
            Community.CommunityMemberList.Add(new("Gazillion", 8, 0, 0, Array.Empty<AvatarSlotInfo>(), CommunityMemberOnlineStatus.Offline, "", new int[] { 0 }));
            Community.CommunityMemberList.Add(new("FriendlyLawyer", 100, 0, 0, new AvatarSlotInfo[] { new((PrototypeId)12394659164528645362, (PrototypeId)2844257346122946366, 99, 1) }, CommunityMemberOnlineStatus.Online, "", new int[] { 2 }));
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            sb.AppendLine($"MissionManager: {MissionManager}");
            sb.AppendLine($"AvatarProperties: {AvatarProperties}");
            sb.AppendLine($"ShardId: {ShardId}");
            sb.AppendLine($"Name: {Name}");
            sb.AppendLine($"ConsoleAccountId1: 0x{ConsoleAccountId1:X}");
            sb.AppendLine($"ConsoleAccountId2: 0x{ConsoleAccountId2:X}");
            sb.AppendLine($"UnkName: {UnkName}");
            sb.AppendLine($"MatchQueueStatus: 0x{MatchQueueStatus:X}");
            sb.AppendLine($"EmailVerified: {EmailVerified}");
            sb.AppendLine($"AccountCreationTimestamp: 0x{AccountCreationTimestamp:X}");
            sb.AppendLine($"PartyId: {PartyId}");
            sb.AppendLine($"HasGuildInfo: {HasGuildInfo}");
            sb.AppendLine($"GuildInfo: {GuildInfo}");
            sb.AppendLine($"UnknownString: {UnknownString}");
            sb.AppendLine($"HasCommunity: {HasCommunity}");
            sb.AppendLine($"Community: {Community}");
            sb.AppendLine($"UnkBool: {UnkBool}");
            for (int i = 0; i < StashInventories.Length; i++) sb.AppendLine($"StashInventory{i}: {GameDatabase.GetPrototypeName(StashInventories[i])}");
            for (int i = 0; i < AvailableBadges.Length; i++) sb.AppendLine($"AvailableBadge{i}: 0x{AvailableBadges[i]:X}");
            sb.AppendLine($"GameplayOptions: {GameplayOptions}");
            for (int i = 0; i < AchievementStates.Length; i++) sb.AppendLine($"AchievementState{i}: {AchievementStates[i]}");
            for (int i = 0; i < StashTabOptions.Length; i++) sb.AppendLine($"StashTabOption{i}: {StashTabOptions[i]}");
        }
    }
}
