namespace WinformsFileClient
{
    partial class SigninForm
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
            userTextBox = new TextBox();
            label1 = new Label();
            label2 = new Label();
            pwdTextBox = new TextBox();
            button1 = new Button();
            SuspendLayout();
            // 
            // textBox1
            // 
            userTextBox.Location = new Point(109, 33);
            userTextBox.Name = "textBox1";
            userTextBox.Size = new Size(170, 27);
            userTextBox.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 36);
            label1.Name = "label1";
            label1.Size = new Size(75, 20);
            label1.TabIndex = 1;
            label1.Text = "Username";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 90);
            label2.Name = "label2";
            label2.Size = new Size(70, 20);
            label2.TabIndex = 3;
            label2.Text = "Password";
            // 
            // textBox2
            // 
            pwdTextBox.Location = new Point(109, 87);
            pwdTextBox.Name = "textBox2";
            pwdTextBox.Size = new Size(170, 27);
            pwdTextBox.TabIndex = 2;
            // 
            // button1
            // 
            button1.Location = new Point(12, 172);
            button1.Name = "button1";
            button1.Size = new Size(267, 29);
            button1.TabIndex = 4;
            button1.Text = "Submit";
            button1.UseVisualStyleBackColor = true;
            button1.Click += submit_click;
            // 
            // SigninForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(307, 233);
            Controls.Add(button1);
            Controls.Add(label2);
            Controls.Add(pwdTextBox);
            Controls.Add(label1);
            Controls.Add(userTextBox);
            Name = "SigninForm";
            Text = "SigninForm";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox userTextBox;
        private Label label1;
        private Label label2;
        private TextBox pwdTextBox;
        private Button button1;
    }
}