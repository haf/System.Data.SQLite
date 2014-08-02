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
  using Microsoft.Win32;

  /// <summary>
  /// Provides rudimentary connectionproperties support
  /// </summary>
  internal sealed class SQLiteConnectionProperties : AdoDotNetConnectionProperties
  {
    public SQLiteConnectionProperties()
      : this(null)
    {
    }

    public SQLiteConnectionProperties(string connectionString)
      : base(SQLiteOptions.GetProviderName(), connectionString)
    {
    }

    public override string[] GetBasicProperties()
    {
      return new string[] { "data source" };
    }

    protected override bool ShouldPersistProperty(string propertyName)
    {
      if (String.Compare(propertyName, "Database", StringComparison.OrdinalIgnoreCase) == 0) return false;

      return base.ShouldPersistProperty(propertyName);
    }

    public override bool Contains(string propertyName)
    {
      if (String.Compare(propertyName, "Database", StringComparison.OrdinalIgnoreCase) == 0)
        return (base.Contains("data source") || base.Contains("uri") || base.Contains("fulluri"));

      return base.Contains(propertyName);
    }

    public override object this[string propertyName]
    {
      get
      {
        if (String.Compare(propertyName, "Database", StringComparison.OrdinalIgnoreCase) == 0)
          return System.IO.Path.GetFileNameWithoutExtension(GetDatabaseFile());

        return base[propertyName];
      }
      set
      {
        base[propertyName] = value;
      }
    }

    internal string GetDatabaseFile()
    {
      if (this["data source"] is string && ((string)this["data source"]).Length > 0)
        return (string)this["data source"];
      else if (this["uri"] is string)
        return MapUriPath((string)this["uri"]);
      else if (this["fulluri"] is string)
        return (string)this["fulluri"];
      return String.Empty;
    }

    public override bool  IsComplete
    {
      get 
      {
        if (Contains("data source") == true)
        {
          if (this["data source"] is string && ((string)this["data source"]).Length > 0)
            return true;
        }
        else if (Contains("uri") == true)
        {
          if (this["uri"] is string && MapUriPath((string)this["uri"]).Length > 0)
            return true;
        }
        else if (Contains("fulluri") == true)
        {
          if (this["fulluri"] is string && ((string)this["fulluri"]).Length > 0)
            return true;
        }

        return false;
      }
    }

    internal static string MapUriPath(string path)
    {
      if (path.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        return path.Substring(7);
      else if (path.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        return path.Substring(5);
      else if (path.StartsWith("/", StringComparison.OrdinalIgnoreCase))
        return path;
      else
        return String.Empty;
    }
  }
}
