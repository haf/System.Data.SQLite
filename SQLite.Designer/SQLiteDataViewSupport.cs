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
  using System.Globalization;
  using Microsoft.VisualStudio.Data;
  using Microsoft.VisualStudio.OLE.Interop;
  using Microsoft.VisualStudio.Data.AdoDotNet;

  /// <summary>
  /// Provides DataViewSupport with a location where the XML file is for the Server Explorer's view.
  /// </summary>
  internal sealed class SQLiteDataViewSupport : DataViewSupport
  {
    public SQLiteDataViewSupport()
      : base(String.Format(CultureInfo.InvariantCulture, "SQLite.Designer.SQLiteDataViewSupport{0}", GetVSVersion()), typeof(SQLiteDataViewSupport).Assembly)
    {
    }

    private static string GetVSVersion()
    {
      switch (System.Diagnostics.FileVersionInfo.GetVersionInfo(Environment.GetCommandLineArgs()[0]).FileMajorPart)
      {
        case 8:
          return "2005";
        case 9:
          return "2008";
        case 10:
          return "2010";
        case 11:
          return "2012";
        default:
          return null;
      }
    }
  }
}
