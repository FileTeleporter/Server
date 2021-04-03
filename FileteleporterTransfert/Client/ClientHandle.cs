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
        public static void Welcome(Packet _packet)
        {
            string _msg = _packet.ReadString();
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
            if(validate)
            {
                SendFile sendFile = new SendFile(NetController.instance.handleNetController.pendingTransfer[0], client.Client.instance.ip);
                sendFile.SendPartAsync(Constants.BUFFER_FOR_FILE);
            }
            NetController.instance.handleNetController.pendingTransfer.RemoveAt(0);
        }

    }
}
