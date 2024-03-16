using Gazillion;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games;

namespace MHServerEmu.PlayerManagement
{
    /// <summary>
    /// Manages <see cref="ClientSession"/> instances.
    /// </summary>
    public class SessionManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly PlayerManagerService _playerManager;

        private readonly IdGenerator _idGenerator = new(IdType.Session, 0);

        private readonly object _sessionLock = new();
        private readonly Dictionary<ulong, ClientSession> _sessionDict = new();
        private readonly Dictionary<ulong, FrontendClient> _clientDict = new();

        public int SessionCount { get => _sessionDict.Count; }

        /// <summary>
        /// Constructs a new <see cref="SessionManager"/> instance for the provided <see cref="PlayerManagerService"/>.
        /// </summary>
        public SessionManager(PlayerManagerService playerManager)
        {
            _playerManager = playerManager;
        }

        /// <summary>
        /// Verifies the provided <see cref="LoginDataPB"/> instance and creates a new <see cref="ClientSession"/> if it's valid.
        /// <see cref="AuthStatusCode"/> indicates the outcome of validation.
        /// </summary>
        public AuthStatusCode TryCreateSessionFromLoginDataPB(LoginDataPB loginDataPB, out ClientSession session)
        {
            session = null;

            // Check client version
            if (loginDataPB.Version != Game.Version)
            {
                Logger.Warn($"TryCreateSessionFromLoginDataPB(): Client version mismatch ({loginDataPB.Version} instead of {Game.Version})");

                // Fail auth if version mismatch is not allowed
                if (_playerManager.Config.AllowClientVersionMismatch == false)
                    return AuthStatusCode.PatchRequired;
            }

            // Verify credentials
            DBAccount account;
            AuthStatusCode statusCode;

            if (_playerManager.Config.BypassAuth)  // Auth always succeeds when BypassAuth is set to true
            {
                account = AccountManager.DefaultAccount;
                statusCode = AuthStatusCode.Success;
            }
            else                                    // Check credentials with AccountManager
            {
                statusCode = AccountManager.TryGetAccountByLoginDataPB(loginDataPB, out account);
            }

            // Create a new session if login data is valid
            if (statusCode == AuthStatusCode.Success)
            {
                // Validate downloader value
                if (Enum.TryParse(loginDataPB.ClientDownloader, out ClientDownloader downloaderEnum) == false)
                {
                    downloaderEnum = ClientDownloader.None;
                    Logger.Warn($"TryCreateSessionFromLoginDataPB(): Invalid client downloader {loginDataPB.ClientDownloader}, defaulting to {downloaderEnum}");
                }

                lock (_sessionLock)
                {
                    session = new(_idGenerator.Generate(), account, downloaderEnum, loginDataPB.Locale);
                    _sessionDict.Add(session.Id, session);
                }
            }

            return statusCode;
        }

        /// <summary>
        /// Verifies the provided <see cref="ClientCredentials"/> and assigns the appropriate <see cref="ClientSession"/>
        /// to the specified <see cref="FrontendClient"/> if they are valid. Returns <see langword="true"/> if the credentials
        /// are valid.
        /// </summary>
        public bool VerifyClientCredentials(FrontendClient client, ClientCredentials credentials)
        {
            // Check if the session exists
            if (_sessionDict.TryGetValue(credentials.Sessionid, out ClientSession session) == false)
                return Logger.WarnReturn (false, $"VerifyClientCredentials(): SessionId {credentials.Sessionid} not found");

            // Try to decrypt the token
            if (CryptographyHelper.TryDecryptToken(credentials.EncryptedToken.ToByteArray(), session.Key,
                credentials.Iv.ToByteArray(), out byte[] decryptedToken) == false)
            {
                lock (_sessionLock) _sessionDict.Remove(session.Id);    // Invalidate the session after a failed login attempt
                return Logger.WarnReturn(false, $"VerifyClientCredentials(): Failed to decrypt token for sessionId {session.Id}"); ;
            }

            // Verify the token
            if (CryptographyHelper.VerifyToken(decryptedToken, session.Token) == false)
            {
                lock (_sessionLock) _sessionDict.Remove(session.Id);    // Invalidate the session after a failed login attempt
                return Logger.WarnReturn(false, $"VerifyClientCredentials(): Failed to verify token for sessionId {session.Id}"); ;
            }

            Logger.Info($"Verified client for sessionId {session.Id} - account {session.Account}");

            // Assign the session to the client if the token is valid
            lock (_sessionLock)
            {
                client.AssignSession(session);
                _clientDict.Add(session.Id, client);
                return true;
            }
        }

        /// <summary>
        /// Removes the <see cref="ClientSession"/> with the specified id.
        /// </summary>
        public void RemoveSession(ulong sessionId)
        {
            lock (_sessionLock)
            {
                _sessionDict.Remove(sessionId);
                _clientDict.Remove(sessionId);
            }
        }

        /// <summary>
        /// Retrieves the <see cref="ClientSession"/> for the specified session id. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool TryGetSession(ulong sessionId, out ClientSession session) => _sessionDict.TryGetValue(sessionId, out session);

        /// <summary>
        /// Retrieves the <see cref="FrontendClient"/> for the specified session id. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool TryGetClient(ulong sessionId, out FrontendClient client) => _clientDict.TryGetValue(sessionId, out client);
    }
}
