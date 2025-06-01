using System.Text;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Achievements;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Dialog;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Entities.Options;
using MHServerEmu.Games.Entities.PowerCollections;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Leaderboards;
using MHServerEmu.Games.GameData.Tables;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Populations;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Regions.Maps;
using MHServerEmu.Games.Regions.MatchQueues;
using MHServerEmu.Games.Social.Communities;
using MHServerEmu.Games.Social.Guilds;

namespace MHServerEmu.Games.Entities
{
    // Avatar index for console versions that have local coop, mostly meaningless on PC.
    public enum PlayerAvatarIndex       
    {
        Primary,
        Secondary,
        Count
    }

    public enum CanSwitchAvatarResult
    {
        Success                     = 0,
        NotAllowedInPrivateInstance = 1 << 0,
        NotAllowedInRegion          = 1 << 1,
        NotAllowedInQueue           = 1 << 2,
        NotAllowedInTransformMode   = 1 << 3,
        NotAllowedGeneric           = 1 << 4,
        NotAllowedUnknown           = 1 << 5,
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

    public partial class Player : Entity, IMissionManagerOwner
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly EventPointer<SwitchAvatarEvent> _switchAvatarEvent = new();
        private readonly EventPointer<CheckHoursPlayedEvent> _checkHoursPlayedEvent = new();
        private readonly EventPointer<ScheduledHUDTutorialResetEvent> _hudTutorialResetEvent = new();
        private readonly EventPointer<CommunityBroadcastEvent> _communityBroadcastEvent = new();
        private readonly EventGroup _pendingEvents = new();

        private ReplicatedPropertyCollection _avatarProperties = new();
        private ulong _shardId;     // This was probably used for database sharding, we don't need this
        private RepString _playerName = new();
        private ulong[] _consoleAccountIds = new ulong[(int)PlayerAvatarIndex.Count];
        private RepString _secondaryPlayerName = new();

        // NOTE: EmailVerified and AccountCreationTimestamp are set in NetMessageGiftingRestrictionsUpdate that
        // should be sent in the packet right after logging in. NetMessageGetCurrencyBalanceResponse should be
        // sent along with it.
        private bool _emailVerified;
        private TimeSpan _accountCreationTimestamp;     // UnixTime

        private RepULong _partyId = new();

        private ulong _guildId;
        private string _guildName;
        private GuildMembership _guildMembership;

        private Community _community;
        private List<PrototypeId> _unlockedInventoryList = new();
        private SortedVector<AvailableBadges> _badges = new();
        private HashSet<ulong> _tagEntities = new();
        private Queue<PrototypeId> _kismetSeqQueue = new();
        private Dictionary<PrototypeId, StashTabOptions> _stashTabOptionsDict = new();

        // TODO: Serialize on migration
        private Dictionary<ulong, MapDiscoveryData> _mapDiscoveryDict = new();

        private uint _loginCount;
        private TimeSpan _loginRewardCooldownTimeStart;

        private TeleportData _teleportData;
        private SpawnGimbal _spawnGimbal;
        private bool _newPlayerUISystemsUnlocked;

        public ArchiveVersion LastSerializedArchiveVersion { get; private set; } = ArchiveVersion.Current;    // Updated on serialization

        public MissionManager MissionManager { get; private set; }
        public MatchQueueStatus MatchQueueStatus { get; private set; } = new();
        public override ulong PartyId { get => _partyId.Get(); }
        public Community Community { get => _community; }
        public GameplayOptions GameplayOptions { get; private set; } = new();
        public AchievementState AchievementState { get; private set; } = new();
        public AchievementManager AchievementManager { get; private set; }
        public LeaderboardManager LeaderboardManager { get; private set; }
        public ScoringEventContext ScoringEventContext { get; set; }

        public bool IsFullscreenMoviePlaying { get => Properties[PropertyEnum.FullScreenMoviePlaying]; }
        public bool IsOnLoadingScreen { get; private set; }
        public bool IsFullscreenObscured { get => IsFullscreenMoviePlaying || IsOnLoadingScreen; }
        public uint FullscreenMovieSyncRequestId { get; set; }

        public bool IsSwitchingAvatar { get; private set; }

        public PlayerConnection PlayerConnection { get; private set; }
        public AreaOfInterest AOI { get => PlayerConnection.AOI; }

        public Avatar CurrentAvatar { get; private set; }
        public HUDTutorialPrototype CurrentHUDTutorial { get; private set; }

        // Console stuff - not implemented
        public bool IsConsolePlayer { get => false; }
        public bool IsConsoleUI { get => false; }
        public bool IsUsingUnifiedStash { get => IsConsolePlayer || IsConsoleUI; }

        public bool IsInParty { get; internal set; }
        public static bool IsPlayerTradeEnabled { get; internal set; }
        public Avatar PrimaryAvatar { get => CurrentAvatar; } // Fix for PC
        public Avatar SecondaryAvatar { get; private set; }
        public int CurrentAvatarCharacterLevel { get => PrimaryAvatar?.CharacterLevel ?? 0; }
        public GuildMembership GuildMembership { get; internal set; }
        public PrototypeId ActiveChapter { get => Properties[PropertyEnum.ActiveMissionChapter]; }
        public PrototypeId Faction { get => Properties[PropertyEnum.Faction]; }
        public ulong DialogTargetId { get; private set; }
        public ulong DialogInteractorId { get; private set; }
        public PrototypeId CurrentOpenStashPagePrototypeRef { get; set; }
        public long InfinityXP { get => Properties[PropertyEnum.InfinityXP]; }
        public long OmegaXP { get => Properties[PropertyEnum.OmegaXP]; }
        public long GazillioniteBalance { get => PlayerConnection.GazillioniteBalance; set => PlayerConnection.GazillioniteBalance = value; }
        public int PowerSpecIndexUnlocked { get => Properties[PropertyEnum.PowerSpecIndexUnlocked]; }
        public ulong TeamUpSynergyConditionId { get; set; }

        public Player(Game game) : base(game)
        {
            MissionManager = new(Game, this);
            AchievementManager = new(this);
            LeaderboardManager = new(this);
            ScoringEventContext = new();
            GameplayOptions.SetOwner(this);
        }

        public override string ToString()
        {
            return $"{GetName()} (entityId={Id}, dbGuid=0x{DatabaseUniqueId:X})";
        }

        public override bool Initialize(EntitySettings settings)
        {
            base.Initialize(settings);

            PlayerConnection = settings.PlayerConnection;
            _playerName.Set(settings.PlayerName);

            Game.EntityManager.AddPlayer(this);
            MatchQueueStatus.SetOwner(this);

            _community = new(this);
            _community.Initialize();

            LeaderboardManager.Initialize();

            // Default loading screen before we start loading into a region
            QueueLoadingScreen(PrototypeId.Invalid);

            var popProto = GameDatabase.GlobalsPrototype.PopulationGlobalsPrototype;
            if (popProto == null) return false;
            _spawnGimbal = new (popProto.SpawnMapGimbalRadius, popProto.SpawnMapHorizon);

            return true;
        }

        public override void OnPostInit(EntitySettings settings)
        {
            base.OnPostInit(settings);

            SetGiftingRestrictions();
        }

        public override void OnPropertyChange(PropertyId id, PropertyValue newValue, PropertyValue oldValue, SetPropertyFlags flags)
        {
            base.OnPropertyChange(id, newValue, oldValue, flags);

            if (flags.HasFlag(SetPropertyFlags.Refresh)) return;

            switch (id.Enum)
            {
                case PropertyEnum.TeamUpsAtMaxLevelPersistent:
                    var avatar = CurrentAvatar;
                    if (avatar != null && avatar.IsInWorld)
                    {
                        var teamUpAgent = avatar.CurrentTeamUpAgent;
                        if (teamUpAgent != null)
                        {
                            teamUpAgent.SetTeamUpsAtMaxLevel(this);
                            if (teamUpAgent.IsInWorld) 
                                teamUpAgent.UpdateTeamUpSynergyCondition();
                        }
                    }
                    break;

                case PropertyEnum.PowerCooldownDuration:
                    {
                        Property.FromParam(id, 0, out PrototypeId powerProtoRef);
                        PowerPrototype powerProto = powerProtoRef.As<PowerPrototype>();
                        if (powerProto == null)
                        {
                            Logger.Warn("OnPropertyChange(): powerProto == null");
                            break;
                        }

                        if (Power.IsCooldownPersistent(powerProto))
                            Properties[PropertyEnum.PowerCooldownDurationPersistent, powerProtoRef] = newValue;

                        break;
                    }

                case PropertyEnum.PowerCooldownStartTime:
                {
                    Property.FromParam(id, 0, out PrototypeId powerProtoRef);
                    PowerPrototype powerProto = powerProtoRef.As<PowerPrototype>();
                    if (powerProto == null)
                    {
                        Logger.Warn("OnPropertyChange(): powerProto == null");
                        break;
                    }

                    if (Power.IsCooldownPersistent(powerProto))
                        Properties[PropertyEnum.PowerCooldownStartTimePersistent, powerProtoRef] = newValue;

                    break;
                }
            }
        }

        public override void OnUnpackComplete(Archive archive)
        {
            base.OnUnpackComplete(archive);

            foreach (PrototypeId invProtoRef in _unlockedInventoryList)
            {
                PlayerStashInventoryPrototype stashInvProto = GameDatabase.GetPrototype<PlayerStashInventoryPrototype>(invProtoRef);
                if (stashInvProto == null)
                {
                    Logger.Warn("OnUnpackComplete(): stashInvProto == null");
                    continue;
                }

                if (stashInvProto.IsPlayerStashInventory && IsUsingUnifiedStash == false && stashInvProto.ConvenienceLabel == InventoryConvenienceLabel.UnifiedStash)
                    continue;

                Inventory inventory = GetInventoryByRef(invProtoRef);
                if (inventory == null && AddInventory(invProtoRef) == false)
                    Logger.Warn($"OnUnpackComplete(): Failed to add inventory, invProtoRef={invProtoRef.GetName()}");
            }

            // Restore persistent cooldowns
            if (archive.IsPersistent)
            {
                Dictionary<PropertyId, PropertyValue> setDict = DictionaryPool<PropertyId, PropertyValue>.Instance.Get();

                foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.PowerCooldownDurationPersistent))
                {
                    Property.FromParam(kvp.Key, 0, out PrototypeId powerProtoRef);
                    PowerPrototype powerProto = powerProtoRef.As<PowerPrototype>();
                    if (powerProto == null)
                    {
                        Logger.Warn("OnUnpackComplete(): powerProto == null");
                        continue;
                    }

                    // Discard if no longer flagged as persistent or stored on player
                    if (Power.IsCooldownPersistent(powerProto) == false || Power.IsCooldownOnPlayer(powerProto) == false)
                        continue;

                    setDict[new(PropertyEnum.PowerCooldownDuration, powerProtoRef)] = kvp.Value;
                }

                foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.PowerCooldownStartTimePersistent))
                {
                    Property.FromParam(kvp.Key, 0, out PrototypeId powerProtoRef);
                    PowerPrototype powerProto = powerProtoRef.As<PowerPrototype>();
                    if (powerProto == null)
                    {
                        Logger.Warn("OnUnpackComplete(): powerProto == null");
                        continue;
                    }

                    // Discard if no longer flagged as persistent or stored on player
                    if (Power.IsCooldownPersistent(powerProto) == false || Power.IsCooldownOnPlayer(powerProto) == false)
                        continue;

                    setDict[new(PropertyEnum.PowerCooldownStartTime, powerProtoRef)] = kvp.Value;
                }

                foreach (var kvp in setDict)
                    Properties[kvp.Key] = kvp.Value;

                DictionaryPool<PropertyId, PropertyValue>.Instance.Return(setDict);
            }
        }

        protected override void BindReplicatedFields()
        {
            base.BindReplicatedFields();

            _avatarProperties.Bind(this, AOINetworkPolicyValues.AOIChannelOwner);
            _playerName.Bind(this, AOINetworkPolicyValues.AOIChannelParty | AOINetworkPolicyValues.AOIChannelOwner);
            _partyId.Bind(this, AOINetworkPolicyValues.AOIChannelParty | AOINetworkPolicyValues.AOIChannelOwner);
        }

        protected override void UnbindReplicatedFields()
        {
            base.UnbindReplicatedFields();

            _avatarProperties.Unbind();
            _playerName.Unbind();
            _partyId.Unbind();
        }

        public override bool Serialize(Archive archive)
        {
            LastSerializedArchiveVersion = archive.Version;

            bool success = base.Serialize(archive);

            if (archive.IsReplication == false) PlayerConnection.MigrationData.TransferMap(_mapDiscoveryDict, archive.IsPacking);

            if (archive.Version >= ArchiveVersion.AddedMissions)
                success &= Serializer.Transfer(archive, MissionManager);

            success &= Serializer.Transfer(archive, ref _avatarProperties);

            if (archive.IsTransient)
            {
                success &= Serializer.Transfer(archive, ref _shardId);
                success &= Serializer.Transfer(archive, ref _playerName);
                success &= Serializer.Transfer(archive, ref _consoleAccountIds[0]);
                success &= Serializer.Transfer(archive, ref _consoleAccountIds[1]);
                success &= Serializer.Transfer(archive, ref _secondaryPlayerName);
                success &= Serializer.Transfer(archive, MatchQueueStatus);
                success &= Serializer.Transfer(archive, ref _emailVerified);
                success &= Serializer.Transfer(archive, ref _accountCreationTimestamp);

                if (archive.IsReplication)
                {
                    success &= Serializer.Transfer(archive, ref _partyId);
                    success &= GuildMember.SerializeReplicationRuntimeInfo(archive, ref _guildId, ref _guildName, ref _guildMembership);

                    // Unused string, always empty
                    string emptyString = string.Empty;
                    success &= Serializer.Transfer(archive, ref emptyString);
                }
            }

            bool hasCommunityData = /* archive.IsPersistent || */ archive.IsMigration ||    // REMOVEME/TODO: Persistent communities
                (archive.IsReplication && archive.HasReplicationPolicy(AOINetworkPolicyValues.AOIChannelOwner));
            success &= Serializer.Transfer(archive, ref hasCommunityData);
            if (hasCommunityData)
                success &= Serializer.Transfer(archive, ref _community);

            // Unused bool, always false
            bool unkBool = false;
            success &= Serializer.Transfer(archive, ref unkBool);

            success &= Serializer.Transfer(archive, ref _unlockedInventoryList);

            if (archive.IsMigration || (archive.IsReplication && archive.HasReplicationPolicy(AOINetworkPolicyValues.AOIChannelOwner)))
                success &= Serializer.Transfer(archive, ref _badges);

            success &= Serializer.Transfer(archive, GameplayOptions);

            // Achievement state (NOTE: This is not saved in persistent archives in the client)
            if (archive.IsPersistent || archive.IsMigration || (archive.IsReplication && archive.HasReplicationPolicy(AOINetworkPolicyValues.AOIChannelOwner)))
                success &= Serializer.Transfer(archive, AchievementState);

            success &= Serializer.Transfer(archive, ref _stashTabOptionsDict);

            if (archive.InvolvesClient == false)
            {
                // TODO: Serialize map discovery data

                if (archive.Version >= ArchiveVersion.AddedVendorPurchaseData)
                {
                    uint numVendorPurchaseData = (uint)_vendorPurchaseDataDict.Count;
                    success &= Serializer.Transfer(archive, ref numVendorPurchaseData);

                    if (archive.IsPacking)
                    {
                        foreach (VendorPurchaseData purchaseData in _vendorPurchaseDataDict.Values)
                            success &= Serializer.Transfer(archive, purchaseData);
                    }
                    else
                    {
                        _vendorPurchaseDataDict.Clear();

                        for (uint i = 0; i < numVendorPurchaseData; i++)
                        {
                            VendorPurchaseData purchaseData = new(PrototypeId.Invalid);
                            success &= Serializer.Transfer(archive, ref purchaseData);

                            if (_vendorPurchaseDataDict.TryAdd(purchaseData.InventoryProtoRef, purchaseData) == false)
                                Logger.Warn($"Serialize(): Failed to add deserialized vendor purchase data {purchaseData}");
                        }
                    }
                }

                if (archive.Version >= ArchiveVersion.ImplementedLoginRewards)
                {
                    success &= Serializer.Transfer(archive, ref _loginCount);
                    success &= Serializer.Transfer(archive, ref _loginRewardCooldownTimeStart);
                }
            }

            return success;
        }

        public override void EnterGame(EntitySettings settings = null)
        {
            SendMessage(NetMessageMarkFirstGameFrame.CreateBuilder()
                .SetCurrentservergametime((ulong)Game.CurrentTime.TotalMilliseconds)
                .SetCurrentservergameid(Game.Id)
                .SetGamestarttime((ulong)Game.StartTime.TotalMilliseconds)
                .Build());

            SendMessage(NetMessageServerVersion.CreateBuilder().SetVersion(Game.Version).Build());
            SendMessage(Game.LiveTuningData.GetLiveTuningUpdate());

            SendMessage(NetMessageLocalPlayer.CreateBuilder()
                .SetLocalPlayerEntityId(Id)
                .SetGameOptions(Game.GameOptions)
                .Build());

            SendMessage(NetMessageReadyForTimeSync.DefaultInstance);

            // Enter game to become added to the AOI
            base.EnterGame(settings);

            OnEnterGameInitStashTabOptions();

            InitializeVendors();
            ScheduleCheckHoursPlayedEvent();
            UpdateUISystemLocks();
        }

        public override void ExitGame()
        {
            CancelPlayerTrade();

            SendMessage(NetMessageBeginExitGame.DefaultInstance);
            AOI.SetRegion(0, true);

            base.ExitGame();
        }

        public override void Destroy()
        {
            var region = GetRegion();
            if (region != null)
                MissionManager.Shutdown(region);

            LeaderboardManager.Destroy();

            base.Destroy();
        }

        public override void OnDeallocate()
        {
            Game?.GameEventScheduler?.CancelAllEvents(_pendingEvents);

            MissionManager.Deallocate();
            AchievementManager.Deallocate();
            Game.EntityManager.RemovePlayer(this);
            base.OnDeallocate();
        }

        /// <summary>
        /// Returns the name of the player for the specified <see cref="PlayerAvatarIndex"/>.
        /// </summary>
        public string GetName(PlayerAvatarIndex avatarIndex = PlayerAvatarIndex.Primary)
        {
            if ((avatarIndex >= PlayerAvatarIndex.Primary && avatarIndex < PlayerAvatarIndex.Count) == false)
                Logger.Warn("GetName(): avatarIndex out of range");

            if (avatarIndex == PlayerAvatarIndex.Secondary)
                return _secondaryPlayerName.Get();

            return _playerName.Get();
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

        public void SetGameplayOptions(NetMessageSetPlayerGameplayOptions clientOptions)
        {
            GameplayOptions newOptions = new(clientOptions.OptionsData);
            GameplayOptions = newOptions;

            // TODO: Update chat channels
            CurrentAvatar?.UpdateAvatarSynergyExperienceBonus();
        }

        #region Public Event Team

        public PrototypeId GetPublicEventTeam(PublicEventPrototype eventProto)
        {
            int eventInstance = eventProto.GetEventInstance();
            var teamProp = new PropertyId(PropertyEnum.PublicEventTeamAssignment, eventProto.DataRef, eventInstance);
            return Properties[teamProp];
        }

        public PublicEventTeamPrototype GetPublicEventTeamPrototype()
        {
            // TODO PropertyEnum.PublicEventTeamAssignment
            // GetPublicEventTeam(ActivePublicEvent).As<PublicEventTeamPrototype>()
            //return ((PrototypeId)12697619663363773645).As<PublicEventTeamPrototype>();  // Events/PublicEvents/Teams/CivilWarAntiReg.prototype
            return null;
        }

        #endregion

        #region Region

        public Region GetRegion()
        {
            // This shouldn't need any null checks, at least for now
            return AOI.Region;
        }

        public void UpdateSpawnMap(Vector3 position)
        {
            var region = GetRegion();
            if (region == null || _spawnGimbal == null) return;
            if (_spawnGimbal.ProjectGimbalPosition(region.Aabb, position, out Point2 coord) == false) return;
            if (_spawnGimbal.Coord == coord) return;

            bool inGimbal = _spawnGimbal.InGimbal(coord);
            _spawnGimbal.UpdateGimbal(coord);
            if (inGimbal) return;

            Aabb volume = _spawnGimbal.HorizonVolume(position);
            foreach (var area in region.IterateAreas(volume))
                if (area.SpawnMap != null)
                    area.PopulationArea?.UpdateSpawnMap(position);
        }

        public bool ViewedRegion(ulong regionId)
        {
            return PlayerConnection.WorldView.ContainsRegionInstanceId(regionId);
        }

        #endregion

        #region Inventories

        /// <summary>
        /// Returns <see langword="true"/> if the inventory with the specified <see cref="PrototypeId"/> is unlocked for this <see cref="Player"/>.
        /// </summary>
        public bool IsInventoryUnlocked(PrototypeId invProtoRef)
        {
            if (invProtoRef == PrototypeId.Invalid)
                return Logger.WarnReturn(false, $"IsInventoryUnlocked(): invProtoRef == PrototypeId.Invalid");

            return _unlockedInventoryList.Contains(invProtoRef);
        }

        /// <summary>
        /// Unlocks the inventory with the specified <see cref="PrototypeId"/> for this <see cref="Player"/>.
        /// </summary>
        public bool UnlockInventory(PrototypeId invProtoRef)
        {
            if (GetInventoryByRef(invProtoRef) != null)
                return Logger.WarnReturn(false, $"UnlockInventory(): {GameDatabase.GetFormattedPrototypeName(invProtoRef)} already exists");

            if (_unlockedInventoryList.Contains(invProtoRef))
                return Logger.WarnReturn(false, $"UnlockInventory(): {GameDatabase.GetFormattedPrototypeName(invProtoRef)} is already unlocked");

            _unlockedInventoryList.Add(invProtoRef);

            if (AddInventory(invProtoRef) == false || GetInventoryByRef(invProtoRef) == null)
                return Logger.WarnReturn(false, $"UnlockInventory(): Failed to add {GameDatabase.GetFormattedPrototypeName(invProtoRef)}");

            if (Inventory.IsPlayerStashInventory(invProtoRef))
                StashTabInsert(invProtoRef, 0);

            // Send unlock to the client
            var inventoryUnlockMessage = NetMessageInventoryUnlock.CreateBuilder()
                .SetInvProtoId((ulong)invProtoRef)
                .Build();

            SendMessage(inventoryUnlockMessage);

            return true;
        }

        /// <summary>
        /// Returns <see cref="PrototypeId"/> values of all locked and/or unlocked stash tabs for this <see cref="Player"/>.
        /// </summary>
        public bool GetStashInventoryProtoRefs(List<PrototypeId> stashInvRefs, bool getLocked, bool getUnlocked)
        {
            if (Prototype is not PlayerPrototype playerProto) return Logger.WarnReturn(false, "GetStashInventoryProtoRefs(): Prototype is not PlayerPrototype playerProto");
            if (playerProto.StashInventories.IsNullOrEmpty()) return Logger.WarnReturn(false, "GetStashInventoryProtoRefs(): playerProto.StashInventories.IsNullOrEmpty()");

            foreach (EntityInventoryAssignmentPrototype invAssignmentProto in playerProto.StashInventories)
            {
                if (invAssignmentProto.Inventory == PrototypeId.Invalid)
                {
                    Logger.Warn("GetStashInventoryProtoRefs(): invAssignmentProto.Inventory == PrototypeId.Invalid");
                    continue;
                }

                bool isLocked = GetInventoryByRef(invAssignmentProto.Inventory) == null;

                if (isLocked && getLocked || isLocked == false && getUnlocked)
                    stashInvRefs.Add(invAssignmentProto.Inventory);
            }

            return true;
        }

        /// <summary>
        /// Updates <see cref="StashTabOptions"/> with the data from a <see cref="NetMessageStashTabOptions"/>.
        /// </summary>
        public bool UpdateStashTabOptions(NetMessageStashTabOptions optionsMessage)
        {
            PrototypeId inventoryRef = (PrototypeId)optionsMessage.InventoryRefId;

            if (Inventory.IsPlayerStashInventory(inventoryRef) == false)
                return Logger.WarnReturn(false, $"UpdateStashTabOptions(): {inventoryRef} is not a player stash ref");

            if (GetInventoryByRef(inventoryRef) == null)
                return Logger.WarnReturn(false, $"UpdateStashTabOptions(): Inventory {GameDatabase.GetFormattedPrototypeName(inventoryRef)} not found");

            if (_stashTabOptionsDict.TryGetValue(inventoryRef, out StashTabOptions options) == false)
            {
                options = new();
                _stashTabOptionsDict.Add(inventoryRef, options);
            }

            // Stash tab names can be up to 30 characters long
            if (optionsMessage.HasDisplayName)
            {
                string displayName = optionsMessage.DisplayName;
                if (displayName.Length > 30)
                    displayName = displayName.Substring(0, 30);
                options.DisplayName = displayName;
            }

            if (optionsMessage.HasIconPathAssetId)
                options.IconPathAssetId = (AssetId)optionsMessage.IconPathAssetId;

            if (optionsMessage.HasColor)
                options.Color = (StashTabColor)optionsMessage.Color;

            return true;
        }

        /// <summary>
        /// Inserts the stash tab with the specified <see cref="PrototypeId"/> into the specified position.
        /// </summary>
        public bool StashTabInsert(PrototypeId insertedStashRef, int newSortOrder)
        {
            if (newSortOrder < 0)
                return Logger.WarnReturn(false, $"StashTabInsert(): Invalid newSortOrder {newSortOrder}");

            if (insertedStashRef == PrototypeId.Invalid)
                return Logger.WarnReturn(false, $"StashTabInsert(): Invalid insertedStashRef {insertedStashRef}");

            if (Inventory.IsPlayerStashInventory(insertedStashRef) == false)
                return Logger.WarnReturn(false, $"StashTabInsert(): insertedStashRef {insertedStashRef} is not a player stash ref");

            if (GetInventoryByRef(insertedStashRef) == null)
                return Logger.WarnReturn(false, $"StashTabInsert(): Inventory {GameDatabase.GetFormattedPrototypeName(insertedStashRef)} not found");

            // Get options for the tab we need to insert
            if (_stashTabOptionsDict.TryGetValue(insertedStashRef, out StashTabOptions options))
            {
                // Only new tabs are allowed to be in the same location
                if (options.SortOrder == newSortOrder)
                    return Logger.WarnReturn(false, "StashTabInsert(): Inserting an existing tab at the same location");
            }
            else
            {
                // Create options of the tab if there are none yet
                options = new();
                _stashTabOptionsDict.Add(insertedStashRef, options);
            }

            // No need to sort if only have a single tab
            if (_stashTabOptionsDict.Count == 1)
                return true;

            // Assign the new sort order to the tab
            int oldSortOrder = options.SortOrder;
            options.SortOrder = newSortOrder;

            // Rearrange other tabs
            int sortIncrement, sortStart, sortFinish;

            if (oldSortOrder < newSortOrder)
            {
                // If the sort order is increasing we need to shift back everything in between
                sortIncrement = -1;
                sortStart = oldSortOrder;
                sortFinish = newSortOrder;
            }
            else
            {
                // If the sort order is decreasing we need to shift forward everything in between
                sortIncrement = 1;
                sortStart = newSortOrder;
                sortFinish = oldSortOrder;
            }

            // Fall back in case our sort order overflows for some reason
            SortedList<int, PrototypeId> sortedTabs = new();
            bool orderOverflow = false;

            // Update sort order for all tabs
            foreach (var kvp in _stashTabOptionsDict)
            {
                PrototypeId sortRef = kvp.Key;
                StashTabOptions sortOptions = kvp.Value;

                if (sortRef != insertedStashRef)
                {
                    // Move the tab if:
                    // 1. We are adding a new tab and everything needs to be shifted
                    // 2. We are within our sort range
                    bool isNew = oldSortOrder == newSortOrder && sortOptions.SortOrder >= newSortOrder;
                    bool isWithinSortRange = sortOptions.SortOrder >= sortStart && sortOptions.SortOrder <= sortFinish;

                    if (isNew || isWithinSortRange)
                        sortOptions.SortOrder += sortIncrement;
                }

                // Make sure our sort order does not exceed the amount of stored stash tab options
                sortedTabs[sortOptions.SortOrder] = sortRef;
                if (sortOptions.SortOrder >= _stashTabOptionsDict.Count)
                    orderOverflow = true;
            }

            // Reorder if our sort order overflows
            if (orderOverflow)
            {
                Logger.Warn($"StashTabInsert(): Sort order overflow, reordering");
                int fixedOrder = 0;
                foreach (var kvp in sortedTabs)
                {
                    _stashTabOptionsDict[kvp.Value].SortOrder = fixedOrder;
                    fixedOrder++;
                }
            }

            return true;
        }

        public bool OnStashInventoryViewed(PrototypeId stashInventoryProtoRef)
        {
            if (stashInventoryProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "OnStashInventoryViewed(): stashInventoryProtoRef == PrototypeId.Invalid");

            int newItemCount = Properties[PropertyEnum.StashNewItemCount, stashInventoryProtoRef];
            if (newItemCount == 0)
                return true;

            Properties.RemoveProperty(new(PropertyEnum.StashNewItemCount, stashInventoryProtoRef));

            Inventory inventory = GetInventoryByRef(stashInventoryProtoRef);
            if (inventory == null) return Logger.WarnReturn(false, "OnStashInventoryViewed(): inventory == null");

            EntityManager entityManager = Game.EntityManager;

            foreach (var entry in inventory)
            {
                Item item = entityManager.GetEntity<Item>(entry.Id);
                if (item == null)
                {
                    Logger.Warn("OnStashInventoryViewed(): item == null");
                    continue;
                }

                item.Properties[PropertyEnum.ItemRecentlyAddedGlint] = false;
            }

            return true;
        }

        public bool RevealInventory(InventoryPrototype inventoryProto)
        {
            // Validate inventory prototype
            if (inventoryProto == null) return Logger.WarnReturn(false, "RevealInventory(): inventoryPrototype == null");

            // Skip reveal if this inventory does not require flagged visibility
            if (inventoryProto.InventoryRequiresFlaggedVisibility() == false)
                return true;

            // Validate inventory
            Inventory inventory = GetInventoryByRef(inventoryProto.DataRef);
            if (inventory == null) return Logger.WarnReturn(false, "RevealInventory(): inventory == null");

            // Skip reveal if already visible
            if (inventory.VisibleToOwner) return true;

            // Enable visibility
            inventory.VisibleToOwner = true;

            // Update interest for all contained entities
            EntityManager entityManager = Game.EntityManager;

            foreach (var entry in inventory)
            {
                Entity entity = entityManager.GetEntity<Entity>(entry.Id);
                if (entity == null)
                {
                    Logger.Warn("RevealInventory(): entity == null");
                    continue;
                }

                AOI.ConsiderEntity(entity);
            }

            return true;
        }

        public override void OnOtherEntityAddedToMyInventory(Entity entity, InventoryLocation invLoc, bool unpackedArchivedEntity)
        {
            base.OnOtherEntityAddedToMyInventory(entity, invLoc, unpackedArchivedEntity);

            if (entity is Avatar avatar)
            {
                avatar.SetPlayer(this);

                if (invLoc.InventoryConvenienceLabel == InventoryConvenienceLabel.AvatarInPlay && invLoc.Slot == 0)
                    CurrentAvatar = avatar;
            }

            if (IsInGame == false || entity is not Item item)
                return;

            InventoryCategory category = invLoc.InventoryCategory;
            InventoryConvenienceLabel convenienceLabel = invLoc.InventoryConvenienceLabel;

            if (convenienceLabel == InventoryConvenienceLabel.General ||
                convenienceLabel == InventoryConvenienceLabel.TeamUpGeneral ||
                convenienceLabel == InventoryConvenienceLabel.PvP)
            {
                // Assign the item's OnUse power to the current avatar
                Avatar currentAvatar = CurrentAvatar;
                if (currentAvatar?.IsInWorld == true)
                {
                    ItemPrototype itemProto = item.ItemPrototype;
                    if (itemProto == null) return;

                    PrototypeId powerProtoRef = item.OnUsePower;

                    if (powerProtoRef != PrototypeId.Invalid &&
                        currentAvatar.HasPowerInPowerCollection(powerProtoRef) == false &&
                        (itemProto.AbilitySettings == null || itemProto.AbilitySettings.OnlySlottableWhileEquipped == false))
                    {
                        int characterLevel = currentAvatar.CharacterLevel;
                        int combatLevel = currentAvatar.CombatLevel;
                        int itemLevel = item.Properties[PropertyEnum.ItemLevel];
                        float itemVariation = item.Properties[PropertyEnum.ItemVariation];
                        PowerIndexProperties indexProps = new(0, characterLevel, combatLevel, itemLevel, itemVariation);

                        if (currentAvatar.AssignPower(powerProtoRef, indexProps) == null)
                        {
                            Logger.Warn($"OnOtherEntityAddedToMyInventory(): Failed to assign item power {powerProtoRef.GetName()} to avatar {currentAvatar}");
                            return;
                        }
                    }
                }
            }

            // Highlight items that get put into stash tabs different from the current one
            if (invLoc.InventoryRef != CurrentOpenStashPagePrototypeRef &&
                (category == InventoryCategory.PlayerStashGeneral ||
                category == InventoryCategory.PlayerStashAvatarSpecific ||
                category == InventoryCategory.PlayerStashTeamUpGear))
            {
                item.Properties[PropertyEnum.ItemRecentlyAddedGlint] = true;
                Properties.AdjustProperty(1, new(PropertyEnum.StashNewItemCount, invLoc.InventoryRef));
            }
        }

        public override void OnOtherEntityRemovedFromMyInventory(Entity entity, InventoryLocation invLoc)
        {
            base.OnOtherEntityRemovedFromMyInventory(entity, invLoc);

            if (IsInGame == false || entity is not Item item)
                return;

            InventoryCategory category = invLoc.InventoryCategory;
            InventoryConvenienceLabel convenienceLabel = invLoc.InventoryConvenienceLabel;

            if (convenienceLabel == InventoryConvenienceLabel.General ||
                convenienceLabel == InventoryConvenienceLabel.TeamUpGeneral ||
                convenienceLabel == InventoryConvenienceLabel.PvP)
            {
                // Unassign the item's OnUse power from the current avatar as long as there are no other item that grant it
                Avatar currentAvatar = CurrentAvatar;
                if (currentAvatar?.IsInWorld == true)
                {
                    ItemPrototype itemProto = item.ItemPrototype;
                    if (itemProto == null) return;

                    PrototypeId powerProtoRef = item.OnUsePower;

                    if (powerProtoRef != PrototypeId.Invalid &&
                        currentAvatar.HasPowerInPowerCollection(powerProtoRef) &&
                        (itemProto.AbilitySettings == null || itemProto.AbilitySettings.OnlySlottableWhileEquipped == false))
                    {
                        if (currentAvatar.FindAbilityItem(itemProto, item.Id) == InvalidId)
                            currentAvatar.UnassignPower(powerProtoRef);
                    }
                }
            }
        }

        public bool TrashItem(Item item)
        {
            // See CPlayer::RequestItemTrash for reference

            // Make sure this player is allowed to destroy this item
            if (item.PlayerCanDestroy(this) == false)
                return false;

            Avatar avatar = CurrentAvatar;

            // Make sure there is an avatar in the world
            if (avatar.IsInWorld == false)
                return false;

            // Destroy the item if it cannot be dropped
            if (item.WouldBeDestroyedOnDrop)
            {
                item.Destroy();
                return true;
            }

            // Drop item to the ground
            Region region = avatar.Region;

            // Find a position to drop
            if (region.ChooseRandomPositionNearPoint(avatar.Bounds, PathFlags.Walk, PositionCheckFlags.CanBeBlockedEntity,
                BlockingCheckFlags.CheckSpawns, 50f, 100f, out Vector3 dropPosition) == false)
            {
                // Fall back to avatar position if didn't find a free spot
                dropPosition = avatar.RegionLocation.Position;

                // Make sure we don't drop it somewhere where it can't be picked up (e.g. if the avatar is flying above something)
                if (region.NaviMesh.Contains(dropPosition, item.Bounds.Radius, new DefaultContainsPathFlagsCheck(PathFlags.Walk)) == false)
                    return false;
            }

            // Remove the item from its inventory (no going back now)
            item.ChangeInventoryLocation(null);

            // Drop it
            using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
            settings.OptionFlags |= EntitySettingsOptionFlags.IsNewOnServer;
            settings.SourceEntityId = avatar.Id;
            settings.SourcePosition = avatar.RegionLocation.Position;

            if (item.EnterWorld(region, dropPosition, Orientation.Zero, settings) == false)
            {
                item.Destroy();     // We have to destroy this item because it's no longer in player's inventory
                return Logger.WarnReturn(false, $"TrashItem(): Item {item} failed to enter world");
            }

            // Reapply lifespan
            float expirationTimeMult = Math.Max(Game.CustomGameOptions.TrashedItemExpirationTimeMultiplier, 0f);
            TimeSpan expirationTime = item.GetExpirationTime() * expirationTimeMult;
            item.ResetLifespan(expirationTime);

            return true;
        }

        public bool CanAcquireCurrencyItem(WorldEntity entity)
        {
            if (entity.IsCurrencyItem() == false)
                return false;

            foreach (var kvp in entity.Properties.IteratePropertyRange(PropertyEnum.ItemCurrency))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId currencyProtoRef);
                CurrencyPrototype currencyProto = currencyProtoRef.As<CurrencyPrototype>();
                if (currencyProto == null)
                {
                    Logger.Warn("CanAcquireCurrencyItem(): currencyProto == null");
                    continue;
                }

                int currentAmount = Properties[PropertyEnum.ItemCurrency, currencyProtoRef];
                int delta = kvp.Value;

                if (currencyProto.MaxAmount > 0 && currentAmount + delta > currencyProto.MaxAmount)
                    return false;
            }

            return true;
        }

        public InventoryResult AcquireItem(Item item, PrototypeId inventoryProtoRef)
        {
            if (item == null) return Logger.WarnReturn(InventoryResult.InvalidSourceEntity, "AcquireItem(): item == null");

            if (AcquireCurrencyItem(item))
            {
                item.Destroy();
                return InventoryResult.Success;
            }

            Inventory inventory = inventoryProtoRef != PrototypeId.Invalid
                ? GetInventoryByRef(inventoryProtoRef)
                : GetInventory(InventoryConvenienceLabel.General);

            if (inventory == null)
                return InventoryResult.NoAvailableInventory;

            ulong? stackEntityId = InvalidId;
            InventoryResult result = item.ChangeInventoryLocation(inventory, Inventory.InvalidSlot, ref stackEntityId, true);

            if (result == InventoryResult.Success)
            {
                // Update our item reference if it got stacked and mark it as recently added
                if (stackEntityId != InvalidId)
                    item = Game.EntityManager.GetEntity<Item>(stackEntityId.Value);

                item?.SetRecentlyAdded(true);
            }
            else
            {
                // Handle overflow
                Inventory deliveryBox = GetInventory(InventoryConvenienceLabel.DeliveryBox);
                if (deliveryBox == null)
                    return InventoryResult.NoAvailableInventory;

                result = item.ChangeInventoryLocation(deliveryBox);

                if (result != InventoryResult.Success)
                {
                    // Second level of overflow - this should not happen under normal circumstances
                    Logger.Warn($"AcquireItem(): Failed to add item {item} to the delivery box for player {this} for reason {result}, moving this item to the error recovery inventory");

                    Inventory errorRecovery = GetInventory(InventoryConvenienceLabel.ErrorRecovery);
                    if (errorRecovery == null)
                        return Logger.WarnReturn(InventoryResult.NoAvailableInventory, $"AcquireItem(): Error recovery inventory is not available for item {item}, player {this}");

                    result = item.ChangeInventoryLocation(errorRecovery);
                }
            }

            return result;
        }

        public bool AcquireCredits(int amount)
        {
            if (amount <= 0)
                return false;

            CurrencyPrototype creditsProto = GameDatabase.CurrencyGlobalsPrototype.CreditsPrototype;
            Properties.AdjustProperty(amount, new(PropertyEnum.Currency, creditsProto.DataRef));
            OnScoringEvent(new(ScoringEventType.CurrencyCollected, creditsProto, amount));

            return true;
        }

        public bool AcquireCurrencyItem(Entity entity)
        {
            if (entity.IsCurrencyItem() == false)
                return false;

            bool result = false;

            foreach (var kvp in entity.Properties.IteratePropertyRange(PropertyEnum.ItemCurrency))
            {
                int delta = kvp.Value * entity.CurrentStackSize;
                if (delta <= 0)
                    continue;

                Property.FromParam(kvp.Key, 0, out PrototypeId currencyProtoRef);
                CurrencyPrototype currencyProto = currencyProtoRef.As<CurrencyPrototype>();
                if (currencyProto == null)
                {
                    Logger.Warn("AcquireCurrencyItem(): currencyProto == null");
                    continue;
                }

                int currentAmount = Properties[PropertyEnum.ItemCurrency, currencyProtoRef];
                if (currencyProto.MaxAmount > 0 && currentAmount + delta > currencyProto.MaxAmount)
                    continue;

                var propId = new PropertyId (PropertyEnum.Currency, currencyProtoRef);
                Properties.AdjustProperty(delta, propId);
                GetRegion()?.CurrencyCollectedEvent.Invoke(new(this, currencyProtoRef, Properties[propId]));
                OnScoringEvent(new(ScoringEventType.CurrencyCollected, currencyProto, delta));

                result = true;
            }

            int runestonesAmount = entity.Properties[PropertyEnum.RunestonesAmount];
            if (runestonesAmount > 0)
            {
                Properties.AdjustProperty(runestonesAmount, new(PropertyEnum.RunestonesAmount));
                result = true;
            }

            return result;
        }

        public bool AcquireGazillionite(long amount)
        {
            if (amount <= 0) return Logger.WarnReturn(false, "AcquireGazillionite(): amount <= 0");

            long balance = GazillioniteBalance;
            balance += amount;
            GazillioniteBalance = balance;

            SendMessage(NetMessageGrantGToPlayerNotification.CreateBuilder()
                .SetDidSucceed(true)
                .SetCurrentCurrencyBalance(balance)
                .Build());

            return true;
        }

        public bool AwardBonusItemFindPoints(int amount, LootInputSettings settings)
        {
            if (amount <= 0)
                return true;

            Avatar avatar = CurrentAvatar;
            if (avatar == null) return Logger.WarnReturn(false, "AwardBonusItemFindPoints(): avatar == null");

            int bonusItemFindRating = avatar.Properties[PropertyEnum.BonusItemFindRating];
            if (bonusItemFindRating <= 0)
                return true;

            LootGlobalsPrototype lootGlobalsProto = GameDatabase.LootGlobalsPrototype;
            Curve bonusItemFindCurve = GameDatabase.LootGlobalsPrototype.BonusItemFindCurve.AsCurve();
            if (bonusItemFindCurve == null) return Logger.WarnReturn(false, "AwardBonusItemFindPoints(): bonusItemFindCurve == null");

            amount = (int)(amount * bonusItemFindCurve.GetAt(bonusItemFindRating));
            if (amount <= 0)
                return true;

            int points = Properties[PropertyEnum.BonusItemFindPoints] + amount;
            if (points >= lootGlobalsProto.BonusItemFindNumPointsForBonus)
            {
                Game.LootManager.GiveLootFromTable(lootGlobalsProto.BonusItemFindLootTable, settings);
                points -= lootGlobalsProto.BonusItemFindNumPointsForBonus;
            }

            Properties[PropertyEnum.BonusItemFindPoints] = points;
            return true;
        }

        public bool InitPowerFromCreationItem(Item item)
        {
            // Only the current avatar is in the world and can have powers, so it's pointless to use AvatarIterator here like the client does
            if (item.GetOwnerOfType<Player>() != this) return Logger.WarnReturn(false, "InitPowerFromCreationItem(): item.GetOwnerOfType<Player>() != this");
            
            CurrentAvatar?.InitPowerFromCreationItem(item);
            return true;
        }

        protected override bool InitInventories(bool populateInventories)
        {
            bool success = base.InitInventories(populateInventories);

            PlayerPrototype playerProto = Prototype as PlayerPrototype;
            if (playerProto == null) return Logger.WarnReturn(false, "InitInventories(): playerProto == null");

            foreach (EntityInventoryAssignmentPrototype invEntryProto in playerProto.StashInventories)
            {
                var stashInvProto = invEntryProto.Inventory.As<PlayerStashInventoryPrototype>();
                if (stashInvProto == null)
                {
                    Logger.Warn("InitInventories(): stashInvProto == null");
                    continue;
                }

                if (stashInvProto.IsPlayerStashInventory && IsUsingUnifiedStash == false && stashInvProto.ConvenienceLabel == InventoryConvenienceLabel.UnifiedStash)
                    continue;

                if (stashInvProto.LockedByDefault == false)
                {
                    if (AddInventory(invEntryProto.Inventory) == false)
                    {
                        Logger.Warn($"InitInventories(): Failed to add inventory, invProtoRef={GameDatabase.GetPrototypeName(invEntryProto.Inventory)}");
                        success = false;
                    }
                }
            }

            return success;
        }

        /// <summary>
        /// Initializes <see cref="StashTabOptions"/> for any stash tabs that are unlocked but don't have any options yet.
        /// </summary>
        private void OnEnterGameInitStashTabOptions()
        {
            List<PrototypeId> stashInvRefs = ListPool<PrototypeId>.Instance.Get();
            if (GetStashInventoryProtoRefs(stashInvRefs, false, true))
            {
                foreach (PrototypeId stashRef in stashInvRefs)
                {
                    if (_stashTabOptionsDict.ContainsKey(stashRef) == false)
                        StashTabInsert(stashRef, 0);
                }
            }
            else
            {
                Logger.Warn("OnEnterGameInitStashTabOptions()(): GetStashInventoryProtoRefs(stashInvRefs, false, true) == false");
            }

            ListPool<PrototypeId>.Instance.Return(stashInvRefs);
        }

        #endregion

        #region Badges

        /// <summary>
        /// Add the specified badge to this <see cref="Player"/>. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool AddBadge(AvailableBadges badge)
        {
            return _badges.SortedInsert(badge);
        }

        /// <summary>
        /// Removes the specified badge from this <see cref="Player"/>. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool RemoveBadge(AvailableBadges badge)
        {
            return _badges.Remove(badge);
        }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="Player"/> has the specified badge.
        /// </summary>
        public bool HasBadge(AvailableBadges badge)
        {
            return _badges.Contains(badge);
        }

        #endregion

        #region Tags

        public void AddTag(WorldEntity entity)
        {
            _tagEntities.Add(entity.Id);
        }

        public void RemoveTag(WorldEntity entity)
        {
            _tagEntities.Remove(entity.Id);
        }

        #endregion

        #region Avatar and Team-Up Management

        public Avatar GetAvatar(PrototypeId avatarProtoRef, AvatarMode avatarMode = AvatarMode.Normal)
        {
            if (avatarProtoRef == PrototypeId.Invalid) return Logger.WarnReturn<Avatar>(null, "GetAvatar(): avatarProtoRef == PrototypeId.Invalid");

            AvatarIterator iterator = new(this, AvatarIteratorMode.IncludeArchived, avatarProtoRef);
            AvatarIterator.Enumerator enumerator = iterator.GetEnumerator();
            if (enumerator.MoveNext())
                return enumerator.Current;

            return null;
        }

        public Avatar GetActiveAvatarById(ulong avatarEntityId)
        {
            // TODO: Secondary avatar for consoles?
            return (CurrentAvatar.Id == avatarEntityId) ? CurrentAvatar : null;
        }

        public Avatar GetActiveAvatarByIndex(int index)
        {
            // TODO: Secondary avatar for consoles?
            return (index == 0) ? CurrentAvatar : null;
        }

        public Avatar CreateAvatar(PrototypeId avatarProtoRef)
        {
            AvatarPrototype avatarProto = avatarProtoRef.As<AvatarPrototype>();
            if (avatarProto == null) return Logger.WarnReturn<Avatar>(null, "CreateAvatar(): avatarProto == null");

            if (GetAvatar(avatarProtoRef) != null)
                return Logger.WarnReturn<Avatar>(null, $"CreateAvatar(): Player [{this}] is trying to create avatar {avatarProto} that is already unlocked");

            Inventory avatarLibrary = GetInventory(InventoryConvenienceLabel.AvatarLibrary);
            if (avatarLibrary == null) return Logger.WarnReturn<Avatar>(null, "CreateAvatar(): avatarLibrary == null");

            using EntitySettings avatarSettings = ObjectPoolManager.Instance.Get<EntitySettings>();
            avatarSettings.EntityRef = avatarProtoRef;
            avatarSettings.InventoryLocation = new(Id, avatarLibrary.PrototypeDataRef);

            Avatar avatar = Game.EntityManager.CreateEntity(avatarSettings) as Avatar;
            if (avatar == null) return Logger.ErrorReturn<Avatar>(null, "CreateAvatar(): avatar == null");

            avatar.InitializeLevel(1);
            avatar.ResetResources(false);
            avatar.GiveStartingCostume();

            return avatar;
        }

        public Agent GetTeamUpAgent(PrototypeId teamUpProtoRef)
        {
            if (teamUpProtoRef == PrototypeId.Invalid) return Logger.WarnReturn<Agent>(null, "GetTeamUpAgent(): teamUpProtoRef == PrototypeId.Invalid");

            Inventory teamUpInv = GetInventory(InventoryConvenienceLabel.TeamUpLibrary);
            if (teamUpInv == null) return Logger.WarnReturn<Agent>(null, "GetTeamUpAgent(): teamUpInv == null");

            return teamUpInv.GetMatchingEntity(teamUpProtoRef) as Agent;
        }

        public bool IsTeamUpAgentUnlocked(PrototypeId teamUpRef)
        {
            return GetTeamUpAgent(teamUpRef) != null;
        }

        public bool UnlockTeamUpAgent(PrototypeId teamUpRef, bool sendToClient = true)
        {
            if (Game.GameOptions.TeamUpSystemEnabled == false)
                return false;

            if (IsTeamUpAgentUnlocked(teamUpRef))
                return Logger.WarnReturn(false, $"UnlockTeamUpAgent(): Player [{this}] is trying to unlock team-up {teamUpRef.GetName()} that is already unlocked");

            AgentTeamUpPrototype teamUpProto = GameDatabase.GetPrototype<AgentTeamUpPrototype>(teamUpRef);
            if (teamUpProto == null) return Logger.WarnReturn(false, "UnlockTeamUpAgent(): teamUpProto == null");

            Inventory teamUpLibrary = GetInventory(InventoryConvenienceLabel.TeamUpLibrary);
            if (teamUpLibrary == null) return Logger.WarnReturn(false, "UnlockTeamUpAgent(): teamUpLibrary == null");

            using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
            settings.InventoryLocation = new(Id, teamUpLibrary.PrototypeDataRef);
            settings.EntityRef = teamUpRef;

            Agent teamUp = Game.EntityManager.CreateEntity(settings) as Agent;
            if (teamUp == null)
                return Logger.WarnReturn(false, $"UnlockTeamUpAgent(): Failed to create team-up agent entity {teamUpRef.GetName()} for player [{this}]");

            teamUp.InitializeLevel(1);
            teamUp.CombatLevel = 1;

            teamUp.Properties[PropertyEnum.PowerProgressionVersion] = teamUp.GetLatestPowerProgressionVersion();

            if (sendToClient)
                SendNewTeamUpAcquired(teamUpRef);

            GetRegion()?.PlayerUnlockedTeamUpEvent.Invoke(new(this, teamUpRef));

            return true;
        }

        public bool BeginAvatarSwitch(PrototypeId avatarProtoRef, bool ignoreGameplayRestrictions = false)
        {
            Avatar currentAvatar = CurrentAvatar;
            if (currentAvatar == null) return Logger.WarnReturn(false, "BeginAvatarSwitch(): currentAvatar == null");
            if (currentAvatar.IsInWorld == false) return Logger.WarnReturn(false, "BeginAvatarSwitch(): currentAvatar.IsInWorld == false");

            if (avatarProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "BeginAvatarSwitch(): avatarProtoRef == PrototypeId.Invalid");
            if (avatarProtoRef == currentAvatar.PrototypeDataRef) return Logger.WarnReturn(false, "BeginAvatarSwitch(): avatarProtoRef == currentAvatar.PrototypeDataRef");

            Avatar avatarToSwitchTo = GetAvatar(avatarProtoRef);

            SwitchToAvatarFailedReason failReason = SwitchToAvatarFailedReason.eSAFR_Unknown;
            CanSwitchAvatarResult canSwitch = CanSwitchToAvatar(currentAvatar, avatarToSwitchTo, ignoreGameplayRestrictions);
            if (canSwitch != CanSwitchAvatarResult.Success)
            {
                if (canSwitch == CanSwitchAvatarResult.NotAllowedInRegion)
                    failReason = SwitchToAvatarFailedReason.eSAFR_RegionRestrictionEval;

                currentAvatar.SendSwitchToAvatarFailedMessage(failReason);
                return false;
            }

            // TODO: Prefetch assets to ensure the swap-in animation always plays

            // Activate the swap power
            Power avatarSwapChannel = CurrentAvatar.GetPower(GameDatabase.GlobalsPrototype.AvatarSwapChannelPower);
            if (avatarSwapChannel == null) return Logger.WarnReturn(false, "BeginAvatarSwitch(): avatarSwapChannel == null");

            PowerActivationSettings settings = new(CurrentAvatar.Id, CurrentAvatar.RegionLocation.Position, CurrentAvatar.RegionLocation.Position);
            settings.Flags = PowerActivationSettingsFlags.NotifyOwner;
            PowerUseResult avatarSwapChannelResult = currentAvatar.ActivatePower(avatarSwapChannel.PrototypeDataRef, ref settings);

            if (avatarSwapChannelResult != PowerUseResult.Success)
            {
                if (avatarSwapChannelResult == PowerUseResult.RegionRestricted)
                    failReason = SwitchToAvatarFailedReason.eSAFR_RegionRestrictionKwd;

                Logger.Warn($"BeginAvatarSwitch(): Failed to activate swap channel power for avatar [{currentAvatar}], result={avatarSwapChannelResult}");
                currentAvatar.SendSwitchToAvatarFailedMessage(failReason);
                return false;
            }

            Properties.RemovePropertyRange(PropertyEnum.AvatarSwitchPending);
            Properties[PropertyEnum.AvatarSwitchPending, avatarProtoRef] = true;
            return true;
        }

        public void ScheduleSwitchAvatarEvent()
        {
            // Schedule avatar switch at the end of the current frame to let switch power application finish first
            ScheduleEntityEvent(_switchAvatarEvent, TimeSpan.Zero);
        }

        public bool SwitchAvatar()
        {
            // Retrieve pending avatar proto ref recorded in properties
            PrototypeId avatarProtoRef = PrototypeId.Invalid;

            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.AvatarSwitchPending))
            {
                Property.FromParam(kvp.Key, 0, out avatarProtoRef);
                break;
            }

            if (avatarProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "SwitchAvatar(): Failed to find pending avatar switch");

            // Get information about the previous avatar
            ulong lastCurrentAvatarId = CurrentAvatar != null ? CurrentAvatar.Id : InvalidId;
            ulong prevRegionId = CurrentAvatar.RegionLocation.RegionId;
            Vector3 prevPosition = CurrentAvatar.RegionLocation.Position;
            Orientation prevOrientation = CurrentAvatar.RegionLocation.Orientation;

            // Find the avatar to switch to
            Inventory avatarLibrary = GetInventory(InventoryConvenienceLabel.AvatarLibrary);
            Inventory avatarInPlay = GetInventory(InventoryConvenienceLabel.AvatarInPlay);

            if (avatarLibrary.GetMatchingEntity(avatarProtoRef) is not Avatar avatar)
                return Logger.WarnReturn(false, $"SwitchAvatar(): Failed to find avatar entity for avatarProtoRef {GameDatabase.GetPrototypeName(avatarProtoRef)}");

            // Remove non-persistent conditions from the current avatar
            ConditionCollection previousConditions = CurrentAvatar?.ConditionCollection;
            previousConditions?.RemoveAllConditions(false);

            // Do the switch
            InventoryResult result = avatar.ChangeInventoryLocation(avatarInPlay, 0);

            if (result != InventoryResult.Success)
                return Logger.WarnReturn(false, $"SwitchAvatar(): Failed to change library avatar's inventory location ({result})");

            IsSwitchingAvatar = true;

            // Transfer conditions to the new current avatar
            ConditionCollection currentConditions = CurrentAvatar?.ConditionCollection;
            if (previousConditions != null && currentConditions != null)
                currentConditions.TransferConditionsFrom(previousConditions);

            EnableCurrentAvatar(true, lastCurrentAvatarId, prevRegionId, prevPosition, prevOrientation);

            IsSwitchingAvatar = false;

            ScheduleCommunityBroadcast();

            GetRegion()?.PlayerSwitchedToAvatarEvent.Invoke(new(this, avatarProtoRef));

            return true;
        }

        public bool EnableCurrentAvatar(bool withSwapInPower, ulong lastCurrentAvatarId, ulong regionId, in Vector3 position, in Orientation orientation)
        {
            if (CurrentAvatar == null)
                return Logger.WarnReturn(false, "EnableCurrentAvatar(): CurrentAvatar == null");

            if (CurrentAvatar.IsInWorld)
                return Logger.WarnReturn(false, "EnableCurrentAvatar(): Current avatar is already active");

            Region region = Game.RegionManager.GetRegion(regionId);
            if (region == null)
                return Logger.WarnReturn(false, "EnableCurrentAvtar(): region == null");

            Logger.Trace($"EnableCurrentAvatar(): [{CurrentAvatar}] entering world in region [{region}]");

            // Disable initial visibility and schedule swap-in power if requested
            using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
            if (withSwapInPower)
            {
                settings.OptionFlags = EntitySettingsOptionFlags.IsClientEntityHidden;
                CurrentAvatar.ScheduleSwapInPower();
            }

            // Add new avatar to the world
            if (CurrentAvatar.EnterWorld(region, CurrentAvatar.FloorToCenter(position), orientation, settings) == false)
                return false;

            // Reset resources AFTER entering world because entering world initializes resources
            if (withSwapInPower)
                CurrentAvatar.ResetResources(true);

            OnChangeActiveAvatar(0, lastCurrentAvatarId);
            return true;
        }

        public int GetLevelCapForCharacter(PrototypeId agentProtoRef)
        {
            AgentPrototype agentProto = agentProtoRef.As<AgentPrototype>();

            switch (agentProto)
            {
                case AvatarPrototype avatarProto:
                    if (HasAvatarAsStarter(agentProtoRef))
                        return Avatar.GetStarterAvatarLevelCap();

                    return Avatar.GetAvatarLevelCap();

                case AgentTeamUpPrototype teamUpProto:
                    return Agent.GetTeamUpLevelCap();

                default:
                    return Logger.WarnReturn(0, $"GetLevelCapForCharacter(): Agent is neither an Avatar nor a Team-Up: [{agentProto}]");
            }
        }

        public void SetAvatarLibraryProperties()
        {
            if (CurrentAvatar == null)
            {
                // We should have a current avatar at this point
                Logger.Warn("SetAvatarLibraryProperties(): CurrentAvatar == null");
                return;
            }

            int maxAvatarLevel = 1;

            TimeSpan timePlayed = TimeSpan.Zero;
            int legendaryMissionsComplete = 0;
            int pvpWins = 0;
            int pvpLosses = 0;

            foreach (Avatar avatar in new AvatarIterator(this))
            {
                PrototypeId avatarProtoRef = avatar.PrototypeDataRef;

                // AvatarLibraryLevel will be set by running the level up logic in the avatar (see OnAvatarCharacterLevelChanged())
                Properties[PropertyEnum.AvatarLibraryCostume, 0, avatarProtoRef] = avatar.Properties[PropertyEnum.CostumeCurrent];
                Properties[PropertyEnum.AvatarLibraryTeamUp, 0, avatarProtoRef] = avatar.Properties[PropertyEnum.AvatarTeamUpAgent];

                // Update max level
                maxAvatarLevel = Math.Max(maxAvatarLevel, avatar.CharacterLevel);

                // Add statistics to total
                timePlayed += avatar.GetTimePlayed();
                legendaryMissionsComplete += avatar.Properties[PropertyEnum.LegendaryMissionsComplete];
                pvpWins += avatar.Properties[PropertyEnum.PvPWins];
                pvpLosses += avatar.Properties[PropertyEnum.PvPLosses];
            }

            Properties[PropertyEnum.PlayerMaxAvatarLevel] = maxAvatarLevel;

            Properties[PropertyEnum.AvatarTotalTimePlayed] = timePlayed;
            Properties[PropertyEnum.LegendaryMissionsComplete] = legendaryMissionsComplete;
            Properties[PropertyEnum.PvPWins] = pvpWins;
            Properties[PropertyEnum.PvPLosses] = pvpLosses;
        }

        public void SetTeamUpLibraryProperties()
        {
            if (Properties.HasProperty(PropertyEnum.TeamUpsAtMaxLevelPersistent))
                return;

            Inventory teamUpLibrary = GetInventory(InventoryConvenienceLabel.TeamUpLibrary);
            if (teamUpLibrary == null)
                return;

            EntityManager entityManager = Game.EntityManager;

            int teamUpsAtMaxLevel = 0;
            foreach (var entry in teamUpLibrary)
            {
                Agent teamUpAgent = entityManager.GetEntity<Agent>(entry.Id);
                if (teamUpAgent == null)
                {
                    Logger.Warn("SetTeamUpLibraryProperties(): teamUpAgent == null");
                    continue;
                }

                if (teamUpAgent.IsAtLevelCap)
                    teamUpsAtMaxLevel++;
            }

            if (teamUpsAtMaxLevel > 0)
                Properties[PropertyEnum.TeamUpsAtMaxLevelPersistent] = teamUpsAtMaxLevel;
        }

        public void OnChangeActiveAvatar(int avatarIndex, ulong lastCurrentAvatarId)
        {
            // TODO: Apply and remove avatar properties stored in the player

            SendMessage(NetMessageCurrentAvatarChanged.CreateBuilder()
                .SetAvatarIndex(avatarIndex)
                .SetLastCurrentEntityId(lastCurrentAvatarId)
                .Build());
        }

        public void OnAvatarCharacterLevelChanged(Avatar avatar)
        {
            int characterLevel = avatar.CharacterLevel;
            int prestigeLevel = avatar.PrestigeLevel;
            int levelCap = Avatar.GetAvatarLevelCap();
            int totalLevel = prestigeLevel * levelCap + characterLevel;

            Properties[PropertyEnum.AvatarLibraryLevel, (int)avatar.AvatarMode, avatar.PrototypeDataRef] = totalLevel;

            if (avatar.AvatarMode == AvatarMode.Normal)
            {
                OnScoringEvent(new(ScoringEventType.AvatarLevelTotal, totalLevel));
                OnScoringEvent(new(ScoringEventType.AvatarLevelTotalAllAvatars, ScoringEvents.GetPlayerAvatarsTotalLevels(this)));
            }

            // Update max avatar level for things like mode unlocks
            if (characterLevel > Properties[PropertyEnum.PlayerMaxAvatarLevel])
                Properties[PropertyEnum.PlayerMaxAvatarLevel] = characterLevel;

            UpdateUISystemLocks();
        }

        public bool CanUseLiveTuneBonuses()
        {
            float serverBonusUnlockLevelOverride = LiveTuningManager.GetLiveGlobalTuningVar(GlobalTuningVar.eGTV_ServerBonusUnlockLevelOverride);
            int playerMaxAvatarLevel = Properties[PropertyEnum.PlayerMaxAvatarLevel];

            if (serverBonusUnlockLevelOverride != LiveTuningData.DefaultTuningVarValue)
                return playerMaxAvatarLevel >= serverBonusUnlockLevelOverride;

            // NOTE: ServerBonusUnlockLevel is set to 60 in 1.52.
            return playerMaxAvatarLevel >= GameDatabase.GlobalsPrototype.ServerBonusUnlockLevel;
        }

        public bool HasAvatarFullyUnlocked(PrototypeId avatarRef)
        {
            AvatarUnlockType unlockType = GetAvatarUnlockType(avatarRef);
            return unlockType != AvatarUnlockType.None && unlockType != AvatarUnlockType.Starter;
        }

        public bool HasAvatarAsStarter(PrototypeId avatarRef)
        {
            AvatarUnlockType unlockType = GetAvatarUnlockType(avatarRef);
            return unlockType == AvatarUnlockType.Starter;
        }

        public bool HasAvatarAsCappedStarter(Avatar avatar)
        {
            return HasAvatarAsStarter(avatar.PrototypeDataRef) && avatar.CharacterLevel >= Avatar.GetStarterAvatarLevelCap();
        }

        public AvatarUnlockType GetAvatarUnlockType(PrototypeId avatarRef)
        {
            var avatarProto = GameDatabase.GetPrototype<AvatarPrototype>(avatarRef);
            if (avatarProto == null) return AvatarUnlockType.None;
            AvatarUnlockType unlockType = (AvatarUnlockType)(int)Properties[PropertyEnum.AvatarUnlock, avatarRef];
            if (unlockType == AvatarUnlockType.None && avatarProto.IsStarterAvatar)
                return AvatarUnlockType.Starter;
            return unlockType;
        }

        public bool UnlockAvatar(PrototypeId avatarRef, bool sendToClient)
        {
            AvatarUnlockType currentUnlockType = GetAvatarUnlockType(avatarRef);
            if (currentUnlockType != AvatarUnlockType.None && currentUnlockType != AvatarUnlockType.Starter)
                return false;

            Properties[PropertyEnum.AvatarUnlock, avatarRef] = (int)AvatarUnlockType.Type2;
            GetRegion()?.PlayerUnlockedAvatarEvent.Invoke(new(this, avatarRef));
            OnScoringEvent(new(ScoringEventType.AvatarsUnlocked, avatarRef.As<AvatarPrototype>(), 1));

            if (sendToClient)
                SendMessage(NetMessageNewAvatarAcquired.CreateBuilder().SetPrototypeId((ulong)avatarRef).Build());

            return true;
        }

        public int GetCharacterLevelForAvatar(PrototypeId avatarRef, AvatarMode avatarMode)
        {
            int levelCap = Avatar.GetAvatarLevelCap();
            if (levelCap <= 0) return 0;
            int level = Properties[PropertyEnum.AvatarLibraryLevel, (int)avatarMode, avatarRef];
            if (level <= 0) return 0;
            level %= levelCap;
            return level == 0 ? levelCap : level;
        }

        public int GetPrestigeLevelForAvatar(PrototypeId avatarRef, AvatarMode avatarMode)
        {
            int levelCap = Avatar.GetAvatarLevelCap();
            if (levelCap <= 0) return 0;
            int level = Properties[PropertyEnum.AvatarLibraryLevel, (int)avatarMode, avatarRef];
            if (level <= 0) return 0;
            return (level - 1) / levelCap;
        }

        public int GetMaxCharacterLevelAttainedForAvatar(PrototypeId avatarRef, AvatarMode avatarMode = AvatarMode.Normal)
        {
            return Math.Min(Properties[PropertyEnum.AvatarLibraryLevel, (int)avatarMode, avatarRef], Avatar.GetAvatarLevelCap());
        }

        private CanSwitchAvatarResult CanSwitchToAvatar(Avatar currentAvatar, Avatar avatarToSwitchTo, bool ignoreGameplayRestrictions)
        {
            if (currentAvatar == avatarToSwitchTo) return Logger.WarnReturn(CanSwitchAvatarResult.NotAllowedUnknown, "CanSwitchToAvatar(): currentAvatar == avatarToSwitchTo");

            // Check ownership
            if (avatarToSwitchTo.InventoryLocation.InventoryConvenienceLabel != InventoryConvenienceLabel.AvatarLibrary)
                return Logger.WarnReturn(CanSwitchAvatarResult.NotAllowedUnknown, $"CanSwitchToAvatar(): Attempting to switch to avatar not in avatar library\ncurrentAvatar=[{currentAvatar}], avatarToSwitchTo={avatarToSwitchTo}");

            if (Owns(avatarToSwitchTo) == false)
                return Logger.WarnReturn(CanSwitchAvatarResult.NotAllowedUnknown, $"CanSwitchToAvatar(): Attempting to switch to avatar not belonging to this player\ncurrentAvatar=[{currentAvatar}], avatarToSwitchTo={avatarToSwitchTo}");

            // Check live tuning
            AvatarPrototype avatarToSwitchToProto = avatarToSwitchTo.AvatarPrototype;
            if (avatarToSwitchToProto == null) return Logger.WarnReturn(CanSwitchAvatarResult.NotAllowedUnknown, "CanSwitchToAvatar(): avatarToSwitchToProto == null");

            if (avatarToSwitchToProto.IsLiveTuningEnabled() == false)
                return CanSwitchAvatarResult.NotAllowedUnknown;

            // Do not check anything else if not requested
            if (ignoreGameplayRestrictions)
                return CanSwitchAvatarResult.Success;

            // Generic check if avatar switch is possible
            CanSwitchAvatarResult genericCheckResult = CanSwitchAvatars();
            if (genericCheckResult != CanSwitchAvatarResult.Success)
                return genericCheckResult;

            // Avatar-specific checks
            if (currentAvatar.IsDead)
                return CanSwitchAvatarResult.NotAllowedUnknown;

            Region region = GetRegion();
            if (region == null)
                return CanSwitchAvatarResult.NotAllowedInRegion;

            RegionPrototype regionProto = region.Prototype;

            // Run eval checks
            if (regionProto.RunEvalAccessRestriction(this, avatarToSwitchTo, region.DifficultyTierRef) == false)
                return CanSwitchAvatarResult.NotAllowedInRegion;

            // Check roster restriction
            if (region.IsRestrictedRosterEnabled)
            {
                bool success = false;
                PrototypeId avatarToSwitchToProtoRef = avatarToSwitchToProto.DataRef;
                foreach (PrototypeId avatarProtoRef in regionProto.RestrictedRoster)
                {
                    if (avatarProtoRef == avatarToSwitchToProtoRef)
                        success = true;
                }

                if (success == false)
                    return CanSwitchAvatarResult.NotAllowedInRegion;
            }

            return CanSwitchAvatarResult.Success;
        }

        private CanSwitchAvatarResult CanSwitchAvatars(PlayerAvatarIndex avatarIndex = PlayerAvatarIndex.Primary)
        {
            Region region = GetRegion();
            if (region == null) return Logger.WarnReturn(CanSwitchAvatarResult.NotAllowedUnknown, "CanSwitchAvatars(): region == null");

            // Region lock
            if (region.AvatarSwapEnabled == false && CurrentHUDTutorial?.HighlightAvatars.HasValue() == false)
                return CanSwitchAvatarResult.NotAllowedInRegion;

            RegionPrototype regionProto = region.Prototype;
            if (regionProto == null) return Logger.WarnReturn(CanSwitchAvatarResult.NotAllowedUnknown, "CanSwitchAvatars(): regionProto == null");

            if (regionProto.Behavior == RegionBehavior.PrivateStory)
                return CanSwitchAvatarResult.NotAllowedInPrivateInstance;

            // Transform mode
            Avatar avatar = GetActiveAvatarByIndex((int)avatarIndex);
            if (avatar == null) return Logger.WarnReturn(CanSwitchAvatarResult.NotAllowedUnknown, "CanSwitchAvatars(): avatar == null");

            PrototypeId transformModeProtoRef = avatar.CurrentTransformMode;
            if (transformModeProtoRef != PrototypeId.Invalid)
            {
                TransformModePrototype transformModeProto = transformModeProtoRef.As<TransformModePrototype>();
                if (transformModeProto == null) return Logger.WarnReturn(CanSwitchAvatarResult.NotAllowedUnknown, "CanSwitchAvatars(): transformModeProto == null");

                if (transformModeProto.GetDuration(avatar) != TimeSpan.Zero)
                    return CanSwitchAvatarResult.NotAllowedInTransformMode; 
            }

            // Property restriction
            if (avatar.Properties[PropertyEnum.NoAvatarSwap])
                return CanSwitchAvatarResult.NotAllowedGeneric;

            // Throwable
            if (avatar.GetThrowablePower() != null)
                return CanSwitchAvatarResult.NotAllowedGeneric;

            // Check if the region prohibits the use of swap powers
            GlobalsPrototype globalsProto = GameDatabase.GlobalsPrototype;

            if (regionProto.PowerKeywordBlacklist.HasValue())
            {
                PowerPrototype swapOutPowerProto = globalsProto.AvatarSwapOutPower.As<PowerPrototype>();
                if (swapOutPowerProto == null) return Logger.WarnReturn(CanSwitchAvatarResult.NotAllowedUnknown, "CanSwitchAvatars(): swapOutPowerProto == null");

                PowerPrototype swapInPowerProto = globalsProto.AvatarSwapInPower.As<PowerPrototype>();
                if (swapInPowerProto == null) return Logger.WarnReturn(CanSwitchAvatarResult.NotAllowedUnknown, "CanSwitchAvatars(): swapInPowerProto == null");

                if (swapOutPowerProto.Keywords.ShareElement(regionProto.PowerKeywordBlacklist) || swapInPowerProto.Keywords.ShareElement(regionProto.PowerKeywordBlacklist))
                    return CanSwitchAvatarResult.NotAllowedInRegion;
            }

            // Queue status
            if (MatchQueueStatus.IsOwnerInQueue())
                return CanSwitchAvatarResult.NotAllowedInQueue;

            // Do not allow switching while using powers
            PrototypeId activePowerRef = avatar.ActivePowerRef;
            if (activePowerRef != PrototypeId.Invalid && activePowerRef != globalsProto.AvatarSwapChannelPower && activePowerRef != globalsProto.AvatarSwapOutPower)
                return CanSwitchAvatarResult.NotAllowedGeneric;

            return CanSwitchAvatarResult.Success;
        }

        #endregion

        #region Alternate Advancement (Omega and Infinity)

        public long GetTotalInfinityPoints()
        {
            long infinityPoints = 0;

            for (InfinityGem gem = 0; gem < InfinityGem.NumGems; gem++)
                infinityPoints += Properties[PropertyEnum.InfinityPoints, (int)gem];

            return infinityPoints;
        }

        public void AwardInfinityXP(long amount, bool notifyClient)
        {
            if (amount <= 0)
                return;

            long infinityXP = Math.Min(InfinityXP + amount, GameDatabase.AdvancementGlobalsPrototype.InfinityXPCap);
            Properties[PropertyEnum.InfinityXP] = infinityXP;

            TryInfinityLevelUp(notifyClient);
        }

        public void TryInfinityLevelUp(bool notifyClient)
        {
            long pointsBefore = GetTotalInfinityPoints();
            long pointsAfter = CalcInfinityPointsFromXP(InfinityXP);

            // Early out if we don't have any points
            if (pointsAfter <= pointsBefore)
                return;

            long pointsPerGem = Math.DivRem(pointsAfter - pointsBefore, (int)InfinityGem.NumGems, out long pointsPerGemRemainder);

            InfinityGemBonusTable infinityBonusTable = GameDataTables.Instance.InfinityGemBonusTable;

            NetMessageInfinityPointGain.Builder messageBuilder = notifyClient ? NetMessageInfinityPointGain.CreateBuilder() : null;

            InfinityGem gemStart = (InfinityGem)(int)Properties[PropertyEnum.InfinityGemNext];
            InfinityGem gemCurrent = gemStart;
            InfinityGem gemLast = gemStart;

            // Need to use a do loop here instead of for to ensure at least one iteration of it
            do
            {
                long numPointsGained = pointsPerGem;

                // Distribute the remainder
                if (pointsPerGemRemainder > 0)
                {
                    numPointsGained++;
                    pointsPerGemRemainder--;

                    if (pointsPerGemRemainder == 0)
                        gemLast = gemCurrent;
                }

                // Adjust the number of points
                if (numPointsGained > 0)
                {
                    Properties.AdjustProperty((int)numPointsGained, new(PropertyEnum.InfinityPoints, (PropertyParam)gemCurrent));
                    
                    messageBuilder?.AddPointsGained(NetStructInfinityPointGain.CreateBuilder()
                        .SetNumPointsGained(numPointsGained)
                        .SetInfinityGemEnum((int)gemCurrent));
                }

                gemCurrent = infinityBonusTable.GetNextGem(gemCurrent);
            } while (gemCurrent != gemStart);

            Properties[PropertyEnum.InfinityGemNext] = (int)infinityBonusTable.GetNextGem(gemLast);

            Avatar avatar = CurrentAvatar;
            if (messageBuilder != null && avatar != null && avatar.IsInfinitySystemUnlocked())
            {
                messageBuilder.SetAvatarId(avatar.Id);
                Game.NetworkManager.SendMessageToInterested(messageBuilder.Build(), avatar, AOINetworkPolicyValues.AOIChannelProximity | AOINetworkPolicyValues.AOIChannelOwner);
            }
        }

        private static long CalcInfinityPointsFromXP(long xp)
        {
            long points = (long)Math.Sqrt(xp / AdvancementGlobalsPrototype.InfinityXPFactor);
            return Math.Min(points, GameDatabase.AdvancementGlobalsPrototype.InfinityPointsCap);
        }

        public long GetOmegaPoints()
        {
            return Properties[PropertyEnum.OmegaPoints];
        }

        public void AwardOmegaXP(long amount, bool notifyClient)
        {
            if (amount <= 0)
                return;

            long omegaXP = Math.Min(OmegaXP + amount, GameDatabase.AdvancementGlobalsPrototype.InfinityXPCap);
            Properties[PropertyEnum.OmegaXP] = omegaXP;

            TryOmegaLevelUp(notifyClient);
        }

        public void TryOmegaLevelUp(bool notifyClient)
        {
            // TODO: Verify if it's working correctly

            long pointsBefore = GetOmegaPoints();
            long pointsAfter = CalcOmegaPointsFromXP(OmegaXP);

            // Early out if we don't have any points
            if (pointsAfter <= pointsBefore)
                return;

            long numPointsGained = pointsAfter - pointsBefore;
            Properties.AdjustProperty((int)numPointsGained, PropertyEnum.OmegaPoints);

            Avatar avatar = CurrentAvatar;
            if (notifyClient && avatar != null && avatar.IsOmegaSystemUnlocked())
            {
                NetMessageOmegaPointGain message = NetMessageOmegaPointGain.CreateBuilder()
                    .SetAvatarId(avatar.Id)
                    .SetNumPointsGained((uint)numPointsGained)
                    .Build();

                Game.NetworkManager.SendMessageToInterested(message, avatar, AOINetworkPolicyValues.AOIChannelProximity | AOINetworkPolicyValues.AOIChannelOwner);
            }
        }

        private static long CalcOmegaPointsFromXP(long xp)
        {
            int points = MathHelper.RoundToInt(Math.Sqrt(xp / AdvancementGlobalsPrototype.OmegaXPFactor));
            return Math.Min(points, GameDatabase.AdvancementGlobalsPrototype.OmegaPointsCap);
        }

        #endregion

        #region Difficulty

        public bool CanChangeDifficulty(PrototypeId difficultyTierProtoRef)
        {
            DifficultyTierPrototype difficultyTierProto = difficultyTierProtoRef.As<DifficultyTierPrototype>();
            if (difficultyTierProto == null) return Logger.WarnReturn(false, "CanChangeDifficulty(): difficultyTierProto == null");

            // The game assumes all difficulties to be unlocked if there is no current avatar
            if (CurrentAvatar != null && CurrentAvatar.CharacterLevel < difficultyTierProto.UnlockLevel)
                return false;

            return true;
        }

        public PrototypeId GetDifficultyTierPreference()
        {
            // TODO: Party

            if (CurrentAvatar != null)
                return CurrentAvatar.Properties[PropertyEnum.DifficultyTierPreference];

            return GameDatabase.GlobalsPrototype.DifficultyTierDefault;
        }

        public PrototypeId GetDifficultyTierForRegion(PrototypeId regionProtoRef, PrototypeId preferenceProtoRef = PrototypeId.Invalid)
        {
            if (preferenceProtoRef == PrototypeId.Invalid)
                preferenceProtoRef = GetDifficultyTierPreference();

            PrototypeId difficultyTierProtoRef = RegionPrototype.ConstrainDifficulty(regionProtoRef, preferenceProtoRef);
            if (difficultyTierProtoRef == preferenceProtoRef)
                return preferenceProtoRef;

            if (CanChangeDifficulty(difficultyTierProtoRef))
                return difficultyTierProtoRef;

            return PrototypeId.Invalid;
        }

        #endregion

        #region Loading and Teleports

        public void OnFullscreenMovieStarted(PrototypeId movieRef)
        {
            Logger.Trace($"OnFullscreenMovieStarted {GameDatabase.GetFormattedPrototypeName(movieRef)} for {_playerName}");
            var movieProto = GameDatabase.GetPrototype<FullscreenMoviePrototype>(movieRef);
            if (movieProto == null) return;
            if (movieProto.MovieType == MovieType.Cinematic)
            {
                Properties[PropertyEnum.FullScreenMovieSession] = Game.Random.Next();
                Properties[PropertyEnum.FullScreenMoviePlaying] = true;
            }
        }

        public void OnFullscreenMovieFinished(PrototypeId movieRef, bool userCancelled, uint syncRequestId)
        {
            if (syncRequestId != 0 && syncRequestId < FullscreenMovieSyncRequestId) return;

            Logger.Trace($"OnFullscreenMovieFinished {GameDatabase.GetFormattedPrototypeName(movieRef)} Canceled = {userCancelled} by {_playerName}");

            var movieProto = GameDatabase.GetPrototype<FullscreenMoviePrototype>(movieRef);
            if (movieProto == null) return;

            if (movieProto.MovieType == MovieType.Cinematic)
            {
                FullScreenMovieDequeued(movieRef);
                GetRegion()?.CinematicFinishedEvent.Invoke(new(this, movieRef));
                Properties.RemoveProperty(PropertyEnum.FullScreenMovieSession);
            }
        }

        private void FullScreenMovieDequeued(PrototypeId movieRef)
        {
            var propId = new PropertyId(PropertyEnum.FullScreenMovieQueued, movieRef);
            Properties.RemoveProperty(propId);
            if (Properties.HasProperty(PropertyEnum.FullScreenMovieQueued) == false)
                Properties.RemoveProperty(PropertyEnum.FullScreenMoviePlaying);
        }

        public void QueueFullscreenMovie(PrototypeId movieRef)
        {
            var movieProto = GameDatabase.GetPrototype<FullscreenMoviePrototype>(movieRef);
            if (movieProto == null) return;

            if (movieProto.MovieType == MovieType.Cinematic)
                FullScreenMovieQueued(movieRef);

            SendMessage(NetMessageQueueFullscreenMovie.CreateBuilder()
                .SetMoviePrototypeId((ulong)movieRef)
                .Build());
        }

        private void FullScreenMovieQueued(PrototypeId movieRef)
        {
            Properties[PropertyEnum.FullScreenMovieQueued, movieRef] = true;
            Properties[PropertyEnum.FullScreenMoviePlaying] = true;
        }

        public void QueueLoadingScreen(PrototypeId regionProtoRef)
        {
            IsOnLoadingScreen = true;

            SendMessage(NetMessageQueueLoadingScreen.CreateBuilder()
                .SetRegionPrototypeId((ulong)regionProtoRef)
                .Build());
        }

        public void QueueLoadingScreen(ulong regionId)
        {
            Region region = Game.RegionManager.GetRegion(regionId);
            PrototypeId regionProtoRef = region != null ? region.PrototypeDataRef : PrototypeId.Invalid;
            QueueLoadingScreen(regionProtoRef);
        }

        public void DequeueLoadingScreen()
        {
            SendMessage(NetMessageDequeueLoadingScreen.DefaultInstance);
        }

        public void OnLoadingScreenFinished()
        {
            if (IsOnLoadingScreen)
            {
                IsOnLoadingScreen = false;
                var region = GetRegion();
                if (region == null) return;
                region.LoadingScreenFinishedEvent.Invoke(new(this, region.PrototypeDataRef));
                region.OnLoadingFinished();
            }
        }

        public void BeginTeleport(ulong regionId, in Vector3 position, in Orientation orientation)
        {
            _teleportData.Set(regionId, position, orientation);
            QueueLoadingScreen(regionId);
        }

        public void TEMP_ScheduleMoveToTarget(PrototypeId targetProtoRef, TimeSpan delay)
        {
            // REMOVEME: Get rid of this when we overhaul the teleport system
            EventPointer<MoveToTargetEvent> moveToTargetEvent = new();
            ScheduleEntityEvent(moveToTargetEvent, delay, targetProtoRef);
        }

        public void OnCellLoaded(uint cellId, ulong regionId)
        {
            AOI.OnCellLoaded(cellId, regionId);
            int numLoaded = AOI.GetLoadedCellCount();

            //Logger.Trace($"Player {this} loaded cell id={cellId} in region id=0x{regionId:X} ({numLoaded}/{AOI.TrackedCellCount})");

            if (_teleportData.IsValid && numLoaded == AOI.TrackedCellCount)
                FinishTeleport();
        }

        private bool FinishTeleport()
        {
            if (_teleportData.IsValid == false) return Logger.WarnReturn(false, "FinishTeleport(): No valid teleport data");

            EnableCurrentAvatar(false, CurrentAvatar.Id, _teleportData.RegionId, _teleportData.Position, _teleportData.Orientation);
            _teleportData.Clear();
            DequeueLoadingScreen();
            TryPlayKismetSequences();

            return true;
        }

        #endregion

        #region AOI & Discovery

        public bool InterestedInEntity(Entity entity, AOINetworkPolicyValues interestFilter)
        {
            if (entity == null) return Logger.WarnReturn(false, "InterestedInEntity(): entity == null");

            if (entity.InterestReferences.IsPlayerInterested(this) == false)
                return false;

            return AOI.InterestedInEntity(entity.Id, interestFilter);
        }

        public MapDiscoveryData GetMapDiscoveryData(ulong regionId)
        {
            var manager = Game.RegionManager;
            Region region = manager.GetRegion(regionId);
            if (region == null) return Logger.WarnReturn<MapDiscoveryData>(null, "GetMapDiscoveryData(): region == null");

            if (_mapDiscoveryDict.TryGetValue(regionId, out MapDiscoveryData mapDiscoveryData) == false)
            {
                mapDiscoveryData = new(region);
                _mapDiscoveryDict.Add(regionId, mapDiscoveryData);
            }

            // clear old regions if limit is reached
            if (_mapDiscoveryDict.Count > 25)
                foreach (var kvp in _mapDiscoveryDict)
                    if (manager.GetRegion(kvp.Key) == null)
                        _mapDiscoveryDict.Remove(kvp.Key);

            return mapDiscoveryData;
        }

        public MapDiscoveryData GetMapDiscoveryDataForEntity(WorldEntity worldEntity)
        {
            Region region = worldEntity?.Region;
            if (region == null) return null;
            return GetMapDiscoveryData(region.Id);
        }

        public bool DiscoverEntity(WorldEntity worldEntity, bool updateInterest)
        {
            MapDiscoveryData mapDiscoveryData = GetMapDiscoveryDataForEntity(worldEntity);
            if (mapDiscoveryData == null) return Logger.WarnReturn(false, "DiscoverEntity(): mapDiscoveryData == null");

            if (mapDiscoveryData.DiscoverEntity(worldEntity) == false)
                return false;

            if (updateInterest)
                AOI.ConsiderEntity(worldEntity);

            return true;
        }

        public bool UndiscoverEntity(WorldEntity worldEntity, bool updateInterest)
        {
            MapDiscoveryData mapDiscoveryData = GetMapDiscoveryDataForEntity(worldEntity);
            if (mapDiscoveryData == null) return Logger.WarnReturn(false, "UndiscoverEntity(): mapDiscoveryData == null");

            if (mapDiscoveryData.UndiscoverEntity(worldEntity) == false)
                return false;

            if (updateInterest)
                AOI.ConsiderEntity(worldEntity);

            return true;
        }

        public bool IsEntityDiscovered(WorldEntity worldEntity)
        {
            MapDiscoveryData mapDiscoveryData = GetMapDiscoveryDataForEntity(worldEntity);
            return mapDiscoveryData != null && mapDiscoveryData.IsEntityDiscovered(worldEntity);
        }

        public bool RevealDiscoveryMap(Vector3 position)
        {
            var region = CurrentAvatar?.Region;
            if (region == null) return Logger.WarnReturn(false, "UpdateMapDiscovery(): region == null");

            MapDiscoveryData mapDiscoveryData = GetMapDiscoveryDataForEntity(CurrentAvatar);
            if (mapDiscoveryData == null) return Logger.WarnReturn(false, "UpdateDiscoveryMap(): mapDiscoveryData == null");

            bool reveal = mapDiscoveryData.RevealPosition(this, position);

            // TODO party reveal

            return reveal;
        }

        #endregion

        #region Trading

        public void CancelPlayerTrade()
        {
            // TODO
        }

        #endregion

        #region Targeting

        public bool IsTargetable(AlliancePrototype allianceProto)
        {
            Avatar avatar = PrimaryAvatar ?? SecondaryAvatar;
            if (avatar != null && allianceProto != null && allianceProto.IsFriendlyTo(avatar.Alliance)) return true;
            if (IsFullscreenObscured) return false;
            if (Properties[PropertyEnum.GracePeriod]) return false;
            return true;
        }

        public WorldEntity GetDialogTarget(bool validateTarget = false)
        {
            if (DialogTargetId == InvalidId) return null;
            var target = Game.EntityManager.GetEntity<WorldEntity>(DialogTargetId);
            if (validateTarget && ValidateDialogTarget(target, DialogInteractorId) == false) return null;
            return target;
        }

        public bool SetDialogTargetId(ulong targetId, ulong interactorId)
        {
            if (targetId != InvalidId && interactorId != InvalidId)
            {
                var interactor = Game.EntityManager.GetEntity<WorldEntity>(interactorId);
                if (interactor == null || interactor.IsInWorld == false)
                    return false;

                var target = Game.EntityManager.GetEntity<WorldEntity>(targetId);
                if (ValidateDialogTarget(target, interactorId) == false)
                    return Logger.WarnReturn(false, $"SetDialogTargetId(): Failed to validate dialog target for target=[{target}], interactor=[{interactor}]");
            }

            DialogTargetId = targetId;
            DialogInteractorId = interactorId;

            return true;
        }

        private bool ValidateDialogTarget(WorldEntity target, ulong interactorId)
        {
            if (target == null || target.IsInWorld == false) return false;
            var interactor = Game.EntityManager.GetEntity<WorldEntity>(interactorId);
            if (interactor == null || interactor.IsInWorld == false) return false;
            if (interactor.InInteractRange(target, InteractionMethod.Use, false) == false) return false;
            if (DialogInteractorId != InvalidId && DialogInteractorId != interactorId) return false;
            return true;
        }

        #endregion

        #region Missions and Chapters

        public void InitializeMissionTrackerFilters()
        {
            foreach (PrototypeId filterRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<MissionTrackerFilterPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                var filterProto = GameDatabase.GetPrototype<MissionTrackerFilterPrototype>(filterRef);
                if (filterProto.DisplayByDefault)
                    Properties[PropertyEnum.MissionTrackerFilter, filterRef] = true;
            }
        }

        public void SetActiveChapter(PrototypeId chapterRef)
        {
            Properties[PropertyEnum.ActiveMissionChapter] = chapterRef;
            GetRegion()?.ActiveChapterChangedEvent.Invoke(new(this, chapterRef));
        }

        public bool ChapterIsUnlocked(PrototypeId chapterRef)
        {
            var avatar = CurrentAvatar;
            if (avatar == null) return false;
            return avatar.Properties[PropertyEnum.ChapterUnlocked, chapterRef];
        }

        public void UnlockChapter(PrototypeId chapterRef)
        {
            var avatar = CurrentAvatar;
            if (avatar == null) return;
            avatar.Properties[PropertyEnum.ChapterUnlocked, chapterRef] = true;
        }

        public void MissionInteractRelease(WorldEntity entity, PrototypeId missionRef)
        {
            if (missionRef == PrototypeId.Invalid) return;
            if (InterestedInEntity(entity, AOINetworkPolicyValues.AOIChannelOwner))
                SendMessage(NetMessageMissionInteractRelease.DefaultInstance);
        }

        public void RequestLegendaryMissionReroll()
        {
            var game = Game;
            var avatar = CurrentAvatar;
            var manager = MissionManager;
            if (game == null || avatar == null || manager == null) return;

            var currencyProto = GameDatabase.CurrencyGlobalsPrototype;
            var missionProto = GameDatabase.MissionGlobalsPrototype;
            if (currencyProto == null || missionProto?.LegendaryRerollCost == null) return;

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.Game = Game;
            evalContext.SetVar_EntityPtr(EvalContext.Default, avatar);
            int rerollCost = Eval.RunInt(missionProto.LegendaryRerollCost, evalContext);
            var propId = new PropertyId(PropertyEnum.Currency, currencyProto.Credits);
            if (Properties[propId] < rerollCost) return;

            manager.LegendaryMissionReroll();
            Properties.AdjustProperty(-rerollCost, propId);
        }

        #endregion

        #region Waypoint

        public void UnlockChapters()
        {
            foreach (PrototypeId chapterRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<ChapterPrototype>(PrototypeIterateFlags.NoAbstract))
            {
                var ChapterProto = GameDatabase.GetPrototype<ChapterPrototype>(chapterRef);
                if (ChapterProto.StartLocked == false)
                    UnlockChapter(chapterRef);
            }
        }

        public void LockWaypoint(PrototypeId waypointRef)
        {
            var waypointProto = GameDatabase.GetPrototype<WaypointPrototype>(waypointRef);
            if (waypointProto == null) return;

            PropertyCollection collection;
            if (waypointProto.IsAccountWaypoint)
                collection = Properties;
            else
                collection = CurrentAvatar.Properties;

            var propId = new PropertyId(PropertyEnum.Waypoint, waypointRef);
            if (collection[propId])
                SendOnWaypointUpdated();

            collection[propId] = false;
        }

        public bool WaypointIsUnlocked(PrototypeId waypointRef)
        {
            var waypointProto = GameDatabase.GetPrototype<WaypointPrototype>(waypointRef);
            if (waypointProto == null) return false;

            PropertyCollection collection;
            if (waypointProto.IsAccountWaypoint)
                collection = Properties;
            else
                collection = CurrentAvatar.Properties;

            return collection[PropertyEnum.Waypoint, waypointRef];
        }

        public void UnlockWaypoint(PrototypeId waypointRef)
        {
            var waypointProto = GameDatabase.GetPrototype<WaypointPrototype>(waypointRef);
            if (waypointProto == null) return;

            PropertyCollection collection;
            if (waypointProto.IsAccountWaypoint)
                collection = Properties;
            else
                collection = CurrentAvatar.Properties;

            var propId = new PropertyId(PropertyEnum.Waypoint, waypointRef);
            if (collection[propId] == false)
            {
                SendWaypointUnlocked();
                collection[propId] = true;

                OnScoringEvent(new(ScoringEventType.WaypointUnlocked, waypointProto));
            }
        }

        public void UnlockWaypoints()
        {
            foreach (PrototypeId waypointRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<WaypointPrototype>(PrototypeIterateFlags.NoAbstract))
            {
                var waypointProto = GameDatabase.GetPrototype<WaypointPrototype>(waypointRef);
                if (waypointProto.StartLocked == false)
                    UnlockWaypoint(waypointRef);
            }
        }

        #endregion

        #region Kismet

        public void PlayKismetSeq(PrototypeId kismetSeq)
        {
            var avatar = CurrentAvatar;
            if (avatar == null) return;

            var kismetProto = GameDatabase.GetPrototype<KismetSequencePrototype>(kismetSeq);
            if (kismetProto == null) return;

            if (kismetProto.KismetSeqBlocking)
            {
                if (IsFullscreenMoviePlaying) return;
                FullScreenMovieQueued(kismetSeq);
            }

            SendPlayKismetSeq(kismetSeq);
        }

        public void QueuePlayKismetSeq(PrototypeId kismetSeq)
        {
            if (_teleportData.IsValid)
                _kismetSeqQueue.Enqueue(kismetSeq);
            else
                PlayKismetSeq(kismetSeq);
        }

        public bool TryPlayKismetSequences()
        {
            // play kismetSeq from Queue
            while (_kismetSeqQueue.Count > 0)
            {
                var kismetSeq = _kismetSeqQueue.Dequeue();
                if (kismetSeq != PrototypeId.Invalid)
                    PlayKismetSeq(kismetSeq);
            }

            // try play kismetSeq for region
            var region = CurrentAvatar.Region;
            if (region == null) return Logger.WarnReturn(false, "TryPlayKismetSequences(): region == null");

            var startTarget = GameDatabase.GetPrototype<RegionConnectionTargetPrototype>(region.Prototype.StartTarget);
            if (startTarget == null || startTarget.IntroKismetSeq == PrototypeId.Invalid) return false;
            var targetCellRef = GameDatabase.GetDataRefByAsset(startTarget.Cell);
            var cellRef = CurrentAvatar.Cell.PrototypeDataRef;
            if (targetCellRef != cellRef) return false;

            PlayKismetSeq(startTarget.IntroKismetSeq);
            return true;
        }

        public void OnPlayKismetSeqDone(PrototypeId kismetSeqRef, uint syncRequestId)
        {
            if (syncRequestId != 0 && syncRequestId < FullscreenMovieSyncRequestId) return;

            if (kismetSeqRef == PrototypeId.Invalid) return;
            var kismetSeqProto = GameDatabase.GetPrototype<KismetSequencePrototype>(kismetSeqRef);
            if (kismetSeqProto == null) return;
            if (kismetSeqProto.KismetSeqBlocking && IsFullscreenMoviePlaying)
            {
                FullScreenMovieDequeued(kismetSeqRef);
                SendMissionInteract(Id);
                GetRegion()?.KismetSeqFinishedEvent.Invoke(new(this, kismetSeqRef));
            }
        }

        #endregion

        #region SendMessage

        public void SendMessage(IMessage message)
        {
            PlayerConnection?.SendMessage(message);
        }

        public void SendPlayKismetSeq(PrototypeId kismetSeqRef)
        {
            SendMessage(NetMessagePlayKismetSeq.CreateBuilder()
                .SetKismetSeqPrototypeId((ulong)kismetSeqRef)
                .Build());
        }

        public void SendFullscreenMovieSync()
        {
            if (Properties.HasProperty(PropertyEnum.FullScreenMovieQueued) == false) return;

            var message = NetMessageFullscreenMovieSync.CreateBuilder();
            message.SetSyncRequestId(++FullscreenMovieSyncRequestId);

            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.FullScreenMovieQueued))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId movieRef);
                if (movieRef == PrototypeId.Invalid) continue;
                message.AddMovieProtoId((ulong)movieRef);
            }

            SendMessage(message.Build());
        }

        public void SendAIAggroNotification(PrototypeId bannerMessageRef, Agent aiAgent, Player targetPlayer, bool party = false)
        {
            if (party)
            {
                // TODO send to party members
            }
            else
            {
                var message = NetMessageAIAggroNotification.CreateBuilder()
                    .SetBannerMessageRef((ulong)bannerMessageRef)
                    .SetAiRef((ulong)aiAgent.PrototypeDataRef)
                    .SetPlayerId(targetPlayer.Id)
                    .Build();

                SendMessage(message);
            }
        }

        public void SendMissionInteract(ulong targetId)
        {
            var avatar = CurrentAvatar;
            if (avatar == null) return;
            var target = Game.EntityManager.GetEntity<WorldEntity>(targetId);

            if (target != null)
            {
                var entityDesc = new EntityDesc(target);
                var outInteractData = new InteractData { IndicatorType = HUDEntityOverheadIcon.None };
                var interactionType = InteractionManager.CallGetInteractionStatus(entityDesc, avatar, 0, InteractionFlags.Default, ref outInteractData);           
                if (interactionType.HasFlag(InteractionMethod.Converse) && outInteractData.IndicatorType.HasValue)
                {
                    var indicatorType = outInteractData.IndicatorType.Value;
                    if (indicatorType == HUDEntityOverheadIcon.DiscoveryBestower
                        || indicatorType == HUDEntityOverheadIcon.MissionBestower
                        || indicatorType == HUDEntityOverheadIcon.DiscoveryAdvancer
                        || indicatorType == HUDEntityOverheadIcon.MissionAdvancer)
                    {
                        var message = NetMessageMissionInteractRepeat.CreateBuilder()
                            .SetTargetEntityId(targetId)
                            .SetMissionPrototypeId(0).Build(); // client not use MissionPrototype

                        Game.NetworkManager.SendMessageToInterested(message, this, AOINetworkPolicyValues.AOIChannelOwner);
                        return;
                    }
                }
            }

            var messageRelease = NetMessageMissionInteractRelease.DefaultInstance;
            Game.NetworkManager.SendMessageToInterested(messageRelease, this, AOINetworkPolicyValues.AOIChannelOwner);           
        }

        public void SendRegionRestrictedRosterUpdate(bool enabled)
        {
            var message = NetMessageRegionRestrictedRosterUpdate.CreateBuilder().SetEnabled(enabled).Build(); 
            SendMessage(message);
        }

        public void SendRegionAvatarSwapUpdate(bool enabled)
        {
            var message = NetMessageRegionAvatarSwapUpdate.CreateBuilder().SetEnabled(enabled).Build();
            SendMessage(message);
        }

        public void SendNewTeamUpAcquired(PrototypeId teamUpRef)
        {
            var message = NetMessageNewTeamUpAcquired.CreateBuilder().SetPrototypeId((ulong)teamUpRef).Build();
            SendMessage(message);
        }

        public void SendPlayStoryBanter(AssetId banterRef)
        {
            var message = NetMessagePlayStoryBanter.CreateBuilder().SetBanterAssetId((ulong)banterRef).Build();
            SendMessage(message);
        }

        public void SendUINotification(UINotificationPrototype notification)
        {
            if (notification is BannerMessagePrototype banner)
                SendBannerMessage(banner);
            else if (notification is StoryNotificationPrototype story)
                SendStoryNotification(story);
            else if (notification is HUDTutorialPrototype tutorial)
                SendHUDTutorial(tutorial);
        }

        private void SendWaypointUnlocked()
        {
            SendOnWaypointUpdated();
            if (InterestedInEntity(this, AOINetworkPolicyValues.AOIChannelOwner) == false) return;
            var message = NetMessageOnWaypointUpdated.CreateBuilder().SetIdPlayer(Id).Build();
            SendMessage(message);
        }

        public void SendOnWaypointUpdated()
        {
            if (InterestedInEntity(this, AOINetworkPolicyValues.AOIChannelOwner) == false) return;
            var message = NetMessageOnWaypointUpdated.CreateBuilder().SetIdPlayer(Id).Build();
            SendMessage(message);
        }

        public void SendRegionDifficultyChange(int dificultyIndex)
        {
            var message = NetMessageRegionDifficultyChange.CreateBuilder().SetDifficultyIndex((ulong)dificultyIndex).Build();
            SendMessage(message);
        }

        public void SendHUDTutorial(HUDTutorialPrototype hudTutorialProto)
        {
            var hudTutorialRef = PrototypeId.Invalid;
            if (hudTutorialProto != null) hudTutorialRef = hudTutorialProto.DataRef;
            var message = NetMessageHUDTutorial.CreateBuilder().SetHudTutorialProtoId((ulong)hudTutorialRef).Build();
            SendMessage(message);
        }

        public void SendOpenUIPanel(AssetId panelNameId)
        {
            if (panelNameId == AssetId.Invalid) return;
            string panelName = GameDatabase.GetAssetName(panelNameId);
            if (panelName == "Unknown") return;
            var message = NetMessageOpenUIPanel.CreateBuilder().SetPanelName(panelName).Build();
            SendMessage(message);
        }

        public void SendWaypointNotification(PrototypeId waypointRef, bool show = true)
        {
            if (waypointRef == PrototypeId.Invalid) return;
            var message = NetMessageWaypointNotification.CreateBuilder()
                .SetWaypointProtoId((ulong)waypointRef)
                .SetShow(show).Build();
            SendMessage(message);
        }

        public void SendStoryNotification(StoryNotificationPrototype storyNotification, PrototypeId missionRef = PrototypeId.Invalid)
        {
            if (storyNotification == null) return;

            var message = NetMessageStoryNotification.CreateBuilder();
            message.SetDisplayTextStringId((ulong)storyNotification.DisplayText);

            if (storyNotification.SpeakingEntity != PrototypeId.Invalid)
                message.SetSpeakingEntityPrototypeId((ulong)storyNotification.SpeakingEntity);

            message.SetTimeToLiveMS((uint)storyNotification.TimeToLiveMS);
            message.SetVoTriggerAssetId((ulong)storyNotification.VOTrigger);

            if (missionRef != PrototypeId.Invalid)
                message.SetMissionPrototypeId((ulong)missionRef);

            SendMessage(message.Build());
        }

        public void SendBannerMessage(BannerMessagePrototype bannerMessage)
        {
            if (bannerMessage == null) return;
            var message = NetMessageBannerMessage.CreateBuilder()
                .SetBannerText((ulong)bannerMessage.BannerText)
                .SetTextStyle((ulong)bannerMessage.TextStyle)
                .SetTimeToLiveMS((uint)bannerMessage.TimeToLiveMS)
                .SetMessageStyle((uint)bannerMessage.MessageStyle)
                .SetDoNotQueue(bannerMessage.DoNotQueue)
                .SetShowImmediately(bannerMessage.ShowImmediately).Build();
            SendMessage(message);
        }

        #endregion

        #region Scoring Events

        public void OnScoringEvent(in ScoringEvent scoringEvent, ulong entityId = Entity.InvalidId)
        {
            AchievementManager.OnScoringEvent(scoringEvent, entityId);
            LeaderboardManager.OnScoringEvent(scoringEvent, entityId);
        }

        public void UpdateScoringEventContext()
        {
            ScoringEventContext = new(this);
            AchievementManager.OnUpdateEventContext();
            LeaderboardManager.OnUpdateEventContext();
        }

        #endregion

        #region Time Played

        public TimeSpan GetTimePlayed()
        {
            TimeSpan timePlayed = Properties[PropertyEnum.AvatarTotalTimePlayed];

            Avatar avatar = CurrentAvatar;
            if (avatar != null)
                timePlayed += avatar.GetTimePlayed() - avatar.Properties[PropertyEnum.AvatarTotalTimePlayed];

            return timePlayed;
        }

        public void UpdateTimePlayed()
        {
            Properties[PropertyEnum.AvatarTotalTimePlayed] = GetTimePlayed();
        }

        private void CheckHoursPlayed()
        {
            int count = (int)Math.Floor(GetTimePlayed().TotalHours);
            OnScoringEvent(new(ScoringEventType.HoursPlayed, count));

            Avatar avatar = CurrentAvatar;
            if (avatar != null)
            {
                count = (int)Math.Floor(avatar.GetTimePlayed().TotalHours);
                OnScoringEvent(new(ScoringEventType.HoursPlayedByAvatar, avatar.Prototype, count));
            }

            ScheduleCheckHoursPlayedEvent();
        }

        private void ScheduleCheckHoursPlayedEvent()
        {
            if (_checkHoursPlayedEvent.IsValid) return;
            var scheduler = Game?.GameEventScheduler;
            if (scheduler == null) return;
            scheduler.ScheduleEvent(_checkHoursPlayedEvent, TimeSpan.FromMinutes(1), _pendingEvents);
            _checkHoursPlayedEvent.Get().Initialize(this);
        }

        #endregion

        #region Tutorial

        public void UnlockNewPlayerUISystems()
        {
            if (_newPlayerUISystemsUnlocked)
                return;

            foreach (PrototypeId uiSystemLockRef in GameDatabase.UIGlobalsPrototype.UISystemLockList)
            {
                var uiSystemLockProto = GameDatabase.GetPrototype<UISystemLockPrototype>(uiSystemLockRef);
                if (uiSystemLockProto.IsNewPlayerExperienceLocked && Properties[PropertyEnum.UISystemLock, uiSystemLockRef] != 1)
                    Properties[PropertyEnum.UISystemLock, uiSystemLockRef] = 1;
            }

            if (Game.CustomGameOptions.AutoUnlockAvatars)
            {
                // HACK: Unlock avatars here too
                foreach (PrototypeId avatarRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<AvatarPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
                {
                    if (avatarRef == (PrototypeId)6044485448390219466) continue;   //zzzBrevikOLD.prototype
                    UnlockAvatar(avatarRef, false);
                }
            }

            if (Game.GameOptions.TeamUpSystemEnabled && Game.CustomGameOptions.AutoUnlockTeamUps)
            {
                // HACK: And team-ups as well
                Inventory teamUpLibrary = GetInventory(InventoryConvenienceLabel.TeamUpLibrary);
                if (teamUpLibrary.Count == 0)
                {
                    foreach (PrototypeId teamUpRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<AgentTeamUpPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
                        UnlockTeamUpAgent(teamUpRef, false);
                }
            }

            _newPlayerUISystemsUnlocked = true;
        }

        public bool HasFinishedTutorial()
        {
            // HACK: Chat unlocks when the player finishes the tutorial in 1.52, so this can be used to check tutorial completion.
            // We unlock chat by default for accounts with elevated privileges, so they effectively always have the tutorial completed.
            return Properties[PropertyEnum.UISystemLock, UIGlobalsPrototype.ChatSystemLock];
        }

        public void UpdateUISystemLocks()
        {
            foreach (PrototypeId uiSystemLockProtoRef in GameDatabase.UIGlobalsPrototype.UISystemLockList)
            {
                UISystemLockPrototype uiSystemLockProto = uiSystemLockProtoRef.As<UISystemLockPrototype>();
                if (uiSystemLockProto.UnlockLevel == -1)
                    continue;

                int currentState = Properties[PropertyEnum.UISystemLock, uiSystemLockProtoRef];
                int maxAvatarLevel = Properties[PropertyEnum.PlayerMaxAvatarLevel];

                if (currentState == 0 && maxAvatarLevel >= uiSystemLockProto.UnlockLevel)
                    Properties[PropertyEnum.UISystemLock, uiSystemLockProtoRef] = 1;
            }
        }

        public void SetTipSeen(PrototypeId tipDataRef)
        {
            if (tipDataRef == PrototypeId.Invalid) return;
            Properties[PropertyEnum.TutorialHasSeenTip, tipDataRef] = true;
        }

        public void ShowHUDTutorial(HUDTutorialPrototype hudTutorialProto)
        {
            if (hudTutorialProto != null && hudTutorialProto.ShouldShowTip(this) == false) return;

            if (CurrentHUDTutorial != hudTutorialProto)
            {
                var manager = Game.EntityManager;
                var inventory = GetInventory(InventoryConvenienceLabel.AvatarInPlay);
                if (inventory == null) return;

                bool send = hudTutorialProto != null;
                if (CurrentHUDTutorial != null)
                {
                    foreach(var entry in inventory)
                    {
                        var avatar = manager.GetEntity<Avatar>(entry.Id);
                        avatar?.ResetTutorialProps();
                    }
                    send |= CurrentHUDTutorial.CanDismiss == false && CurrentHUDTutorial.DisplayDurationMS <= 0;
                }
                if (send) SendHUDTutorial(hudTutorialProto);

                CurrentHUDTutorial = hudTutorialProto;

                if (hudTutorialProto != null)
                {
                    foreach (var entry in inventory)
                    {
                        var avatar = manager.GetEntity<Avatar>(entry.Id);
                        avatar?.SetTutorialProps(hudTutorialProto);
                    }

                    CancelScheduledHUDTutorialEvent();
                    if (hudTutorialProto.DisplayDurationMS > 0)
                        ScheduleEntityEvent(_hudTutorialResetEvent, TimeSpan.FromMilliseconds(hudTutorialProto.DisplayDurationMS));
                }
            }
        }

        private void CancelScheduledHUDTutorialEvent()
        {
            if (_hudTutorialResetEvent.IsValid)
            {
                var scheduler = Game.GameEventScheduler;
                scheduler.CancelEvent(_hudTutorialResetEvent);
            }
        }

        private void ResetHUDTutorial()
        {
            CancelScheduledHUDTutorialEvent();
            ShowHUDTutorial(null);
        }

        #endregion

        #region Vanity Titles

        public bool UnlockVanityTitle(PrototypeId vanityTitleProtoRef)
        {
            VanityTitlePrototype vanityTitleProto = vanityTitleProtoRef.As<VanityTitlePrototype>();
            if (vanityTitleProto == null) return Logger.WarnReturn(false, "UnlockVanityTitle(): vanityTitleProto == null");

            Properties[PropertyEnum.VanityTitleUnlocked, vanityTitleProtoRef] = true;
            return true;
        }

        public bool IsVanityTitleUnlocked(PrototypeId vanityTitleProtoRef)
        {
            if (vanityTitleProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "IsVanityTitleUnlocked(): vanityTitleProtoRef == PrototypeId.Invalid");
            return Properties.HasProperty(new PropertyId(PropertyEnum.VanityTitleUnlocked, vanityTitleProtoRef));
        }

        #endregion

        #region Daily Login

        public void CheckDailyLogin()
        {
            // Send currency balance so that the client has something to display without opening the store.
            // TODO: Add coupons here?
            SendMessage(NetMessageGetCurrencyBalanceResponse.CreateBuilder()
                .SetCurrencyBalance(GazillioniteBalance)
                .Build());

            // Update veteran (login) rewards
            if (Game.GameOptions.VeteranRewardsEnabled)
            {
                int loginCount = GetLoginCount();

                GiveLoginRewards(loginCount);
                Properties[PropertyEnum.LoginCount] = loginCount;
            }

            // Send gifting restrictions update.
            // NOTE: Currently we don't actually need this since we don't rely on external services,
            // but I'm leaving this here for consistency with our packet captures.
            SendMessage(NetMessageGiftingRestrictionsUpdate.CreateBuilder()
                .SetEmailVerified(_emailVerified)
                .SetAccountCreationTimestampUtc((long)_accountCreationTimestamp.TotalSeconds)
                .Build());
        }

        private int GetLoginCount()
        {
            // Check the rollover (daily at 10 AM UTC+0, same as shared quests)
            using PropertyCollection rolloverProperties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            rolloverProperties[PropertyEnum.LootCooldownRolloverWallTime, 0, (PropertyParam)Weekday.All] = 10f;

            TimeSpan currentTime = Clock.UnixTime;

            if (LootUtilities.GetLastLootCooldownRolloverWallTime(rolloverProperties, currentTime, out TimeSpan lastRolloverTime) == false)
                return Logger.WarnReturn(0, "GetLoginCount(): Failed to get last loot cooldown rollover wall time");

            if (lastRolloverTime > _loginRewardCooldownTimeStart)
            {
                _loginCount++;
                _loginRewardCooldownTimeStart = currentTime;
                Logger.Trace($"GetLoginCount(): Rollover for player [{this}], loginCount = {_loginCount}");
            }

            return (int)_loginCount;
        }

        private void GiveLoginRewards(int loginCount)
        {
            LootManager lootManager = Game.LootManager;

            foreach (PrototypeId loginRewardProtoRef in DataDirectory.Instance.IteratePrototypesInHierarchy<LoginRewardPrototype>(PrototypeIterateFlags.NoAbstract))
            {
                LoginRewardPrototype loginRewardProto = loginRewardProtoRef.As<LoginRewardPrototype>();
                if (loginRewardProto == null)
                {
                    Logger.Warn("GiveLoginRewards(): loginRewardProto == null");
                    continue;
                }

                if (loginRewardProto.Day > loginCount)
                    continue;

                PropertyId rewardId = new(PropertyEnum.LoginRewardReceivedDate, loginRewardProtoRef);

                if (Properties.HasProperty(rewardId))
                    continue;

                if (lootManager.GiveItem(loginRewardProto.Item, LootContext.CashShop, this) == false)
                {
                    Logger.Warn($"GiveLoginRewards(): Failed to give login reward {loginRewardProto} to player [{this}]");
                    continue;
                }

                Properties[rewardId] = (long)Clock.UnixTime.TotalSeconds;
            }
        }

        private void SetGiftingRestrictions()
        {
            // Email is always verified (for now)
            _emailVerified = true;

            // We are taking advantage of the fact that our database guids include account creation timestamp.
            // Review this code if this ever changes.
            _accountCreationTimestamp = TimeSpan.FromSeconds(DatabaseUniqueId >> 16 & 0xFFFFFFFF);
        }

        #endregion

        #region Communities

        // Community update broadcasts are done via scheduled events to avoid multiple broadcasts at once.

        public void ScheduleCommunityBroadcast()
        {
            if (_communityBroadcastEvent.IsValid)
                return;

            Game.GameEventScheduler.ScheduleEvent(_communityBroadcastEvent, TimeSpan.Zero, _pendingEvents);
            _communityBroadcastEvent.Get().Initialize(this);
        }

        public CommunityMemberBroadcast BuildCommunityBroadcast()
        {
            Region region = GetRegion();
            Avatar avatar = CurrentAvatar;

            return CommunityMemberBroadcast.CreateBuilder()
                .SetMemberPlayerDbId(DatabaseUniqueId)
                .SetCurrentRegionRefId(region != null ? (ulong)region.PrototypeDataRef : 0)
                .SetCurrentDifficultyRefId(region != null ? (ulong)region.DifficultyTierRef : 0)
                .AddSlots(CommunityMemberAvatarSlot.CreateBuilder()
                    .SetAvatarRefId(avatar != null ? (ulong)avatar.PrototypeDataRef : 0)
                    .SetCostumeRefId(avatar != null ? (ulong)avatar.EquippedCostumeRef : 0)
                    .SetLevel(avatar != null ? (uint)avatar.CharacterLevel : 0)
                    .SetPrestigeLevel(avatar != null ? (uint)avatar.PrestigeLevel : 0))
                .SetCurrentPlayerName(GetName())
                .SetIsOnline(1)
                .Build();
        }

        private void DoCommunityBroadcast()
        {
            // TODO: Send a broadcast message to the player manager to update other game instances

            // Update players in this game instance
            foreach (Player otherPlayer in new PlayerIterator(Game))
            {
                Community community = otherPlayer.Community;
                CommunityMember member = community.GetMember(DatabaseUniqueId);

                // Skip communities that don't have this player as a member
                if (member == null)
                    continue;

                community.RequestLocalBroadcast(member);
            }
        }

        #endregion

        #region Scheduled Events

        private class ScheduledHUDTutorialResetEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => (t as Player).ResetHUDTutorial();
        }

        private class SwitchAvatarEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => (t as Player).SwitchAvatar();
        }

        private class CheckHoursPlayedEvent : CallMethodEvent<Player>
        {
            protected override CallbackDelegate GetCallback() => (player) => player.CheckHoursPlayed();
        }

        private class MoveToTargetEvent : CallMethodEventParam1<Entity, PrototypeId>
        {
            protected override CallbackDelegate GetCallback() => (t, p1) => ((Player)t).PlayerConnection.MoveToTarget(p1);
        }

        private class CommunityBroadcastEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => ((Player)t).DoCommunityBroadcast();
        }

        #endregion

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            sb.AppendLine($"{nameof(MissionManager)}: {MissionManager}");
            sb.AppendLine($"{nameof(_avatarProperties)}: {_avatarProperties}");
            sb.AppendLine($"{nameof(_shardId)}: {_shardId}");
            sb.AppendLine($"{nameof(_playerName)}: {_playerName}");
            sb.AppendLine($"{nameof(_consoleAccountIds)}[0]: {_consoleAccountIds[0]}");
            sb.AppendLine($"{nameof(_consoleAccountIds)}[1]: {_consoleAccountIds[1]}");
            sb.AppendLine($"{nameof(_secondaryPlayerName)}: {_secondaryPlayerName}");
            sb.AppendLine($"{nameof(MatchQueueStatus)}: {MatchQueueStatus}");
            sb.AppendLine($"{nameof(_emailVerified)}: {_emailVerified}");
            sb.AppendLine($"{nameof(_accountCreationTimestamp)}: {Clock.UnixTimeToDateTime(_accountCreationTimestamp)}");
            sb.AppendLine($"{nameof(_partyId)}: {_partyId}");

            if (_guildId != GuildMember.InvalidGuildId)
            {
                sb.AppendLine($"{nameof(_guildId)}: {_guildId}");
                sb.AppendLine($"{nameof(_guildName)}: {_guildName}");
                sb.AppendLine($"{nameof(_guildMembership)}: {_guildMembership}");
            }

            sb.AppendLine($"{nameof(_community)}: {_community}");

            for (int i = 0; i < _unlockedInventoryList.Count; i++)
                sb.AppendLine($"{nameof(_unlockedInventoryList)}[{i}]: {GameDatabase.GetPrototypeName(_unlockedInventoryList[i])}");

            if (_badges.Count > 0)
            {
                sb.Append($"{nameof(_badges)}: ");
                foreach (AvailableBadges badge in _badges)
                    sb.Append(badge.ToString()).Append(' ');
                sb.AppendLine();
            }

            sb.AppendLine($"{nameof(GameplayOptions)}: {GameplayOptions}");
            sb.AppendLine($"{nameof(AchievementState)}: {AchievementState}");

            foreach (var kvp in _stashTabOptionsDict)
                sb.AppendLine($"{nameof(_stashTabOptionsDict)}[{GameDatabase.GetFormattedPrototypeName(kvp.Key)}]: {kvp.Value}");
        }

        private struct TeleportData
        {
            public ulong RegionId { get; private set; }
            public Vector3 Position { get; private set; }
            public Orientation Orientation { get; private set; }

            public bool IsValid { get => RegionId != 0; }

            public void Set(ulong regionId, in Vector3 position, in Orientation orientation)
            {
                RegionId = regionId;
                Position = position;
                Orientation = orientation;
            }

            public void Clear()
            {
                RegionId = 0;
                Position = Vector3.Zero;
                Orientation = Orientation.Zero;
            }
        }
    }
}
