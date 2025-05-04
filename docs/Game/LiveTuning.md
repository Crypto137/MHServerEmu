# Live Tuning

Live Tuning is a system that allows server owners to make certain gameplay adjustments without modifying game data directly.

## How to Use

Live Tuning data is automatically loaded on server startup from all `.json` files that start with `LiveTuningData` located in `MHServerEmu\Data\Game`. Live Tuning data can be reloaded while the server is running by typing the `!server reloadlivetuning` command in the server console.

The server comes bundled with a number of Live Tuning data files containing dumped settings from original servers and fixes for some bugs. These bundled files can be used as reference for creating your own Live Tuning data files.

## Tuning Variables

This section lists all Live Tuning variables that can be adjusted in game version `1.52.0.1700`.

The default value for all tuning variables is `1.0`. All tuning variables types except for `Global` (prefixed by `eGTV`) require a prototype reference. For `Global` tuning variables the prototype field should be empty.

### Global

| Tuning Variable                     | Description                                                                                                                                                                                                              |
| ----------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| eGTV_VendorBuyPrice                 | Multiplier for prices when buying items from vendors.                                                                                                                                                                    |
| eGTV_VendorSellPrice                | Multiplier for prices when selling items to vendors.                                                                                                                                                                     |
| eGTV_VendorXPGain                   | Multiplier for vendor experience when donating items.                                                                                                                                                                    |
| eGTV_PVPEnabled                     | Disables PvP game modes when set to 0.                                                                                                                                                                                   |
| eGTV_XPGain                         | Multiplier for experience.                                                                                                                                                                                               |
| eGTV_LootDropRate                   | Multiplier for the chance of loot rolling.                                                                                                                                                                               |
| eGTV_LootSpecialDropRate            | Multiplier for special item find (SIF).                                                                                                                                                                                  |
| eGTV_LootRarity                     | Multiplier for rare item find (RIF).                                                                                                                                                                                     |
| eGTV_PartyXPBonusPct                |                                                                                                                                                                                                                          |
| eGTV_PlayerTradeEnabled             | Disables player trade window when set to 0.                                                                                                                                                                              |
| eGTV_CosmicPrestigeXPPct            | Override for the cosmic prestige experience multiplier. Uses the game data multiplier when set to 1 (0.04 by default).                                                                                                   |
| eGTV_LootVaporizationEnabled        | Disables loot vaporization when set to 0.                                                                                                                                                                                |
| eGTV_XPBuffDisplay                  |                                                                                                                                                                                                                          |
| eGTV_SIFBuffDisplay                 |                                                                                                                                                                                                                          |
| eGTV_RIFBuffDisplay                 |                                                                                                                                                                                                                          |
| eGTV_OmegaXPPct                     | Multiplier for Omega experience.                                                                                                                                                                                         |
| eGTV_RespectLevelForGlobalXP        | Disables minimum level requirement for global experience multipliers when set to 0.                                                                                                                                      |
| eGTV_RespectLevelForGlobalRIF       | Disables minimum level requirement for global rare item find multipliers when set to 0.                                                                                                                                  |
| eGTV_RespectLevelForGlobalSIF       | Disables minimum level requirement for global special item find multipliers when set to 0.                                                                                                                               |
| eGTV_RespectLevelForOmegaXP         | Disables minimum level requirement for global Omega experience multipliers when set to 0.                                                                                                                                |
| eGTV_RespectLevelForAvatarXP        | Disables minimum level requirement for avatar experience multipliers when set to 0.                                                                                                                                      |
| eGTV_RespectLevelForRegionXP        | Disables minimum level requirement for region experience multipliers when set to 0.                                                                                                                                      |
| eGTV_ServerBonusUnlockLevelOverride | Override for the minimum level required for live tuning multipliers to apply. Uses the game data value when set to 1 (60 by default). This level needs to be reached on any avatar on an account for the bonus to apply. |
| eGTV_BoostTimersRunning             | Pauses boost timers even outside of hubs when set to 0.                                                                                                                                                                  |
| eGTV_InfinityXPPct                  | Multiplier for Infinity experience.                                                                                                                                                                                      |
| eGTV_RespectLevelForInfinityXP      | Disables minimum level requirement for global Infinity experience multipliers when set to 0.                                                                                                                             |
| eGTV_SuperVerboseMetricsEnabled     |                                                                                                                                                                                                                          |
| eGTV_HighVolumeMetricsEnabled       |                                                                                                                                                                                                                          |
| eGTV_MediumVolumeMetricsEnabled     |                                                                                                                                                                                                                          |
| eGTV_LowVolumeMetricsEnabled        |                                                                                                                                                                                                                          |

### Area

| Tuning Variable             | Description |
| --------------------------- | ----------- |
| eATV_AreaMobSpawnHeat       |             |
| eATV_AreaMobSpawnHeatReturn |             |

### World Entity

| Tuning Variable             | Description |
| --------------------------- | ----------- |
| eWETV_MobPowerDamage        |             |
| eWETV_MobHealth             |             |
| eWETV_MobXP                 |             |
| eWETV_MobDropRate           |             |
| eWETV_MobSpecialDropRate    |             |
| eWETV_Enabled               |             |
| eWETV_MobDropRarity         |             |
| eWETV_VendorEnabled         |             |
| eWETV_Unused1               |             |
| eWETV_EternitySplinterPrice |             |
| eWETV_LootGroupNum          |             |
| eWETV_LootNoDropPercent     |             |
| eWETV_Visible               |             |

### Avatar

| Tuning Variable             | Description |
| --------------------------- | ----------- |
| eAETV_BonusXPPct            |             |
| eAETV_XPBuffDisplay         |             |
| eAETV_EternitySplinterPrice |             |
| eAETV_Enabled               |             |

### Population Object

| Tuning Variable              | Description |
| ---------------------------- | ----------- |
| ePOTV_PopulationObjectWeight |             |

### Power

| Tuning Variable     | Description |
| ------------------- | ----------- |
| ePTV_PowerCost      |             |
| ePTV_PowerDamagePVE |             |
| ePTV_PowerDamagePVP |             |
| ePTV_PowerEnabled   |             |

### Region

| Tuning Variable             | Description |
| --------------------------- | ----------- |
| eRTV_PlayerLimit            |             |
| eRTV_Enabled                |             |
| eRT_BonusXPPct              |             |
| eRT_XPBuffDisplay           |             |
| eRT_BonusItemFindMultiplier |             |

### Loot Table

| Tuning Variable     | Description |
| ------------------- | ----------- |
| eLTTV_Enabled       |             |
| eLTTV_Weight        |             |
| eLTTV_Rolls         |             |
| eLTTV_NoDropPercent |             |
| eLTTV_GroupNum      |             |

### Mission

| Tuning Variable    | Description |
| ------------------ | ----------- |
| eMTV_Enabled       |             |
| eMTV_EventInstance |             |

### Condition

| Tuning Variable | Description |
| --------------- | ----------- |
| eCTV_Enabled    |             |

### Public Event

| Tuning Variable     | Description |
| ------------------- | ----------- |
| ePETV_Enabled       |             |
| ePETV_EventInstance |             |

### Metrics Frequency

| Tuning Variable  | Description |
| ---------------- | ----------- |
| eMFTV_SampleRate |             |