using System.Text;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;

namespace MHServerEmu.Games.Entities.Options
{
    public enum GameplayOptionSetting
    {
        AutoPartyEnabled,
        PreferLowPopulationRegions,     // Name from the protocol for version 1.26, may be inaccurate
        DisableHeroSynergyBonusXP,
        Setting3,
        EnableVaporizeCredits,
        Setting5,
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
        GammaLevel,
        ShowPlayerHealingNumbers,
        ShowPlayerIndicator,
        NumSettings
    }

    /// <summary>
    /// Manages various gameplay options for a specific owner <see cref="Player"/>.
    /// </summary>
    public class GameplayOptions : ISerialize
    {
        private const int NumChatTabs = 4;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private static readonly long[] GamePlayOptionDefaults = new long[]
        {
            0,      // AutoPartyEnabled
            0,      // PreferLowPopulationRegions
            0,      // DisableHeroSynergyBonusXP
            0,      // Setting3
            0,      // EnableVaporizeCredits
            0,      // Setting5
            1,      // ShowPlayerFloatingDamageNumbers
            0,      // ShowEnemyFloatingDamageNumbers
            1,      // ShowExperienceFloatingNumbers
            1,      // ShowBossIndicator
            1,      // ShowPartyMemberArrows
            100,    // MusicLevel
            100,    // SfxLevel
            0,      // ShowMovieSubtitles
            100,    // MicLevel
            100,    // SpeakerLevel
            80,     // GammaLevel
            0,      // ShowPlayerHealingNumbers
            1       // ShowPlayerIndicator
        };

        private Player _owner;

        private long[] _optionSettings = new long[(int)GameplayOptionSetting.NumSettings];                      // Various settings (see enum above)
        private SortedDictionary<PrototypeId, bool> _chatChannelFilterDict = new();                             // Whether the channel is included in the main chat tab 
        private PrototypeId[] _chatTabChannels = new PrototypeId[NumChatTabs];                                  // Chat channels bound to tabs other than the main one
        private SortedDictionary<EquipmentInvUISlot, PrototypeId> _armorRarityVaporizeThresholdDict = new();    // PetTech item vacuum settings

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
            if (numSettings > (int)GameplayOptionSetting.NumSettings)
            {
                Logger.Warn($"GameplayOptions(): numSettings > GameplayOptionSetting.NumSettings");
                numSettings = (int)GameplayOptionSetting.NumSettings;
            }

            for (int i = 0; i < numSettings; i++)
                _optionSettings[i] = (long)netStruct.OptionSettingsList[i];

            // Chat channel filters
            foreach (var filter in netStruct.ChatChannelFiltersMapList)
                _chatChannelFilterDict.Add((PrototypeId)filter.ChannelProtoId, filter.IsSubscribed);

            // Chat tab channels
            int numChannels = netStruct.ChatTabChannelsArrayCount;
            if (numChannels > NumChatTabs)
            {
                Logger.Warn($"GameplayOptions(): numTabs > NumChatTabs");
                numChannels = NumChatTabs;
            }

            for (int i = 0; i < numChannels; i++)
                _chatTabChannels[i] = (PrototypeId)netStruct.ChatTabChannelsArrayList[i].ChannelProtoId;

            // Vaporize thresholds
            for (var slot = EquipmentInvUISlot.Gear01; slot <= EquipmentInvUISlot.Gear05; slot++)
            {
                int index = (int)slot - 1;
                if (index >= netStruct.ArmorRarityVaporizeThresholdProtoIdCount)
                {
                    Logger.Warn($"GameplayOptions(): index >= netStruct.ArmorRarityVaporizeThresholdProtoIdCount");
                    continue;
                }

                _armorRarityVaporizeThresholdDict[slot] = (PrototypeId)netStruct.ArmorRarityVaporizeThresholdProtoIdList[(int)slot - 1]; ;
            }
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            // NOTE: Archives use a different encoding order from protobufs: filters - tabs - options - thresholds.
            // The client implementation includes a lot of legacy backward compatibility code that we don't really need.

            success &= Serializer.Transfer(archive, ref _chatChannelFilterDict);
            success &= Serializer.Transfer(archive, ref _chatTabChannels);
            success &= Serializer.Transfer(archive, ref _optionSettings);
            success &= Serializer.Transfer(archive, ref _armorRarityVaporizeThresholdDict);

            return success;
        }

