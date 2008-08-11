using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Data.Common;

namespace test
{
  public partial class TestCasesDialog : Form
  {
    private TestCases _test;

    public TestCasesDialog()
    {
      InitializeComponent();

      using (DataTable tbl = DbProviderFactories.GetFactoryClasses())
      {
        foreach (DataRow row in tbl.Rows)
        {
          string prov = row[2].ToString();

          if (prov.IndexOf("SQLite", 0, StringComparison.OrdinalIgnoreCase) != -1
            //|| prov.IndexOf("SqlClient", 0, StringComparison.OrdinalIgnoreCase) != -1
            )
            _provider.Items.Add(prov);
          if (prov == "System.Data.SQLite") _provider.SelectedItem = prov;
        }
      }
      _connectionString.Text = "Data Source=Test.db3;Pooling=true;FailIfMissing=false";
    }

    private void runButton_Click(object sender, EventArgs e)
    {
      DbProviderFactory factory = DbProviderFactories.GetFactory(_provider.SelectedItem.ToString());
      _test = new TestCases(factory, _connectionString.Text);
      _test.OnTestStarting += new TestStartingEvent(_test_OnTestStarting);
      _test.OnTestFinished += new TestCompletedEvent(_test_OnTestFinished);
      _test.OnAllTestsDone += new EventHandler(_test_OnAllTestsDone);
      _grid.Rows.Clear();
      runButton.Enabled = false;

      System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(_threadFunc));
      t.IsBackground = true;
      t.Start();
    }

    void _test_OnAllTestsDone(object sender, EventArgs e)
    {
      if (InvokeRequired)
        Invoke(new EventHandler(_test_OnAllTestsDone), sender, e);
      else
        runButton.Enabled = true;
    }

    void _threadFunc()
    {
      _test.Run();
    }

    void _test_OnTestFinished(object sender, TestEventArgs args)
    {
      if (InvokeRequired)
        Invoke(new TestCompletedEvent(_test_OnTestFinished), sender, args);
      else
      {
        _grid.Rows[_grid.Rows.Count - 1].SetValues(args.TestName, args.Result, args.Duration, (args.Exception == null) ? args.Message : args.Exception.Message);
        if (args.Result == TestResultEnum.Failed)
        {
          _grid.Rows[_grid.Rows.Count - 1].Cells[1].Style.BackColor = Color.Red;
        }
        else if (args.Result == TestResultEnum.Inconclusive)
        {
          _grid.Rows[_grid.Rows.Count - 1].Cells[1].Style.BackColor = Color.LightBlue;
        }
        //_grid.Rows[_grid.Rows.Count - 1].Height = _grid.Rows[_grid.Rows.Count - 1].GetPreferredHeight(_grid.Rows.Count - 1, DataGridViewAutoSizeRowMode.AllCells, true);
      }
    }

    void _test_OnTestStarting(object sender, TestEventArgs args)
    {
      if (InvokeRequired)
        Invoke(new TestStartingEvent(_test_OnTestStarting), sender, args);
      else
      {
        _grid.Rows.Add(args.TestName, "Starting", null, null);
        _grid.FirstDisplayedScrollingRowIndex = _grid.Rows.Count - 1;
      }
    }

    private void closeButton_Click(object sender, EventArgs e)
    {
      Close();
    }
  }
}
