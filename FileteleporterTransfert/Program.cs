using System;
using System.Threading;
using FileteleporterTransfert.Tools;

namespace FileteleporterTransfert
{
    class Program
    {
        private static bool _isRunning = false;

        private static void Main(string[] args)
        {
            EZConsole.AddHeader("Server", "[SERVER]", ConsoleColor.DarkRed, ConsoleColor.White);
            EZConsole.AddHeader("Client", "[CLIENT]", ConsoleColor.Cyan, ConsoleColor.White);
            EZConsole.AddHeader("ThreadManager", "[THREADMANAGER]", ConsoleColor.Red, ConsoleColor.Red);
            EZConsole.AddHeader("NetController", "[NETCONTROLLER]", ConsoleColor.Blue, ConsoleColor.White);
            EZConsole.AddHeader("handle", "[HANDLENETCONTROLLER]", ConsoleColor.Magenta, ConsoleColor.White);
            EZConsole.AddHeader("error", "[ERROR]", ConsoleColor.Red, ConsoleColor.Red);
            EZConsole.AddHeader("Discovery", "[DISCOVERY]", ConsoleColor.Yellow, ConsoleColor.White);
            EZConsole.AddHeader("SendFile", "[SENDFILE]", ConsoleColor.DarkCyan, ConsoleColor.Cyan);
            EZConsole.AddHeader("infos", "[INFOS]", ConsoleColor.Blue, ConsoleColor.White);
            

            Console.Title = "FileTeleporter Server";
            _isRunning = true;

            var mainThread = new Thread(MainThread);
            mainThread.Start();

            Server.Server.Start(50, 50, 26950);

            Network.NetDiscovery.Discover();
        }

        private static void MainThread()
        {
            EZConsole.WriteLine($"Main thread started. Running at {Constants.TICKS_PER_SEC} ticks per second.", ConsoleColor.Green);
            var nextLoop = DateTime.Now;

            while (_isRunning)
            {
                while (nextLoop < DateTime.Now)
                {
                    // If the time for the next loop is in the past, aka it's time to execute another tick
                    GameLogic.Update(); // Execute game logic

                    nextLoop = nextLoop.AddMilliseconds(Constants.MS_PER_TICK); // Calculate at what point in time the next tick should be executed

                    if (nextLoop > DateTime.Now)
                    {
                        // If the execution time for the next tick is in the future, aka the server is NOT running behind
                        Thread.Sleep(nextLoop - DateTime.Now); // Let the thread sleep until it's needed again.
                    }
                }
            }
        }
    }
}
