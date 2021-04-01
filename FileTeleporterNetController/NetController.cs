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

        public NetController(string ip, int portSend, int portReceive)
        {
            this.ip = ip;
            this.portSend = portSend;
            this.portReceive = portReceive;
            instance = this;
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
            sendSocket.EndConnect(result);
            EZConsole.WriteLine("NetController", "Send socket connected");
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
            });
        }
        #endregion

        #region Receive
        const int BUFFERSIZE = 1024;
        byte[] buffer = new byte[BUFFERSIZE];

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
            receiveSocket = receiveSocket.EndAccept(result);

            receiveSocket.BeginReceive(buffer, 0, BUFFERSIZE, 0, ReadCallback, null);
        }


        public void ReadCallback(IAsyncResult ar)
        {
            try
            {
                int read = receiveSocket.EndReceive(ar);

                if (read <= 0)
                {
                    receiveSocket.Disconnect(true);
                    EZConsole.WriteLine("NetController", $"Connection closed with {receiveSocket.RemoteEndPoint}");
                }

                byte[] _data = new byte[read];
                Array.Copy(buffer, _data, read);

                EZConsole.WriteLine(Encoding.ASCII.GetString(_data));
                receiveSocket.BeginReceive(buffer, 0, BUFFERSIZE, 0, ReadCallback, null);
            }
            catch
            {
                receiveSocket.Disconnect(true);
                EZConsole.WriteLine("NetController", $"Connection closed with {receiveSocket.RemoteEndPoint}");
            }
        }
        #endregion
    }
}
