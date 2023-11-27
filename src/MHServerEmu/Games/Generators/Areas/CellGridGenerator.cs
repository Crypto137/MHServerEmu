using MHServerEmu.Common;
using MHServerEmu.Games.Generators.Prototypes;
using MHServerEmu.Games.Generators.Regions;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Generators.Areas
{
    public class CellGridGenerator : BaseGridAreaGenerator
    {
        private enum ProcessEnum
        {
            Initialize,
            Generate
        }

        public override bool InitializeContainer()
        {
            if (!base.InitializeContainer()) return false;

            if (Area.AreaPrototype.Generator is not GridAreaGeneratorPrototype proto) return false;

            if (proto.Behaviors != null)
            {
                RunBehaviors(null, proto.Behaviors, ProcessEnum.Initialize);
            }

            return true;
        }

        private void RunBehaviors(GRandom random, CellGridBehaviorPrototype[] behaviors, ProcessEnum process)
        {
            throw new NotImplementedException();
        }

        public override bool Initialize(Area area)
        {
            CellContainer = new();

            return base.Initialize(area);
        }

        public override bool Generate(GRandom random, RegionGenerator regionGenerator, List<ulong> areas)
        {
            if (CellContainer == null) return false;

            if (Area.AreaPrototype.Generator is not GridAreaGeneratorPrototype proto) return false;

            bool success = false;
            int tries = 10;

            while (!success && (--tries > 0))
            {
                success = InitializeContainer()
                    && EstablishExternalConnections()
                    && GenerateRandomInstanceLinks(random)
                    && CreateRequiredCells(random, regionGenerator, areas);
            }

            if (!success)
            {
                Logger.Trace($"GridAreaGenerator failed after {10 - tries} attempts\nregion: {Region}\narea: {Area}");
                return false;
            }

            RunBehaviors(random, proto.Behaviors, ProcessEnum.Generate);

            ProcessDeleteExtraneousCells(random, (int)proto.RoomKillChancePct);
            ProcessDeleteExtraneousConnections(random, (int)proto.ConnectionKillChancePct);
            ProcessRegionConnectionsAndDepth();
            ProcessAssignUniqueCellIds();
            ProcessCellPositions(proto.CellSize);

            return ProcessCellTypes(random);
        }

        private bool ProcessCellTypes(GRandom random)
        {
            throw new NotImplementedException();
        }   

        private void ProcessDeleteExtraneousConnections(GRandom random, int connectionKillChancePct)
        {
            throw new NotImplementedException();
        }

        internal static void CellGridBorderBehavior(Area area)
        {
            throw new NotImplementedException();
        }
    }
}
