/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

using System;
using System.Data.Common;
using System.Data;
using System.Data.SQLite;
using System.Transactions;
using System.Collections.Generic;
using System.Text;

namespace test
{
  internal class TestCases : TestCaseBase
  {
    private const int NumThreads = 8;
    private const int ThreadTimeout = 60000;

    private List<string> droptables = new List<string>();
    private List<string> maydroptable = new List<string>();

#if !INTEROP_LOG
    private long logevents = 0;
#endif

    internal TestCases()
    {
    }

    internal TestCases(DbProviderFactory factory, string connectionString)
      : base(factory, connectionString)
    {
    }

    /// <summary>
    /// Inserts binary data into the database using a named parameter
    /// </summary>
    internal void BinaryInsert()
    {
      using (DbCommand cmd = _cnn.CreateCommand())
      {
        cmd.CommandText = "INSERT INTO TestCase(Field6) VALUES(@bin)";
        DbParameter Field6 = cmd.CreateParameter();

        byte[] b = new byte[4000];
        b[0] = 1;
        b[100] = 2;
        b[1000] = 3;
        b[2000] = 4;
        b[3000] = 5;

        Field6.ParameterName = "@bin";
        Field6.Value = b;

        cmd.Parameters.Add(Field6);

        cmd.ExecuteNonQuery();
      }
    }

    internal void CheckLocked()
    {
      // Lets make sure the database isn't locked.  If it is, we've failed.
      using (DbConnection newcnn = ((ICloneable)_cnn).Clone() as DbConnection)
      using (DbCommand cmd = newcnn.CreateCommand())
      {
        if (newcnn.State != ConnectionState.Open) newcnn.Open();

        cmd.CommandText = "INSERT INTO TestCase (Field1) SELECT 1 WHERE 1 = 2";
        cmd.ExecuteNonQuery();
      }
    }

    internal void CheckSQLite()
    {
      if (_fact.GetType().Name.IndexOf("SQLite", StringComparison.OrdinalIgnoreCase) == -1)
        throw new InconclusiveException("Not a SQLite database");
    }

#if INTEROP_CODEC
    /// <summary>
    /// Tests changing password on an encrypted database.
    /// </summary>
    [Test]
    internal void ChangePasswordTest()
    {
        if (_fact.GetType().Name.IndexOf("SQLite", StringComparison.OrdinalIgnoreCase) > -1)
        {
            // Opens an unencrypted database
            SQLiteConnection cnn = new SQLiteConnection(_cnnstring.ConnectionString);

            cnn.Open();

            // Encrypts the database. The connection remains valid and usable afterwards.
            cnn.ChangePassword("mypassword");

            maydroptable.Add("ChangePasswordTest");
            if (cnn.State != ConnectionState.Open) cnn.Open();
            using (DbCommand cmd = cnn.CreateCommand())
            {
                cmd.CommandText = "CREATE TABLE ChangePasswordTest(ID int primary key)";
                cmd.ExecuteNonQuery();
            }

            cnn.Close();

            // Try re-opening with bad password
            cnn.SetPassword("!mypassword");
            cnn.Open();
            cnn.Close();

            // Try re-opening with good password
            cnn.SetPassword("mypassword");
            cnn.Open();

            // Decrpyt database
            cnn.ChangePassword("");

            cnn.Close();

            ///////////////////////////////////////////////////////////////////

            cnn.Open();

            // Re-Encrypts the database. The connection remains valid and usable afterwards.
            cnn.ChangePassword("mypassword");
            cnn.ChangePassword("mynewerpassword");

            maydroptable.Add("ChangePasswordTest2");
            if (cnn.State != ConnectionState.Open) cnn.Open();
            using (DbCommand cmd = cnn.CreateCommand())
            {
                cmd.CommandText = "CREATE TABLE ChangePasswordTest2(ID int primary key)";
                cmd.ExecuteNonQuery();
            }

            // Decrpyt database
            cnn.ChangePassword("");
            cnn.Close();

            ///////////////////////////////////////////////////////////////////

            // Try opening now without password
            cnn.Open();
            cnn.Close();

        }
    }
#endif

    [Test(Sequence=1)]
    internal string VersionTest()
    {
      CheckSQLite();
      string[] version = _cnn.ServerVersion.Split('.');
      if (Convert.ToInt32(version[0]) < 3
        || (Convert.ToInt32(version[0]) == 3 && Convert.ToInt32(version[1]) < 6)
        || (Convert.ToInt32(version[0]) == 3 && Convert.ToInt32(version[1]) == 6 && Convert.ToInt32(version[2]) < 1)
        ) throw new Exception(String.Format("SQLite Engine is {0}.  Minimum supported version is 3.6.1", _cnn.ServerVersion));

      return String.Format("SQLite Engine is {0}", _cnn.ServerVersion);
    }

    //[Test(Sequence = 1)]
    internal void ParseTest()
    {
      DataTable tbl = _cnn.GetSchema("ViewColumns");
      DataTable tbl2 = _cnn.GetSchema("Views");

      using (DbCommand cmd = _cnn.CreateCommand())
      {
        cmd.Parameters.Add(cmd.CreateParameter());
        cmd.Parameters[0].Value = 1;

        cmd.Parameters.Add(cmd.CreateParameter());
        cmd.Parameters[1].Value = 1;

        cmd.CommandText = "select * from sqlite_master limit ? offset ?";
        object obj = cmd.ExecuteScalar();

        cmd.CommandText = @"
CREATE TEMP TABLE A(ID INTEGER, BID INTEGER);CREATE TEMP TABLE B(ID INTEGER, MYVAL VARCHAR);
INSERT INTO A (ID, BID) VALUES(2, 1);
INSERT INTO B (ID, MYVAL) VALUES(1,'TEST');
";
        cmd.ExecuteNonQuery();
        
        cmd.CommandText = "select *, (select 1 as c from b where b.id = a.bid) from a;";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
        }

        cmd.CommandText = "select a.id as aa from a where (select 1 from (select 1 where 1 = aa));";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
        }
        
        cmd.CommandText = "select *, (select count(c) from (select 1 as c from b where b.id = a.bid)) from a;";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
        }
      }
    }

    [Test(Sequence = 39)]
    internal void MultipleFunctions()
    {
      CheckSQLite();
      using (DbCommand cmd = _cnn.CreateCommand())
      {
        cmd.CommandText = "SELECT MYCOUNT(Field1), MYCOUNT(Field2) FROM TestCase";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
        }
      }
    }

#if USE_INTEROP_DLL && INTEROP_EXTENSION_FUNCTIONS
    [Test(Sequence = 8)]
    internal void FunctionWithCollation()
    {
      CheckSQLite();
      using (DbCommand cmd = _cnn.CreateCommand())
      {
        cmd.CommandText = "SELECT CHARINDEX('pat', 'thepat'), CHARINDEX('pat', 'THEPAT'), CHARINDEX('pat' COLLATE NOCASE, 'THEPAT' COLLATE NOCASE)";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
          if (reader.GetInt64(0) != reader.GetInt64(2) || reader.GetInt64(1) != 0 || reader.GetInt64(0) != 4)
            throw new Exception("CharIndex returned wrong results!");
        }
      }
    }

    [Test(Sequence = 9)]
    internal void FunctionWithCollation2()
    {
      CheckSQLite();
      using (DbCommand cmd = _cnn.CreateCommand())
      {
        cmd.CommandText = "SELECT CASETEST('pat', 'pat'), CASETEST('pat', 'PAT'), CASETEST('pat' COLLATE NOCASE, 'PAT' COLLATE NOCASE), CASETEST('pat' COLLATE MYSEQUENCE, 'PAT' COLLATE MYSEQUENCE), CASETEST('tap', 'TAP' COLLATE NOCASE)";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
          if (reader.GetInt64(0) != reader.GetInt64(2) || reader.GetInt64(1) != 1 || reader.GetInt64(0) != 0)
            throw new Exception("CharIndex returned wrong results!");
        }
      }
    }
