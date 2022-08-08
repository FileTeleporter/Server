using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using FileteleporterTransfert.Tools;

namespace FileteleporterTransfert.Network
{
    public static class NetDiscovery
    {
        private static UdpClient _udpClient;
        private static readonly Dictionary<IPAddress, string> Machines = new();
        public static void Discover()
        {
            // ip and broadcast
            var ipsAndBc = new Dictionary<IPAddress, IPAddress>();
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Down) continue;
                foreach (var unicastIp in ni.GetIPProperties().UnicastAddresses)
                {
                    if(unicastIp.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipsAndBc.Add(unicastIp.Address, GetBroadcastAddress(unicastIp));
                    }
                }
            }

            _udpClient = new UdpClient();
            _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any.Address, Constants.DISCOVERY_PORT));
            ReceiveBroadcast();
            foreach (var keyValuePair in ipsAndBc)
            {
                var message = Environment.MachineName;
                message += $";{keyValuePair.Key}";
                var bMessage = Encoding.UTF8.GetBytes(message);
                _udpClient.Send(bMessage, bMessage.Length, keyValuePair.Value.ToString(), Constants.DISCOVERY_PORT);
                EZConsole.WriteLine("Discovery", "sent : " + message + " to : " + keyValuePair.Value);
            }
        }

        private static async void ReceiveBroadcast()
        {
            while(true)
            {
                var pcName = "";
                var pcIp = "";
                await Task.Run(() =>
                {
                    var from = new IPEndPoint(0, 0);
                    var recvBuffer = _udpClient.Receive(ref from);
                    var recvMessage = Encoding.UTF8.GetString(recvBuffer) ?? throw new ArgumentNullException("Encoding.UTF8.GetString(recvBuffer)");
                    //if the program receive info about another pc

                    var messageSplited = recvMessage.Split(";");
                    pcName = messageSplited[0];
                    pcIp = messageSplited[1];
                    var ip = IPAddress.Parse(pcIp);
                    if (Machines.ContainsKey(ip)) return;
                    if (pcName == Environment.MachineName) return;
                    Machines.Add(ip, pcName);
                    EZConsole.WriteLine("Discovery", $"received {pcName} {pcIp}");

                });
            }
        }

        public static void GetDiscoveredMachine()
        {
            foreach (var keyValuePair in Machines)
            {
                NetController.instance.SendData(NetController.ActionOnController.DiscoverReturn, new[] { keyValuePair.Key.ToString(), keyValuePair.Value });
            }
        }

        private static IPAddress GetBroadcastAddress(UnicastIPAddressInformation unicastAddress)
        {
            return GetBroadcastAddress(unicastAddress.Address, unicastAddress.IPv4Mask);
        }

        private static IPAddress GetBroadcastAddress(IPAddress address, IPAddress mask)
        {
            var ipAddress = BitConverter.ToUInt32(address.GetAddressBytes(), 0);
            var ipMaskV4 = BitConverter.ToUInt32(mask.GetAddressBytes(), 0);
            var broadCastIpAddress = ipAddress | ~ipMaskV4;

            return new IPAddress(BitConverter.GetBytes(broadCastIpAddress));
        }
    }
}
