using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions
{
    public class MissionManager : ISerialize
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private PrototypeId _avatarPrototypeRef;
        private Dictionary<PrototypeId, Mission> _missionDict = new();
        private SortedDictionary<PrototypeGuid, List<PrototypeGuid>> _legendaryMissionBlacklist = new();

        public Player Player { get; private set; }       
        public Game Game { get; private set; }
        public IMissionManagerOwner Owner { get; set; }

        private ulong _regionId; 
        private HashSet<ulong> _missionInterestEntities = new();

        public MissionManager() { }

        public bool Serialize(Archive archive)
        {
            bool success = true;
            success &= Serializer.Transfer(archive, ref _avatarPrototypeRef);
            success &= SerializeMissions(archive);
            return success;
        }

        public void Decode(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            _avatarPrototypeRef = stream.ReadPrototypeRef<Prototype>();

            // MissionManager::SerializeMissions()
            _missionDict.Clear();
            ulong numMissions = stream.ReadRawVarint64();
            for (ulong i = 0; i < numMissions; i++)
            {
                PrototypeGuid missionGuid = (PrototypeGuid)stream.ReadRawVarint64();
                PrototypeId missionRef = GameDatabase.GetDataRefByPrototypeGuid(missionGuid);
                Mission mission = CreateMission(missionRef);
                mission.Decode(stream, boolDecoder);
                InsertMission(mission);
            }

            _legendaryMissionBlacklist.Clear();
            int numCategories = stream.ReadRawInt32();
            for (int i = 0; i < numCategories; i++)
            {
                PrototypeGuid category = (PrototypeGuid)stream.ReadRawVarint64();

                List<PrototypeGuid> categoryMissionList = new();
                ulong numBlacklistCategoryMissions = stream.ReadRawVarint64();
                for (ulong j = 0; j < numBlacklistCategoryMissions; j++)
                    categoryMissionList.Add((PrototypeGuid)stream.ReadRawVarint64());

                _legendaryMissionBlacklist.Add(category, categoryMissionList);
            }
        }

        public void EncodeBools(BoolEncoder boolEncoder)
        {
            foreach (var mission in _missionDict)
                boolEncoder.EncodeBool(mission.Value.IsSuspended);
        }

        public void Encode(CodedOutputStream stream, BoolEncoder boolEncoder)
        {
            stream.WritePrototypeRef<Prototype>(_avatarPrototypeRef);

            // MissionManager::SerializeMissions()
            stream.WriteRawVarint64((ulong)_missionDict.Count);
            foreach (var kvp in _missionDict)
            {
                PrototypeGuid missionGuid = GameDatabase.GetPrototypeGuid(kvp.Key);
                stream.WriteRawVarint64((ulong)missionGuid);    // missionGuid
                kvp.Value.Encode(stream, boolEncoder);
            }

            stream.WriteRawInt32(_legendaryMissionBlacklist.Count);
            foreach (var kvp in _legendaryMissionBlacklist)
            {
                stream.WriteRawVarint64((ulong)kvp.Key);        // category

                List<PrototypeGuid> categoryMissionList = kvp.Value;
                stream.WriteRawVarint64((ulong)categoryMissionList.Count);
                foreach (PrototypeGuid guid in categoryMissionList)
                    stream.WriteRawVarint64((ulong)guid);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(_avatarPrototypeRef)}: {GameDatabase.GetPrototypeName(_avatarPrototypeRef)}");

            foreach (var kvp in _missionDict)
                sb.AppendLine($"{nameof(_missionDict)}[{kvp.Key}]: {kvp.Value}");

            foreach (var kvp in _legendaryMissionBlacklist)
            {
                string categoryName = Path.GetFileNameWithoutExtension(GameDatabase.GetPrototypeNameByGuid(kvp.Key));
                sb.AppendLine($"{nameof(_legendaryMissionBlacklist)}[{categoryName}]:");
                foreach (PrototypeGuid guid in kvp.Value)
                    sb.AppendLine(GameDatabase.GetPrototypeNameByGuid(guid));
            }

            return sb.ToString();
        }

        public MissionManager(Game game, IMissionManagerOwner owner)
        {
            Game = game;
            Owner = owner;
        }

        public bool InitializeForPlayer(Player player, Region region)
        {
            if (player == null) return false;

            Player = player;
            SetRegion(region);

            return true;
        }

        public bool SetAvatar(PrototypeId avatarPrototypeRef)
        {
            // TODO: Pass the avatar instance itself rather than its prototype and do all the necessary initialization
            _avatarPrototypeRef = avatarPrototypeRef;
            return true;
        }

        public bool IsPlayerMissionManager()
        {
            return (Owner != null) && Owner is Player;
        }

        public bool IsRegionMissionManager()
        {
            return (Owner != null) && Owner is Region;
        }

        public bool InitializeForRegion(Region region)
        {
            if (region == null)  return false;

            Player = null;
            SetRegion(region);

            return true;
        }

        private void SetRegion(Region region)
        {
            _regionId = region != null ? region.Id : 0;
        }

        public Region GetRegion()
        {
            if (_regionId == 0 || Game == null) return null;
            return RegionManager.GetRegion(Game, _regionId);
        }

        public Mission CreateMission(PrototypeId missionRef)
        {
            return new(this, missionRef);
        }

        public Mission InsertMission(Mission mission)
        {
            if (mission == null) return null;
            _missionDict.Add(mission.PrototypeDataRef, mission); 
            return mission;
        }

        internal void Shutdown(Region region)
        {
            throw new NotImplementedException();
        }

        public bool GenerateMissionPopulation()
        {
            Region region = GetRegion();
            // search all Missions with encounter
            foreach (var missionRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy(typeof(MissionPrototype), PrototypeIterateFlags.NoAbstract | PrototypeIterateFlags.ApprovedOnly))
            {
                MissionPrototype missionProto = GameDatabase.GetPrototype<MissionPrototype>(missionRef);
                if (missionProto == null) continue;
                if (missionProto.HasPopulationInRegion(region) == false) continue;
                if (IsMissionValidAndApprovedForUse(missionProto))
                    region.PopulationManager.MissionRegistry(missionProto);
            }
            return true;
        }

        /// <summary>
        /// Returns <see langword="true"/> if the provided <see cref="MissionPrototype"/> is valid for this <see cref="MissionManager"/> instance.
        /// </summary>
        public bool ShouldCreateMission(MissionPrototype missionPrototype)
        {
            if (missionPrototype == null)
                return Logger.WarnReturn(false, "ShouldCreateMission(): missionPrototype == false");

            if (missionPrototype is OpenMissionPrototype)
            {
                if (IsRegionMissionManager() == false)
                    return false;
            }
            else
            {
                if (IsPlayerMissionManager() == false)
                    return false;
            }

            return IsMissionValidAndApprovedForUse(missionPrototype);
        }

        private bool SerializeMissions(Archive archive)
        {
            bool success = true;

            ulong numMissions = (ulong)_missionDict.Count;
            success &= Serializer.Transfer(archive, ref numMissions);

            if (archive.IsPacking)
            {
                foreach (var kvp in _missionDict)
                {
                    ulong guid = (ulong)GameDatabase.GetPrototypeGuid(kvp.Key);
                    success &= Serializer.Transfer(archive, ref guid);

                    ISerialize mission = kvp.Value;
                    success &= Serializer.Transfer(archive, ref mission);
                }

                int numBlacklistCategories = _legendaryMissionBlacklist.Count;
                success &= Serializer.Transfer(archive, ref numBlacklistCategories);
                foreach (var kvp in _legendaryMissionBlacklist)
                {
                    ulong categoryGuid = (ulong)kvp.Key;
                    success &= Serializer.Transfer(archive, ref categoryGuid);

                    List<PrototypeGuid> categoryMissionList = kvp.Value;
                    success &= Serializer.Transfer(archive, ref categoryMissionList);
                }
            }
            else
            {
                for (ulong i = 0; i < numMissions; i++)
                {
                    ulong guid = 0;
                    success &= Serializer.Transfer(archive, ref guid);

                    PrototypeId missionRef = GameDatabase.GetDataRefByPrototypeGuid((PrototypeGuid)guid);
                    Mission mission = CreateMission(missionRef);
                    success &= mission.Serialize(archive);
                    InsertMission(mission);
                }

                int numBlacklistCategories = 0;
                success &= Serializer.Transfer(archive, ref numBlacklistCategories);
                if (numBlacklistCategories == 0) return success;

                _legendaryMissionBlacklist.Clear();
                for (int i = 0; i < numBlacklistCategories; i++)
                {
                    ulong categoryGuid = 0;
                    success &= Serializer.Transfer(archive, ref categoryGuid);

                    List<PrototypeGuid> categoryMissionList = new();
                    success &= Serializer.Transfer(archive, ref categoryMissionList);

                    _legendaryMissionBlacklist.Add((PrototypeGuid)categoryGuid, categoryMissionList);
                }
            }

            return success;
        }

        /// <summary>
        /// Validates the provided <see cref="MissionPrototype"/>.
        /// </summary>
        private bool IsMissionValidAndApprovedForUse(MissionPrototype missionPrototype)
        {
            if (missionPrototype == null)
                return false;

            if (missionPrototype.ApprovedForUse() == false)
                return false;

            if (missionPrototype is OpenMissionPrototype
             || missionPrototype is LegendaryMissionPrototype
             || missionPrototype is DailyMissionPrototype
             || missionPrototype is AdvancedMissionPrototype)
            {
                if (missionPrototype.IsLiveTuningEnabled() == false)
                    return false;
            }

            // TODO: Game::OmegaMissionsEnabled() && missionPrototype is DailyMissionPrototype
            bool omegaMissionsEnabled = true;
            if (omegaMissionsEnabled == false && missionPrototype is DailyMissionPrototype)
                return false;

            return true;
        }

        public static Mission FindMissionForPlayer(Player player, PrototypeId missionRef)
        {
            MissionManager missionManager = FindMissionManagerForMission(player, player.GetRegion(), missionRef);
            if (missionManager == null)
            {
                Console.WriteLine($"Couldn't find appropriate mission manager on player {player} for mission [{GameDatabase.GetPrototypeName(missionRef)}].");
                return null;
            }
            return missionManager.FindMissionByDataRef(missionRef);
        }

        private static MissionManager FindMissionManagerForMission(Player player, Region region, PrototypeId missionRef)
        {
            return FindMissionManagerForMission(player, region, missionRef.As<MissionPrototype>());
        }

        private static MissionManager FindMissionManagerForMission(Player player, Region region, MissionPrototype missionProto)
        {
            if (player != null)
            {
                MissionManager playerMissionManager = player.MissionManager;
                if (playerMissionManager != null && playerMissionManager.ShouldCreateMission(missionProto))
                    return playerMissionManager;
            }

            if (region != null)
            {
                MissionManager regionMissionManager = region.MissionManager;
                if (regionMissionManager != null && regionMissionManager.ShouldCreateMission(missionProto))
                    return regionMissionManager;
            }

            return null;
        }

        private Mission FindMissionByDataRef(PrototypeId missionRef)
        {
            if (_missionDict.TryGetValue(missionRef, out var mission))
                return mission;
            else
                return null;
        }

        public static readonly MissionPrototypeId[] DisabledMissions = new MissionPrototypeId[]
        {
            MissionPrototypeId.CH00TrainingPathingController,
            MissionPrototypeId.CH00NPETrainingRoom,

            MissionPrototypeId.CivilWarDailyCapOM01DefeatSpiderman,
            MissionPrototypeId.CivilWarDailyCapOM02DestroyCrates,
            MissionPrototypeId.CivilWarDailyCapOM03DefeatThor,
            MissionPrototypeId.CivilWarDailyCapOM04SaveDumDum,
            MissionPrototypeId.CivilWarDailyCapOM05HydraZoo,
            MissionPrototypeId.CivilWarDailyCapOM06TeamUpDefeatSHIELD,
            MissionPrototypeId.CivilWarDailyCapOM07InteractDefeatTurrets,
            MissionPrototypeId.CivilWarDailyIronmanOM01DefeatSpiderman,
            MissionPrototypeId.CivilWarDailyIronmanOM02DefeatThor,
            MissionPrototypeId.CivilWarDailyIronmanOM03SaveJocasta,
            MissionPrototypeId.CivilWarDailyIronmanOM04DestroyCrates,
            MissionPrototypeId.CivilWarDailyIronmanOM05HydraZoo,
            MissionPrototypeId.CivilWarDailyIronmanOM06TeamUpDefeatAIM,
            MissionPrototypeId.CivilWarDailyIronmanOM07InteractDefeatHand,

            MissionPrototypeId.Ch09ActivateSiegeDoorDefense,
        };

        // TODO replace this mission to MetaStates
        public static readonly MissionPrototypeId[] EventMissions = new MissionPrototypeId[]
        {
            /*
            MissionPrototypeId.MoloidAttackAftermath,
            MissionPrototypeId.Moloid3AgainstLeaper,
            MissionPrototypeId.MoloidRescueCivilian,
            MissionPrototypeId.MoloidAmbushBreakIn,
            */

            MissionPrototypeId.NorwayFrostGolemsFaeAmbushV1,
            MissionPrototypeId.NorwayFrostGolemsFaeAmbushV2,
            MissionPrototypeId.NorwayFrostGolemsFaeAmbushV3,
            MissionPrototypeId.NorwayFrostGolemsFaeAmbushV4,
            MissionPrototypeId.NorwayFrostGolemsFaeAmbushV5,
            MissionPrototypeId.NorwayFrostGolemsFaeAmbushV6,
            MissionPrototypeId.NorwayFrostGolemsFaeAmbushV7,
            MissionPrototypeId.NorwayFrostGolemsFaeAmbushV8,
            MissionPrototypeId.NorwayFrostGolemsFaeAmbushV9,
            MissionPrototypeId.NorwayFrostGolemsMeleeAmbushV1,
            MissionPrototypeId.NorwayFrostGolemsMeleeAmbushV2,
            MissionPrototypeId.NorwayFrostGolemsMeleeAmbushV3,
            MissionPrototypeId.NorwayFrostGolemsMeleeAmbushV4,
            MissionPrototypeId.NorwayFrostGolemsMeleeAmbushV5,
            MissionPrototypeId.NorwayFrostGolemsMeleeAmbushV6,
            MissionPrototypeId.NorwayFrostGolemsMeleeAmbushV7,
            MissionPrototypeId.NorwayFrostGolemsMeleeAmbushV8,
            MissionPrototypeId.NorwayFrostGolemsMeleeAmbushV9,
            MissionPrototypeId.NorwayFrostGolemsRangedAmbushV1,
            MissionPrototypeId.NorwayFrostGolemsRangedAmbushV2,
            MissionPrototypeId.NorwayFrostGolemsRangedAmbushV3,
            MissionPrototypeId.NorwayFrostGolemsRangedAmbushV4,
            MissionPrototypeId.NorwayFrostGolemsRangedAmbushV5,
            MissionPrototypeId.NorwayFrostGolemsRangedAmbushV6,
            MissionPrototypeId.NorwayFrostGolemsRangedAmbushV7,
            MissionPrototypeId.NorwayFrostGolemsRangedAmbushV8,
            MissionPrototypeId.NorwayFrostGolemsRangedAmbushV9,

            MissionPrototypeId.PoliceVsShark,
            MissionPrototypeId.CivTrappedUnderRhino,
            MissionPrototypeId.NamedEliteLizardMonkey, 
        };

        public enum MissionPrototypeId : ulong
        {
            CH00TrainingPathingController = 3126128604301631533,
            CH00NPETrainingRoom = 17508547083537161214,

            CivilWarDailyCapOM01DefeatSpiderman = 422011357013684087,
            CivilWarDailyCapOM02DestroyCrates = 16726105122650140376,
            CivilWarDailyCapOM03DefeatThor = 17525168409710964083,
            CivilWarDailyCapOM04SaveDumDum = 1605098401643834761,
            CivilWarDailyCapOM05HydraZoo = 16108444317179587775,
            CivilWarDailyCapOM06TeamUpDefeatSHIELD = 16147585525915463870,
            CivilWarDailyCapOM07InteractDefeatTurrets = 11425191689973609005,

            CivilWarDailyIronmanOM01DefeatSpiderman = 10006467310735077687,
            CivilWarDailyIronmanOM02DefeatThor = 10800373542996422450,
            CivilWarDailyIronmanOM03SaveJocasta = 1692932771743412129,
            CivilWarDailyIronmanOM04DestroyCrates = 2469191070689800346,
            CivilWarDailyIronmanOM05HydraZoo = 14812369129072701055,
            CivilWarDailyIronmanOM06TeamUpDefeatAIM = 6784016171053232444,
            CivilWarDailyIronmanOM07InteractDefeatHand = 8062690480896488047,

            // Event Missions
            LavaBugOverCiv1 = 3051637045813386860,
            LavaBugOverCiv2 = 12951210928479411821,
            LavaBugOverCiv3 = 8229534989490265710,
            MoloidAttackAftermath = 9846291500756181529,
            Moloid3AgainstLeaper = 7901699126451183992,
            MoloidRescueCivilian = 2105266359721140667,
            MoloidAmbushBreakIn = 8273714847963488577,

            // Ch05MutantTown
            OMMutantsUnderFire = 1307786597808155026,
            MutantsRunningGroup1 = 10873519943997006861,
            MutantsRunningGroup2 = 1082243550031913998,
            MutantRunningSoloF5 = 6582400594476082068,
            OMSentinelAttack = 8470993979061837457,
            OMNgaraiInvasion = 17739825775665686436,
            // Ch07SavageLand
            OMRaptorVillageSurvival = 9997628235003932057,
            OMBroodSensors = 18170546091391854063,
            SunTribeKingLizard = 4490088042433880038,
            SunTribeLeadingRaptors = 10007010211070222742,
            // Ch08Latveria
            OMCommArray = 4824312982332121730,
            OMSHIELDBeachhead = 8114921592377321192,
            // Ch09Asgard
            OMStoneCircle = 3980473410108269374,
            OMForgottenPyre = 10224091465615418680,
            OMAshesToAshes = 6056188340475601950,
            OMNorwaySHIELDAssist = 4758892475970890088,
            // Ambushes
            NorwayFrostGolemsFaeAmbushV1 = 6885407105936335832,
            NorwayFrostGolemsFaeAmbushV2 = 14298796090790781913,
            NorwayFrostGolemsFaeAmbushV3 = 567832374723683290,
            NorwayFrostGolemsFaeAmbushV4 = 3376095577277866971,
            NorwayFrostGolemsFaeAmbushV5 = 17245570270655488988,
            NorwayFrostGolemsFaeAmbushV6 = 8554293696219063261,
            NorwayFrostGolemsFaeAmbushV7 = 13202349635148653534,
            NorwayFrostGolemsFaeAmbushV8 = 2648362653723272159,
            NorwayFrostGolemsFaeAmbushV9 = 16523144139972355040,
            NorwayFrostGolemsMeleeAmbushV1 = 11237279034766402740,
            NorwayFrostGolemsMeleeAmbushV2 = 148816423127164085,
            NorwayFrostGolemsMeleeAmbushV3 = 14095823178937410742,
            NorwayFrostGolemsMeleeAmbushV4 = 16908054501776368823,
            NorwayFrostGolemsMeleeAmbushV5 = 3110762746763683000,
            NorwayFrostGolemsMeleeAmbushV6 = 12883039579802248377,
            NorwayFrostGolemsMeleeAmbushV7 = 8306643211702772922,
            NorwayFrostGolemsMeleeAmbushV8 = 16194921426191328443,
            NorwayFrostGolemsMeleeAmbushV9 = 2391751132260738236,
            NorwayFrostGolemsRangedAmbushV1 = 5625787139408602397,
            NorwayFrostGolemsRangedAmbushV2 = 15560123190288327966,
            NorwayFrostGolemsRangedAmbushV3 = 1620863043653018911,
            NorwayFrostGolemsRangedAmbushV4 = 4559159988866131232,
            NorwayFrostGolemsRangedAmbushV5 = 18359975128891467041,
            NorwayFrostGolemsRangedAmbushV6 = 7433712472581743906,
            NorwayFrostGolemsRangedAmbushV7 = 12008573372694471971,
            NorwayFrostGolemsRangedAmbushV8 = 3846057716786537764,
            NorwayFrostGolemsRangedAmbushV9 = 17640932745242813733,
            // Formations
            CH9HYDRALargeV1 = 2870567467016199194,
            CH9HYDRALargeV2 = 13705627841416535067,
            CH9HYDRALargeV3 = 9203937241110486044,
            CH9HYDRAMediumV1 = 3960261659038456976,
            CH9HYDRAMediumV2 = 12616776073852558481,
            CH9HYDRAMediumV3 = 7969424372122000530,
            CH9HYDRAMediumV4 = 5161688935150591123,
            CH9HYDRAMediumV5 = 9668348228504001684,
            CH9HYDRAMediumV6 = 2291973203088448661,
            // Siege
            Ch09ActivateSiegeDoorDefense = 17270497231078564226,
            OMSiegeDropshipAssault = 12090724917985880814,
            OMSiegeRescue = 3946739667481535280,

            PoliceVsShark = 9206170907141351562,
            CivTrappedUnderRhino = 12254878804928310140,
            NamedEliteLizardMonkey = 1618332889826339901, 
        }
    }
}
