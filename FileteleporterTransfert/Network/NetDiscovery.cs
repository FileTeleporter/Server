﻿using System;
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
        public static Dictionary<IPAddress, string> machines = new Dictionary<IPAddress, string>();
        private static Dictionary<IPAddress, IPAddress> ipsAndBcs = new Dictionary<IPAddress, IPAddress>();
        public static void Discover()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if(ni.OperationalStatus != OperationalStatus.Down)
                {
                    foreach (UnicastIPAddressInformation unicastIP in ni.GetIPProperties().UnicastAddresses)
                    {
                        if(unicastIP.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            ipsAndBcs.Add(unicastIP.Address, GetBroadcastAddress(unicastIP));
                        }
                    }
                }
            }

            udpClient = new UdpClient();
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any.Address, discoveryPort));
            ReceiveBroadcast();
            SendBroadCast();
        }

        private static async void SendBroadCast()
        {
            foreach (KeyValuePair<IPAddress, IPAddress> keyValuePair in ipsAndBcs)
            {
                string message = Environment.MachineName;
                message += $";{keyValuePair.Key}";
                byte[] bMessage = Encoding.UTF8.GetBytes(message);
                await udpClient.SendAsync(bMessage, bMessage.Length, keyValuePair.Value.ToString(), discoveryPort);
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
                IPAddress pcIP = IPAddress.Parse(pcIp);
                if (!machines.ContainsKey(pcIP) && !ipsAndBcs.ContainsKey(pcIP))
                {
                    machines.Add(IPAddress.Parse(pcIp), pcName);
                    SendBroadCast();
                    EZConsole.WriteLine("Discovery", $"received {pcName} {pcIp}");
                }
            }
        }

        public static void GetDiscoveredMachine()
        {
            foreach (KeyValuePair<IPAddress, string> keyValuePair in machines)
            {
                NetController.instance.SendData(NetController.ActionOnController.discoverReturn, new string[] { keyValuePair.Key.ToString(), keyValuePair.Value });
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
