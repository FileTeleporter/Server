using System;
using FileTeleporterNetController.Tools;
using System.Threading;
using System.Runtime.InteropServices;

namespace FileTeleporterNetController
{
    class Program
    {

        private static Action enterCommand;
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

            EZConsole.AddHeader("cmd", "[CMD]", ConsoleColor.Blue, ConsoleColor.White);
            EZConsole.AddHeader("NetController", "[NETCONTROLLER]", ConsoleColor.Blue, ConsoleColor.White);
            EZConsole.AddHeader("handle", "[HANDLENETCONTROLLER]", ConsoleColor.Magenta, ConsoleColor.White);
            EZConsole.AddHeader("error", "[ERROR]", ConsoleColor.Red, ConsoleColor.Red);
            EZConsole.AddHeader("infos", "[INFOS]", ConsoleColor.Blue, ConsoleColor.White);


            NetController netController = new NetController("127.0.0.1", 56235, 56236);
            EnterCommand();
            while(true)
            {
                enterCommand?.Invoke();
                Thread.Sleep(500);
            }
        }

        public static void EnterCommand()
        {
            enterCommand = () =>
            {
                EZConsole.Write(" > ", ConsoleColor.Cyan);
                switch (Console.ReadLine())
                {
                    case string a when a.StartsWith("test connection"):
                        NetController.instance.SendData(NetController.ActionOnTransferer.testCon);
                        EZConsole.WriteLine("cmd", "Testing connection...");
                        enterCommand = null;
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
        }
    }
}
