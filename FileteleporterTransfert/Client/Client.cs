using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using FileteleporterTransfert.Network;
using FileteleporterTransfert.Tools;

namespace FileteleporterTransfert.Client
{
    public class Client
    { 
        public static Client instance;

        public readonly string ip;
        public string name;
        public int myId = 0;
        public TCP tcp;
        public UDP udp;

        private bool isConnected;
        private delegate void PacketHandler(Packet packet);
        private static Dictionary<int, PacketHandler> _packetHandlers;

        public Client(string ip, string name)
        {
            this.ip = ip;
            this.name = name;
            instance = this;
        }

        private void OnApplicationQuit()
        {
            Disconnect(); // Disconnect when the game is closed
        }

        /// <summary>Attempts to connect to the server.</summary>
        public void ConnectToServer()
        {
            tcp = new TCP();
            udp = new UDP();

            InitializeClientData();

            isConnected = true;
            tcp.Connect(); // Connect tcp, udp gets connected once tcp is done
        }

        public class TCP
        {
            public TcpClient socket;

            private NetworkStream stream;
            private Packet receivedData;
            private byte[] receiveBuffer;

            /// <summary>Attempts to connect to the server via TCP.</summary>
            public void Connect()
            {
                socket = new TcpClient
                {
                    ReceiveBufferSize = Constants.DATA_BUFFER_SIZE,
                    SendBufferSize = Constants.DATA_BUFFER_SIZE
                };

                receiveBuffer = new byte[Constants.DATA_BUFFER_SIZE];
                socket.BeginConnect(instance.ip, Constants.MAIN_PORT, ConnectCallback, socket);
            }

            /// <summary>Initializes the newly connected client's TCP-related info.</summary>
            private void ConnectCallback(IAsyncResult result)
            {
                socket.EndConnect(result);

                if (!socket.Connected)
                {
                    return;
                }

                stream = socket.GetStream();

                receivedData = new Packet();

                stream.BeginRead(receiveBuffer, 0, Constants.DATA_BUFFER_SIZE, ReceiveCallback, null);
            }

            /// <summary>Sends data to the client via TCP.</summary>
            /// <param name="packet">The packet to send.</param>
            public void SendData(Packet packet)
            {
                try
                {
                    if (socket != null)
                    {
                        stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null); // Send data to server
                    }
                }
                catch (Exception ex)
                {
                    EZConsole.WriteLine("Client", $"Error sending data to server via TCP: {ex}");
                }
            }

            /// <summary>Reads incoming data from the stream.</summary>
            private void ReceiveCallback(IAsyncResult result)
            {
                try
                {
                    var byteLength = stream.EndRead(result);
                    if (byteLength <= 0)
                    {
                        Disconnect();
                        return;
                    }

                    byte[] data = new byte[byteLength];
                    Array.Copy(receiveBuffer, data, byteLength);

                    receivedData.Reset(HandleData(data)); // Reset receivedData if all data was handled
                    stream.BeginRead(receiveBuffer, 0, Constants.DATA_BUFFER_SIZE, ReceiveCallback, null);
                }
                catch
                {
                    Disconnect();
                }
            }

            /// <summary>Prepares received data to be used by the appropriate packet handler methods.</summary>
            /// <param name="data">The received data.</param>
            private bool HandleData(byte[] data)
            {
                var packetLength = 0;

                receivedData.SetBytes(data);

                if (receivedData.UnreadLength() >= 4)
                {
                    // If client's received data contains a packet
                    packetLength = receivedData.ReadInt();
                    if (packetLength <= 0)
                    {
                        // If packet contains no data
                        return true; // Reset receivedData instance to allow it to be reused
                    }
                }

                while (packetLength > 0 && packetLength <= receivedData.UnreadLength())
                {
                    // While packet contains data AND packet data length doesn't exceed the length of the packet we're reading
                    byte[] packetBytes = receivedData.ReadBytes(packetLength);
                    ThreadManager.ExecuteOnMainThread(() =>
                    {
                        using Packet packet = new Packet(packetBytes);
                        int packetId = packet.ReadInt();
                        _packetHandlers[packetId](packet); // Call appropriate method to handle the packet
                    });

                    packetLength = 0; // Reset packet length
                    if (receivedData.UnreadLength() >= 4)
                    {
                        // If client's received data contains another packet
                        packetLength = receivedData.ReadInt();
                        if (packetLength <= 0)
                        {
                            // If packet contains no data
                            return true; // Reset receivedData instance to allow it to be reused
                        }
                    }
                }

                if (packetLength <= 1)
                {
                    return true; // Reset receivedData instance to allow it to be reused
                }

                return false;
            }

