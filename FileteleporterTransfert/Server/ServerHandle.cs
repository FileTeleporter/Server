using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using System.Drawing;
using FileteleporterTransfert.Tools;

namespace server
{
    class ServerHandle
    {
        public static void WelcomeReceived(int _fromClient, Packet _packet)
        {
            int _clientIdCheck = _packet.ReadInt();
            string _username = _packet.ReadString();

            EZConsole.WriteLine("Server", $"{Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} connected successfully and is now player {_fromClient} with the name {_username}.");
            if (_fromClient != _clientIdCheck)
            {
                EZConsole.WriteLine("Server", $"Player \"{_username}\" (ID: {_fromClient}) has assumed the wrong client ID ({_clientIdCheck})!");
            }
        }
    }
}
