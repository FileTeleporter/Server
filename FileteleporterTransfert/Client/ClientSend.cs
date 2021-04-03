using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Net.Sockets;
using static client.Client;
using System.Threading.Tasks;

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

        public static void AskForSendFile(string fileName, long fileSize)
        {
            using (Packet _packet = new Packet((int)ClientPackets.askSendFile))
            {
                _packet.Write(fileName);
                _packet.Write(fileSize);

                SendTCPData(_packet);
            }
        }

        private static TCPFileSend tcp;
        public static void SendFileTestPrepare(string ip, Action canReceiveCallBack)
        {
            tcp = new TCPFileSend();
            tcp.Connect(Constants.BUFFER_FOR_FILE, ip, 60589, canReceiveCallBack);
        }

        public static void SendFileTest(byte[] file)
        {
            tcp.SendData(file);
        }

        public static void SendFileDisconnect()
        {
            tcp.Disconnect();
            tcp = null;
        }
        #endregion
    }
}
