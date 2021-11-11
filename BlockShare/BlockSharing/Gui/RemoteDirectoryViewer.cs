using BlockShare.BlockSharing.DirectoryDigesting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlockShare.BlockSharing.Gui
{
    public partial class RemoteDirectoryViewer : ListView
    {
        private DirectoryDigest rootDigest;
        private DirectoryDigest currentDirectory;
        private Stack<DirectoryDigest> pathStack = new Stack<DirectoryDigest>();

        public RemoteDirectoryViewer()
        {
            InitializeComponent();
        }

        public void SetRootDigest(DirectoryDigest directoryDigest)
        {
            rootDigest = directoryDigest;
            currentDirectory = rootDigest;

            DisplayEntries();
        }
        /*
        protected override void OnDoubleClick(EventArgs e)
        {
            base.OnDoubleClick(e);

            Debug.WriteLine(e.GetType());
        }
        */

        private void AddDirectoryToList(DirectoryDigest directoryDigest)
        {

        }

        private void AddFileToList(FileDigest fileDigest)
        {

        }

        private void DisplayEntries()
        {
            foreach(DirectoryDigest directoryDigest in currentDirectory.GetSubDirectories())
            {
                AddDirectoryToList(directoryDigest);
            }
            foreach (FileDigest fileDigest in currentDirectory.GetFiles())
            {
                AddFileToList(fileDigest);
            }
        }
    }
}
