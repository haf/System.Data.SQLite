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
  using SQLite.Designer.Design;

  public partial class ChangeScriptDialog : Form
  {
    private string _tableName;

    public ChangeScriptDialog(string tableName, string script)
    {
      _tableName = tableName;
      InitializeComponent();

      _script.Text = script;
    }

    private void noButton_Click(object sender, EventArgs e)
    {
      DialogResult = DialogResult.Cancel;
      Close();
    }

    private void yesButton_Click(object sender, EventArgs e)
    {
      using (SaveFileDialog save = new SaveFileDialog())
      {
        save.DefaultExt = "sql";
        save.OverwritePrompt = true;
        save.Filter = "SQL Script Files (*.sql)|*.sql|All Files (*.*)|*.*";
        save.FileName = String.Format("{0}.sql", _tableName);
        save.Title = "Save SQLite Change Script";

        DialogResult = save.ShowDialog(this);

        if (DialogResult == DialogResult.OK)
        {
          System.IO.File.WriteAllText(save.FileName, _script.Text.Replace("\r", "").Replace("\n", "\r\n"), Encoding.UTF8);
        }
      }
      Close();
    }
  }
}
