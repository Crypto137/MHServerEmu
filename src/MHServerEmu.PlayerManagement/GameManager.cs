using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System;
using MHServerEmu.Games;
using System; // Added for Math.Max
using System.Collections.Concurrent; // Added for ConcurrentDictionary
using System.Collections.Generic; // Keep for IEnumerable/List potentially
using System.Linq; // Added for potential LINQ usage (though kept simple loop here)
using System.Threading; // Potentially useful for future async/lock patterns if needed

namespace MHServerEmu.PlayerManagement
{

    /// <remarks>
    /// TODO: GameInstanceServer (GIS) implementation details are still pending.
    /// This implementation focuses on managing local Game objects concurrently.
    /// </remarks>
    public class GameManager
    {
        // Using nameof() makes logger name resilient to class renaming
        private static readonly Logger Logger = LogManager.CreateLogger(nameof(GameManager));

        // Use ConcurrentDictionary for thread-safe operations on the game collection
        private readonly ConcurrentDictionary<ulong, Game> _games = new();
        private readonly IdGenerator _idGenerator = new(IdType.Game, 0);
        private int _targetGameInstanceCount = 0; // Default to 0, initialized means > 0
        private int _playerCountDivisor = 1;
        private volatile bool _isInitialized = false; // Flag to ensure initialization occurs
        private int _gameInstanceCapacity = 40; // Example default capacity

        /// <summary>
        /// Gets the current number of active game instances.
        /// </summary>
        public int GameCount => _games.Count; // ConcurrentDictionary.Count is thread-safe

        /// <summary>
        /// Constructs a new GameManager instance. Call InitializeGames() before use.
        /// </summary>
        public GameManager() { }

        /// <summary>
        /// Initializes the GameManager with a target number of game instances.
        /// This method should be called once before the manager is used.
        /// </summary>
        /// <param name="targetGameInstanceCount">The desired number of concurrent game instances.</param>
        /// <param name="playerCountDivisor">Divisor used for basic load balancing (higher value groups players more).</param>
        /// <param name="gameInstanceCapacity">The maximum number of players a single game instance can hold.</param>
        public void InitializeGames(int targetGameInstanceCount, int playerCountDivisor, int gameInstanceCapacity = 40)
        {
            if (_isInitialized)
            {
                Logger.Warn("GameManager is already initialized. Ignoring subsequent call.");
                return;
            }

            // Ensure at least one game instance if target > 0, otherwise allow 0.
            _targetGameInstanceCount = Math.Max(targetGameInstanceCount, 0);
            _playerCountDivisor = Math.Max(playerCountDivisor, 1); // Divisor must be at least 1
            _gameInstanceCapacity = Math.Max(1, gameInstanceCapacity); // Capacity must be at least 1

            Logger.Info($"Initializing GameManager with TargetInstances={_targetGameInstanceCount}, PlayerCountDivisor={_playerCountDivisor}, GameInstanceCapacity={_gameInstanceCapacity}");

            // Pre-populate the first game instance if target > 0
            if (_targetGameInstanceCount > 0)
            {
                CreateAndStartGame();
            }

            _isInitialized = true;
            Logger.Info($"GameManager initialized. Current active games: {GameCount}");
        }

        /// <summary>
        /// Gets the Game instance with the specified ID.
        /// </summary>
        /// <param name="id">The unique ID of the game.</param>
        /// <returns>The Game instance, or null if the ID is 0 or not found.</returns>
        public Game GetGameById(ulong id)
        {
            // ID 0 is considered a valid "not in game" state by the client.
            if (id == 0)
            {
                return null;
            }

            if (!_isInitialized)
            {
                Logger.Error("Attempted to get game by ID before GameManager was initialized.");
                return null; // Or throw InvalidOperationException
            }

            if (_games.TryGetValue(id, out Game game))
            {
                // Optional: Check if game HasBeenShutDown here? Depends on requirements.
                // If a client holds an old ID for a game that shut down, this might return it briefly.
                // RefreshGames handles cleanup, but there could be a race condition.
                return game;
            }
            else
            {
                // Log requests for non-existent game IDs. Use Warn level as it might indicate client state issues.
                Logger.Warn($"GetGameById(): Game instance with ID 0x{id:X} not found.");
                return null;
            }
        }

