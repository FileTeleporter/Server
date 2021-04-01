using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FileteleporterTransfert.Tools;

namespace FileteleporterTransfert.Network
{
    public static class NetDiscovery
    {
        private static int discoveryPort = 56237;
        public static UdpClient udpClient;
        public static void Discover()
        {
            // ip thand broadcast
            Dictionary<IPAddress, IPAddress> ipsAndBc = new Dictionary<IPAddress, IPAddress>();
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if(ni.OperationalStatus != OperationalStatus.Down)
                {
                    foreach (UnicastIPAddressInformation unicastIP in ni.GetIPProperties().UnicastAddresses)
                    {
                        if(unicastIP.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            ipsAndBc.Add(unicastIP.Address, GetBroadcastAddress(unicastIP));
                        }
                    }
                }
            }

            udpClient = new UdpClient();
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any.Address, discoveryPort));
            ReceiveBroadcast();
            foreach (KeyValuePair<IPAddress,IPAddress> keyValuePair in ipsAndBc)
            {
                string message = Environment.MachineName;
                message += $";{keyValuePair.Key}";
                byte[] bMessage = Encoding.UTF8.GetBytes(message);
                udpClient.Send(bMessage, bMessage.Length, keyValuePair.Value.ToString(), discoveryPort);
                EZConsole.WriteLine("Discovery", "sent : " + message + " to : " + keyValuePair.Value.ToString());
            }
        }

        private static async void ReceiveBroadcast()
        {
            while(true)
            {
                string pcName = "";
                string pcIp = "";
                await Task.Run(() =>
                {
                    IPEndPoint from = new IPEndPoint(0, 0);
                    var recvBuffer = udpClient.Receive(ref from);
                    string recvMessage = Encoding.UTF8.GetString(recvBuffer);
                    //if the programm receive info about antoher pc

                    string[] messageSplited = recvMessage.Split(";");
                    pcName = messageSplited[0];
                    pcIp = messageSplited[1];
                });
                NetController.instance.SendData(NetController.ActionOnController.discoverReturn, new string[] { pcName, pcIp });
                EZConsole.WriteLine("Discovery", $"received {pcName} {pcIp}");
            }
        }

        public static IPAddress GetBroadcastAddress(UnicastIPAddressInformation unicastAddress)
        {
            return GetBroadcastAddress(unicastAddress.Address, unicastAddress.IPv4Mask);
        }

        public static IPAddress GetBroadcastAddress(IPAddress address, IPAddress mask)
        {
            uint ipAddress = BitConverter.ToUInt32(address.GetAddressBytes(), 0);
            uint ipMaskV4 = BitConverter.ToUInt32(mask.GetAddressBytes(), 0);
            uint broadCastIpAddress = ipAddress | ~ipMaskV4;

            return new IPAddress(BitConverter.GetBytes(broadCastIpAddress));
        }
    }
}
