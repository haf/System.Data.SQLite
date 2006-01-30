/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

namespace SQLite.Designer
{
  using System;
  using System.Collections.Generic;
  using System.Text;
  using Microsoft.VisualStudio.Data;
  using Microsoft.VisualStudio.OLE.Interop;
  using Microsoft.VisualStudio.Data.AdoDotNet;

  internal class SQLiteDataObjectSupport : DataObjectSupport
  {
    public SQLiteDataObjectSupport()
      : base("SQLite.Designer.SQLiteDataObjectSupport", typeof(SQLiteDataObjectSupport).Assembly)
    {
    }
  }
}
