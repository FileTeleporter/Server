using FileteleporterTransfert.Tools;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileteleporterTransfert
{
    class NetController
    {
        public enum ActionOnController
        {
            testCon,
            discoverReturn,
            transferAck,
            showTransfers,
            infos
        }

        public enum ActionOnTransferer
        {
            testCon,
            discover,
            connect,
            disconnect,
            transfer,
            infos
        }


        public static NetController instance;

        private string ip;
        private int portSend;
        private int portReceive;

        private Socket sendSocket;
        private Socket receiveSocket;
        public HandleNetController handleNetController;


        public NetController(string ip, int portSend, int portReceive)
        {
            this.ip = ip;
            this.portSend = portSend;
            this.portReceive = portReceive;
            handleNetController = new HandleNetController();
            instance = this;
            ConnectReceiveSocket();
            ConnectSendSocket();
        }

        private IPEndPoint sendipe;
        private Action onConnectAction;

        #region Send
        private void ConnectSendSocket()
        {
            sendipe = new IPEndPoint(IPAddress.Parse(ip), portSend);
            sendSocket = new Socket(sendipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sendSocket.BeginConnect(sendipe, ConnectToRemote, null);
        }

        public void ConnectToRemote(IAsyncResult result)
        {
            try
            {
                sendSocket.EndConnect(result);
                EZConsole.WriteLine("NetController", "Send socket connected");
                onConnectAction?.Invoke();
                onConnectAction = null;
            }catch
            {
                ConnectSendSocket();
            }
        }

        /// <summary>
        /// Send an action on the NetController
        /// </summary>
        /// <param name="aController">The action to execute</param>
        /// <param name="parameters">A list of params. Must not contain semicolon</param>
        public void SendData(ActionOnController aController, string[] parameters = null)
        {
            bool part1 = sendSocket.Poll(1000, SelectMode.SelectRead);
            bool part2 = (sendSocket.Available == 0);
            if (part1 && part2)
            {
                onConnectAction = () => { SendData(aController, parameters); };
                sendSocket.Shutdown(SocketShutdown.Both);
                sendSocket.Close();
                sendSocket = null;
                ConnectSendSocket();
            }
            else
            {
                Task.Run(() =>
                {
                    string dataToSend = $"{aController}@";
                    if (parameters != null)
                    {
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            dataToSend += parameters[i];
                            if (i != parameters.Length - 1)
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
        }
        #endregion

        #region Receive
        const int BUFFERSIZE = 1024;
        byte[] buffer = new byte[BUFFERSIZE];
        private Socket rcvSocket;

        private void ConnectReceiveSocket()
        {

            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(ip), portReceive);
            receiveSocket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

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

                Task.Run(() =>
                {
                    handleNetController.Handle(_data);
                });
                rcvSocket.BeginReceive(buffer, 0, BUFFERSIZE, 0, ReadCallback, null);
            }catch
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
