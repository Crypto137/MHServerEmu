# Live Tuning

Live Tuning is a system that allows server owners to make certain gameplay adjustments without modifying game data directly.

## How to Use

Live Tuning data is automatically loaded on server startup from all `.json` files that start with `LiveTuningData` located in `MHServerEmu\Data\Game`. Live Tuning data can be reloaded while the server is running by typing the `!server reloadlivetuning` command in the server console.

The server comes bundled with a number of Live Tuning data files containing dumped settings from original servers and fixes for some bugs. These bundled files can be used as reference for creating your own Live Tuning data files.

## Tuning Variables

This section lists all Live Tuning variables that can be adjusted in game version `1.52.0.1700`.

The default value for all tuning variables is `1.0`. All tuning variables types except for `Global` (prefixed by `eGTV`) require a prototype reference. For `Global` tuning variables the prototype field should be empty.

### Global

| Tuning Variable                     | Description                                                                                                                                                                                                            |
| ----------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| eGTV_VendorBuyPrice                 | Multiplier for prices when buying items from vendors.                                                                                                                                                                  |
| eGTV_VendorSellPrice                | Multiplier for prices when selling items to vendors.                                                                                                                                                                   |
| eGTV_VendorXPGain                   | Multiplier for vendor experience when donating items.                                                                                                                                                                  |
| eGTV_PVPEnabled                     | Disables PvP game modes when set to 0.                                                                                                                                                                                 |
| eGTV_XPGain                         | Multiplier for experience.                                                                                                                                                                                             |
| eGTV_LootDropRate                   | Multiplier for the chance of loot rolling.                                                                                                                                                                             |
| eGTV_LootSpecialDropRate            | Multiplier for special item find (SIF).                                                                                                                                                                                |
| eGTV_LootRarity                     | Multiplier for rare item find (RIF).                                                                                                                                                                                   |
| eGTV_PartyXPBonusPct                |                                                                                                                                                                                                                        |
| eGTV_PlayerTradeEnabled             | Disables player trade window when set to 0.                                                                                                                                                                            |
| eGTV_CosmicPrestigeXPPct            | Override for the cosmic prestige experience multiplier. Uses the game data multiplier when set to 1 (0.04 by default).                                                                                                 |
| eGTV_LootVaporizationEnabled        | Disables loot vaporization when set to 0.                                                                                                                                                                              |
| eGTV_XPBuffDisplay                  |                                                                                                                                                                                                                        |
| eGTV_SIFBuffDisplay                 |                                                                                                                                                                                                                        |
| eGTV_RIFBuffDisplay                 |                                                                                                                                                                                                                        |
| eGTV_OmegaXPPct                     | Multiplier for Omega experience.                                                                                                                                                                                       |
| eGTV_RespectLevelForGlobalXP        | Disables minimum level requirement for global experience multipliers when set to 0.                                                                                                                                    |
| eGTV_RespectLevelForGlobalRIF       | Disables minimum level requirement for global rare item find multipliers when set to 0.                                                                                                                                |
| eGTV_RespectLevelForGlobalSIF       | Disables minimum level requirement for global special item find multipliers when set to 0.                                                                                                                             |
| eGTV_RespectLevelForOmegaXP         | Disables minimum level requirement for Omega experience multipliers when set to 0.                                                                                                                                     |
| eGTV_RespectLevelForAvatarXP        | Disables minimum level requirement for avatar experience multipliers when set to 0.                                                                                                                                    |
| eGTV_RespectLevelForRegionXP        | Disables minimum level requirement for region experience multipliers when set to 0.                                                                                                                                    |
| eGTV_ServerBonusUnlockLevelOverride | Override for the minimum level required for live tuning multipliers to apply. Uses the game data value when set to 1 (60 by default). This level needs to be reached on any avatar on an account multipliers to apply. |
| eGTV_BoostTimersRunning             | Pauses boost timers even outside of hubs when set to 0.                                                                                                                                                                |
| eGTV_InfinityXPPct                  | Multiplier for Infinity experience.                                                                                                                                                                                    |
| eGTV_RespectLevelForInfinityXP      | Disables minimum level requirement for Infinity experience multipliers when set to 0.                                                                                                                                  |
| eGTV_SuperVerboseMetricsEnabled     |                                                                                                                                                                                                                        |
| eGTV_HighVolumeMetricsEnabled       |                                                                                                                                                                                                                        |
| eGTV_MediumVolumeMetricsEnabled     |                                                                                                                                                                                                                        |
| eGTV_LowVolumeMetricsEnabled        |                                                                                                                                                                                                                        |

