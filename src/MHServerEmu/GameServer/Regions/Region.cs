using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.GameServer.Common;
using MHServerEmu.Networking;

namespace MHServerEmu.GameServer.Regions
{
    public class Region
    {
        public RegionPrototype Prototype { get; }
        public ulong Id { get; }
        public int RandomSeed { get; }
        public byte[] ArchiveData { get; }
        public Vector3 Min { get; }
        public Vector3 Max { get; }
        public CreateRegionParams CreateParams { get; }

        public List<Area> AreaList { get; } = new();

        public Vector3 EntrancePosition { get; set; }
        public Vector3 EntranceOrientation { get; set; }
        public Vector3 WaypointPosition { get; set; }
        public Vector3 WaypointOrientation { get; set; }

        public Region(RegionPrototype prototype, ulong id, int randomSeed, byte[] archiveData, Vector3 min, Vector3 max, CreateRegionParams createParams)
        {
            Prototype = prototype;
            Id = id;
            RandomSeed = randomSeed;
            ArchiveData = archiveData;
            Min = min;
            Max = max;
            CreateParams = createParams;
        }

        public void AddArea(Area area) => AreaList.Add(area);

        public GameMessage[] GetLoadingMessages(ulong serverGameId)
        {
            List<GameMessage> messageList = new();

            // Before changing to the actual destination region the game seems to first change into a transitional region
            messageList.Add(new(NetMessageRegionChange.CreateBuilder()
                .SetRegionId(0)
                .SetServerGameId(0)
                .SetClearingAllInterest(false)
                .Build()));

            messageList.Add(new(NetMessageQueueLoadingScreen.CreateBuilder()
                .SetRegionPrototypeId((ulong)Prototype)
                .Build()));

            messageList.Add(new(NetMessageRegionChange.CreateBuilder()
                .SetRegionPrototypeId((ulong)Prototype)
                .SetServerGameId(serverGameId)
                .SetClearingAllInterest(false)
                .SetRegionId(Id)
                .SetRegionRandomSeed(RandomSeed)
                .SetCreateRegionParams(CreateParams.ToNetStruct())
                .SetRegionMin(Min.ToNetStructPoint3())
                .SetRegionMax(Max.ToNetStructPoint3())
                .Build()));

            // mission updates and entity creation happens here

            // why is there a second NetMessageQueueLoadingScreen?
            messageList.Add(new(NetMessageQueueLoadingScreen.CreateBuilder().SetRegionPrototypeId((ulong)Prototype).Build()));

            // TODO: prefetch other regions

            foreach (Area area in AreaList)
            {
                messageList.Add(new((byte)GameServerToClientMessage.NetMessageAddArea, NetMessageAddArea.CreateBuilder()
                    .SetAreaId(area.Id)
                    .SetAreaPrototypeId((ulong)area.Prototype)
                    .SetAreaOrigin(area.Origin.ToNetStructPoint3())
                    .SetIsStartArea(area.IsStartArea)
                    .Build().ToByteArray()));

                foreach (Cell cell in area.CellList)
                {
                    var builder = NetMessageCellCreate.CreateBuilder()
                        .SetAreaId(area.Id)
                        .SetCellId(cell.Id)
                        .SetCellPrototypeId(cell.PrototypeId)
                        .SetPositionInArea(cell.PositionInArea.ToNetStructPoint3())
                        .SetCellRandomSeed(RandomSeed)
                        .SetBufferwidth(0)
                        .SetOverrideLocationName(0);

                    foreach (ReservedSpawn reservedSpawn in cell.EncounterList)
                        builder.AddEncounters(reservedSpawn.ToNetStruct());

                    messageList.Add(new(builder.Build()));
                }
            }

            messageList.Add(new(NetMessageEnvironmentUpdate.CreateBuilder().SetFlags(1).Build()));

            messageList.Add(new(NetMessageUpdateMiniMap.CreateBuilder()
                .SetArchiveData(ByteString.CopyFrom(Convert.FromHexString("EF0181")))
                .Build()));

            return messageList.ToArray();
        }
    }
}