        /// <summary>
        /// Finds and returns an available Game instance. It will prioritize filling up the first instance
        /// before creating new ones, up to the target instance count.
        /// </summary>
        /// <returns>An available Game instance, or null if none can be provided.</returns>
        public Game GetAvailableGame()
        {
            if (!_isInitialized)
            {
                Logger.Error("Attempted to get available game before GameManager was initialized.");
                return null; // Or throw InvalidOperationException
            }

            // Ensure game instances are maintained (removes shutdown ones, adds new ones if needed)
            MaintainGameInstances();

            // Find the best candidate game based on filling the first one
            return FindBestAvailableGame();
        }

        /// <summary>
        /// Requests shutdown for all managed game instances and removes them.
        /// </summary>
        public void ShutdownAllGames()
        {
            Logger.Info($"Shutting down all game instances ({GameCount} currently)...");
            _isInitialized = false; // Mark as uninitialized during/after shutdown


            List<ulong> gameIds = _games.Keys.ToList();

            foreach (ulong gameId in gameIds)
            {

                if (_games.TryRemove(gameId, out Game gameToShutdown))
                {
                    try
                    {
                        gameToShutdown.RequestShutdown();

                    }


                    catch (Exception ex)
                    {

                        Logger.Error($"Exception while requesting shutdown for game ID 0x{gameId:X}: {ex}", LogChannels.General);
                        // Continue shutting down others
                    }
                }
            }
            _games.Clear();
            Logger.Info("All game instances requested to shut down and removed from manager.");
        }

        /// <summary>
        /// Creates a new Game instance, starts it, and adds it to the collection.
        /// </summary>
        /// <returns>The newly created and started Game instance, or null on failure.</returns>
        private Game CreateAndStartGame()
        {
            try
            {
                ulong id = _idGenerator.Generate();
                Game newGame = new Game(id);

                // TryAdd is thread-safe. If ID somehow collides (shouldn't with IdGenerator), it fails.
                if (_games.TryAdd(id, newGame))
                {
                    Logger.Debug($"Created new game instance with ID 0x{id:X}.");
                    newGame.Run(); // Start the game's execution loop/thread
                    Logger.Info($"Started game instance 0x{id:X}.");
                    return newGame;
                }
                else
                {
                    // This should ideally never happen if IdGenerator is correct.
                    Logger.Error($"Failed to add new game instance with ID 0x{id:X} to dictionary (ID collision?).");
                    // Did not add, so no need to shut down newGame object explicitly unless it holds resources.
                    return null;
                }
            }
            // Inside the catch block in CreateAndStartGame()
            catch (Exception ex)
            {
                // Put the message string first, then the exception object
                Logger.Error("Exception occurred during game creation and startup.", LogChannels.Default); // Or LogChannels.General
                return null;
            }
        }

