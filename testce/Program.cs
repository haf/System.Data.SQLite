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

        ///////////////////////////////////////////////////////////////////////

        private static string GetConnectionString(
            string directory
            )
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
                using (StreamReader streamReader = File.OpenText(Path.Combine(
                        directory, "test.cfg")))
                {
                    connectionString = streamReader.ReadToEnd().Trim();
                }
            }
            catch
            {
                // do nothing.
            }

            return connectionString;
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetInitializationSQL(
            string directory
            )
        {
            string sql = null;

            try
            {
                //
                // NOTE: Attempt to open the SQL file associated with this test
                //       executable.  If present, it can contain SQL statements
                //       to be executed against the new connection prior to the
                //       tests running.
                //
                using (StreamReader streamReader = File.OpenText(Path.Combine(
                        directory, "test.sql")))
                {
                    sql = streamReader.ReadToEnd().Trim();
                }
            }
            catch
            {
                // do nothing.
            }

            return sql;
        }

        ///////////////////////////////////////////////////////////////////////

        internal static void ExecuteInitializationSQL(
            DbConnection connection,
            string sql,
            bool isolated
            )
        {
            if (!String.IsNullOrEmpty(sql))
            {
                if (isolated)
                {
                    using (DbConnection newConnection = NewConnection())
                    {
                        using (DbCommand command = newConnection.CreateCommand())
                        {
                            command.CommandText = sql;

                            /* IGNORED */
                            command.ExecuteNonQuery(); /* throw */
                        }
                    }
                }
                else
                {
                    if (connection == null)
                        return;

                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = sql;

                        /* IGNORED */
                        command.ExecuteNonQuery(); /* throw */
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        internal static DbConnection NewConnection()
        {
            return new SQLiteConnection();
        }

        ///////////////////////////////////////////////////////////////////////

        [MTAThread]
        static int Main(string[] args)
        {
            bool autoClose = false;
            bool isolatedSql = false;
            int exitCode = 2; /* INCOMPLETE */
            Assembly assembly = Assembly.GetExecutingAssembly();
            AssemblyName assemblyName = assembly.GetName();
            string directory = Path.GetDirectoryName(assemblyName.CodeBase);

            if (args.Length > 0)
            {
                try { autoClose = bool.Parse(args[0]); }
                catch { }
            }

            if (args.Length > 1)
            {
                try { isolatedSql = bool.Parse(args[1]); }
                catch { }
            }

            try { File.Delete(directory + "\\test.db"); }
            catch { }

            SQLiteFunction.RegisterFunction(typeof(TestFunc));
            SQLiteFunction.RegisterFunction(typeof(MyCount));
            SQLiteFunction.RegisterFunction(typeof(MySequence));

            using (DbConnection cnn = NewConnection())
            {
                string connectionString = GetConnectionString(directory);

                //
                // NOTE: If we are unable to obtain a valid connection string
                //       bail out now.
                //
                if (connectionString != null)
                {
                    //
                    // NOTE: Replace the "{DataDirectory}" token, if any, in
                    //       the connection string with the actual directory
                    //       this test assembly is executing from.
                    //
                    connectionString = connectionString.Replace(
                      "{DataDirectory}", directory);

                    cnn.ConnectionString = connectionString;
                    cnn.Open();

                    string sql = GetInitializationSQL(directory);

                    ExecuteInitializationSQL(cnn, sql, isolatedSql);

                    TestCases tests = new TestCases(
                        connectionString, cnn, sql, autoClose, isolatedSql);

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
