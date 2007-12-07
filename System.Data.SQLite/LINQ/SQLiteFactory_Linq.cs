/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

namespace System.Data.SQLite
{
  using System;
  using System.Data.Common;

  /// <summary>
  /// SQLite implementation of DbProviderFactory.
  /// </summary>
  public sealed partial class SQLiteFactory : IServiceProvider
  {
    /// <summary>
    /// Will provide a DbProviderServices object in .NET 3.5
    /// </summary>
    /// <param name="serviceType">The class or interface type to query for</param>
    /// <returns></returns>
    public object GetService(Type serviceType)
    {
      return null;
    }
  }
}
