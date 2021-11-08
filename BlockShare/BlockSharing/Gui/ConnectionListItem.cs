using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.Gui
{
    public class ConnectionListItem
    {
        private FileInfo fileInfo;
        private string fileName;

        public FileInfo FileInfo { get => fileInfo; set => fileInfo = value; }

        public string Connection { get; set; }
        public string FileName
        {
            get
            {
                return fileName;
            }
            set
            {
                fileName = value;
            }
        }
        public string Action { get; set; }
        public double Progress { get; set; }

        public override string ToString()
        {
            string fileName = Utils.TruncateStringStart(FileName, 50);
            if(FileName.Length > fileName.Length)
            {
                fileName = "..." + fileName;
            }
            return $"{Connection}\t{fileName}\t{Action}\t{Progress * 100.0f:F2}%";
        }

        public ConnectionListItem(string connection)
        {
            Connection = connection;
            Action = "Idle";
            FileName = "";
        }
    }
}
