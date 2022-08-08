using System;
using System.IO;
using System.Net;
using FileteleporterTransfert.Network;
using static FileteleporterTransfert.Client.Client;

namespace FileteleporterTransfert.Client
{
    public class ClientSend
    {
        #region Send data
        /// <summary>Sends a packet to the server via TCP.</summary>
        /// <param name="packet">The packet to send to the sever.</param>
        private static void SendTcpData(Packet packet)
        {
            packet.WriteLength();
            instance.tcp.SendData(packet);
        }

        /// <summary>Sends a packet to the server via UDP.</summary>
        /// <param name="packet">The packet to send to the sever.</param>
        private static void SendUdpData(Packet packet)
        {
            packet.WriteLength();
            instance.udp.SendData(packet);
        }
        #endregion

        #region Packets
        /// <summary>Lets the server know that the welcome message was received.</summary>
        public static void WelcomeReceived()
        {
            using var packet = new Packet((int)ClientPackets.WelcomeReceived);
            packet.Write(instance.myId);
            packet.Write(instance.name);

            SendTcpData(packet);
        }

        public static void AskForSendFile(string filepath, long fileSize)
        {
            IPAddress to = IPAddress.Parse(instance.ip);
            if (SendFile.outboundTransfers.ContainsKey(to))
                SendFile.outboundTransfers[to] = new SendFile.Transfer(
                    filepath,
                    new SendFile.Transfer.Machine(Environment.MachineName, "127.0.0.1"),
                    new SendFile.Transfer.Machine(ClientHandle.ServerMachineName, instance.ip),
                    fileSize,
                    0,
                    null,
                    SendFile.Transfer.Status.Initialised);
            else
                SendFile.outboundTransfers.Add(to,
                    new SendFile.Transfer(
                        filepath,
                        new SendFile.Transfer.Machine(Environment.MachineName, "127.0.0.1"),
                        new SendFile.Transfer.Machine(ClientHandle.ServerMachineName, instance.ip),
                        fileSize,
                        0,
                        null,
                        SendFile.Transfer.Status.Initialised));

            using var packet = new Packet((int)ClientPackets.AskSendFile);
            packet.Write(Path.GetFileName(filepath));
            packet.Write(fileSize);

            SendTcpData(packet);
        }
        #endregion
    }
}
