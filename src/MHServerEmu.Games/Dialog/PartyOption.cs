namespace MHServerEmu.Games.Dialog
{
    public class PartyOption : InteractionOption
    {
    }

    public class ChatOption : PartyOption
    {
        public ChatOption()
        {
            MethodEnum = InteractionMethod.Chat;
        }
    }

    public class PartyBootOption : PartyOption
    {
        public PartyBootOption()
        {
            MethodEnum = InteractionMethod.PartyBoot;
        }
    }

    public class PartyInviteOption : PartyOption
    {
        public PartyInviteOption()
        {
            MethodEnum = InteractionMethod.PartyInvite;
        }
    }

    public class PartyLeaveOption : PartyOption
    {
        public PartyLeaveOption()
        {
            MethodEnum = InteractionMethod.PartyLeave;
        }
    }

    public class PlayerMuteOption : PartyOption
    {
        public PlayerMuteOption()
        {
            MethodEnum = InteractionMethod.Mute;
        }
    }

    public class GroupChangeTypeOption : PartyOption
    {
        public GroupChangeTypeOption()
        {
            MethodEnum = InteractionMethod.None;
        }
    }

    public class MakeLeaderOption : PartyOption
    {
        public MakeLeaderOption()
        {
            MethodEnum = InteractionMethod.MakeLeader;
        }
    }

    public class PartyShareLegendaryQuestOption : PartyOption
    {
        public PartyShareLegendaryQuestOption()
        {
            MethodEnum = InteractionMethod.PartyShareLegendaryQuest;
        }
    }

    public class TeleportOption : PartyOption
    {
        public TeleportOption()
        {
            Priority = 50;
            MethodEnum = InteractionMethod.Teleport;
        }
    }
}
