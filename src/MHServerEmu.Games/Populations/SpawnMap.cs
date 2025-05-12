using Gazillion;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Populations
{
    public enum HeatData : byte
    {
        Min = 0,
        Max = 63,
        ValueMask = Max,

        BlackOut = 64,
        Empty = 128,
        FlagMask = BlackOut | Empty
    }

    public class SpawnHeat
    {
        public SpawnMap SpawnMap;
        public int Heat;

        public SpawnHeat(SpawnMap spawnMap, int heat)
        {
            SpawnMap = spawnMap;
            Heat = heat;
        }

        public void Return()
        {
            if (Heat == 0) return;
            
            SpawnMap.PoolHeat(Heat);
            Heat = 0;
        }
    }

    public class SpawnMap
    {
        public const int Resolution = 256;

        public Area Area;

        public EventScheduler GameEventScheduler { get => Area?.Game?.GameEventScheduler; }
        private readonly EventPointer<PoolUpdateEvent> _poolUpdateEvent = new();
        private readonly EventPointer<LevelUpdateEvent> _levelUpdateEvent = new();
        private EventGroup _pendingEvents = new();

        private int _boundsX;
        private int _boundsY;
        private HeatData[] _heatMap;

        private int _spawnZone;
        private float _heatDencity;
        private int _heatBase;
        private int _heatMax;
        private int _pool;
        private int _poolIndex;

        public float DensityMin { get; }
        public float DensityMax { get; }
        public float DensityStep { get; }   
        public float CrowdSupression { get; }
        public int CrowdSupressionStart { get; }
        public bool SupressSpawnOnPlayer { get; }
        public float CrowdSupressionRadius { get; }

        public int HeatReturnPerSecond { get; }
        public EvalPrototype HeatReturnPerSecondEval { get; }
        public int HeatPerSecondMin { get; }
        public int HeatPerSecondMax { get; }
        public int HeatPerSecondScalar { get; }

        public TimeSpan LevelTick { get; }
        public TimeSpan PoolTick { get; }
        public int PoolTickSec { get; }
        public float HeatBleed { get; }
        public int DistributeDistance { get; }
        public int DistributeSpread { get; }
        public int Horizon { get; }
        public float MaxChance { get; }

        public SpawnMapPicker Picker { get; }
        public SpawnMapPicker DistributePicker { get; }

        public SpawnMap(Area area, PopulationPrototype populationProto)
        {
            Area = area;

            var aabb = area.LocalBounds;
            _boundsX = MathHelper.RoundToInt(aabb.Width) / Resolution;
            _boundsY = MathHelper.RoundToInt(aabb.Length) / Resolution;
            if (_boundsX == 0 || _boundsY == 0) return;
            int size = _boundsX * _boundsY;

            // save prototype constants
            HeatBleed = populationProto.SpawnMapHeatBleed;
            DistributeDistance = populationProto.SpawnMapDistributeDistance;
            DistributeSpread = populationProto.SpawnMapDistributeSpread;

            DensityMin = populationProto.SpawnMapDensityMin;
            DensityMax = populationProto.SpawnMapDensityMax;
            DensityStep = populationProto.SpawnMapDensityStep;

            CrowdSupression = populationProto.SpawnMapCrowdSupression;
            CrowdSupressionStart = populationProto.SpawnMapCrowdSupressionStart;

            HeatReturnPerSecond = populationProto.SpawnMapHeatReturnPerSecond;
            HeatReturnPerSecondEval = populationProto.SpawnMapHeatReturnPerSecondEval;

            var globalsProto = GameDatabase.GlobalsPrototype.PopulationGlobalsPrototype;
            if (globalsProto == null) return;

            Horizon = globalsProto.SpawnMapHorizon;
            MaxChance = globalsProto.SpawnMapMaxChance;

            HeatPerSecondMin = globalsProto.SpawnMapHeatPerSecondMin;
            HeatPerSecondMax = globalsProto.SpawnMapHeatPerSecondMax;
            HeatPerSecondScalar = globalsProto.SpawnMapHeatPerSecondScalar;

            LevelTick = TimeSpan.FromMilliseconds(globalsProto.SpawnMapLevelTickMS);
            PoolTick = TimeSpan.FromMilliseconds(globalsProto.SpawnMapPoolTickMS);
            PoolTickSec = globalsProto.SpawnMapPoolTickMS / 1000;

            SupressSpawnOnPlayer = globalsProto.SupressSpawnOnPlayer;
            CrowdSupressionRadius = globalsProto.CrowdSupressionRadius;

            // build heat map
            _heatMap = new HeatData[size];
            _heatMap.AsSpan().Fill(HeatData.Empty);

            float spawnRadius = Resolution / 2.0f;
            var navi = area.Region.NaviMesh;
            var center = area.RegionBounds.Min + new Vector3(spawnRadius, spawnRadius, 0.0f);

            _spawnZone = 0;
            int index = 0;

            // add spawn zone
            for (int y = 0; y < _boundsY; y++)
                for (int x = 0; x < _boundsX; x++)
                {
                    var position = center + new Vector3(Resolution * x, Resolution * y, 0.0f);
                    if (navi.Contains(position, spawnRadius, new WalkPathFlagsCheck()))
                    {
                        _heatMap[index] = HeatData.Min;
                        _spawnZone++;
                    }
                    index++;
                }

            var random = Area.Game.Random;
            Picker = new SpawnMapPicker(random, this);
            DistributePicker = new SpawnMapPicker(random, this);

            // calc Density and Heat
            float dencity = CalcDencity();
            if (dencity == 0.0f) return;
            _heatDencity = dencity;

            _heatBase = CalcHeat(dencity);
            _heatMax = _heatBase * _spawnZone;

            // add Heat in heatMap
            _pool = 0;

            for (index = 0; index < _heatMap.Length; index++)
            {
                var heatData = _heatMap[index];
                if (HasFlags(heatData)) continue;

                int heat = GetHeat(heatData);
                if (heat + _heatBase < (int)HeatData.Max)
                    _heatMap[index] = (HeatData)(heat + _heatBase);
                else
                    _pool += _heatBase;
            }

            ScheduleLevelEvent();
        }

        public void Destroy()
        {
            _heatMap = null;
            GameEventScheduler?.CancelAllEvents(_pendingEvents);
        }

        public void BlackOutZonesRebuild()
        {
            int oldHeatMax = _heatMax;

            var manager = Area.Region.PopulationManager;
            var zones = manager.IterateBlackOutZoneInVolume(Area.RegionBounds).ToArray();

            float spawnRadius = Resolution / 2.0f;
            var center = Area.RegionBounds.Min + new Vector3(spawnRadius, spawnRadius, 0.0f);

            int index = 0;
            int spawnZone = 0;
            for (int y = 0; y < _boundsY; y++)
                for (int x = 0; x < _boundsX; x++)
                {
                    _heatMap[index] &= ~HeatData.BlackOut;

                    var position = center + new Vector3(Resolution * x, Resolution * y, 0.0f);
                    foreach (var zone in zones)
                    {
                        float radiusSq = MathHelper.Square(zone.Sphere.Radius);
                        float distanceSq = Vector3.DistanceSquared2D(position, zone.Sphere.Center);
                        if (distanceSq < radiusSq)
                        {
                            int heat = GetHeat(_heatMap[index]);
                            _heatMap[index] = (_heatMap[index] & HeatData.FlagMask) | HeatData.BlackOut;
                            PoolHeat(heat);
                        }
                    }

                    if (HasFlags(_heatMap[index]) == false) spawnZone++;
                    index++;
                }

            int newHeatMax = _heatBase * _spawnZone;
            if (oldHeatMax == newHeatMax) return;

            _spawnZone = spawnZone;
            _heatMax = newHeatMax;

            PoolHeat(newHeatMax - oldHeatMax);
        }

        public void UpdateMap()
        {
            int oldHeatBase = _heatBase;
            int oldHeatMax = _heatMax;

            _heatDencity = CalcDencity();
            int newHeatBase = CalcHeat(_heatDencity);
            int newHeatMax = newHeatBase * _spawnZone;

            if (oldHeatBase == newHeatBase) return;

            _heatBase = newHeatBase;
            _heatMax = newHeatMax;

            PoolHeat(newHeatMax - oldHeatMax);
        }

        public bool GetHeatData(int x, int y, out int index, out HeatData heatData)
        {
            index = x + y * _boundsX;
            heatData = HeatData.Empty;

            if (x >= 0 && x < _boundsX && y >= 0 && y < _boundsY)
            {
                heatData = _heatMap[index];
                return true;
            }
            return false;
        }

        public HeatData GetHeatData(int index)
        {
            return _heatMap[index];
        }

        public static int GetHeat(HeatData heatData)
        {
            return (int)(heatData & HeatData.ValueMask);
        }

        public static bool HasFlags(HeatData heatData)
        {
            return (heatData & HeatData.FlagMask) != 0;
        }

        private static int CalcHeat(float heatDencity, int maxHeat = (int)HeatData.Max)
        {
            return Math.Min((int)((maxHeat + 1) * heatDencity), maxHeat);
        }

        private float CalcDencity()
        {
            float dencity;

            int playerCount = Area.PopulationArea.PlayerCount;
            int playerLimit = Area.Region.Prototype.PlayerLimit;

            if (DensityMin == DensityMax)
            {
                dencity = DensityMin;
            }
            else if (DensityStep > 0)
            {
                if (DensityMin < DensityMax)
                    dencity = Math.Clamp(DensityMin + DensityStep * playerCount, DensityMin, DensityMax);
                else
                    dencity = Math.Clamp(DensityMin - DensityStep * playerCount, DensityMax, DensityMin);
            }
            else
            {
                dencity = Segment.Lerp(DensityMin, DensityMax, playerCount / (float)playerLimit);
            }

            return dencity;
        }

        public float CalcCrowdSupression(Vector3 position)
        {
            float crowd = 0.0f;
            if (SupressSpawnOnPlayer == false && CrowdSupression <= 0.0f) return crowd;

            var region = Area.Region;
            List<WorldEntity> entities = new(256);
            var volume = new Sphere(position, CrowdSupressionRadius);
            region.GetEntitiesInVolume(entities, volume, new(EntityRegionSPContextFlags.ActivePartition));

            int crowdSize = 0;
            bool hasPlayer = false;

            foreach (var entity in entities)
            {
                if (entity is Avatar avatar) hasPlayer = true;
                if (entity.IsPopulation) crowdSize++;
            }

            if (hasPlayer && SupressSpawnOnPlayer) return 1.0f;

            if (crowdSize > 0 && CrowdSupression > 0.0f) 
            {
                crowd = (crowdSize - CrowdSupressionStart) * CrowdSupression;
                crowd = Math.Clamp(crowd, 0.0f, 1.0f);
            }

            return crowd;
        }

        public bool ProjectToCoord(int index, out Point2 coord)
        {
            coord = new();
            if (index < 0 || index >= _heatMap.Length) return false;

            coord = new(index % _boundsX, index / _boundsX);
            return true;
        }

        public bool ProjectToPosition(int index, out Vector3 position)
        {
            var aabb = Area.RegionBounds;

            position = Vector3.Zero;
            if (ProjectToCoord(index, out Point2 coord) == false) return false;

            position = aabb.Min;

            position.X += coord.X * Resolution;
            position.Y += coord.Y * Resolution;
            position.Z = 0f;

            return aabb.IntersectsXY(position);
        }

        public bool ProjectAreaPosition(Vector3 position, out Point2 coord)
        {
            var aabb = Area.RegionBounds;
            var pos = position - aabb.Min;

            int posX = MathHelper.RoundToInt(pos.X) / Resolution;
            int posY = MathHelper.RoundToInt(pos.Y) / Resolution;

            coord = new(posX, posY);
            return true;
        }

        public void DistributeHeat(int index, Point2 start)
        {
            if (ProjectToCoord(index, out Point2 coord) == false) return;

            var picker = DistributePicker;
            if (start == coord) return;
            picker.AddSpread(start, coord, DistributeSpread, DistributeDistance, true);
            PickSpreadHeat(index, picker);
        }

        public int PickBleedHeat(int index)
        {
            int heat = GetHeat(_heatMap[index]);
            _heatMap[index] = HeatData.Min;

            if (HeatBleed > 0.0f && ProjectToCoord(index, out Point2 coord))
            {
                var picker = Picker;
                picker.AddHorizon(coord, 1, false);
                while (picker.Pick(out int pickIndex))
                {
                    if (pickIndex < 0) continue;

                    int pickHeat = GetHeat(_heatMap[pickIndex]);
                    if (pickHeat == 0) continue;

                    int heatBleed = CalcHeat(HeatBleed, pickHeat);
                    _heatMap[pickIndex] = (HeatData)(pickHeat - heatBleed);
                    _pool += heatBleed;

                }

                SchedulePoolEvent();
            }

            return heat;
        }

        private void PickSpreadHeat(int index, SpawnMapPicker picker)
        {
            int heat = GetHeat(_heatMap[index]);
            _heatMap[index] = HeatData.Min;

            int count = picker.Count;
            if (count == 0)
            {
                PoolHeat(heat);
                return;
            }

            int numHeat = heat / count;
            if (numHeat > 0)
            {
                while (picker.Pick(out int pickIndex))
                {
                    if (pickIndex < 0) continue;

                    int pickHeat = GetHeat(_heatMap[pickIndex]) + numHeat;
                    if (pickHeat > (int)HeatData.Max)
                    {
                        PoolHeat(pickHeat - (int)HeatData.Max);
                        _heatMap[pickIndex] = HeatData.Max;
                    }
                    else
                    {
                        _heatMap[pickIndex] = (HeatData)pickHeat;
                    }
                }
            }

            PoolHeat(heat % count);
        }

        public void PoolHeat(int heat)
        {
            _pool += heat;
            SchedulePoolEvent();
        }

        private void SchedulePoolEvent()
        {
            if (_pool == 0) return;
            var scheduler = GameEventScheduler;
            if (scheduler == null || _poolUpdateEvent.IsValid) return;
            scheduler.ScheduleEvent(_poolUpdateEvent, PoolTick, _pendingEvents);
            _poolUpdateEvent.Get().Initialize(this);
        }

        private void ScheduleLevelEvent()
        {
            var scheduler = GameEventScheduler;
            if (scheduler == null || _levelUpdateEvent.IsValid) return;
            scheduler.ScheduleEvent(_levelUpdateEvent, LevelTick, _pendingEvents);
            _levelUpdateEvent.Get().Initialize(this);
        }

        private void OnPoolUpdate()
        {
            if (_pool == 0 || _heatMap == null) return;

            int heatReturn = PoolTickSec * CalcHeatReturnPerSecond();
            if (heatReturn > 0)
            {
                heatReturn = Math.Min(_pool, heatReturn);
                _pool -= heatReturn;

                int size = _heatMap.Length;
                int maxCycle = size * 5; // attempts to return heat

                int index = _poolIndex;
                int cycle = 0;

                // distribute heat evenly several times and return remainder in pool
                while (heatReturn > 0 && cycle++ < maxCycle)
                {
                    var heatData = _heatMap[index];
                    if (HasFlags(heatData) == false)
                    {
                        int heat = GetHeat(heatData);
                        if (heat < _heatBase && heat != (int)HeatData.Max)
                        {
                            _heatMap[index] = (HeatData)(heat + 1) & HeatData.ValueMask;
                            heatReturn--;
                        }
                    }
                    // next index
                    index = (index + 1) % size;
                }

                _poolIndex = index;

                if (heatReturn > 0) _pool += heatReturn;
            }

            SchedulePoolEvent();
        }

        private int CalcHeatReturnPerSecond()
        {
            int heatReturn = 0;

            if (HeatReturnPerSecondEval != null)
            {
                int playerCount = Area.PopulationArea.PlayerCount;
                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                evalContext.SetVar_Int(EvalContext.Var1, playerCount);
                heatReturn = Eval.RunInt(HeatReturnPerSecondEval, evalContext);
            }
            else
            {
                heatReturn = HeatReturnPerSecond;
            }

            // HeatPerSecondScalar = 0
            heatReturn = MathHelper.ClampNoThrow(heatReturn, HeatPerSecondMin, HeatPerSecondMax);

            // LiveTuning AreaMobSpawnHeatReturn
            float liveReturn = heatReturn * LiveTuningManager.GetLiveAreaTuningVar(Area.Prototype, AreaTuningVar.eATV_AreaMobSpawnHeatReturn);
            heatReturn = MathHelper.ClampNoThrow((int)liveReturn, 0, HeatPerSecondMax);

            return heatReturn;
        }

        private void OnLevelUpdate()
        {
            int level = 0;

            for (int index = 0; index < _heatMap.Length; index++)
            {
                var heatData = _heatMap[index];
                if (HasFlags(heatData)) continue;

                int heat = GetHeat(heatData);
                if (heat > _heatBase)
                {
                    _heatMap[index] = (HeatData)(heat - 1) & HeatData.ValueMask;
                    level++;
                }
            }

            _pool += level;

            ScheduleLevelEvent();
        }

        protected class PoolUpdateEvent : CallMethodEvent<SpawnMap>
        {
            protected override CallbackDelegate GetCallback() => (spawnMap) => spawnMap.OnPoolUpdate();
        }

        protected class LevelUpdateEvent : CallMethodEvent<SpawnMap>
        {
            protected override CallbackDelegate GetCallback() => (spawnMap) => spawnMap.OnLevelUpdate();
        }
    }

    public class SpawnGimbal
    {
        public Point2 Coord;
        public Point2 Min;
        public Point2 Max;
        public float Horizon;

        public SpawnGimbal(int radius, int horizon)
        {
            Coord = new(0, 0);
            Min = new(-radius, -radius);
            Max = new(radius, radius);
            Horizon = horizon * SpawnMap.Resolution;
        }

        public Aabb HorizonVolume(Vector3 position)
        {
            Vector3 min = new(position.X - Horizon, position.Y - Horizon, -1024.0f);
            Vector3 max = new(position.X + Horizon, position.Y + Horizon, 1024.0f);
            return new(min, max);
        }

        public void UpdateGimbal(Point2 coord)
        {
            Coord = coord;

            int x = 0;
            if (Max.X < coord.X) x = coord.X - Max.X;
            else if (Min.X > coord.X) x = coord.X - Min.X;

            Min.X += x;
            Max.X += x;

            int y = 0;
            if (Max.Y < coord.Y) y = coord.Y - Max.Y;
            else if (Min.Y > coord.Y) y = coord.Y - Min.Y;

            Min.Y += y;
            Max.Y += y;
        }

        public bool InGimbal(Point2 coord)
        {
            return Min.X <= coord.X && Max.X >= coord.X 
                && Min.Y <= coord.Y && Max.Y >= coord.Y;
        }

        public bool ProjectGimbalPosition(Aabb aabb, Vector3 position, out Point2 coord)
        {
            coord = new();
            if (aabb.IntersectsXY(position) == false) return false;

            var pos = position - aabb.Min;

            int boundsX = MathHelper.RoundToInt(aabb.Width) / SpawnMap.Resolution;
            int posX = MathHelper.RoundToInt(pos.X) / SpawnMap.Resolution;
            if (posX < 0 || posX > boundsX) return false;

            int boundsY = MathHelper.RoundToInt(aabb.Length) / SpawnMap.Resolution;
            int posY = MathHelper.RoundToInt(pos.Y) / SpawnMap.Resolution;
            if (posY < 0 || posY > boundsY) return false;

            coord = new(posX, posY);
            return true;
        }
    }
}
