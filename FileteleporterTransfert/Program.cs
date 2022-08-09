using System;
using System.Runtime.InteropServices;
using System.Threading;
using FileteleporterTransfert.Network;
using FileteleporterTransfert.Tools;

namespace FileteleporterTransfert
{
    class Program
    {

        // https://docs.microsoft.com/en-us/windows/console/setconsolectrlhandler?WT.mc_id=DT-MVP-5003978
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(SetConsoleCtrlEventHandler handler, bool add);

        // https://docs.microsoft.com/en-us/windows/console/handlerroutine?WT.mc_id=DT-MVP-5003978
        private delegate bool SetConsoleCtrlEventHandler(CtrlType sig);

        private enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private static bool _isRunning;
        private static Thread mainThread;
        private static bool finish;

        private static void Main(string[] args)
        {
            SetConsoleCtrlHandler(Handler, true);
            EZConsole.AddHeader("Server", "[SERVER]", ConsoleColor.DarkRed, ConsoleColor.White);
            EZConsole.AddHeader("Client", "[CLIENT]", ConsoleColor.Cyan, ConsoleColor.White);
            EZConsole.AddHeader("ThreadManager", "[THREADMANAGER]", ConsoleColor.Red, ConsoleColor.Red);
            EZConsole.AddHeader("NetController", "[NETCONTROLLER]", ConsoleColor.Blue, ConsoleColor.White);
            EZConsole.AddHeader("handle", "[HANDLENETCONTROLLER]", ConsoleColor.Magenta, ConsoleColor.White);
            EZConsole.AddHeader("error", "[ERROR]", ConsoleColor.Red, ConsoleColor.Red);
            EZConsole.AddHeader("Discovery", "[DISCOVERY]", ConsoleColor.Yellow, ConsoleColor.White);
            EZConsole.AddHeader("SendFile", "[SENDFILE]", ConsoleColor.DarkCyan, ConsoleColor.Cyan);
            EZConsole.AddHeader("infos", "[INFOS]", ConsoleColor.Blue, ConsoleColor.White);

            var netController = new NetController("127.0.0.1", 56236, 56235);

            Console.Title = "FileTeleporter Server";
            _isRunning = true;

            mainThread = new Thread(MainThread);
            mainThread.Start();

            Server.Server.Start(50, 50, Constants.MAIN_PORT);

            NetDiscovery.Discover();
        }

        private static void MainThread()
        {
            EZConsole.WriteLine($"Main thread started. Running at {Constants.TICKS_PER_SEC} ticks per second.",
                ConsoleColor.Green);
            var nextLoop = DateTime.Now;
            var finishNext = false;
            while (_isRunning)
            {
                while (nextLoop < DateTime.Now)
                {
                    if (finishNext)
                        Environment.Exit(0); 
                    if (finish)
                    {
                        NetDiscovery.Disconnect();
                        finishNext = true;
                    }

                    // If the time for the next loop is in the past, aka it's time to execute another tick
                    GameLogic.Update(); // Execute game logic

                    nextLoop = nextLoop.AddMilliseconds(Constants
                        .MS_PER_TICK); // Calculate at what point in time the next tick should be executed

                    if (nextLoop > DateTime.Now)
                    {
                        // If the execution time for the next tick is in the future, aka the server is NOT running behind
                        Thread.Sleep(nextLoop - DateTime.Now); // Let the thread sleep until it's needed again.
                    }
                }
            }
        }

        private static bool Handler(CtrlType signal)
        {
            switch (signal)
            {
                case CtrlType.CTRL_BREAK_EVENT:
                case CtrlType.CTRL_C_EVENT:
                case CtrlType.CTRL_LOGOFF_EVENT:
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                case CtrlType.CTRL_CLOSE_EVENT:
                    finish = true;
                    return true;
                default:
                    return false;
            } 
        }
    }
}
