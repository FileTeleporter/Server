using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileTeleporterNetController.Tools;

namespace FileTeleporterNetController
{

    class HandleNetController
    {
        Dictionary<NetController.ActionOnController, Action<string[]>> packetHandler;

        public HandleNetController()
        {
            Init();
        }

        public void Init()
        {
            packetHandler = new Dictionary<NetController.ActionOnController, Action<string[]>>()
                {
                    { NetController.ActionOnController.testCon, TestConnection},
                };
        }

        public void Handle(byte[] _data)
        {
            string data = Encoding.ASCII.GetString(_data);

            string[] dataSplit = data.Split(new char[] { ':', ';'}, StringSplitOptions.RemoveEmptyEntries);

            NetController.ActionOnController actionOnController = (NetController.ActionOnController)Enum.Parse(typeof(NetController.ActionOnController), dataSplit[0]);

            packetHandler[actionOnController].Invoke(null);

        }

        public void TestConnection(string[] parameters)
        {
            EZConsole.WriteLine("handle", "Connection ok");
            Program.EnterCommand();
        }
    }
}