#endif

    [Test]
    internal void DataTypesSchema()
    {
      using (DataTable tbl = _cnn.GetSchema("DataTypes"))
      {
      }
    }

    /// <summary>
    /// Make sure our implementation of ClearPool() behaves exactly as the SqlClient version is documented to behave.
    /// </summary>
    [Test(Sequence=90)]
    internal void ClearPoolTest()
    {
      string table = "clearpool";
      string temp = "TEMP";

      if (_fact.GetType().Name.IndexOf("SQLite", StringComparison.OrdinalIgnoreCase) == -1)
      {
        temp = String.Empty;
        table = "#clearpool";
      }

      object value;
      if (_cnnstring.TryGetValue("Pooling", out value) == false) throw new Exception("Pooling not present in connection string");
      if ((bool)value == false) throw new InconclusiveException("Pooling not enabled in the connection string");

      string sql = String.Format("CREATE {0} TABLE {1}(id int primary key);", temp, table);
      using (DbCommand cmd = _cnn.CreateCommand())
      {
        // Create a temp table in the main connection so we can confirm our new connections are using true new connections
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
      }

      for (int n = 0; n < 10; n++)
      {
        using (DbConnection newcnn = ((ICloneable)_cnn).Clone() as DbConnection)
        {
          if (newcnn.State != ConnectionState.Open) newcnn.Open();
          using (DbCommand cmd = newcnn.CreateCommand())
          {
            // If the pool is properly implemented and the pooled connection properly destroyed, this command will succeed.
            // If the new connection was obtained from the pool even after we cleared it, then this table will already exist
            // and the test fails.
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
          }
          // Try and clear the pool associated with this file
          newcnn.GetType().InvokeMember("ClearPool", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Public, null, null, new object[] { newcnn });

          // At this point when the connection is destroyed, it should not be returned to the pool, but instead disposed.
        }
      }
    }

    /// <summary>
    /// This tests ClearAllPools() functionality.  Makes sure that the pool is working properly and clearing properly.
    /// </summary>
    [Test(Sequence = 100)]
    internal void ClearAllPoolsTest()
    {
      string table = "clearpool";
      string temp = "TEMP";
      string exists = " IF NOT EXISTS ";

      if (_fact.GetType().Name.IndexOf("SQLite", StringComparison.OrdinalIgnoreCase) == -1)
      {
        temp = String.Empty;
        exists = String.Empty;
        table = "#clearpool";
      }

      object value;
      if (_cnnstring.TryGetValue("Pooling", out value) == false) throw new Exception("Pooling not present in connection string");
      if ((bool)value == false) throw new InconclusiveException("Pooling not enabled in the connection string");

      string sql = String.Format("CREATE {0} TABLE {2}{1}(id int primary key);", temp, table, exists);

      _cnn.GetType().InvokeMember("ClearAllPools", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Public, null, null, null);

      DbConnection[] arr = new DbConnection[10];

      try
      {
        // Create 10 connections and create temporary tables on them
        for (int n = 0; n < 10; n++)
        {
          arr[n] = ((ICloneable)_cnn).Clone() as DbConnection;
          if (arr[n].State != ConnectionState.Open) arr[n].Open();

          using (DbCommand cmd = arr[n].CreateCommand())
          {
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
            cmd.CommandText = String.Format("INSERT INTO {1} (id) VALUES({0})", n, table);
            cmd.ExecuteNonQuery();
          }

          switch (n)
          {
            case 2: // Put this one back into the pool
              arr[n].Dispose();
              arr[n] = null;
              break;
            case 4:
              // Clear all the pools
              _cnn.GetType().InvokeMember("ClearAllPools", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Public, null, null, null);
              break;
          }
        }

        // Now close all the connections.  Only the last 5 should go into the pool
        for (int n = 0; n < 10; n++)
        {
          if (arr[n] != null)
          {
            arr[n].Dispose();
            arr[n] = null;
          }
        }

        // Open 10 connections.  They should either have a clearpool containing an id of 5 or greater,
        // or should have no clearpool table at all.
        for (int n = 0; n < 10; n++)
        {
          arr[n] = ((ICloneable)_cnn).Clone() as DbConnection;
          if (arr[n].State != ConnectionState.Open) arr[n].Open();

          using (DbCommand cmd = arr[n].CreateCommand())
          {
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
            cmd.CommandText = String.Format("SELECT [id] FROM {0}", table);
            object o = cmd.ExecuteScalar();

            if (o == null || o == DBNull.Value)
              continue; // No data in the table at all, which means we must've just created it -- connection wasn't part of the pool

            if (Convert.ToInt32(o) < 5)
              throw new Exception("Unexpected data returned from table!");
          }
        }

        // Clear all the pools
        _cnn.GetType().InvokeMember("ClearAllPools", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Public, null, null, null);

      }
      finally
      {
        // Close all the connections
        for (int n = 0; n < 10; n++)
        {
          if (arr[n] != null)
          {
            arr[n].Dispose();
            arr[n] = null;
          }
        }
        // Clear all the pools
        _cnn.GetType().InvokeMember("ClearAllPools", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Public, null, null, null);
      }
    }

    [Test(Sequence = 50)]
    internal void CoersionTest()
    {
      using (DbCommand cmd = _cnn.CreateCommand())
      {
        cmd.CommandText = "SELECT Field1, Field2, [Fiëld3], [Fiæld4], Field5, 'A', 1, 1 + 1, 3.14159 FROM TestCase";
        using (DbDataReader rd = cmd.ExecuteReader())
        {
          if (rd.Read())
          {
            object Field1 = rd.GetInt32(0);
            object Field2 = rd.GetDouble(1);
            object Field3 = rd.GetString(2);
            object Field4 = rd.GetString(3).TrimEnd();
            object Field5 = rd.GetDateTime(4);

            // The next statement should cause an exception
            try
            {
              Field1 = rd.GetString(0);
              throw new Exception("Should have failed type checking!");
            }
            catch (InvalidCastException)
            {
            }

            try
            {
              Field2 = rd.GetString(1);
              throw new Exception("Should have failed type checking!");
            }
            catch (InvalidCastException)
            {
            }
            Field3 = rd.GetString(2);
            Field4 = rd.GetString(3);

            Field1 = rd.GetInt32(0);

            try
            {
              Field2 = rd.GetInt32(1);
              throw new Exception("Should have failed type checking!");
            }
            catch (InvalidCastException)
            {
            }

            try
            {
              Field3 = rd.GetInt32(2);
              throw new Exception("Should have failed type checking!");
            }
            catch (InvalidCastException)
            {
            }

            try
            {
              Field4 = rd.GetInt32(3);
              throw new Exception("Should have failed type checking!");
            }
            catch (InvalidCastException)
            {
            }
            try
            {
              Field5 = rd.GetInt32(4);
              throw new Exception("Should have failed type checking!");
            }
            catch (InvalidCastException)
            {
            }

            try
            {
              Field3 = rd.GetDecimal(2);
              throw new Exception("Should have failed type checking!");
            }
            catch (InvalidCastException)
            {
            }
            catch (FormatException)
            {
            }
            try
            {
              Field4 = rd.GetDecimal(3);
              throw new Exception("Should have failed type checking!");
            }
            catch (InvalidCastException)
            {
            }
            catch (FormatException)
            {
            }
            try
            {
              Field5 = rd.GetDecimal(4);
              throw new Exception("Should have failed type checking!");
            }
            catch (InvalidCastException)
            {
            }
            catch (FormatException)
            {
            }
          }
          else throw new Exception("No data in table");
        }
      }
    }

    [Test(Sequence = 10)]
    internal void CreateTable()
    {
      droptables.Add("TestCase");

      using (DbCommand cmd = _cnn.CreateCommand())
      {
        if (_fact.GetType().Name.IndexOf("SQLite", StringComparison.OrdinalIgnoreCase) == -1)
          cmd.CommandText = "CREATE TABLE TestCase (ID bigint primary key identity, Field1 integer, Field2 Float, [Fiëld3] VARCHAR(50), [Fiæld4] CHAR(10), Field5 DateTime, Field6 Image)";
        else
          cmd.CommandText = "CREATE TABLE TestCase (ID integer primary key autoincrement, Field1 int, Field2 Float, [Fiëld3] VARCHAR(50), [Fiæld4] CHAR(10), Field5 DateTime, Field6 Image)";

        cmd.ExecuteNonQuery();
      }
    }

    [Test(Sequence = 1100)]
    internal string DataAdapterTest()
    {
      return DataAdapter(false);
    }

    [Test(Sequence = 1200)]
    internal string DataAdapterWithIdentityFetch()
    {
      return DataAdapter(true);
    }

    /// <summary>
    /// Utilizes the SQLiteCommandBuilder, 
    /// which in turn utilizes SQLiteDataReader's GetSchemaTable() functionality
    /// This insert is slow because it must raise callbacks before and after every update.
    /// For a fast update, see the FastInsertMany function beneath this one
    /// </summary>
    internal string DataAdapter(bool bWithIdentity)
    {
      StringBuilder builder = new StringBuilder();

      using (DbTransaction dbTrans = _cnn.BeginTransaction())
      {
        using (DbDataAdapter adp = _fact.CreateDataAdapter())
        {
          using (DbCommand cmd = _cnn.CreateCommand())
          {
            cmd.Transaction = dbTrans;
            cmd.CommandText = "SELECT * FROM TestCase WHERE 1 = 2";
            adp.SelectCommand = cmd;

            using (DbCommandBuilder bld = _fact.CreateCommandBuilder())
            {
              bld.DataAdapter = adp;
              using (adp.InsertCommand = (DbCommand)((ICloneable)bld.GetInsertCommand()).Clone())
              {
                if (bWithIdentity)
                {
                  if (_fact.GetType().Name.IndexOf("SQLite", StringComparison.OrdinalIgnoreCase) == -1)
                    adp.InsertCommand.CommandText += ";SELECT SCOPE_IDENTITY() AS [ID]";
                  else
                    adp.InsertCommand.CommandText += ";SELECT last_insert_rowid() AS [ID]";
                  adp.InsertCommand.UpdatedRowSource = UpdateRowSource.FirstReturnedRecord;
                }
                bld.DataAdapter = null;

                using (DataTable tbl = new DataTable())
                {
                  adp.Fill(tbl);
                  for (int n = 0; n < 10000; n++)
                  {
                    DataRow row = tbl.NewRow();
                    row[1] = n + (50000 * ((bWithIdentity == true) ? 2 : 1));
                    tbl.Rows.Add(row);
                  }

                  //Console.WriteLine(String.Format("          Inserting using CommandBuilder and DataAdapter\r\n          ->{0} (10,000 rows) ...", (bWithIdentity == true) ? "(with identity fetch)" : ""));
                  int dtStart = Environment.TickCount;
                  adp.Update(tbl);
                  int dtEnd = Environment.TickCount;
                  dtEnd -= dtStart;
                  builder.AppendFormat("Insert Ends in {0} ms ... ", (dtEnd));

                  dtStart = Environment.TickCount;
                  dbTrans.Commit();
                  dtEnd = Environment.TickCount;
                  dtEnd -= dtStart;
                  builder.AppendFormat("Commits in {0} ms", (dtEnd));

                  if (bWithIdentity)
                  {
                    using (DataTable tbl2 = new DataTable())
                    {
                      adp.SelectCommand.CommandText = "SELECT * FROM TestCase WHERE Field1 BETWEEN 100000 AND 199999 ORDER BY Field1";
                      adp.Fill(tbl2);

                      if (tbl2.Rows.Count != tbl.Rows.Count) throw new Exception("Selected data doesn't match updated data!");

                      for (int n = 0; n < tbl.Rows.Count; n++)
                      {
                        if (tbl.Rows[n][0].Equals(tbl2.Rows[n][0]) == false)
                          throw new Exception("Fetched identity doesn't match selected identity!");
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
      return builder.ToString();
    }

    /// <summary>
    /// Make sure a datareader can run even if the parent command is disposed, and that the connection is closed
    /// by the datareader when it is finished.
    /// </summary>
    [Test]
    internal void DataReaderCleanup()
    {
      DbConnection newcnn = ((ICloneable)_cnn).Clone() as DbConnection;
      DbCommand cmd = newcnn.CreateCommand();

      try
      {
        if (newcnn.State != ConnectionState.Open)
          newcnn.Open();

        cmd.CommandText = "SELECT 1, 2, 3";
        using (DbDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
        {
          cmd.Dispose(); // Dispose of the command while an open reader is active ... should still be able to read
          try
          {
            reader.Read();
          }
          catch
          {
            throw new Exception("Unable to read from a DataReader!");
          }

          if (reader.GetInt32(0) != 1 || reader.GetInt32(1) != 2 || reader.GetInt32(2) != 3)
            throw new Exception("Unexpected return values from reader!");

          reader.Close(); // Close the reader, and check if the connection is closed

          if (newcnn.State != ConnectionState.Closed)
            throw new Exception("DataReader failed to cleanup!");
        }
      }
      finally
      {
        cmd.Dispose();
        newcnn.Dispose();
      }
    }

    [Test]
    internal void DataTypeTest()
    {
      DateTime now = DateTime.Now;

      using (DbCommand cmd = _cnn.CreateCommand())
      {
        droptables.Add("datatypetest");

        if (_fact.GetType().Name.IndexOf("SQLite", StringComparison.OrdinalIgnoreCase) == -1)
          cmd.CommandText = "create table datatypetest(id bigint identity primary key, myvalue sql_variant, datetimevalue datetime, decimalvalue decimal(38,18))";
        else
          cmd.CommandText = "create table datatypetest(id integer primary key, myvalue sql_variant, datetimevalue datetime, decimalvalue decimal(38,18))";

        cmd.ExecuteNonQuery();

        System.Globalization.CultureInfo oldculture = System.Threading.Thread.CurrentThread.CurrentCulture;
        System.Globalization.CultureInfo olduiculture = System.Threading.Thread.CurrentThread.CurrentUICulture;

        // Insert using a different current culture
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("es-ES");
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Threading.Thread.CurrentThread.CurrentCulture;

        try
        {
          cmd.CommandText = "insert into datatypetest(myvalue, datetimevalue, decimalvalue) values(@p1,@p2,@p3)";
          DbParameter p1 = cmd.CreateParameter();
          DbParameter p2 = cmd.CreateParameter();
          DbParameter p3 = cmd.CreateParameter();

          cmd.Parameters.Add(p1);
          cmd.Parameters.Add(p2);
          cmd.Parameters.Add(p3);

          p1.ParameterName = "@p1";
          p2.ParameterName = "@p2";
          p3.ParameterName = "@p3";

          p1.Value = (long)1;
          p2.Value = new DateTime(1753, 1, 1);
          p3.Value = (Decimal)1.05;
          cmd.ExecuteNonQuery();

          p1.ResetDbType();
          p2.ResetDbType();
          p3.ResetDbType();

          p1.Value = "One";
          p2.Value = "2001-01-01";
          p3.Value = (Decimal)1.0;
          cmd.ExecuteNonQuery();

          p1.ResetDbType();
          p2.ResetDbType();
          p3.ResetDbType();

          p1.Value = 1.01;
          p2.Value = now;
          p3.Value = (Decimal)9.91;
          cmd.ExecuteNonQuery();

          // Read using a different current culture
          System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
          System.Threading.Thread.CurrentThread.CurrentUICulture = System.Threading.Thread.CurrentThread.CurrentCulture;

          cmd.CommandText = "select myvalue, datetimevalue, decimalvalue from datatypetest";
          cmd.Parameters.Clear();
          using (DbDataReader reader = cmd.ExecuteReader())
          {
            for (int n = 0; n < 3; n++)
            {
              reader.Read();
              if (reader.GetValue(1).GetType() != reader.GetDateTime(1).GetType()) throw new Exception("DateTime type non-match");
              if (reader.GetValue(2).GetType() != reader.GetDecimal(2).GetType()) throw new Exception("Decimal type non-match");

              switch (n)
              {
                case 0:
                  if (reader.GetValue(0).GetType() != typeof(long)) throw new Exception("long type non-match");

                  if (reader.GetValue(0).Equals((long)1) == false) throw new Exception("long value non-match");
                  if (reader.GetValue(1).Equals(new DateTime(1753, 1, 1)) == false) throw new Exception(String.Format("DateTime value non-match expected {0} got {1}", new DateTime(1753, 1, 1), reader.GetValue(1)));
                  if (reader.GetValue(2).Equals((Decimal)1.05) == false) throw new Exception("Decimal value non-match");

                  if (reader.GetValue(0).Equals(reader.GetInt64(0)) == false) throw new Exception(String.Format("long value failed to match itself, {0} and {1}", reader.GetValue(0), reader.GetInt64(0)));
                  if (reader.GetValue(1).Equals(reader.GetDateTime(1)) == false) throw new Exception(String.Format("DateTime failed to match itself {0} and {1}", reader.GetValue(1), reader.GetDateTime(1)));
                  if (reader.GetValue(2).Equals(reader.GetDecimal(2)) == false) throw new Exception(String.Format("Decimal failed to match itself {0} and {1}", reader.GetValue(2), reader.GetDecimal(2)));
                  break;
                case 1:
                  if (reader.GetValue(0).GetType() != typeof(string)) throw new Exception("String type non-match");
                  if (reader.GetValue(0).Equals("One") == false) throw new Exception("String value non-match");
                  if (reader.GetValue(1).Equals(new DateTime(2001, 1, 1)) == false) throw new Exception(String.Format("DateTime value non-match expected {0} got {1}", new DateTime(2001, 1, 1), reader.GetValue(1)));
                  if (reader.GetValue(2).Equals((Decimal)1.0) == false) throw new Exception("Decimal value non-match");

                  if (reader.GetString(0) != "One") throw new Exception("String value non-match");
                  if (reader.GetValue(1).Equals(reader.GetDateTime(1)) == false) throw new Exception(String.Format("DateTime failed to match itself {0} and {1}", reader.GetValue(1), reader.GetDateTime(1)));
                  if (reader.GetValue(2).Equals(reader.GetDecimal(2)) == false) throw new Exception(String.Format("Decimal failed to match itself {0} and {1}", reader.GetValue(2), reader.GetDecimal(2)));
                  break;
                case 2:
                  if (reader.GetValue(0).GetType() != typeof(double)) throw new Exception("Double type non-match");
                  if (reader.GetValue(0).Equals(1.01) == false) throw new Exception("Double value non-match");
                  if (reader.GetValue(1).ToString() != now.ToString()) throw new Exception(String.Format("DateTime value non-match, expected {0} got {1}", now, reader.GetValue(1)));
                  if (reader.GetValue(2).Equals((Decimal)9.91) == false) throw new Exception("Decimal value non-match");

                  if (reader.GetDouble(0) != 1.01) throw new Exception("Double value non-match");
                  if (reader.GetValue(1).Equals(reader.GetDateTime(1)) == false) throw new Exception(String.Format("DateTime failed to match itself {0} and {1}", reader.GetValue(1), reader.GetDateTime(1)));
                  if (reader.GetValue(2).Equals(reader.GetDecimal(2)) == false) throw new Exception(String.Format("Decimal failed to match itself {0} and {1}", reader.GetValue(2), reader.GetDecimal(2)));
                  break;
              }
            }
          }
        }
        finally
        {
          System.Threading.Thread.CurrentThread.CurrentCulture = oldculture;
          System.Threading.Thread.CurrentThread.CurrentUICulture = olduiculture;
        }
      }
    }

    /// <summary>
    /// This is an mean ugly test that leaves a lot of open datareaders out on many connections
    /// to see if the database can survive being cloned a lot and disposed while active readers are up.
    /// </summary>
    [Test(Sequence = 40)]
    internal void LeakyDataReaders()
    {
      try
      {
        {
          DbConnection newcnn = null;
          try
          {
            for (int x = 0; x < 10000; x++)
            {
              if (newcnn == null)
              {
                newcnn = ((ICloneable)_cnn).Clone() as DbConnection;
              }

              if (newcnn.State != ConnectionState.Open)
                newcnn.Open();

              DbCommand cmd = newcnn.CreateCommand();
              cmd.CommandText = "SELECT * FROM TestCase";
              DbDataReader reader = cmd.ExecuteReader();
              reader.Read();
              object obj = reader[0];

              if (x % 500 == 0)
              {
                newcnn.Close();
                newcnn = null;
              }
            }
          }
          finally
          {
            if (newcnn != null)
              newcnn.Close();

            newcnn = null;
          }
        }
        CheckLocked();
      }
      catch (Exception e)
      {
        System.Diagnostics.Debug.WriteLine(e.Message);
      }
    }

    [Test(Sequence = int.MaxValue)]
    internal void DropTable()
    {
      DropTables(true);
    }

    internal void DropTables(bool throwError)
    {
      //string[] arr = new string[] { "TestCase", "datatypetest", "MultiThreadedTest", "fulltext", "guidtest", "keyinfotest", "stepreader", "nonexistent" };
      string errors = String.Empty;

      using (DbCommand cmd = _cnn.CreateCommand())
      {
        foreach(string table in droptables)
        {
          try
          {
            cmd.CommandText = String.Format("DROP TABLE{1} [{0}]", table, (throwError == false) ? " IF EXISTS" : "");
            cmd.ExecuteNonQuery();
          }
          catch (Exception e)
          {
            if (throwError == true)
              errors += String.Format("{0}\r\n", e.Message);
          }
        }

        foreach (string table in maydroptable)
        {
          try
          {
            cmd.CommandText = String.Format("DROP TABLE{1} [{0}]", table, (throwError == false) ? " IF EXISTS" : "");
            cmd.ExecuteNonQuery();
          }
          catch (Exception)
          {
          }
        }
      }

      if (String.IsNullOrEmpty(errors) == false)
        throw new Exception(errors);

    }

    [Test(Sequence = 1000)]
    internal string FastInsertMany()
    {
      StringBuilder builder = new StringBuilder();
      using (DbTransaction dbTrans = _cnn.BeginTransaction())
      {
        int dtStart;
        int dtEnd;

        using (DbCommand cmd = _cnn.CreateCommand())
        {
          cmd.Transaction = dbTrans;
          cmd.CommandText = "INSERT INTO TestCase(Field1) VALUES(@p1)";
          DbParameter Field1 = cmd.CreateParameter();

          Field1.ParameterName = "@p1";
          cmd.Parameters.Add(Field1);

          //Console.WriteLine(String.Format("          Fast insert using parameters and prepared statement\r\n          -> (100,000 rows) Begins ... "));
          dtStart = Environment.TickCount;
          for (int n = 0; n < 100000; n++)
          {
            Field1.Value = n + 200000;
            cmd.ExecuteNonQuery();
          }

          dtEnd = Environment.TickCount;
          dtEnd -= dtStart;
          builder.AppendFormat("Ends in {0} ms ... ", (dtEnd));
        }

        dtStart = Environment.TickCount;
        dbTrans.Commit();
        dtEnd = Environment.TickCount;
        dtEnd -= dtStart;
        builder.AppendFormat("Commits in {0} ms", (dtEnd));
      }
      return builder.ToString();
    }

    [Test]
    internal void FullTextTest()
    {
      CheckSQLite();

      using (DbCommand cmd = _cnn.CreateCommand())
      {
        droptables.Add("FullText");
        cmd.CommandText = "CREATE VIRTUAL TABLE FullText USING FTS3(name, ingredients);";
        cmd.ExecuteNonQuery();

        string[] names = { "broccoli stew", "pumpkin stew", "broccoli pie", "pumpkin pie" };
        string[] ingredients = { "broccoli peppers cheese tomatoes", "pumpkin onions garlic celery", "broccoli cheese onions flour", "pumpkin sugar flour butter" };
        int n;

        cmd.CommandText = "insert into FullText (name, ingredients) values (@name, @ingredient);";
        DbParameter name = cmd.CreateParameter();
        DbParameter ingredient = cmd.CreateParameter();

        name.ParameterName = "@name";
        ingredient.ParameterName = "@ingredient";

        cmd.Parameters.Add(name);
        cmd.Parameters.Add(ingredient);

        for (n = 0; n < names.Length; n++)
        {
          name.Value = names[n];
          ingredient.Value = ingredients[n];

          cmd.ExecuteNonQuery();
        }

        cmd.CommandText = "select rowid, name, ingredients from FullText where name match 'pie';";
        cmd.Parameters.Clear();

        int[] rowids = { 3, 4 };
        n = 0;

        using (DbDataReader reader = cmd.ExecuteReader())
        {
          while (reader.Read())
          {
            if (reader.GetInt64(0) != rowids[n++])
              throw new Exception("Unexpected rowid returned");

            if (n > rowids.Length) throw new Exception("Too many rows returned");
          }
        }
      }
    }

    [Test]
    internal void GuidTest()
    {
      using (DbCommand cmd = _cnn.CreateCommand())
      {
        Guid guid = Guid.NewGuid();

        droptables.Add("GuidTest");

        cmd.CommandText = "CREATE TABLE GuidTest(MyGuid uniqueidentifier)";
        cmd.ExecuteNonQuery();

        // Insert a guid as a default binary representation
        cmd.CommandText = "INSERT INTO GuidTest(MyGuid) VALUES(@b)";
        DbParameter parm = cmd.CreateParameter();
        parm.ParameterName = "@b";
        parm.Value = guid;
        cmd.Parameters.Add(parm);
        //((SQLiteParameterCollection)cmd.Parameters).AddWithValue("@b", guid);

        // Insert a guid as text
        cmd.ExecuteNonQuery();
        cmd.Parameters[0].Value = guid.ToString();
        cmd.Parameters[0].DbType = DbType.String;
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT MyGuid FROM GuidTest";
        cmd.Parameters.Clear();

        using (DbDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
          if (reader.GetFieldType(0) != typeof(Guid)) throw new Exception("Column is not a Guid");
          if (reader.GetGuid(0) != guid) throw new Exception(String.Format("Got guid {0}, expected {1}", reader.GetGuid(0), guid));

          reader.Read();
          if (reader.GetFieldType(0) != typeof(Guid)) throw new Exception("Column is not a Guid");
          if (reader.GetGuid(0) != guid) throw new Exception(String.Format("Got guid {0}, expected {1}", reader.GetGuid(0), guid));
        }
      }
    }

    [Test(Sequence = 20)]
    internal void InsertTable()
    {
      using (DbCommand cmd = _cnn.CreateCommand())
      {
        cmd.CommandText = "INSERT INTO TestCase(Field1, Field2, [Fiëld3], [Fiæld4], Field5) VALUES(1, 3.14159, 'Fiëld3', 'Fiæld4', '2005-01-01 13:49:00')";
        cmd.ExecuteNonQuery();
      }
    }

    [Test]
    internal string IterationTest1()
    {
      CheckSQLite();
      using (DbCommand cmd = _cnn.CreateCommand())
      {
        int dtStart;
        int dtEnd;
        int nCount;
        long n;

        cmd.CommandText = "SELECT Foo(ID, ID) FROM TestCase";
        cmd.Prepare();
        dtStart = Environment.TickCount;
        nCount = 0;
        using (DbDataReader rd = cmd.ExecuteReader())
        {
          while (rd.Read())
          {
            n = rd.GetInt64(0);
            nCount++;
          }
          dtEnd = Environment.TickCount;
        }
        return String.Format("User Function iteration of {0} records in {1} ms", nCount, (dtEnd - dtStart));
      }
    }

    [Test]
    internal string IterationTest2()
    {
      StringBuilder builder = new StringBuilder();
      using (DbCommand cmd = _cnn.CreateCommand())
      {
        int dtStart;
        int dtEnd;
        int nCount;
        long n;

        cmd.CommandText = "SELECT ID FROM TestCase";
        cmd.Prepare();
        dtStart = Environment.TickCount;
        nCount = 0;
        using (DbDataReader rd = cmd.ExecuteReader())
        {
          while (rd.Read())
          {
            n = rd.GetInt64(0);
            nCount++;
          }
          dtEnd = Environment.TickCount;
        }
        return String.Format("Raw iteration of {0} records in {1} ms", nCount, (dtEnd - dtStart));
      }
    }

    [Test]
    internal string IterationTest3()
    {
      StringBuilder builder = new StringBuilder();
      using (DbCommand cmd = _cnn.CreateCommand())
      {
        int dtStart;
        int dtEnd;
        int nCount;
        long n;

        cmd.CommandText = "SELECT ABS(ID) FROM TestCase";
        cmd.Prepare();
        dtStart = Environment.TickCount;
        nCount = 0;
        using (DbDataReader rd = cmd.ExecuteReader())
        {
          while (rd.Read())
          {
            n = rd.GetInt64(0);
            nCount++;
          }
          dtEnd = Environment.TickCount;
        }
        return String.Format("Intrinsic Function iteration of {0} records in {1} ms", nCount, (dtEnd - dtStart));
      }
    }

    [Test(Sequence=21)]
    internal void KeyInfoTest()
    {
      using (DbCommand cmd = _cnn.CreateCommand())
      {
        try
        {
          // First test against integer primary key (optimized) keyinfo fetch
          droptables.Add("keyinfotest");
          cmd.CommandText = "Create table keyinfotest (id integer primary key, myuniquevalue integer unique not null, myvalue varchar(50))";
          cmd.ExecuteNonQuery();

          cmd.CommandText = "Select * from keyinfotest";
          using (DbDataReader reader = cmd.ExecuteReader(CommandBehavior.KeyInfo | CommandBehavior.SchemaOnly))
          {
            using (DataTable tbl = reader.GetSchemaTable())
            {
              if (tbl.Rows.Count != 3) throw new Exception("Wrong number of columns returned");
            }
          }

          cmd.CommandText = "SELECT MyValue FROM keyinfotest";
          using (DbDataReader reader = cmd.ExecuteReader(CommandBehavior.KeyInfo | CommandBehavior.SchemaOnly))
          {
            using (DataTable tbl = reader.GetSchemaTable())
            {
              if (tbl.Rows.Count != 2) throw new Exception("Wrong number of columns returned");
            }
          }
        }
        finally
        {
        }

        cmd.CommandText = "DROP TABLE keyinfotest";
        cmd.ExecuteNonQuery();

        droptables.Remove("keyinfotest");

        try
        {
          // Now test against non-integer primary key (unoptimized) subquery keyinfo fetch
          droptables.Add("keyinfotest");
          cmd.CommandText = "Create table keyinfotest (id char primary key, myuniquevalue integer unique not null, myvalue varchar(50))";
          cmd.ExecuteNonQuery();

          cmd.CommandText = "SELECT MyValue FROM keyinfotest";
          using (DbDataReader reader = cmd.ExecuteReader(CommandBehavior.KeyInfo | CommandBehavior.SchemaOnly))
          {
            using (DataTable tbl = reader.GetSchemaTable())
            {
              if (tbl.Rows.Count != 2) throw new Exception("Wrong number of columns returned");
            }
          }

          cmd.CommandText = "Select * from keyinfotest";
          using (DbDataReader reader = cmd.ExecuteReader(CommandBehavior.KeyInfo | CommandBehavior.SchemaOnly))
          {
            using (DataTable tbl = reader.GetSchemaTable())
            {
              if (tbl.Rows.Count != 3) throw new Exception("Wrong number of columns returned");
            }
          }

          // Make sure commandbuilder can generate an update command with the correct parameter count
          using (DbDataAdapter adp = _fact.CreateDataAdapter())
          using (DbCommandBuilder builder = _fact.CreateCommandBuilder())
          {
            adp.SelectCommand = cmd;
            builder.DataAdapter = adp;
            builder.ConflictOption = ConflictOption.OverwriteChanges;

            //
            // NOTE: *MONO* This test fails on all recent versions of Mono (e.g.
            //       2.10, 2.11) for reasons that are presently unknown.
            //
            using (DbCommand updatecmd = builder.GetUpdateCommand())
            {
              if (updatecmd.Parameters.Count != 4)
                throw new Exception("Wrong number of parameters in update command!");
            }
          }
        }
        finally
        {
        }
      }
    }

    [Test]
    internal void ConnectionStringBuilder()
    {
      DbConnectionStringBuilder builder = _fact.CreateConnectionStringBuilder();
      if (builder is SQLiteConnectionStringBuilder)
      {
        bool pool = ((SQLiteConnectionStringBuilder)builder).Pooling;
      }
    }

    [Test]
    internal void LeakyCommands()
    {
      for (int n = 0; n < 100000; n++)
      {
        DbCommand cmd = _cnn.CreateCommand();
        cmd.CommandText = "SELECT * FROM sqlite_master";
        cmd.Prepare();
      }
      CheckLocked();
    }

    [Test(Sequence = 60)]
    internal void LockTest()
    {
      CheckSQLite();

      using (DbCommand cmd = _cnn.CreateCommand())
      {
        cmd.CommandText = "SELECT Field6 FROM TestCase WHERE Field6 IS NOT NULL";
        byte[] b = new byte[4000];

        using (DbDataReader rd = cmd.ExecuteReader())
        {
          if (rd.Read() == false) throw new Exception("No data to read!");

          rd.GetBytes(0, 0, b, 0, 4000);

          if (b[0] != 1) throw new Exception("Binary value non-match byte 0");
          if (b[100] != 2) throw new Exception("Binary value non-match byte 100");
          if (b[1000] != 3) throw new Exception("Binary value non-match byte 1000");
          if (b[2000] != 4) throw new Exception("Binary value non-match byte 2000");
          if (b[3000] != 5) throw new Exception("Binary value non-match byte 3000");

          using (DbConnection clone = (DbConnection)((ICloneable)_cnn).Clone())
          {
            if (clone.State != ConnectionState.Open) clone.Open();
            using (DbCommand newcmd = clone.CreateCommand())
            {
              newcmd.CommandText = "DELETE FROM TestCase WHERE Field6 IS NULL";
              newcmd.CommandTimeout = 2;
              int cmdStart = Environment.TickCount;
              int cmdEnd;

              try
              {
                newcmd.ExecuteNonQuery(); // should fail because there's a reader on the database
                throw new ArgumentException("Should not have allowed an execute with an open reader"); // If we got here, the test failed
              }
              catch (Exception e)
              {
                if (e is ArgumentException) throw new Exception(e.Message);

                cmdEnd = Environment.TickCount;
                if (cmdEnd - cmdStart < 2000 || cmdEnd - cmdStart > 3000)
                  throw new Exception("Did not give up the lock at the right time!"); // Didn't wait the right amount of time

              }
            }
          }
        }
      }
    }

    /// <summary>
    /// Execute multiple steps in a command and verify the results.  Makes sure that commands after a select still
    /// get executed even if MoveNext() isn't called explicitly to move things along.
    /// </summary>
    [Test]
    internal void MultiStepReaderTest()
    {
      using (DbCommand cmd = _cnn.CreateCommand())
      {
        droptables.Add("stepreader");
        cmd.CommandText = "CREATE TABLE stepreader (id int primary key);INSERT INTO stepreader values(1);SELECT * FROM stepreader;UPDATE stepreader set id = id + 1;";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
          if (reader.Read() == false) throw new Exception("Failed to read from the table");
          if (reader.GetInt32(0) != 1) throw new Exception(String.Format("Expected {0} got {1}", 1, reader.GetInt32(0)));
        }
        cmd.CommandText = "SELECT * FROM stepreader";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
          if (reader.Read() == false) throw new Exception("Failed to read from the table");
          if (reader.GetInt32(0) != 2) throw new Exception(String.Format("Expected {0} got {1}", 2, reader.GetInt32(0)));
        }
      }
    }

    internal class MTTest
    {
      internal DbConnection cnn;
      internal Exception e;
      internal System.Threading.Thread t;
      internal int value;
      internal System.Threading.ManualResetEvent ev;
    }

    [Test(Sequence=11)]
    internal void MultithreadingTest()
    {
      using (DbCommand cmd = _cnn.CreateCommand())
      {
        droptables.Add("MultiThreadedTest");
        if (_fact.GetType().Name.IndexOf("SQLite", StringComparison.OrdinalIgnoreCase) == -1)
          cmd.CommandText = "CREATE TABLE MultiThreadedTest(ID integer identity primary key, ThreadId integer, MyValue integer)";
        else
          cmd.CommandText = "CREATE TABLE MultiThreadedTest(ID integer primary key, ThreadId integer, MyValue integer)";

        cmd.ExecuteNonQuery();
      }

      System.Threading.ManualResetEvent[] events = new System.Threading.ManualResetEvent[NumThreads];
      MTTest[] arr = new MTTest[NumThreads];

      for (int n = 0; n < arr.Length; n++)
      {
        arr[n] = new MTTest();
        arr[n].t = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(MultithreadedTestThread));
        arr[n].t.IsBackground = true;
        arr[n].cnn = ((ICloneable)_cnn).Clone() as DbConnection;
        arr[n].ev = events[n] = new System.Threading.ManualResetEvent(false);
        arr[n].t.Start(arr[n]);
      }

      System.Threading.WaitHandle.WaitAll(events, ThreadTimeout);

      bool failed = false;
      Exception e = null;

      for (int n = 0; n < arr.Length; n++)
      {
        if (arr[n].t.Join(0) == false)
        {
          failed = true;
          arr[n].t.Abort();
          arr[n].t.Join();
        }
        if (arr[n].e != null) e = arr[n].e;
        arr[n].cnn.Dispose();
        arr[n].ev.Close();
      }
      if (failed) throw new Exception("One or more threads deadlocked");
      if (e != null) 
        throw e;
    }

    internal void MultithreadedTestThread(object obj)
    {
      MTTest test = obj as MTTest;

      if (test.cnn.State != ConnectionState.Open)
        test.cnn.Open();

      int start = Environment.TickCount;
      try
      {
        using (DbCommand cmd = test.cnn.CreateCommand())
        {
          bool once = false;
          while (!once || ((Environment.TickCount - start) < 2000))
          {
            using (DbTransaction trans = test.cnn.BeginTransaction())
            {
              cmd.CommandText = String.Format("SELECT * FROM MultiThreadedTest WHERE ThreadId = {0}", test.t.ManagedThreadId);
              cmd.Transaction = trans;
              using (DbDataReader reader = cmd.ExecuteReader())
              {
                while (reader.Read())
                {
                  test.value += Convert.ToInt32(reader[2]);
                }
              }
              cmd.CommandText = String.Format("INSERT INTO MultiThreadedTest(ThreadId, MyValue) VALUES({0}, {1})", test.t.ManagedThreadId, Environment.TickCount);
              cmd.ExecuteNonQuery();

              trans.Commit();
            }

            once = true;
          }
        }
      }
      catch (Exception e)
      {
        test.e = e;
      }
      finally
      {
        test.ev.Set();
      }
    }

    [Test]
    internal void ParameterizedInsert()
    {
      using (DbCommand cmd = _cnn.CreateCommand())
      {
        cmd.CommandText = "INSERT INTO TestCase(Field1, Field2, [Fiëld3], [Fiæld4], Field5) VALUES(@p1,@p2,@p3,@p4,@p5)";
        DbParameter Field1 = cmd.CreateParameter();
        DbParameter Field2 = cmd.CreateParameter();
        DbParameter Field3 = cmd.CreateParameter();
        DbParameter Field4 = cmd.CreateParameter();
        DbParameter Field5 = cmd.CreateParameter();

        Field1.ParameterName = "@p1";
        Field2.ParameterName = "@p2";
        Field3.ParameterName = "@p3";
        Field4.ParameterName = "@p4";
        Field5.ParameterName = "@p5";

        Field1.Value = 2;
        Field2.Value = 3.14159;
        Field3.Value = "Param Field3";
        Field4.Value = "Field4 Par";
        Field5.Value = DateTime.Now;

        cmd.Parameters.Add(Field1);
        cmd.Parameters.Add(Field2);
        cmd.Parameters.Add(Field3);
        cmd.Parameters.Add(Field4);
        cmd.Parameters.Add(Field5);

        cmd.ExecuteNonQuery();
      }
    }

    [Test]
    internal void ParameterizedInsertMissingParams()
    {
      using (DbCommand cmd = _cnn.CreateCommand())
      {
        cmd.CommandText = "INSERT INTO TestCase(Field1, Field2, [Fiëld3], [Fiæld4], Field5) VALUES(@p1,@p2,@p3,@p4,@p5)";
        DbParameter Field1 = cmd.CreateParameter();
        DbParameter Field2 = cmd.CreateParameter();
        DbParameter Field3 = cmd.CreateParameter();
        DbParameter Field4 = cmd.CreateParameter();
        DbParameter Field5 = cmd.CreateParameter();

        Field1.ParameterName = "@p1";
        Field2.ParameterName = "@p2";
        Field3.ParameterName = "@p3";
        Field4.ParameterName = "@p4";
        Field5.ParameterName = "@p5";

        Field1.DbType = System.Data.DbType.Int32;

        Field1.Value = 2;
        Field2.Value = 3.14159;
        Field3.Value = "Field3 Param";
        Field4.Value = "Field4 Par";
        Field5.Value = DateTime.Now;

        cmd.Parameters.Add(Field1);
        cmd.Parameters.Add(Field2);
        cmd.Parameters.Add(Field3);
        cmd.Parameters.Add(Field4);

        // Assertion here, not enough parameters
        try
        {
          cmd.ExecuteNonQuery();
          throw new Exception("Executed with a missing parameter");
        }
        catch (Exception) // Expected
        {
        }
      }
    }

    /// <summary>
    /// Call Prepare() on a multi-statement command text where the second command depends on the existence of the first.
    /// </summary>
    [Test]
    internal void PrepareTest()
    {
      using (DbCommand cmd = _cnn.CreateCommand())
      {
        droptables.Add("nonexistent");
        cmd.CommandText = "CREATE TABLE nonexistent(id int primary key);SELECT id FROM nonexistent UNION SELECT 1";
        cmd.Prepare();
        object ob = cmd.ExecuteScalar();

        if (ob == null || ob == DBNull.Value) throw new Exception("Multiple statements may not be supported");
        if (Convert.ToInt32(ob) != 1) throw new Exception(String.Format("Expected {0} got {1}", 1, ob));
      }
    }

    /// <summary>
    /// Checks to make sure transactions are rolled back before a connection goes back onto the pool
    /// </summary>
    [Test]
    internal void PoolingWithStealthTransactionTest()
    {
      object value;
      if (_cnnstring.TryGetValue("Pooling", out value) == false) throw new Exception("Pooling not present in connection string");
      if ((bool)value == false) throw new InconclusiveException("Pooling not enabled in the connection string");

      maydroptable.Add("PoolTest");

      for (int n = 0; n < 100; n++)
      {
        using (DbConnection newcnn = ((ICloneable)_cnn).Clone() as DbConnection)
        {
          if (newcnn.State != ConnectionState.Open) newcnn.Open();
          using (DbCommand cmd = newcnn.CreateCommand())
          {
            cmd.CommandText = "BEGIN TRANSACTION";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "CREATE TABLE PoolTest(ID int primary key)";
            cmd.ExecuteNonQuery();
          }
        }
      }
    }

    /// <summary>
    /// Checks to make sure transactions are rolled back before a connection goes back onto the pool
    /// </summary>
    [Test]
    internal void PoolingWithTransactionTest()
    {
      object value;
      if (_cnnstring.TryGetValue("Pooling", out value) == false) throw new Exception("Pooling not present in connection string");
      if ((bool)value == false) throw new InconclusiveException("Pooling not enabled in the connection string");

      maydroptable.Add("PoolTest");
      for (int n = 0; n < 100; n++)
      {
        using (DbConnection newcnn = ((ICloneable)_cnn).Clone() as DbConnection)
        {
          if (newcnn.State != ConnectionState.Open) newcnn.Open();
          DbTransaction trans = newcnn.BeginTransaction();
          using (DbCommand cmd = newcnn.CreateCommand())
          {
            cmd.Transaction = trans;
            cmd.CommandText = "CREATE TABLE PoolTest(ID int primary key)";
            cmd.ExecuteNonQuery();
          }
        }
      }
    }

    /// <summary>
    /// Checks to make sure we can open DB read only.
    /// </summary>
    [Test]
    internal void ReadOnlyTest()
    {
      string RO_connectionString = _cnnstring.ConnectionString;
      object value;
      if (_cnnstring.TryGetValue("Read Only", out value) == false)
      {
        throw new Exception("Read Only not supported by connection string");
      }
      if ((bool)value == false)
      {
        // "Read Only" not present in connection string - add it
        RO_connectionString += ";Read Only=true";
      }

      maydroptable.Add("ReadOnlyTest");

      using (DbConnection newcnn = ((ICloneable)_cnn).Clone() as DbConnection)
      {
        if (newcnn.State == ConnectionState.Open) 
        {
          newcnn.Close();
        }
        newcnn.ConnectionString = RO_connectionString;
        newcnn.Open();
        newcnn.Dispose();
      } 
    }

    /// <summary>
    /// Checks to extended error code result support.
    /// </summary>
    [Test]
    internal void ExtendedResultCodesTest()
    {
      if (_fact.GetType().Name.IndexOf("SQLite", StringComparison.OrdinalIgnoreCase) > -1)
      {
        SQLiteConnection cnn = new SQLiteConnection(_cnnstring.ConnectionString);

        cnn.Open();

        // Turn on extended result codes
        cnn.SetExtendedResultCodes(true);

        SQLiteErrorCode rc = cnn.ResultCode();
        SQLiteErrorCode xrc = cnn.ExtendedResultCode();

        cnn.Close();
      }
    }

#if !INTEROP_LOG
    //Logging EventHandler
    public void OnLogEvent(object sender, LogEventArgs logEvent)
    {
        object errorCode = logEvent.ErrorCode;
        string err_msg = logEvent.Message;
        logevents++;
    }

    /// <summary>
    /// Tests SQLITE_CONFIG_LOG support.
    /// </summary>
    [Test]
    internal void SetLogCallbackTest()
    {
        if (_fact.GetType().Name.IndexOf("SQLite", StringComparison.OrdinalIgnoreCase) > -1)
        {
            SQLiteConnection cnn = new SQLiteConnection(_cnnstring.ConnectionString);

            // create and add a log event handler
            SQLiteLogEventHandler logHandler = new SQLiteLogEventHandler(OnLogEvent);
            SQLiteFactory sqlite_fact = (SQLiteFactory)_fact;

            sqlite_fact.Log += logHandler;

            cnn.Open();

            logevents = 0;

            cnn.LogMessage(SQLiteErrorCode.Error, "test log event");

            if (logevents != 1)
                throw new Exception(String.Format(
                    "Log event count {0} incorrect.", logevents));

            cnn.Close();

            // remove the log handler before the connection is closed.
            sqlite_fact.Log -= logHandler;

        }
    }
#endif

    /// <summary>
    /// Open a reader and then attempt to write to test the writer's command timeout property
    /// SQLite doesn't allow a write when a reader is active.
    /// *** NOTE AS OF 3.3.8 this test no longer blocks because SQLite now allows you to update table(s)
    /// while a reader is active on the same connection.  Therefore the timeout test is invalid
    /// </summary>
    internal void TimeoutTest()
    {
      CheckSQLite();

      using (DbCommand cmdRead = _cnn.CreateCommand())
      {
        cmdRead.CommandText = "SELECT ID FROM TestCase";
        using (DbDataReader rd = cmdRead.ExecuteReader())
        {
          using (DbCommand cmdwrite = _cnn.CreateCommand())
          {
            cmdwrite.CommandText = "UPDATE [KeyInfoTest] SET [ID] = [ID]";
            cmdwrite.CommandTimeout = 5;

            int dwtick = Environment.TickCount;
            try
            {
              cmdwrite.ExecuteNonQuery();
            }
            catch (Exception)
            {
              dwtick = (Environment.TickCount - dwtick) / 1000;
              if (dwtick < 5 || dwtick > 6)
                throw new Exception("Timeout didn't wait long enough!");

              return;
            }
            throw new Exception("Operation should have failed but completed successfully");
          }
        }
      }
    }

    [Test(Sequence = 41)]
    internal void TransactionScopeTest()
    {
      using (TransactionScope scope = new TransactionScope())
      {
        using (DbConnection cnn2 = ((ICloneable)_cnn).Clone() as DbConnection)
        {
          if (cnn2.State != ConnectionState.Open) cnn2.Open();
          using (DbCommand cmd = cnn2.CreateCommand())
          {
            // Created a table inside the transaction scope
            cmd.CommandText = "CREATE TABLE VolatileTable (ID INTEGER PRIMARY KEY, MyValue VARCHAR(50))";
            cmd.ExecuteNonQuery();

            maydroptable.Add("VolatileTable");

            using (DbCommand cmd2 = cnn2.CreateCommand())
            {
              using (cmd2.Transaction = cnn2.BeginTransaction())
              {
                // Inserting a value inside the table, inside a transaction which is inside the transaction scope
                cmd2.CommandText = "INSERT INTO VolatileTable (ID, MyValue) VALUES(1, 'Hello')";
                cmd2.ExecuteNonQuery();
                cmd2.Transaction.Commit();
              }
            }
          }
          // Connection is disposed before the transactionscope leaves, thereby forcing the connection to stay open
        }
        // Exit the transactionscope without committing it, causing a rollback of both the create table and the insert
      }

      // Verify that the table does not exist
      using (DbCommand cmd = _cnn.CreateCommand())
      {
        cmd.CommandText = "SELECT COUNT(*) FROM VolatileTable";
        try
        {
          object o = cmd.ExecuteScalar();
          cmd.CommandText = "DROP TABLE VolatileTable";
          cmd.ExecuteNonQuery();
          throw new InvalidOperationException("Transaction failed! The table exists!");
        }
        catch(Exception e)
        {
          if (e is InvalidOperationException) throw new Exception(e.Message);
          return; // Succeeded, the table should not have existed
        }
      }
    }

    /// <summary>
    /// Causes the user-defined aggregate to be iterated through
    /// </summary>
    /// <returns></returns>
    [Test]
    internal string UserAggregate()
    {
      CheckSQLite();

      StringBuilder builder = new StringBuilder();
      using (DbCommand cmd = _cnn.CreateCommand())
      {
        int dtStart;
        int n = 0;
        int nCount;

        cmd.CommandText = "SELECT MyCount(*) FROM TestCase";

        nCount = 0;
        dtStart = Environment.TickCount;
        while (Environment.TickCount - dtStart < 1000)
        {
          n = Convert.ToInt32(cmd.ExecuteScalar());
          nCount++;
        }
        if (n != 120003) throw new Exception("Unexpected count");
        builder.Append(String.Format("UserAggregate executed {0} times in 1 second.", nCount));
      }
      return builder.ToString();
    }

    /// <summary>
    /// Causes the user-defined collation sequence to be iterated through
    /// </summary>
    [Test]
    internal void UserCollation()
    {
      CheckSQLite();

      using (DbCommand cmd = _cnn.CreateCommand())
      {
        // Using a default collating sequence in descending order, "Param Field3" will appear at the top
        // and "Field3" will be next, followed by a NULL.  Our user-defined collating sequence will 
        // deliberately place them out of order so Field3 is first.
        cmd.CommandText = "SELECT [Fiëld3] FROM TestCase ORDER BY [Fiëld3] COLLATE MYSEQUENCE DESC";
        string s = (string)cmd.ExecuteScalar();
        if (s != "Fiëld3") throw new Exception("MySequence didn't sort properly");
      }
    }

    /// <summary>
    /// Causes the user-defined function to be called
    /// </summary>
    /// <returns></returns>
    [Test]
    internal string UserFunction1()
    {
      CheckSQLite();
      using (DbCommand cmd = _cnn.CreateCommand())
      {
        int nTimes;
        int dtStart;

        nTimes = 0;
        cmd.CommandText = "SELECT Foo('ee','foo')";
        dtStart = Environment.TickCount;
        while (Environment.TickCount - dtStart < 1000)
        {
          cmd.ExecuteNonQuery();
          nTimes++;
        }
        return String.Format("User (text) command executed {0} times in 1 second.", nTimes);
      }
    }

    [Test]
    internal string UserFunction2()
    {
      CheckSQLite();
      using (DbCommand cmd = _cnn.CreateCommand())
      {
        int nTimes;
        int dtStart;

        nTimes = 0;
        cmd.CommandText = "SELECT Foo(10,11)";
        dtStart = Environment.TickCount;
        while (Environment.TickCount - dtStart < 1000)
        {
          cmd.ExecuteNonQuery();
          nTimes++;
        }
        return String.Format("UserFunction command executed {0} times in 1 second.", nTimes);
      }
    }

    [Test]
    internal string UserFunction3()
    {
      CheckSQLite();
      using (DbCommand cmd = _cnn.CreateCommand())
      {
        int nTimes;
        int dtStart;

        nTimes = 0;
        cmd.CommandText = "SELECT ABS(1)";
        dtStart = Environment.TickCount;
        while (Environment.TickCount - dtStart < 1000)
        {
          cmd.ExecuteNonQuery();
          nTimes++;
        }
        return String.Format("Intrinsic command executed {0} times in 1 second.", nTimes);
      }
    }

    [Test]
    internal string UserFunction4()
    {
      CheckSQLite();
      using (DbCommand cmd = _cnn.CreateCommand())
      {
        int nTimes;
        int dtStart;

        nTimes = 0;
        cmd.CommandText = "SELECT lower('FOO')";
        dtStart = Environment.TickCount;
        while (Environment.TickCount - dtStart < 1000)
        {
          cmd.ExecuteNonQuery();
          nTimes++;
        }
        return String.Format("Intrin (txt) command executed {0} times in 1 second.", nTimes);
      }
    }

    [Test]
    internal string UserFunction5()
    {
      CheckSQLite();
      using (DbCommand cmd = _cnn.CreateCommand())
      {
        int nTimes;
        int dtStart;

        nTimes = 0;
        cmd.CommandText = "SELECT 1";
        dtStart = Environment.TickCount;
        while (Environment.TickCount - dtStart < 1000)
        {
          cmd.ExecuteNonQuery();
          nTimes++;
        }
        return String.Format("Raw Value command executed {0} times in 1 second.", nTimes);
      }
    }
    
    [Test(Sequence = 42)]
    internal void VerifyBinaryData()
    {
      BinaryInsert();
      using (DbCommand cmd = _cnn.CreateCommand())
      {
        cmd.CommandText = "SELECT Field6 FROM TestCase WHERE Field6 IS NOT NULL";
        byte[] b = new byte[4000];

        using (DbDataReader rd = cmd.ExecuteReader())
        {
          if (rd.Read() == false) throw new Exception("No data to read!");

          long n = rd.GetBytes(0, 0, null, 0, 0);
          if (n != 4000) throw new Exception("Invalid byte length!");

          rd.GetBytes(0, 0, b, 0, 4000);

          if (b[0] != 1) throw new Exception("Binary value non-match byte 0");
          if (b[100] != 2) throw new Exception("Binary value non-match byte 100");
          if (b[1000] != 3) throw new Exception("Binary value non-match byte 1000");
          if (b[2000] != 4) throw new Exception("Binary value non-match byte 2000");
          if (b[3000] != 5) throw new Exception("Binary value non-match byte 3000");
        }
      }
    }

    [Test]
    internal void DecimalTest()
    {
      using (DbCommand cmd = _cnn.CreateCommand())
      {
        droptables.Add("DECTEST");

        cmd.CommandText = "CREATE TABLE DECTEST(x DECIMAL(38,18))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO DECTEST(x) VALUES(0.00001)";
        cmd.ExecuteNonQuery();
        
        cmd.CommandText = "SELECT * FROM DECTEST";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
          decimal d = (decimal)reader.GetValue(0);
          d = reader.GetDecimal(0);
        }
      }
    }

    [Test(Sequence = 98)]
    internal void ScalarPreTest()
    {
      using (DbCommand cmd = _cnn.CreateCommand())
      {
        droptables.Add("SCALARTEST");

        cmd.CommandText = "CREATE TABLE SCALARTEST(x INTEGER PRIMARY KEY, y)";
        cmd.ExecuteNonQuery();

        for (int i = 1; i <= 1000; i++)
        {
          DbParameter param1 = cmd.CreateParameter();

          param1.ParameterName = "param1";
          param1.DbType = DbType.Int32;
          param1.Value = i;

          DbParameter param2 = cmd.CreateParameter();

          param2.ParameterName = "param2";
          param2.DbType = DbType.Int32;
          param2.Value = i;

          cmd.CommandText =
              "INSERT OR REPLACE INTO SCALARTEST(x, y) VALUES(?, ?)";

          cmd.Parameters.Clear();
          cmd.Parameters.Add(param1);
          cmd.Parameters.Add(param2);

          cmd.ExecuteNonQuery();
        }
      }
    }

    [Test(Sequence = 99)]
    internal void ScalarTest()
    {
      using (DbCommand cmd = _cnn.CreateCommand())
      {
        cmd.CommandText = "SELECT x FROM SCALARTEST ORDER BY x";
        cmd.ExecuteScalar();
      }
    }

    [Test(Sequence = 30)]
    internal void VerifyInsert()
    {
      using (DbCommand cmd = _cnn.CreateCommand())
      {
        cmd.CommandText = "SELECT Field1, Field2, [Fiëld3], [Fiæld4], Field5 FROM TestCase";
        cmd.Prepare();
        using (DbDataReader rd = cmd.ExecuteReader())
        {
          if (rd.Read())
          {
            int Field1 = rd.GetInt32(0);
            double Field2 = rd.GetDouble(1);
            string Field3 = rd.GetString(2);
            string Field4 = rd.GetString(3).TrimEnd();
            DateTime Field5 = rd.GetDateTime(4);

            if (Field1 != 1) throw new Exception(String.Format("Field1 {0} did not match {1}", Field1, 1));
            if (Field2 != 3.14159) throw new Exception(String.Format("Field2 {0} did not match {1}", Field2, 3.14159));
            if (Field3 != "Fiëld3") throw new Exception(String.Format("Field3 {0} did not match {1}", Field3, "Fiëld3"));
            if (Field4 != "Fiæld4") throw new Exception(String.Format("Field4 {0} did not match {1}", Field4, "Fiæld4"));
            if (Field5.CompareTo(DateTime.Parse("2005-01-01 13:49:00")) != 0) throw new Exception(String.Format("Field5 {0} did not match {1}", Field4, DateTime.Parse("2005-01-01 13:49:00")));

            if (rd.GetName(0) != "Field1") throw new Exception("Non-Match column name Field1");
            if (rd.GetName(1) != "Field2") throw new Exception("Non-Match column name Field2");
            if (rd.GetName(2) != "Fiëld3") throw new Exception("Non-Match column name Field3");
            if (rd.GetName(3) != "Fiæld4") throw new Exception("Non-Match column name Field4");
            if (rd.GetName(4) != "Field5") throw new Exception("Non-Match column name Field5");
          }
          else throw new Exception("No data in table");
        }
      }
    }
  }

  /// <summary>
  /// Scalar user-defined function.  In this example, the same class is declared twice with 
  /// different function names to demonstrate how to use alias names for user-defined functions.
  /// </summary>
  [SQLiteFunction(Name = "Foo", Arguments = 2, FuncType = FunctionType.Scalar)]
  [SQLiteFunction(Name = "TestFunc", Arguments = 2, FuncType = FunctionType.Scalar)]
  class TestFunc : SQLiteFunction
  {
    public override object Invoke(object[] args)
    {
      if (args[0].GetType() != typeof(int)) return args[0];

      int Param1 = Convert.ToInt32(args[0]); // First parameter
      int Param2 = Convert.ToInt32(args[1]); // Second parameter

      return Param1 + Param2;
    }
  }

  [SQLiteFunction(Name = "CASETEST", Arguments = 2, FuncType = FunctionType.Scalar)]
  class CaseTestFunc : SQLiteFunctionEx
  {
    public override object Invoke(object[] args)
    {
      CollationSequence seq = GetCollationSequence();
      return seq.Compare(args[0].ToString(), args[1].ToString());
    }
  }

  /// <summary>
  /// Aggregate user-defined function.  Arguments = -1 means any number of arguments is acceptable
  /// </summary>
  [SQLiteFunction(Name = "MyCount", Arguments = -1, FuncType = FunctionType.Aggregate)]
  class MyCount : SQLiteFunction
  {
    public override void Step(object[] args, int nStep, ref object contextData)
    {
      if (contextData == null)
      {
        contextData = 1;
      }
      else
        contextData = (int)contextData + 1;
    }

    public override object Final(object contextData)
    {
      return contextData;
    }
  }

  /// <summary>
  /// Sample regular expression function.  Example Usage:
  /// SELECT * FROM foo WHERE name REGEXP '$bar'
  /// SELECT * FROM foo WHERE REGEXP('$bar', name)
  /// 
  /// </summary>
  [SQLiteFunction(Name = "REGEXP", Arguments = 2, FuncType = FunctionType.Scalar)]
  class MyRegEx : SQLiteFunction
  {
    public override object Invoke(object[] args)
    {
      return System.Text.RegularExpressions.Regex.IsMatch(Convert.ToString(args[1]), Convert.ToString(args[0]));
    }
  }

  /// <summary>
  /// User-defined collating sequence.
  /// </summary>
  [SQLiteFunction(Name = "MYSEQUENCE", FuncType = FunctionType.Collation)]
  class MySequence : SQLiteFunction
  {
    public override int Compare(string param1, string param2)
    {
      // Make sure the string "Fiëld3" is sorted out of order
      if (param1 == "Fiëld3") return 1;
      if (param2 == "Fiëld3") return -1;
      return String.Compare(param1, param2, true);
    }
  }

  [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
  public sealed class TestAttribute : Attribute, IComparable<TestAttribute>
  {
    private static int _start = 65535;
    private int _sequence;

    public TestAttribute()
    {
      _sequence = _start;
      _start++;
    }

    public int Sequence
    {
      get { return _sequence; }
      set { _sequence = value; }
    }

    #region IComparable<TestAttribute> Members

    public int CompareTo(TestAttribute other)
    {
      return _sequence.CompareTo(other._sequence);
    }
    #endregion
  }

  internal enum TestResultEnum
  {
    Succeeded = 0,
    Failed = 1,
    Inconclusive = 2,
  }

  internal class InconclusiveException : Exception
  {
    internal InconclusiveException()
      : base()
    {
    }

    internal InconclusiveException(string message)
      : base(message)
    {
    }
  }

  internal class TestEventArgs : EventArgs
  {
    public readonly string TestName;
    public readonly TestResultEnum Result;
    public readonly Exception Exception;
    public readonly string Message;
    public readonly int Duration;

    internal TestEventArgs(string testName, TestResultEnum success, int duration, Exception e, string message)
    {
      TestName = testName;
      Result = success;
      Exception = e;
      Message = message;
      Duration = duration;
    }
  }

  delegate void TestCompletedEvent(object sender, TestEventArgs args);
  delegate void TestStartingEvent(object sender, TestEventArgs args);

  internal abstract class TestCaseBase
  {
    protected DbProviderFactory _fact;
    protected DbConnection _cnn = null;
    protected DbConnectionStringBuilder _cnnstring;
    protected Dictionary<string, bool> _tests = new Dictionary<string,bool>();

    public event TestCompletedEvent OnTestFinished;
    public event TestStartingEvent OnTestStarting;
    public event EventHandler OnAllTestsDone;

    protected TestCaseBase()
    {
      SortedList<TestAttribute, System.Reflection.MethodInfo> items = new SortedList<TestAttribute, System.Reflection.MethodInfo>();
      foreach (System.Reflection.MethodInfo mi in GetType().GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.InvokeMethod))
      {
        object[] att = mi.GetCustomAttributes(typeof(TestAttribute), false);
        if (att.Length == 1)
        {
          items.Add((TestAttribute)att[0], mi);
        }
      }

      foreach (KeyValuePair<TestAttribute, System.Reflection.MethodInfo> pair in items)
      {
        _tests.Add(pair.Value.Name, true);
      }
    }

    protected TestCaseBase(DbProviderFactory factory, string connectionString)
    {
      _fact = factory;
      _cnn = _fact.CreateConnection();
      _cnn.ConnectionString = connectionString;
      _cnnstring = _fact.CreateConnectionStringBuilder();
      _cnnstring.ConnectionString = connectionString;
      _cnn.Open();
    }

    internal Dictionary<string, bool> Tests
    {
      get
      {
        return _tests;
      }
      set
      {
        _tests = value;
      }
    }

    internal void Run()
    {
      SortedList<TestAttribute, System.Reflection.MethodInfo> items = new SortedList<TestAttribute, System.Reflection.MethodInfo>();
      foreach (System.Reflection.MethodInfo mi in GetType().GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.InvokeMethod))
      {
        object[] att = mi.GetCustomAttributes(typeof(TestAttribute), false);
        if (att.Length == 1 && _tests[mi.Name] == true)
        {
          items.Add((TestAttribute)att[0], mi);
        }
      }

      foreach (KeyValuePair<TestAttribute, System.Reflection.MethodInfo> pair in items)
      {
        if (OnTestStarting != null)
          OnTestStarting(this, new TestEventArgs(pair.Value.Name, TestResultEnum.Inconclusive, 0, null, null));

        int start = Environment.TickCount;
        try
        {
          object obj = pair.Value.Invoke(this, null);
          int duration = Environment.TickCount - start;
          if (OnTestFinished != null)
            OnTestFinished(this, new TestEventArgs(pair.Value.Name, TestResultEnum.Succeeded, duration, null, (obj is string) ? (string)obj : String.Empty));
        }
        catch (Exception e)
        {
          int duration = Environment.TickCount - start;
          Exception inner = e.InnerException;

          if (OnTestFinished != null)
          {
            if (inner is InconclusiveException)
            {
              OnTestFinished(this, new TestEventArgs(pair.Value.Name, TestResultEnum.Inconclusive, duration, null, inner.Message));
            }
            else
            {
              OnTestFinished(this, new TestEventArgs(pair.Value.Name, TestResultEnum.Failed, duration, inner, null));
            }
          }
        }
      }

      if (OnAllTestsDone != null)
        OnAllTestsDone(this, EventArgs.Empty);
    }
  }
}