        public bool Decode(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            // NOTE: Archives use a different encoding order from protobufs (filters - tabs - options - thresholds)

            // Chat channel filters
            _chatChannelFilterDict.Clear();
            ulong numChatChannelFilters = stream.ReadRawVarint64();
            for (ulong i = 0; i < numChatChannelFilters; i++)
            {
                PrototypeId channelProtoId = stream.ReadPrototypeRef<Prototype>();
                bool isSubscribed = boolDecoder.ReadBool(stream);
                _chatChannelFilterDict.Add(channelProtoId, isSubscribed);
            }

            // Chat tab channels
            Array.Clear(_chatTabChannels);
            ulong numChatTabChannels = stream.ReadRawVarint64();
            if (numChatTabChannels > NumChatTabs)
                return Logger.ErrorReturn(false, $"numChatTabChannels {numChatTabChannels} > NumChatTabs {NumChatTabs}");

            for (int i = 0; i < _chatTabChannels.Length; i++)
                _chatTabChannels[i] = stream.ReadPrototypeRef<Prototype>();

            // Settings
            Array.Clear(_optionSettings);
            ulong numSettings = stream.ReadRawVarint64();
            for (ulong i = 0; i < numSettings; i++)
                _optionSettings[i] = (long)stream.ReadRawVarint64();

            // Vaporize thresholds
            _armorRarityVaporizeThresholdDict.Clear();
            ulong numVaporizeThresholds = stream.ReadRawVarint64();
            for (ulong i = 0; i < numVaporizeThresholds; i++)
            {
                var slot = (EquipmentInvUISlot)stream.ReadRawVarint64();
                var rarityPrototypeRef = stream.ReadPrototypeRef<Prototype>();
                _armorRarityVaporizeThresholdDict[slot] = rarityPrototypeRef;
            }

            return true;
        }

        public void EncodeBools(BoolEncoder boolEncoder)
        {
            foreach (bool isEnabled in _chatChannelFilterDict.Values)
                boolEncoder.EncodeBool(isEnabled);
        }

