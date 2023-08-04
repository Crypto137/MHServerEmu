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
        public Point3 Min { get; }
        public Point3 Max { get; }
        public CreateRegionParams CreateParams { get; }

        public List<Area> AreaList { get; } = new();

        public Region(RegionPrototype prototype, ulong id, int randomSeed, byte[] archiveData, Point3 min, Point3 max, CreateRegionParams createParams)
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
            messageList.Add(new(GameServerToClientMessage.NetMessageRegionChange, NetMessageRegionChange.CreateBuilder()
                .SetRegionId(0)
                .SetServerGameId(0)
                .SetClearingAllInterest(false)
                .Build().ToByteArray()));

            messageList.Add(new(GameServerToClientMessage.NetMessageQueueLoadingScreen, NetMessageQueueLoadingScreen.CreateBuilder()
                .SetRegionPrototypeId((ulong)Prototype)
                .Build().ToByteArray()));

            messageList.Add(new(GameServerToClientMessage.NetMessageRegionChange, NetMessageRegionChange.CreateBuilder()
                .SetRegionPrototypeId((ulong)Prototype)
                .SetServerGameId(serverGameId)
                .SetClearingAllInterest(false)
                .SetRegionId(Id)
                .SetRegionRandomSeed(RandomSeed)
                .SetCreateRegionParams(CreateParams.ToNetStruct())
                .SetRegionMin(Min.ToNetStruct())
                .SetRegionMax(Max.ToNetStruct())
                .Build().ToByteArray()));

            // mission updates and entity creation happens here

            // why is there a second NetMessageQueueLoadingScreen?
            messageList.Add(new(GameServerToClientMessage.NetMessageQueueLoadingScreen, NetMessageQueueLoadingScreen.CreateBuilder()
                .SetRegionPrototypeId((ulong)Prototype)
                .Build().ToByteArray()));

            // TODO: prefetch other regions

            foreach (Area area in AreaList)
            {
                messageList.Add(new((byte)GameServerToClientMessage.NetMessageAddArea, NetMessageAddArea.CreateBuilder()
                    .SetAreaId(area.Id)
                    .SetAreaPrototypeId((ulong)area.Prototype)
                    .SetAreaOrigin(area.Origin.ToNetStruct())
                    .SetIsStartArea(area.IsStartArea)
                    .Build().ToByteArray()));

                foreach (Cell cell in area.CellList)
                {
                    messageList.Add(new(GameServerToClientMessage.NetMessageCellCreate, NetMessageCellCreate.CreateBuilder()
                        .SetAreaId(area.Id)
                        .SetCellId(cell.Id)
                        .SetCellPrototypeId(cell.PrototypeId)
                        .SetPositionInArea(cell.PositionInArea.ToNetStruct())
                        .SetCellRandomSeed(RandomSeed)
                        .SetBufferwidth(0)
                        .SetOverrideLocationName(0)
                        .Build().ToByteArray()));
                }
            }

            messageList.Add(new((byte)GameServerToClientMessage.NetMessageEnvironmentUpdate, NetMessageEnvironmentUpdate.CreateBuilder()
                .SetFlags(1)
                .Build().ToByteArray()));

            messageList.Add(new(GameServerToClientMessage.NetMessageUpdateMiniMap, NetMessageUpdateMiniMap.CreateBuilder()
                .SetArchiveData(ByteString.CopyFrom(new byte[] { 0xEF, 0x01, 0x81 }))
                .Build().ToByteArray()));

            return messageList.ToArray();
        }
    }
}
