namespace SQLite.Designer
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Data;
  using System.Drawing;
  using System.Text;
  using System.Windows.Forms;
  using Microsoft.VisualStudio.Data;
  using System.Windows.Forms.Design;
  using Microsoft.VisualStudio.Shell.Interop;
  using Microsoft.VisualStudio;
  using System.Data.Common;

  public partial class ChangePasswordDialog : Form
  {
    internal string Password = null;

    private SQLiteConnectionProperties _props;

    internal ChangePasswordDialog(SQLiteConnectionProperties props)
    {
      _props = props;
      InitializeComponent();

      password.Text = _props["Password"] as string;
    }

    private void password_TextChanged(object sender, EventArgs e)
    {
      if (String.IsNullOrEmpty(password.Text) || password.Text == _props["Password"] as string)
      {
        confirmLabel.Enabled = false;
        passwordConfirm.Enabled = false;
        passwordConfirm.Text = "";

        if (String.IsNullOrEmpty(password.Text) && _props["Password"] != null)
          action.Text = VSPackage.Decrypt;
        else
          action.Text = "";
      }
      else
      {
        confirmLabel.Enabled = true;
        passwordConfirm.Enabled = true;

        if (_props["Password"] != null)
          action.Text = VSPackage.ReEncrypt;
        else
          action.Text = VSPackage.Encrypt;
      }

      okButton.Enabled = (password.Text == passwordConfirm.Text);
    }

    private void okButton_Click(object sender, EventArgs e)
    {
      Password = password.Text;
      DialogResult = DialogResult.OK;
      Close();
    }
  }
}