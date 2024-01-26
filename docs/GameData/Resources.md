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
    byte[StringLength] String; // UTF-8
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
    CellType Type;
    uint Walls;
    CellFiller FillerEdges;
    CellType RoadConnections;
    FixedString32 ClientMap;
    MarkerSetPrototype InitializeSet;
    MarkerSetPrototype MarkerSet;
    NaviPatchSourcePrototype NaviPatchSource;
    byte IsOffsetInMapFile;
    HeightMapPrototype HeightMap;

    uint NumHotspotPrototypes;
    ulong[NumHotspotPrototypes] HotspotPrototypes; // PrototypeGuid 
}
```

For more information on `MarkerSetPrototype` and `NaviPatchSourcePrototype` see [here](./AuxiliaryResourcePrototypes.md).

Cardinal directions are specified in cells using the following enums:

```csharp
enum CellType
{
    None = 0,
    N = 1,
    E = 2,
    S = 4,
    W = 8,
    NS = 5,
    EW = 10,
    NE = 3,
    NW = 9,
    ES = 6,
    SW = 12,
    ESW = 14,
    NSW = 13,
    NEW = 11,
    NES = 7,
    NESW = 15,
    NESWdNW = 159,
    NESWdNE = 207,
    NESWdSW = 63,
    NESWdSE = 111,
    NESWcN = 351,
    NESWcE = 303,
    NESWcS = 159,
    NESWcW = 207,
}

enum CellWallGroup
{
    N = 254,
    E = 251,
    S = 239,
    W = 191,
    NE = 250,
    ES = 235,
    SW = 175,
    NW = 190,
    NS = 238,
    EW = 187,
    NES = 234,
    ESW = 171,
    NSW = 174,
    NEW = 186,
    NESW = 170,
    WideNE = 248,
    WideES = 227,
    WideSW = 143,
    WideNW = 62,
    WideNES = 224,
    WideESW = 131,
    WideNSW = 14,
    WideNEW = 56,
    WideNESW = 0,
    WideNESWcN = 130,
    WideNESWcE = 10,
    WideNESWcS = 40,
    WideNESWcW = 160,
}

enum CellFiller
{
    N = 1,
    NE = 2,
    E = 4,
    SE = 8,
    S = 16,
    SW = 32,
    W = 64,
    NW = 128,
    C = 256,
}
```

Height maps have the following structure:

```csharp
struct HeightMapPrototype
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

District (`.district`) files contain district resource prototypes. Districts are collections of cells with fixed layouts.

District files have the following structure:

```csharp
struct DistrictResourcePrototype
{
    ResourceHeader Header;
    MarkerSetPrototype CellMarkerSet; 
    MarkerSetPrototype MarkerSet; // Always empty in 1.52.0.1700
    PathCollectionPrototype PathCollection;
}
```

For more information on `MarkerSetPrototype` and `PathCollectionPrototype` see [here](./AuxiliaryResourcePrototypes.md). 

## Encounter

Encounter (`.encounter`) files contain encounter prototypes.

Encounter files have the following structure:

```csharp
struct EncounterPrototype
{
    ResourceHeader Header;
    ulong PopulationMarkerGuid; // PrototypeGuid
    FixedString32 ClientMap;
    MarkerSetPrototype MarkerSet;
    NaviPatchSourcePrototype NaviPatchSource;
}
```

For more information on `MarkerSetPrototype` and `NaviPatchSourcePrototype` see [here](./AuxiliaryResourcePrototypes.md).

## Prop

Prop (`.prop`) files contain prop package prototypes.

Prop files have the following structure:

```csharp
struct PropPackagePrototype
{
    ResourceHeader Header;

    uint NumPropGroups;
    ProceduralPropGroupPrototype[NumPropGroups] PropGroups;
}
```

Each procedural prop group has the following structure:

```csharp
struct ProceduralPropGroupPrototype
{
    uint ProtoNameHash;
    FixedString32 NameId;
    FixedString32 PrefabPath;
    Vector3 MarkerPosition;
    Vector3 MarkerRotation;
    MarkerSetPrototype Objects;
    NaviPatchSourcePrototype NaviPatchSource;
    ushort RandomRotationDegrees;
    ushort RandomPosition;
}
```

For more information on `MarkerSetPrototype` and `NaviPatchSourcePrototype` see [here](./AuxiliaryResourcePrototypes.md). 

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

The format of the panel to follow depends on the ProtoNameHash, similarly to [markers](./AuxiliaryResourcePrototypes.md). A ProtoNameHash with a value of `0` indicates that no data follows it.

All UI panel prototypes have the following fields:

```csharp
struct UIPanelPrototypeCommon
{
    FixedString32 PanelName;
    FixedString32 TargetName;
    PanelScaleMode ScaleMode;
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

There are six panel scale modes:

```csharp
enum PanelScaleMode
{
    None,
    XStretch,
    YOnly,
    XOnly,
    Both,
    ScreenSize
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
