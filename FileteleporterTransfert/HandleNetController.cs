using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileteleporterTransfert.Tools;
using FileteleporterTransfert.Network;

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
            };
        }

        public void Handle(byte[] _data)
        {
            try
            {
                string data = Encoding.ASCII.GetString(_data);

                string[] dataSplit = data.Split(new char[] { ':', ';'}, StringSplitOptions.RemoveEmptyEntries);

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
            if(NetDiscovery.udpClient == null)
                NetDiscovery.Discover();
        }

        public void ConnectToSrv(string[] parameters)
        {
            client.Client client = new client.Client(parameters[0], Environment.MachineName);
            client.ConnectToServer();
        }
    }
}
