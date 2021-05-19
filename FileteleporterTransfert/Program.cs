using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FileteleporterTransfert.Network;
using FileteleporterTransfert.Tools;

namespace FileteleporterTransfert
{
    class Program
    {
        private static bool isRunning = false;
        private static TcpListener tcpListener;

        static void Main(string[] args)
        {
            EZConsole.AddHeader("Server", "[SERVER]", ConsoleColor.DarkRed, ConsoleColor.White);
            EZConsole.AddHeader("Client", "[CLIENT]", ConsoleColor.Cyan, ConsoleColor.White);
            EZConsole.AddHeader("ThreadManager", "[THREADMANAGER]", ConsoleColor.Red, ConsoleColor.Red);
            EZConsole.AddHeader("NetController", "[NETCONTROLLER]", ConsoleColor.Blue, ConsoleColor.White);
            EZConsole.AddHeader("handle", "[HANDLENETCONTROLLER]", ConsoleColor.Magenta, ConsoleColor.White);
            EZConsole.AddHeader("error", "[ERROR]", ConsoleColor.Red, ConsoleColor.Red);
            EZConsole.AddHeader("Discovery", "[DISCOVERY]", ConsoleColor.Yellow, ConsoleColor.White);
            EZConsole.AddHeader("SendFile", "[SENDFILE]", ConsoleColor.DarkCyan, ConsoleColor.Cyan);

            NetController netController = new NetController("127.0.0.1", 56236, 56235);


            Console.Title = "Game Server";
            isRunning = true;

            Thread mainThread = new Thread(new ThreadStart(MainThread));
            mainThread.Start();

            server.Server.Start(50, /*26950*/56237);

            Network.NetDiscovery.Discover();

            // start listening for a file transfer
            tcpListener = new TcpListener(IPAddress.Any, 60589);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(TCPConnectCallback, null);
        }

        // multiple transfer at the same times might corrupt files need to check the code below
        private static server.Client.TCPFileSend tcpFileSend;
        private static void TCPConnectCallback(IAsyncResult _result)
        {
            Task.Run(() =>
            {
                TcpClient _client = tcpListener.EndAcceptTcpClient(_result);
                tcpListener.BeginAcceptTcpClient(TCPConnectCallback, null);
                EZConsole.WriteLine("SendFile", $"Incoming connection from {_client.Client.RemoteEndPoint}...");

                tcpFileSend = new server.Client.TCPFileSend();
                tcpFileSend.Connect(_client, Constants.BUFFER_FOR_FILE);
            });
        }

        private static void MainThread()
        {
            EZConsole.WriteLine($"Main thread started. Running at {Constants.TICKS_PER_SEC} ticks per second.", ConsoleColor.Green);
            DateTime _nextLoop = DateTime.Now;

            while (isRunning)
            {
                while (_nextLoop < DateTime.Now)
                {
                    // If the time for the next loop is in the past, aka it's time to execute another tick
                    GameLogic.Update(); // Execute game logic

                    _nextLoop = _nextLoop.AddMilliseconds(Constants.MS_PER_TICK); // Calculate at what point in time the next tick should be executed

                    if (_nextLoop > DateTime.Now)
                    {
                        // If the execution time for the next tick is in the future, aka the server is NOT running behind
                        Thread.Sleep(_nextLoop - DateTime.Now); // Let the thread sleep until it's needed again.
                    }
                }
            }
        }
    }
}
