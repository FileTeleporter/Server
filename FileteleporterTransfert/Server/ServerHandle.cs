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

            //public static void PlayerMovement(int _fromClient, Packet _packet)
            //{
            //    bool[] _inputs = new bool[_packet.ReadInt()];
            //    for (int i = 0; i < _inputs.Length; i++)
            //    {
            //        _inputs[i] = _packet.ReadBool();
            //    }
            //    Quaternion _rotation = _packet.ReadQuaternion();

            //    Server.clients[_fromClient].player.SetInput(_inputs, _rotation);
            //}
        }
    }
}
