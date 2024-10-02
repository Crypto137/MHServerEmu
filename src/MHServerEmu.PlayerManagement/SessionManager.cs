using System.Text;
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

            // Early exit if there is an issue with the account
            if (statusCode != AuthStatusCode.Success)
                return statusCode;

            // Check if this login request is coming from a Steam Deck, use the name of its custom GPU to identify it.
            // NOTE: This also affects Steam Deck users running Windows, but we have no way of distinguishing Windows vs Proton.
            const string SteamDeckGpuName = "AMD Custom GPU 0405 (RADV VANGOGH)";

            string gpuName = GetGpuNameFromLoginDataPB(loginDataPB);
            if (gpuName == SteamDeckGpuName && _playerManager.Config.IgnoreSessionToken == false && account.Flags.HasFlag(AccountFlags.LinuxCompatibilityMode) == false)
            {
                Logger.Warn($"TryCreateSessionFromLoginDataPB(): {account} attempted to connect from a Steam Deck without enabling compatibility mode");
                return AuthStatusCode.InternalError500;
            }

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
            lock (_sessionDict)
                _sessionDict.Add(session.Id, session);

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

        /// <summary>
        /// Parses GPU name from the machineIdDebugInfo field of the provided <see cref="LoginDataPB"/>.
        /// </summary>
        private static string GetGpuNameFromLoginDataPB(LoginDataPB loginDataPB)
        {
            // Constants are multiplied by 2 because each byte is represented by 2 chars
            const int MachineIdDebugInfoLength = 352 * 2;
            const int GpuNameOffset = 74 * 2;
            const int GpuNameLength = 128 * 2;

            if (loginDataPB.HasMachineIdDebugInfo == false)
                return string.Empty;

            string machineIdDebugInfo = loginDataPB.MachineIdDebugInfo;
            
            // Validate length, we may get malformed data from hackers here
            if (machineIdDebugInfo.Length != MachineIdDebugInfoLength)
                return Logger.WarnReturn(string.Empty, $"GetGpuNameFromLoginDataPB(): Received machineIdDebugInfo with invalid length {machineIdDebugInfo.Length}");

            // Slice the chars representing UTF-16 encoded name bytes and convert them to bytes
            ReadOnlySpan<char> gpuNameHexString = loginDataPB.MachineIdDebugInfo.AsSpan(GpuNameOffset, GpuNameLength);
            byte[] data = Convert.FromHexString(gpuNameHexString);

            // Trim nulls
            int i = 0;

            while (i < data.Length)
            {
                if (data[i] == 0 && data[i + 1] == 0)
                    break;

                i++;
            }

            ReadOnlySpan<byte> trimmedData = data.AsSpan(0, i + 1);

            // Convert hex string to a readable string
            return Encoding.Unicode.GetString(trimmedData);
        }
    }
}
