# Internal Console Commands

These are console commands that were available in internal game builds as of version `1.0.4932.0` from June 25, 2015.

| Name                                          | Description                                                                                                                            |
| --------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------- |
| AbilitiesRemoveAll                            | Removes all abilities slotted on the local player's current avatar                                                                     |
| AbilityActivate                               | Casts the specified ability in the facing direction                                                                                    |
| AbilityRemove                                 | Remove the ability in the given slot for the local player's current avatar                                                             |
| AbilitySlot                                   | Slots the given ability prototype in the given slot for the local player's current avatar                                              |
| AchievementCompleteAll                        | Completes the progress for all achievements.                                                                                           |
| AchievementEventReport                        | Prints information about how many achievement events have been processed and the average events per second                             |
| AchievementFireEvent                          | Simulates firing a game event for the achievement.                                                                                     |
| AchievementIdToggle                           | Toggles display of Achievement Ids in the UI.                                                                                          |
| AchievementList                               | Prints a list of achievements.                                                                                                         |
| AchievementReset                              | Resets the progress for an achievement.                                                                                                |
| AchievementResetAll                           | Resets the progress for all achievements.                                                                                              |
| AchievementScores                             | Prints total achievement score and per category scores.                                                                                |
| AchievementStatus                             | Prints the status of the achievement specified by id.                                                                                  |
| AcquireAndSwitchToAvatar                      | Unlock and switch to the specified playable character                                                                                  |
| ActionLoopToggle                              | Toggles the player to repeat their last action (basic attack or power)                                                                 |
| AI                                            |                                                                                                                                        |
| AIBlackboardPropertySet                       | Set a blackboard property on a specific AI agents                                                                                      |
| AIPerformanceTimeSliceClient                  | Set this value for the performance capture time slice on the client                                                                    |
| AIPerformanceTimeSliceServer                  | Set this value for the performance capture time slice on the server                                                                    |
| AISetProceduralOverride                       | Override a procedural AI with a given profile                                                                                          |
| AIToggle                                      | Toggles the specified agent's AI override                                                                                              |
| AllianceSet                                   | Sets the Alliance property as given for the given entity                                                                               |
| AnimDebugTextToggle                           | Toggles the display of anim debug text in world next to entities                                                                       |
| AOIDiscovered                                 | Prints a list of discovered entities.                                                                                                  |
| AOIRefresh                                    | Refresh AOI                                                                                                                            |
| AOIRevealAll                                  | Override AOI to reveal everything                                                                                                      |
| AOIRevealDefault                              | Remove any AOI override that has been enabled                                                                                          |
| AOIShow                                       |                                                                                                                                        |
| AOIUpdateOthers                               | Tell the server to consider your current entity in all AOI                                                                             |
| ApocolypseEngine                              | Teleports player around level and destroys everything hostile in region.                                                               |
| ApprovalThresholdSet                          | Set the approval threshold of the game database.                                                                                       |
| AreaDebug                                     |                                                                                                                                        |
| AreaListInCurrentRegion                       | Display a list of the areas in the current region                                                                                      |
| AssertClient                                  | Fire an assert on the client                                                                                                           |
| AssertServer                                  | Fires an assert on the server                                                                                                          |
| AudioBusStatus                                | Displays the current status for audio busses                                                                                           |
| AudioToggle                                   | Toggles all audio on/off.                                                                                                              |
| AvatarSpeed                                   | Sets a multiplier on your current avatar's speed                                                                                       |
| AvatarsUnlockShipping                         | Unlocks the playable characters that we plan to ship                                                                                   |
| AvatarSynergySelect                           | Enables/disables avatar synergies from the specified avatar on your current avatar                                                     |
| AwardXp                                       | Grants specified amount of XP to your current avatar                                                                                   |
| BadgeList                                     | Lists all badges on the current account.                                                                                               |
| BlockerEntityVisToggle                        | Enable/disable invisible blocker entity debug visualization                                                                            |
| BlueprintHistory                              | Display the replacement history for a blueprint guid                                                                                   |
| BlueprintList                                 | Lists blueprints that match the search pattern                                                                                         |
| BotAIToggle                                   | Toggle the bot AI                                                                                                                      |
| BotGodMode                                    | Makes the current Avatar invulnerable but keeps ability cooldowns.                                                                     |
| Bug                                           | Submits a bug description                                                                                                              |
| CameraLoadSettings                            | Load a camera setting prototype                                                                                                        |
| CatalogList                                   | Lists items in the catalog.                                                                                                            |
| CellDebug                                     |                                                                                                                                        |
| CellSetNextCell                               | Change the player into the next cell in a cellset for testing                                                                          |
| CellSetTest                                   | Change the player into a cellset for testing                                                                                           |
| CellTest                                      | Change the player into a cell for testing                                                                                              |
| ChapterList                                   | Shows the status of all chapters                                                                                                       |
| ChapterLock                                   | Locks a given chapter                                                                                                                  |
| ChapterLockAll                                | Locks all chapters                                                                                                                     |
| ChapterUnlock                                 | Unlocks a given chapter                                                                                                                |
| ChapterUnlockAll                              | Unlocks all chapters                                                                                                                   |
| ClientDownloaderSet                           | Set the client downloader as returned by the static function in ClientApp                                                              |
| ClusterGamesGet                               | Get a report of all the live games on the server *cluster*                                                                             |
| ClusterPlayerStateLifetime                    | Change the lifetime player state is kept on the client manager after logout.  Note does not affect already logged out players.         |
| ClusterRegionAccessChange                     | Change the access of the region you are in.                                                                                            |
| ClusterRegionCloseLifetime                    | Change the lifetime of regions that can soft close                                                                                     |
| ClusterRegionEmptyLifetime                    | Change the lifetime of regions that can shutdown when empty                                                                            |
| ClusterRegionMaxPlayers                       | Change the max players allowed for *ALL* regions.                                                                                      |
| ClusterRegionsGet                             | Get a report of all the live games on the server *cluster*                                                                             |
| ClusterServersGet                             | Get a report of all the live games on the server *cluster*                                                                             |
| ClusterWorldViewGet                           | Get a report of the world view for the specified player from the client manager                                                        |
| ClusterWorldViewShrinkToCurrent               | Reduce my world view to only the current region that I'm in                                                                            |
| CohortClear                                   | Clears the given experiment's cohort for metrics                                                                                       |
| CohortSet                                     | Forces a given experiment/cohort for metrics                                                                                           |
| CombatLevel                                   | Sets the effective level in combat of your current Avatar                                                                              |
| CombatLogToggle                               | Toggles the logging of combat details to chat                                                                                          |
| CommandBatch                                  | Run a series of console commands separated by a semicolon (                                                                            |
| CommunityAddMember                            | Add a player to a community circle                                                                                                     |
| CommunityListClient                           | List client community                                                                                                                  |
| CommunityListServer                           | List server community                                                                                                                  |
| CommunityRemoveMember                         | Remove a player from a community circle                                                                                                |
| ConditionsRemoveAll                           | Removes all conditions from the local player's current avatar (including persisted ones)                                               |
| ControlsPreferLastClicked                     | Toggles whether when entities overlap underneath the cursor, the last clicked on will be chosen                                        |
| Craft                                         | Attempts to craft the recipe with the given ID using the ingredients with the given ID's                                               |
| CraftGetResults                               | Moves any items from your crafting results inventory to your general inventory                                                         |
| CrashClient                                   | Force client crash                                                                                                                     |
| CrashServer                                   | Force server crash                                                                                                                     |
| CreditsSet                                    | Set the amount of money your player has to the specified amount.                                                                       |
| CritChanceSet                                 | Set the Crit and SuperCrit chance. Default: Resets Crit and SuperCrit chance.                                                          |
| CriticalThreadTimeoutServer                   | Timeout the game thread on the server.                                                                                                 |
| CurrencyCapsClient                            | Prints all the player's currency caps on the client.                                                                                   |
| CurrencyItemsConvertPrintLog                  | Forces a print of the currency conversion log                                                                                          |
| CurrencyItemsConvertTest                      | Converts Items to Currency                                                                                                             |
| CurrencyItemsConvertToggle                    | Turns on/off conversion of Currency Items to Currency properties                                                                       |
| CurrencySet                                   | Set the amount of the specified currency your player has to the specified amount.                                                      |
| DamageToggle                                  | Toggles damage for all entities ON/OFF                                                                                                 |
| DataMineReports                               | Display a report of game instances including how many players are currently online                                                     |
| DataValidateDisplay                           | Display the list of client data validation errors that have occurred                                                                   |
| DataValidatePrototype                         | Run data validation on a prototype                                                                                                     |
| DeathLimitsDisable                            | Disables any death limit metastates in the current region's metagames                                                                  |
| DeathLimitSet                                 | Sets the numbers of deaths that have happened.                                                                                         |
| DeathPenaltyToggle                            | Toggles the penalties for dieing ON/OFF                                                                                                |
| DebugAvatarDestroy                            | Directly destroys your current avatar, used for debugging only                                                                         |
| DebugHUDSetEntity                             | Sets the current entity for the debug HUD.                                                                                             |
| DebugHUDSetEntityToPlayer                     | Sets the local player as the entity for the debug HUD.                                                                                 |
| Die                                           | Kills the current avatar                                                                                                               |
| DifficultyIndexSet                            | Set the region difficulty index to the specified value.                                                                                |
| DifficultySet                                 | Set the region difficulty to the specified value.                                                                                      |
| DisplayNameToggle                             | Swaps Display Name for Prototype Names                                                                                                 |
| DodgeChanceSet                                | Set the dodge change. Default: Resets dodge chance.                                                                                    |
| DPSHUDResetAll                                | Resets the entire DPS hud. Resets the Session time for SESSON DPS                                                                      |
| DPSHUDResetSingle                             | Resets a single power for the DPS hud, Removing its data from the DPS calculation.                                                     |
| Duel                                          | Invite a player to join a player in a Duel!                                                                                            |
| DuelAccept                                    | Accept a pending invite to a Duel!                                                                                                     |
| DuelCancel                                    | Cancel a pending invite or cancel your invite to a Duel!                                                                               |
| EnableSpecializationPower                     | Enables or disables a specialization power                                                                                             |
| EntityBoostCreate                             | Create the entity with the given prototype (w/ optional modifier)                                                                      |
| EntityCreate                                  | Create the entity with the given prototype (optional # of them to create)                                                              |
| EntityCreateShortcutOverride                  | Set the entity that is created via Ctrl-Shift-H                                                                                        |
| EntityDebug                                   | Display Entities that have hopped off the navi mesh                                                                                    |
| EntityInWorldCount                            | Outputs list of all entities in world and how many of each type.                                                                       |
| EntityKill                                    | Kill the entity with the given ID. DOES NOT invoke full combat code path.                                                              |
| EntityKillByRef                               | Kill the entity with the given data ref. DOES NOT invoke full combat code path.                                                        |
| EntityListByProximity                         | Displays a list of WorldEntities near the player, Default = 1000.f                                                                     |
| EntityListClient                              | Display a list of all the entities on the client                                                                                       |
| EntityListServer                              | Display a list of all the entities on the server                                                                                       |
| EntitySetState                                | Sets the State of the Entity                                                                                                           |
| EntityTrackerReport                           | Outputs a list of all tracked objects in the current player's region.                                                                  |
| EnvironmentMapGuess                           | Overrides the current environment map with a best guess                                                                                |
| EnvironmentMapSet                             | Overrides the current environment map                                                                                                  |
| EnvironmentMapSetPackage                      | Overrides the current environment map                                                                                                  |
| EquipRestrictionsToggle                       | Toggles item equip restrictions. Use with caution.                                                                                     |
| EvalDebug                                     | Runs Eval Practice and reports value                                                                                                   |
| EvalUnitTest                                  | Runs Eval Unit Test                                                                                                                    |
| FactionSet                                    | Set your player's Faction for Pvp                                                                                                      |
| FidgetAnimsToggle                             | Toggles whether characters that support idle fidget animations will play them                                                          |
| FloatingNumbersToggle                         | Toggles forced display of floating damage numbers over entities                                                                        |
| GameIdGet                                     | Get the id of the current game on the server                                                                                           |
| GetMocoVolume                                 | Gets the volume of the motion comics.                                                                                                  |
| GetMusicVolume                                | Gets the music volume.                                                                                                                 |
| GetSfxVolume                                  | Gets the volume of sfx/ui.                                                                                                             |
| GetVoiceoverVolume                            | Gets the volume of voiceover.                                                                                                          |
| GetVolumeLevels                               | Displays all the volume levels                                                                                                         |
| GISGameCrash                                  | Crash a server game by id                                                                                                              |
| GISGameListGet                                | Get a list of game ids running on the current GIS                                                                                      |
| GlobalGameEventDonate                         | Donates count of items for a global game event.                                                                                        |
| GodMode                                       | Makes the current Avatar invulnerable and able to use powers as frequently as desired.                                                 |
| GracePeriod                                   | Set the grace period for players.                                                                                                      |
| GuildCreate                                   | Form a new guild                                                                                                                       |
| GuildDemote                                   | Demotes officers to members, kicks members from the guild                                                                              |
| GuildEntityClient                             | Print to the console the guild information stored on the entity.                                                                       |
| GuildEntityServer                             | Print to the console the guild information stored on the entity.                                                                       |
| GuildInviteNotify                             | Test the notification system for guild invites                                                                                         |
| GuildInvitePlayer                             | Invite another player to your guild                                                                                                    |
| GuildLeader                                   | Select a new guild leader, can only be done by the current leader.                                                                     |
| GuildLeave                                    | Leave your current guild                                                                                                               |
| GuildListAllClient                            | Print to the console all guilds the client has knowledge of.                                                                           |
| GuildListAllServer                            | Print to the console all guilds the server game has knowledge of.                                                                      |
| GuildListLocalClient                          | Print to the console what your client knows about your own guild.                                                                      |
| GuildListLocalServer                          | Print to the console what the server knows about your own guild.                                                                       |
| GuildNameChange                               | Change the name of your guild                                                                                                          |
| GuildPromote                                  | Promotes members to officers                                                                                                           |
| GuildsUnlock                                  | Toggle the unlocked status of guilds for your player.                                                                                  |
| HardMode                                      | Toggles development hard mode, which buffs mobs and debuffs players                                                                    |
| HasKeyword                                    | Tests if keyword is present on entity                                                                                                  |
| HealthBarToggleAvatars                        | Toggles whether in-world health bars are shown for avatars                                                                             |
| HealthBarToggleMobs                           | Toggles whether in world health bars are shown for mobs                                                                                |
| HealthSetPct                                  | Sets the HP on a target entity id                                                                                                      |
| Help                                          | List all commands and their descriptions, optionally takes name of a specific command for more info                                    |
| HotloadToggle                                 | Toggles hot-loading of game db data on both the client and the server                                                                  |
| HotloadToggleClient                           | Toggles hot-loading of game db data on the CLIENT                                                                                      |
| HotloadToggleServer                           | Toggles hot-loading of game db data on the SERVER                                                                                      |
| HotspotBoundsDebug                            | Toggles debug visualization of hotspot bounds                                                                                          |
| HotspotForceUpdate                            | Forces the specified hotspot to immediately update its powers.                                                                         |
| HotspotToggle                                 | Toggles hotspot ticking.                                                                                                               |
| InstanceReset                                 |                                                                                                                                        |
| InteractRegion                                | Reports the interaction options for a particular region                                                                                |
| InteractReport                                | Reports the interaction options for a particular entity                                                                                |
| InventoryClear                                | Clears your general inventory of all items                                                                                             |
| InventoryList                                 | Lists all the items in all the inventories of the player and current active avatar                                                     |
| InventoryUnlock                               | Attempts to unlocks the specified inventory on the player                                                                              |
| ItemAffixLevelUp                              | Increase your currently equipped Legendary item's affix-level                                                                          |
| ItemCreate                                    | Create an item and drop it on the ground                                                                                               |
| ItemGive                                      | Create an item and add it to your inventory                                                                                            |
| ItemGiveDestroyAndAutoEquip                   | Create an item and equip it destroying current item in destination location                                                            |
| ItemMaxDrop                                   | Set the max drop item limit for all players                                                                                            |
| ItemMove                                      | Move an item in your inventory                                                                                                         |
| ItemSplitStack                                | Split a single item off from an item stack in your inventory (main inventory only)                                                     |
| ItemStackCreate                               | Create a stack of items and drop them on the ground                                                                                    |
| ItemStackGive                                 | Create a stack of items and add them to your inventory                                                                                 |
| ItemTrash                                     | Destroy an item in your inventory                                                                                                      |
| ItemUse                                       | Attempt a use interaction on the item with the given entity ID in this player's inventory                                              |
| KillAllSummons                                | Kill all entities in your current avatar's summoned-entity inventory                                                                   |
| KillAndRemoveControlledAgents                 | Kills the controlled agents and removes them from the current avatars inventory                                                        |
| KillControlledAgents                          | Kills the controlled agents in the current avatars inventory                                                                           |
| KismetSeqPlay                                 | Play a Kismet Sequence                                                                                                                 |
| KismetSeqState                                | Fire a Kismet Sequence Event                                                                                                           |
| LagServer                                     | Lag the server randomly                                                                                                                |
| LatencyBufferDelay                            | Set the latency buffer delay time                                                                                                      |
| LatencySet                                    | Set the latency simulator round trip time                                                                                              |
| LegendaryMarksSet                             | Set the number of legendary marks your player has to the specified amount.                                                             |
| LegendaryMissionShare                         | Shares your current legendary mission with a party member or the entire party                                                          |
| LevelUp                                       | Levels your current Avatar                                                                                                             |
| LevelUpAll                                    | Levels all your Avatars                                                                                                                |
| LimitedEditionItemGive                        | Create a limited edition item and add it to your inventory                                                                             |
| LineHitReportClient                           | Generate a report containing instrumentation information                                                                               |
| LineHitReportServer                           | Generate a report containing instrumentation information                                                                               |
| LineOfSightTest                               |                                                                                                                                        |
| LiveTuningVarSet                              | Set the live tuning var with the given enum to the given value                                                                         |
| LoadingScreenStart                            | Displays the specified loading screen                                                                                                  |
| LoadingScreenStop                             | Hide the current loading screen                                                                                                        |
| LocaleGetResolver                             | Returns current string tag resolution method.                                                                                          |
| LocaleList                                    | Lists the available locales                                                                                                            |
| LocaleListMods                                | Lists formatting modifiers available for string construction                                                                           |
| LocaleListTokens                              | Lists tokens available for string construction by system                                                                               |
| LocaleNextResolver                            | Cycles current string tag resolution method.                                                                                           |
| LocaleSet                                     | Sets the current locale                                                                                                                |
| Localize                                      | Print the translated text given a 'Translation ref' label starting at the translation data root for the current locale                 |
| LocomotionSync                                | Toggles experimental locomotion sync mode                                                                                              |
| LocomotorHeightSweepToggle                    | Toggles the height sweep for flying                                                                                                    |
| LogEntityReport                               | Logs an entity report.                                                                                                                 |
| LogEventReport                                | Logs an event report.                                                                                                                  |
| LoggingFlagClearClient                        | Stop logging on the given log channel                                                                                                  |
| LoggingFlagClearServer                        | Stop logging on the given log channel                                                                                                  |
| LoggingFlagSetClient                          | Start logging on the given log channel                                                                                                 |
| LoggingFlagSetServer                          | Start logging on the given log channel                                                                                                 |
| LoggingTraceLevelClient                       | Sets the logging trace level to the given value                                                                                        |
| LoggingTraceLevelServer                       | Sets the logging trace level to the given value                                                                                        |
| LogHotspotReport                              | Logs a hotspot report.                                                                                                                 |
| LootCooldownCurrentTime                       | Resets the current time used by loot cooldown to real time.                                                                            |
| LootCooldownCurrentTimeSet                    | The current time used when computing the roll over time by loot.                                                                       |
| LootCooldownReset                             | Resets all loot cooldown timers on the current player and avatar.                                                                      |
| LootCooldownToggle                            | If false loot generation will skip the check for per-entity cooldown on loot drops                                                     |
| LootDebugToggle                               | Turns on/off debug output for loot tables. Look for verifies when LootTable Roll fails.                                                |
| LootDropDebugDraw                             | Toggles loot debug lines on & off                                                                                                      |
| LootDropToggle                                | Toggles loot dropping on and off                                                                                                       |
| LootGive                                      | Gives loot directly to the player                                                                                                      |
| LootRollOverSet                               | Sets all loot cap roll over times to the next rollover time minus N weeks.                                                             |
| LootSpawn                                     | Spawns loot from the given loot table                                                                                                  |
| LootStatus                                    | Shows the current Cooldowns and Caps running on the player and avatar.                                                                 |
| LootSummary                                   | Test roll on the given loot table N times and print a result summary                                                                   |
| LootSummaryEntity                             | Test roll on the given entity N times and print a result summary                                                                       |
| LootSummaryItem                               | Test roll on the given item N times and print a result summary                                                                         |
| LootTest                                      | Test roll on the given loot table N times with detailed results                                                                        |
| LowPopulationRegions                          | Prefer low population regions player option.                                                                                           |
| MapAlpha                                      | Set the minimap Alpha                                                                                                                  |
| MapColorBlocked                               | Set the minimap blocked color                                                                                                          |
| MapColorBlockedEdge                           | Set the minimap blocked edge color                                                                                                     |
| MapColorFloor                                 | Set the minimap floor color                                                                                                            |
| MapEnable                                     | Enable / Disable the minimap                                                                                                           |
| MapInfoGet                                    |                                                                                                                                        |
| MapRevealAll                                  | Reveal the entire region map to the client                                                                                             |
| MapRevealRadius                               | Set the minimap reveal radius                                                                                                          |
| MapToggleFillerCell                           | Toggle Minimap Filler Cell Visibility                                                                                                  |
| MapToggleMainCell                             | Toggle Minimap Main Cell Visibility                                                                                                    |
| MapTogglePOIMode                              | Toggle Minimap POI Render Mode                                                                                                         |
| MapToggleSmallMapProj                         | Toggle Small Minimap Projection Mode                                                                                                   |
| MapZoom                                       | Set the minimap zoom factor                                                                                                            |
| MemoryReportClient                            | Generate a csv file in the server directory of all current memory in use                                                               |
| MemoryReportServer                            | Generate a csv file in the server directory of all current memory in use                                                               |
| MetaGameDifficultyPerSec                      | Set DifficultyRate to value                                                                                                            |
| MetaGameDifficultySet                         | Set Difficulty to value                                                                                                                |
| MetaGameModeSet                               | Advance the meta game mode to the passed in value                                                                                      |
| MetaGameProgressSet                           | Set Progress to the passed in value                                                                                                    |
| MetaGameStateApply                            | Apply Specified State                                                                                                                  |
| MetaGameStateRemove                           | Remove Specified State                                                                                                                 |
| MetaGameStatesActive                          | List of states currently Active                                                                                                        |
| MetaGameWaveCount                             | Force a wave count                                                                                                                     |
| MetaGameWaveForce                             | Force a state to activate                                                                                                              |
| MetaStateWaveCount                            | Force a wave on a particular MetaStateWaveInstance to the specified count                                                              |
| MetaStateWaveForce                            | Force a wave on a particular MetaStateWaveInstance to the specified state                                                              |
| MissileSocketSpawnToggle                      | Toggles whether missiles (skillshots) spawn at a specified mesh socket or at the game system position                                  |
| MissionActivate                               | Activates a given mission by prototype name.                                                                                           |
| MissionActivateAll                            | Activates all missions for your player                                                                                                 |
| MissionActivateLegendary                      | Forces a given legendary mission to activate.                                                                                          |
| MissionAdvance                                | Advances a given mission by prototype name.                                                                                            |
| MissionArrowEntitiesClient                    | Print a list of all of the entities that should be displaying arrows.                                                                  |
| MissionArrowEntitiesServer                    | Print a list of all of the entities that should be displaying arrows.                                                                  |
| MissionEntityCreate                           | Create a mission entity with the given prototype                                                                                       |
| MissionItemCreate                             | Create a mission item and drop it on the ground                                                                                        |
| MissionItemGive                               | Create a mission item and add it to your inventory                                                                                     |
| MissionMarkerReport                           | Shows information about population marker usage by missions                                                                            |
| MissionObjectiveSetState                      | Sets a mission objective state by prototype name and objective index                                                                   |
| MissionProgressionReset                       | Resets/clears any of the player's MetaStateMissionProgress saved state                                                                 |
| MissionReset                                  | Resets a given mission by prototype name.                                                                                              |
| MissionResetAll                               | Resets all missions.                                                                                                                   |
| MissionSetState                               | Sets the state of a mission by prototype name                                                                                          |
| ModTypeReset                                  | Resets all skill ranks to zero.                                                                                                        |
| MotionBlurToggle                              | Enable/Disable the motion blur effect                                                                                                  |
| MouseHitShow                                  | Toggles debug view for mouse hit collision volumes                                                                                     |
| MouseStopOnRelease                            | Toggles whether or not you immediately stop when releasing the mouse button when moving or path to the mouse cursor                    |
| MoviePlay                                     | Trigger a movie to play from the server.                                                                                               |
| MusicDisable                                  | Disables music playing                                                                                                                 |
| MusicEnable                                   | Enables music playing                                                                                                                  |
| MusicInfo                                     | Display information about the current music track playing                                                                              |
| MusicPlay                                     | Play music track                                                                                                                       |
| MusicStop                                     | Stop current music                                                                                                                     |
| MusicToggle                                   | Toggles music on/off (unmute/mute).                                                                                                    |
| NaviDebug                                     |                                                                                                                                        |
| NaviHeightSweepTest                           | Toggles the the test mode for height sweep                                                                                             |
| NaviPatchReport                               | Display a report about the complexity of all navi patches found in the game data                                                       |
| NaviRegen                                     | Regenerate Navigation                                                                                                                  |
| NaviShowPaths                                 | Toggles path debug visualization                                                                                                       |
| NaviVerifyIntegrityClient                     |                                                                                                                                        |
| NaviVerifyIntegrityServer                     |                                                                                                                                        |
| NetMsgSentGameClient                          | Display lifetime tracking of message sent to a remote server game                                                                      |
| NetMsgSentGameGIS                             | Display lifetime tracking of message to remote players across *all* games on this GIS                                                  |
| NetMsgSentGameServer                          | Display lifetime tracking of message to remote players                                                                                 |
| NetMsgTracingToggle                           | Toggles tracing of high-volume network messages.                                                                                       |
| NoAffixVarianceToggle                         | Toggles whether affix property values are rolled in a range, or just pegged to the max                                                 |
| NoLootDropVarianceToggle                      | Toggles whether variances should not be computed for loot dropping                                                                     |
| NoVarianceToggle                              | Toggles whether variances should not be computed (Damage, DamageOverTime, Healing, and Endurance)                                      |
| NumNearbyPlayers                              | No Args: Print number of nearby players.                                                                                               |
| ObjectiveGraphClient                          | Prints the ObjectiveGraph on client.                                                                                                   |
| ObjectiveGraphDebug                           | Renders ObjectiveGraph in game.                                                                                                        |
| ObjectiveGraphMode                            | Set the mode of the ObjectiveGraph 0, 1, 2                                                                                             |
| ObjectiveGraphServer                          | Prints the ObjectiveGraph on server.                                                                                                   |
| OctreeToggle                                  | Toggles the octree visualization on/off                                                                                                |
| OmegaBonusPointsChange                        | Change your player's omega gem points                                                                                                  |
| OmegaBonusRespec                              | Respec a specific omega bonus                                                                                                          |
| OmegaBonusRespecAll                           | Respec all OmegaBonuses                                                                                                                |
| OmegaBonusSelect                              | Select an Omega Bonus using OmegaBonusPoints                                                                                           |
| OmegaBonusUnlock                              | Unlock an Omega Bonus without requiring OmegaBonusPoints                                                                               |
| OneHP                                         | Sets the health of entity id to 1                                                                                                      |
| Orient                                        |                                                                                                                                        |
| PAMHim                                        | Spawns a new target attached to P.A.M.                                                                                                 |
| PAMSetHim                                     | Sets the Entity used in Him menu.                                                                                                      |
| PAMYou                                        | Sets your avatar and attached to P.A.M.                                                                                                |
| PartyAccept                                   | Accept a party invite                                                                                                                  |
| PartyBoot                                     | Boot from  party                                                                                                                       |
| PartyChangeLeader                             | Make target new party leader                                                                                                           |
| PartyDecline                                  | Decline a party invite                                                                                                                 |
| PartyFilterList                               | Lists any party filters the current player matches                                                                                     |
| PartyInfoGet                                  | List party members                                                                                                                     |
| PartyInvite                                   | Invite a player to join a player party group by player name.                                                                           |
| PartyLeave                                    | Leave a party                                                                                                                          |
| PauseCondition                                | (Un)Pauses a condition's duration timer                                                                                                |
| PauseServer                                   |                                                                                                                                        |
| PermaBuffUnlock                               | Unlocks the specified perma-buff (permanently) on the player                                                                           |
| PetTechAffixUnlock                            | Unlocks the affix at the speicified Rarity, if no rarity is specified it unlocks all affixes.                                          |
| PhysicsTest                                   |                                                                                                                                        |
| Ping                                          | Test the round-trip time for network traffic                                                                                           |
| PlayerAvatarLibraryList                       | Lists all the avatars currently in the player's library (those not on the current team)                                                |
| PlayerCountOverride                           |                                                                                                                                        |
| PlayerReset                                   | Reset player and avatars to default new player state                                                                                   |
| PlayerSave                                    | Force an immediate and complete save of the player                                                                                     |
| PlayerTeleport                                | Teleport to the specified player                                                                                                       |
| PlayerTradeAddItem                            | Add item from general inventory to trade inventory.                                                                                    |
| PlayerTradeCancel                             | Cancel the current trading session.                                                                                                    |
| PlayerTradeListInventory                      | List the player's trade inventory.                                                                                                     |
| PlayerTradeRemoveItem                         | Remove item from trade inventory and return it to general inventory.                                                                   |
| PlayerTradeSetConfirmFlag                     | Set the trade confirmation flag. When both players have set the flag to true, the trade executes.                                      |
| PlayerTradeStart                              | Start trading with a player.                                                                                                           |
| PlayerTradeStatus                             | Print the player's trade status.                                                                                                       |
| PopBlackoutsAtPos                             | Reports all blackouts that overlap current avatar position.                                                                            |
| PopDensity                                    | Dumps current hostile counts in cells and all relevant SpawnGroups with hostile weight                                                 |
| PopEnabled                                    | Enables/disables future random population                                                                                              |
| PopErrorReport                                | Reports the errors that have occured in Population System                                                                              |
| PopGroupByEntityId                            | Gets the spawn group attached to an EntityId.                                                                                          |
| PopGroupById                                  | Gets the spawn group by SpawnGroupId.                                                                                                  |
| PopGroupsByProximity                          | Does a search for spawn groups within a range.                                                                                         |
| PopGroupsContainingEntityRef                  | Does a reverse search for all spawn groups with that Entity PrototypeDataRef                                                           |
| PopHandles                                    | Population Memory State                                                                                                                |
| PopHandlesContainingEntityRef                 | Population Handles that contain a particular entity ref.                                                                               |
| PopMarkerQuery                                | Population Marker State                                                                                                                |
| PopSetCrowdSupression                         | Reduces the chances to spawn mobs in a radius around existing mobs                                                                     |
| PopSetHeatBleed                               | Adjusts the Heat Map Bleed of neighbooring tiles on spawn. (0.0f-1.0f)                                                                 |
| PopSetHeatDensity                             | Adjusts the Heat Map to a new density. (0.0f-0.5f)                                                                                     |
| PopSetHeatLevelTick                           | Adjusts the Heat Map Leveling tick rate.                                                                                               |
| PopSetHeatPoolTick                            | Adjusts the Heat Map Distribute tick rate.                                                                                             |
| PopSpawnMapAudit                              | Reports an Audit of all spawn map heat info.                                                                                           |
| PopSpawnMapReset                              | Population SpawnMap Reset                                                                                                              |
| PopSpawnMapVis                                | Population SpawnMap Visualize                                                                                                          |
| PopSummary                                    | Abreviated Population Memory State                                                                                                     |
| PowerForceFailToggle                          | Toggles the ability to force the server to fail power activations and send them to the client                                          |
| PowersAllowInAllRegions                       | Toggles whether some regions can restrict power use                                                                                    |
| PowersAllowTeleportToRegion                   | Toggles whether the TeleportToRegion power event action will function                                                                  |
| PowersAvatarFailReportToggle                  | Toggles whether we print why the player failed.                                                                                        |
| PowersCooldownReset                           | Clears all cooldowns active on powers, but doesn't disable future ones.                                                                |
| PowersCooldownToggle                          | Toggles whether you will be able to activate powers on cooldown or not                                                                 |
| PowersExportAll                               | Exports all power progression table powers to a file for all Avatars.                                                                  |
| PowersForceActivate                           | Forces the power to cast in the direction the player faces                                                                             |
| PowersListAssigned                            |                                                                                                                                        |
| PowersListAssignedServer                      |                                                                                                                                        |
| PowersQueueActivate                           | Activate all powers queue'd up by PowersQueueUp                                                                                        |
| PowersQueueRemove                             | Clear the queue queued up by PowersQueueUp.                                                                                            |
| PowersQueueRepeat                             | Toggles repeating the active queue of powers playing.                                                                                  |
| PowersQueueUp                                 | Queue up a power, then fire them all with PowersQueueActivate                                                                          |
| PowersRespec                                  | Resets all current avatar power point allocations and refunds the points.                                                              |
| PowersShowAreaToggle                          | Toggles power visualizations                                                                                                           |
| PowersSpecLockAll                             | Locks all powers spec slots (except the default one).                                                                                  |
| PowersSpecSelect                              | Activate the specified power spec. It must be unlocked.                                                                                |
| PowersSpecUnlock                              | Unlocks the next available powers spec slot.                                                                                           |
| PowersUltimateRank                            | Sets the current avatar's Ultimate Power to the specified rank.                                                                        |
| PowersUnlockAll                               | Unlocks all powers the current Avatar and current Team-Up can use.                                                                     |
| PrestigeModeActivate                          | Reset current avatar to Level 1 using the Prestige Mode feature.                                                                       |
| PrestigeModeClear                             | Clear the Prestige Mode state for the current avatar.                                                                                  |
| PrintPrototype                                | Prints the values of the given Prototype                                                                                               |
| PrintPrototypeClient                          | Prints the values of the given Prototype on the Client.                                                                                |
| ProcAlwaysToggle                              | Toggles whether proc chance rolls will always succeed.                                                                                 |
| ProcChanceSet                                 | Overrides the chance that the given proc effect power will proc                                                                        |
| ProfileServerFrame                            | Toggles server frame profiling.                                                                                                        |
| PropertyRemove                                | Removes the given property, resetting it back to its default value                                                                     |
| PropertyRemoveRange                           | Removes the entire range of the given property enum, restting it back to its default value                                             |
| PropertySet                                   | Sets the given property for the given entity to the given value                                                                        |
| PrototypeHistory                              | Display the replacement history for a prototype guid                                                                                   |
| PrototypeList                                 | Lists prototypes that match the search pattern                                                                                         |
| PurchaseUnlock                                | Attempts to purchase the unlock for the given avatar/teamup                                                                            |
| PvPCollisionEnable                            | Toggles player versus player collision                                                                                                 |
| RawDamageToggle                               | Toggles sending raw damage (i.e. reveal scaling by nearby players to client).                                                          |
| RedeemPromoCodeCheat                          | Redeems a promo code for current player                                                                                                |
| RegionChange                                  | Change the region the local player is in                                                                                               |
| RegionPopulationSpawn                         | Automatically cause all populations in all cells in the current players region to spawn                                                |
| RegionPrimitiveToggle                         |                                                                                                                                        |
| RegionRequestQueue                            | Request to enter a queue                                                                                                               |
| RegionRequestQueueAccept                      | Accept an available match                                                                                                              |
| RegionRequestQueueDecline                     | Decline an available match                                                                                                             |
| RegionRequestQueueForce                       | Force starts a match with players in queue of question                                                                                 |
| RegionRequestQueueInfo                        | Report Info on queue and matches                                                                                                       |
| RegionReset                                   | Reload the region with a different seed                                                                                                |
| RegionWarp                                    | Transport yourself to a specific region.                                                                                               |
| ReleaseCheckVerify                            | Checks if an avatar or team up has all the correct data set up for release                                                             |
| ReloadGamePackages                            | Reloads the Game Packages (that are needed given the AOI                                                                               |
| ResetVolumeLevels                             | Resets all audio volume levels to max.                                                                                                 |
| Respawning                                    |                                                                                                                                        |
| RosterLoadAvatar                              | Loads the given roster avatar from the database                                                                                        |
| RunestonesSet                                 | Set the number of runestones your player has to the specified amount.                                                                  |
| ScreenArrowDebugTextToggle                    | Toggles debug text on screen arrows                                                                                                    |
| ServerNotification                            | Sends a server message to all clients                                                                                                  |
| SetAudioBudget                                | Sets the audio budget (in MB).                                                                                                         |
| SetMocoVolume                                 | Sets the volume of the motion comics.                                                                                                  |
| SetMusicVolume                                | Sets the music volume.                                                                                                                 |
| SetSfxVolume                                  | Sets the volume of sfx/ui.                                                                                                             |
| SetVoiceoverVolume                            | Sets the volume of voiceover.                                                                                                          |
| SfxToggle                                     | Toggles sound effects on/off.                                                                                                          |
| ShowServerPosition                            | Toggles showing server's position for entities                                                                                         |
| SiteCommunityBatchBroadcastCountLimit         | Set the community batch broadcast limit                                                                                                |
| SiteCommunityBatchBroadcastMaxPerMessage      | Set the community max batch broadcasts per message                                                                                     |
| SiteCommunityBatchBroadcastTimeLimitInSeconds | Set the community batch broadcast time limit in seconds                                                                                |
| SiteCommunityMultithreadedBroadcasts          | Enable or disable multi-threaded broadcast sends                                                                                       |
| SiteGameInfo                                  | Display information for a game instance                                                                                                |
| SiteLoginQueueStatus                          | Display login queue status                                                                                                             |
| SiteMatchQueueAvailability                    | Set queue availability                                                                                                                 |
| SiteOpenMissionQuery                          | Lists the state of open missions in the current region                                                                                 |
| SitePlayerInfo                                | Display information for a player                                                                                                       |
| SitePlayerList                                | Display list of players (including online and offline)                                                                                 |
| SitePlayerListOffline                         | Display list of offline players that we still have state for in the site                                                               |
| SitePlayerListOnline                          | Display list of only online players                                                                                                    |
| SiteRegionChange                              | Transport yourself to a specific region instance by id.  Player limits and region age are bypassed.                                    |
| SiteRegionInfo                                | Display information for a region instance                                                                                              |
| SiteRegionList                                | Get all regions at the site sorted by player population                                                                                |
| SiteRegionShutdown                            | Kicks all players from region                                                                                                          |
| SiteServerInfo                                | Display information for a Game Instance Server                                                                                         |
| SiteServerList                                | List all Game Instances Servers at the host site                                                                                       |
| SiteServerListByLoad                          | List all Game Instances Servers at the host site sorted by load                                                                        |
| SiteSetLoadTestMultiplier                     | Set a multiplier the target server will use when reporting load                                                                        |
| SkillsReport                                  | Lists your skill ranks.                                                                                                                |
| SkillsSet                                     | Sets a skill to a value.                                                                                                               |
| SleepClient                                   | Sleeps the client                                                                                                                      |
| SleepServer                                   | Sleeps the server                                                                                                                      |
| Slomo                                         | Sets the time dialation multiplier                                                                                                     |
| Smite                                         | Kills entity by id. Triggers all normal combat side-effects. Some targets may be invulnerable.                                         |
| SmiteAOE                                      | Kills all hostiles around the avatar within a given radius. Triggers all normal combat side-effects. Some targets may be invulnerable. |
| SpawnerEnable                                 | Enable or Disable a spawner                                                                                                            |
| SpawnerPulse                                  | Pulse a spawner                                                                                                                        |
| StealPower                                    | Steal a power that is in this avatar's StealablePowersAllowed list. The Power will be slotted in the stealable power ref's place       |
| StealPowerWithValidation                      | Steal a power that is in this avatar's StealablePowersAllowed list. The Power will be slotted in the s                                 |
| StolenPowerForget                             | Unassign a stolen power.                                                                                                               |
| StoreDeliveryBoxListInventory                 | List the player's store delivery box inventory.                                                                                        |
| StoryNotificationVOPlay                       |                                                                                                                                        |
| TargetShadersToggle                           | Enable/Disable all hit and mouse-over shader effects                                                                                   |
| TeamUpDestroy                                 | Destroy a Team Up, removing it from the TeamUp Library                                                                                 |
| TeamUpDismiss                                 | Dismiss the currently selected Team Up, removing it from the world.                                                                    |
| TeamUpLevel                                   | Sets the character level of your current TeamUp                                                                                        |
| TeamUpLevelAll                                | Sets the character level of ALL TeamUps                                                                                                |
| TeamUpPowerSelect                             | Select the specified power in the current Team Up's power progression.                                                                 |
| TeamUpRespec                                  | Forget all power choices of your the current TeamUp                                                                                    |
| TeamUpSelect                                  | Select an unlocked Team Up to be the currently active one for the current Avatar.                                                      |
| TeamUpStyle                                   | Sets the style of your current TeamUp (assigns a basic passive power)                                                                  |
| TeamUpSummon                                  | Summon the currently selected Team Up, spawning it in the world.                                                                       |
| TeamUpUnlock                                  | Unlock a Team Up, creating it in the TeamUp Library                                                                                    |
| TeamUpUnlockAll                               | Unlock ALL Team Ups, creating them in the TeamUp Library                                                                               |
| TenacityToggle                                | Toggles tenacity computation.                                                                                                          |
| TestItemLinkChat                              | Unit test for item linking say                                                                                                         |
| TestItemLinkTell                              | Unit test for item linking tell                                                                                                        |
| TestNaviCreateDoor                            |                                                                                                                                        |
| TestNaviRemoveDoor                            |                                                                                                                                        |
| TestNaviSweep                                 |                                                                                                                                        |
| TestTooltipMod                                |                                                                                                                                        |
| TestTooltipPower                              |                                                                                                                                        |
| ToggleMobLosVisibility                        | Toggles whether mobs get hidden when out of line-of-sight                                                                              |
| TooltipHiddenTextToggle                       | Toggles showing tooltip text that is marked as #hidden# (for advanced tooltips).                                                       |
| TutorialDisableTips                           | Disables tutorial tips                                                                                                                 |
| TutorialEnableTips                            | Enables tutorial tips                                                                                                                  |
| TutorialResetTips                             | Resets all tutorial tips                                                                                                               |
| TutorialShowTip                               | Shows a tutorial tip                                                                                                                   |
| UIAudioToggle                                 | Toggles UI audio on/off.                                                                                                               |
| UINotificationToggle                          | Toggles UI notifications on/off                                                                                                        |
| UIPlayerHUDConsider                           | Calls PropertyHUD::Consider on an EntityId for debugging.                                                                              |
| UIPlayerHUDInterest                           | Dumps list of tracked UI entities.                                                                                                     |
| UnhandledExceptionClient                      | Fire an unhandled exception on the client                                                                                              |
| UnhandledExceptionServer                      | Fire an unhandled exception on the server                                                                                              |
| UniqueBoxGive                                 | Create a box that, when used, grants uniques and cosmics, and add it to your inventory.                                                |
| UnloadGamePackages                            | Attempts to Unload all Packages                                                                                                        |
| UnlockEmotePower                              | Unlocks an emote power for a specific avatar                                                                                           |
| VanityTitleLockAll                            | Re-lock all Vanity Titles for the player                                                                                               |
| VanityTitleSelect                             | Select a Vanity Title for current avatar from those the player has unlocked                                                            |
| VanityTitleUnlock                             | Unlock a Vanity Title for the player                                                                                                   |
| VendorDonateItem                              | Donates to the current open vendor the first item found in your player inv or the item with the specified EntityId if one is given     |
| VendorEnergySet                               | Sets your vendor energy level in the given VendorType to 100%, or to the specified [0,1] pct if given                                  |
| VendorLevelUp                                 | Increases your vendor level in the given VendorType by 1 or to the specified level if given                                            |
| VendorRefresh                                 | Requests refresh of the inventory of the current open vendor entity                                                                    |
| VendorStats                                   | Prints your current vendor level, XP, and current energy pct for each VendorType                                                       |
| VerifyClient                                  | Fires a verify failure on the client                                                                                                   |
| VerifyServer                                  | Fire a failed verify on the server                                                                                                     |
| VerifyUploadSet                               | Set which verifies are shown on the client. Use the format: +ALL,-TEST                                                                 |
| Version                                       | Displays the game version                                                                                                              |
| WalletBalance                                 | Get balance in the wallet                                                                                                              |
| WalletBuyGift                                 | Buy gift from the catalog.                                                                                                             |
| WalletBuyItem                                 | Buy an item that was in the catalog.                                                                                                   |
| WarpArea                                      | Warp to random location inside of specified area.  If multiple areas exist, the first one will be chosen.                              |
| WarpAreaNext                                  | Warp to the next area in a region                                                                                                      |
| WarpAreaPrev                                  | Warp to the next area in a region                                                                                                      |
| WarpCell                                      | Warp to random location inside of specified cell.  If multiple areas exist, the first one will be chosen.                              |
| WarpCellNext                                  | Warp to the next cell in a region                                                                                                      |
| WarpCellPrev                                  | Warp to the next cell in a region                                                                                                      |
| WarpEntityId                                  | Warp next to the entity specified                                                                                                      |
| WarpEntityNext                                | Warp to the next closest entity moving forward through the set                                                                         |
| WarpEntityPrev                                | Warp to the next closest entity moving reverse through the set                                                                         |
| WarpMissionEntityNext                         | Warp to the next closest entity relevant to the given mission                                                                          |
| WarpMissionEntityPrev                         | Warp to the previous closest entity relevant to the given mission                                                                      |
| WarpPosition                                  | Warp to the specified position                                                                                                         |
| WarpTransitionNext                            | Warp to the next transition entity in a region                                                                                         |
| WarpTransitionPrev                            | Warp to the next transition entity in a region                                                                                         |
| WarpWaypoint                                  | Warps to a given waypoint                                                                                                              |
| WaypointList                                  | Shows the status of all waypoints                                                                                                      |
| WaypointLock                                  | Locks a given waypoint                                                                                                                 |
| WaypointLockAll                               | Locks all waypoints                                                                                                                    |
| WaypointUnlock                                | Unlocks a given waypoint                                                                                                               |
| WaypointUnlockAll                             | Unlocks all waypoints                                                                                                                  |
| WhoRegion                                     | Display players in your current region                                                                                                 |
| WhoServer                                     | Display players in your current game instance server                                                                                   |
| XPAwardCount                                  | Displays the number of times the currently active Avatar received XP due to a mob kill                                                 |
| XPAwardCountReset                             | Resets the Kill Counter for the currently active avatar                                                                                |
| XPNumbersToggle                               | Toggles the display of floating experience numbers over entities                                                                       |
