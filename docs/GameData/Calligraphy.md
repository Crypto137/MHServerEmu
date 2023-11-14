# Calligraphy

Calligraphy is a custom game data management system developed by Gazillion. Its main purpose was most likely providing game designers convenient tools for editing game data. Data exported from Calligraphy is stored in the `Calligraphy.sip` archive.

Calligraphy uses five file formats: directory, curve, asset type, blueprint, and prototype. All Calligraphy files start with the same four-byte header:

```csharp
char[3] Magic;
byte Version;
```

The magic string defines what format is used in the file. The version depends on the game version: game versions 1.9-1.17 used Calligraphy version 10, and all later game versions starting with 1.18 released on January 24th 2014 use Calligraphy version 11.

## Directory

Directory (`.directory`) files contain information required for the initialization of the `DataDirectory` singleton class. There's a total of five directory files, each containing a number of records with slightly different structures.

All directories start with a Calligraphy header and the number of records contained in the directory.

```csharp
CalligraphyHeader Header;
uint RecordsLength;
Record[RecordsLength] Records;
```

`Curve.directory` (signature `CDR`), `Type.directory` (signature `CDR`), and `Blueprint.directory` (signature `BDR`) have the same standard record structure:

```csharp
ulong Id;
ulong Guid;
byte Flags;
ushort FilePathLength;
char[FilePathLength] FilePath;
```

`Prototype.directory` (signature `PDR`) has a modified structure:

```csharp
ulong PrototypeId;
ulong PrototypeGuid;
ulong BlueprintId; // Even though it's called BlueprintId, this is actually a parent default prototype id
byte Flags;
ushort FilePathLength;
char[FilePathLength] FilePath;
```

`Replacement.directory` (signature `RDR`) is a special directory used for handling deprecated GUIDs. Replacement records are managed by the `ReplacementDirectory` class. This file has a different record structure:

```csharp
ulong OldGuid;
ulong NewGuid;
ushort NameLength;
char[NameLength] Name;
```

Please note that file paths contained in these directory files use the `\` symbol as the path delimiter, while [pak files](./PakFile.md) use `/`. To use these paths for reading files from the pak file system you need to replace `\` with `/` while reading them.

## Curve

Curve (`.curve`, signature `CRV`) files contain collections of 64-bit floating point values. They are used for various purposes as values for prototype fields. Loaded curves are managed by the `CurveDirectory` class.

Curve files have the following structure:

```csharp
CalligraphyHeader Header;
int StartPosition;
int EndPosition;
double[EndPosition - StartPosition + 1] Values;
```

## Asset Type

Asset type (`.type`, signature `TYP`) files contain collections of asset references of specific types. They function as essentially enumerators for instances of various types of external data, and some of them are bound to enums in code. Loaded asset types are managed by the `AssetDirectory` class.

Asset type files have the following structure:

```csharp
CalligraphyHeader Header;
ushort AssetsLength;
Asset[AssetsLength]; Assets;
```

Each asset in an asset type has the following structure:

```csharp
ulong AssetId;    // Processed by the client as the StringId for the name
ulong AssetGuid;
byte Flags;
ushort NameLength;
char[NameLength] Name;
```



## Blueprint

TODO

## Prototype

TODO
