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
  using System.Globalization;
  using Microsoft.VisualStudio.Data;
  using Microsoft.Win32;

  /// <summary>
  /// Provides a UI to edit/create SQLite database connections
  /// </summary>
  [ToolboxItem(false)]
  public partial class SQLiteConnectionUIControl : DataConnectionUIControl
  {
    bool enableProviderChanged = false;

    public SQLiteConnectionUIControl()
    {
      InitializeComponent();
      SQLiteOptions.AddProviderNames(providerComboBox.Items);
      SQLiteOptions.SelectProviderName(providerComboBox);
      enableProviderChanged = true;
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

    public override void LoadProperties()
    {
      if (ConnectionProperties.Contains("data source"))
        fileTextBox.Text = ConnectionProperties["data source"] as string;
      else
        fileTextBox.Text = String.Empty;

      if (ConnectionProperties.Contains("password"))
        passwordTextBox.Text = ConnectionProperties["password"] as string;
      else
        passwordTextBox.Text = String.Empty;
    }

    #endregion

    private void passwordTextBox_Leave(object sender, EventArgs e)
    {
      if (String.IsNullOrEmpty(passwordTextBox.Text))
        ConnectionProperties.Remove("password");
      else
        ConnectionProperties["password"] = passwordTextBox.Text;
    }

    private void encoding_Changed(object sender, EventArgs e)
    {
      if (utf8RadioButton.Checked == true)
        ConnectionProperties.Remove("useutf16encoding");
      else
        ConnectionProperties["useutf16encoding"] = utf16RadioButton.Checked;
    }

    private void datetime_Changed(object sender, EventArgs e)
    {
      if (iso8601RadioButton.Checked == true)
        ConnectionProperties.Remove("datetimeformat");
      else if (ticksRadioButton.Checked == true)
        ConnectionProperties["datetimeformat"] = "Ticks";
      else
        ConnectionProperties["datetimeformat"] = "JulianDay";
    }

    private void provider_Changed(object sender, EventArgs e)
    {
        if (!enableProviderChanged)
            return;

        object item = providerComboBox.SelectedItem;

        if (item != null)
        {
            SQLiteOptions.SetProviderName(item.ToString());
            LoadProperties();
        }
    }

    private void sync_Changed(object sender, EventArgs e)
    {
      string sync = "Normal";
      if (fullRadioButton.Checked == true) sync = "Full";
      else if (offRadioButton.Checked == true) sync = "Off";

      if (sync == "Normal")
        ConnectionProperties.Remove("synchronous");
      else
        ConnectionProperties["synchronous"] = sync;
    }

    private void pageSizeTextBox_Leave(object sender, EventArgs e)
    {
      int n = Convert.ToInt32(pageSizeTextBox.Text, CultureInfo.CurrentCulture);
      ConnectionProperties["page size"] = n;
    }

    private void cacheSizeTextbox_Leave(object sender, EventArgs e)
    {
      int n = Convert.ToInt32(cacheSizeTextbox.Text, CultureInfo.CurrentCulture);
      ConnectionProperties["cache size"] = n;
    }

    private void fileTextBox_Leave(object sender, EventArgs e)
    {
      ConnectionProperties["data source"] = fileTextBox.Text;
    }
  }
}