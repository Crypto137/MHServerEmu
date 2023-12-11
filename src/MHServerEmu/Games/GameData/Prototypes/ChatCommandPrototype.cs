using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum ChatCommandArgumentType
    {
        Boolean = 1,
        Float = 2,
        Integer = 3,
        String = 4,
    }

    [AssetEnum]
    public enum ChatMessageFormatType
    {
        None = -1,
        ChatLocal = 0,
        ChatSay = 1,
        ChatParty = 2,
        ChatTell = 3,
        ChatBroadcast = 4,
        ChatSocialChinese = 22,
        ChatSocialEnglish = 5,
        ChatSocialFrench = 6,
        ChatSocialGerman = 7,
        ChatSocialGreek = 8,
        ChatSocialJapanese = 21,
        ChatSocialKorean = 9,
        ChatSocialPortuguese = 10,
        ChatSocialRussian = 11,
        ChatSocialSpanish = 12,
        ChatTrade = 13,
        ChatLFG = 14,
        ChatGuild = 15,
        ChatFaction = 16,
        ChatEmote = 17,
        ChatEndgame = 18,
        ChatMetaGame = 19,
        ChatGuildOfficer = 20,
        ChatAll = 21,
        ChatMission = 22,
        CombatLog = 23,
        SystemInfo = 24,
        SystemError = 25,
        Gifting = 26,
    }

    [AssetEnum]
    public enum LanguageType
    {
        Chinese = 1,
        English = 2,
        French = 3,
        German = 4,
        Greek = 5,
        Japanese = 6,
        Korean = 7,
        Portuguese = 8,
        Russian = 9,
        Spanish = 10,
    }

    #endregion

    public class ChatCommandArgumentPrototype : Prototype
    {
        public ulong Description { get; set; }
        public ChatCommandArgumentType Type { get; set; }
        public bool Required { get; set; }
    }

    public class ChatCommandPrototype : Prototype
    {
        public ulong Command { get; set; }
        public ulong Description { get; set; }
        public int Function { get; set; }
        public ChatCommandArgumentPrototype[] Parameters { get; set; }
        public bool ShowInHelp { get; set; }
        public bool RespondsToSpacebar { get; set; }
        public DesignWorkflowState DesignState { get; set; }
    }

    public class EmoteChatCommandPrototype : ChatCommandPrototype
    {
        public ulong EmotePower { get; set; }
        public ulong EmoteText { get; set; }
    }

    public class ChatChannelPrototype : Prototype
    {
        public ChatMessageFormatType ChannelType { get; set; }
        public ulong PromptText { get; set; }
        public ulong TextStyle { get; set; }
        public ulong DisplayName { get; set; }
        public ulong ChatCommand { get; set; }
        public bool ShowChannelNameInChat { get; set; }
        public ulong ShortName { get; set; }
        public bool ShowInChannelList { get; set; }
        public bool VisibleOnAllTabs { get; set; }
        public DesignWorkflowState DesignState { get; set; }
        public bool IsGlobalChannel { get; set; }
        public bool AllowPlayerFilter { get; set; }
        public bool SubscribeByDefault { get; set; }
        public bool DoHashtagFormatting { get; set; }
        public ulong ChatPanelTabName { get; set; }
        public bool AllowChatPanelTab { get; set; }
        public LanguageType Language { get; set; }
    }
}
