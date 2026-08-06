using System.Text;
using Gazillion;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;

namespace MHServerEmu.Games.Entities.Options
{
    public enum GameplayOptionSetting   // Names from Gazillion::GameplayOptions::terminatedStringFromGameplayOptionsEnum
    {
        AutoPartyEnabled,
        PreferLowPopulationRegions,
        DisableHeroSynergyBonusXP,
        QueueWithoutSeekingOtherPlayers,
        VaporizeCredits,
        VaporizeMedKits,
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        ShowPlayerFloatingDamageNumbers,
        ShowEnemyFloatingDamageNumbers,
        ShowExperienceFloatingNumbers,
        ShowBossIndicator,
        ShowPartyMemberArrows,
        MusicLevel,
        SfxLevel,
        ShowMovieSubtitles,
        MicLevel,
        SpeakerLevel,
        Gamma,
        ShowPlayerHealingNumbers,
        ShowPlayerIndicator,
#endif
#if GAME_VERSION_1_53
        Vibration,
        ShowPlayerName,
        ShowPlayerTargetIndicator,
        CameraShake,
        MocoLevel,
        VoiceoverLevel,
        PlayerDialogLevel,
        ModifierButtonInstantPowerActivation,
#endif
        Count,
    }

    // NOTE: Pre-BUE game versions stored default settings as booleans instead of integers,
    // but we use integers for all versions for simplicity.
    public readonly struct GameplayOptionDefault(GameplayOptionSetting defaultEnum, long defaultValue)
    {
        public readonly GameplayOptionSetting DefaultEnum = defaultEnum;
        public readonly long DefaultValue = defaultValue;
    }

    /// <summary>
    /// Manages various gameplay options for a specific owner <see cref="Player"/>.
    /// </summary>
    public class GameplayOptions : ISerialize
    {
        private const int NumChatTabs = 4;

        private static readonly IReadOnlyList<GameplayOptionDefault> GamePlayOptionDefaults =
        [
            new(GameplayOptionSetting.AutoPartyEnabled,                     0),
            new(GameplayOptionSetting.PreferLowPopulationRegions,           0),
            new(GameplayOptionSetting.DisableHeroSynergyBonusXP,            0),
            new(GameplayOptionSetting.QueueWithoutSeekingOtherPlayers,      0),
            new(GameplayOptionSetting.VaporizeCredits,                      0),
            new(GameplayOptionSetting.VaporizeMedKits,                      0),
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            new(GameplayOptionSetting.ShowPlayerFloatingDamageNumbers,      1),
            new(GameplayOptionSetting.ShowEnemyFloatingDamageNumbers,       0),
            new(GameplayOptionSetting.ShowExperienceFloatingNumbers,        1),
            new(GameplayOptionSetting.ShowBossIndicator,                    1),
            new(GameplayOptionSetting.ShowPartyMemberArrows,                1),
            new(GameplayOptionSetting.MusicLevel,                           100),
            new(GameplayOptionSetting.SfxLevel,                             100),
            new(GameplayOptionSetting.ShowMovieSubtitles,                   0),
            new(GameplayOptionSetting.MicLevel,                             100),
            new(GameplayOptionSetting.SpeakerLevel,                         100),
            new(GameplayOptionSetting.Gamma,                                80),
            new(GameplayOptionSetting.ShowPlayerHealingNumbers,             0),
            new(GameplayOptionSetting.ShowPlayerIndicator,                  1),
#endif
#if GAME_VERSION_1_53
            new(GameplayOptionSetting.Vibration,                            1),
            new(GameplayOptionSetting.ShowPlayerName,                       1),
            new(GameplayOptionSetting.ShowPlayerTargetIndicator,            0),
            new(GameplayOptionSetting.CameraShake,                          1),
            new(GameplayOptionSetting.MocoLevel,                            100),
            new(GameplayOptionSetting.VoiceoverLevel,                       100),
            new(GameplayOptionSetting.PlayerDialogLevel,                    100),
            new(GameplayOptionSetting.ModifierButtonInstantPowerActivation, 1),
#endif
        ];

        private Player _owner;

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        private long[] _optionSettings = new long[(int)GameplayOptionSetting.Count];
#else
        private GBitArray _optionSettings = new();
#endif
        private SortedDictionary<PrototypeId, bool> _chatChannelFilters = new();                            // Whether the channel is included in the main chat tab 
        private PrototypeId[] _chatTabChannels = new PrototypeId[NumChatTabs];                              // Chat channels bound to tabs other than the main one
        private SortedDictionary<EquipmentInvUISlot, PrototypeId> _armorRarityVaporizeThresholds = new();   // PetTech item vacuum settings

