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
using FileTeleporterNetController.Tools;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileTeleporterNetController
{
    class NetController
    {
        public enum ActionOnController
        {
            testCon
        }

        public enum ActionOnTransferer
        {
            testCon
        }


        public static NetController instance;

        private string ip;
        private int portSend;
        private int portReceive;

        private Socket sendSocket;
        private Socket receiveSocket;

        private HandleNetController handleNetController;

        public NetController(string ip, int portSend, int portReceive)
        {
            this.ip = ip;
            this.portSend = portSend;
            this.portReceive = portReceive;
            handleNetController = new HandleNetController();
            instance = this;
            ConnectSendSocket();
            ConnectReceiveSocket();
        }

        #region Send
        public void ConnectSendSocket()
        {
            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(ip), portSend);
            sendSocket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sendSocket.BeginConnect(ipe, ConnectToRemote, null);
        }

        public void ConnectToRemote(IAsyncResult result)
        {
            try
            {
                sendSocket.EndConnect(result);
                EZConsole.WriteLine("NetController", "Send socket connected");
            }
            catch
            {
                ConnectSendSocket();
            }
        }

        /// <summary>
        /// Send an action on the NetController
        /// </summary>
        /// <param name="aController">The action to execute</param>
        /// <param name="parameters">A list of params. Must not contain semicolon</param>
        public void SendData(ActionOnTransferer aController, string[] parameters = null)
        {
            Task.Run(() =>
            {
                try
                {
                    string dataToSend = $"{aController}:";
                    if(parameters != null)
                    {
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            dataToSend += parameters[i];
                            if (i == parameters.Length - 1)
                                dataToSend += ";";
                        }
                    }
                    byte[] bytesSent = Encoding.ASCII.GetBytes(dataToSend);
                    if (sendSocket == null)
                        return;

                    // Send data to the controller
                    sendSocket.Send(bytesSent, bytesSent.Length, 0);
                }
                catch
                {
                    EZConsole.WriteLine("error", "Error while sending the data");
                }
            });
        }
        #endregion

        #region Receive
        const int BUFFERSIZE = 1024;
        byte[] buffer = new byte[BUFFERSIZE];
        private Socket rcvSocket;

        private void ConnectReceiveSocket()
        {

            IPEndPoint ipe = new IPEndPoint(IPAddress.Any, portReceive);
            receiveSocket = new Socket(IPAddress.Any.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            receiveSocket.Bind(ipe);
            receiveSocket.Listen(10);
            receiveSocket.BeginAccept(BeginAccept_CallBack, null);
        }
        private void BeginAccept_CallBack(IAsyncResult result)
        {
            rcvSocket = receiveSocket.EndAccept(result);

            rcvSocket.BeginReceive(buffer, 0, BUFFERSIZE, 0, ReadCallback, null);
        }


        public void ReadCallback(IAsyncResult ar)
        {
            try
            {
                int read = rcvSocket.EndReceive(ar);

                if (read <= 0)
                {
                    rcvSocket.Disconnect(true);
                    EZConsole.WriteLine("NetController", $"Connection closed with {rcvSocket.RemoteEndPoint}");
                }

                byte[] _data = new byte[read];
                Array.Copy(buffer, _data, read);

                handleNetController.Handle(_data);
                rcvSocket.BeginReceive(buffer, 0, BUFFERSIZE, 0, ReadCallback, null);
            }
            catch
            {
                EZConsole.WriteLine("NetController", $"Connection closed with {rcvSocket.RemoteEndPoint}");
                rcvSocket.Shutdown(SocketShutdown.Both);
                rcvSocket.Close();
                receiveSocket.BeginAccept(BeginAccept_CallBack, null);
            }
        }
        #endregion
    }
}
