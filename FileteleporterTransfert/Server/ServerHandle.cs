using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using System.Drawing;
using FileteleporterTransfert.Tools;
using FileteleporterTransfert;
using FileteleporterTransfert.Network;
using System.Net;

namespace server
{
    class ServerHandle
    {

        public static void WelcomeReceived(int _fromClient, Packet _packet)
        {
            int _clientIdCheck = _packet.ReadInt();
            string _username = _packet.ReadString();
            Server.clients[_fromClient].name = _username;

            EZConsole.WriteLine("Server", $"{Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} connected successfully and is now player {_fromClient} with the name {_username}.");
            if (_fromClient != _clientIdCheck)
            {
                EZConsole.WriteLine("Server", $"Player \"{_username}\" (ID: {_fromClient}) has assumed the wrong client ID ({_clientIdCheck})!");
            }
        }

        public static void AskSendFile(int _fromClient, Packet _packet)
        {
            string fileName = _packet.ReadString();
            long fileSize = _packet.ReadLong();

            IPAddress from = ((IPEndPoint)Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint).Address;

            if (SendFile.inboundTransfers.ContainsKey(from))
                SendFile.inboundTransfers[from] = new SendFile.Transfer(
                    null,
                    new SendFile.Transfer.Machine(Server.clients[_fromClient].name, from.ToString()),
                    new SendFile.Transfer.Machine(Environment.MachineName, "127.0.0.1"),
                    fileSize,
                    0,
                    null,
                    SendFile.Transfer.Status.Initialised);
            else
                SendFile.inboundTransfers.Add(from,
                    new SendFile.Transfer(
                        null,
                        new SendFile.Transfer.Machine(Server.clients[_fromClient].name, from.ToString()),
                        new SendFile.Transfer.Machine(Environment.MachineName, "127.0.0.1"),
                        fileSize,
                        0,
                        null,
                        SendFile.Transfer.Status.Initialised));

            NetController.instance.SendData(NetController.ActionOnController.transferAck, new string[] { fileName, fileSize.ToString(), Server.clients[_fromClient].name });
        }
    }
}
