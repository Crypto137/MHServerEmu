# Pak File

A pak file (also known as `GPAK` by its signature and `sip` by its extension) is an archive that contains game data. Early versions of the game used a SQLite database for storage, but later on it was replaced with a custom format.

Initially all data was stored in a single `mu_cdata.sip` file located in `%GameDirectory%\UnrealEngine3\Binaries\Win32\Data`. Eventually data was separated into two files (`Calligraphy.sip` and `mu_cdata.sip`) located in `%GameDirectory%\Data\Game`.

## SQLite Paks

These older paks can be opened with existing general-purpose tools, such as [DB Browser for SQLite](https://sqlitebrowser.org/). They contain two tables: `data_tbl` and `ver`.

The `data_tbl` table has the following columns:

| Name | Type      | Constraints             | Description    |
| ---- | --------- | ----------------------- | -------------- |
| `i`  | `INTEGER` |                         | File id / hash |
| `n`  | `TEXT`    | `UNIQUE COLLATE NOCASE` | File name      |
| `b`  | `BLOB`    |                         | Data           |
| `l`  | `INTEGER` |                         | Data size      |
| `s`  | `INTEGER` |                         | Timestamp      |

The `ver` table has only a single row with the following columns:

| Name | Type   | Description           |
| ---- | ------ | --------------------- |
| `v`  | `REAL` | Format version number |
| `s`  | `TEXT` | Format version note   |

There are two known versions of these SQLite-based paks:

| Format Version | Note                                                                        | Client Version |
| -------------- | --------------------------------------------------------------------------- | -------------- |
| 1.5            | Added back index for names for non-maxload optimization                     | 1.9-1.22       |
| 1.6            | Added lz4 compression for stored data to speed up shipping client load time | 1.22-1.28      |

## Custom Gazillion Paks

In version `1.29.0.25` released on November 11, 2014 the original SQLite-based archives were replaced with a new custom format developed by Gazillion. Later on in version `1.0.4458.0` released on January 24, 2015 data was also split into two files: `Calligraphy.sip` for data exported from [Calligraphy](./Calligraphy.md) and `mu_cdata.sip` for [resource](./Resources.md) data.

**Note:** it's possible to force newer client versions to load all data from the `mu_cdata.sip` archive by using the `-nocalligraphypak` launch parameter.

Files with this newer format have the following structure:

```csharp
struct PakFile
{
    uint Header;    // KAPG
    uint Version;

    int NumEntries;
    PakEntry[NumEntries] Entries;

    byte[] CompressedRawData;
}
```

Entries have the following structure:

```csharp
struct PakEntry
{
    ulong FileHash;
    int FileNameLength;
    byte[FileNameLength] FileName; // UTF-8
    int ModTime;
    int Offset;
    int CompressedSize;
    int UncompressedSize;
}
```

The entries are followed by raw data compressed using the [LZ4](https://github.com/lz4/lz4) algorithm. The offsets specified in the entries are from where the raw data begins.

Data from these custom paks can be extracted and parsed with [MHDataParser](https://github.com/Crypto137/MHDataParser).
