using BlockShare.BlockSharing.NetworkStatistics;
using BlockShare.BlockSharing.PreferencesManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlockShare.BlockSharing.Gui
{

    public class ServerForm : Form
    {
        private Timer InfoTimer;
        private System.ComponentModel.IContainer components;
        private ListBox ActiveConnectionsList;
        private StatusStrip StatusStrip;
        private ToolStripStatusLabel DownSpeedLabel;
        private ToolStripStatusLabel UpSpeedLabel;
        private Button StopButton;
        private ToolStripStatusLabel ListeningLabel;
        private Button StartButton;

        private Preferences Preferences;

        private BlockShareServer blockShareServer;

        private NetStatHistory netStatHistory = new NetStatHistory(30);
       

        public ServerForm(Preferences preferences)
        {
            InitializeComponent();

            Preferences = preferences;
        }        

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.InfoTimer = new System.Windows.Forms.Timer(this.components);
            this.ActiveConnectionsList = new System.Windows.Forms.ListBox();
            this.StatusStrip = new System.Windows.Forms.StatusStrip();
            this.ListeningLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.DownSpeedLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.UpSpeedLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.StopButton = new System.Windows.Forms.Button();
            this.StartButton = new System.Windows.Forms.Button();
            this.StatusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // InfoTimer
            // 
            this.InfoTimer.Enabled = true;
            this.InfoTimer.Interval = 500;
            this.InfoTimer.Tick += new System.EventHandler(this.InfoTimer_Tick);
            // 
            // ActiveConnectionsList
            // 
            this.ActiveConnectionsList.Dock = System.Windows.Forms.DockStyle.Top;
            this.ActiveConnectionsList.FormattingEnabled = true;
            this.ActiveConnectionsList.Location = new System.Drawing.Point(0, 0);
            this.ActiveConnectionsList.Margin = new System.Windows.Forms.Padding(3, 3, 3, 50);
            this.ActiveConnectionsList.Name = "ActiveConnectionsList";
            this.ActiveConnectionsList.Size = new System.Drawing.Size(619, 251);
            this.ActiveConnectionsList.TabIndex = 3;
            // 
            // StatusStrip
            // 
            this.StatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ListeningLabel,
            this.DownSpeedLabel,
            this.UpSpeedLabel});
            this.StatusStrip.Location = new System.Drawing.Point(0, 290);
            this.StatusStrip.Name = "StatusStrip";
            this.StatusStrip.Size = new System.Drawing.Size(619, 22);
            this.StatusStrip.TabIndex = 4;
            this.StatusStrip.Text = "statusStrip1";
            // 
            // ListeningLabel
            // 
            this.ListeningLabel.Name = "ListeningLabel";
            this.ListeningLabel.Size = new System.Drawing.Size(97, 17);
            this.ListeningLabel.Text = "Listening to: N/A";
            // 
            // DownSpeedLabel
            // 
            this.DownSpeedLabel.Name = "DownSpeedLabel";
            this.DownSpeedLabel.Size = new System.Drawing.Size(54, 17);
            this.DownSpeedLabel.Text = "D/L: N/A";
            // 
            // UpSpeedLabel
            // 
            this.UpSpeedLabel.Name = "UpSpeedLabel";
            this.UpSpeedLabel.Size = new System.Drawing.Size(54, 17);
            this.UpSpeedLabel.Text = "U/L: N/A";
            // 
            // StopButton
            // 
            this.StopButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.StopButton.Enabled = false;
            this.StopButton.Location = new System.Drawing.Point(532, 264);
            this.StopButton.Name = "StopButton";
            this.StopButton.Size = new System.Drawing.Size(75, 23);
            this.StopButton.TabIndex = 5;
            this.StopButton.Text = "StopServer";
            this.StopButton.UseVisualStyleBackColor = true;
            this.StopButton.Click += new System.EventHandler(this.StopButton_Click);
            // 
            // StartButton
            // 
            this.StartButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.StartButton.Location = new System.Drawing.Point(451, 264);
            this.StartButton.Name = "StartButton";
            this.StartButton.Size = new System.Drawing.Size(75, 23);
            this.StartButton.TabIndex = 6;
            this.StartButton.Text = "Start Server";
            this.StartButton.UseVisualStyleBackColor = true;
            this.StartButton.Click += new System.EventHandler(this.StartButton_Click);
            // 
            // ServerForm
            // 
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(619, 312);
            this.Controls.Add(this.StartButton);
            this.Controls.Add(this.StopButton);
            this.Controls.Add(this.StatusStrip);
            this.Controls.Add(this.ActiveConnectionsList);
            this.Name = "ServerForm";
            this.Load += new System.EventHandler(this.ServerForm_Load);
            this.ResizeEnd += new System.EventHandler(this.ServerForm_ResizeEnd);
            this.StatusStrip.ResumeLayout(false);
            this.StatusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        

        private void RegisterForEvents()
        {
            blockShareServer.OnClientConnected += BlockShareServer_OnClientConnected;
            blockShareServer.OnClientDisconnected += BlockShareServer_OnClientDisconnected;
            blockShareServer.OnServerStopped += BlockShareServer_OnServerStopped;
            blockShareServer.OnUnhandledException += BlockShareServer_OnUnhandledException;

            blockShareServer.OnHashingProgressChanged += BlockShareServer_OnHashingProgressChanged;
            blockShareServer.OnHashingFinished += BlockShareServer_OnHashingFinished;
            blockShareServer.OnBlockUploaded += BlockShareServer_OnBlockUploaded;
        }

        private void OnBlockUploadedSync(BlockShareServer arg1, IPEndPoint client, string fileName, long blockIndex)
        {
            KeyValuePair<int, ConnectionListItem> itemPair;
            var connectionItems = FindItems(i => i.Connection == client.ToString());          
            if(connectionItems.Count == 0)
            {
                ConnectionListItem item = new ConnectionListItem(client.ToString());
                itemPair = new KeyValuePair<int, ConnectionListItem>(ActiveConnectionsList.Items.Count, item);
                ActiveConnectionsList.Items.Add(item);
            }
            else
            {                
                itemPair = connectionItems[0];
            }

            itemPair.Value.Action = "Uploading";
            itemPair.Value.FileName = fileName;
            if (itemPair.Value.FileInfo == null)
            {
                itemPair.Value.FileInfo = new FileInfo(fileName);
            }

            long fileLength = itemPair.Value.FileInfo.Length;
            long blocksCount = fileLength / Preferences.BlockSize;
            if(fileLength % Preferences.BlockSize != 0)
            {
                blocksCount++;
            }
            double progress = (double)blockIndex / blocksCount;
            itemPair.Value.Progress = progress;
            ActiveConnectionsList.Items[itemPair.Key] = itemPair.Value;
        }

        private void BlockShareServer_OnBlockUploaded(BlockShareServer arg1, IPEndPoint client, string fileName, long blockIndex)
        {
            BeginInvoke(new Action<BlockShareServer, IPEndPoint, string, long>(OnBlockUploadedSync), arg1, client, fileName, blockIndex);
        }

        private IList<KeyValuePair<int, ConnectionListItem>> FindItems(Func<ConnectionListItem, bool> filter = null)
        {
            List<KeyValuePair<int, ConnectionListItem>> result = new List<KeyValuePair<int, ConnectionListItem>>(ActiveConnectionsList.Items.Count);
            for(int i = 0; i < ActiveConnectionsList.Items.Count; i++)
            {
                object item = ActiveConnectionsList.Items[i];
                if(item is ConnectionListItem connectionItem)
                {
                    if(filter == null)
                    {
                        result.Add(new KeyValuePair<int, ConnectionListItem>(i, connectionItem));
                    }
                    else if(filter(connectionItem))
                    {
                        result.Add(new KeyValuePair<int, ConnectionListItem>(i, connectionItem));
                    }
                }
            }
            return result;
        }

        private void OnHashingFinishedSync(BlockShareServer arg1, string fileName)
        {
            var items = FindItems(i => i.Connection == "Hashing" && i.FileName == fileName);
            foreach(var itemPair in items)
            {
                ActiveConnectionsList.Items.RemoveAt(itemPair.Key);
            }
        }

        private void BlockShareServer_OnHashingFinished(BlockShareServer sender, string fileName)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<BlockShareServer, string>(OnHashingFinishedSync), sender, fileName);
            }
            else
            {
                OnHashingFinishedSync(sender, fileName);
            }
        }       
        
        private void OnHashingProgressChangedSync(BlockShareServer sender, string fileName, double progress)
        {
            KeyValuePair<int, ConnectionListItem> itemPair;
            var items = FindItems(i => i.Connection == "Hashing" && i.FileName == fileName);
            if (items.Count == 0)
            {
                ConnectionListItem item = new ConnectionListItem("Hashing");
                item.FileName = fileName;
                item.Action = "";

                itemPair = new KeyValuePair<int, ConnectionListItem>(ActiveConnectionsList.Items.Count, item);
                ActiveConnectionsList.Items.Add(item);
            }
            else
            {
                itemPair = items[0];                
            }
            itemPair.Value.Progress = progress;
            ActiveConnectionsList.Refresh();
            ActiveConnectionsList.Items[itemPair.Key] = itemPair.Value;
        }

        private void BlockShareServer_OnHashingProgressChanged(BlockShareServer sender, string fileName, double progress)
        {
            if(InvokeRequired)
            {
                BeginInvoke(new Action<BlockShareServer, string, double>(OnHashingProgressChangedSync), sender, fileName, progress);
            }
            else
            {
                OnHashingProgressChangedSync(sender, fileName, progress);
            }
        }

        private void BlockShareServer_OnUnhandledException(BlockShareServer arg1, string arg2)
        {
            MessageBox.Show("Server rised an exception: " + arg2, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void BlockShareServer_OnServerStopped(BlockShareServer obj)
        {
            BeginInvoke(new Action(() => ActiveConnectionsList.Items.Clear()));
        }

        private void OnClientDisconnectedSync(IPEndPoint connectionEp)
        {
            ConnectionListItem connectionListItem = null;
            for(int i = 0; i < ActiveConnectionsList.Items.Count; i++)
            {
                ConnectionListItem item = (ConnectionListItem)ActiveConnectionsList.Items[i];
                if(item.Connection == connectionEp.ToString())
                {
                    connectionListItem = item;
                    break;
                } 
            }

            if(connectionListItem == null)
            {
                return;
            }

            ActiveConnectionsList.Items.Remove(connectionListItem);
        }

        private void BlockShareServer_OnClientDisconnected(BlockShareServer arg1, System.Net.IPEndPoint arg2)
        {
            BeginInvoke(new Action(() => OnClientDisconnectedSync(arg2)));
        }

        private void OnClientConnectedSync(IPEndPoint connectionEp)
        {
            ConnectionListItem connectionListItem = new ConnectionListItem(connectionEp.ToString());
            ActiveConnectionsList.Items.Add(connectionListItem);
        }

        private void BlockShareServer_OnClientConnected(BlockShareServer arg1, System.Net.IPEndPoint arg2)
        {            
            BeginInvoke(new Action(() => OnClientConnectedSync(arg2)));
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            StartButton.Enabled = false;

            blockShareServer = new BlockShareServer(Preferences, null);
            RegisterForEvents();
            blockShareServer.StartServer();

            SetListeningLabel(blockShareServer.GetLocalEndpoint());

            StopButton.Enabled = true;
        }

        private void SetListeningLabel(string value)
        {
            ListeningLabel.Text = "Listening to: " + value;
        }

        private void SetDownSpeedLabel(string value)
        {
            DownSpeedLabel.Text = "D/L: " + value;
        }

        private void SetUpSpeedLabel(string value)
        {
            UpSpeedLabel.Text = "U/L: " + value;
        }   

        private void InfoTimer_Tick(object sender, EventArgs e)
        {
            if (blockShareServer == null)
            {
                SetListeningLabel("N/A");
                SetDownSpeedLabel("N/A");
                SetUpSpeedLabel("N/A");
                return;
            }

            NetStat netStat = blockShareServer.GetServerNetStat();
            netStatHistory.AppendHistoricalRecord(netStat);

            double seconds = InfoTimer.Interval / 1000.0f;

            NetStatSpeed speed = netStatHistory.GetAverageSpeed(seconds);
            double downSpeed = speed.DownSpeed;
            double upSpeed = speed.UpSpeed;
            SetDownSpeedLabel(Utils.FormatSpeed(downSpeed));
            SetUpSpeedLabel(Utils.FormatSpeed(upSpeed));            
        }

        private void ServerForm_Load(object sender, EventArgs e)
        {

        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            blockShareServer.StopServer();
            netStatHistory.Clear();
            StartButton.Enabled = true;
            StopButton.Enabled = false;
            SetListeningLabel("N/A");
        }

        private void ServerForm_ResizeEnd(object sender, EventArgs e)
        {
            Size size = ActiveConnectionsList.Size;
            size.Height = Height - ActiveConnectionsList.Location.Y - 90;
            ActiveConnectionsList.Size = size;
        }
    }
}
