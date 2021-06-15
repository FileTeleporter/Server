using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileTeleporterNetController.Tools;

namespace FileTeleporterNetController
{
    class ShowConsole : Show
    {
        public override void ShowInfos(string title, string info)
        {
            EZConsole.WriteLine("infos", info);
        }

        public override void ShowErrors(string title, string error)
        {
            EZConsole.WriteLine("errors", error);
        }

        public override void ShowTransfers(string title, HandleNetController.Transfer[] transfers)
        {
            string text = "Current transfers : " + Environment.NewLine;
            for (int i = 0; i < transfers.Length; i++)
            {
                text += $" - FROM : {transfers[i].from} | TO : {transfers[i].to} | FILEPATH : {transfers[i].filepath} | FILESIZE : {transfers[i].fileSize}| STATUS : {transfers[i].status} | PROGRESS : {transfers[i].progress * 100}%";
                if (i < transfers.Length - 1)
                    text += Environment.NewLine;
            }
            EZConsole.WriteLine("transfers", text);
        }

        public override void ShowTransfers(string title, string transfer)
        {
            EZConsole.WriteLine("transfers", transfer);
        }

    }
}
