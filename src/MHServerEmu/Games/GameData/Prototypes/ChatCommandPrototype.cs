using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)None)]
    public enum ChatCommandArgumentType
    {
        None = 0,
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
        public LocaleStringId Description { get; protected set; }
        public ChatCommandArgumentType Type { get; protected set; }
        public bool Required { get; protected set; }
    }

    public class ChatCommandPrototype : Prototype
    {
        public LocaleStringId Command { get; protected set; }
        public LocaleStringId Description { get; protected set; }
        public AssetId Function { get; protected set; }     // TODO: this is an asset id that needs to be bound to a function (CalligraphySerializer::ParseFunctionPtr)
        public ChatCommandArgumentPrototype[] Parameters { get; protected set; }
        public bool ShowInHelp { get; protected set; }
        public bool RespondsToSpacebar { get; protected set; }
        public DesignWorkflowState DesignState { get; protected set; }
    }

    public class EmoteChatCommandPrototype : ChatCommandPrototype
    {
        public PrototypeId EmotePower { get; protected set; }
        public LocaleStringId EmoteText { get; protected set; }
    }

    public class ChatChannelPrototype : Prototype
    {
        public ChatMessageFormatType ChannelType { get; protected set; }
        public LocaleStringId PromptText { get; protected set; }
        public PrototypeId TextStyle { get; protected set; }
        public LocaleStringId DisplayName { get; protected set; }
        public PrototypeId ChatCommand { get; protected set; }
        public bool ShowChannelNameInChat { get; protected set; }
        public LocaleStringId ShortName { get; protected set; }
        public bool ShowInChannelList { get; protected set; }
        public bool VisibleOnAllTabs { get; protected set; }
        public DesignWorkflowState DesignState { get; protected set; }
        public bool IsGlobalChannel { get; protected set; }
        public bool AllowPlayerFilter { get; protected set; }
        public bool SubscribeByDefault { get; protected set; }
        public bool DoHashtagFormatting { get; protected set; }
        public LocaleStringId ChatPanelTabName { get; protected set; }
        public bool AllowChatPanelTab { get; protected set; }
        public LanguageType Language { get; protected set; }
    }
}
