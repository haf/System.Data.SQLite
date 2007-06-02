using System;
using System.Data;
using System.Text;
using System.Data.Common;
using System.Data.SQLite;

namespace test
{
  class Program
  {
    static void Main(string[] args)
    {
      DbProviderFactory fact;
      fact = DbProviderFactories.GetFactory("System.Data.SQLite");

      System.IO.File.Delete("test.db3");
      using (DbConnection cnn = fact.CreateConnection())
      {
        cnn.ConnectionString = "Data Source=test.db3";
        cnn.Open();

//        using (DbCommand cmd = cnn.CreateCommand())
//        {
//          cmd.CommandText = @"CREATE TABLE test (col1 VARCHAR(30), col2 DECIMAL(10, 4));
//INSERT INTO test VALUES('test 1', 3);
//INSERT INTO test VALUES('test 2', 4.44);";
//          object value = cmd.ExecuteNonQuery();

//          cmd.CommandText = "SELECT col1, SUM(col2) FROM test GROUP BY col1";

//          using (DbDataReader reader = cmd.ExecuteReader())
//          {
//            while (reader.Read())
//            {
//              object val1 = reader[0];
//              object val2 = reader[1];
//            }
//          }
//          Console.ReadLine();
//        }

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
