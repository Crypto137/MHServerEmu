# Auxiliary Resource Prototypes

[Resources](./Resources.md) make use of a number of auxiliary prototypes.

## Markers

Markers are prototypes that indicate position and rotation of various objects. 

They are stored in sets with the following structure:

```csharp
struct MarkerSetPrototype
{
    int NumMarkers;
    MarkerPrototype[NumMarkers] Markers;
}
```

All markers start with a [djb2 hash](https://theartincode.stanis.me/008-djb2/) of the prototype name that defines the further structure. There's a total of six marker types used by resources: `CellConnectorMarkerPrototype`, `DotCornerMarkerPrototype`, `EntityMarkerPrototype`, `ResourceMarkerPrototype`, `RoadConnectionMarkerPrototype`, and `UnrealPropMarkerPrototype`.

Cell connectors, dot corner, and road connection markers share the following structure:

```csharp
// Same as DotCornerMarkerPrototype and RoadConnectionMarkerPrototype
struct CellConnectorMarkerPrototype 
{
    uint ProtoNameHash;
    Vector3 Extents;
    Vector3 Position;
    Vector3 Rotation;
}
```

Entity markers have the following structure:

```csharp
struct EntityMarkerPrototype
{
    uint ProtoNameHash;
    ulong EntityGuid;    // PrototypeGuid
    FixedString32 LastKnownEntityName;
    ulong Modifier1Guid; // PrototypeGuid
    // eFlagDontCook FixedString32 Modifier1Text
    ulong Modifier2Guid; // PrototypeGuid
    // eFlagDontCook FixedString32 Modifier2Text
    ulong Modifier3Guid; // PrototypeGuid
    // eFlagDontCook FixedString32 Modifier3Text
    uint EncounterSpawnPhase;
    byte OverrideSnapToFloor;
    byte OverrideSnapToFloorValue;
    ulong FilterGuid;    // PrototypeGuid
    FixedString32 LastKnownFilterName;
    Vector3 Position;
    Vector3 Rotation;
}
```

Resource markers have the following structure:

```csharp
struct ResourceMarkerPrototype
{
    uint ProtoNameHash;
    FixedString32 Resource;
    Vector3 Position;
    Vector3 Rotation;
}
```

Unreal prop markers have the following structure:

```csharp
struct UnrealPropMarkerPrototype
{
    uint ProtoNameHash;
    FixedString32 UnrealClassName;
    FixedString32 UnrealQualifiedName;
    FixedString32 UnrealArchetypeName;
    Vector3 Position;
    Vector3 Rotation;
}
```

## NaviPatch

NaviPatch prototypes are used in cells, encounters and props. They are stored in a NaviPatch source prototypes that have the following structure:

```csharp
struct NaviPatchSourcePrototype
{
    // eFlagDontCook PatchFragments
    uint NaviPatchCrc;
    NaviPatchPrototype NaviPatch;
    NaviPatchPrototype PropPatch;
    float PlayableArea;
    float SpawnableArea;
}
```

NaviPatches themselves have the following structure:

```csharp
struct NaviPatchPrototype
{
    uint NumPoints;
    Vector3[NumPoints] Points;

    uint NumEdges;
    NaviPatchEdgePrototype[NumEdges] Edges;
}
```

NaviPatch edge prototypes have the following structure:

```csharp
struct NaviPatchEdgePrototype
{
    uint ProtoNameHash;
    uint Index0;
    uint Index1;

    uint NumFlags0;
    NaviContentFlags[NumFlags0] Flags0;

    uint NumFlags1;
    NaviContentFlags[NumFlags1] Flags1;
}

[Flags]
enum NaviContentFlags
{
    None        = 0,
    AddWalk     = 1 << 0,
    RemoveWalk  = 1 << 1,
    AddFly      = 1 << 2,
    RemoveFly   = 1 << 3,
    AddPower    = 1 << 4,
    RemovePower = 1 << 5,
    AddSight    = 1 << 6,
    RemoveSight = 1 << 7
}
```

## Path Collection

Path collection prototypes are used only in districts. They have the following structure:

```csharp
struct PathCollectionPrototype
{
    uint NumPathCollection;
    PathNodeSetPrototype[NumPathCollection] PathCollection;
}
```

Path node sets have the following structure:

```csharp
struct PathNodeSetPrototype
{
    uint ProtoNameHash;
    ushort Group;

    uint NumPathNodes;
    PathNodePrototype[NumPathNodes] PathNodes;

    ushort NumNodes;
}
```

Each path node is a Vector3 preceded by a hash of the prototype class name.

```csharp
struct PathNodePrototype
{
    uint ProtoNameHash;
    Vector3 Position;
}
```
