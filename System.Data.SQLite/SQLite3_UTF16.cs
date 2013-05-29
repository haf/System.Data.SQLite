/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

namespace System.Data.SQLite
{
  using System;

#if !NET_COMPACT_20 && TRACE_CONNECTION
  using System.Diagnostics;
#endif

  using System.IO;
  using System.Runtime.InteropServices;

  /// <summary>
  /// Alternate SQLite3 object, overriding many text behaviors to support UTF-16 (Unicode)
  /// </summary>
  internal sealed class SQLite3_UTF16 : SQLite3
  {
    internal SQLite3_UTF16(SQLiteDateFormats fmt, DateTimeKind kind)
      : base(fmt, kind)
    {
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    #region IDisposable "Pattern" Members
    private bool disposed;
    private void CheckDisposed() /* throw */
    {
#if THROW_ON_DISPOSED
        if (disposed)
            throw new ObjectDisposedException(typeof(SQLite3_UTF16).Name);
#endif
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    protected override void Dispose(bool disposing)
    {
        try
        {
            if (!disposed)
            {
                //if (disposing)
                //{
                //    ////////////////////////////////////
                //    // dispose managed resources here...
                //    ////////////////////////////////////
                //}

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        finally
        {
            base.Dispose(disposing);
        }
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Overrides SQLiteConvert.ToString() to marshal UTF-16 strings instead of UTF-8
    /// </summary>
    /// <param name="b">A pointer to a UTF-16 string</param>
    /// <param name="nbytelen">The length (IN BYTES) of the string</param>
    /// <returns>A .NET string</returns>
    public override string ToString(IntPtr b, int nbytelen)
    {
      CheckDisposed();
      return UTF16ToString(b, nbytelen);
    }

    public static string UTF16ToString(IntPtr b, int nbytelen)
    {
      if (nbytelen == 0 || b == IntPtr.Zero) return "";

      if (nbytelen == -1)
        return Marshal.PtrToStringUni(b);
      else
        return Marshal.PtrToStringUni(b, nbytelen / 2);
    }

    internal override void Open(string strFilename, SQLiteConnectionFlags connectionFlags, SQLiteOpenFlagsEnum openFlags, int maxPoolSize, bool usePool)
    {
      if (_sql != null) return;

      _usePool = usePool;
      _fileName = strFilename;

      if (usePool)
      {
        _sql = SQLiteConnectionPool.Remove(strFilename, maxPoolSize, out _poolVersion);

#if !NET_COMPACT_20 && TRACE_CONNECTION
        Trace.WriteLine(String.Format("Open16 (Pool): {0}", (_sql != null) ? _sql.ToString() : "<null>"));
#endif
      }

      if (_sql == null)
      {
        try
        {
            // do nothing.
        }
        finally /* NOTE: Thread.Abort() protection. */
        {
          IntPtr db;
          SQLiteErrorCode n;

#if !SQLITE_STANDARD
          if ((connectionFlags & SQLiteConnectionFlags.NoExtensionFunctions) != SQLiteConnectionFlags.NoExtensionFunctions)
          {
            n = UnsafeNativeMethods.sqlite3_open16_interop(ToUTF8(strFilename), openFlags, out db);
          }
          else
#endif
          {
            //
            // NOTE: This flag check is designed to enforce the constraint that opening
            //       a database file that does not already exist requires specifying the
            //       "Create" flag, even when a native API is used that does not accept
            //       a flags parameter.
            //
            if (((openFlags & SQLiteOpenFlagsEnum.Create) != SQLiteOpenFlagsEnum.Create) && !File.Exists(strFilename))
              throw new SQLiteException(SQLiteErrorCode.CantOpen, strFilename);

            n = UnsafeNativeMethods.sqlite3_open16(strFilename, out db);
          }

#if !NET_COMPACT_20 && TRACE_CONNECTION
          Trace.WriteLine(String.Format("Open16: {0}", db));
#endif

          if (n != SQLiteErrorCode.Ok) throw new SQLiteException(n, null);
          _sql = new SQLiteConnectionHandle(db);
        }
        lock (_sql) { /* HACK: Force the SyncBlock to be "created" now. */ }
      }
      _functionsArray = SQLiteFunction.BindFunctions(this, connectionFlags);
      SetTimeout(0);
      GC.KeepAlive(_sql);
    }

    internal override void Bind_DateTime(SQLiteStatement stmt, SQLiteConnectionFlags flags, int index, DateTime dt)
    {
        switch (_datetimeFormat)
        {
            case SQLiteDateFormats.Ticks:
            case SQLiteDateFormats.JulianDay:
            case SQLiteDateFormats.UnixEpoch:
                {
                    base.Bind_DateTime(stmt, flags, index, dt);
                    break;
                }
            default:
                {
#if !PLATFORM_COMPACTFRAMEWORK
                    if ((flags & SQLiteConnectionFlags.LogBind) == SQLiteConnectionFlags.LogBind)
                    {
                        SQLiteStatementHandle handle =
                            (stmt != null) ? stmt._sqlite_stmt : null;

                        LogBind(handle, index, dt);
                    }
#endif

                    Bind_Text(stmt, flags, index, ToString(dt));
                    break;
                }
        }
    }

    internal override void Bind_Text(SQLiteStatement stmt, SQLiteConnectionFlags flags, int index, string value)
    {
        SQLiteStatementHandle handle = stmt._sqlite_stmt;

#if !PLATFORM_COMPACTFRAMEWORK
        if ((flags & SQLiteConnectionFlags.LogBind) == SQLiteConnectionFlags.LogBind)
        {
            LogBind(handle, index, value);
        }
#endif

        SQLiteErrorCode n = UnsafeNativeMethods.sqlite3_bind_text16(handle, index, value, value.Length * 2, (IntPtr)(-1));
        if (n != SQLiteErrorCode.Ok) throw new SQLiteException(n, GetLastError());
    }

    internal override DateTime GetDateTime(SQLiteStatement stmt, int index)
    {
      return ToDateTime(GetText(stmt, index));
    }

    internal override string ColumnName(SQLiteStatement stmt, int index)
    {
#if !SQLITE_STANDARD
      int len;
      return UTF16ToString(UnsafeNativeMethods.sqlite3_column_name16_interop(stmt._sqlite_stmt, index, out len), len);
#else
      return UTF16ToString(UnsafeNativeMethods.sqlite3_column_name16(stmt._sqlite_stmt, index), -1);
#endif
    }

    internal override string GetText(SQLiteStatement stmt, int index)
    {
#if !SQLITE_STANDARD
      int len;
      return UTF16ToString(UnsafeNativeMethods.sqlite3_column_text16_interop(stmt._sqlite_stmt, index, out len), len);
#else
      return UTF16ToString(UnsafeNativeMethods.sqlite3_column_text16(stmt._sqlite_stmt, index),
        UnsafeNativeMethods.sqlite3_column_bytes16(stmt._sqlite_stmt, index));
#endif
    }

    internal override string ColumnOriginalName(SQLiteStatement stmt, int index)
    {
#if !SQLITE_STANDARD
      int len;
      return UTF16ToString(UnsafeNativeMethods.sqlite3_column_origin_name16_interop(stmt._sqlite_stmt, index, out len), len);
#else
      return UTF16ToString(UnsafeNativeMethods.sqlite3_column_origin_name16(stmt._sqlite_stmt, index), -1);
#endif
    }

    internal override string ColumnDatabaseName(SQLiteStatement stmt, int index)
    {
#if !SQLITE_STANDARD
      int len;
      return UTF16ToString(UnsafeNativeMethods.sqlite3_column_database_name16_interop(stmt._sqlite_stmt, index, out len), len);
#else
      return UTF16ToString(UnsafeNativeMethods.sqlite3_column_database_name16(stmt._sqlite_stmt, index), -1);
#endif
    }

    internal override string ColumnTableName(SQLiteStatement stmt, int index)
    {
#if !SQLITE_STANDARD
      int len;
      return UTF16ToString(UnsafeNativeMethods.sqlite3_column_table_name16_interop(stmt._sqlite_stmt, index, out len), len);
#else
      return UTF16ToString(UnsafeNativeMethods.sqlite3_column_table_name16(stmt._sqlite_stmt, index), -1);
#endif
    }

    internal override string GetParamValueText(IntPtr ptr)
    {
#if !SQLITE_STANDARD
      int len;
      return UTF16ToString(UnsafeNativeMethods.sqlite3_value_text16_interop(ptr, out len), len);
#else
      return UTF16ToString(UnsafeNativeMethods.sqlite3_value_text16(ptr),
        UnsafeNativeMethods.sqlite3_value_bytes16(ptr));
#endif
    }

    internal override void ReturnError(IntPtr context, string value)
    {
      UnsafeNativeMethods.sqlite3_result_error16(context, value, value.Length * 2);
    }

    internal override void ReturnText(IntPtr context, string value)
    {
      UnsafeNativeMethods.sqlite3_result_text16(context, value, value.Length * 2, (IntPtr)(-1));
    }
  }
}
