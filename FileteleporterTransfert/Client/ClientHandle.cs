using System.Net;
using FileteleporterTransfert.Network;
using FileteleporterTransfert.Tools;

namespace FileteleporterTransfert.Client
{
    public static class ClientHandle
    {
        public static string ServerMachineName { get; private set; }
        public static void Welcome(Packet packet)
        {
            var msg = packet.ReadString();
            ServerMachineName = packet.ReadString();
            var myId = packet.ReadInt();

            EZConsole.WriteLine("Client", $"Message from server: {msg}");
            Client.instance.myId = myId;
            ClientSend.WelcomeReceived();

            // Now that we have the client's id, connect UDP
            Client.instance.udp.Connect(((IPEndPoint)Client.instance.tcp.socket.Client.LocalEndPoint)!.Port);
        }

        public static void ValidateDenyTransfer(Packet packet)
        {
            bool validate = packet.ReadBool();
            IPAddress to = IPAddress.Parse(Client.instance.ip);
            SendFile.Transfer transfer = SendFile.outboundTransfers[to];

            Client.instance.Disconnect();

            if (validate)
            {
                var sendFile = new SendFile(transfer.filepath, to.ToString(), transfer);
                transfer.status = SendFile.Transfer.Status.Started;
                transfer.sendfile = sendFile;

                sendFile.SendPartAsync();
            }else
            {
                transfer.status = SendFile.Transfer.Status.Denied;
                SendFile.finishedTransfers.Add(transfer);
                SendFile.outboundTransfers.Remove(to);
            }
        }

    }
}
