namespace WinformsFileClient;

using Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

public partial class SigninForm : Form
{
    public SigninForm()
    {
        InitializeComponent();
    }

    private async void submit_click(object sender, EventArgs e)
    {
        if (userTextBox.Text.Length > 0 && pwdTextBox.Text.Length > 0) 
        {
            await Auth.SetUser(new User(userTextBox.Text, Convert.ToBase64String(Crypto.Hash(pwdTextBox.Text))));
            var main = new MainForm();
            main.Show();
            Close();
        }
    }
}
