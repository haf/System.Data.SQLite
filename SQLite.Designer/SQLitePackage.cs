/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

namespace SQLite.Designer
{
  using System;
  using Microsoft.VisualStudio.Shell;
  using System.Runtime.InteropServices;
  using System.ComponentModel.Design;
  using Microsoft.Win32;
  using Microsoft.VisualStudio.Shell.Interop;


//  [ProvideLoadKey("Standard", "1.0", "SQLite Designer", "Black Castle Software, LLC", 1)]
  [Guid("DCBE6C8D-0E57-4099-A183-98FF74C64D9C")]
  internal sealed class SQLitePackage : Package
  {
    public SQLitePackage()
    {
    }

    protected override void Initialize()
    {
      ((IServiceContainer)this).AddService(typeof(SQLiteProviderObjectFactory), new ServiceCreatorCallback(CreateService), true);
      base.Initialize();
    }

    private object CreateService(IServiceContainer container, Type serviceType)
    {
      if (serviceType == typeof(SQLiteProviderObjectFactory))
        return new SQLiteProviderObjectFactory();

      return null;
    }
  }
}
