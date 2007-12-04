using System;
using System.Data;
using System.Text;
using System.Data.Common;
using System.Data.SQLite;

namespace test
{
  class Program
  {
    [MTAThread]
    static void Main()
    {
      DbConnection cnn;

      SQLiteFunction.RegisterFunction(typeof(TestFunc));
      SQLiteFunction.RegisterFunction(typeof(MyCount));
      SQLiteFunction.RegisterFunction(typeof(MySequence));

      try
      {
        System.IO.File.Delete("test.db3");
      }
      catch
      {
      }

      using (cnn = new SQLiteConnection())
      {
        TestCases tests = new TestCases();

        cnn.ConnectionString = "Data Source=test.db3;Password=yVXL39etehPX";
        cnn.Open();
        tests.Run(cnn);

        System.Windows.Forms.Application.Run(tests.frm);
      }
    }
  }
}
