/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Data.Common;

namespace test
{
  public partial class TestCasesDialog : Form
  {
    private delegate void DelegateWithNoArgs();

    /// <summary>
    /// The total number of tests that have failed during the previous test run.
    /// </summary>
    private int _failed;

    /// <summary>
    /// If set, then automatically run all the tests and exit.
    /// </summary>
    private bool _autoRun;

    private TestCases _test;
    private TestCases _testitems;

    public TestCasesDialog(string fileName, bool autoRun)
    {
      InitializeComponent();

      using (DataTable tbl = DbProviderFactories.GetFactoryClasses())
      {
        foreach (DataRow row in tbl.Rows)
        {
          string prov = row[2].ToString();

          if (prov.IndexOf("SQLite", 0, StringComparison.OrdinalIgnoreCase) != -1
            || prov.IndexOf("SqlClient", 0, StringComparison.OrdinalIgnoreCase) != -1
            )
            _provider.Items.Add(prov);
          if (prov == "System.Data.SQLite") _provider.SelectedItem = prov;
        }
      }
      _connectionString.Items.Add(String.Format("Data Source={0};Pooling=true;FailIfMissing=false", fileName));
      _connectionString.Items.Add("Data Source=(local);Initial Catalog=sqlite;Integrated Security=True;Max Pool Size=10");
      _connectionString.SelectedIndex = 0;

      _autoRun = autoRun;
      _testitems = new TestCases();
      foreach (KeyValuePair<string, bool> pair in _testitems.Tests)
      {
        ToolStripMenuItem item = (ToolStripMenuItem)testMenu.DropDownItems.Add(pair.Key, null, new EventHandler(_tests_Clicked));
        item.Checked = true;
        item.CheckOnClick = true;
      }

      this.Load += new EventHandler(TestCasesDialog_Load);
    }

    private StringBuilder GridToText()
    {
        StringBuilder result = new StringBuilder();

        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (result.Length > 0)
                result.Append(Environment.NewLine);

            result.AppendFormat("{0}\t{1}\t{2}\t{3}", row.Cells[0].Value,
                row.Cells[1].Value, row.Cells[2].Value, row.Cells[3].Value);
        }

        return result;
    }

    void TestCasesDialog_Load(object sender, EventArgs e)
    {
        //
        // NOTE: In "automatic" mode, run all the tests as soon as the form is
        //       fully loaded.
        //
        if (_autoRun)
        {
            BeginInvoke(new DelegateWithNoArgs(delegate()
            {
                runButton_Click(sender, e);
            }));
        }
    }

    void _tests_Clicked(object sender, EventArgs e)
    {
      ToolStripMenuItem item = sender as ToolStripMenuItem;
      if (item != null)
        _testitems.Tests[item.Text] = item.Checked;
    }

    private void runButton_Click(object sender, EventArgs e)
    {
      string factoryString = _provider.SelectedItem.ToString();
      DbProviderFactory factory = DbProviderFactories.GetFactory(factoryString);

      _failed = 0;

      _test = new TestCases(factory, _connectionString.Text);
      _test.Tests = _testitems.Tests;

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

      //
      // NOTE: In "automatic" mode, check if any of the tests failed and return
      //       the appropriate error code to the operating system as we exit
      //       the process.  Also, attempt to write the entire contents of the
      //       test grid to the standard output channel via the console.  This
      //       may fail if we have no console; however, the failure will simply
      //       be ignored.
      //
      if (_autoRun)
      {
          try
          {
              Console.Write("{0}", GridToText());
          }
          catch
          {
              // do nothing, ignored.
          }

          Environment.Exit(_failed != 0 ? 1 : 0);
      }
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
          _failed++;

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
      if (_autoRun)
      {
        try
        {
          Console.Write("canceled...");
        }
        catch
        {
          // do nothing, ignored.
        }

        Environment.Exit(2);
      }

      Close();
    }
  }
}
