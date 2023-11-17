# Auxiliary Resource Prototypes

[Resources](./Resources.md) make use of a number of auxiliary prototypes.

## Markers

Cells, districts, and props also contain markers - prototypes that indicate position and rotation of various things. All markers start with a [djb2 hash](https://theartincode.stanis.me/008-djb2/) of the prototype name that defines the further structure. There's a total of six marker types used by resources: cell connector, dot corner, entity, resource, road connection, and unreal prop.

Cell connectors, dot corners, and road connection markers are used in cells, encounters and props, and have the same structure:

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

Entity markers are also used in cells, encounters and props, and have the following structure:

```csharp
struct EntityMarkerPrototype
{
    uint ProtoNameHash;
    ulong EntityGuid;    // PrototypeGuid
    FixedString32 LastKnownEntityName;
    ulong Modifier1Guid; // PrototypeGuid
    if (Modifier1Guid != 0) FixedString32 Modifier1Text;
    ulong Modifier2Guid; // PrototypeGuid
    if (Modifier2Guid != 0) FixedString32 Modifier2Text;
    ulong Modifier3Guid; // PrototypeGuid
    if (Modifier3Guid != 0) FixedString32 Modifier3Text;
    uint EncounterSpawnPhase;
    byte OverrideSnapToFloor;
    byte OverrideSnapToFloorValue;
    ulong FilterGuid;    // PrototypeGuid
    FixedString32 LastKnownFilterName;
    Vector3 Position;
    Vector3 Rotation;
}
```

Resource markers are used in districts, encounters and props, and have the following structure:

```csharp
struct ResourceMarkerPrototype
{
    uint ProtoNameHash;
    FixedString32 Resource;
    Vector3 Position;
    Vector3 Rotation;
}
```

Unreal prop markers are used only in encounters and props, and have the following structure:

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
    byte[NumFlags0] Flags0;

    uint NumFlags1;
    byte[NumFlags1] Flags1;
}
```

## Path Nodes

Path node prototypes are used only in districts. They are stored in sets that have the following structure:

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

Each path node is a Vector3 preceded by a hash of the prototype name.

```csharp
struct PathNodePrototype
{
    uint ProtoNameHash;
    Vector3 Position;
}
```
