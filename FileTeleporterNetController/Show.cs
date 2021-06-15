using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTeleporterNetController
{
    public abstract class Show
    {
        public abstract void ShowInfos(string title, string info);

        public abstract void ShowErrors(string title, string errors);

        public abstract void ShowTransfers(string title, HandleNetController.Transfer[] transfers);

        public abstract void ShowTransfers(string title, string transfers);
    }
}
