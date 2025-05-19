using System.Diagnostics;
using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Leaderboards
{   
    /// <summary>
    /// A singleton containing cached leaderboard state data used by game instances.
    /// </summary>
    public class LeaderboardInfoCache
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<PrototypeGuid, LeaderboardInfo> _leaderboardInfoMap = new();

        public static LeaderboardInfoCache Instance { get; } = new();

        private LeaderboardInfoCache() { }

        /// <summary>
        /// Initializes the <see cref="LeaderboardInfoCache"/> instance.
        /// </summary>
        public bool Initialize()
        {
            var stopwatch = Stopwatch.StartNew();

            int count = 0;

            // Load leaderboard prototypes
            foreach (var dataRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<LeaderboardPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                var proto = GameDatabase.GetPrototype<LeaderboardPrototype>(dataRef);
                if (proto == null)
                {
                    Logger.Warn($"Prototype {dataRef} == null");
                    continue;
                }

                if (proto.Public == false)
                    continue;

                var guid = GameDatabase.GetPrototypeGuid(dataRef);
                _leaderboardInfoMap[guid] = new(proto);
                count++;
            }

            Logger.Info($"Initialized {count} leaderboards in {stopwatch.ElapsedMilliseconds} ms");

            return true;
        }

        public void GetActiveLeaderboardPrototypes(List<LeaderboardPrototype> activeLeaderboards)
        {
            // Get all prototypes in one go instead of using an iterator to minimize lock time.
            lock (_leaderboardInfoMap)
            {
                foreach (var leaderboard in _leaderboardInfoMap.Values)
                    foreach (var instance in leaderboard.Instances)
                        if (instance.State == LeaderboardState.eLBS_Active)
                            activeLeaderboards.Add(leaderboard.Prototype);
            }
        }

        public bool GetLeaderboardInstances(PrototypeGuid guid, List<LeaderboardInstanceInfo> instances)
        {
            lock (_leaderboardInfoMap)
            {
                if (_leaderboardInfoMap.TryGetValue(guid, out LeaderboardInfo info) == false)
                    return false;

                if (info.Prototype == null)
                    return false;

                int maxInstances = info.Prototype.MaxArchivedInstances;
                foreach (LeaderboardInstanceInfo instance in info.Instances)
                {
                    instances.Add(instance);
                    if (--maxInstances < 0)
                        break;
                }

                return true;
            }
        }

        public NetMessageLeaderboardInitializeRequestResponse BuildInitializeRequestResponse(NetMessageLeaderboardInitializeRequest initializeRequest)
        {
            var response = NetMessageLeaderboardInitializeRequestResponse.CreateBuilder();
            List<LeaderboardInstanceInfo> instances = ListPool<LeaderboardInstanceInfo>.Instance.Get();

            lock (_leaderboardInfoMap)
            {
                foreach (ulong guid in initializeRequest.LeaderboardIdsList)
                {
                    instances.Clear();
                    if (GetLeaderboardInstances((PrototypeGuid)guid, instances))
                    {
                        var initDataBuilder = LeaderboardInitData.CreateBuilder().SetLeaderboardId(guid);
                        foreach (var instance in instances)
                        {
                            var instanceData = instance.ToProtobuf();
                            if (instance.State == LeaderboardState.eLBS_Active || instance.State == LeaderboardState.eLBS_Created)
                                initDataBuilder.SetCurrentInstanceData(instanceData);
                            else
                                initDataBuilder.AddArchivedInstanceList(instanceData);
                        }
                        response.AddLeaderboardInitDataList(initDataBuilder.Build());
                    }
                }
            }

            ListPool<LeaderboardInstanceInfo>.Instance.Return(instances);
            return response.Build();
        }

        public void UpdateLeaderboardInstances(in GameServiceProtocol.LeaderboardStateChangeList instances)
        {
            lock (_leaderboardInfoMap)
            {
                foreach (var instance in instances)
                    UpdateLeaderboardInstance(instance);
            }
        }

        public void UpdateLeaderboardInstance(in GameServiceProtocol.LeaderboardStateChange instanceInfo)
        {
            lock (_leaderboardInfoMap)
            {
                if (_leaderboardInfoMap.TryGetValue((PrototypeGuid)instanceInfo.LeaderboardId, out var leaderboardInfo))
                {
                    ulong instanceId = instanceInfo.InstanceId;
                    var updateInstance = leaderboardInfo.Instances.Find(instance => instance.InstanceId == instanceId);

                    if (updateInstance != null)
                        updateInstance.Update(instanceInfo);
                    else
                        leaderboardInfo.SortedInsertInstance(new(instanceInfo));
                }
                else
                {
                    var dataRef = GameDatabase.GetDataRefByPrototypeGuid((PrototypeGuid)instanceInfo.LeaderboardId);
                    var proto = GameDatabase.GetPrototype<LeaderboardPrototype>(dataRef);

                    if (proto != null)
                    {
                        leaderboardInfo = new(proto);
                        leaderboardInfo.SortedInsertInstance(new(instanceInfo));
                        _leaderboardInfoMap[(PrototypeGuid)instanceInfo.LeaderboardId] = leaderboardInfo;
                    }
                }
            }
        }
    }
}
