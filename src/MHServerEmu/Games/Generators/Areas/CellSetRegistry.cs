using MHServerEmu.Common;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Generators.Prototypes;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.GameData.Prototypes.Markers;

namespace MHServerEmu.Games.Generators.Areas
{
    public class CellSetRegistryEntry
    {
        public ulong CellRef;
        public bool Picked;
        public bool Unique;
        public int Weight;
        
        public List<AreaTransition> AreaTransitions = new();

        public CellSetRegistryEntry(){}
    }
    public class EntryList : List<CellSetRegistryEntry>
    {
        public EntryList() { }
    }
    public class CellSetRegistry
    {
        public static readonly Logger Logger = LogManager.CreateLogger();
        public Aabb CellBounds;

        private EntryList _cells = new();
        private Dictionary<Cell.Filler, EntryList> _cellsFiller = new();
        private Dictionary<Cell.Type, EntryList> _cellsType = new();
        private Dictionary<Cell.Walls, EntryList> _cellsWalls = new();
        private Dictionary<Cell.Type, Vector3> _connectionsType = new();

        public bool IsInitialized { get; internal set; }

        public ulong GetCellSetAssetPicked(GRandom random, Cell.Type cellType, List<ulong> skipList)
        {
            EntryList entryList = _cellsType[cellType];

            if (entryList == null || entryList.Count == 0) return 0;

            Picker<CellSetRegistryEntry> picker = new (random);
            bool picked = PopulatePickerPhases(picker, entryList, skipList);
            
            if (picked)
            {
                if (!picker.Empty() && picker.Pick(out CellSetRegistryEntry entry))
                {
                    entry.Picked = true;
                    return entry.CellRef;
                }
            }
            else
                Logger.Warn($"Warning: Generator tried to prevent choosing a type {cellType} cell that was similar to it's neighbors but failed doing so due to a lack of alternatives, consider making more variations of that type.");

            return 0;
        }

