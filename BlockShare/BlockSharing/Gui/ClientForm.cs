using BlockShare.BlockSharing.DirectoryDigesting;
using BlockShare.BlockSharing.NetworkStatistics;
using BlockShare.BlockSharing.PreferencesManagement;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlockShare.BlockSharing.Gui
{
    public partial class ClientForm : Form
    {
        private Preferences Preferences;

        private BlockShareClient blockShareClient;

        private NetStatHistory netStatHistory = new NetStatHistory(30);


        public ClientForm(Preferences preferences)
        {
            InitializeComponent();

            Preferences = preferences;
            
        }

        private void ConnectToServer()
        {
            blockShareClient = new BlockShareClient(Preferences, null);
        }

        private void ListRemote(string remotePath)
        {
            if(blockShareClient == null)
            {
                MessageBox.Show("Not connected to the server!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            DirectoryDigest directoryDigest = blockShareClient.GetDirectoryDigest(remotePath, Preferences.BrowserRecursionLevel);
            
            
        }

        private void ListRemoteRoot()
        {
            ListRemote(string.Empty);
        }

        private void DirectoryViewer_DoubleClick(object sender, EventArgs e)
        {

        }
    }
}
