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
  using System.Reflection;
  using System.Security.Permissions;

  /// <summary>
  /// SQLite implementation of DbProviderFactory.
  /// </summary>
  public sealed partial class SQLiteFactory : IServiceProvider
  {
    private static Type _dbProviderServicesType;
    private static object _sqliteServices;

    static SQLiteFactory()
    {
      _dbProviderServicesType = Type.GetType("System.Data.Common.DbProviderServices, System.Data.Entity, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", false);
    }

    /// <summary>
    /// Will provide a DbProviderServices object in .NET 3.5
    /// </summary>
    /// <param name="serviceType">The class or interface type to query for</param>
    /// <returns></returns>
    object IServiceProvider.GetService(Type serviceType)
    {
      if (serviceType == typeof(ISQLiteSchemaExtensions) ||
        (_dbProviderServicesType != null && serviceType == _dbProviderServicesType))
      {
        return GetSQLiteProviderServicesInstance();
      }
      return null;
    }

    [ReflectionPermission(SecurityAction.Assert, MemberAccess = true)]
    private object GetSQLiteProviderServicesInstance()
    {
      if (_sqliteServices == null)
      {
        Type type = Type.GetType("System.Data.SQLite.SQLiteProviderServices, System.Data.SQLite.Linq, Version=2.0.35.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139", false);
        if (type != null)
        {
          FieldInfo field = type.GetField("Instance", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
          _sqliteServices = field.GetValue(null);
        }
      }
      return _sqliteServices;
    }
  }
}