        /// <summary>
        /// Constructs a new <see cref="GameplayOptions"/> instance for the provided owner <see cref="Player"/>.
        /// </summary>
        public GameplayOptions(Player owner = null)
        {
            _owner = owner;
            ResetToDefaults();
        }

        /// <summary>
        /// Constructs a new <see cref="GameplayOptions"/> instance from the provided <see cref="NetStructGameplayOptions"/>.
        /// </summary>
        public GameplayOptions(NetStructGameplayOptions netStruct)
        {
            // Settings
            int numSettings = netStruct.OptionSettingsCount;
            if (!Verify.IsTrue(numSettings <= (int)GameplayOptionSetting.Count))
                numSettings = (int)GameplayOptionSetting.Count;

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            for (int i = 0; i < numSettings; i++)
                _optionSettings[i] = (long)netStruct.OptionSettingsList[i];
#else
            for (int i = 0; i < numSettings; i++)
                _optionSettings[i] = netStruct.OptionSettingsList[i];
#endif

            // Chat channel filters
            foreach (var filter in netStruct.ChatChannelFiltersMapList)
                _chatChannelFilters.Add((PrototypeId)filter.ChannelProtoId, filter.IsSubscribed);

            // Chat tab channels
            int numChannels = netStruct.ChatTabChannelsArrayCount;
            if (!Verify.IsTrue(numChannels <= NumChatTabs))
                numChannels = NumChatTabs;

            for (int i = 0; i < numChannels; i++)
                _chatTabChannels[i] = (PrototypeId)netStruct.ChatTabChannelsArrayList[i].ChannelProtoId;

            // Vaporize thresholds
            for (var slot = EquipmentInvUISlot.Gear01; slot <= EquipmentInvUISlot.Gear05; slot++)
            {
                int index = (int)slot - 1;
                if (!Verify.IsTrue(index < netStruct.ArmorRarityVaporizeThresholdProtoIdCount))
                    continue;

                if (!Verify.IsTrue(IsGearSlotVaporizing(slot)))
                    continue;

                _armorRarityVaporizeThresholds[slot] = (PrototypeId)netStruct.ArmorRarityVaporizeThresholdProtoIdList[(int)slot - 1]; ;
            }
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            // NOTE: The order is different for pre-BUE and BUE.
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            success &= Serializer.Transfer(archive, ref _chatChannelFilters);
            success &= Serializer.Transfer(archive, _chatTabChannels);
            success &= Serializer.Transfer(archive, _optionSettings);
            success &= Serializer.Transfer(archive, ref _armorRarityVaporizeThresholds);
#else
            success &= Serializer.Transfer(archive, ref _optionSettings);
            success &= Serializer.Transfer(archive, ref _chatChannelFilters);
            success &= Serializer.Transfer(archive, _chatTabChannels);
            success &= Serializer.Transfer(archive, ref _armorRarityVaporizeThresholds);
#endif

            return success;
        }

        /// <summary>
        /// Sets the owner <see cref="Player"/> of this <see cref="GameplayOptions"/> instance.
        /// </summary>
        public void SetOwner(Player player)
        {
            _owner = player;
        }

        /// <summary>
        /// Returns the current value of the specified <see cref="GameplayOptionSetting"/>.
        /// </summary>
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        public long GetOptionSetting(GameplayOptionSetting settingEnum)
        {
            return _optionSettings[(int)settingEnum];
        }
#else
        public bool GetOptionSetting(GameplayOptionSetting settingEnum)
        {
            return _optionSettings[(int)settingEnum];
        }
#endif

        /// <summary>
        /// Returns the default value of the specified <see cref="GameplayOptionSetting"/>.
        /// </summary>
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        public long GetOptionSettingDefault(GameplayOptionSetting settingEnum)
        {
            return GamePlayOptionDefaults[(int)settingEnum].DefaultValue;
        }
#else
        public bool GetOptionSettingDefault(GameplayOptionSetting settingEnum)
        {
            return GamePlayOptionDefaults[(int)settingEnum].DefaultValue != 0;
        }
#endif

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        /// <summary>
        /// Sets the value of the specified <see cref="GameplayOptionSetting"/>.
        /// </summary>
        public void SetOptionSetting(GameplayOptionSetting setting, long value, bool doUpdate)
        {
            _optionSettings[(int)setting] = value;
            if (doUpdate) DoUpdate();
        }
#endif

        /// <summary>
        /// Sets the value of the specified <see cref="GameplayOptionSetting"/>.
        /// </summary>
        public void SetOptionSetting(GameplayOptionSetting setting, bool value, bool doUpdate)
        {
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            SetOptionSetting(setting, Convert.ToInt64(value), doUpdate);
#else
            _optionSettings[(int)setting] = value;
            if (doUpdate) DoUpdate();
#endif
        }

