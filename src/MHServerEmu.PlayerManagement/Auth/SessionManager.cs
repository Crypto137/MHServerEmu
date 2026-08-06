using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System;
using MHServerEmu.Core.System.Time;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games;
using MHServerEmu.PlayerManagement.Players;

namespace MHServerEmu.PlayerManagement.Auth
{
    public enum VerifyPlatformTicketResult
    {
        Success,
        InvalidTicket,
        SessionNotFound,
        AccountNotFound,
        TicketMismatch,
        EmailMismatch,
    }

    /// <summary>
    /// Authenticates clients and manages <see cref="ClientSession"/> instances.
    /// </summary>
    public class SessionManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly TimeSpan PendingSessionLifespan = TimeSpan.FromSeconds(60);

        private readonly PlayerManagerService _playerManager;

        private readonly IdGenerator _idGenerator = new(IdType.Session, 0);
        private readonly TokenManager<ulong> _platformTicketManager = new();
        // "Platform Tickets" are tokens used to access the Add G page from the MTX store.

        private readonly Dictionary<ulong, ClientSession> _pendingSessions = new();
        private readonly Dictionary<ulong, IFrontendClient> _activeSessions = new();

        private CooldownTimer _updateTimer = new(TimeSpan.FromMilliseconds(1000));

        public bool WhitelistEnabled { get; private set; }

        public int PendingSessionCount { get => _pendingSessions.Count; }
        public int ActiveSessionCount { get => _activeSessions.Count; }

        /// <summary>
        /// Constructs a new <see cref="SessionManager"/> instance for the provided <see cref="PlayerManagerService"/>.
        /// </summary>
        public SessionManager(PlayerManagerService playerManager)
        {
            _playerManager = playerManager;
            WhitelistEnabled = playerManager.Config.UseWhitelist;
        }

        public void SetWhitelistEnabled(bool enable)
        {
            if (WhitelistEnabled == enable)
                return;

            WhitelistEnabled = enable;
            Logger.Info($"Whitelist {(enable ? "enabled" : "disabled")}");
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
            if (!Verify.IsTrue(loginDataPB.HasVersion, $"LoginDataPB for {loginDataPB.EmailAddress} contains no version information"))
                return AuthStatusCode.PatchRequired;

            if (loginDataPB.Version != Game.Version)
            {
                Logger.Warn($"TryCreateSessionFromLoginDataPB(): Client version mismatch ({loginDataPB.Version} instead of {Game.Version})");

                // Fail auth if version mismatch is not allowed
                if (_playerManager.Config.AllowClientVersionMismatch == false)
                    return AuthStatusCode.PatchRequired;
            }

            // Verify credentials
            AuthStatusCode statusCode = AccountManager.TryGetAccountByLoginDataPB(loginDataPB, WhitelistEnabled, out DBAccount account);

            if (statusCode != AuthStatusCode.Success)
                return statusCode;

            // Validate client downloader
            ClientDownloader downloaderEnum = ClientDownloader.None;

            if (loginDataPB.HasClientDownloader)
            {
                bool parseResult = Enum.TryParse(loginDataPB.ClientDownloader, out downloaderEnum);
                if (!Verify.IsTrue(parseResult, $"Invalid client downloader {loginDataPB.ClientDownloader} for {account}, defaulting to {downloaderEnum}"))
                    downloaderEnum = ClientDownloader.None;
            }

            string locale = loginDataPB.HasLocale ? loginDataPB.Locale : "en_us";

            // Create a new session
            ulong sessionId = _idGenerator.Generate();
            string platformTicket = _platformTicketManager.GenerateToken(sessionId);

            ClientSession session = new(sessionId, account, platformTicket, downloaderEnum, locale);
            _pendingSessions.Add(session.Id, session);

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
            bool sessionFound = _pendingSessions.Remove(credentials.Sessionid, out ClientSession session);
            if (!Verify.IsTrue(sessionFound, $"SessionId 0x{credentials.Sessionid:X} not found"))
                return false;

            // Try to decrypt the token (we avoid extra allocations and copying by accessing buffers directly with Unsafe.GetBuffer())
            byte[] encryptedToken = ByteString.Unsafe.GetBuffer(credentials.EncryptedToken);
            byte[] iv = ByteString.Unsafe.GetBuffer(credentials.Iv);

            bool decryptResult = CryptographyHelper.TryDecryptToken(encryptedToken, session.Key, iv, out byte[] decryptedToken);
            if (!Verify.IsTrue(decryptResult, $"Failed to decrypt server token for session {session}"))
                return false;

            bool verifyResult = CryptographyHelper.VerifyToken(decryptedToken, session.Token);
            if (!Verify.IsTrue(verifyResult, $"Failed to verify token for session {session}"))
                return false;

#if GAME_VERSION_1_53
            // 1.53 added a second token generated by the client and encrypted using the same key/iv as the server token.
            if (credentials.HasEncryptedClientToken)
            {
                byte[] encryptedClientToken = ByteString.Unsafe.GetBuffer(credentials.EncryptedClientToken);

                bool decryptClientResult = CryptographyHelper.TryDecryptToken(encryptedClientToken, session.Key, iv, out byte[] decryptedClientToken);
                if (!Verify.IsTrue(decryptClientResult, $"Failed to decrypt client token for session {session}"))
                    return false;

                // Client token CRC will be sent back to the client to prove the server's identity once the client passes the login queue.
                session.ClientTokenCrc = HashHelper.Crc32(decryptedClientToken);
            }
#endif

            // Assign the session to the client if the token is valid
            // Handle the case when someone hijacks another client's credentials and attempts to log in with them while the actual client is still logged in
            if (!Verify.IsTrue(_activeSessions.TryAdd(session.Id, client), $"A client is attempting to use session {session} that is already in use!"))
                return false;

            // Sessions cannot be reassigned
            if (!Verify.IsTrue(client.AssignSession(session), $"Failed to assign {session} to a client"))
            {
                _activeSessions.Remove(session.Id);
                _platformTicketManager.RemoveToken(session.PlatformTicket);
                return false;
            }

            // Success!
            Logger.Info($"Successful auth for client [{client}]");
            return true;
        }

