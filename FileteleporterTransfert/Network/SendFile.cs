using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using client;

namespace FileteleporterTransfert.Network
{
    class SendFile
    {
        byte[] file;
        private int fileLength;
        int filePos;
        public bool finished;

        public SendFile(string filePath)
        {
            file = File.ReadAllBytes(filePath);
            fileLength = file.Length;
            filePos = 0;
            finished = false;
        }

        public void SendPartAsync(int nbByte)
        {
            long timeStart = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            Task t = Task.Run(() => SendPart(nbByte));
            t.Wait();
            long timeEnd = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            long timeElapsed = timeEnd - timeStart;
            Console.WriteLine($"\n" +
                    $"----------------------------------------\n" +
                    $"{file.Length/ 1048576} Mio/s transmited in {timeElapsed} sec\n" +
                    $"With a speed of {(file.Length / timeElapsed)/1048576} Mio/s\n" +
                    $"----------------------------------------");
        }

        public void SendPart(int nbByte)
        {
            ClientSend.SendFileTestPrepare();
            byte[] fileSmall = new byte[nbByte];
            while(!finished)
            {
                if(fileLength < filePos + nbByte)
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
