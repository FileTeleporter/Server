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
using System.Net.Sockets;
using System.Threading;
using FileteleporterTransfert.Tools;

namespace FileteleporterTransfert
{
    class Program
    {
        private static bool isRunning = false;

        static void Main(string[] args)
        {
            EZConsole.AddHeader("Server", "[SERVER]", ConsoleColor.DarkRed, ConsoleColor.White);
            EZConsole.AddHeader("Client", "[CLIENT]", ConsoleColor.Cyan, ConsoleColor.White);
            EZConsole.AddHeader("ThreadManager", "[THREADMANAGER]", ConsoleColor.Red, ConsoleColor.Red);
            EZConsole.AddHeader("NetController", "[NETCONTROLLER]", ConsoleColor.Blue, ConsoleColor.White);
            EZConsole.AddHeader("handle", "[HANDLENETCONTROLLER]", ConsoleColor.Magenta, ConsoleColor.White);
            EZConsole.AddHeader("error", "[ERROR]", ConsoleColor.Red, ConsoleColor.Red);

            NetController netController = new NetController("127.0.0.1", 56236, 56235);


            Console.Title = "Game Server";
            isRunning = true;

            Thread mainThread = new Thread(new ThreadStart(MainThread));
            mainThread.Start();

            server.Server.Start(50, 26950);

            client.Client client = new client.Client("127.0.0.1", "test");
            client.ConnectToServer();
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
