using System;
using System.Data;
using System.Text;
using System.Data.Common;
using System.Data.SQLite;
using System.Data.SqlClient;
using System.Transactions;

namespace test
{
  class Program
  {
    static void Main(string[] args)
    {
      DbProviderFactory fact;
      fact = DbProviderFactories.GetFactory("System.Data.SQLite");

      System.IO.File.Delete("test.db3");

      //SqlConnection cnn2 = new SqlConnection("Data Source=(local);Initial Catalog=iDiscover;Integrated Security=True");
      //cnn2.Open();
      //cnn2.BeginTransaction();
      //cnn2.Close();

      //cnn2 = new SqlConnection("Data Source=(local);Initial Catalog=iDiscover;Integrated Security=True");
      //cnn2.Open();
      //cnn2.BeginTransaction();
      //cnn2.Close();

      SQLiteConnection cnn = new SQLiteConnection();
      {
        cnn.ConnectionString = "Data Source=test.db3;Pooling=False;Password=yVXL39etehPX";
        cnn.Open();

        //using (DbCommand cmd = cnn.CreateCommand())
        //{
        //  cmd.CommandText = "CREATE TABLE Foo(ID integer primary key, myvalue varchar(50))";
        //  cmd.ExecuteNonQuery();

        //  cmd.CommandText = "CREATE TABLE Foo2(ID integer primary key, myvalue2)";
        //  cmd.ExecuteNonQuery();

        //  cmd.CommandText = "create view myview as select a.id, a.myvalue, b.myvalue2 from foo as a inner join foo2 as b on a.id = b.id";
        //  cmd.ExecuteNonQuery();

        //  cmd.CommandText = "select * from myview";
        //  using (DbDataReader reader = cmd.ExecuteReader())
        //  {
        //    DataTable tbl = reader.GetSchemaTable();

        //    Type t = reader.GetFieldType(0);
        //    t = reader.GetFieldType(1);
        //    t = reader.GetFieldType(2);
        //  }
        //}

        //cnn.BeginTransaction();
        //cnn.Close();

        //cnn = new SQLiteConnection("Data Source=test.db3;Pooling=True");
        //cnn.Open();
        //cnn.BeginTransaction();

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