        /// <summary>
        /// Verifies credentials for MTX store authentication.
        /// </summary>
        public VerifyPlatformTicketResult VerifyPlatformTicket(string email, string ticket, out ulong playerDbId)
        {
            playerDbId = 0;

            if (_platformTicketManager.TryGetValue(ticket, out ulong sessionId) == false)
                return VerifyPlatformTicketResult.InvalidTicket;

            if (TryGetActiveSession(sessionId, out ClientSession session) == false || session == null)
                return VerifyPlatformTicketResult.SessionNotFound;

            if (session.Account is not DBAccount account)
                return VerifyPlatformTicketResult.AccountNotFound;

            if (session.PlatformTicket != ticket)
                return VerifyPlatformTicketResult.TicketMismatch;

            if (account.Email.Equals(email, StringComparison.OrdinalIgnoreCase) == false)
                return VerifyPlatformTicketResult.EmailMismatch;

            playerDbId = (ulong)account.Id;
            return VerifyPlatformTicketResult.Success;
        }

        /// <summary>
        /// Removes the <see cref="ClientSession"/> with the specified id.
        /// </summary>
        public void RemoveActiveSession(ulong sessionId)
        {
            Verify.IsTrue(_activeSessions.Remove(sessionId, out IFrontendClient client), $"No active session for sessionId 0x{sessionId:X}");

            ClientSession session = client.Session as ClientSession;

            if (Verify.IsNotNull(session))
                _platformTicketManager.RemoveToken(session.PlatformTicket);
        }

        /// <summary>
        /// Retrieves the <see cref="ClientSession"/> for the specified session id. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool TryGetActiveSession(ulong sessionId, out ClientSession session)
        {
            session = null;

            if (_activeSessions.TryGetValue(sessionId, out IFrontendClient client))
                session = client.Session as ClientSession;

            return session != null;
        }

        private void PurgeExpiredSessions()
        {
            if (_pendingSessions.Count == 0)
                return;

            foreach (var kvp in _pendingSessions)
            {
                ClientSession session = kvp.Value;

                if (session.Length <= PendingSessionLifespan)
                    continue;

                Logger.Warn($"Pending session expired: sessionId=0x{session.Id:X}, account=[{session.Account}]");
                _pendingSessions.Remove(kvp.Key);
                _platformTicketManager.RemoveToken(session.PlatformTicket);
            }
        }
    }
}
