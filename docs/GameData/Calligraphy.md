# Calligraphy

Calligraphy is a custom game data management system developed by Gazillion. Its main purpose was most likely providing game designers convenient tools for editing game data. Data exported from Calligraphy is stored in the `Calligraphy.sip` archive.

Calligraphy uses five file formats: directories, curves, asset types, blueprints, and prototypes. All Calligraphy files start with the same four-byte header:

```csharp
char[3] Magic;
byte Version;
```

The magic string defines what format is used in the file. The version depends on the game version: game versions 1.9-1.17 used Calligraphy version 10, and all later game versions starting with 1.18 released on January 24th 2014 use Calligraphy version 11.

## Directory

Directory (.directory) files contain information required for the initialization of the `DataDirectory` singleton class. There's a total of five directory files, each containing a number of records with slightly different structures.

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
ulong Id;
ulong Guid;
ulong BlueprintId; // Even though it's called BlueprintId, this is actually a parent default prototype id
byte Flags;
ushort FilePathLength;
char[FilePathLength] FilePath;
```

`Replacement.directory` (signature `RDR`) is a special directory used for handling deprecated GUIDs. It has a different structure:

```csharp
ulong OldGuid;
ulong NewGuid;
ushort NameLength;
char[NameLength] Name;
```

Please note that file paths contained in these directory files use the `\` symbol as the path delimiter, while [pak files](./PakFile.md) use `/`. To use these paths for reading files from the pak file system you need to replace `\` with `/` while reading them.

## Curve

TODO

## Asset Type

TODO

## Blueprint

TODO

## Prototype

TODO
