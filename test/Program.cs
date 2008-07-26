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

      DbConnection cnn = fact.CreateConnection();
      {
        cnn.ConnectionString = "Data Source=test.db3;Pooling=False;FailIfMissing=False";
        cnn.Open();

        using (DbCommand cmd = cnn.CreateCommand())
        {
          cmd.CommandText = "TYPES integer, nvarchar, double;SELECT 1, 2, 3;";
          using (DbDataReader reader = cmd.ExecuteReader())
          {
            reader.Read();
          }
        }

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
