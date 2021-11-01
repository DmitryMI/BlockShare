using BlockShare.BlockSharing.BlockShareTypes;
using BlockShare.BlockSharing.PreferencesManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        private NetStat[] netstatHistory = new NetStat[30];
        private TableLayoutPanel StateTable;
        private int netstatHistoryIndex = 0;

        private Dictionary<string, StateInfo> stateInfoTable = new Dictionary<string, StateInfo>();

        class StateInfo
        {
            public Label FileNameLabel { get; set; }
            public ProgressBar HashingProgressBar { get; set; }
            public double HashingProgress { get; set; }

            public StateInfo(Label fileNameLabel, ProgressBar progressBar)
            {
                FileNameLabel = fileNameLabel;
                HashingProgressBar = progressBar;
            }
        }

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
            this.StateTable = new System.Windows.Forms.TableLayoutPanel();
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
            this.ActiveConnectionsList.FormattingEnabled = true;
            this.ActiveConnectionsList.Location = new System.Drawing.Point(765, 12);
            this.ActiveConnectionsList.Name = "ActiveConnectionsList";
            this.ActiveConnectionsList.Size = new System.Drawing.Size(156, 238);
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
            this.StatusStrip.Size = new System.Drawing.Size(933, 22);
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
            this.StopButton.Location = new System.Drawing.Point(846, 264);
            this.StopButton.Name = "StopButton";
            this.StopButton.Size = new System.Drawing.Size(75, 23);
            this.StopButton.TabIndex = 5;
            this.StopButton.Text = "StopServer";
            this.StopButton.UseVisualStyleBackColor = true;
            this.StopButton.Click += new System.EventHandler(this.StopButton_Click);
            // 
            // StartButton
            // 
            this.StartButton.Location = new System.Drawing.Point(765, 264);
            this.StartButton.Name = "StartButton";
            this.StartButton.Size = new System.Drawing.Size(75, 23);
            this.StartButton.TabIndex = 6;
            this.StartButton.Text = "Start Server";
            this.StartButton.UseVisualStyleBackColor = true;
            this.StartButton.Click += new System.EventHandler(this.StartButton_Click);
            // 
            // StateTable
            // 
            this.StateTable.ColumnCount = 2;
            this.StateTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.StateTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 75F));
            this.StateTable.Location = new System.Drawing.Point(12, 12);
            this.StateTable.Name = "StateTable";
            this.StateTable.RowCount = 1;
            this.StateTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.StateTable.Size = new System.Drawing.Size(747, 35);
            this.StateTable.TabIndex = 7;
            // 
            // ServerForm
            // 
            this.ClientSize = new System.Drawing.Size(933, 312);
            this.Controls.Add(this.StateTable);
            this.Controls.Add(this.StartButton);
            this.Controls.Add(this.StopButton);
            this.Controls.Add(this.StatusStrip);
            this.Controls.Add(this.ActiveConnectionsList);
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

            blockShareServer.OnHashingProgressChanged += BlockShareServer_OnHashingProgressChanged;
            blockShareServer.OnHashingFinished += BlockShareServer_OnHashingFinished;            
        }

        private void OnHashingFinishedSync(BlockShareServer arg1, string arg2)
        {
            if(stateInfoTable.ContainsKey(arg2))
            {
                StateInfo stateInfo = stateInfoTable[arg2];
                StateTable.SuspendLayout();
                StateTable.Controls.Remove(stateInfo.FileNameLabel);
                StateTable.Controls.Remove(stateInfo.HashingProgressBar);
                StateTable.RowCount--;
                ResizeStateTable();
                StateTable.ResumeLayout();

                stateInfoTable.Remove(arg2);
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

        private void ResizeStateTable()
        {
            StateTable.Height = StateTable.RowCount * 35;
        }
        
        private void OnHashingProgressChangedSync(BlockShareServer sender, string fileName, double progress)
        {
            if (!stateInfoTable.ContainsKey(fileName))
            {
                StateTable.SuspendLayout();

                Label fileNameLabel = new Label();
                FileInfo fileInfo = new FileInfo(fileName);
                fileNameLabel.Text = fileInfo.Name;
                ProgressBar hashingProgressBar = new ProgressBar();
                hashingProgressBar.Name = "Hashing";
                hashingProgressBar.Maximum = 100;
                hashingProgressBar.Value = (int)(100 * progress);
                hashingProgressBar.Width = 555;

                StateTable.RowCount++;
                ResizeStateTable();

                StateTable.Controls.Add(fileNameLabel);
                StateTable.Controls.Add(hashingProgressBar);
                StateTable.ResumeLayout();

                StateInfo stateInfo = new StateInfo(fileNameLabel, hashingProgressBar);
                stateInfo.HashingProgress = progress;
                stateInfoTable.Add(fileName, stateInfo);
            }
            else
            {
                StateInfo stateInfo = stateInfoTable[fileName];
                if (progress - stateInfo.HashingProgress > 0.01f)
                {
                    ProgressBar hashingProgressBar = stateInfo.HashingProgressBar;
                    hashingProgressBar.Value = (int)(100 * progress);
                    stateInfo.HashingProgress = progress;
                }
            }
        }

        private void BlockShareServer_OnHashingProgressChanged(BlockShareServer sender, string fileName, double progress)
        {
            if(stateInfoTable.ContainsKey(fileName))
            {
                StateInfo stateInfo = stateInfoTable[fileName];
                if (progress - stateInfo.HashingProgress < 0.01f)
                {
                    return;
                }
            }

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

        private NetStat GetHistoricalRecord(int relativeIndex)
        {
            int index = (netstatHistory.Length + (netstatHistoryIndex - relativeIndex)) % netstatHistory.Length;
            if(index < 0)
            {
                index = -index;
            }
            //Debug.WriteLine("GetHistoricalRecord: " + index);
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
            //Debug.WriteLine("GetAverageUpSpeed...");
            NetStat firstRecord = GetHistoricalRecord(netstatHistory.Length - 1);
            NetStat lastRecord = GetHistoricalRecord(0);
            NetStat diff = lastRecord - firstRecord;
            double sentAverage = (double)diff.TotalSent / netstatHistory.Length;
            //Debug.WriteLine("GetAverageUpSpeed: " + sentAverage);
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
