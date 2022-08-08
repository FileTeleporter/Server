using System;
using System.Net;
using System.Net.Sockets;
using FileteleporterTransfert.Network;
using FileteleporterTransfert.Tools;

namespace FileteleporterTransfert.Server
{
    class Client
    {

        private readonly int id;
        public string name;
        public TCP tcp;
        public UDP udp;

        public Client(int clientId)
        {
            id = clientId;
            tcp = new TCP(id);
            udp = new UDP(id);
        }

        public class TCP
        {
            public TcpClient socket;

            private readonly int id;
            private NetworkStream stream;
            private Packet receivedData;
            private byte[] receiveBuffer;

            public TCP(int id)
            {
                this.id = id;
            }

            /// <summary>Initializes the newly connected client's TCP-related info.</summary>
            /// <param name="socket">The TcpClient instance of the newly connected client.</param>
            public void Connect(TcpClient socket)
            {
                this.socket = socket;
                this.socket.ReceiveBufferSize = Constants.DATA_BUFFER_SIZE;
                this.socket.SendBufferSize = Constants.DATA_BUFFER_SIZE;

                stream = this.socket.GetStream();

                receivedData = new Packet();
                receiveBuffer = new byte[Constants.DATA_BUFFER_SIZE];

                stream.BeginRead(receiveBuffer, 0, Constants.DATA_BUFFER_SIZE, ReceiveCallback, null);

                ServerSend.Welcome(id, "Welcome to the server!");
            }

            /// <summary>Sends data to the client via TCP.</summary>
            /// <param name="packet">The packet to send.</param>
            public void SendData(Packet packet)
            {
                try
                {
                    if (socket != null)
                    {
                        stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null); // Send data to appropriate client
                    }
                }
                catch (Exception ex)
                {
                    EZConsole.WriteLine("Server", $"Error sending data to player {id} via TCP: { ex}");
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
                        Server.clients[id].Disconnect();
                        return;
                    }

                    var data = new byte[byteLength];
                    Array.Copy(receiveBuffer, data, byteLength);

                    receivedData.Reset(HandleData(data)); // Reset receivedData if all data was handled
                    stream.BeginRead(receiveBuffer, 0, Constants.DATA_BUFFER_SIZE, ReceiveCallback, null);
                }
                catch (Exception ex)
                {
                    EZConsole.WriteLine("Server", $"Error receiving TCP data: {ex}");
                    Server.clients[id].Disconnect();
                }
            }

            /// <summary>Prepares received data to be used by the appropriate packet handler methods.</summary>
            /// <param name="data">The recieved data.</param>
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
                    var packetBytes = receivedData.ReadBytes(packetLength);
                    ThreadManager.ExecuteOnMainThread(() =>
                    {
                        using var packet = new Packet(packetBytes);
                        var packetId = packet.ReadInt();
                        Server.packetHandlers[packetId](id, packet); // Call appropriate method to handle the packet
                    });

                    packetLength = 0; // Reset packet length
                    if (receivedData.UnreadLength() < 4) continue;
                    // If client's received data contains another packet
                    EZConsole.WriteLine("Server", "another packet");
                    packetLength = receivedData.ReadInt();
                    if (packetLength <= 0)
                    {
                        // If packet contains no data
                        return true; // Reset receivedData instance to allow it to be reused
                    }
                }

                return packetLength <= 1;
                // Reset receivedData instance to allow it to be reused
            }

            /// <summary>Closes and cleans up the TCP connection.</summary>
            public void Disconnect()
            {
                socket.Close();
                stream = null;
                receivedData = null;
                receiveBuffer = null;
                socket = null;
            }
        }

        public class UDP
        {
            public IPEndPoint endPoint;

            private int id;

            public UDP(int id)
            {
                this.id = id;
            }

            /// <summary>Initializes the newly connected client's UDP-related info.</summary>
            /// <param name="endpoint">The IPEndPoint instance of the newly connected client.</param>
            public void Connect(IPEndPoint endpoint)
            {
                this.endPoint = endpoint;
            }

            /// <summary>Sends data to the client via UDP.</summary>
            /// <param name="packet">The packet to send.</param>
            public void SendData(Packet packet)
            {
                Server.SendUDPData(endPoint, packet);
            }

            /// <summary>Prepares received data to be used by the appropriate packet handler methods.</summary>
            /// <param name="packetData">The packet containing the recieved data.</param>
            public void HandleData(Packet packetData)
            {
                var packetLength = packetData.ReadInt();
                var packetBytes = packetData.ReadBytes(packetLength);

                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using var packet = new Packet(packetBytes);
                    var packetId = packet.ReadInt();
                    Server.packetHandlers[packetId](id, packet); // Call appropriate method to handle the packet
                });
            }

            /// <summary>Cleans up the UDP connection.</summary>
            public void Disconnect()
            {
                endPoint = null;
            }
        }

        /// <summary>Disconnects the client and stops all network traffic.</summary>
        private void Disconnect()
        {
            EZConsole.WriteLine("Server", $"{tcp.socket.Client.RemoteEndPoint} has disconnected.");

            //player = null;

            tcp.Disconnect();
            udp.Disconnect();
        }
    }
}
