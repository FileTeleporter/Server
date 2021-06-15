using System;
using FileTeleporterNetController.Tools;
using System.Threading;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace FileTeleporterNetController
{
    class Program
    {
        private static bool isRunning = false;
        private static Action readInput;
        static void Main(string[] args)
        {
            EZConsole.WriteLine("Welcome to the fileteleporter net controller", ConsoleColor.Cyan);
            EZConsole.WriteLine("Commandes : " + Environment.NewLine + 
                                "   test connection" + Environment.NewLine + 
                                "   discover" + Environment.NewLine +
                                "   connect <ip address>" + Environment.NewLine +
                                "   disconnect" + Environment.NewLine +
                                "   transfer <file>" + Environment.NewLine +
                                "   transfer validate <ip> <save path>" + Environment.NewLine +
                                "   transfer deny <ip>" + Environment.NewLine +
                                "   transfer list <pending/finished>" + Environment.NewLine +
                                "   transfer getFirstFinished" + Environment.NewLine +
                                "   transfer deleteFirstFinished"
                                , ConsoleColor.Cyan);
            EZConsole.WriteLine("connect to a pc before transfering any files");

            EZConsole.AddHeader("cmd", "[CMD]", ConsoleColor.Cyan, ConsoleColor.White);
            EZConsole.AddHeader("NetController", "[NETCONTROLLER]", ConsoleColor.Yellow, ConsoleColor.White);
            EZConsole.AddHeader("transfers", "[TRANSFERS]", ConsoleColor.Magenta, ConsoleColor.White);
            EZConsole.AddHeader("errors", "[ERRORS]", ConsoleColor.Red, ConsoleColor.Red);
            EZConsole.AddHeader("infos", "[INFOS]", ConsoleColor.Blue, ConsoleColor.White);


            NetController netController = new NetController("127.0.0.1", 56235, 56236);

            readInput = () =>
            {
                switch (Console.ReadLine())
                {
                    case string a when a.StartsWith("test connection"):
                        NetController.instance.SendData(NetController.ActionOnTransferer.testCon);
                        EZConsole.WriteLine("cmd", "Testing connection...");
                        break;

                    case string a when a.StartsWith("discover"):
                        NetController.instance.SendData(NetController.ActionOnTransferer.discover);
                        EZConsole.WriteLine("cmd", "Discovering the network...");
                        break;

                    case string a when a.StartsWith("connect"):
                        string param = a.Split(' ')[1];
                        NetController.instance.SendData(NetController.ActionOnTransferer.connect, new string[] { param });
                        break;
                    case string a when a.StartsWith("disconnect"):
                        NetController.instance.SendData(NetController.ActionOnTransferer.disconnect);
                        break;

                    case string a when a.StartsWith("transfer"):
                        string[] splitString = a.Split(' ');
                        string[] parameters = new string[splitString.Length - 1];
                        Array.Copy(splitString, 1, parameters, 0, parameters.Length);
                        NetController.instance.SendData(NetController.ActionOnTransferer.transfer, parameters);
                        break;

                    default:
                        EZConsole.WriteLine("cmd", "Unknown command");
                        break;
                }
            };

            Task t = new Task(() =>
            {
                while(true)
                {
                    readInput?.Invoke();
                }
            });
            t.Start();

            isRunning = true;

            Thread mainThread = new Thread(new ThreadStart(MainThread));
            mainThread.Start();
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
                    Update(); // Execute game logic

                    _nextLoop = _nextLoop.AddMilliseconds(Constants.MS_PER_TICK); // Calculate at what point in time the next tick should be executed

                    if (_nextLoop > DateTime.Now)
                    {
                        // If the execution time for the next tick is in the future, aka the server is NOT running behind
                        Thread.Sleep(_nextLoop - DateTime.Now); // Let the thread sleep until it's needed again.
                    }
                }
            }
        }

        private static void Update()
        {
            ThreadManager.UpdateMain();
        }
    }
}
