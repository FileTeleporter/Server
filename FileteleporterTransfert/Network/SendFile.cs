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
using System.Net;

namespace FileteleporterTransfert.Network
{
    class SendFile
    {
        [Serializable]
        public struct Transfer
        {
            public enum Status
            {
                Initialised,
                Started,
                Finished,
            }

            [Serializable]
            public struct Machine
            {
                public string name { get; set; }
                public string ipAddress { get; set; }

                public Machine(string name, string ip)
                {
                    this.name = name;
                    this.ipAddress = ip;
                }
            }

            public string filepath { get; set; }
            public Machine from { get; set; }
            public Machine to { get; set; }
            public float progress { get; set; }
            public Status status { get; set; }

            public SendFile sendfile;

            public Transfer(string filepath, Machine from, Machine to, float progress, SendFile sendfile, Status status)
            {
                this.filepath = filepath;
                this.from = from;
                this.to = to;
                this.progress = progress;
                this.sendfile = sendfile;
                this.status = status;
            }
        }

        public static Dictionary<IPAddress, Transfer> inboundTransfers = new Dictionary<IPAddress, Transfer>();
        public static Dictionary<IPAddress, Transfer> outboundTransfers = new Dictionary<IPAddress, Transfer>();

        string filePath;
        private long fileLength;
        public bool finished;
        private string ip;
        private TCPFileSend tcp;
        internal TCPFileSend Tcp { get => tcp;}


        public SendFile() { }

        public SendFile(string filePath, string ip)
        {
            this.filePath = filePath;
            this.ip = ip;
            finished = false;
        }

        public SendFile(TcpClient client, bool shouldWrite)
        {
            finished = false;
            tcp = new TCPFileSend();
            tcp.Connect(client, shouldWrite, this);
        }

        public void SendPartAsync()
        {
            client.Client.instance.Disconnect();
            Connect();
        }

        private void Connect()
        {
            SendFileTestPrepare(ip, SendAsync);
        }
        private void SendFileTestPrepare(string ip, Action canReceiveCallBack)
        {
            tcp = new TCPFileSend();
            tcp.Connect(ip, canReceiveCallBack, this);
        }

        private void SendAsync()
        {
            SendAsync(null);
        }

        private async void SendAsync(IAsyncResult asyncResult)
        {
            long timeStart = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            await Task.Run(() => SendPart(() =>
            {
                long timeEnd = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                long timeElapsed = timeEnd - timeStart;
                if (timeElapsed == 0)
                    timeElapsed = 1;

                NetController.instance.SendData(NetController.ActionOnController.infos, new string[]
                {
                    $" - Raw length : {fileLength} B",
                    $" - File length : {fileLength / 1048576} MiB",
                    $" - Transmit time : {(float)timeElapsed / 1000} sec",
                    $" - Transmit speed : {(float)(fileLength / ((float)timeElapsed/1000)) / 1048576} MiB/s",

                });
                tcp.Disconnect();
                tcp = null;
                GC.Collect();
            }));
        }

        private async void SendPart(Action callBack)
        {
            byte[] fileSmall;
            FileStream file = File.OpenRead(filePath);
            fileLength = file.Length;

            int lengthToRead = 0;
            if (fileLength < Constants.BUFFER_FOR_FILE)
                lengthToRead = (int)fileLength;
            else
                lengthToRead = Constants.BUFFER_FOR_FILE;

            Task<byte[]> readData = new Task<byte[]>(() => ReadData(file, lengthToRead));
            readData.Start();
            fileSmall = await readData;

            while (!finished)
            {
                if(fileLength < file.Position + Constants.BUFFER_FOR_FILE)
                {
                    if(fileLength == file.Position)
                    {
                        tcp.SendDataSync(fileSmall);
                        finished = true;
                        callBack?.Invoke();
                        return;
                    }
                    else
                    {
                        lengthToRead = Convert.ToInt32(fileLength - file.Position);
                    }
                }

                readData = new Task<byte[]>(() => ReadData(file, lengthToRead));
                readData.Start();

                tcp.SendDataSync(fileSmall);

                fileSmall = await readData;
            }
        }

        private byte[] ReadData(FileStream stream, int lengthToRead)
        {
            byte[] toRead = new byte[lengthToRead];
            stream.Read(toRead, 0, lengthToRead);
            return toRead;
        }

        public class TCPFileSend
        {
            public SendFile sendFile;
            public TcpClient socket;

            private NetworkStream stream;
            private byte[] receiveBuffer;
            private int dataBufferSize = Constants.BUFFER_FOR_FILE;

            private bool shouldWrite = false;

            private Action canReceiveCallBack;

            /// <summary>Attempts to connect to the server via TCP.</summary>
            public void Connect(string ip, Action canReceiveCallBack, SendFile sendFile)
            {
                this.canReceiveCallBack = canReceiveCallBack;
                this.sendFile = sendFile;
                socket = new TcpClient
                {
                    ReceiveBufferSize = dataBufferSize,
                    SendBufferSize = dataBufferSize
                };


                receiveBuffer = new byte[dataBufferSize];
                socket.BeginConnect(ip, Constants.SEND_FILE_PORT, ConnectCallback, socket);
            }

            public void Connect(TcpClient _socket, bool shouldWrite, SendFile sendFile)
            {
                this.shouldWrite = shouldWrite;
                this.sendFile = sendFile;

                socket = _socket;
                socket.ReceiveBufferSize = dataBufferSize;
                socket.SendBufferSize = dataBufferSize;

                stream = socket.GetStream();

                receiveBuffer = new byte[dataBufferSize];

                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
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
            private byte[] _data;
            // pls only use this type of file stream, if use File.Open perfs will suffer
            private FileStream fileStream;
            Task t = null;
            /// <summary>Reads incoming data from the stream.</summary>
            private async void ReceiveCallback(IAsyncResult _result)
            {
                if(fileStream == null && shouldWrite)
                    fileStream = File.OpenWrite("result1.dat");
                if (t != null)
                {
                    await t;
                }
                try
                {
                    int _byteLength = stream.EndRead(_result);
                    if (_byteLength <= 0)
                    {
                        Disconnect();
                        Purge();
                        return;
                    }
                    GC.Collect();
                    _data = new byte[_byteLength];
                    Array.Copy(receiveBuffer, _data, _byteLength);

                    t = new Task(() =>
                    {
                        fileStream.Write(_data, 0, _data.Length);
                    });
                    t.Start();
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                }
                catch (Exception _ex)
                {
                    //Console.WriteLine($"Error receiving TCP data: {_ex}");
                    Disconnect();
                }
            }


            public void Disconnect()
            {
                if (fileStream != null)
                    fileStream.Close();
                fileStream = null;
                if(socket != null)
                {
                    socket.Close();
                    socket = null;
                }
            }

            public void Purge()
            {
                stream = null;
                receiveBuffer = null;
                sendFile.tcp = null;
            }
        }
    }
}
