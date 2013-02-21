/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

using System;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace test
{
  class Program
  {
    private static readonly string DefaultConnectionString =
        "Data Source={DataDirectory}\\test.db;Password=yVXL39etehPX;";

    internal static DbConnection NewConnection()
    {
        return new SQLiteConnection();
    }

    [MTAThread]
    static int Main(string[] args)
    {
      bool autoClose = false;
      int exitCode = 2; /* INCOMPLETE */
      Assembly assembly = Assembly.GetExecutingAssembly();
      AssemblyName assemblyName = assembly.GetName();
      string directory = Path.GetDirectoryName(assemblyName.CodeBase);

      if (args.Length > 0)
      {
          try { autoClose = bool.Parse(args[0]); } catch { }
      }

      try { File.Delete(directory + "\\test.db"); } catch { }

      SQLiteFunction.RegisterFunction(typeof(TestFunc));
      SQLiteFunction.RegisterFunction(typeof(MyCount));
      SQLiteFunction.RegisterFunction(typeof(MySequence));

      using (DbConnection cnn = NewConnection())
      {
        string connectionString = DefaultConnectionString;

        try
        {
          //
          // NOTE: Attempt to open the configuration file associated with
          //       this test executable.  It should contain *EXACTLY* one
          //       line, which will be the connection string to use for
          //       this test run.
          //
          using (StreamReader streamReader = File.OpenText(
              directory + "\\test.cfg"))
          {
            connectionString = streamReader.ReadToEnd().Trim();
          }
        }
        catch
        {
          // do nothing.
        }

        //
        // NOTE: If we are unable to obtain a valid connection string
        //       bail out now.
        //
        if (connectionString != null)
        {
          //
          // NOTE: Replace the "{DataDirectory}" token, if any, in the
          //       connection string with the actual directory this test
          //       assembly is executing from.
          //
          connectionString = connectionString.Replace(
            "{DataDirectory}", directory);

          cnn.ConnectionString = connectionString;
          cnn.Open();

          TestCases tests = new TestCases(connectionString, cnn, autoClose);

          tests.Run();

          Application.Run(tests.frm);

          if (tests.Succeeded())
              exitCode = 0; /* SUCCESS */
          else
              exitCode = 1; /* FAILURE */
        }
      }

      return exitCode;
    }
  }
}
