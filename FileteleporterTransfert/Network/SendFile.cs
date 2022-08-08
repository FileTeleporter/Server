using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using FileteleporterTransfert.Tools;
using System.Net.Sockets;
using System.Net;

namespace FileteleporterTransfert.Network
{
    class SendFile
    {
        [Serializable]
        public class Transfer
        {
            public enum Status
            {
                Initialised,
                Started,
                Finished,
                Denied
            }

            [Serializable]
            public struct Machine
            {
                public string Name { get; set; }
                public string IpAddress { get; set; }

                public Machine(string name, string ip)
                {
                    this.Name = name;
                    this.IpAddress = ip;
                }
            }

            public string filepath { get; set; }
            public Machine from { get; set; }
            public Machine to { get; set; }
            public long fileSize { get; set; }
            public float progress {get; set; }
            public Status status { get; set; }

            public SendFile sendfile;

            public Transfer(string filepath, Machine from, Machine to, long fileSize, float progress, SendFile sendfile, Status status)
            {
                this.filepath = filepath;
                this.from = from;
                this.to = to;
                this.fileSize = fileSize;
                this.progress = progress;
                this.sendfile = sendfile;
                this.status = status;
            }
        }
        //                       From
        public static Dictionary<IPAddress, Transfer> inboundTransfers = new();
        //                       To
        public static Dictionary<IPAddress, Transfer> outboundTransfers = new();

        public static List<Transfer> finishedTransfers = new();

        string filePath;
        private long fileLength;
        public bool finished;
        private string ip;
        private TCPFileSend tcp;
        internal TCPFileSend Tcp { get => tcp;}

        private Transfer currentTransfer;

        public SendFile() { }

        public SendFile(string filePath, string ip, Transfer transfer)
        {
            this.filePath = filePath;
            this.ip = ip;
            currentTransfer = transfer;
            finished = false;
        }

        public SendFile(TcpClient client, bool shouldWrite, Transfer transfer)
        {
            currentTransfer = transfer;
            finished = false;
            tcp = new TCPFileSend();
            tcp.Connect(client, shouldWrite, this);
        }

        public void SendPartAsync()
        {
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
            var timeStart = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            await Task.Run(() => SendPart(() =>
            {
                var timeEnd = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                var timeElapsed = timeEnd - timeStart;
                if (timeElapsed == 0)
                    timeElapsed = 1;

                NetController.instance.SendData(NetController.ActionOnController.Infos, new[]
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
            var file = File.OpenRead(filePath);
            fileLength = file.Length;

            var lengthToRead = 0;
            if (fileLength < Constants.BUFFER_FOR_FILE)
                lengthToRead = (int)fileLength;
            else
                lengthToRead = Constants.BUFFER_FOR_FILE;

            var readData = new Task<byte[]>(() => ReadData(file, lengthToRead));
            readData.Start();
            fileSmall = await readData;

            while (!finished)
            {
                currentTransfer.progress = (float)file.Position / file.Length;
                if(fileLength < file.Position + Constants.BUFFER_FOR_FILE)
                {
                    if(fileLength == file.Position)
                    {
                        tcp.SendDataSync(fileSmall);

                        currentTransfer.status = Transfer.Status.Finished;
                        finishedTransfers.Add(currentTransfer);
                        outboundTransfers.Remove(IPAddress.Parse(currentTransfer.to.IpAddress));

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
            var toRead = new byte[lengthToRead];
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

            public void Connect(TcpClient socket, bool shouldWrite, SendFile sendFile)
            {
                this.shouldWrite = shouldWrite;
                this.sendFile = sendFile;

                this.socket = socket;
                this.socket.ReceiveBufferSize = dataBufferSize;
                this.socket.SendBufferSize = dataBufferSize;

                stream = this.socket.GetStream();

                receiveBuffer = new byte[dataBufferSize];

                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }

            /// <summary>Initializes the newly connected client's TCP-related info.</summary>
            private void ConnectCallback(IAsyncResult result)
            {
                socket.EndConnect(result);

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
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending data to server via TCP: {ex}");
                }
            }

            /// <summary>Reads incoming data from the stream.</summary>
            private byte[] data;
            // pls only use this type of file stream, if use File.Open perfs will suffer
            private FileStream fileStream;
            Task t;
            /// <summary>Reads incoming data from the stream.</summary>
            private async void ReceiveCallback(IAsyncResult result)
            {
                if(fileStream == null && shouldWrite)
                {
                    if(File.Exists(sendFile.currentTransfer.filepath))
                    {
                        File.Delete(sendFile.currentTransfer.filepath);
                    }
                    fileStream = File.OpenWrite(sendFile.currentTransfer.filepath);
                }
                if (t != null)
                {
                    await t;
                }
                if(shouldWrite)
                {
                    if (fileStream != null)
                    {
                        sendFile.currentTransfer.progress =
                            (float)fileStream.Position / sendFile.currentTransfer.fileSize;
                        if (fileStream.Position == sendFile.currentTransfer.fileSize)
                        {
                            sendFile.currentTransfer.status = Transfer.Status.Finished;
                            finishedTransfers.Add(sendFile.currentTransfer);
                            inboundTransfers.Remove(IPAddress.Parse(sendFile.currentTransfer.from.IpAddress));
                        }
                    }
                }
                try
                {
                    var byteLength = stream.EndRead(result);
                    if (byteLength <= 0)
                    {
                        Disconnect();
                        Purge();
                        return;
                    }
                    GC.Collect();
                    data = new byte[byteLength];
                    Array.Copy(receiveBuffer, data, byteLength);


                    t = new Task(() =>
                    {
                        fileStream.Write(data, 0, data.Length);
                    });
                    t.Start();
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                }
                catch (Exception ex)
                {
                    //Console.WriteLine($"Error receiving TCP data: {_ex}");
                    Disconnect();
                }
            }


            public void Disconnect()
            {
                fileStream?.Close();
                fileStream = null;
                if (socket == null) return;
                socket.Close();
                socket = null;
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
