using Gazillion;
using Google.ProtocolBuffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmuTests.Business
{
    public class TcpClientManager : IDisposable
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private readonly string _server;
        private readonly int _port;

        public TcpClientManager(string server, int port)
        {
            _server = server;
            _port = port;
        }

        public bool EtablishConnectionWithFrontEndServer()
        {
            try
            {
                _client = new TcpClient(_server, _port);
                _stream = _client.GetStream();
                _stream.ReadTimeout = 30000;

                PacketOut packetOut = new(1, MuxCommand.Connect);
                byte[] data = packetOut.Data;
                _stream.Write(data, 0, data.Length);

                CodedInputStream codedInputStream = CodedInputStream.CreateInstance(_stream);
                PacketIn packet = new(codedInputStream);
                return packet.Command == MuxCommand.ConnectAck;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error : {e.Message}");
                return false;
            }
        }

        public PacketIn SendDataToFrontEndServer(List<GameMessage> gameMessages)
        {
            try
            {
                    PacketOut packetOut = new(1, MuxCommand.Data);
                    packetOut.AddMessages(gameMessages);
                    byte[] data = packetOut.Data;
                    _stream.Write(data, 0, data.Length);

                    CodedInputStream codedInputStream = CodedInputStream.CreateInstance(_stream);
                    return new PacketIn(codedInputStream);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Erreur : {e.Message}");
                return null;
            }
        }

        public void Close()
        {
            _stream?.Close();
            _client?.Close();
        }

        void IDisposable.Dispose() => Close();
    }
}
