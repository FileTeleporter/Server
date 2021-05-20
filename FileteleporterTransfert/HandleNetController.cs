using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileteleporterTransfert.Tools;
using FileteleporterTransfert.Network;
using System.IO;

namespace FileteleporterTransfert
{
    class HandleNetController
    {
        Dictionary<NetController.ActionOnTransferer, Action<string[]>> packetHandler;

        public HandleNetController()
        {
            Init();
        }

        public void Init()
        {
            packetHandler = new Dictionary<NetController.ActionOnTransferer, Action<string[]>>()
            {
                { NetController.ActionOnTransferer.testCon, TestConnection},
                { NetController.ActionOnTransferer.discover,  Discover},
                { NetController.ActionOnTransferer.connect,  ConnectToSrv},
                { NetController.ActionOnTransferer.disconnect, Disconnect },
                { NetController.ActionOnTransferer.transfer, Transfer },
                { NetController.ActionOnTransferer.infos, ShowInfos },
            };
        }

        public void Handle(byte[] _data)
        {
            try
            {
                string data = Encoding.ASCII.GetString(_data);

                string[] dataSplit = data.Split(new char[] { '@', ';'}, StringSplitOptions.RemoveEmptyEntries);

                NetController.ActionOnTransferer actionOnController = (NetController.ActionOnTransferer)Enum.Parse(typeof(NetController.ActionOnTransferer), dataSplit[0]);

                string[] parameters = null;
                if (dataSplit.Length > 1)
                {
                    parameters = new string[dataSplit.Length - 1];
                    Array.Copy(dataSplit, 1, parameters, 0, dataSplit.Length - 1);
                }

                packetHandler[actionOnController].Invoke(parameters);
            }
            catch (Exception e)
            {
                EZConsole.WriteLine("error", "Error while handling the packet" + e.ToString());
            }
        }

        public void TestConnection(string[] parameters)
        {
            EZConsole.WriteLine("handle", "Connection ok");
            NetController.instance.SendData(NetController.ActionOnController.testCon);
        }

        public void Discover(string[] parameters)
        {
            NetDiscovery.GetDiscoveredMachine();
        }

        public void ConnectToSrv(string[] parameters)
        {
            if (client.Client.instance == null)
            {
                EZConsole.WriteLine("handle", $"Connect to {parameters[0]}");
                client.Client connectClient = new client.Client(parameters[0], Environment.MachineName);
                client.Client.instance.ConnectToServer();
                NetController.instance.SendData(NetController.ActionOnController.infos, new string[] { $"Client connected to {client.Client.instance.ip} as {Environment.MachineName}" });
            }
            else
            {
                EZConsole.WriteLine("handle", $"Client could not connect to the new server");
                NetController.instance.SendData(NetController.ActionOnController.infos, new string[] { "Client could not connect to the new server" });
            }
        }

        public void Disconnect(string[] parameters)
        {
            if (client.Client.instance != null)
            {
                EZConsole.WriteLine("handle", $"Disconnect from {client.Client.instance.ip}");
                client.Client.instance.Disconnect();
                NetController.instance.SendData(NetController.ActionOnController.infos, new string[] { "Client disconnected" });
            }
        }

        // store the path for the transfer
        public List<string> pendingTransfer = new List<string>();

        public void Transfer(string[] parameters)
        {
            switch(parameters[0])
            {
                case string a when a.StartsWith("validate"):
                    if(server.ServerHandle.pendingTransfer.Count > 0)
                    {
                        server.ServerSend.ValidateDenyTransfer(true);
                        EZConsole.WriteLine("handle", $"Transfer has been validated");
                    }
                    else
                    {
                        EZConsole.WriteLine("handle", $"No pending transfer");
                    }
                    break;
                case string a when a.StartsWith("deny"):
                    if (server.ServerHandle.pendingTransfer.Count > 0)
                    {
                        server.ServerSend.ValidateDenyTransfer(false);
                        EZConsole.WriteLine("handle", $"Transfer has been denied");
                    }
                    else
                    {
                        EZConsole.WriteLine("handle", $"No pending transfer");
                    }
                    break;
                default:
                    pendingTransfer.Add(parameters[0]);
                    // filename, filelength
                    client.ClientSend.AskForSendFile(Path.GetFileName(parameters[0]), new System.IO.FileInfo(parameters[0]).Length);
                    break;
            }
        }

        public void ShowInfos(string[] parameters)
        {
            string message = "";
            for (int i = 0; i < parameters.Length; i++)
            {
                message += parameters[i];
                if (i < parameters.Length - 1)
                    message += Environment.NewLine;
            }
            EZConsole.WriteLine("infos", message);
        }
    }
}
