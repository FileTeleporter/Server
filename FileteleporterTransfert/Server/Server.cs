using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using FileteleporterTransfert.Tools;

namespace server
{
    class Server
    {
        public static int MaxPlayers { get; private set; }
        public static int MaxInboundTransfers { get; private set; }
        public static int Port { get; private set; }
        public static Dictionary<int, Client> clients = new Dictionary<int, Client>();
        public static Dictionary<int, FileteleporterTransfert.Network.SendFile> inboundTransfers = new Dictionary<int, FileteleporterTransfert.Network.SendFile>();
        public delegate void PacketHandler(int _fromClient, Packet _packet);
        public static Dictionary<int, PacketHandler> packetHandlers;

        private static TcpListener tcpListener;
        private static TcpListener tcpSendFileListener;
        private static UdpClient udpListener;

        /// <summary>Starts the server.</summary>
        /// <param name="_maxPlayers">The maximum players that can be connected simultaneously.</param>
        /// <param name="_port">The port to start the server on.</param>
        public static void Start(int _maxPlayers, int _maxTransfers, int _port)
        {
            MaxPlayers = _maxPlayers;
            MaxInboundTransfers = _maxTransfers;
            Port = _port;

            EZConsole.WriteLine("Server", "Starting server...");
            InitializeServerData();

            tcpListener = new TcpListener(IPAddress.Any, Port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(TCPConnectCallback, null);


            // connect to inbound transferer
            tcpSendFileListener = new TcpListener(IPAddress.Any, 60589);
            tcpSendFileListener.Start();
            tcpSendFileListener.BeginAcceptTcpClient(TCPSendFileConnectCallback, null);

            udpListener = new UdpClient(Port);
            udpListener.BeginReceive(UDPReceiveCallback, null);

            EZConsole.WriteLine("Server", $"Server started on port {Port}.");
        }

        /// <summary>Handles new TCP connections.</summary>
        private static void TCPConnectCallback(IAsyncResult _result)
        {
            TcpClient _client = tcpListener.EndAcceptTcpClient(_result);
            tcpListener.BeginAcceptTcpClient(TCPConnectCallback, null);
            EZConsole.WriteLine("Server", $"Incoming connection from {_client.Client.RemoteEndPoint}...");

            for (int i = 1; i <= MaxPlayers; i++)
            {
                if (clients[i].tcp.socket == null)
                {
                    clients[i].tcp.Connect(_client);
                    return;
                }
            }

            EZConsole.WriteLine("Server", $"{_client.Client.RemoteEndPoint} failed to connect: Server full!");
        }

        private static void TCPSendFileConnectCallback(IAsyncResult _result)
        {
            TcpClient _client = tcpListener.EndAcceptTcpClient(_result);
            tcpListener.BeginAcceptTcpClient(TCPSendFileConnectCallback, null);
            EZConsole.WriteLine("SendFile", $"Inbound transfer from {_client.Client.RemoteEndPoint}...");

            //tcpFileSend = new server.Client.TCPFileSend();
            //tcpFileSend.Connect(_client, Constants.BUFFER_FOR_FILE);

            for (int i = 1; i <= MaxInboundTransfers; i++)
            {
                if (inboundTransfers[i].Tcp == null)
                {
                    inboundTransfers[i] = new FileteleporterTransfert.Network.SendFile(_client, true);
                    return;
                }
            }
        }

        /// <summary>Receives incoming UDP data.</summary>
        private static void UDPReceiveCallback(IAsyncResult _result)
        {
            try
            {
                IPEndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] _data = udpListener.EndReceive(_result, ref _clientEndPoint);
                udpListener.BeginReceive(UDPReceiveCallback, null);

                if (_data.Length < 4)
                {
                    return;
                }

                using (Packet _packet = new Packet(_data))
                {
                    int _clientId = _packet.ReadInt();

                    if (_clientId == 0)
                    {
                        return;
                    }

                    if (clients[_clientId].udp.endPoint == null)
                    {
                        // If this is a new connection
                        clients[_clientId].udp.Connect(_clientEndPoint);
                        return;
                    }

                    if (clients[_clientId].udp.endPoint.ToString() == _clientEndPoint.ToString())
                    {
                        // Ensures that the client is not being impersonated by another by sending a false clientID
                        clients[_clientId].udp.HandleData(_packet);
                    }
                }
            }
            catch (Exception _ex)
            {
                EZConsole.WriteLine("Server", $"Error receiving UDP data: {_ex}");
            }
        }

        /// <summary>Sends a packet to the specified endpoint via UDP.</summary>
        /// <param name="_clientEndPoint">The endpoint to send the packet to.</param>
        /// <param name="_packet">The packet to send.</param>
        public static void SendUDPData(IPEndPoint _clientEndPoint, Packet _packet)
        {
            try
            {
                if (_clientEndPoint != null)
                {
                    udpListener.BeginSend(_packet.ToArray(), _packet.Length(), _clientEndPoint, null, null);
                }
            }
            catch (Exception _ex)
            {
                EZConsole.WriteLine("Server", $"Error sending data to {_clientEndPoint} via UDP: {_ex}");
            }
        }

        /// <summary>Initializes all necessary server data.</summary>
        private static void InitializeServerData()
        {
            for (int i = 1; i <= MaxPlayers; i++)
            {
                clients.Add(i, new Client(i));
            }

            for (int i = 1; i <= MaxInboundTransfers; i++)
            {
                inboundTransfers.Add(i, new FileteleporterTransfert.Network.SendFile());
            }

            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)ClientPackets.welcomeReceived, ServerHandle.WelcomeReceived },
                { (int)ClientPackets.askSendFile, ServerHandle.AskSendFile },
            };
            EZConsole.WriteLine("Server", "Initialized packets.");
        }
    }
}
