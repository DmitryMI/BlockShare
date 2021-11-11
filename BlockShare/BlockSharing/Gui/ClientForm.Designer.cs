
namespace BlockShare.BlockSharing.Gui
{
    partial class ClientForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.GlobalProgressBar = new System.Windows.Forms.ProgressBar();
            this.LocalProgressBar = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // GlobalProgressBar
            // 
            this.GlobalProgressBar.Location = new System.Drawing.Point(12, 415);
            this.GlobalProgressBar.Name = "GlobalProgressBar";
            this.GlobalProgressBar.Size = new System.Drawing.Size(776, 23);
            this.GlobalProgressBar.TabIndex = 0;
            // 
            // LocalProgressBar
            // 
            this.LocalProgressBar.Location = new System.Drawing.Point(12, 386);
            this.LocalProgressBar.Name = "LocalProgressBar";
            this.LocalProgressBar.Size = new System.Drawing.Size(776, 23);
            this.LocalProgressBar.TabIndex = 1;
            // 
            // ClientForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.LocalProgressBar);
            this.Controls.Add(this.GlobalProgressBar);
            this.Name = "ClientForm";
            this.Text = "ClientForm";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ProgressBar GlobalProgressBar;
        private System.Windows.Forms.ProgressBar LocalProgressBar;
    }
}