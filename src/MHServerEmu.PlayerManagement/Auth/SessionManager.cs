using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Web;
using MHServerEmu.Core.System;
using MHServerEmu.Core.System.Time;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games;
using MHServerEmu.PlayerManagement.Players;

namespace MHServerEmu.PlayerManagement.Auth
{
    /// <summary>
    /// Authenticates clients and manages <see cref="ClientSession"/> instances.
    /// </summary>
    public class SessionManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly TimeSpan PendingSessionLifespan = TimeSpan.FromSeconds(60);

        private readonly PlayerManagerService _playerManager;

        private readonly IdGenerator _idGenerator = new(IdType.Session, 0);
        private readonly WebTokenManager<ulong> _platformTicketManager = new();
        // "Platform Tickets" are tokens used to access the Add G page from the MTX store.

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
        /// Verifies the provided <see cref="LoginDataPB"/> instance, and creates a new <see cref="ClientSession"/> for it if successful.
        /// </summary>
        /// <remarks>
        /// <see cref="AuthStatusCode"/> indicates the outcome of verification, <see cref="AuthTicket"/> contains the data required for the client to proceed.
        /// </remarks>
        public AuthStatusCode TryCreateSession(LoginDataPB loginDataPB, out AuthTicket authTicket)
        {
            authTicket = AuthTicket.DefaultInstance;

#if DEBUG
            // Send a TOS popup when the client uses tos@test.com as email
            if (loginDataPB.EmailAddress == "tos@test.com")
            {
                authTicket = AuthTicket.CreateBuilder()
                    .SetSessionId(0)
                    .SetTosurl("http://localhost/tos")  // The client adds &locale=en_us to this url (or another locale code)
                    .Build();

                return AuthStatusCode.NeedToAcceptLegal;
            }
#endif

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
            AuthStatusCode statusCode = AccountManager.TryGetAccountByLoginDataPB(loginDataPB, _playerManager.Config.UseWhitelist, out DBAccount account);

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
            ulong sessionId = _idGenerator.Generate();
            string platformTicket = _platformTicketManager.GenerateToken(sessionId);

            ClientSession session = new(sessionId, account, platformTicket, downloaderEnum, locale);
            lock (_pendingSessionDict)
                _pendingSessionDict.Add(session.Id, session);

            // Create an AuthTicket for the client
            // Avoid extra allocations and copying by using Unsafe.FromBytes() for session key and token.
            authTicket = AuthTicket.CreateBuilder()
                .SetSessionKey(ByteString.Unsafe.FromBytes(session.Key))
                .SetSessionToken(ByteString.Unsafe.FromBytes(session.Token))
                .SetSessionId(session.Id)
                .SetFrontendServer(IFrontendClient.FrontendAddress)
                .SetFrontendPort(IFrontendClient.FrontendPort)
                .SetPlatformTicket(platformTicket)
                .SetHasnews(_playerManager.Config.ShowNewsOnLogin)
                .SetNewsurl(_playerManager.Config.NewsUrl)
                .SetSuccess(true)
                .Build();

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

            // Success!
            Logger.Info($"Successful auth for client [{client}]");
            return true;
        }

        /// <summary>
        /// Verifies credentials for MTX store authentication.
        /// </summary>
        public bool VerifyPlatformTicket(string email, string token, out ulong playerDbId)
        {
            playerDbId = 0;

            if (_platformTicketManager.TryGetValue(token, out ulong sessionId) == false)
                return Logger.WarnReturn(false, $"VerifyPlatformTicket(): Invalid token {token}");

            ClientSession session;
            lock (_activeSessionDict)
                _activeSessionDict.TryGetValue(sessionId, out session);

            if (session == null)
                return Logger.WarnReturn(false, $"VerifyPlatformTicket(): Failed to retrieve session! sessionId=0x{sessionId:X}, token={token}, email={email}");

            if (session.PlatformTicket != token)
                return Logger.WarnReturn(false, $"VerifyPlatformTicket(): Token mismatch for session 0x{sessionId:X}: expected {session.PlatformTicket}, received {token}");

            if (session.Account is not DBAccount account)
                return Logger.WarnReturn(false, $"VerifyPlatformTicket(): No account for session 0x{sessionId:X}");

            if (account.Email.Equals(email, StringComparison.OrdinalIgnoreCase) == false)
                return Logger.WarnReturn(false, $"VerifyPlatformTicket(): Email mismatch for sessionId 0x{sessionId:X}");

            playerDbId = (ulong)account.Id;
            return true;
        }

        /// <summary>
        /// Removes the <see cref="ClientSession"/> with the specified id.
        /// </summary>
        public void RemoveActiveSession(ulong sessionId)
        {
            lock (_activeSessionDict)
            {
                if (_activeSessionDict.Remove(sessionId, out ClientSession session) == false)
                    Logger.Warn($"RemoveActiveSession(): No active session for sessionId {sessionId:X}");

                if (_clientDict.Remove(sessionId) == false)
                    Logger.Warn($"RemoveActiveSession(): No client for sessionId {sessionId:X}");

                _platformTicketManager.RemoveToken(session.PlatformTicket);
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
                    _platformTicketManager.RemoveToken(session.PlatformTicket);
                }
            }
        }
    }
}
