using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Threading;

namespace FileTeleporterNetController
{

    public class HandleNetController
    {
        Dictionary<NetController.ActionOnController, Action<string[]>> packetHandler;

        private Show show;

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
                { NetController.ActionOnController.transferAck, TransferAcknowledgement },
                { NetController.ActionOnController.showTransfers, ShowTransfers },
                { NetController.ActionOnController.infos, ShowInfos },
            };
            show = new ShowConsole();
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
                show.ShowErrors("Error while parsing packet", $"Error while handling the packet {e.ToString()}");
            }

        }

        public void TestConnection(string[] parameters)
        {
            show.ShowInfos("Testing connection with the transferer", "Connection OK");
        }

        public void DiscoverReturn(string[] parameters)
        {
            show.ShowInfos("discover", $"{parameters[0]} {parameters[1]}");
        }

        public void TransferAcknowledgement(string[] parameters)
        {
            show.ShowTransfers("Inbound transfer", $"Would you like to receive {parameters[0]} with a size of {Math.Round(float.Parse(parameters[1]) / 1048576, 2)}MiB from {parameters[2]}" + Environment.NewLine +
                           "transfer validate or transfer deny");
        }

        public void ShowTransfers(string[] parameters)
        {
            Transfer[] transfers = new Transfer[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                transfers[i] = JsonSerializer.Deserialize<Transfer>(parameters[i]);
            }
            show.ShowTransfers("All the transfers", transfers);
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
            show.ShowInfos("infos", message);
        }

        [Serializable]
        public struct Transfer
        {
            public enum Status
            {
                Initialised,
                Started,
                Finished,
                Denied,
            }

            [Serializable]
            public struct Machine
            {
                public string name { get; set; }
                public string ipAddress { get; set; }

                public Machine(string name, string ip)
                {
                    this.name = name;
                    this.ipAddress = ip;
                }
                public override string ToString()
                {
                    return $"Machine name : {name}, Machine IP : {ipAddress}";
                }
            }

            public string filepath { get; set; }
            public Machine from { get; set; }
            public Machine to { get; set; }
            public long fileSize { get; set; }
            public float progress { get; set; }
            public Status status { get; set; }


            public Transfer(string filepath, Machine from, Machine to, long fileSize, float progress, Status status)
            {
                this.filepath = filepath;
                this.from = from;
                this.to = to;
                this.fileSize = fileSize;
                this.progress = progress;
                this.status = status;
            }
        }
    }
}