            /// <summary>Disconnects from the server and cleans up the TCP connection.</summary>
            private void Disconnect()
            {
                instance?.Disconnect();

                stream = null;
                receivedData = null;
                receiveBuffer = null;
                socket = null;
            }
        }

        public class UDP
        {
            public UdpClient socket;
            public IPEndPoint endPoint;

            public UDP()
            {
                endPoint = new IPEndPoint(IPAddress.Parse(instance.ip), Constants.MAIN_PORT);
            }

            /// <summary>Attempts to connect to the server via UDP.</summary>
            /// <param name="localPort">The port number to bind the UDP socket to.</param>
            public void Connect(int localPort)
            {
                socket = new UdpClient(localPort);

                socket.Connect(endPoint);
                socket.BeginReceive(ReceiveCallback, null);

                using var packet = new Packet();
                SendData(packet);
            }

            /// <summary>Sends data to the client via UDP.</summary>
            /// <param name="packet">The packet to send.</param>
            public void SendData(Packet packet)
            {
                try
                {
                    packet.InsertInt(instance.myId); // Insert the client's ID at the start of the packet
                    socket?.BeginSend(packet.ToArray(), packet.Length(), null, null);
                }
                catch (Exception ex)
                {
                    EZConsole.WriteLine("Client", $"Error sending data to server via UDP: {ex}");
                }
            }

            /// <summary>Receives incoming UDP data.</summary>
            private void ReceiveCallback(IAsyncResult result)
            {
                try
                {
                    var data = socket.EndReceive(result, ref endPoint);
                    socket.BeginReceive(ReceiveCallback, null);

                    if (data.Length < 4)
                    {
                        Disconnect();
                        return;
                    }

                    HandleData(data);
                }
                catch
                {
                    Disconnect();
                }
            }

            /// <summary>Prepares received data to be used by the appropriate packet handler methods.</summary>
            /// <param name="data">The received data.</param>
            private void HandleData(byte[] data)
            {
                using (var packet = new Packet(data))
                {
                    var packetLength = packet.ReadInt();
                    data = packet.ReadBytes(packetLength);
                }

                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using var packet = new Packet(data);
                    var packetId = packet.ReadInt();
                    _packetHandlers[packetId](packet); // Call appropriate method to handle the packet
                });
            }

            /// <summary>Disconnects from the server and cleans up the UDP connection.</summary>
            private void Disconnect()
            {
                instance?.Disconnect();

                endPoint = null;
                socket = null;
            }
        }

        /// <summary>Initializes all necessary client data.</summary>
        private void InitializeClientData()
        {
            _packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)ServerPackets.Welcome, ClientHandle.Welcome },
                { (int)ServerPackets.ValidateDenyTransfer, ClientHandle.ValidateDenyTransfer }
            };
            EZConsole.WriteLine("Client", "Initialized packets.");
        }

        /// <summary>Disconnects from the server and stops all network traffic.</summary>
        /// 
        public void Disconnect(Object sender, EventArgs e)
        {
            Disconnect();
        }
        public void Disconnect()
        {
            if (!isConnected) return;
            isConnected = false;
            if(tcp.socket != null)
                tcp.socket.Close();
            if(udp.socket != null)
                udp.socket.Close();
            instance = null;

            tcp = null;
            udp = null;

            instance = null;

            EZConsole.WriteLine("Client", "Disconnected from server.");
            NetController.instance.SendData(NetController.ActionOnController.Infos, new[] { "Client disconnected" });
        }
    }

}
