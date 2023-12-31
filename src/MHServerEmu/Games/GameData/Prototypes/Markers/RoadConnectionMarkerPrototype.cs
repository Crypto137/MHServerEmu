﻿using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.GameData.Prototypes.Markers
{
    public class RoadConnectionMarkerPrototype : MarkerPrototype
    {
        public Vector3 Extents { get; }

        public RoadConnectionMarkerPrototype(BinaryReader reader)
        {
            Extents = reader.ReadVector3();

            Position = reader.ReadVector3();
            Rotation = reader.ReadVector3();
        }
    }
}
