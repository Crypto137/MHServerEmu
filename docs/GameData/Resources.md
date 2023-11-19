# Resources

Resources are serialized prototypes that do not follow the standard [Calligraphy](./Calligraphy.md) format. They are stored separately in the `mu_cdata.sip` archive.

There are five types of resources: cells, districts, encounters, props, prop sets, and UIs. All resource files start with the same 12-byte header:

```csharp
struct ResourceHeader
{
    uint Signature;
    uint Version;
    uint ClassId;
}
```

Strings in resource files are similar to Calligraphy (fixed-length UTF-8 strings preceded by their length), however they use 32-bit instead of 16-bit integers for length;

```csharp
struct FixedString32
{
    int StringLength;
    char[StringLength] String;
}
```

Resources make use of vectors, that have the following structure unless noted otherwise:

```csharp
struct Vector3
{
    float X;
    float Y;
    float Z;
}
```

There are also some auxiliary prototypes used in resources that you can read about [here](./AuxiliaryResourcePrototypes.md).

## Cell

Cell (`.cell`) files contain cell prototypes. Cells are basic building blocks used in procedural area generation, similar to tiles in other Diablo-like games.

Cell files have the following structure:

```csharp
struct CellPrototype
{
    ResourceHeader Header;
    Vector3 AabbMax;    // Aabb BoundingBox
    Vector3 AabbMin;
    uint Type;
    uint Walls;
    uint FillerEdges;
    uint RoadConnections;
    FixedString32 ClientMap;

    int NumInitializeSet;
    MarkerPrototype[NumInitializeSet] InitializeSet;

    int NumMarkerSet;
    MarkerPrototype[NumMarkerSet] MarkerSet;

    NaviPatchSourcePrototype NaviPatchSource;
    byte IsOffsetInMapFile;
    CellHeightMap HeightMap;

    uint NumHotspotPrototypes;
    ulong[NumHotspotPrototypes] HotspotPrototypes; // PrototypeGuid 
}
```

For more information on `MarkerPrototype` and `NaviPatchSourcePrototype` see [here](./AuxiliaryResourcePrototypes.md). Cell height maps have the following structure:

```csharp
struct CellHeightMap
{
    uint HeightMapSizeX;
    uint HeightMapSizeY;

    uint NumHeightMapData;
    short[NumHeightMapData] HeightMapData;

    uint NumHotspotData;
    byte[NumHotspotData] Hotspotdata
}
```

## District

District (`.district`) files contain district prototypes. Districts are collections of cells with fixed layouts.

District files have the following structure:

```csharp
struct DistrictPrototype
{
    ResourceHeader Header;

    uint NumCellMarkerSet;
    ResourceMarkerPrototype[NumCellMarkerSet] CellMarkerSet;

    uint NumMarkerSet;    // Seems to be always 0 in 1.52.0.1700
    MarkerPrototype[NumMarkerSet] MarkerSet;

    PathCollectionPrototype PathCollection;
}
```

For more information on `PathCollectionPrototype` see [here](./AuxiliaryResourcePrototypes.md). 

## Encounter

Encounter (`.encounter`) files contain encounter prototypes.

Encounter files have the following structure:

```csharp
struct EncounterPrototype
{
    ResourceHeader Header;
    ulong PopulationMarkerGuid; // PrototypeGuid
    FixedString32 ClientMap;

    uint NumMarkerSet;
    MarkerPrototype[NumMarkerSet] MarkerSet;

    NaviPatchSourcePrototype NaviPatchSource;
}
```

For more information on `MarkerPrototype` and `NaviPatchSourcePrototype` see [here](./AuxiliaryResourcePrototypes.md).

## Prop

Prop (`.prop`) files contain prop prototypes.

Prop files have the following structure:

```csharp
struct PropPrototype
{
    ResourceHeader Header;

    uint NumPropGroups;
    ProceduralPropGroupPrototype[NumPropGroups] PropGroups;
}
```

Each prop group has the following structure:

```csharp
struct ProceduralPropGroupPrototype
{
    uint ProtoNameHash;
    FixedString32 NameId;
    FixedString32 PrefabPath;
    Vector3 MarkerPosition;
    Vector3 MarkerRotation;

    uint NumObjects;
    MarkerPrototype[NumObjects] Objects;

    NaviPatchSourcePrototype NaviPatchSource;
    ushort RandomRotationDegrees;
    ushort RandomPosition;
}
```

For more information on `MarkerPrototype` and `NaviPatchSourcePrototype` see [here](./AuxiliaryResourcePrototypes.md). 

## Prop Set

Prop set (`.propset`) files contain prop set prototypes.

Prop set files have the following structure:

```csharp
struct PropSetPrototype
{
    ResourceHeader Header;

    uint NumPropShapeLists;
    PropSetTypeListPrototype[NumPropShapeLists] PropShapeLists;

    FixedString32 PropSetPackage;
}
```

Each prop set type list prototype has the following structure:

```csharp
struct PropSetTypeListPrototype
{
    uint ProtoNameHash;

    uint NumPropShapeEntries;
    PropSetTypeEntryPrototype[NumPropShapeEntries] PropShapeEntries;

    ulong PropType; // PrototypeGuid
}
```

Entries in prop set type lists have the following structure:

```csharp
struct PropSetTypeEntryPrototype
{
    uint ProtoNameHash;
    FixedString32 NameId;
    FixedString32 ResourcePackage;
}
```

## UI

UI (`.ui`) files contain UI prototypes. It is currently unclear if these prototypes are needed server-side.

UI files have the following structure:

```csharp
struct UIPrototype
{
    ResourceHeader Header;

    uint NumUIPanels;
    UIPanelPrototype[NumUIPanels] UIPanels;
}
```

There are two types of UI panel prototypes used in 1.52.0.1700: `StretchedPanelPrototype` and `AnchoredPanelPrototype`. Other versions of the client use additional panel prototypes that are currently unknown.

The format of the panel to follow depends on the ProtoNameHash, similarly to markers. A ProtoNameHash with a value of `0` indicates that no data follows it.

All UI panel prototypes have the following fields:

```csharp
struct UIPanelPrototypeCommon
{
    FixedString32 PanelName;
    FixedString32 TargetName;
    uint ScaleMode;
    UIPanelPrototype Children;
    FixedString32 WidgetClass;
    FixedString32 SwfName;
    byte OpenOnStart;
    byte VisibilityToggleable;
    byte CanClickThrough;
    byte StaticPosition;
    byte EntityInteractPanel;
    byte UseNewPlacementSystem;
    byte KeepLoaded;
}
```

`StretchedPanelPrototype` has the following structure:

```csharp
struct StretchedPanelPrototype
{
    uint ProtoNameHash; // djb2 805156721
    Vector2 TopLeftPin;
    FixedString32 TL_X_TargetName;
    FixedString32 TL_Y_TargetName;
    Vector2 BottomRightPin;
    FixedString32 BR_X_TargetName;
    FixedString32 BR_Y_TargetName;

    UIPanelPrototypeCommon CommonFields;
}
```

`AnchoredPanelPrototype` has the following structure:

```csharp
struct AnchoredPanelPrototype
{
    uint ProtoNameHash; // djb2 1255662575
    Vector2 SourceAttachmentPin;
    Vector2 TargetAttachmentPin;
    Vector2 VirtualPixelOffset;
    FixedString32 PreferredLane;
    Vector2 OuterEdgePin;
    Vector2 NewSourceAttachmentPin;

    UIPanelPrototypeCommon CommonFields;
}
```
