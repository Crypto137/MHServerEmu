using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Social.Parties
{
    public class PartyMemberInfo
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public ulong PlayerDbId { get; private set; }
        public string PlayerName { get; private set; }
        public HashSet<PrototypeId> Boosts { get; } = new();    // the client uses a sorted map here
        public ulong ConsoleAccountId { get; private set; }
        public ulong SecondaryConsoleAccountId { get; private set; }
        public string SecondaryPlayerName { get; private set; }

        public PartyMemberInfo()
        {
        }

        public override string ToString()
        {
            return $"{PlayerName}, PlayerDbGuid=0x{PlayerDbId:X}";
        }

        public void SetFromMsg(Gazillion.PartyMemberInfo protobuf)
        {
            PlayerDbId = protobuf.PlayerDbId;
            PlayerName = protobuf.PlayerName;
            ConsoleAccountId = protobuf.HasConsoleAccountId ? protobuf.ConsoleAccountId : 0;
            SecondaryConsoleAccountId = protobuf.HasSecondaryConsoleAccountId ? protobuf.SecondaryConsoleAccountId : 0;
            SecondaryPlayerName = protobuf.HasSecondaryPlayerName ? protobuf.SecondaryPlayerName : string.Empty;

            Boosts.Clear();
            for (int i = 0; i < protobuf.BoostsCount; i++)
            {
                PrototypeId boostRef = GameDatabase.GetDataRefByPrototypeGuid((PrototypeGuid)protobuf.BoostsList[i]);
                if (boostRef == PrototypeId.Invalid)
                {
                    Logger.Warn("SetFromMsg(): boostRef == PrototypeId.Invalid");
                    continue;
                }

                Boosts.Add(boostRef);
            }
        }
    }
}
