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
                { NetController.ActionOnTransferer.discover,  Discover}
            };
        }

        public void Handle(byte[] _data)
        {
            try
            {
                string data = Encoding.ASCII.GetString(_data);

                string[] dataSplit = data.Split(new char[] { ':', ';'}, StringSplitOptions.RemoveEmptyEntries);

                NetController.ActionOnTransferer actionOnController = (NetController.ActionOnTransferer)Enum.Parse(typeof(NetController.ActionOnTransferer), dataSplit[0]);

                packetHandler[actionOnController].Invoke(null);
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
    }
}
