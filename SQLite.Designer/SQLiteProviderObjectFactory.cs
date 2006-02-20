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
  using System.Runtime.InteropServices;
  using Microsoft.Data.ConnectionUI;

  [Guid("DCBE6C8D-0E57-4099-A183-98FF74C64D9D")]
  internal sealed class SQLiteProviderObjectFactory : AdoDotNetProviderObjectFactory
  {
    public SQLiteProviderObjectFactory()
    {
    }

    public override object CreateObject(Type objType)
    {
      if (objType == typeof(DataConnectionSupport))
        return new SQLiteDataConnectionSupport();

      if (objType == typeof(IDataConnectionProperties))
        return new SQLiteConnectionProperties();

      if (objType == typeof(IDataConnectionUIControl))
        return new SQLiteConnectionUIControl();

      return base.CreateObject(objType);
    }
  }
}
