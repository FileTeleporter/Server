using System;
using FileTeleporterNetController.Tools;

namespace FileTeleporterNetController
{
    class Program
    {
        static void Main(string[] args)
        {
            EZConsole.WriteLine("Welcome to the fileteleporter net controller", ConsoleColor.Cyan);
            EZConsole.AddHeader("cmd", "[CMD]", ConsoleColor.Blue, ConsoleColor.White);

            NetController netController = new NetController("127.0.0.1", 56235, 56236);
            NetController.instance.ConnectSendSocket();

            while(true)
            {
                EZConsole.Write(" > ", ConsoleColor.Cyan);
                switch(Console.ReadLine())
                {
                    case "test connection":
                        NetController.instance.SendData(NetController.ActionOnTransferer.testCon);
                        EZConsole.WriteLine("cmd", "Testing connection");
                        break;

                    default:
                        EZConsole.WriteLine("cmd", "Unknown command");
                        break;
                }
            }
        }
    }
}
