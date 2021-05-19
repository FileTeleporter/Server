using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using System.Drawing;
using FileteleporterTransfert.Tools;
using FileteleporterTransfert;

namespace server
{
    class ServerHandle
    {

        // the int represent the client id then validate or deny in the order of the list
        public static List<int> pendingTransfer = new List<int>();

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
            pendingTransfer.Add(_fromClient);
            NetController.instance.SendData(NetController.ActionOnController.transferAck, new string[] { fileName, fileSize.ToString(), Server.clients[_fromClient].name });
        }

        public static void ReceiveFile(int _fromClient, Packet _packet)
        {
            Console.WriteLine("receiving");
            int length = _packet.ReadInt();
            byte[] file = _packet.ReadBytes(length);
            Console.WriteLine("Finished receiving");
            string fileName = "result.dat";
            using (BinaryWriter writer = new BinaryWriter(File.Open(fileName, FileMode.Append)))
            {
                writer.Write(file);
            }
            Console.WriteLine("finished");
        }
    }
}
