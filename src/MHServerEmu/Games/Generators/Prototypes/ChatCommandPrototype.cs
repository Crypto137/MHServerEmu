using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{

    public class ChatCommandArgumentPrototype : Prototype
    {
        public ulong Description;
        public ChatCommandArgumentType Type;
        public bool Required;
        public ChatCommandArgumentPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ChatCommandArgumentPrototype), proto); }
    }
    public enum ChatCommandArgumentType
    {
        Boolean = 1,
        Float = 2,
        Integer = 3,
        String = 4,
    }

    public class ChatCommandPrototype : Prototype
    {
        public ulong Command;
        public ulong Description;
        public int Function;
        public ChatCommandArgumentPrototype[] Parameters;
        public bool ShowInHelp;
        public bool RespondsToSpacebar;
        public DesignWorkflowState DesignState;
        public ChatCommandPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ChatCommandPrototype), proto); }
    }

    public class EmoteChatCommandPrototype : ChatCommandPrototype
    {
        public ulong EmotePower;
        public ulong EmoteText;
        public EmoteChatCommandPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EmoteChatCommandPrototype), proto); }
    }

    public class ChatChannelPrototype : Prototype
    {
        public ChatMessageFormatTypes ChannelType;
        public ulong PromptText;
        public ulong TextStyle;
        public ulong DisplayName;
        public ulong ChatCommand;
        public bool ShowChannelNameInChat;
        public ulong ShortName;
        public bool ShowInChannelList;
        public bool VisibleOnAllTabs;
        public DesignWorkflowState DesignState;
        public bool IsGlobalChannel;
        public bool AllowPlayerFilter;
        public bool SubscribeByDefault;
        public bool DoHashtagFormatting;
        public ulong ChatPanelTabName;
        public bool AllowChatPanelTab;
        public LanguageType Language;
        public ChatChannelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ChatChannelPrototype), proto); }
    }

    public enum ChatMessageFormatTypes
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
}
