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

  internal class SQLiteDataConnectionSupport : AdoDotNetConnectionSupport
  {
    public SQLiteDataConnectionSupport()
      : base("System.Data.SQLite")
    {
    }

    protected override DataSourceInformation CreateDataSourceInformation()
    {
      return base.CreateDataSourceInformation();
    }

    public override int CompareVersions(string versionA, string versionB)
    {
      return base.CompareVersions(versionA, versionB);
    }

    protected override object GetServiceImpl(Type serviceType)
    {
      if (serviceType == typeof(DataViewSupport))
        return new SQLiteDataViewSupport();

      if (serviceType == typeof(DataObjectSupport))
        return new SQLiteDataObjectSupport();

      return base.GetServiceImpl(serviceType);
    }

    public override void Initialize(object providerObj)
    {
      base.Initialize(providerObj);
    }

    public override object ProviderObject
    {
      get
      {
        return base.ProviderObject;
      }
    }
  }
}
