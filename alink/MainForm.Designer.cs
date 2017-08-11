namespace alink
{
    partial class MainForm
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
            this.attachButton = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lobbyContainer1 = new alink.UI.LobbyContainer();
            this.rulesSelector1 = new alink.UI.RulesSelector();
            this.memoryOffsetSelector = new alink.UI.MemoryOffsetSelector();
            this.processSelector1 = new alink.UI.ProcessSelector();
            this.statusStrip1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // attachButton
            // 
            this.attachButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.attachButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.attachButton.Location = new System.Drawing.Point(507, 13);
            this.attachButton.Name = "attachButton";
            this.attachButton.Size = new System.Drawing.Size(86, 23);
            this.attachButton.TabIndex = 3;
            this.attachButton.Text = "Attach!";
            this.attachButton.UseVisualStyleBackColor = true;
            this.attachButton.Click += new System.EventHandler(this.attachButton_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel});
            this.statusStrip1.Location = new System.Drawing.Point(0, 374);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(608, 22);
            this.statusStrip1.TabIndex = 4;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripLabel
            // 
            this.toolStripLabel.Name = "toolStripLabel";
            this.toolStripLabel.Size = new System.Drawing.Size(0, 17);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.lobbyContainer1);
            this.groupBox1.Location = new System.Drawing.Point(15, 72);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(581, 267);
            this.groupBox1.TabIndex = 6;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Lobby";
            // 
            // lobbyContainer1
            // 
            this.lobbyContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lobbyContainer1.Location = new System.Drawing.Point(3, 16);
            this.lobbyContainer1.Name = "lobbyContainer1";
            this.lobbyContainer1.Nickname = "";
            this.lobbyContainer1.Size = new System.Drawing.Size(575, 248);
            this.lobbyContainer1.TabIndex = 5;
            // 
            // rulesSelector1
            // 
            this.rulesSelector1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rulesSelector1.Location = new System.Drawing.Point(12, 345);
            this.rulesSelector1.Name = "rulesSelector1";
            this.rulesSelector1.Size = new System.Drawing.Size(583, 26);
            this.rulesSelector1.TabIndex = 2;
            // 
            // memoryOffsetSelector
            // 
            this.memoryOffsetSelector.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.memoryOffsetSelector.Location = new System.Drawing.Point(12, 42);
            this.memoryOffsetSelector.Name = "memoryOffsetSelector";
            this.memoryOffsetSelector.Size = new System.Drawing.Size(583, 24);
            this.memoryOffsetSelector.TabIndex = 1;
            // 
            // processSelector1
            // 
            this.processSelector1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.processSelector1.Location = new System.Drawing.Point(12, 12);
            this.processSelector1.Name = "processSelector1";
            this.processSelector1.Size = new System.Drawing.Size(584, 24);
            this.processSelector1.TabIndex = 0;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(608, 396);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.attachButton);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.rulesSelector1);
            this.Controls.Add(this.memoryOffsetSelector);
            this.Controls.Add(this.processSelector1);
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(477, 300);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "A-Link";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private UI.ProcessSelector processSelector1;
        private UI.MemoryOffsetSelector memoryOffsetSelector;
        private UI.RulesSelector rulesSelector1;
        private System.Windows.Forms.Button attachButton;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripLabel;
        private UI.LobbyContainer lobbyContainer1;
        private System.Windows.Forms.GroupBox groupBox1;
    }
}