        private void PopulatePicker(Picker<CellSetRegistryEntry> picker, List<CellSetRegistryEntry> entryList, List<ulong> skipList, bool skipPicked, bool skipUnique)
        {
            foreach (var entry in entryList)
            {
                bool skip = false;

                if (skipList != null && skipList.Count > 0)
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

        private bool PopulatePickerPhases(Picker<CellSetRegistryEntry> picker, List<CellSetRegistryEntry> entryList, List<ulong> skipList)
        {
            PopulatePicker(picker, entryList, skipList, false, false);
            if (picker.Empty()) PopulatePicker(picker, entryList, skipList, true, false);
            if (picker.Empty()) PopulatePicker(picker, entryList, skipList, true, true);
            if (picker.Empty() && skipList != null) PopulatePicker(picker, entryList, null, true, true);
            return !picker.Empty();
        }

        public void LoadDirectory(ulong cellSet, CellSetEntryPrototype cellSetEntry, int weight, bool unique)
        {
            if (weight <= 0) return;

            if (GatherCellSet(cellSet, cellSetEntry, out List<ulong> cells, out CellBounds))
            {
                foreach (var cellRef in cells)
                    AddReference(cellRef, weight, unique);
            }
        }

        private void AddReference(ulong cellRef, int weight, bool unique)
        {
            if (cellRef == 0 || weight <= 0)  return;

            CellPrototype cellProto = GameDatabase.GetPrototype<CellPrototype>(cellRef);
            if (cellProto == null) return;

            Cell.Type cellType = cellProto.Type;
            Cell.Walls cellWalls = cellProto.Walls;
            Cell.Filler fillerEdges = cellProto.FillerEdges;
            Aabb bounds = cellProto.BoundingBox;

            if (!Segment.EpsilonTest(bounds.Length, bounds.Width)) {
                Logger.Error($"Data:(.cell file) Grid Generation requires square cells.\n\tCell: {cellProto.ClientMap}\n\tLength:{bounds.Length}\n\tWidth:{bounds.Width}");
                return;
            }

            float playableArea = cellProto.NaviPatchSource.PlayableArea;
            bool filler = (cellWalls == Cell.Walls.All) && (playableArea <= 0.0f);

            CellSetRegistryEntry entry = new()
            {
                CellRef = cellRef,
                Weight = weight,
                Unique = unique
            };

            _cells.Add(entry);

            if (!filler)
            {
                if (_cellsType[cellType] == null) _cellsType[cellType] = new();
                _cellsType[cellType].Add(entry);
            }
            else
            {
                if (_cellsFiller[fillerEdges] == null) _cellsFiller[fillerEdges] = new();
                _cellsFiller[fillerEdges].Add(entry);
            }

            if (_cellsWalls[cellWalls] == null) _cellsWalls[cellWalls] = new();
            _cellsWalls[cellWalls].Add(entry);

            if (cellProto.MarkerSet.Markers != null)
            {
                foreach (var marker in cellProto.MarkerSet.Markers)
                {
                    if (marker == null) continue;               

                    if (marker is EntityMarkerPrototype entityMarker)
                    {
                        ulong entityGuid = entityMarker.EntityGuid;
                        if (entityGuid != 0)
                        {
                            ulong entityRef = GameDatabase.GetDataRefByPrototypeGuid(entityGuid);
                            if (entityRef != 0)
                            {
                                Prototype markedProto = GameDatabase.GetPrototype<Prototype>(entityRef); // TODO: Load Prototypes

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

        private bool AddNewConnectionMapEntry(Cell.Type type, Vector3 position, ulong cellRef)
        {
            if (Vector3.Zero == _connectionsType[type] || Vector3.EpsilonSphereTest(position, _connectionsType[type], 64.0f))
            {
                _connectionsType[type] = position;
                return true;
            } else
                Logger.Error( $"CellSet contains more than one edge connection type {type}:\n  {position}\n  {_connectionsType[type]}\n  Adding: {GameDatabase.GetPrototypeName(cellRef)}");

            return false;
        }

        private static bool GatherCellSet(ulong cellSet, CellSetEntryPrototype cellSetEntry, out List<ulong> cells, out Aabb cellBox)
        {
            cells = new ();
            cellBox = new(Aabb.InvertedLimit);
            
            if (cellSet == 0) return false;

            string cellSetPath = GameDatabase.GetAssetName(cellSet);
            Logger.Trace($"CellSetRegistry::LoadCellSet({cellSetPath})");

            List<CellPrototype> cellPrototypes = new ();
            cellPrototypes = GameDatabase.GetCellPrototypesByPath(cellSetPath);

            int numCells = 0;
            foreach (CellPrototype cell in cellPrototypes)
            {
                if (IsInCellSet(cellSetEntry, cellSetPath, cell))
                {
                    cells.Add(cell.DataRef);
                    cellBox = cell.BoundingBox;
                    ++numCells;
                }
            }

            return true;
        }

        public static bool IsInCellSet(CellSetEntryPrototype cellSetEntry, string cellSetPath, CellPrototype cell)
        {
            if (cell == null) return false;

            ulong cellRef = cell.DataRef;
            if (cellRef == 0) return false;

            string cellPath = GameDatabase.GetPrototypeName(cellRef);
            if (!cellPath.StartsWith(cellSetPath, StringComparison.OrdinalIgnoreCase) ||
                cellPath.Contains("entry", StringComparison.OrdinalIgnoreCase) ||
                cellPath.Contains("exit", StringComparison.OrdinalIgnoreCase) ||
                cellPath.Contains("trans", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            Cell.WallGroup walls = (Cell.WallGroup)cell.Walls;
            if (cellSetEntry != null && cellSetEntry.IgnoreOfType != null)
            {
                foreach (var entry in cellSetEntry.IgnoreOfType)
                    if (walls == entry.Ignore)  return false;
            }

            return true;
        }

        internal Vector3 GetConnectionListForType(Cell.Type type)
        {
            throw new NotImplementedException();
        }

        internal bool IsComplete()
        {
            throw new NotImplementedException();
        }

        internal void Initialize(bool supressMissingCellErrors)
        {
            throw new NotImplementedException();
        }
    }
}