using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using client;
using FileteleporterTransfert.Tools;
using System.Net.Sockets;

namespace FileteleporterTransfert.Network
{
    class SendFile
    {
        string filePath;
        private long fileLength;
        int filePos;
        public bool finished;
        private string ip;
        private int bufferSize;
        private TCPFileSend tcp;


        public SendFile(string filePath, string ip)
        {
            this.filePath = filePath;
            this.ip = ip;
            filePos = 0;
            finished = false;
        }

        public void SendPartAsync(int nbByte)
        {
            bufferSize = nbByte;
            Connect();
        }

        private void Connect()
        {
            SendFileTestPrepare(ip, SendAsync);
        }
        private void SendFileTestPrepare(string ip, Action canReceiveCallBack)
        {
            tcp = new TCPFileSend();
            tcp.Connect(Constants.BUFFER_FOR_FILE, ip, 60589, canReceiveCallBack, this);
        }

        private void SendAsync()
        {
            SendAsync(null);
        }

        private async void SendAsync(IAsyncResult asyncResult)
        {
            long timeStart = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            await Task.Run(() => SendPart());
            long timeEnd = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            long timeElapsed = timeEnd - timeStart;
            EZConsole.WriteLine("SendFile", $"\n" +
                    $"----------------------------------------\n" +
                    $"{fileLength / 1048576} Mio transmited in {timeElapsed} sec\n" +
                    $"With a speed of {(fileLength / timeElapsed) / 1048576} Mio/s\n" +
                    $"----------------------------------------");
            tcp.Disconnect();
            tcp = null;
            GC.Collect();
        }

        private void SendPart()
        {
            byte[] fileSmall = new byte[bufferSize];
            FileStream file = File.OpenRead(filePath);
            fileLength = file.Length;
            while(!finished)
            {
                if(fileLength < filePos + bufferSize)
                {
                    if(fileLength == filePos)
                    {
                        finished = true;
                        return;
                    }else
                    {
                        fileSmall = new byte[fileLength - filePos];
                    }
                }
                file.Read(fileSmall, 0, fileSmall.Length);
                filePos += fileSmall.Length;
                tcp.SendDataSync(fileSmall);
            }
        }

        public class TCPFileSend
        {
            public SendFile sendFile;
            public TcpClient socket;

            private NetworkStream stream;
            private byte[] receiveBuffer;
            private int dataBufferSize;

            private Action canReceiveCallBack;

            /// <summary>Attempts to connect to the server via TCP.</summary>
            public void Connect(int dataBufferSize, string ip, int port, Action canReceiveCallBack, SendFile sendFile)
            {
                this.canReceiveCallBack = canReceiveCallBack;
                this.dataBufferSize = dataBufferSize;
                this.sendFile = sendFile;
                socket = new TcpClient
                {
                    ReceiveBufferSize = dataBufferSize,
                    SendBufferSize = dataBufferSize
                };


                receiveBuffer = new byte[dataBufferSize];
                socket.BeginConnect(ip, port, ConnectCallback, socket);
            }

            /// <summary>Initializes the newly connected client's TCP-related info.</summary>
            private void ConnectCallback(IAsyncResult _result)
            {
                socket.EndConnect(_result);

                if (!socket.Connected)
                {
                    return;
                }

                stream = socket.GetStream();

                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                canReceiveCallBack?.Invoke();
            }

            /// <summary>Sends data to the client via TCP.</summary>
            /// <param name="_packet">The packet to send.</param>
            public void SendData(byte[] file)
            {
                try
                {
                    if (socket != null)
                    {
                        stream.BeginWrite(file, 0, file.Length, sendFile.SendAsync, null); // Send data to server
                    }
                }
                catch (Exception _ex)
                {
                    Console.WriteLine($"Error sending data to server via TCP: {_ex}");
                }
            }

            public void SendDataSync(byte[] file)
            {
                try
                {
                    if (socket != null)
                    {
                        stream.Write(file, 0, file.Length); // Send data to server
                    }
                }
                catch (Exception _ex)
                {
                    Console.WriteLine($"Error sending data to server via TCP: {_ex}");
                }
            }

            /// <summary>Reads incoming data from the stream.</summary>
            private void ReceiveCallback(IAsyncResult _result)
            {
                try
                {
                    int _byteLength = stream.EndRead(_result);
                    if (_byteLength <= 0)
                    {
                        //instance.Disconnect();
                        return;
                    }

                    byte[] _data = new byte[_byteLength];
                    Array.Copy(receiveBuffer, _data, _byteLength);
                    //do something if data is recieved
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                }
                catch
                {
                    Disconnect();
                }
            }


            public void Disconnect()
            {
                //instance.Disconnect();

                stream = null;
                receiveBuffer = null;
                socket = null;
            }
        }
    }
}
