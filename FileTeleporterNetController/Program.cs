/*
 * Copyright (C) 2021  Jolan Aklin And Yohan Zbinden

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
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
            EZConsole.AddHeader("cmd", "[CMD]", ConsoleColor.Blue, ConsoleColor.White);
            EZConsole.AddHeader("NetController", "[NETCONTROLLER]", ConsoleColor.Blue, ConsoleColor.White);
            EZConsole.AddHeader("handle", "[HANDLENETCONTROLLER]", ConsoleColor.Magenta, ConsoleColor.White);
            EZConsole.AddHeader("error", "[ERROR]", ConsoleColor.Red, ConsoleColor.Red);


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
                    case "test connection":
                        NetController.instance.SendData(NetController.ActionOnTransferer.testCon);
                        EZConsole.WriteLine("cmd", "Testing connection");
                        enterCommand = null;
                        break;

                    default:
                        EZConsole.WriteLine("cmd", "Unknown command");
                        break;
                }
            };
        }
    }
}
