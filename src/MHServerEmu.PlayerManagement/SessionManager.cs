using Google.ProtocolBuffers;
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
            AuthStatusCode statusCode = AccountManager.TryGetAccountByLoginDataPB(loginDataPB, out DBAccount account);

            // Create a new session if login data is valid
            if (statusCode == AuthStatusCode.Success)
            {
                // Validate downloader value
                if (Enum.TryParse(loginDataPB.ClientDownloader, out ClientDownloader downloaderEnum) == false)
                {
                    downloaderEnum = ClientDownloader.None;
                    Logger.Warn($"TryCreateSessionFromLoginDataPB(): Invalid client downloader {loginDataPB.ClientDownloader}, defaulting to {downloaderEnum}");
                }

                session = new(_idGenerator.Generate(), account, downloaderEnum, loginDataPB.Locale);
                lock (_sessionDict)
                    _sessionDict.Add(session.Id, session);
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
                return Logger.WarnReturn (false, $"VerifyClientCredentials(): SessionId 0x{credentials.Sessionid:X} not found");

            // Verify the token if enabled
            if (_playerManager.Config.UseJsonDBManager == false && _playerManager.Config.IgnoreSessionToken == false &&
                session.Account.Flags.HasFlag(AccountFlags.LinuxCompatibilityMode) == false)
            {
                // Try to decrypt the token (we avoid extra allocations and copying by accessing buffers directly with Unsafe.GetBuffer())
                if (CryptographyHelper.TryDecryptToken(ByteString.Unsafe.GetBuffer(credentials.EncryptedToken), session.Key,
                    ByteString.Unsafe.GetBuffer(credentials.Iv), out byte[] decryptedToken) == false)
                {
                    lock (_sessionDict) _sessionDict.Remove(session.Id);    // Invalidate the session after a failed login attempt
                    return Logger.WarnReturn(false, $"VerifyClientCredentials(): Failed to decrypt token for {session}");
                }

                // Verify the token
                if (CryptographyHelper.VerifyToken(decryptedToken, session.Token) == false)
                {
                    lock (_sessionDict) _sessionDict.Remove(session.Id);    // Invalidate the session after a failed login attempt
                    return Logger.WarnReturn(false, $"VerifyClientCredentials(): Failed to verify token for {session}");
                }
            }

            // Assign the session to the client if the token is valid
            lock (_sessionDict)
            {
                // Handle the case when someone hijacks another client's credentials and attempts to log in with them while the actual client is still logged in
                if (_clientDict.TryAdd(session.Id, client) == false)
                    return Logger.WarnReturn(false, $"VerifyClientCredentials(): A client is attempting to use {session} that is already in use");

                // Sessions cannot be reassigned
                if (client.AssignSession(session) == false)
                {
                    _clientDict.Remove(session.Id);
                    return Logger.WarnReturn(false, $"VerifyClientCredentials(): Failed to assign {session} to a client");
                }
            }

            return true;
        }

        /// <summary>
        /// Removes the <see cref="ClientSession"/> with the specified id.
        /// </summary>
        public void RemoveSession(ulong sessionId)
        {
            lock (_sessionDict)
            {
                _sessionDict.Remove(sessionId);
                _clientDict.Remove(sessionId);
            }
        }

        /// <summary>
        /// Retrieves the <see cref="ClientSession"/> for the specified session id. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool TryGetSession(ulong sessionId, out ClientSession session)
        {
            return _sessionDict.TryGetValue(sessionId, out session);
        }

        /// <summary>
        /// Retrieves the <see cref="FrontendClient"/> for the specified session id. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool TryGetClient(ulong sessionId, out FrontendClient client)
        {
            return _clientDict.TryGetValue(sessionId, out client);
        }
    }
}
