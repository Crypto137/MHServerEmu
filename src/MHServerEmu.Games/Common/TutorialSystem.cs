#if GAME_VERSION_1_48
using Gazillion;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Common
{
    public static class TutorialSystem
    {
        public static bool ShouldShowTips(Player player)
        {
            return player.Properties[PropertyEnum.TutorialTipsEnabled];
        }

        public static bool ShouldShowTip(Player player, TipPrototype tipProto)
        {
            if (ShouldShowTips(player) == false)
                return false;

            if (tipProto.AlwaysShow)
                return true;

            if (tipProto.ShowForEachAvatar)
            {
                Avatar avatar = player.CurrentAvatar;
                if (avatar == null)
                    return false;

                return avatar.Properties[PropertyEnum.TutorialHasSeenTip, tipProto.DataRef] == false;
            }
            else
            {
                return player.Properties[PropertyEnum.TutorialHasSeenTip, tipProto.DataRef] == false;
            }
        }

        public static void ShowTip(Player player, TipPrototype tipProto)
        {
            if (ShouldShowTip(player, tipProto) == false)
                return;

            NetMessageShowTutorialTip message = NetMessageShowTutorialTip.CreateBuilder()
                .SetTipDataRefId((ulong)tipProto.DataRef)
                .Build();

            player.SendMessage(message);
        }
    }
}
#endif
