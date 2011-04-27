using System;
using System.Data.SQLite;
using System.Windows.Forms;

namespace test
{
  class Program
  {
    static void Main()
    {
      Application.Run(new TestCasesDialog());
    }

    static void cnn_RollBack(object sender, EventArgs e)
    {
    }

    static void cnn_Commit(object sender, CommitEventArgs e)
    {
    }

    static void cnn_Updated(object sender, UpdateEventArgs e)
    {
    }
  }
}