        /// <summary>
        /// Returns <see langword="true"/> if the chat channel with the specified <see cref="PrototypeId"/> is included in the main chat tab.
        /// </summary>
        public bool IsChannelFiltered(PrototypeId channelProtoRef)
        {
            if (_chatChannelFilters.TryGetValue(channelProtoRef, out bool value) == false)
                return GetChannelDefaultSubscription(channelProtoRef);

            return value;
        }

        /// <summary>
        /// Includes or removes the chat channel with the specified <see cref="PrototypeId"/> from the main chat tab.
        /// </summary>
        public bool SetChatChannelFilter(PrototypeId channelProtoRef, bool value, bool doUpdate)
        {
            bool oldValue = IsChannelFiltered(channelProtoRef);
            _chatChannelFilters[channelProtoRef] = value;
            if (value == oldValue) return false;
            if (doUpdate) DoUpdate();
            return true;
        }

        /// <summary>
        /// Returns the <see cref="PrototypeId"/> of the chat channel in the specified tab.
        /// </summary>
        public PrototypeId GetChatTabChannel(int tabIndex)
        {
            if (!Verify.IsTrue(tabIndex >= 0 && tabIndex < NumChatTabs)) return PrototypeId.Invalid;
            return _chatTabChannels[tabIndex];
        }

        /// <summary>
        /// Sets the chat channel in the specified tab to the one corresponding to the provided <see cref="PrototypeId"/>.
        /// </summary>
        public bool SetChatTabChannel(int tabIndex, PrototypeId channelProtoRef, bool doUpdate)
        {
            if (!Verify.IsTrue(tabIndex >= 0 && tabIndex < NumChatTabs)) return false;

            if (_chatTabChannels[tabIndex] == channelProtoRef)
                return false;

            _chatTabChannels[tabIndex] = channelProtoRef;
            if (doUpdate)
                DoUpdate();

            return true;
        }

        /// <summary>
        /// Returns <see langword="true"/> if the chat channel with the specified <see cref="PrototypeId"/> is enabled.
        /// </summary>
        public bool IsSubscribedToChannel(PrototypeId channelProtoRef)
        {
            return IsChannelFiltered(channelProtoRef) || IsChatTabCreatedForChannel(channelProtoRef);
        }

        /// <summary>
        /// Returns the <see cref="PrototypeId"/> of the vaporize rarity threshold for the specified <see cref="EquipmentInvUISlot"/>.
        /// </summary>
        public PrototypeId GetArmorRarityVaporizeThreshold(EquipmentInvUISlot slot)
        {
            if (IsGearSlotVaporizing(slot) == false)
                return PrototypeId.Invalid;

            if (_armorRarityVaporizeThresholds.TryGetValue(slot, out PrototypeId rarityRef) == false)
                return PrototypeId.Invalid;

            return rarityRef;
        }

        /// <summary>
        /// Sets the <see cref="PrototypeId"/> of the vaporize rarity threshold for the specified <see cref="EquipmentInvUISlot"/>.
        /// </summary>
        public void SetArmorRarityVaporizeThreshold(PrototypeId rarityRef, EquipmentInvUISlot slot)
        {
            if (!Verify.IsTrue(IsGearSlotVaporizing(slot))) return;

            if (rarityRef == _armorRarityVaporizeThresholds[slot])
                return;

            _armorRarityVaporizeThresholds[slot] = rarityRef;
            DoUpdate();
        }

        /// <summary>
        /// Resets all options in this <see cref="GameplayOptions"/> instance to their default values.
        /// </summary>
        public void ResetToDefaults()
        {
            // Option settings
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            Array.Clear(_optionSettings);
            for (int i = 0; i < (int)GameplayOptionSetting.Count; i++)
                _optionSettings[i] = GamePlayOptionDefaults[i].DefaultValue;
#else
            _optionSettings.Clear();
            for (int i = 0; i < (int)GameplayOptionSetting.Count; i++)
                _optionSettings[i] = GamePlayOptionDefaults[i].DefaultValue != 0;
#endif

            // Chat channel filters
            _chatChannelFilters.Clear();
            foreach(PrototypeId channelProtoRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<ChatChannelPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                var chatChannelPrototype = channelProtoRef.As<ChatChannelPrototype>();
                if (chatChannelPrototype.AllowPlayerFilter)
                    _chatChannelFilters[channelProtoRef] = GetChannelDefaultSubscription(channelProtoRef);
            }

            // Chat tabs
            Array.Clear(_chatTabChannels);

            // Vaporize thresholds
            for (var slot = EquipmentInvUISlot.Gear01; slot <= EquipmentInvUISlot.Gear05; slot++)
            {
                if (IsGearSlotVaporizing(slot) == false) continue;
                _armorRarityVaporizeThresholds[slot] = PrototypeId.Invalid;
            }
        }

