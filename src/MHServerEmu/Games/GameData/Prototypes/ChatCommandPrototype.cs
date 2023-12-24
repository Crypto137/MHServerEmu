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
        public ulong Description { get; private set; }
        public ChatCommandArgumentType Type { get; private set; }
        public bool Required { get; private set; }
    }

    public class ChatCommandPrototype : Prototype
    {
        public ulong Command { get; private set; }
        public ulong Description { get; private set; }
        public int Function { get; private set; }
        public ChatCommandArgumentPrototype[] Parameters { get; private set; }
        public bool ShowInHelp { get; private set; }
        public bool RespondsToSpacebar { get; private set; }
        public DesignWorkflowState DesignState { get; private set; }
    }

    public class EmoteChatCommandPrototype : ChatCommandPrototype
    {
        public ulong EmotePower { get; private set; }
        public ulong EmoteText { get; private set; }
    }

    public class ChatChannelPrototype : Prototype
    {
        public ChatMessageFormatType ChannelType { get; private set; }
        public ulong PromptText { get; private set; }
        public ulong TextStyle { get; private set; }
        public ulong DisplayName { get; private set; }
        public ulong ChatCommand { get; private set; }
        public bool ShowChannelNameInChat { get; private set; }
        public ulong ShortName { get; private set; }
        public bool ShowInChannelList { get; private set; }
        public bool VisibleOnAllTabs { get; private set; }
        public DesignWorkflowState DesignState { get; private set; }
        public bool IsGlobalChannel { get; private set; }
        public bool AllowPlayerFilter { get; private set; }
        public bool SubscribeByDefault { get; private set; }
        public bool DoHashtagFormatting { get; private set; }
        public ulong ChatPanelTabName { get; private set; }
        public bool AllowChatPanelTab { get; private set; }
        public LanguageType Language { get; private set; }
    }
}
