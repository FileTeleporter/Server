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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileteleporterTransfert.Tools;

namespace FileteleporterTransfert
{
    class HandleNetController
    {
        Dictionary<NetController.ActionOnTransferer, Action<string[]>> packetHandler;

        public HandleNetController()
        {
            Init();
        }

        public void Init()
        {
            packetHandler = new Dictionary<NetController.ActionOnTransferer, Action<string[]>>()
            {
                { NetController.ActionOnTransferer.testCon, TestConnection},
            };
        }

        public void Handle(byte[] _data)
        {
            try
            {
                string data = Encoding.ASCII.GetString(_data);

                string[] dataSplit = data.Split(new char[] { ':', ';'}, StringSplitOptions.RemoveEmptyEntries);

                NetController.ActionOnTransferer actionOnController = (NetController.ActionOnTransferer)Enum.Parse(typeof(NetController.ActionOnTransferer), dataSplit[0]);

                packetHandler[actionOnController].Invoke(null);
            }
            catch (Exception e)
            {
                EZConsole.WriteLine("error", "Error while handling the packet" + e.ToString());
            }
        }

        public void TestConnection(string[] parameters)
        {
            EZConsole.WriteLine("handle", "Connection ok");
            NetController.instance.SendData(NetController.ActionOnTransferer.testCon);
        }
    }
}
