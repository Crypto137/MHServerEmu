using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Regions;
using System;
using System.Reflection;

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
        public const int GimbalResolution = 128;
        public const int SpawnMapResolution = 256;

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
            _boundsX = MathHelper.RoundToInt(aabb.Width) / SpawnMapResolution;
            _boundsY = MathHelper.RoundToInt(aabb.Length) / SpawnMapResolution;
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

            float spawnRadius = SpawnMapResolution / 2;
            var navi = area.Region.NaviMesh;
            var center = area.RegionBounds.Min + new Vector3(spawnRadius, spawnRadius, 0.0f);

            _spawnZone = 0;
            int index = 0;

            // add spawn zone
            for (int y = 0; y < _boundsY; y++)
                for (int x = 0; x < _boundsX; x++)
                {
                    var position = center + new Vector3(SpawnMapResolution * x, SpawnMapResolution * y, 0.0f);
                    if (navi.Contains(position, spawnRadius, new WalkPathFlagsCheck()))
                    {
                        _heatMap[index] = HeatData.Min;
                        _spawnZone++;
                    }
                    index++;
                }

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

            var random = Area.Game.Random;
            Picker = new SpawnMapPicker(random, this);
            DistributePicker = new SpawnMapPicker(random, this);

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

            int index;
            for (index = 0; index < _heatMap.Length; index++)
                _heatMap[index] &= ~HeatData.BlackOut;

            var manager = Area.Region.PopulationManager;
            var zones = manager.IterateBlackOutZoneInVolume(Area.RegionBounds).ToArray();

            float spawnRadius = SpawnMapResolution / 2;
            var center = Area.RegionBounds.Min + new Vector3(spawnRadius, spawnRadius, 0.0f);

            index = 0;
            for (int y = 0; y < _boundsY; y++)
                for (int x = 0; x < _boundsX; x++)
                {
                    var position = center + new Vector3(SpawnMapResolution * x, SpawnMapResolution * y, 0.0f);
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
                    index++;
                }

            _spawnZone = 0;
            for (index = 0; index < _heatMap.Length; index++)
                if (HasFlags(_heatMap[index]) == false)
                    _spawnZone++;

            int newHeatMax = _heatBase * _spawnZone;
            if (oldHeatMax == newHeatMax) return;

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

            position.X += coord.X * SpawnMapResolution;
            position.Y += coord.Y * SpawnMapResolution;

            return aabb.IntersectsXY(position);
        }

        public bool ProjectAreaPosition(Vector3 position, out Point2 coord, bool checkBounds = true)
        {
            var aabb = Area.RegionBounds;
            coord = new();
            if (checkBounds && aabb.IntersectsXY(position) == false) return false;

            var pos = position - aabb.Min;

            int posX = MathHelper.RoundToInt(pos.X) / SpawnMapResolution;
            if (checkBounds && (posX < 0 || posX > _boundsX)) return false;

            int posY = MathHelper.RoundToInt(pos.Y) / SpawnMapResolution;
            if (checkBounds && (posY < 0 || posY > _boundsY)) return false;

            coord = new(posX, posY);
            return true;
        }

        public static bool ProjectGimbalPosition(Aabb aabb, Vector3 position, out Point2 coord)
        {
            coord = new();
            if (aabb.IntersectsXY(position) == false) return false;

            var pos = position - aabb.Min;

            int boundsX = MathHelper.RoundToInt(aabb.Width) / GimbalResolution;
            int posX = MathHelper.RoundToInt(pos.X) / GimbalResolution;
            if (posX < 0 || posX > boundsX) return false;

            int boundsY = MathHelper.RoundToInt(aabb.Length) / GimbalResolution;
            int posY = MathHelper.RoundToInt(pos.Y) / GimbalResolution;
            if (posY < 0 || posY > boundsY) return false;

            coord = new(posX, posY);
            return true;
        }

        public static Aabb HorizonVolume(Vector3 position, int spawnMapHorizon)
        {
            float horizon = spawnMapHorizon * SpawnMapResolution;
            Vector3 min = new(position.X - horizon, position.Y - horizon, -1024.0f);
            Vector3 max = new(position.X + horizon, position.Y + horizon, 1024.0f);
            return new(min, max);
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
            if (_pool == 0) return;

            int heatReturnPerSecond = CalcHeatReturnPerSecond();
            int heatReturn = PoolTickSec * heatReturnPerSecond;
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
            return Math.Clamp(heatReturn, HeatPerSecondMin, HeatPerSecondMax);
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
        public Square Gimbal;

        public SpawnGimbal(int radius)
        {
            Coord = new Point2(0, 0);
            Gimbal = new Square(new(-radius, -radius), new(radius, radius));
        }

        public void UpdateGimbal(Point2 coord)
        {
            Coord = coord;

            int x = 0;
            if (Gimbal.Max.X < coord.X) x = coord.X - Gimbal.Max.X;
            else if (Gimbal.Min.X > coord.X) x = coord.X - Gimbal.Min.X;

            Gimbal.Min.X += x;
            Gimbal.Max.X += x;

            int y = 0;
            if (Gimbal.Max.Y < coord.Y) y = coord.Y - Gimbal.Max.Y;
            else if (Gimbal.Min.Y > coord.Y) y = coord.Y - Gimbal.Min.Y;

            Gimbal.Min.Y += y;
            Gimbal.Max.Y += y;
        }

        public bool InGimbal(Point2 coord)
        {
            return Gimbal.Min.X <= coord.X && Gimbal.Max.X >= coord.X 
                && Gimbal.Min.Y <= coord.Y && Gimbal.Max.Y >= coord.Y;
        }
    }
}
