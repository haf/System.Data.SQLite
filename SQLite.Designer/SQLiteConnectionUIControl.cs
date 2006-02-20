/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

namespace SQLite.Designer
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Data;
  using System.Drawing;
  using System.Text;
  using System.Windows.Forms;
  using Microsoft.Data.ConnectionUI;

  public partial class SQLiteConnectionUIControl : UserControl, IDataConnectionUIControl
  {
    private SQLiteConnectionProperties _connectionProperties;

    public SQLiteConnectionUIControl()
    {
      InitializeComponent();
    }

    private void browseButton_Click(object sender, EventArgs e)
    {
      OpenFileDialog dlg = new OpenFileDialog();
      dlg.FileName = fileTextBox.Text;
      dlg.Title = "Select SQLite Database File";

      if (dlg.ShowDialog(this) == DialogResult.OK)
      {
        fileTextBox.Text = dlg.FileName;
        fileTextBox_Leave(sender, e);
      }
    }

    private void newDatabase_Click(object sender, EventArgs e)
    {
      SaveFileDialog dlg = new SaveFileDialog();
      dlg.Title = "Create SQLite Database File";
      if (dlg.ShowDialog() == DialogResult.OK)
      {
        fileTextBox.Text = dlg.FileName;
        fileTextBox_Leave(sender, e);
      }
    }

    #region IDataConnectionUIControl Members

    public void Initialize(IDataConnectionProperties connectionProperties)
    {
      _connectionProperties = (SQLiteConnectionProperties)connectionProperties;
    }

    public void LoadProperties()
    {
      fileTextBox.Text = _connectionProperties["Data Source"] as string;
      passwordTextBox.Text = _connectionProperties["Password"] as string;
    }

    #endregion

    private void passwordTextBox_Leave(object sender, EventArgs e)
    {
      _connectionProperties["Password"] = passwordTextBox.Text;
    }

    private void encoding_Changed(object sender, EventArgs e)
    {
      _connectionProperties["UseUTF16Encoding"] = utf16RadioButton.Checked;
    }

    private void datetime_Changed(object sender, EventArgs e)
    {
      _connectionProperties["DateTimeFormat"] = (iso8601RadioButton.Checked == true) ? "ISO8601" : "Ticks";
    }

    private void sync_Changed(object sender, EventArgs e)
    {
      string sync = "Normal";
      if (fullRadioButton.Checked == true) sync = "Full";
      else if (offRadioButton.Checked == true) sync = "Off";

      _connectionProperties["Synchronous"] = sync;
    }

    private void pageSizeTextBox_Leave(object sender, EventArgs e)
    {
      int n = Convert.ToInt32(pageSizeTextBox.Text);
      _connectionProperties["Page Size"] = n;
    }

    private void cacheSizeTextbox_Leave(object sender, EventArgs e)
    {
      int n = Convert.ToInt32(cacheSizeTextbox.Text);
      _connectionProperties["Cache Size"] = n;
    }

    private void fileTextBox_Leave(object sender, EventArgs e)
    {
      _connectionProperties["Data Source"] = fileTextBox.Text;
    }
  }
}