        /// <summary>
        /// Converts this <see cref="GameplayOptions"/> instance to <see cref="NetStructGameplayOptions"/>.
        /// </summary>
        public NetStructGameplayOptions ToProtobuf()
        {
            var builder = NetStructGameplayOptions.CreateBuilder();

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            builder.AddRangeOptionSettings(_optionSettings.Select(setting => (ulong)setting));
#else
            for (int i = 0; i < (int)GameplayOptionSetting.Count; i++)
                builder.AddOptionSettings(_optionSettings[i]);
#endif

            builder.AddRangeChatChannelFiltersMap(_chatChannelFilters.Select(kvp => NetStructChatChannelFilterState.CreateBuilder()
                .SetChannelProtoId((ulong)kvp.Key)
                .SetIsSubscribed(kvp.Value)
                .Build()));

            builder.AddRangeChatTabChannelsArray(_chatTabChannels.Select(channel => NetStructChatTabState.CreateBuilder()
                .SetChannelProtoId((ulong)channel)
                .Build()));

            builder.AddRangeArmorRarityVaporizeThresholdProtoId(_armorRarityVaporizeThresholds.Select(kvp => (ulong)kvp.Value));

            return builder.Build();
        }

        public override string ToString()
        {
            StringBuilder sb = new();

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            for (int i = 0; i < _optionSettings.Length; i++)
                sb.AppendLine($"{nameof(_optionSettings)}[{(GameplayOptionSetting)i}]: {_optionSettings[i]}");
#else
            for (GameplayOptionSetting i = 0; i < GameplayOptionSetting.Count; i++)
                sb.AppendLine($"{nameof(_optionSettings)}[{i}]: {_optionSettings[(int)i]}");
#endif

            foreach (var kvp in _chatChannelFilters)
                sb.AppendLine($"{nameof(_chatChannelFilters)}[{GameDatabase.GetFormattedPrototypeName(kvp.Key)}]: {kvp.Value}");

            for (int i = 0; i < _chatTabChannels.Length; i++)
                sb.AppendLine($"{nameof(_chatTabChannels)}[{i}]: {GameDatabase.GetFormattedPrototypeName(_chatTabChannels[i])}");

            foreach (var kvp in _armorRarityVaporizeThresholds)
                sb.AppendLine($"{nameof(_armorRarityVaporizeThresholds)}[{kvp.Key}]: {GameDatabase.GetFormattedPrototypeName(kvp.Value)}");

            return sb.ToString();
        }

        /// <summary>
        /// Returns the default subscription value for the specified chat channel <see cref="PrototypeId"/>.
        /// </summary>
        private bool GetChannelDefaultSubscription(PrototypeId chatChannelRef)
        {
            ChatChannelPrototype chatChannelPrototype = chatChannelRef.As<ChatChannelPrototype>();
            if (!Verify.IsNotNull(chatChannelPrototype)) return false;

            // TODO: LocaleManager::GetCurrentLocale();
            // Assume the locale is English for now
            switch (chatChannelPrototype.Language)
            {
                case LanguageType.All:
                case LanguageType.English:
                    return chatChannelPrototype.SubscribeByDefault;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns <see langword="true"/> is the chat channel with the specified <see cref="PrototypeId"/> has a chat tab dedicated to it.
        /// </summary>
        private bool IsChatTabCreatedForChannel(PrototypeId chatChannelRef)
        {
            return _chatTabChannels.Contains(chatChannelRef);
        }

        /// <summary>
        /// Returns <see langword="true"/> if the specified <see cref="EquipmentInvUISlot"/> can be vaporized.
        /// </summary>
        private bool IsGearSlotVaporizing(EquipmentInvUISlot slot)
        {
            return slot >= EquipmentInvUISlot.Gear01 && slot <= EquipmentInvUISlot.Gear05;
        }

        /// <summary>
        /// Sends updated <see cref="GameplayOptions"/> to the owner <see cref="Player"/>.
        /// </summary>
        private bool DoUpdate()
        {
            if (_owner == null) return false;
            // This is where the client calls CPlayer::SendGameplayOptionsToServer().
            // The server has nothing to do here since there are no messages that
            // include NetStructGameplayOptions in the GameServerToClient protocol.
            return true;
        }
    }
}
