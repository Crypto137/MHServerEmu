# Locale

The game supports multiple locales. Locales and all localized strings for them are loaded from `%GameDirectory%\Data\Loco`.

Locales can be referred to using various identifiers. See the table below for a list of them.

| Enum Value | Display Name  | Directory Name | Region Code | Language Code 2 | Language Code 3 | Language Code PSN | Website Code |
| ---------- | ------------- | -------------- | ----------- | --------------- | --------------- | ----------------- | ------------ |
| 0          |               | chi            | CN          | zh              | chi             | zh-TW             | zh_tw        |
| 1          | English       | eng            | US          | en              | eng             | en                | en_us        |
| 2          | français      | fra            | FR          | fr              | fra             | fr                | fr_fr        |
| 3          | Deutsch       | deu            | DE          | de              | deu             | de                | de_de        |
| 4          | Greek Cypher  | sg2            | US          | el              |                 |                   | en_us        |
| 5          |               | jpn            | JP          | jp              | jpn             | ja                | ja_jp        |
| 6          |               | kor            | KR          | ko              | kor             | ko                | ko_kr        |
| 7          | Pig Latin     | sg1            | US          |                 |                 |                   | en_us        |
| 8          | [Placeholder] | sg3            | US          | en              | eng             | en                | en_us        |
| 9          | português     | por            | PT          | pt              | por             | pt                | pt_br        |
| 10         | Русский       | rus            | RU          | ru              | rus             | ru                | ru_ru        |
| 11         | español       | spa            | MX          | es              | spa             | es                | es_mx        |

## Locale File

Locale (`.locale`, signature `LOC`) files contain metadata for a given localization. They use the same header and string formats as [Calligraphy](./Calligraphy.md).

Locale files have the following structure:

```csharp
struct LocaleFile
{
    CalligraphyHeader Header;
    FixedString16 Name;
    FixedString16 LanguageDisplayName;
    FixedString16 RegionDisplayName;
    string Directory;
    ushort NumFlags;
    LocaleFlag[NumFlags] Flags;
}
```

Locale flags have the following structure:

```csharp
struct LocaleFlag
{
    ushort BitValue;
    ushort BitMask;
    string FlagText;
}
```

Localized strings for the given locale are stored in a subdirectory specified in the `Directory` field. Generally there are four files named `locale.all_3FFFFFFFFFFFFFFF.string`, `locale.all_7FFFFFFFFFFFFFFF.string`, `locale.all_BFFFFFFFFFFFFFFF.string`, `locale.all_FFFFFFFFFFFFFFFF.string`, with an appropriate language code instead of `locale` (see the table above for reference).

## String File

String (`.string`, signature `STR`) files contain a map of localized string ids. This format is also used in the achievement database dump sent to the client on login.

String files have the following structure:

```csharp
struct StringFile
{
    CalligraphyHeader Header;
    ushort NumEntries;
    StringMapEntry[NumEntries] StringMap;
}
```

Each string map entry has the following structure:

```csharp
struct StringMapEntry
{
    ulong LocaleStringId;
    ushort NumVariants;
    ushort FlagsProduced;
    uint Offset;

    if (NumVariants > 0)
        StringVariation[NumVariants - 1] Variants;
}
```

Variations are alternate versions of strings used in some languages (e.g. for grammatical cases in Russian). They have the following structure:

```csharp
struct StringVariation
{
    ulong FlagsConsumed;
    ulong FlagsProduced;
    uint Offset;
}
```

The actual strings are null-terminated UTF-8 encoded strings stored at offsets specified in the map. The offset is relative to the beginning of the file (0).
