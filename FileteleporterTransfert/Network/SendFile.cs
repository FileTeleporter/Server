using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using client;
using FileteleporterTransfert.Tools;

namespace FileteleporterTransfert.Network
{
    class SendFile
    {
        byte[] file;
        private int fileLength;
        int filePos;
        public bool finished;
        private string ip;
        private int bufferSize;

        public SendFile(string filePath, string ip)
        {
            file = File.ReadAllBytes(filePath);
            this.ip = ip;
            fileLength = file.Length;
            filePos = 0;
            finished = false;
        }

        public async void SendPartAsync(int nbByte)
        {
            bufferSize = nbByte;
            Connect();
        }

        public void Connect()
        {
            ClientSend.SendFileTestPrepare(ip, SendAsync);
        }

        public async void SendAsync()
        {
            long timeStart = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            await Task.Run(() => SendPart());
            long timeEnd = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            long timeElapsed = timeEnd - timeStart;
            EZConsole.WriteLine("SendFile", $"\n" +
                    $"----------------------------------------\n" +
                    $"{file.Length / 1048576} Mio transmited in {timeElapsed} sec\n" +
                    $"With a speed of {(file.Length / timeElapsed) / 1048576} Mio/s\n" +
                    $"----------------------------------------");
        }

        private void SendPart()
        {
            byte[] fileSmall = new byte[bufferSize];
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
                for (int i = 0; i < fileSmall.Length; i++)
                {
                    fileSmall[i] = file[i + filePos];
                }
                filePos += fileSmall.Length;
                //ClientSend.SendFile(fileSmall, fileSmall.Length);
                ClientSend.SendFileTest(fileSmall);
            }
            ClientSend.SendFileDisconnect();
        }
    }
}
