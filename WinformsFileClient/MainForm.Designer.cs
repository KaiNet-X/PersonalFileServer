namespace WinformsFileClient
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            treeView = new TreeView();
            label1 = new Label();
            downloadButton = new Button();
            uploadButton = new Button();
            label2 = new Label();
            directoryButton = new Button();
            deleteFileButton = new Button();
            label3 = new Label();
            label5 = new Label();
            label6 = new Label();
            sAddr = new Label();
            sPort = new Label();
            label12 = new Label();
            label11 = new Label();
            label10 = new Label();
            cAddr = new Label();
            cPort = new Label();
            panel1 = new Panel();
            connectedStatus = new Label();
            connectedLabel = new Label();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // treeView
            // 
            treeView.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            treeView.Location = new Point(17, 69);
            treeView.Name = "treeView";
            treeView.Size = new Size(309, 337);
            treeView.TabIndex = 0;
            treeView.AfterSelect += treeView_AfterSelect;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(17, 31);
            label1.Name = "label1";
            label1.Size = new Size(92, 20);
            label1.TabIndex = 1;
            label1.Text = "Remote files";
            // 
            // downloadButton
            // 
            downloadButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            downloadButton.Enabled = false;
            downloadButton.Location = new Point(17, 421);
            downloadButton.Name = "downloadButton";
            downloadButton.Size = new Size(309, 29);
            downloadButton.TabIndex = 2;
            downloadButton.Text = "Download file";
            downloadButton.UseVisualStyleBackColor = true;
            downloadButton.Click += downloadButton_Click;
            // 
            // uploadButton
            // 
            uploadButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            uploadButton.Enabled = false;
            uploadButton.Location = new Point(346, 421);
            uploadButton.Name = "uploadButton";
            uploadButton.Size = new Size(314, 29);
            uploadButton.TabIndex = 3;
            uploadButton.Text = "Upload file";
            uploadButton.UseVisualStyleBackColor = true;
            uploadButton.Click += uploadButton_Click;
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            label2.AutoSize = true;
            label2.Location = new Point(457, 31);
            label2.Name = "label2";
            label2.Size = new Size(118, 20);
            label2.TabIndex = 0;
            label2.Text = "Connection stats";
            // 
            // directoryButton
            // 
            directoryButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            directoryButton.Image = Properties.Resources.icons8_folder_48;
            directoryButton.Location = new Point(612, 12);
            directoryButton.Name = "directoryButton";
            directoryButton.Size = new Size(48, 48);
            directoryButton.TabIndex = 5;
            directoryButton.UseVisualStyleBackColor = true;
            directoryButton.Click += directoryButton_Click;
            // 
            // deleteFileButton
            // 
            deleteFileButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            deleteFileButton.Enabled = false;
            deleteFileButton.Location = new Point(18, 465);
            deleteFileButton.Name = "deleteFileButton";
            deleteFileButton.Size = new Size(309, 29);
            deleteFileButton.TabIndex = 6;
            deleteFileButton.Text = "Delete file";
            deleteFileButton.UseVisualStyleBackColor = true;
            deleteFileButton.Click += deleteFileButton_Click;
            // 
            // label3
            // 
            label3.Anchor = AnchorStyles.Left;
            label3.AutoSize = true;
            label3.Location = new Point(-1, 65);
            label3.Name = "label3";
            label3.Size = new Size(53, 20);
            label3.TabIndex = 5;
            label3.Text = "Server:";
            // 
            // label5
            // 
            label5.Anchor = AnchorStyles.Left;
            label5.AutoSize = true;
            label5.Location = new Point(15, 105);
            label5.Name = "label5";
            label5.Size = new Size(38, 20);
            label5.TabIndex = 7;
            label5.Text = "Port:";
            // 
            // label6
            // 
            label6.Anchor = AnchorStyles.Left;
            label6.AutoSize = true;
            label6.Location = new Point(15, 85);
            label6.Name = "label6";
            label6.Size = new Size(65, 20);
            label6.TabIndex = 8;
            label6.Text = "Address:";
            // 
            // sAddr
            // 
            sAddr.Anchor = AnchorStyles.Right;
            sAddr.AutoSize = true;
            sAddr.Location = new Point(235, 85);
            sAddr.Name = "sAddr";
            sAddr.Size = new Size(62, 20);
            sAddr.TabIndex = 9;
            sAddr.Text = "Address";
            sAddr.TextAlign = ContentAlignment.MiddleRight;
            // 
            // sPort
            // 
            sPort.Anchor = AnchorStyles.Right;
            sPort.AutoSize = true;
            sPort.Location = new Point(262, 105);
            sPort.Name = "sPort";
            sPort.Size = new Size(35, 20);
            sPort.TabIndex = 10;
            sPort.Text = "Port";
            sPort.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label12
            // 
            label12.Anchor = AnchorStyles.Left;
            label12.AutoSize = true;
            label12.Location = new Point(3, 153);
            label12.Name = "label12";
            label12.Size = new Size(50, 20);
            label12.TabIndex = 11;
            label12.Text = "Client:";
            // 
            // label11
            // 
            label11.Anchor = AnchorStyles.Left;
            label11.AutoSize = true;
            label11.Location = new Point(19, 193);
            label11.Name = "label11";
            label11.Size = new Size(38, 20);
            label11.TabIndex = 12;
            label11.Text = "Port:";
            // 
            // label10
            // 
            label10.Anchor = AnchorStyles.Left;
            label10.AutoSize = true;
            label10.Location = new Point(19, 173);
            label10.Name = "label10";
            label10.Size = new Size(65, 20);
            label10.TabIndex = 13;
            label10.Text = "Address:";
            // 
            // cAddr
            // 
            cAddr.Anchor = AnchorStyles.Right;
            cAddr.AutoSize = true;
            cAddr.Location = new Point(235, 173);
            cAddr.Name = "cAddr";
            cAddr.Size = new Size(62, 20);
            cAddr.TabIndex = 14;
            cAddr.Text = "Address";
            cAddr.TextAlign = ContentAlignment.MiddleRight;
            // 
            // cPort
            // 
            cPort.Anchor = AnchorStyles.Right;
            cPort.AutoSize = true;
            cPort.Location = new Point(262, 193);
            cPort.Name = "cPort";
            cPort.Size = new Size(35, 20);
            cPort.TabIndex = 15;
            cPort.Text = "Port";
            cPort.TextAlign = ContentAlignment.MiddleRight;
            // 
            // panel1
            // 
            panel1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Controls.Add(connectedStatus);
            panel1.Controls.Add(connectedLabel);
            panel1.Controls.Add(cPort);
            panel1.Controls.Add(cAddr);
            panel1.Controls.Add(label10);
            panel1.Controls.Add(label11);
            panel1.Controls.Add(label12);
            panel1.Controls.Add(sPort);
            panel1.Controls.Add(sAddr);
            panel1.Controls.Add(label6);
            panel1.Controls.Add(label5);
            panel1.Controls.Add(label3);
            panel1.Location = new Point(346, 69);
            panel1.Name = "panel1";
            panel1.Size = new Size(314, 337);
            panel1.TabIndex = 4;
            // 
            // connectedStatus
            // 
            connectedStatus.Anchor = AnchorStyles.Right;
            connectedStatus.AutoSize = true;
            connectedStatus.Location = new Point(222, 15);
            connectedStatus.Name = "connectedStatus";
            connectedStatus.Size = new Size(80, 20);
            connectedStatus.TabIndex = 17;
            connectedStatus.Text = "Connected";
            connectedStatus.TextAlign = ContentAlignment.MiddleRight;
            // 
            // connectedLabel
            // 
            connectedLabel.Anchor = AnchorStyles.Left;
            connectedLabel.AutoSize = true;
            connectedLabel.Location = new Point(3, 15);
            connectedLabel.Name = "connectedLabel";
            connectedLabel.Size = new Size(83, 20);
            connectedLabel.TabIndex = 16;
            connectedLabel.Text = "Connected:";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(672, 509);
            Controls.Add(deleteFileButton);
            Controls.Add(directoryButton);
            Controls.Add(label2);
            Controls.Add(panel1);
            Controls.Add(uploadButton);
            Controls.Add(downloadButton);
            Controls.Add(label1);
            Controls.Add(treeView);
            MinimumSize = new Size(410, 498);
            Name = "MainForm";
            Text = "File client v2";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TreeView treeView;
        private Label label1;
        private Button downloadButton;
        private Button uploadButton;
        private Label label2;
        private Button directoryButton;
        private Button deleteFileButton;
        private Label label3;
        private Label label5;
        private Label label6;
        private Label sAddr;
        private Label sPort;
        private Label label12;
        private Label label11;
        private Label label10;
        private Label cAddr;
        private Label cPort;
        private Panel panel1;
        private Label connectedStatus;
        private Label connectedLabel;
    }
}