        public void Encode(CodedOutputStream stream, BoolEncoder boolEncoder)
        {
            // Chat channel filters
            stream.WriteRawVarint64((ulong)_chatChannelFilterDict.Count);
            foreach (var kvp in _chatChannelFilterDict)
            {
                stream.WritePrototypeRef<Prototype>(kvp.Key);
                boolEncoder.WriteBuffer(stream);    // isEnabled
            }

            // Chat tab channels
            stream.WriteRawVarint64((ulong)_chatTabChannels.Length);
            foreach (PrototypeId channel in _chatTabChannels)
                stream.WritePrototypeRef<Prototype>(channel);

            // Settings
            stream.WriteRawVarint64((ulong)_optionSettings.Length);
            foreach (long setting in _optionSettings)
                stream.WriteRawInt64(setting);

            // Vaporize thresholds
            stream.WriteRawVarint64((ulong)_armorRarityVaporizeThresholdDict.Count);
            foreach (var kvp in _armorRarityVaporizeThresholdDict)
            {
                stream.WriteRawVarint64((ulong)kvp.Key);
                stream.WritePrototypeRef<Prototype>(kvp.Value);
            }
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
        public long GetOptionSetting(GameplayOptionSetting settingEnum)
        {
            return _optionSettings[(int)settingEnum];
        }

        /// <summary>
        /// Returns the default value of the specified <see cref="GameplayOptionSetting"/>.
        /// </summary>
        public long GetOptionSettingDefault(GameplayOptionSetting settingEnum)
        {
            return GamePlayOptionDefaults[(int)settingEnum];
        }

        /// <summary>
        /// Sets the value of the specified <see cref="GameplayOptionSetting"/>.
        /// </summary>
        public void SetOptionSetting(GameplayOptionSetting setting, long value, bool doUpdate)
        {
            _optionSettings[(int)setting] = value;
            if (doUpdate) DoUpdate();
        }

        /// <summary>
        /// Sets the value of the specified <see cref="GameplayOptionSetting"/>.
        /// </summary>
        public void SetOptionSetting(GameplayOptionSetting setting, bool value, bool doUpdate)
        {
            SetOptionSetting(setting, Convert.ToInt64(value), doUpdate);
        }

        /// <summary>
        /// Returns <see langword="true"/> if the chat channel with the specified <see cref="PrototypeId"/> is included in the main chat tab.
        /// </summary>
        public bool IsChannelFiltered(PrototypeId channelProtoRef)
        {
            if (_chatChannelFilterDict.TryGetValue(channelProtoRef, out bool value) == false)
                return GetChannelDefaultSubscription(channelProtoRef);

            return value;
        }

        /// <summary>
        /// Includes or removes the chat channel with the specified <see cref="PrototypeId"/> from the main chat tab.
        /// </summary>
        public bool SetChatChannelFilter(PrototypeId channelProtoRef, bool value, bool doUpdate)
        {
            bool oldValue = IsChannelFiltered(channelProtoRef);
            _chatChannelFilterDict[channelProtoRef] = value;
            if (value == oldValue) return false;
            if (doUpdate) DoUpdate();
            return true;
        }

        /// <summary>
        /// Returns the <see cref="PrototypeId"/> of the chat channel in the specified tab.
        /// </summary>
        public PrototypeId GetChatTabChannel(int tabIndex)
        {
            if ((tabIndex >= 0 && tabIndex < NumChatTabs) == false)
                return Logger.WarnReturn(PrototypeId.Invalid, $"GetChatTabChannel(): Invalid tabIndex {tabIndex}");

            return _chatTabChannels[tabIndex];
        }

        /// <summary>
        /// Sets the chat channel in the specified tab to the one corresponding to the provided <see cref="PrototypeId"/>.
        /// </summary>
        public bool SetChatTabChannel(int tabIndex, PrototypeId channelProtoRef, bool doUpdate)
        {
            if ((tabIndex >= 0 && tabIndex < NumChatTabs) == false)
                return Logger.WarnReturn(false, $"SetChatTabChannel(): Invalid tabIndex {tabIndex}");

            if (_chatTabChannels[tabIndex] == channelProtoRef) return false;
            _chatTabChannels[tabIndex] = channelProtoRef;
            if (doUpdate) DoUpdate();
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

            if (_armorRarityVaporizeThresholdDict.TryGetValue(slot, out PrototypeId rarityRef) == false)
                return PrototypeId.Invalid;

            return rarityRef;
        }

        /// <summary>
        /// Sets the <see cref="PrototypeId"/> of the vaporize rarity threshold for the specified <see cref="EquipmentInvUISlot"/>.
        /// </summary>
        public bool SetArmorRarityVaporizeThreshold(PrototypeId rarityRef, EquipmentInvUISlot slot)
        {
            if (IsGearSlotVaporizing(slot) == false)
                return Logger.WarnReturn(false, $"SetArmorRarityVaporizeThreshold(): {slot} is not a valid vaporize slot");

            PrototypeId oldRarityRef = _armorRarityVaporizeThresholdDict[slot];
            if (rarityRef == oldRarityRef) return false;

            _armorRarityVaporizeThresholdDict[slot] = rarityRef;
            DoUpdate();
            return true;
        }

        /// <summary>
        /// Resets all options in this <see cref="GameplayOptions"/> instance to their default values.
        /// </summary>
        public void ResetToDefaults()
        {
            // Option settings
            Array.Clear(_optionSettings);
            for (int i = 0; i < (int)GameplayOptionSetting.NumSettings; i++)
                _optionSettings[i] = GamePlayOptionDefaults[i];

            // Chat channel filters
            _chatChannelFilterDict.Clear();
            foreach(PrototypeId channelProtoRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy(typeof(ChatChannelPrototype), PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                var chatChannelPrototype = channelProtoRef.As<ChatChannelPrototype>();
                if (chatChannelPrototype.AllowPlayerFilter)
                    _chatChannelFilterDict[channelProtoRef] = GetChannelDefaultSubscription(channelProtoRef);
            }

            // Chat tabs
            Array.Clear(_chatTabChannels);

            // Vaporize thresholds
            for (var slot = EquipmentInvUISlot.Gear01; slot <= EquipmentInvUISlot.Gear05; slot++)
            {
                if (IsGearSlotVaporizing(slot) == false) continue;
                _armorRarityVaporizeThresholdDict[slot] = PrototypeId.Invalid;
            }
        }

        /// <summary>
        /// Converts this <see cref="GameplayOptions"/> instance to <see cref="NetStructGameplayOptions"/>.
        /// </summary>
        public NetStructGameplayOptions ToProtobuf()
        {
            var builder = NetStructGameplayOptions.CreateBuilder();

            builder.AddRangeOptionSettings(_optionSettings.Select(setting => (ulong)setting));

            builder.AddRangeChatChannelFiltersMap(_chatChannelFilterDict.Select(kvp => NetStructChatChannelFilterState.CreateBuilder()
                .SetChannelProtoId((ulong)kvp.Key)
                .SetIsSubscribed(kvp.Value)
                .Build()));

            builder.AddRangeChatTabChannelsArray(_chatTabChannels.Select(channel => NetStructChatTabState.CreateBuilder()
                .SetChannelProtoId((ulong)channel)
                .Build()));

            builder.AddRangeArmorRarityVaporizeThresholdProtoId(_armorRarityVaporizeThresholdDict.Select(kvp => (ulong)kvp.Value));

            return builder.Build();
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            for (int i = 0; i < _optionSettings.Length; i++)
                sb.AppendLine($"{nameof(_optionSettings)}[{(GameplayOptionSetting)i}]: {_optionSettings[i]}");

            foreach (var kvp in _chatChannelFilterDict)
                sb.AppendLine($"{nameof(_chatChannelFilterDict)}[{GameDatabase.GetFormattedPrototypeName(kvp.Key)}]: {kvp.Value}");

            for (int i = 0; i < _chatTabChannels.Length; i++)
                sb.AppendLine($"{nameof(_chatTabChannels)}[{i}]: {GameDatabase.GetFormattedPrototypeName(_chatTabChannels[i])}");

            foreach (var kvp in _armorRarityVaporizeThresholdDict)
                sb.AppendLine($"{nameof(_armorRarityVaporizeThresholdDict)}[{kvp.Key}]: {GameDatabase.GetFormattedPrototypeName(kvp.Value)}");

            return sb.ToString();
        }

        /// <summary>
        /// Returns the default subscription value for the specified chat channel <see cref="PrototypeId"/>.
        /// </summary>
        private bool GetChannelDefaultSubscription(PrototypeId chatChannelRef)
        {
            var chatChannelPrototype = GameDatabase.GetPrototype<ChatChannelPrototype>(chatChannelRef);
            if (chatChannelPrototype == null)
                return Logger.WarnReturn(false, $"GetChannelDefaultSubscription(): chatChannelRef {chatChannelRef} is invalid");

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
