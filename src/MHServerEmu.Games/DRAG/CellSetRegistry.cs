using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.DRAG
{
    public class CellSetRegistryEntry
    {
        public PrototypeId CellRef;
        public bool Picked;
        public bool Unique;
        public int Weight;

        public List<AreaTransition> AreaTransitions = new();

        public CellSetRegistryEntry() { }
    }
    public class AreaTransition
    {
        public Vector3 Position;
        public Orientation Rotation;
        public AreaTransitionPrototype Prototype;
    }

    public class EntryList : List<CellSetRegistryEntry>
    {
        public EntryList() { }
    }

    public class CellSetRegistry
    {
        public static readonly Logger Logger = LogManager.CreateLogger();
        public bool Log;
        public Aabb CellBounds { get; private set; }
        public bool IsInitialized { get; private set; }

        private bool _supressMissingCellErrors;
        private readonly EntryList _cells = new();
        private readonly Dictionary<Cell.Filler, EntryList> _cellsFiller = new();
        private readonly Dictionary<Cell.Type, EntryList> _cellsType = new();
        private readonly Dictionary<Cell.Walls, EntryList> _cellsWalls = new();
        private readonly Dictionary<Cell.Type, Vector3> _connectionsType = new();

        public CellSetRegistry()
        {
            IsInitialized = false;
            _supressMissingCellErrors = false;
            CellBounds = Aabb.Zero;
            for (int i = 0; i < 4; i++)
            {
                Cell.Type type = (Cell.Type)(1 << i);
                _connectionsType[type] = Vector3.Zero;
            }
        }

        public void Initialize(bool supressMissingCellErrors, bool log)
        {
            _supressMissingCellErrors = supressMissingCellErrors;
            Log = log;
            IsInitialized = true;
        }

        public PrototypeId GetCellSetAssetPicked(GRandom random, Cell.Type cellType, List<PrototypeId> excludedList)
        {
            if (_cellsType.TryGetValue(cellType, out EntryList entryList))
            {
                if (entryList == null || entryList.Count == 0) return 0;

                Picker<CellSetRegistryEntry> picker = new(random);
                if (PopulatePickerPhases(picker, entryList, excludedList))
                {
                    if (!picker.Empty() && picker.Pick(out CellSetRegistryEntry entry))
                    {
                        entry.Picked = true;
                        return entry.CellRef;
                    }
                }
                else
                    if (Log) Logger.Warn($"Warning: Generator tried to prevent choosing a type {cellType} cell that was similar to it's neighbors but failed doing so due to a lack of alternatives, consider making more variations of that type.");
            }
            return 0;
        }

        public PrototypeId GetCellSetAssetPickedByWall(GRandom random, Cell.Walls wallType, List<PrototypeId> excludedList = null)
        {
            if (_cellsWalls.TryGetValue(wallType, out EntryList entryList))
            {
                if (entryList == null) return 0;

                Picker<CellSetRegistryEntry> picker = new(random);
                if (PopulatePickerPhases(picker, entryList, excludedList))
                {
                    if (!picker.Empty() && picker.Pick(out CellSetRegistryEntry entry))
                    {
                        entry.Picked = true;
                        return entry.CellRef;
                    }
                }
                else
                    if (Log) Logger.Warn("Warning: Generator tried to prevent choosing a type UnknownWallType cell that was similar to its neighbors but failed doing so due to a lack of alternatives, consider making more variations of that type.");
            }
            return 0;
        }

        public PrototypeId GetCellSetAssetPickedByFiller(GRandom random, Cell.Filler fillerType)
        {
            if (_cellsFiller.TryGetValue(fillerType, out EntryList entryList))
            {
                if (entryList == null || entryList.Count == 0) return 0;

                Picker<CellSetRegistryEntry> picker = new(random);
                if (PopulatePickerPhases(picker, entryList, null))
                {
                    if (!picker.Empty() && picker.Pick(out CellSetRegistryEntry entry))
                    {
                        entry.Picked = true;
                        return entry.CellRef;
                    }
                }
            }
            return 0;
        }

        private static void PopulatePicker(Picker<CellSetRegistryEntry> picker, List<CellSetRegistryEntry> entryList, List<PrototypeId> skipList, bool skipPicked, bool skipUnique)
        {
            foreach (var entry in entryList)
            {
                bool skip = false;

                if (skipList != null && skipList.Any())
                {
                    foreach (var skipCell in skipList)
                    {
                        if (skipCell == entry.CellRef)
                        {
                            skip = true;
                            break;
                        }
                    }
                }

                if (!skip && !skipPicked && entry.Picked) skip = true;
                if (!skip && !skipUnique && entry.Unique && entry.Picked) skip = true;
                if (!skip) picker.Add(entry, entry.Weight);
            }
        }

        private bool PopulatePickerPhases(Picker<CellSetRegistryEntry> picker, List<CellSetRegistryEntry> entryList, List<PrototypeId> skipList)
        {
            PopulatePicker(picker, entryList, skipList, false, false);
            if (picker.Empty()) PopulatePicker(picker, entryList, skipList, true, false);
            if (picker.Empty()) PopulatePicker(picker, entryList, skipList, true, true);
            if (picker.Empty() && skipList != null) PopulatePicker(picker, entryList, null, true, true);
            return !picker.Empty();
        }

        public void LoadDirectory(AssetId cellSet, CellSetEntryPrototype cellSetEntry, int weight, bool unique)
        {
            if (weight <= 0) return;

            if (GatherCellSet(cellSet, cellSetEntry, out List<PrototypeId> cells))
            {
                foreach (var cellRef in cells)
                    AddReference(cellRef, weight, unique);
            }
        }

        private void AddReference(PrototypeId cellRef, int weight, bool unique)
        {
            if (cellRef == 0 || weight <= 0) return;

            CellPrototype cellProto = GameDatabase.GetPrototype<CellPrototype>(cellRef);
            if (cellProto == null) return;

            Cell.Type cellType = cellProto.Type;
            Cell.Walls cellWalls = cellProto.Walls;
            Cell.Filler fillerEdges = cellProto.FillerEdges;
            Aabb bounds = cellProto.BoundingBox;

            if (!Segment.EpsilonTest(bounds.Length, bounds.Width))
            {
                if (Log) Logger.Error($"Data:(.cell file) Grid Generation requires square cells.\n\tCell: {cellProto.ClientMap}\n\tLength:{bounds.Length}\n\tWidth:{bounds.Width}");
                return;
            }

            float playableArea = cellProto.NaviPatchSource.PlayableArea;
            bool filler = cellWalls == Cell.Walls.All && playableArea <= 0.0f;

            CellSetRegistryEntry entry = new()
            {
                CellRef = cellRef,
                Weight = weight,
                Unique = unique
            };

            _cells.Add(entry);

            if (!filler)
            {
                if (!HasCellOfType(cellType)) _cellsType[cellType] = new();
                _cellsType[cellType].Add(entry);
            }
            else
            {
                if (!HasCellOfFiller(fillerEdges)) _cellsFiller[fillerEdges] = new();
                _cellsFiller[fillerEdges].Add(entry);
            }

            if (!_cellsWalls.ContainsKey(cellWalls)) _cellsWalls[cellWalls] = new();
            _cellsWalls[cellWalls].Add(entry);

            if (cellProto.MarkerSet.Markers != null)
            {
                foreach (var marker in cellProto.MarkerSet.Markers)
                {
                    if (marker == null) continue;
                    if (marker is EntityMarkerPrototype entityMarker)
                    {
                        PrototypeGuid entityGuid = entityMarker.EntityGuid;
                        if (entityGuid != 0)
                        {
                            PrototypeId entityRef = GameDatabase.GetDataRefByPrototypeGuid(entityGuid);
                            if (entityRef != 0)
                            {
                                Prototype markedProto = GameDatabase.GetPrototype<Prototype>(entityRef); 

                                if (markedProto is AreaTransitionPrototype areaProto)
                                {
                                    AreaTransition areaTransition = new()
                                    {
                                        Position = entityMarker.Position,
                                        Rotation = entityMarker.Rotation,
                                        Prototype = areaProto
                                    };

                                    entry.AreaTransitions.Add(areaTransition);
                                }
                            }
                        }
                    }
                    else if (marker is CellConnectorMarkerPrototype)
                    {
                        Cell.Type type = Cell.Type.None;
                        if (Cell.DetermineType(ref type, marker.Position))
                        {
                            switch (type)
                            {
                                case Cell.Type.N:
                                case Cell.Type.E:
                                case Cell.Type.S:
                                case Cell.Type.W:
                                    AddNewConnectionMapEntry(type, marker.Position, cellRef);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
        }

        private bool AddNewConnectionMapEntry(Cell.Type type, Vector3 position, PrototypeId cellRef)
        {
            if (Vector3.Zero == _connectionsType[type] || Vector3.EpsilonSphereTest(position, _connectionsType[type], 64.0f))
            {
                _connectionsType[type] = position;
                return true;
            }
            else
                if (Log) Logger.Error($"CellSet contains more than one edge connection type {type}:\n  {position}\n  {_connectionsType[type]}\n  Adding: {GameDatabase.GetPrototypeName(cellRef)}");

            return false;
        }

        private bool GatherCellSet(AssetId cellSet, CellSetEntryPrototype cellSetEntry, out List<PrototypeId> cells)
        {
            cells = new();
            if (cellSet == 0) return false;

            string cellSetPath = GameDatabase.GetAssetName(cellSet);
            if (Log) Logger.Trace($"CellSetRegistry::LoadCellSet({cellSetPath})");
            cellSetPath = "Resource/Cells/" + cellSetPath;

            int numCells = 0;

            foreach (var cellRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<CellPrototype>())
            {                
                if (IsInCellSet(cellSetEntry, cellSetPath, cellRef))
                {
                    CellPrototype cell = cellRef.As<CellPrototype>();
                    //Logger.Debug($"0x{cell.DataRef:X16}i64 [{cell.DataRef}] {GameDatabase.GetPrototypeName(cell.DataRef)}");
                    cells.Add(cellRef);
                    CellBounds = cell.BoundingBox;
                    ++numCells;
                }
            }

            return true;
        }

        public static bool IsInCellSet(CellSetEntryPrototype cellSetEntry, string cellSetPath, PrototypeId cellRef)
        {         
            if (cellRef == 0) return false;

            string cellPath = GameDatabase.GetPrototypeName(cellRef);
            if (!cellPath.StartsWith(cellSetPath, StringComparison.OrdinalIgnoreCase) ||
                cellPath.Contains("entry", StringComparison.OrdinalIgnoreCase) ||
                cellPath.Contains("exit", StringComparison.OrdinalIgnoreCase) ||
                cellPath.Contains("trans", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            CellPrototype cell = GameDatabase.GetPrototype<CellPrototype>(cellRef);
            if (cell == null) return false;

            Cell.WallGroup walls = (Cell.WallGroup)cell.Walls;
            if (cellSetEntry != null && cellSetEntry.IgnoreOfType != null)
            {
                foreach (var entry in cellSetEntry.IgnoreOfType)
                    if (walls == entry.Ignore) return false;
            }

            return true;
        }

        public Vector3 GetConnectionListForType(Cell.Type type)
        {
            return _connectionsType[type];
        }

        public bool IsComplete()
        {
            for (int i = 1; i < 16; ++i)
            {
                Cell.Type type = (Cell.Type)i;
                if (!HasCellOfType(type) && !_supressMissingCellErrors)
                {
                    if (Log) Logger.Trace($"CellSetRegistry Missing {type}");
                }
            }
            return true;
        }

        public bool HasCellOfType(Cell.Type cellType)
        {
            return _cellsType.ContainsKey(cellType);
        }

        public bool HasCellOfFiller(Cell.Filler fillerEdges)
        {
            return _cellsFiller.ContainsKey(fillerEdges);
        }

        public bool HasCellWithWalls(Cell.Walls wallType)
        {
           return _cellsWalls.ContainsKey(wallType) && _cellsWalls[wallType] != null;
        }

    }
}