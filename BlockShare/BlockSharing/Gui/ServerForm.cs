using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlockShare.BlockSharing.Gui
{
    public class ServerForm : Form, IProgressReporter
    {
        private Timer InfoTimer;
        private System.ComponentModel.IContainer components;
        private ProgressBar HashingProgressBar;
        private ListBox FileSystemList;
        private ListBox ActiveConnectionsList;
        private StatusStrip StatusStrip;
        private ToolStripStatusLabel DownSpeedLabel;
        private ToolStripStatusLabel UpSpeedLabel;
        private Button StopButton;
        private ToolStripStatusLabel ListeningLabel;
        private Button StartButton;
        private Label label1;

        private double previousProgress = 0;

        private Preferences Preferences;

        private BlockShareServer blockShareServer;

        private NetStat[] netstatHistory = new NetStat[30];
        private int netstatHistoryIndex = 0;

        public ServerForm(Preferences preferences)
        {
            InitializeComponent();

            Preferences = preferences;
        }

        public void ReportFinishing(object sender, bool success, int jobId)
        {
            BeginInvoke((new Action(() => HashingProgressBar.Value = 0)));
        }

        public void ReportOverallFinishing(object sender, bool success)
        {
            BeginInvoke((new Action(() => HashingProgressBar.Value = 0)));
        }

        public void ReportOverallProgress(object sender, double progress)
        {
            if(progress - previousProgress > 0.01f)
            {
                previousProgress = progress;
                BeginInvoke((new Action(() => HashingProgressBar.Value = (int)(progress * HashingProgressBar.Maximum))));
            }            
        }

        public void ReportProgress(object sender, double progress, int jobId)
        {
            
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.InfoTimer = new System.Windows.Forms.Timer(this.components);
            this.HashingProgressBar = new System.Windows.Forms.ProgressBar();
            this.label1 = new System.Windows.Forms.Label();
            this.FileSystemList = new System.Windows.Forms.ListBox();
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
            // HashingProgressBar
            // 
            this.HashingProgressBar.Location = new System.Drawing.Point(12, 229);
            this.HashingProgressBar.Name = "HashingProgressBar";
            this.HashingProgressBar.Size = new System.Drawing.Size(411, 23);
            this.HashingProgressBar.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 210);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(92, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Hashing progress:";
            // 
            // FileSystemList
            // 
            this.FileSystemList.FormattingEnabled = true;
            this.FileSystemList.Location = new System.Drawing.Point(12, 12);
            this.FileSystemList.Name = "FileSystemList";
            this.FileSystemList.Size = new System.Drawing.Size(282, 186);
            this.FileSystemList.TabIndex = 2;
            // 
            // ActiveConnectionsList
            // 
            this.ActiveConnectionsList.FormattingEnabled = true;
            this.ActiveConnectionsList.Location = new System.Drawing.Point(300, 12);
            this.ActiveConnectionsList.Name = "ActiveConnectionsList";
            this.ActiveConnectionsList.Size = new System.Drawing.Size(123, 186);
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
            this.StatusStrip.Size = new System.Drawing.Size(435, 22);
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
            this.StopButton.Enabled = false;
            this.StopButton.Location = new System.Drawing.Point(348, 264);
            this.StopButton.Name = "StopButton";
            this.StopButton.Size = new System.Drawing.Size(75, 23);
            this.StopButton.TabIndex = 5;
            this.StopButton.Text = "StopServer";
            this.StopButton.UseVisualStyleBackColor = true;
            this.StopButton.Click += new System.EventHandler(this.StopButton_Click);
            // 
            // StartButton
            // 
            this.StartButton.Location = new System.Drawing.Point(267, 264);
            this.StartButton.Name = "StartButton";
            this.StartButton.Size = new System.Drawing.Size(75, 23);
            this.StartButton.TabIndex = 6;
            this.StartButton.Text = "Start Server";
            this.StartButton.UseVisualStyleBackColor = true;
            this.StartButton.Click += new System.EventHandler(this.StartButton_Click);
            // 
            // ServerForm
            // 
            this.ClientSize = new System.Drawing.Size(435, 312);
            this.Controls.Add(this.StartButton);
            this.Controls.Add(this.StopButton);
            this.Controls.Add(this.StatusStrip);
            this.Controls.Add(this.ActiveConnectionsList);
            this.Controls.Add(this.FileSystemList);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.HashingProgressBar);
            this.Name = "ServerForm";
            this.Load += new System.EventHandler(this.ServerForm_Load);
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
        }

        private void BlockShareServer_OnUnhandledException(BlockShareServer arg1, string arg2)
        {
            MessageBox.Show("Server rised an exception: " + arg2, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void BlockShareServer_OnServerStopped(BlockShareServer obj)
        {
            BeginInvoke(new Action(() => ActiveConnectionsList.Items.Clear()));
        }

        private void BlockShareServer_OnClientDisconnected(BlockShareServer arg1, System.Net.IPEndPoint arg2)
        {
            BeginInvoke(new Action(() => ActiveConnectionsList.Items.Remove(arg2)));
        }

        private void BlockShareServer_OnClientConnected(BlockShareServer arg1, System.Net.IPEndPoint arg2)
        {
            BeginInvoke(new Action(() => ActiveConnectionsList.Items.Add(arg2)));
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            StartButton.Enabled = false;

            blockShareServer = new BlockShareServer(Preferences, this, null);
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

        private NetStat GetHistoricalRecord(int relativeIndex)
        {
            int index = (netstatHistory.Length + (netstatHistoryIndex - relativeIndex)) % netstatHistory.Length;
            if(index < 0)
            {
                index = -index;
            }
            Debug.WriteLine("GetHistoricalRecord: " + index);
            return netstatHistory[index];
        }

        private void AppendHistoricalRecord(NetStat netStat)
        {
            int nextIndex = (netstatHistoryIndex + 1) % netstatHistory.Length;
            netstatHistoryIndex = nextIndex;
            netstatHistory[nextIndex] = netStat;
        }

        private double GetAverageDownSpeed(double timeSpan)
        {
            NetStat firstRecord = GetHistoricalRecord(netstatHistory.Length - 1);
            NetStat lastRecord = GetHistoricalRecord(0);
            NetStat diff = lastRecord - firstRecord;
            double receivedAverage = (double)diff.TotalReceived / netstatHistory.Length;
            double downSpeed = receivedAverage / timeSpan;
            return downSpeed;
        }

        private double GetAverageUpSpeed(double timeSpan)
        {
            Debug.WriteLine("GetAverageUpSpeed...");
            NetStat firstRecord = GetHistoricalRecord(netstatHistory.Length - 1);
            NetStat lastRecord = GetHistoricalRecord(0);
            NetStat diff = lastRecord - firstRecord;
            double sentAverage = (double)diff.TotalSent / netstatHistory.Length;
            Debug.WriteLine("GetAverageUpSpeed: " + sentAverage);
            double upSpeed = sentAverage / timeSpan;
            return upSpeed;
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
            AppendHistoricalRecord(netStat);

            double seconds = InfoTimer.Interval / 1000.0f;

            double downSpeed = GetAverageDownSpeed(seconds);
            double upSpeed = GetAverageUpSpeed(seconds);
            SetDownSpeedLabel(Utils.FormatSpeed(downSpeed));
            SetUpSpeedLabel(Utils.FormatSpeed(upSpeed));            
        }

        private void ServerForm_Load(object sender, EventArgs e)
        {

        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            blockShareServer.StopServer();
            StartButton.Enabled = true;
            StopButton.Enabled = false;
            SetListeningLabel("N/A");            
        }
    }
}
