using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Auth;
using MHServerEmu.Frontend;
using MHServerEmu.Networking;
using MHServerEmuTests.Maps;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmuTests.Business
{
    public class ServersHelper
    {
        public static FrontendServer FrontendServer { get; private set; }
        public static AuthServer AuthServer { get; private set; }
        public static Thread FrontendServerThread { get; private set; }
        public static Thread AuthServerThread { get; private set; }

        public static void LaunchServers()
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            if (!ConfigManager.IsInitialized)
                throw new Exception("ConfigManager not initialized");

            if (!ProtocolDispatchTable.IsInitialized)
                throw new Exception("ProtocolDispatchTable not initialized");

            if (!GameDatabase.IsInitialized)
                throw new Exception("GameDatabase not initialized");

            if (AccountManager.IsInitialized == false)
                throw new Exception("AccountManager not initialized");

            StartServers();
        }

        public static async Task<AuthTicket> ConnectWithUnitTestCredentials()
        {
            return await ConnectWithCredentials(
                "MHEmuServer@test.com",
                "MHEmuServer",
                "Secret Identity Studios Http Client",
                "Steam");
        }

        public static async Task<AuthTicket> ConnectWithCredentials(string emailAddress, string password, string userAgent, string clientDownloader)
        {
            try
            {
                var url = "http://localhost:8080";

                GameMessage gameMessage = new(LoginDataPB.CreateBuilder()
                    .SetEmailAddress(emailAddress)
                    .SetPassword(password)
                    .SetClientDownloader(clientDownloader)
                    .Build());

                using (MemoryStream memoryStream = new())
                {
                    CodedOutputStream codedOutputStream = CodedOutputStream.CreateInstance(memoryStream);
                    gameMessage.Encode(codedOutputStream);
                    codedOutputStream.Flush();
                    byte[] data = memoryStream.ToArray();

                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", userAgent);
                        var content = new ByteArrayContent(data);
                        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
                        var response = await client.PostAsync(url, content);
                        GameMessage message = new(CodedInputStream.CreateInstance(response.Content.ReadAsStreamAsync().Result));
                        message.TryDeserialize<AuthTicket>(out var authTicket);
                        return authTicket;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Connection error : {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Catch the UnhandledException from the app
        /// </summary>
        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            Console.WriteLine(ex);
            Console.ReadLine();
        }

        private static void StartServers()
        {
            StartFrontendServer();
            StartAuthServer();
        }

        private static bool StartFrontendServer()
        {
            if (FrontendServer != null)
                return false;

            FrontendServer = new FrontendServer();
            FrontendServerThread = new(FrontendServer.Run) { IsBackground = true, CurrentCulture = CultureInfo.InvariantCulture };
            FrontendServerThread.Start();

            return true;
        }

        private static bool StartAuthServer()
        {
            if (AuthServer != null)
                return false;

            AuthServer = new(FrontendServer.PlayerManagerService);
            AuthServerThread = new(AuthServer.Run) { IsBackground = true, CurrentCulture = CultureInfo.InvariantCulture };
            AuthServerThread.Start();

            return true;
        }
    }
}
