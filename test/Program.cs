using System;
using System.Data;
using System.Text;
using System.Data.Common;
using System.Data.SQLite;
using System.Threading;
using System.Transactions;

namespace test
{
  class Program
  {
    static void Main(string[] args)
    {
      if (System.IO.File.Exists("test.db3"))
        System.IO.File.Delete("test.db3");

      DbProviderFactory fact;
      fact = DbProviderFactories.GetFactory("System.Data.SQLite");

      SQLiteConnection cnn = new SQLiteConnection();
      {
        cnn.ConnectionString = "Data Source=test.db3;Pooling=False;Password=testing";
        cnn.Open();

        TestCases.Run(fact, cnn);
      }

      Console.ReadKey();
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
