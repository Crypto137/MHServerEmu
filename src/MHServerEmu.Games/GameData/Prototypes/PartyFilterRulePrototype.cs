using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Memory;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PartyFilterRulePrototype : Prototype
    {
        public virtual bool Evaluate(AvatarPrototype avatarProto, CostumePrototype costumeProto)
        {
            return true;
        }
    }

    public class PartyFilterRuleHasKeywordPrototype : PartyFilterRulePrototype
    {
        public PrototypeId Keyword { get; protected set; }

        public override bool Evaluate(AvatarPrototype avatarProto, CostumePrototype costumeProto)
        {
            return avatarProto.HasKeyword(Keyword);
        }
    }

    public class PartyFilterRuleHasPrototypePrototype : PartyFilterRulePrototype
    {
        public PrototypeId Avatar { get; protected set; }

        public override bool Evaluate(AvatarPrototype avatarProto, CostumePrototype costumeProto)
        {
            return avatarProto.DataRef == Avatar;
        }
    }

    public class PartyFilterRuleMemberOfTeamPrototype : PartyFilterRulePrototype
    {
        public PrototypeId Superteam { get; protected set; }

        public override bool Evaluate(AvatarPrototype avatarProto, CostumePrototype costumeProto)
        {
            return avatarProto.IsMemberOfSuperteam(Superteam);
        }
    }

    public class PartyFilterRuleWearingCostumePrototype : PartyFilterRulePrototype
    {
        public PrototypeId Costume { get; protected set; }

        public override bool Evaluate(AvatarPrototype avatarProto, CostumePrototype costumeProto)
        {
            return costumeProto.DataRef == Costume;
        }
    }

    public class PartyFilterPrototype : Prototype
    {
        public bool AllowOutsiders { get; protected set; }
        public bool AllUniqueAvatars { get; protected set; }
        public DesignWorkflowState DesignState { get; protected set; }
        public int NumberRequired { get; protected set; }
        public PartyFilterRulePrototype[] Rules { get; protected set; }

        //---

        public bool Evaluate(List<AvatarPrototype> members, List<CostumePrototype> costumes, int playerIndex)
        {
            int matches = members.Count;
            if (matches < NumberRequired || Rules.IsNullOrEmpty() || matches != costumes.Count)
                return false;

            int numMembers = matches;
            int numMatches = 0;
            var matchedAvatars = ListPool<AvatarPrototype>.Instance.Get();

            try
            {
                for (int i = 0; i < numMembers; i++)
                {
                    var avatarProto = members[i];
                    if (avatarProto == null) continue;

                    var costumeProto = costumes[i];
                    if (costumeProto == null) continue;

                    if (EvaluateAvatar(avatarProto, costumeProto))
                    {
                        if (AllUniqueAvatars)
                        {
                            if (matchedAvatars.Contains(avatarProto)) return false;
                            matchedAvatars.Add(avatarProto);
                        }

                        if ((++numMatches) == NumberRequired && AllowOutsiders && AllUniqueAvatars == false)
                            return true;
                    }
                    else if (i == playerIndex || AllowOutsiders == false)
                    {
                        return false;
                    }
                    else if ((--matches) < NumberRequired)
                    {
                        return false;
                    }
                }
            } 
            finally 
            {
                ListPool<AvatarPrototype>.Instance.Return(matchedAvatars);
            }

            if (numMatches < NumberRequired) return false;

            return true;
        }

        public bool EvaluateAvatar(AvatarPrototype avatarProto, CostumePrototype costumeProto)
        {
            if (Rules.IsNullOrEmpty()) return false;

            foreach (var ruleProto in Rules)
                if (ruleProto != null && ruleProto.Evaluate(avatarProto, costumeProto))
                    return true;

            return false;
        }

    }
}
