using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Social.Parties;

namespace MHServerEmu.Games.Dialog
{
    public class PartyOption : InteractionOption
    {
        public TriBool RequirementSelf { get; protected set; }
        public TriBool RequirementInParty { get; protected set; }
        public TriBool RequirementPartyLeader { get; protected set; }
        public TriBool RequirementInPartyMember { get; protected set; }

        public PartyOption()
        {
            RequirementSelf = TriBool.Undefined;
            RequirementInParty = TriBool.Undefined;
            RequirementPartyLeader = TriBool.Undefined;
            RequirementInPartyMember = TriBool.Undefined;
        }

        public override bool IsCurrentlyAvailable(EntityDesc interacteeDesc, WorldEntity localInteractee, WorldEntity interactor, InteractionFlags interactionFlags)
        {
            return base.IsCurrentlyAvailable(interacteeDesc, localInteractee, interactor, interactionFlags)
                && CheckPartyRequirements(interacteeDesc, interactor);
        }

        private bool CheckPartyRequirements(EntityDesc interacteeDesc, WorldEntity interactor)
        {
            if (interactor is not Avatar) 
                return Logger.WarnReturn(false, $"PartyOption interaction option requires an avatar interactor, but none was found for {interactor.PrototypeName}!");

            Player interactingPlayer = interactor.GetOwnerOfType<Player>();
            if (interactingPlayer == null)
                return Logger.WarnReturn(false, $"PartyOption interaction option requires a player interactor, but none was found for {interactor.PrototypeName}!");

            bool isSelf = interacteeDesc.EntityId == interactor.Id;
            if (CheckRequirement(RequirementSelf, isSelf) == false)
                return false;

            bool isInParty = interactingPlayer.PartyId != 0;
            if (CheckRequirement(RequirementInParty, isInParty) == false)
                return false;

            if (interactor.Game == null) return false;

            Party interactorParty = interactor.Party;
            bool isPartyLeader = interactorParty != null && interactorParty.IsLeader(interactingPlayer);
            if (CheckRequirement(RequirementPartyLeader, isPartyLeader) == false)
                return false;

            bool isPartyMember = interactorParty != null && interactorParty.IsMember(interacteeDesc.PlayerName);
            if (CheckRequirement(RequirementInPartyMember, isPartyMember) == false)
                return false;

            return true;
        }

        private static bool CheckRequirement(TriBool requirement, bool check)
        {
            return requirement == TriBool.Undefined 
                || (requirement == TriBool.True && check)
                || (requirement == TriBool.False && check == false);
        }
    }

    public class ChatOption : PartyOption
    {
        public ChatOption()
        {
            MethodEnum = InteractionMethod.Chat;
            RequirementSelf = TriBool.False;
            RequirementInPartyMember = TriBool.True;
        }
    }

    public class PartyBootOption : PartyOption
    {
        public PartyBootOption()
        {
            MethodEnum = InteractionMethod.PartyBoot;
            RequirementSelf = TriBool.True;
            RequirementInParty = TriBool.True;
            RequirementPartyLeader = TriBool.True;
            RequirementInPartyMember = TriBool.True;
        }
    }

    public class PartyInviteOption : PartyOption
    {
        public PartyInviteOption()
        {
            MethodEnum = InteractionMethod.PartyInvite;
            RequirementSelf = TriBool.False;
            RequirementInPartyMember = TriBool.False;
        }

        public override bool IsCurrentlyAvailable(EntityDesc interacteeDesc, WorldEntity localInteractee, WorldEntity interactor, InteractionFlags interactionFlags)
        {
            bool isAvailable = false;
            if (base.IsCurrentlyAvailable(interacteeDesc, localInteractee, interactor, interactionFlags)
                && localInteractee != null
                && localInteractee.IsHostileTo(interactor) == false)
            {
                Player interactingPlayer = interactor.GetOwnerOfType<Player>();
                if (interactingPlayer == null)
                    return Logger.WarnReturn(false, $"PartyInviteOption only works on avatars with a player, but could find one on {interactor.PrototypeName}!");

                isAvailable = interacteeDesc.PlayerName != interactingPlayer.GetName();
            }
            return isAvailable;
        }
    }

