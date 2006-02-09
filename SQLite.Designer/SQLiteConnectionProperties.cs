/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

namespace SQLite.Designer
{
  using System;
  using Microsoft.VisualStudio.Data.AdoDotNet;
  using Microsoft.VisualStudio.Data;

  internal class SQLiteConnectionProperties : AdoDotNetConnectionProperties
  {
    public SQLiteConnectionProperties() : base("System.Data.SQLite")
    {
    }

    public SQLiteConnectionProperties(string connectionString) : base("System.Data.SQLite", connectionString)
    {
    }

    public override string[] GetBasicProperties()
    {
      return new string[] { "Data Source" };
    }

    public override bool  IsComplete
    {
      get 
      {
        return true;
        //if (!(this["Data Source"] is string) ||
        //  (this["Data Source"] as string).Length == 0)
        //{
        //  return false;
        //}

        //return true;
      }
    }

    public override bool EquivalentTo(DataConnectionProperties connectionProperties)
    {
      return base.EquivalentTo(connectionProperties);
    }
  }
}