        /// <summary>
        /// Cleans up game instances that have been shut down and creates replacements
        /// to meet the target instance count, but only after the existing ones are full.
        /// </summary>
        private void MaintainGameInstances()
        {
            // --- Cleanup Phase ---
            // Iterate safely over a snapshot of the dictionary's state at the start.
            // This avoids issues if items are added/removed concurrently by other operations.
            var currentGames = _games.ToList(); // Creates a List<KeyValuePair<ulong, Game>> snapshot
            int removedCount = 0;

            foreach (KeyValuePair<ulong, Game> kvp in currentGames)
            {
                Game game = kvp.Value;
                ulong gameId = kvp.Key;

                // Check if the game (that was present in the snapshot) is marked as shut down.
                // Also check if it's still present in the main dictionary before trying to remove,
                // in case it was already removed by another thread or ShutdownAllGames.
                if (game.HasBeenShutDown && _games.ContainsKey(gameId))
                {
                    // TryRemove ensures we only remove it if the key/value pair is still the one we expect.
                    // We use kvp here for the check.
                    if (_games.TryRemove(kvp)) // Use the KeyValuePair overload for atomic check-and-remove
                    {
                        Logger.Info($"Removed shut down game instance ID 0x{gameId:X}.");
                        removedCount++;
                    }
                    // else: Game was already removed by another thread between snapshot and TryRemove. Fine.
                }
            }
            if (removedCount > 0) Logger.Debug($"Cleanup removed {removedCount} instances.");

            // --- Replenishment Phase ---
            // Check if we need to create a new instance. We only do this if the first instance is full
            // and we haven't reached the target instance count.
            if (_games.Count < _targetGameInstanceCount)
            {
                // Find the first game instance (if any)
                var firstGame = _games.FirstOrDefault().Value;

                // If there is a first game and it's full, or if there are no games and the target is > 0, create a new one.
                if ((firstGame != null && firstGame.PlayerCount >= _gameInstanceCapacity) || (_games.Count == 0 && _targetGameInstanceCount > 0))
                {
                    Logger.Info($"Current game count ({_games.Count}) is below target ({_targetGameInstanceCount}) and the first instance is full (or no instances exist). Creating new instance...");
                    if (CreateAndStartGame() == null)
                    {
                        // Logged within CreateAndStartGame
                        Logger.Warn("Failed to create and start a new game instance during maintenance.");
                    }
                }
                else if (_games.Count == 0 && _targetGameInstanceCount > 0)
                {
                    // This case should ideally be covered by the previous condition, but adding for clarity.
                    Logger.Info("No game instances exist and target is > 0. Creating the first instance...");
                    CreateAndStartGame();
                }
                else
                {
                    Logger.Debug($"Current game count ({_games.Count}) is below target ({_targetGameInstanceCount}), but the first instance is not yet full (PlayerCount: {firstGame?.PlayerCount ?? 0}, Capacity: {_gameInstanceCapacity}). Not creating a new instance yet.");
                }
            }
        }

        /// <summary>
        /// Finds the most suitable game instance. It prioritizes the first created instance
        /// until it reaches capacity before considering others.
        /// Assumes MaintainGameInstances has already run.
        /// </summary>
        /// <returns>The Game instance considered most available, or null if none exist.</returns>
        private Game FindBestAvailableGame()
        {
            // Use the concurrent dictionary's values directly. This is a snapshot-like enumeration.
            var activeGames = _games.Values;

            if (!activeGames.Any()) // Check if the collection is empty after potential cleanup
            {
                Logger.Warn("FindBestAvailableGame(): No game instances available after maintenance.");
                return null;
            }

            // Try to find the first game instance that isn't full
            foreach (Game game in activeGames)
            {
                if (!game.HasBeenShutDown && game.PlayerCount < _gameInstanceCapacity)
                {
                    Logger.Debug($"Selected game ID 0x{game.Id:X} as best available (PlayerCount: {game.PlayerCount}, Capacity: {_gameInstanceCapacity}). Prioritizing filling this instance.");
                    return game;
                }
            }

            // If the first instance (and potentially others) are full, and we have more capacity (more target instances),
            // the MaintainGameInstances method will eventually create a new one.
            // For now, if all existing instances are full, we can return any of them as a fallback
            // so players aren't stuck. The next player will likely trigger the creation of a new instance
            // if the target count allows.

            // As a fallback, return the game with the lowest player count among the full ones
            Game bestGame = null;
            int lowestPlayerCount = int.MaxValue;

            foreach (Game game in activeGames)
            {
                if (!game.HasBeenShutDown && game.PlayerCount < lowestPlayerCount)
                {
                    lowestPlayerCount = game.PlayerCount;
                    bestGame = game;
                }
            }

            if (bestGame != null)
            {
                Logger.Debug($"All existing game instances are at or near capacity. Returning game ID 0x{bestGame.Id:X} as a fallback (PlayerCount: {bestGame.PlayerCount}, Capacity: {_gameInstanceCapacity}).");
                return bestGame;
            }
            else
            {
                Logger.Warn($"FindBestAvailableGame(): Found no suitable game instance among {activeGames.Count()} candidates (all might be shutting down or full).");
                return null;
            }
        }
    }
}