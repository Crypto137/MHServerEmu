using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System;
using MHServerEmu.Core.System.Time;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games;
using MHServerEmu.PlayerManagement.Network;

namespace MHServerEmu.PlayerManagement.Players
{
    /// <summary>
    /// Manages <see cref="ClientSession"/> instances.
    /// </summary>
    public class SessionManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly TimeSpan PendingSessionLifespan = TimeSpan.FromSeconds(60);

        private readonly PlayerManagerService _playerManager;

        private readonly IdGenerator _idGenerator = new(IdType.Session, 0);

        private readonly Dictionary<ulong, ClientSession> _pendingSessionDict = new();
        private readonly Dictionary<ulong, ClientSession> _activeSessionDict = new();

        // This client dictionary prevents sessions from being used by multiple clients
        private readonly Dictionary<ulong, IFrontendClient> _clientDict = new();

        private CooldownTimer _updateTimer = new(TimeSpan.FromMilliseconds(1000));

        public int PendingSessionCount { get => _pendingSessionDict.Count; }
        public int ActiveSessionCount { get => _activeSessionDict.Count; }

        /// <summary>
        /// Constructs a new <see cref="SessionManager"/> instance for the provided <see cref="PlayerManagerService"/>.
        /// </summary>
        public SessionManager(PlayerManagerService playerManager)
        {
            _playerManager = playerManager;
        }

        public void Update()
        {
            if (_updateTimer.Check() == false)
                return;

            PurgeExpiredSessions();
        }

        /// <summary>
        /// Verifies the provided <see cref="LoginDataPB"/> instance and creates a new <see cref="ClientSession"/> if it's valid.
        /// <see cref="AuthStatusCode"/> indicates the outcome of validation.
        /// </summary>
        public AuthStatusCode TryCreateSessionFromLoginDataPB(LoginDataPB loginDataPB, out ClientSession session)
        {
            session = null;

            // Check client version
            if (loginDataPB.HasVersion == false)
            {
                Logger.Warn($"TryCreateSessionFromLoginDataPB(): LoginDataPB for {loginDataPB.EmailAddress} contains no version information");
                return AuthStatusCode.PatchRequired;
            }

            if (loginDataPB.Version != Game.Version)
            {
                Logger.Warn($"TryCreateSessionFromLoginDataPB(): Client version mismatch ({loginDataPB.Version} instead of {Game.Version})");

                // Fail auth if version mismatch is not allowed
                if (_playerManager.Config.AllowClientVersionMismatch == false)
                    return AuthStatusCode.PatchRequired;
            }

            // Verify credentials
            AuthStatusCode statusCode = AccountManager.TryGetAccountByLoginDataPB(loginDataPB, out DBAccount account);

            if (statusCode != AuthStatusCode.Success)
                return statusCode;

            // Validate client downloader
            ClientDownloader downloaderEnum = ClientDownloader.None;

            if (loginDataPB.HasClientDownloader)
            {
                if (Enum.TryParse(loginDataPB.ClientDownloader, out downloaderEnum) == false)
                {
                    downloaderEnum = ClientDownloader.None;
                    Logger.Warn($"TryCreateSessionFromLoginDataPB(): Invalid client downloader {loginDataPB.ClientDownloader} for {account}, defaulting to {downloaderEnum}");
                }
            }

            string locale = loginDataPB.HasLocale ? loginDataPB.Locale : "en_us";

            // Create a new session
            session = new(_idGenerator.Generate(), account, downloaderEnum, locale);
            lock (_pendingSessionDict)
                _pendingSessionDict.Add(session.Id, session);

            return statusCode;
        }

        /// <summary>
        /// Verifies the provided <see cref="ClientCredentials"/> and assigns the appropriate <see cref="ClientSession"/>
        /// to the specified <see cref="IFrontendClient"/> if they are valid. Returns <see langword="true"/> if the credentials
        /// are valid.
        /// </summary>
        public bool VerifyClientCredentials(IFrontendClient client, ClientCredentials credentials)
        {
            // Check if a pending session for these credentials exists
            ClientSession session;

            lock (_pendingSessionDict)
            {
                if (_pendingSessionDict.Remove(credentials.Sessionid, out session) == false)
                    return Logger.WarnReturn(false, $"VerifyClientCredentials(): SessionId 0x{credentials.Sessionid:X} not found");
            }

            // Verify the token if enabled
            if (_playerManager.Config.UseJsonDBManager == false)
            {
                // Try to decrypt the token (we avoid extra allocations and copying by accessing buffers directly with Unsafe.GetBuffer())
                byte[] encryptedToken = ByteString.Unsafe.GetBuffer(credentials.EncryptedToken);
                byte[] iv = ByteString.Unsafe.GetBuffer(credentials.Iv);

                if (CryptographyHelper.TryDecryptToken(encryptedToken, session.Key, iv, out byte[] decryptedToken) == false)
                    return Logger.WarnReturn(false, $"VerifyClientCredentials(): Failed to decrypt token for {session}");

                // Verify the token
                if (CryptographyHelper.VerifyToken(decryptedToken, session.Token) == false)
                    return Logger.WarnReturn(false, $"VerifyClientCredentials(): Failed to verify token for {session}");
            }

            // Assign the session to the client if the token is valid
            lock (_activeSessionDict)
            {
                // Handle the case when someone hijacks another client's credentials and attempts to log in with them while the actual client is still logged in
                if (_activeSessionDict.TryAdd(session.Id, session) == false || _clientDict.TryAdd(session.Id, client) == false)
                    return Logger.WarnReturn(false, $"VerifyClientCredentials(): A client is attempting to use {session} that is already in use");

                // Sessions cannot be reassigned
                if (client.AssignSession(session) == false)
                {
                    _activeSessionDict.Remove(session.Id);
                    _clientDict.Remove(session.Id);
                    return Logger.WarnReturn(false, $"VerifyClientCredentials(): Failed to assign {session} to a client");
                }
            }

            return true;
        }

        /// <summary>
        /// Removes the <see cref="ClientSession"/> with the specified id.
        /// </summary>
        public void RemoveActiveSession(ulong sessionId)
        {
            lock (_activeSessionDict)
            {
                if (_activeSessionDict.Remove(sessionId) == false)
                    Logger.Warn($"RemoveActiveSession(): No active session for sessionId {sessionId:X}");

                if (_clientDict.Remove(sessionId) == false)
                    Logger.Warn($"RemoveActiveSession(): No client for sessionId {sessionId:X}");
            }
        }

        /// <summary>
        /// Retrieves the <see cref="ClientSession"/> for the specified session id. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool TryGetActiveSession(ulong sessionId, out ClientSession session)
        {
            return _activeSessionDict.TryGetValue(sessionId, out session);
        }

        /// <summary>
        /// Retrieves the <see cref="FrontendClient"/> for the specified session id. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool TryGetClient(ulong sessionId, out IFrontendClient client)
        {
            return _clientDict.TryGetValue(sessionId, out client);
        }

        private void PurgeExpiredSessions()
        {
            lock (_pendingSessionDict)
            {
                if (_pendingSessionDict.Count == 0)
                    return;

                foreach (var kvp in _pendingSessionDict)
                {
                    ClientSession session = kvp.Value;

                    if (session.Length <= PendingSessionLifespan)
                        continue;

                    Logger.Warn($"Pending session expired: sessionId=0x{session.Id:X}, account=[{session.Account}]");
                    _pendingSessionDict.Remove(kvp.Key);
                }
            }
        }
    }
}