    public class PartyLeaveOption : PartyOption
    {
        public PartyLeaveOption()
        {
            MethodEnum = InteractionMethod.PartyLeave;
            RequirementSelf = TriBool.True;
            RequirementInPartyMember = TriBool.True;
        }
    }

    public class PlayerMuteOption : PartyOption
    {
        public PlayerMuteOption()
        {
            MethodEnum = InteractionMethod.Mute;
            RequirementSelf = TriBool.False;
            RequirementInParty = TriBool.True;
            RequirementInPartyMember = TriBool.True;
        }
    }

    public class GroupChangeTypeOption : PartyOption
    {
        public GroupChangeTypeOption()
        {
            MethodEnum = InteractionMethod.None;
            RequirementSelf = TriBool.True;
            RequirementInParty = TriBool.True;
            RequirementPartyLeader = TriBool.True;
        }

        public override bool IsCurrentlyAvailable(EntityDesc interacteeDesc, WorldEntity localInteractee, WorldEntity interactor, InteractionFlags interactionFlags)
        {
            bool isAvailable = false;
            if (base.IsCurrentlyAvailable(interacteeDesc, localInteractee, interactor, interactionFlags))
            {
                Player interactingPlayer = interactor.GetOwnerOfType<Player>();
                if (interactingPlayer == null)
                    return Logger.WarnReturn(false, $"GroupChangeTypeOption only works on avatars with a player, but couldn't find one on {interactor.PrototypeName}!");

                Region interactingRegion = interactor.Region;
                if (interactingRegion == null)
                    return Logger.WarnReturn(false, $"GroupChangeTypeOption only works on avatars actually in the world {interactor.PrototypeName}!");

                Party party = interactingPlayer.GetParty();
                if (party != null)
                {
                    RegionPrototype regionProto = interactingRegion.Prototype;
                    if (regionProto == null) return false;
                    isAvailable = regionProto.AllowRaids();
                }
            }
            return isAvailable;
        }
    }

    public class MakeLeaderOption : PartyOption
    {
        public MakeLeaderOption()
        {
            MethodEnum = InteractionMethod.MakeLeader;
            RequirementSelf = TriBool.False;
            RequirementInParty = TriBool.True;
            RequirementPartyLeader = TriBool.True;
            RequirementInPartyMember = TriBool.True;
        }
    }

    public class PartyShareLegendaryQuestOption : PartyOption
    {
        public PartyShareLegendaryQuestOption()
        {
            MethodEnum = InteractionMethod.PartyShareLegendaryQuest;
            RequirementPartyLeader = TriBool.True;
            RequirementInPartyMember = TriBool.True;
        }

        public override bool IsCurrentlyAvailable(EntityDesc interacteeDesc, WorldEntity localInteractee, WorldEntity interactor, InteractionFlags interactionFlags)
        {
            bool isAvailable = false;
            /* if (base.IsCurrentlyAvailable(interacteeDesc, localInteractee, interactor, interactionFlags) 
                 && localInteractee != null 
                 && localInteractee.IsHostileTo(interactor) == false)
             {
                 Player interactingPlayer = interactor.GetOwnerOfType<Player>();
                 if (interactingPlayer == null) return false;

                 MissionManager missionManager = interactingPlayer.MissionManager;
                 if (missionManager == null) return false;
             }*/ // useless code
            return isAvailable;
        }
    }

    public class TeleportOption : PartyOption
    {
        public TeleportOption()
        {
            Priority = 50;
            MethodEnum = InteractionMethod.Teleport;
            RequirementSelf = TriBool.False;
            RequirementInPartyMember = TriBool.True;
        }

        public override bool IsCurrentlyAvailable(EntityDesc interacteeDesc, WorldEntity localInteractee, WorldEntity interactor, InteractionFlags interactionFlags)
        {
            if (interactor.IsHighFlying) return false;
            return base.IsCurrentlyAvailable(interacteeDesc, localInteractee, interactor, interactionFlags);
        }
    }
}
