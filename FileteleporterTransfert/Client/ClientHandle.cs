using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using FileteleporterTransfert.Network;
using FileteleporterTransfert.Tools;
using FileteleporterTransfert;

namespace client
{
    public class ClientHandle
    {
        public static string serverMachineName { get; private set; }
        public static void Welcome(Packet _packet)
        {
            string _msg = _packet.ReadString();
            serverMachineName = _packet.ReadString();
            int _myId = _packet.ReadInt();

            EZConsole.WriteLine("Client", $"Message from server: {_msg}");
            Client.instance.myId = _myId;
            ClientSend.WelcomeReceived();

            // Now that we have the client's id, connect UDP
            Client.instance.udp.Connect(((IPEndPoint)Client.instance.tcp.socket.Client.LocalEndPoint).Port);
        }

        public static void ValidateDenyTransfer(Packet _packet)
        {
            bool validate = _packet.ReadBool();
            if (validate)
            {
                IPAddress to = IPAddress.Parse(Client.instance.ip);
                SendFile.Transfer transfer = SendFile.outboundTransfers[to];

                SendFile sendFile = new SendFile(transfer.filepath, to.ToString(), transfer);
                transfer.sendfile = sendFile;

                sendFile.SendPartAsync();
            }
        }

    }
}