### Area

| Tuning Variable             | Description |
| --------------------------- | ----------- |
| eATV_AreaMobSpawnHeat       |             |
| eATV_AreaMobSpawnHeatReturn |             |

### World Entity

World Entity tuning variables can be applied to mobs, NPCs, and items.

| Tuning Variable             | Description                                                                                           |
| --------------------------- | ----------------------------------------------------------------------------------------------------- |
| eWETV_MobPowerDamage        | Multiplier for mob damage.                                                                            |
| eWETV_MobHealth             | Multiplier for mob health.                                                                            |
| eWETV_MobXP                 | Multiplier for mob kill experience reward.                                                            |
| eWETV_MobDropRate           | Multiplier for the chance of loot rolling from a specific mob.                                        |
| eWETV_MobSpecialDropRate    | Multiplier for special item find (SIF) from a specific mob.                                           |
| eWETV_Enabled               | Disables this world entity when set to 0.                                                             |
| eWETV_MobDropRarity         | Multiplier for rare item find (RIF) from a specific mob.                                              |
| eWETV_VendorEnabled         | Disables vendor functionality of a specific NPC.                                                      |
| eWETV_Unused1               |                                                                                                       |
| eWETV_EternitySplinterPrice | Override for the Eternity Splinter price when buying this item from a vendor.                         |
| eWETV_LootGroupNum          | Specifies loot group number for this item to add it to a loot table.                                  |
| eWETV_LootNoDropPercent     | Specifies the no drop chance for an item. This should be used in combination with eWETV_LootGroupNum. |
| eWETV_Visible               | Makes this world entity invisible when set to 0.                                                      |

### Avatar

| Tuning Variable             | Description                                                                                                                                                                |
| --------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| eAETV_BonusXPPct            | Avatar-specific experience multiplier.                                                                                                                                     |
| eAETV_XPBuffDisplay         |                                                                                                                                                                            |
| eAETV_EternitySplinterPrice | Override for the Eternity Splinter price when buying this avatar from a vendor. NOTE: This tuning variable is broken client-side when buying heroes from the roster panel. |
| eAETV_Enabled               | Disables this avatar when set to 0.                                                                                                                                        |

### Population Object

| Tuning Variable              | Description |
| ---------------------------- | ----------- |
| ePOTV_PopulationObjectWeight |             |

### Power

| Tuning Variable     | Description                        |
| ------------------- | ---------------------------------- |
| ePTV_PowerCost      | Multiplier for power cost.         |
| ePTV_PowerDamagePVE | Multiplier for power PvE damage.   |
| ePTV_PowerDamagePVP | Multiplier for power PvP damage.   |
| ePTV_PowerEnabled   | Disables this power when set to 0. |

### Region

| Tuning Variable             | Description                                          |
| --------------------------- | ---------------------------------------------------- |
| eRTV_PlayerLimit            |                                                      |
| eRTV_Enabled                | Disables this region when set to 0.                  |
| eRT_BonusXPPct              | Region-specific experience multiplier.               |
| eRT_XPBuffDisplay           |                                                      |
| eRT_BonusItemFindMultiplier | Multiplier for bonus item find (BIF) in this region. |

### Loot Table

| Tuning Variable     | Description                                                                                                      |
| ------------------- | ---------------------------------------------------------------------------------------------------------------- |
| eLTTV_Enabled       | Disables this loot table when set to 0.                                                                          |
| eLTTV_Weight        | Multiplier for the weight value of this loot table.                                                              |
| eLTTV_Rolls         | Override for the number of rolls for this loot table.                                                            |
| eLTTV_NoDropPercent | Override for the no drop chance for this loot table.                                                             |
| eLTTV_GroupNum      | When this loot table is rolled, world entities that have the same loot group number specified are also included. |

### Mission

| Tuning Variable    | Description                          |
| ------------------ | ------------------------------------ |
| eMTV_Enabled       | Disables this mission when set to 0. |
| eMTV_EventInstance |                                      |

### Condition

| Tuning Variable | Description                            |
| --------------- | -------------------------------------- |
| eCTV_Enabled    | Disables this condition when set to 0. |

### Public Event

| Tuning Variable     | Description |
| ------------------- | ----------- |
| ePETV_Enabled       |             |
| ePETV_EventInstance |             |

### Metrics Frequency

| Tuning Variable  | Description |
| ---------------- | ----------- |
| eMFTV_SampleRate |             |