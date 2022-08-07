using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Net.Sockets;
using static client.Client;
using System.Threading.Tasks;
using System.Net;
using FileteleporterTransfert.Network;

namespace client
{
    public class ClientSend
    {
        #region Send data
        /// <summary>Sends a packet to the server via TCP.</summary>
        /// <param name="_packet">The packet to send to the sever.</param>
        private static void SendTCPData(Packet _packet)
        {
            _packet.WriteLength();
            Client.instance.tcp.SendData(_packet);
        }

        /// <summary>Sends a packet to the server via UDP.</summary>
        /// <param name="_packet">The packet to send to the sever.</param>
        private static void SendUDPData(Packet _packet)
        {
            _packet.WriteLength();
            Client.instance.udp.SendData(_packet);
        }
        #endregion

        #region Packets
        /// <summary>Lets the server know that the welcome message was received.</summary>
        public static void WelcomeReceived()
        {
            using (Packet _packet = new Packet((int)ClientPackets.welcomeReceived))
            {
                _packet.Write(Client.instance.myId);
                _packet.Write(Client.instance.name);

                SendTCPData(_packet);
            }
        }

        public static void AskForSendFile(string filepath, long fileSize)
        {
            IPAddress to = IPAddress.Parse(Client.instance.ip);
            if (FileteleporterTransfert.Network.SendFile.outboundTransfers.ContainsKey(to))
                FileteleporterTransfert.Network.SendFile.outboundTransfers[to] = new FileteleporterTransfert.Network.SendFile.Transfer(
                    filepath,
                    new FileteleporterTransfert.Network.SendFile.Transfer.Machine(Environment.MachineName, "127.0.0.1"),
                    new FileteleporterTransfert.Network.SendFile.Transfer.Machine(ClientHandle.serverMachineName, client.Client.instance.ip),
                    fileSize,
                    0,
                    null,
                    FileteleporterTransfert.Network.SendFile.Transfer.Status.Initialised);
            else
                FileteleporterTransfert.Network.SendFile.outboundTransfers.Add(to,
                    new FileteleporterTransfert.Network.SendFile.Transfer(
                        filepath,
                        new FileteleporterTransfert.Network.SendFile.Transfer.Machine(Environment.MachineName, "127.0.0.1"),
                        new FileteleporterTransfert.Network.SendFile.Transfer.Machine(ClientHandle.serverMachineName, client.Client.instance.ip),
                        fileSize,
                        0,
                        null,
                        FileteleporterTransfert.Network.SendFile.Transfer.Status.Initialised));

            using (Packet _packet = new Packet((int)ClientPackets.askSendFile))
            {
                _packet.Write(Path.GetFileName(filepath));
                _packet.Write(fileSize);

                SendTCPData(_packet);
            }
        }
        #endregion
    }
}
