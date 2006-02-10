/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

namespace System.Data.SQLite
{
  using System;
  using System.Security;
  using System.Runtime.InteropServices;

#if !PLATFORM_COMPACTFRAMEWORK
  [SuppressUnmanagedCodeSecurity]
#endif
  internal sealed class UnsafeNativeMethods
  {
#if !USE_INTEROP_DLL
    private const string SQLITE_DLL = "System.Data.SQLite.DLL";
#else
    private const string SQLITE_DLL = "SQLite.Interop.DLL";
#endif

    private UnsafeNativeMethods()
    {
    }

    [DllImport(SQLITE_DLL)]
    internal static extern void sqlite3_sleep_interop(uint dwMilliseconds);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_libversion_interop(out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern void sqlite3_free_interop(IntPtr p);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_open_interop(byte[] utf8Filename, out int db);

    [DllImport(SQLITE_DLL)]
    internal static extern void sqlite3_interrupt_interop(int db);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_close_interop(int db);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_exec_interop(int db, byte[] strSql, int pvCallback, int pvParam, out IntPtr errMsg, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_errmsg_interop(int db, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_changes_interop(int db);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_busy_timeout_interop(int db, int ms);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_prepare_interop(int db, byte[] strSql, int nBytes, out int stmt, out IntPtr ptrRemain, out int nRemain);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_bind_blob_interop(int stmt, int index, Byte[] value, int nSize, int nTransient);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_bind_double_interop(int stmt, int index, ref double value);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_bind_int_interop(int stmt, int index, int value);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_bind_int64_interop(int stmt, int index, ref long value);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_bind_null_interop(int stmt, int index);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_bind_text_interop(int stmt, int index, byte[] value, int nlen, int pvReserved);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_bind_parameter_count_interop(int stmt);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_bind_parameter_name_interop(int stmt, int index, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_bind_parameter_index_interop(int stmt, byte[] strName);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_column_count_interop(int stmt);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_column_name_interop(int stmt, int index, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_column_decltype_interop(int stmt, int index, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_step_interop(int stmt);

    [DllImport(SQLITE_DLL)]
    internal static extern void sqlite3_column_double_interop(int stmt, int index, out double value);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_column_int_interop(int stmt, int index);

    [DllImport(SQLITE_DLL)]
    internal static extern void sqlite3_column_int64_interop(int stmt, int index, out long value);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_column_text_interop(int stmt, int index, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_column_blob_interop(int stmt, int index);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_column_bytes_interop(int stmt, int index);

    [DllImport(SQLITE_DLL)]
    internal static extern TypeAffinity sqlite3_column_type_interop(int stmt, int index);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_finalize_interop(int stmt);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_reset_interop(int stmt);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_create_collation_interop(int db, byte[] strName, int nType, int nArgs, SQLiteCollation func, out int nCookie);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_create_function_interop(int db, byte[] strName, int nArgs, int nType, SQLiteCallback func, SQLiteCallback fstep, SQLiteCallback ffinal, out int nCookie);

    [DllImport(SQLITE_DLL)]
    internal static extern void sqlite3_function_free_callbackcookie(int nCookie);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_aggregate_count_interop(int context);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_value_blob_interop(int p);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_value_bytes_interop(int p);

    [DllImport(SQLITE_DLL)]
    internal static extern void sqlite3_value_double_interop(int p, out double value);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_value_int_interop(int p);

    [DllImport(SQLITE_DLL)]
    internal static extern void sqlite3_value_int64_interop(int p, out Int64 value);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_value_text_interop(int p, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern TypeAffinity sqlite3_value_type_interop(int p);

    [DllImport(SQLITE_DLL)]
    internal static extern void sqlite3_result_blob_interop(int context, byte[] value, int nSize, int pvReserved);

    [DllImport(SQLITE_DLL)]
    internal static extern void sqlite3_result_double_interop(int context, ref double value);

    [DllImport(SQLITE_DLL)]
    internal static extern void sqlite3_result_error_interop(int context, byte[] strErr, int nLen);

    [DllImport(SQLITE_DLL)]
    internal static extern void sqlite3_result_int_interop(int context, int value);

    [DllImport(SQLITE_DLL)]
    internal static extern void sqlite3_result_int64_interop(int context, ref Int64 value);

    [DllImport(SQLITE_DLL)]
    internal static extern void sqlite3_result_null_interop(int context);

    [DllImport(SQLITE_DLL)]
    internal static extern void sqlite3_result_text_interop(int context, byte[] value, int nLen, int pvReserved);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_aggregate_context_interop(int context, int nBytes);

    [DllImport(SQLITE_DLL)]
    internal static extern void sqlite3_realcolnames(int db, int bset);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_table_column_metadata_interop(int db, byte[] dbName, byte[] tblName, byte[] colName, out IntPtr ptrDataType, out IntPtr ptrCollSeq, out int notNull, out int primaryKey, out int autoInc, out int dtLen, out int csLen);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_column_database_name_interop(int stmt, int index, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_column_database_name16_interop(int stmt, int index, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_column_table_name_interop(int stmt, int index, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_column_table_name16_interop(int stmt, int index, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_column_origin_name_interop(int stmt, int index, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_column_origin_name16_interop(int stmt, int index, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_column_text16_interop(int stmt, int index, out int len);

    [DllImport(SQLITE_DLL, CharSet = CharSet.Unicode)]
    internal static extern int sqlite3_open16_interop(string utf16Filename, out int db);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_errmsg16_interop(int db, out int len);

    [DllImport(SQLITE_DLL, CharSet = CharSet.Unicode)]
    internal static extern int sqlite3_prepare16_interop(int db, string strSql, int sqlLen, out int stmt, out IntPtr ptrRemain, out int len);

    [DllImport(SQLITE_DLL, CharSet = CharSet.Unicode)]
    internal static extern int sqlite3_bind_text16_interop(int stmt, int index, string value, int nlen, int nTransient);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_column_name16_interop(int stmt, int index, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_column_decltype16_interop(int stmt, int index, out int len);

    [DllImport(SQLITE_DLL, CharSet = CharSet.Unicode)]
    internal static extern int sqlite3_create_collation16_interop(int db, string strName, int nType, int nArgs, SQLiteCollation func, out int nCookie);

    [DllImport(SQLITE_DLL, CharSet = CharSet.Unicode)]
    internal static extern int sqlite3_create_function16_interop(int db, string strName, int nArgs, int nType, SQLiteCallback func, SQLiteCallback funcstep, SQLiteCallback funcfinal, out int nCookie);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_value_text16_interop(int p, out int len);

    [DllImport(SQLITE_DLL, CharSet = CharSet.Unicode)]
    internal static extern void sqlite3_result_error16_interop(int context, string strName, int nLen);

    [DllImport(SQLITE_DLL, CharSet = CharSet.Unicode)]
    internal static extern void sqlite3_result_text16_interop(int context, string strName, int nLen, int pvReserved);

    [DllImport(SQLITE_DLL, CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int sqlite3_encryptfile(string fileName);

    [DllImport(SQLITE_DLL, CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int sqlite3_decryptfile(string fileName);

    [DllImport(SQLITE_DLL, CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int sqlite3_encryptedstatus(string fileName, out int fileStatus);

    [DllImport(SQLITE_DLL, CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int sqlite3_compressfile(string fileName);

    [DllImport(SQLITE_DLL, CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int sqlite3_decompressfile(string fileName);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_key_interop(int db, byte[] key, int keylen);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_rekey_interop(int db, byte[] key, int keylen);
  }
}
