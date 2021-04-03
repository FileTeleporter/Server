using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FileTeleporterNetController.Tools;
using System.Threading;

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
                { NetController.ActionOnController.discoverReturn, DiscoverReturn },
                { NetController.ActionOnController.transferAck, TransferAcknowledgement }
            };
        }

        public void Handle(byte[] _data)
        {
            try
            {
                string data = Encoding.ASCII.GetString(_data);

                string[] dataSplit = data.Split(new char[] { '@', ';'}, StringSplitOptions.RemoveEmptyEntries);

                NetController.ActionOnController actionOnController = (NetController.ActionOnController)Enum.Parse(typeof(NetController.ActionOnController), dataSplit[0]);
                string[] parameters = null;
                if(dataSplit.Length > 1)
                {
                    parameters = new string[dataSplit.Length - 1];
                    Array.Copy(dataSplit, 1, parameters, 0, dataSplit.Length - 1);
                }

                packetHandler[actionOnController].Invoke(parameters);

            }catch (Exception e)
            {
                EZConsole.WriteLine("error", $"Error while handling the packet {e.ToString()}");
            }

        }

        public void TestConnection(string[] parameters)
        {
            EZConsole.WriteLine("handle", "Connection ok");
            Program.EnterCommand();
        }

        public void DiscoverReturn(string[] parameters)
        {
            EZConsole.WriteLine("handle", $"{parameters[0]} {parameters[1]}");
        }

        public void TransferAcknowledgement(string[] parameters)
        {
            EZConsole.WriteLine("handle", $"Would you like to download {parameters[0]} with a size of {long.Parse(parameters[1]) / 1048576}Mio from {parameters[2]}" + Environment.NewLine +
                                "transfer validate or transfer deny");
        }
    }
}
