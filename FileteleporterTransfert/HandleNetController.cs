using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileteleporterTransfert.Tools;
using FileteleporterTransfert.Network;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using FileteleporterTransfert.Client;

namespace FileteleporterTransfert
{
    public class HandleNetController
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
                { NetController.ActionOnTransferer.TestCon, TestConnection},
                { NetController.ActionOnTransferer.Discover,  Discover},
                { NetController.ActionOnTransferer.Connect,  ConnectToSrv},
                { NetController.ActionOnTransferer.Disconnect, Disconnect },
                { NetController.ActionOnTransferer.Transfer, Transfer },
                { NetController.ActionOnTransferer.Infos, ShowInfos },
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
            NetController.instance.SendData(NetController.ActionOnController.TestCon);
        }

        public void Discover(string[] parameters)
        {
            NetDiscovery.GetDiscoveredMachine();
        }

        public void ConnectToSrv(string[] parameters)
        {
            if (Client.Client.instance == null)
            {
                EZConsole.WriteLine("handle", $"Connect to {parameters[0]}");
                Client.Client connectClient = new Client.Client(parameters[0], Environment.MachineName);
                Client.Client.instance.ConnectToServer();
                NetController.instance.SendData(NetController.ActionOnController.Infos, new string[] { $"Client connected to {Client.Client.instance.ip} as {Environment.MachineName}" });
            }
            else
            {
                EZConsole.WriteLine("handle", $"Client could not connect to the new server");
                NetController.instance.SendData(NetController.ActionOnController.Infos, new string[] { "Client could not connect to the new server" });
            }
        }

        public void Disconnect(string[] parameters)
        {
            if (Client.Client.instance != null)
            {
                EZConsole.WriteLine("handle", $"Disconnect from {Client.Client.instance.ip}");
                Client.Client.instance.Disconnect();
            }
        }

        public void Transfer(string[] parameters)
        {
            if (parameters.Length < 0)
                return;
            switch(parameters[0])
            {
                // usage : transfer validate <ip> <dest. directory>
                case "validate":
                    if(parameters.Length != 3)
                    {
                        NetController.instance.SendData(NetController.ActionOnController.Infos, new string[] { $"Usage : transfer validate <ip> <dest. directory>" });
                        return;
                    }
                    if(SendFile.inboundTransfers.Count > 0)
                    {
                        IPAddress iPAddress = IPAddress.Parse(parameters[1]);
                        SendFile.Transfer transfer;
                        if (SendFile.inboundTransfers.ContainsKey(iPAddress))
                        {
                            transfer = SendFile.inboundTransfers[iPAddress];
                            transfer.status = SendFile.Transfer.Status.Started;
                        }
                        else
                        {
                            NetController.instance.SendData(NetController.ActionOnController.Infos, new string[] { $"The IP address {iPAddress} does not match an inbound transfer" });
                            return;
                        }

                        if (Directory.Exists(Path.GetDirectoryName(parameters[2])))
                            transfer.filepath = parameters[2];
                        else
                        {
                            NetController.instance.SendData(NetController.ActionOnController.Infos, new string[] { $"The path {parameters[2]} does not appear to be correct" });
                            return;
                        }
                        if (!Server.ServerSend.ValidateDenyTransfer(true, iPAddress))
                        {
                            NetController.instance.SendData(NetController.ActionOnController.Infos, new string[] { "Ip not found" });
                            return;
                        }
                        NetController.instance.SendData(NetController.ActionOnController.Infos, new string[] { "Transfer has been validated" });
                        EZConsole.WriteLine("handle", $"Transfer has been validated");
                    }
                    else
                    {
                        NetController.instance.SendData(NetController.ActionOnController.Infos, new string[] { "There is no transfer to validate" });
                        EZConsole.WriteLine("handle", $"No pending transfer");
                    }
                    break;
                case "deny":
                    if (parameters.Length != 2)
                    {
                        NetController.instance.SendData(NetController.ActionOnController.Infos, new string[] { $"Usage : transfer deny <ip>" });
                        return;
                    }
                    if (SendFile.inboundTransfers.Count > 0)
                    {
                        IPAddress iPAddress = IPAddress.Parse(parameters[1]);
                        SendFile.Transfer transfer;
                        if (SendFile.inboundTransfers.ContainsKey(iPAddress))
                        {
                            transfer = SendFile.inboundTransfers[iPAddress];
                            transfer.status = SendFile.Transfer.Status.Denied;
                            SendFile.finishedTransfers.Add(transfer);
                            SendFile.inboundTransfers.Remove(iPAddress);
                        }
                        else
                        {
                            NetController.instance.SendData(NetController.ActionOnController.Infos, new string[] { $"The IP address {iPAddress} does not match an inbound transfer" });
                            return;
                        }
                        if (!Server.ServerSend.ValidateDenyTransfer(false, iPAddress))
                            NetController.instance.SendData(NetController.ActionOnController.Infos, new string[] { "Ip not found" });
                        NetController.instance.SendData(NetController.ActionOnController.Infos, new string[] { "Transfer has been denied" });
                        EZConsole.WriteLine("handle", $"Transfer has been denied");
                    }
                    else
                    {
                        NetController.instance.SendData(NetController.ActionOnController.Infos, new string[] { "There is no transfer to deny" });
                        EZConsole.WriteLine("handle", $"No pending transfer");
                    }
                    break;
                case "list":
                    if (parameters.Length != 2)
                    {
                        NetController.instance.SendData(NetController.ActionOnController.Infos, new string[] { $"Usage : transfer list <pending/finished>" });
                        return;
                    }
                    switch (parameters[1])
                    {
                        case "finished":
                            if (SendFile.finishedTransfers.Count > 0)
                            {
                                string[] transfers = new string[SendFile.finishedTransfers.Count];
                                int i = 0;
                                foreach (SendFile.Transfer item in SendFile.finishedTransfers)
                                {
                                    transfers[i] = JsonSerializer.Serialize(item);
                                    i++;
                                }
                                NetController.instance.SendData(NetController.ActionOnController.ShowTransfers, transfers);
                            }
                            else
                            {
                                NetController.instance.SendData(NetController.ActionOnController.Infos, new string[] { "No transfers to show" });
                            }
                            break;
                        case "pending":
                            if (SendFile.inboundTransfers.Count > 0 || SendFile.outboundTransfers.Count > 0)
                            {
                                string[] transfers = new string[SendFile.inboundTransfers.Count + SendFile.outboundTransfers.Count];
                                int i = 0;
                                foreach (SendFile.Transfer item in SendFile.inboundTransfers.Values)
                                {
                                    transfers[i] = JsonSerializer.Serialize(item);
                                    i++;
                                }
                                foreach (SendFile.Transfer item in SendFile.outboundTransfers.Values)
                                {
                                    transfers[i] = JsonSerializer.Serialize(item);
                                    i++;
                                }
                                NetController.instance.SendData(NetController.ActionOnController.ShowTransfers, transfers);
                            }
                            else
                            {
                                NetController.instance.SendData(NetController.ActionOnController.Infos, new string[] { "No transfers to show" });
                            }
                            break;
                        default:
                            NetController.instance.SendData(NetController.ActionOnController.Infos, new string[] { $"Usage : transfer list <pending/finished>" });
                            break;
                    }
                    break;
                case "getFirstFinished":
                    if (parameters.Length != 1)
                    {
                        NetController.instance.SendData(NetController.ActionOnController.Infos, new string[] { $"Usage : transfer get first finished" });
                        return;
                    }
                    if(SendFile.finishedTransfers.Count > 0)
                        NetController.instance.SendData(NetController.ActionOnController.ShowTransfers, new string[] { JsonSerializer.Serialize(SendFile.finishedTransfers[0]) });
                    else
                        NetController.instance.SendData(NetController.ActionOnController.Infos, new string[] { "No transfers to show" });
                    break;
                case "deleteFirstFinished":
                    if (parameters.Length != 1)
                    {
                        NetController.instance.SendData(NetController.ActionOnController.Infos, new string[] { $"Usage : transfer delete first finished" });
                        return;
                    }
                    if (SendFile.finishedTransfers.Count > 0)
                    {
                        SendFile.finishedTransfers.RemoveAt(0);
                        NetController.instance.SendData(NetController.ActionOnController.Infos, new string[] { "First finished transfer deleted" });
                    }
                    else
                        NetController.instance.SendData(NetController.ActionOnController.Infos, new string[] { "No transfers to delete" });
                    break;
                default:
                    if (parameters.Length != 1)
                    {
                        NetController.instance.SendData(NetController.ActionOnController.Infos, new string[] { $"Usage : transfer <file's fullpath>" });
                        return;
                    }
                    if (Directory.Exists(Path.GetDirectoryName(parameters[0])))
                    {
                        // filepath, filelength
                        ClientSend.AskForSendFile(parameters[0], new System.IO.FileInfo(parameters[0]).Length);
                    }
                    else
                    {
                        NetController.instance.SendData(NetController.ActionOnController.Infos, new string[] { $"The path {parameters[0]} does not appear to be correct" });
                        return;
                    }
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
