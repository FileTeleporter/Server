using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using FileteleporterTransfert.Tools;

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

    }
}
