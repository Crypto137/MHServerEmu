using System.Text;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.GameData;
using MHServerEmu.GameServer.Loot;

namespace MHServerEmu.GameServer.Entities.Options
{
    public enum GameplayOptionSetting
    {
        AutoPartyEnabled,
        Option1,
        DisableHeroSynergyBonusXP,
        Option3,
        EnableVaporizeCredits,
        Option5,
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
        ShowPlayerIndicator
    }

    public class GameplayOptions
    {
        public ChatChannelFilter[] ChatChannelFilters { get; set; } // ChatChannelFilterMap
        public ulong[] ChatTabChannels { get; set; }                // ChatTabState
        public ulong[] OptionSettings { get; set; }
        public ArmorRarityVaporizeThreshold[] ArmorRarityVaporizeThresholds { get; set; }

        public GameplayOptions(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            ChatChannelFilters = new ChatChannelFilter[stream.ReadRawVarint64()];
            for (int i = 0; i < ChatChannelFilters.Length; i++)
                ChatChannelFilters[i] = new(stream, boolDecoder);

            ChatTabChannels = new ulong[stream.ReadRawVarint64()];
            for (int i = 0; i < ChatTabChannels.Length; i++)
                ChatTabChannels[i] = stream.ReadPrototypeId(PrototypeEnumType.All);

            OptionSettings = new ulong[stream.ReadRawVarint64()];
            for (int i = 0; i < OptionSettings.Length; i++)
                OptionSettings[i] = stream.ReadRawVarint64();

            ArmorRarityVaporizeThresholds = new ArmorRarityVaporizeThreshold[stream.ReadRawVarint64()];
            for (int i = 0; i < ArmorRarityVaporizeThresholds.Length; i++)
                ArmorRarityVaporizeThresholds[i] = new(stream);
        }

        public GameplayOptions(ChatChannelFilter[] chatChannelFilters, ulong[] chatTabChannels, ulong[] optionSettings, ArmorRarityVaporizeThreshold[] armorRarityVaporizeThresholds)
        {
            ChatChannelFilters = chatChannelFilters;
            ChatTabChannels = chatTabChannels;
            OptionSettings = optionSettings;
            ArmorRarityVaporizeThresholds = armorRarityVaporizeThresholds;
        }

        public GameplayOptions(NetStructGameplayOptions netStruct)
        {
            ChatChannelFilters = netStruct.ChatChannelFiltersMapList.Select(filter => new ChatChannelFilter(filter)).ToArray();
            ChatTabChannels = netStruct.ChatTabChannelsArrayList.Select(channel => channel.ChannelProtoId).ToArray();
            OptionSettings = netStruct.OptionSettingsList.ToArray();

            ArmorRarityVaporizeThresholds = new ArmorRarityVaporizeThreshold[netStruct.ArmorRarityVaporizeThresholdProtoIdCount];
            for (int i = 0; i < ArmorRarityVaporizeThresholds.Length; i++)
                ArmorRarityVaporizeThresholds[i] = new((EquipmentInvUISlot)(i + 1), netStruct.ArmorRarityVaporizeThresholdProtoIdList[i]);
        }

        public void WriteBools(BoolEncoder boolEncoder)
        {
            foreach (ChatChannelFilter filter in ChatChannelFilters)
                boolEncoder.WriteBool(filter.IsSubscribed);
        }

        public byte[] Encode(BoolEncoder boolEncoder)
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint64((ulong)ChatChannelFilters.Length);
                foreach (ChatChannelFilter filter in ChatChannelFilters)
                    cos.WriteRawBytes(filter.Encode(boolEncoder));

                cos.WriteRawVarint64((ulong)ChatTabChannels.Length);
                foreach (ulong channel in ChatTabChannels)
                    cos.WritePrototypeId(channel, PrototypeEnumType.All);

                cos.WriteRawVarint64((ulong)OptionSettings.Length);
                foreach (ulong setting in OptionSettings)
                    cos.WriteRawVarint64(setting);

                cos.WriteRawVarint64((ulong)ArmorRarityVaporizeThresholds.Length);
                foreach (ArmorRarityVaporizeThreshold threshold in ArmorRarityVaporizeThresholds)
                    cos.WriteRawBytes(threshold.Encode());

                cos.Flush();
                return ms.ToArray();
            }
        }

        public NetStructGameplayOptions ToNetStruct()
        {
            return NetStructGameplayOptions.CreateBuilder()
                .AddRangeOptionSettings(OptionSettings)
                .AddRangeChatChannelFiltersMap(ChatChannelFilters.Select(filter => filter.ToNetStruct()))
                .AddRangeChatTabChannelsArray(ChatTabChannels.Select(channel => NetStructChatTabState.CreateBuilder().SetChannelProtoId(channel).Build()))
                .AddRangeArmorRarityVaporizeThresholdProtoId(ArmorRarityVaporizeThresholds.Select(threshold => threshold.RarityPrototypeId))
                .Build();
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            for (int i = 0; i < ChatChannelFilters.Length; i++) sb.AppendLine($"ChatChannelFilter{i}: {ChatChannelFilters[i]}");
            for (int i = 0; i < ChatTabChannels.Length; i++) sb.AppendLine($"ChatTabChannel{i}: {GameDatabase.GetPrototypePath(ChatTabChannels[i])}");
            for (int i = 0; i < OptionSettings.Length; i++) sb.AppendLine($"OptionSetting{i} ({(GameplayOptionSetting)i}): {OptionSettings[i]}");
            for (int i = 0; i < ArmorRarityVaporizeThresholds.Length; i++) sb.AppendLine($"ArmorRarityVaporizeThreshold{i}: {ArmorRarityVaporizeThresholds[i]}");
            return sb.ToString();
        }
    }
}
