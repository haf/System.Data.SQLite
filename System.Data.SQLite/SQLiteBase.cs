/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 *
 * Released to the public domain, use at your own risk!
 ********************************************************/

namespace System.Data.SQLite
{
  using System;

#if !PLATFORM_COMPACTFRAMEWORK
  using System.Runtime.InteropServices;
#endif

  /// <summary>
  /// This internal class provides the foundation of SQLite support.  It defines all the abstract members needed to implement
  /// a SQLite data provider, and inherits from SQLiteConvert which allows for simple translations of string to and from SQLite.
  /// </summary>
  internal abstract class SQLiteBase : SQLiteConvert, IDisposable
  {
    #region Private Constants
    /// <summary>
    /// The error code used for logging exceptions caught in user-provided
    /// code.
    /// </summary>
    internal const int COR_E_EXCEPTION = unchecked((int)0x80131500);
    #endregion

    /////////////////////////////////////////////////////////////////////////

    internal SQLiteBase(SQLiteDateFormats fmt, DateTimeKind kind, string fmtString)
      : base(fmt, kind, fmtString) { }

    /// <summary>
    /// Returns a string representing the active version of SQLite
    /// </summary>
    internal abstract string Version { get; }
    /// <summary>
    /// Returns an integer representing the active version of SQLite
    /// </summary>
    internal abstract int VersionNumber { get; }
    /// <summary>
    /// Returns the rowid of the most recent successful INSERT into the database from this connection.
    /// </summary>
    internal abstract long LastInsertRowId { get; }
    /// <summary>
    /// Returns the number of changes the last executing insert/update caused.
    /// </summary>
    internal abstract int Changes { get; }
    /// <summary>
    /// Returns the amount of memory (in bytes) currently in use by the SQLite core library.  This is not really a per-connection
    /// value, it is global to the process.
    /// </summary>
    internal abstract long MemoryUsed { get; }
    /// <summary>
    /// Returns the maximum amount of memory (in bytes) used by the SQLite core library since the high-water mark was last reset.
    /// This is not really a per-connection value, it is global to the process.
    /// </summary>
    internal abstract long MemoryHighwater { get; }
    /// <summary>
    /// Returns non-zero if the underlying native connection handle is owned by this instance.
    /// </summary>
    internal abstract bool OwnHandle { get; }
    /// <summary>
    /// Sets the status of the memory usage tracking subsystem in the SQLite core library.  By default, this is enabled.
    /// If this is disabled, memory usage tracking will not be performed.  This is not really a per-connection value, it is
    /// global to the process.
    /// </summary>
    /// <param name="value">Non-zero to enable memory usage tracking, zero otherwise.</param>
    /// <returns>A standard SQLite return code (i.e. zero for success and non-zero for failure).</returns>
    internal abstract SQLiteErrorCode SetMemoryStatus(bool value);
    /// <summary>
    /// Attempts to free as much heap memory as possible for the database connection.
    /// </summary>
    /// <returns>A standard SQLite return code (i.e. zero for success and non-zero for failure).</returns>
    internal abstract SQLiteErrorCode ReleaseMemory();
    /// <summary>
    /// Shutdown the SQLite engine so that it can be restarted with different config options.
    /// We depend on auto initialization to recover.
    /// </summary>
    internal abstract SQLiteErrorCode Shutdown();
    /// <summary>
    /// Determines if the associated native connection handle is open.
    /// </summary>
    /// <returns>
    /// Non-zero if a database connection is open.
    /// </returns>
    internal abstract bool IsOpen();
    /// <summary>
    /// Opens a database.
    /// </summary>
    /// <remarks>
    /// Implementers should call SQLiteFunction.BindFunctions() and save the array after opening a connection
    /// to bind all attributed user-defined functions and collating sequences to the new connection.
    /// </remarks>
    /// <param name="strFilename">The filename of the database to open.  SQLite automatically creates it if it doesn't exist.</param>
    /// <param name="connectionFlags">The flags associated with the parent connection object</param>
    /// <param name="openFlags">The open flags to use when creating the connection</param>
    /// <param name="maxPoolSize">The maximum size of the pool for the given filename</param>
    /// <param name="usePool">If true, the connection can be pulled from the connection pool</param>
    internal abstract void Open(string strFilename, SQLiteConnectionFlags connectionFlags, SQLiteOpenFlagsEnum openFlags, int maxPoolSize, bool usePool);
    /// <summary>
    /// Closes the currently-open database.
    /// </summary>
    /// <remarks>
    /// After the database has been closed implemeters should call SQLiteFunction.UnbindFunctions() to deallocate all interop allocated
    /// memory associated with the user-defined functions and collating sequences tied to the closed connection.
    /// </remarks>
    /// <param name="canThrow">Non-zero if the operation is allowed to throw exceptions, zero otherwise.</param>
    internal abstract void Close(bool canThrow);
    /// <summary>
    /// Sets the busy timeout on the connection.  SQLiteCommand will call this before executing any command.
    /// </summary>
    /// <param name="nTimeoutMS">The number of milliseconds to wait before returning SQLITE_BUSY</param>
    internal abstract void SetTimeout(int nTimeoutMS);
    /// <summary>
    /// Returns the text of the last error issued by SQLite
    /// </summary>
    /// <returns></returns>
    internal abstract string GetLastError();

    /// <summary>
    /// Returns the text of the last error issued by SQLite -OR- the specified default error text if
    /// none is available from the SQLite core library.
    /// </summary>
    /// <param name="defValue">
    /// The error text to return in the event that one is not available from the SQLite core library.
    /// </param>
    /// <returns>
    /// The error text.
    /// </returns>
    internal abstract string GetLastError(string defValue);

    /// <summary>
    /// When pooling is enabled, force this connection to be disposed rather than returned to the pool
    /// </summary>
    internal abstract void ClearPool();

    /// <summary>
    /// When pooling is enabled, returns the number of pool entries matching the current file name.
    /// </summary>
    /// <returns>The number of pool entries matching the current file name.</returns>
    internal abstract int CountPool();

    /// <summary>
    /// Prepares a SQL statement for execution.
    /// </summary>
    /// <param name="cnn">The source connection preparing the command.  Can be null for any caller except LINQ</param>
    /// <param name="strSql">The SQL command text to prepare</param>
    /// <param name="previous">The previous statement in a multi-statement command, or null if no previous statement exists</param>
    /// <param name="timeoutMS">The timeout to wait before aborting the prepare</param>
    /// <param name="strRemain">The remainder of the statement that was not processed.  Each call to prepare parses the
    /// SQL up to to either the end of the text or to the first semi-colon delimiter.  The remaining text is returned
    /// here for a subsequent call to Prepare() until all the text has been processed.</param>
    /// <returns>Returns an initialized SQLiteStatement.</returns>
    internal abstract SQLiteStatement Prepare(SQLiteConnection cnn, string strSql, SQLiteStatement previous, uint timeoutMS, ref string strRemain);
    /// <summary>
    /// Steps through a prepared statement.
    /// </summary>
    /// <param name="stmt">The SQLiteStatement to step through</param>
    /// <returns>True if a row was returned, False if not.</returns>
    internal abstract bool Step(SQLiteStatement stmt);
    /// <summary>
    /// Resets a prepared statement so it can be executed again.  If the error returned is SQLITE_SCHEMA,
    /// transparently attempt to rebuild the SQL statement and throw an error if that was not possible.
    /// </summary>
    /// <param name="stmt">The statement to reset</param>
    /// <returns>Returns -1 if the schema changed while resetting, 0 if the reset was sucessful or 6 (SQLITE_LOCKED) if the reset failed due to a lock</returns>
    internal abstract SQLiteErrorCode Reset(SQLiteStatement stmt);

    /// <summary>
    /// Attempts to interrupt the query currently executing on the associated
    /// native database connection.
    /// </summary>
    internal abstract void Cancel();

    /// <summary>
    /// This function binds a user-defined functions to the connection.
    /// </summary>
    /// <param name="functionAttribute">
    /// The <see cref="SQLiteFunctionAttribute"/> object instance containing
    /// the metadata for the function to be bound.
    /// </param>
    /// <param name="function">
    /// The <see cref="SQLiteFunction"/> object instance that implements the
    /// function to be bound.
    /// </param>
    /// <param name="flags">
    /// The flags associated with the parent connection object.
    /// </param>
    internal abstract void BindFunction(SQLiteFunctionAttribute functionAttribute, SQLiteFunction function, SQLiteConnectionFlags flags);

    internal abstract void Bind_Double(SQLiteStatement stmt, SQLiteConnectionFlags flags, int index, double value);
    internal abstract void Bind_Int32(SQLiteStatement stmt, SQLiteConnectionFlags flags, int index, Int32 value);
    internal abstract void Bind_UInt32(SQLiteStatement stmt, SQLiteConnectionFlags flags, int index, UInt32 value);
    internal abstract void Bind_Int64(SQLiteStatement stmt, SQLiteConnectionFlags flags, int index, Int64 value);
    internal abstract void Bind_UInt64(SQLiteStatement stmt, SQLiteConnectionFlags flags, int index, UInt64 value);
    internal abstract void Bind_Text(SQLiteStatement stmt, SQLiteConnectionFlags flags, int index, string value);
    internal abstract void Bind_Blob(SQLiteStatement stmt, SQLiteConnectionFlags flags, int index, byte[] blobData);
    internal abstract void Bind_DateTime(SQLiteStatement stmt, SQLiteConnectionFlags flags, int index, DateTime dt);
    internal abstract void Bind_Null(SQLiteStatement stmt, SQLiteConnectionFlags flags, int index);

    internal abstract int Bind_ParamCount(SQLiteStatement stmt, SQLiteConnectionFlags flags);
    internal abstract string Bind_ParamName(SQLiteStatement stmt, SQLiteConnectionFlags flags, int index);
    internal abstract int Bind_ParamIndex(SQLiteStatement stmt, SQLiteConnectionFlags flags, string paramName);

    internal abstract int ColumnCount(SQLiteStatement stmt);
    internal abstract string ColumnName(SQLiteStatement stmt, int index);
    internal abstract TypeAffinity ColumnAffinity(SQLiteStatement stmt, int index);
    internal abstract string ColumnType(SQLiteStatement stmt, int index, ref TypeAffinity nAffinity);
    internal abstract int ColumnIndex(SQLiteStatement stmt, string columnName);
    internal abstract string ColumnOriginalName(SQLiteStatement stmt, int index);
    internal abstract string ColumnDatabaseName(SQLiteStatement stmt, int index);
    internal abstract string ColumnTableName(SQLiteStatement stmt, int index);
    internal abstract void ColumnMetaData(string dataBase, string table, string column, ref string dataType, ref string collateSequence, ref bool notNull, ref bool primaryKey, ref bool autoIncrement);
    internal abstract void GetIndexColumnExtendedInfo(string database, string index, string column, ref int sortMode, ref int onError, ref string collationSequence);

    internal abstract double GetDouble(SQLiteStatement stmt, int index);
    internal abstract SByte GetSByte(SQLiteStatement stmt, int index);
    internal abstract Byte GetByte(SQLiteStatement stmt, int index);
    internal abstract Int16 GetInt16(SQLiteStatement stmt, int index);
    internal abstract UInt16 GetUInt16(SQLiteStatement stmt, int index);
    internal abstract Int32 GetInt32(SQLiteStatement stmt, int index);
    internal abstract UInt32 GetUInt32(SQLiteStatement stmt, int index);
    internal abstract Int64 GetInt64(SQLiteStatement stmt, int index);
    internal abstract UInt64 GetUInt64(SQLiteStatement stmt, int index);
    internal abstract string GetText(SQLiteStatement stmt, int index);
    internal abstract long GetBytes(SQLiteStatement stmt, int index, int nDataoffset, byte[] bDest, int nStart, int nLength);
    internal abstract long GetChars(SQLiteStatement stmt, int index, int nDataoffset, char[] bDest, int nStart, int nLength);
    internal abstract DateTime GetDateTime(SQLiteStatement stmt, int index);
    internal abstract bool IsNull(SQLiteStatement stmt, int index);

    internal abstract void CreateCollation(string strCollation, SQLiteCollation func, SQLiteCollation func16);
    internal abstract void CreateFunction(string strFunction, int nArgs, bool needCollSeq, SQLiteCallback func, SQLiteCallback funcstep, SQLiteFinalCallback funcfinal);
    internal abstract CollationSequence GetCollationSequence(SQLiteFunction func, IntPtr context);
    internal abstract int ContextCollateCompare(CollationEncodingEnum enc, IntPtr context, string s1, string s2);
    internal abstract int ContextCollateCompare(CollationEncodingEnum enc, IntPtr context, char[] c1, char[] c2);

    internal abstract int AggregateCount(IntPtr context);
    internal abstract IntPtr AggregateContext(IntPtr context);

    internal abstract long GetParamValueBytes(IntPtr ptr, int nDataOffset, byte[] bDest, int nStart, int nLength);
    internal abstract double GetParamValueDouble(IntPtr ptr);
    internal abstract int GetParamValueInt32(IntPtr ptr);
    internal abstract Int64 GetParamValueInt64(IntPtr ptr);
    internal abstract string GetParamValueText(IntPtr ptr);
    internal abstract TypeAffinity GetParamValueType(IntPtr ptr);

    internal abstract void ReturnBlob(IntPtr context, byte[] value);
    internal abstract void ReturnDouble(IntPtr context, double value);
    internal abstract void ReturnError(IntPtr context, string value);
    internal abstract void ReturnInt32(IntPtr context, Int32 value);
    internal abstract void ReturnInt64(IntPtr context, Int64 value);
    internal abstract void ReturnNull(IntPtr context);
    internal abstract void ReturnText(IntPtr context, string value);

#if INTEROP_VIRTUAL_TABLE
    /// <summary>
    /// Calls the native SQLite core library in order to create a disposable
    /// module containing the implementation of a virtual table.
    /// </summary>
    /// <param name="module">
    /// The module object to be used when creating the native disposable module.
    /// </param>
    /// <param name="flags">
    /// The flags for the associated <see cref="SQLiteConnection" /> object instance.
    /// </param>
    internal abstract void CreateModule(SQLiteModule module, SQLiteConnectionFlags flags);

    /// <summary>
    /// Calls the native SQLite core library in order to cleanup the resources
    /// associated with a module containing the implementation of a virtual table.
    /// </summary>
    /// <param name="module">
    /// The module object previously passed to the <see cref="CreateModule" />
    /// method.
    /// </param>
    /// <param name="flags">
    /// The flags for the associated <see cref="SQLiteConnection" /> object instance.
    /// </param>
    internal abstract void DisposeModule(SQLiteModule module, SQLiteConnectionFlags flags);

    /// <summary>
    /// Calls the native SQLite core library in order to declare a virtual table
    /// in response to a call into the <see cref="ISQLiteNativeModule.xCreate" />
    /// or <see cref="ISQLiteNativeModule.xConnect" /> virtual table methods.
    /// </summary>
    /// <param name="module">
    /// The virtual table module that is to be responsible for the virtual table
    /// being declared.
    /// </param>
    /// <param name="strSql">
    /// The string containing the SQL statement describing the virtual table to
    /// be declared.
    /// </param>
    /// <param name="error">
    /// Upon success, the contents of this parameter are undefined.  Upon failure,
    /// it should contain an appropriate error message.
    /// </param>
    /// <returns>
    /// A standard SQLite return code.
    /// </returns>
    internal abstract SQLiteErrorCode DeclareVirtualTable(SQLiteModule module, string strSql, ref string error);

    /// <summary>
    /// Calls the native SQLite core library in order to declare a virtual table
    /// function in response to a call into the <see cref="ISQLiteNativeModule.xCreate" />
    /// or <see cref="ISQLiteNativeModule.xConnect" /> virtual table methods.
    /// </summary>
    /// <param name="module">
    /// The virtual table module that is to be responsible for the virtual table
    /// function being declared.
    /// </param>
    /// <param name="argumentCount">
    /// The number of arguments to the function being declared.
    /// </param>
    /// <param name="name">
    /// The name of the function being declared.
    /// </param>
    /// <param name="error">
    /// Upon success, the contents of this parameter are undefined.  Upon failure,
    /// it should contain an appropriate error message.
    /// </param>
    /// <returns>
    /// A standard SQLite return code.
    /// </returns>
    internal abstract SQLiteErrorCode DeclareVirtualFunction(SQLiteModule module, int argumentCount, string name, ref string error);
#endif

    /// <summary>
    /// Enables or disabled extension loading by SQLite.
    /// </summary>
    /// <param name="bOnOff">
    /// True to enable loading of extensions, false to disable.
    /// </param>
    internal abstract void SetLoadExtension(bool bOnOff);
    /// <summary>
    /// Loads a SQLite extension library from the named file.
    /// </summary>
    /// <param name="fileName">
    /// The name of the dynamic link library file containing the extension.
    /// </param>
    /// <param name="procName">
    /// The name of the exported function used to initialize the extension.
    /// If null, the default "sqlite3_extension_init" will be used.
    /// </param>
    internal abstract void LoadExtension(string fileName, string procName);
    /// <summary>
    /// Enables or disabled extened result codes returned by SQLite
    /// </summary>
    /// <param name="bOnOff">true to enable extended result codes, false to disable.</param>
    /// <returns></returns>
    internal abstract void SetExtendedResultCodes(bool bOnOff);
    /// <summary>
    /// Returns the numeric result code for the most recent failed SQLite API call
    /// associated with the database connection.
    /// </summary>
    /// <returns>Result code</returns>
    internal abstract SQLiteErrorCode ResultCode();
    /// <summary>
    /// Returns the extended numeric result code for the most recent failed SQLite API call
    /// associated with the database connection.
    /// </summary>
    /// <returns>Extended result code</returns>
    internal abstract SQLiteErrorCode ExtendedResultCode();

    /// <summary>
    /// Add a log message via the SQLite sqlite3_log interface.
    /// </summary>
    /// <param name="iErrCode">Error code to be logged with the message.</param>
    /// <param name="zMessage">String to be logged.  Unlike the SQLite sqlite3_log()
    /// interface, this should be pre-formatted.  Consider using the
    /// String.Format() function.</param>
    /// <returns></returns>
    internal abstract void LogMessage(SQLiteErrorCode iErrCode, string zMessage);

#if INTEROP_CODEC
    internal abstract void SetPassword(byte[] passwordBytes);
    internal abstract void ChangePassword(byte[] newPasswordBytes);
#endif

    internal abstract void SetAuthorizerHook(SQLiteAuthorizerCallback func);
    internal abstract void SetUpdateHook(SQLiteUpdateCallback func);
    internal abstract void SetCommitHook(SQLiteCommitCallback func);
    internal abstract void SetTraceCallback(SQLiteTraceCallback func);
    internal abstract void SetRollbackHook(SQLiteRollbackCallback func);
    internal abstract SQLiteErrorCode SetLogCallback(SQLiteLogCallback func);

    /// <summary>
    /// Checks if the SQLite core library has been initialized in the current process.
    /// </summary>
    /// <returns>
    /// Non-zero if the SQLite core library has been initialized in the current process,
    /// zero otherwise.
    /// </returns>
    internal abstract bool IsInitialized();

    internal abstract int GetCursorForTable(SQLiteStatement stmt, int database, int rootPage);
    internal abstract long GetRowIdForCursor(SQLiteStatement stmt, int cursor);

    internal abstract object GetValue(SQLiteStatement stmt, SQLiteConnectionFlags flags, int index, SQLiteType typ);

    /// <summary>
    /// Returns non-zero if the given database connection is in autocommit mode.
    /// Autocommit mode is on by default.  Autocommit mode is disabled by a BEGIN
    /// statement.  Autocommit mode is re-enabled by a COMMIT or ROLLBACK.
    /// </summary>
    internal abstract bool AutoCommit
    {
      get;
    }

    internal abstract SQLiteErrorCode FileControl(string zDbName, int op, IntPtr pArg);

    /// <summary>
    /// Creates a new SQLite backup object based on the provided destination
    /// database connection.  The source database connection is the one
    /// associated with this object.  The source and destination database
    /// connections cannot be the same.
    /// </summary>
    /// <param name="destCnn">The destination database connection.</param>
    /// <param name="destName">The destination database name.</param>
    /// <param name="sourceName">The source database name.</param>
    /// <returns>The newly created backup object.</returns>
    internal abstract SQLiteBackup InitializeBackup(
        SQLiteConnection destCnn, string destName,
        string sourceName);

    /// <summary>
    /// Copies up to N pages from the source database to the destination
    /// database associated with the specified backup object.
    /// </summary>
    /// <param name="backup">The backup object to use.</param>
    /// <param name="nPage">
    /// The number of pages to copy or negative to copy all remaining pages.
    /// </param>
    /// <param name="retry">
    /// Set to true if the operation needs to be retried due to database
    /// locking issues.
    /// </param>
    /// <returns>
    /// True if there are more pages to be copied, false otherwise.
    /// </returns>
    internal abstract bool StepBackup(SQLiteBackup backup, int nPage, ref bool retry);

    /// <summary>
    /// Returns the number of pages remaining to be copied from the source
    /// database to the destination database associated with the specified
    /// backup object.
    /// </summary>
    /// <param name="backup">The backup object to check.</param>
    /// <returns>The number of pages remaining to be copied.</returns>
    internal abstract int RemainingBackup(SQLiteBackup backup);

    /// <summary>
    /// Returns the total number of pages in the source database associated
    /// with the specified backup object.
    /// </summary>
    /// <param name="backup">The backup object to check.</param>
    /// <returns>The total number of pages in the source database.</returns>
    internal abstract int PageCountBackup(SQLiteBackup backup);

    /// <summary>
    /// Destroys the backup object, rolling back any backup that may be in
    /// progess.
    /// </summary>
    /// <param name="backup">The backup object to destroy.</param>
    internal abstract void FinishBackup(SQLiteBackup backup);

    ///////////////////////////////////////////////////////////////////////////////////////////////

    #region IDisposable Members
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////

    #region IDisposable "Pattern" Members
    private bool disposed;
    private void CheckDisposed() /* throw */
    {
#if THROW_ON_DISPOSED
        if (disposed)
            throw new ObjectDisposedException(typeof(SQLiteBase).Name);
#endif
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual void Dispose(bool disposing)
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
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////

    #region Destructor
    ~SQLiteBase()
    {
        Dispose(false);
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////

    // These statics are here for lack of a better place to put them.
    // They exist here because they are called during the finalization of
    // a SQLiteStatementHandle, SQLiteConnectionHandle, and SQLiteFunctionCookieHandle.
    // Therefore these functions have to be static, and have to be low-level.

    ///////////////////////////////////////////////////////////////////////////////////////////////

    private static string[] _errorMessages = {
        /* SQLITE_OK          */ "not an error",
        /* SQLITE_ERROR       */ "SQL logic error or missing database",
        /* SQLITE_INTERNAL    */ "internal logic error",
        /* SQLITE_PERM        */ "access permission denied",
        /* SQLITE_ABORT       */ "callback requested query abort",
        /* SQLITE_BUSY        */ "database is locked",
        /* SQLITE_LOCKED      */ "database table is locked",
        /* SQLITE_NOMEM       */ "out of memory",
        /* SQLITE_READONLY    */ "attempt to write a readonly database",
        /* SQLITE_INTERRUPT   */ "interrupted",
        /* SQLITE_IOERR       */ "disk I/O error",
        /* SQLITE_CORRUPT     */ "database disk image is malformed",
        /* SQLITE_NOTFOUND    */ "unknown operation",
        /* SQLITE_FULL        */ "database or disk is full",
        /* SQLITE_CANTOPEN    */ "unable to open database file",
        /* SQLITE_PROTOCOL    */ "locking protocol",
        /* SQLITE_EMPTY       */ "table contains no data",
        /* SQLITE_SCHEMA      */ "database schema has changed",
        /* SQLITE_TOOBIG      */ "string or blob too big",
        /* SQLITE_CONSTRAINT  */ "constraint failed",
        /* SQLITE_MISMATCH    */ "datatype mismatch",
        /* SQLITE_MISUSE      */ "library routine called out of sequence",
        /* SQLITE_NOLFS       */ "large file support is disabled",
        /* SQLITE_AUTH        */ "authorization denied",
        /* SQLITE_FORMAT      */ "auxiliary database format error",
        /* SQLITE_RANGE       */ "bind or column index out of range",
        /* SQLITE_NOTADB      */ "file is encrypted or is not a database",
        /* SQLITE_NOTICE      */ "notification message",
        /* SQLITE_WARNING     */ "warning message"
    };

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Returns the error message for the specified SQLite return code using
    /// the internal static lookup table.
    /// </summary>
    /// <param name="rc">The SQLite return code.</param>
    /// <returns>The error message or null if it cannot be found.</returns>
    private static string FallbackGetErrorString(SQLiteErrorCode rc)
    {
        if (_errorMessages == null)
            return null;

        int index = (int)rc;

        if ((index < 0) || (index >= _errorMessages.Length))
            index = (int)SQLiteErrorCode.Error; /* Make into generic error. */

        return _errorMessages[index];
    }

    /// <summary>
    /// Returns the error message for the specified SQLite return code using
    /// the sqlite3_errstr() function, falling back to the internal lookup
    /// table if necessary.
    /// </summary>
    /// <param name="rc">The SQLite return code.</param>
    /// <returns>The error message or null if it cannot be found.</returns>
    internal static string GetErrorString(SQLiteErrorCode rc)
    {
        try
        {
            IntPtr ptr = UnsafeNativeMethods.sqlite3_errstr(rc);

            if (ptr != IntPtr.Zero)
            {
#if !PLATFORM_COMPACTFRAMEWORK
                return Marshal.PtrToStringAnsi(ptr);
#else
                return UTF8ToString(ptr, -1);
#endif
            }
        }
        catch (EntryPointNotFoundException)
        {
            // do nothing.
        }

        return FallbackGetErrorString(rc);
    }

    internal static string GetLastError(SQLiteConnectionHandle hdl, IntPtr db)
    {
        if ((hdl == null) || (db == IntPtr.Zero))
            return "null connection or database handle";

        string result = null;

        try
        {
            // do nothing.
        }
        finally /* NOTE: Thread.Abort() protection. */
        {
#if PLATFORM_COMPACTFRAMEWORK
            lock (hdl.syncRoot)
#else
            lock (hdl)
#endif
            {
                if (!hdl.IsInvalid && !hdl.IsClosed)
                {
#if !SQLITE_STANDARD
                    int len = 0;
                    result = UTF8ToString(UnsafeNativeMethods.sqlite3_errmsg_interop(db, ref len), len);
#else
                    result = UTF8ToString(UnsafeNativeMethods.sqlite3_errmsg(db), -1);
#endif
                }
                else
                {
                    result = "closed or invalid connection handle";
                }
            }
        }
        GC.KeepAlive(hdl);
        return result;
    }

    internal static void FinishBackup(SQLiteConnectionHandle hdl, IntPtr backup)
    {
        if ((hdl == null) || (backup == IntPtr.Zero)) return;

        try
        {
            // do nothing.
        }
        finally /* NOTE: Thread.Abort() protection. */
        {
#if PLATFORM_COMPACTFRAMEWORK
            lock (hdl.syncRoot)
#else
            lock (hdl)
#endif
            {
#if !SQLITE_STANDARD
                SQLiteErrorCode n = UnsafeNativeMethods.sqlite3_backup_finish_interop(backup);
#else
                SQLiteErrorCode n = UnsafeNativeMethods.sqlite3_backup_finish(backup);
#endif
                if (n != SQLiteErrorCode.Ok) throw new SQLiteException(n, null);
            }
        }
    }

    internal static void FinalizeStatement(SQLiteConnectionHandle hdl, IntPtr stmt)
    {
        if ((hdl == null) || (stmt == IntPtr.Zero)) return;

        try
        {
            // do nothing.
        }
        finally /* NOTE: Thread.Abort() protection. */
        {
#if PLATFORM_COMPACTFRAMEWORK
            lock (hdl.syncRoot)
#else
            lock (hdl)
#endif
            {
#if !SQLITE_STANDARD
                SQLiteErrorCode n = UnsafeNativeMethods.sqlite3_finalize_interop(stmt);
#else
                SQLiteErrorCode n = UnsafeNativeMethods.sqlite3_finalize(stmt);
#endif
                if (n != SQLiteErrorCode.Ok) throw new SQLiteException(n, null);
            }
        }
    }

    internal static void CloseConnection(SQLiteConnectionHandle hdl, IntPtr db)
    {
        if ((hdl == null) || (db == IntPtr.Zero)) return;

        try
        {
            // do nothing.
        }
        finally /* NOTE: Thread.Abort() protection. */
        {
#if PLATFORM_COMPACTFRAMEWORK
            lock (hdl.syncRoot)
#else
            lock (hdl)
#endif
            {
#if !SQLITE_STANDARD
                SQLiteErrorCode n = UnsafeNativeMethods.sqlite3_close_interop(db);
#else
                ResetConnection(hdl, db, false);

                SQLiteErrorCode n = UnsafeNativeMethods.sqlite3_close(db);
#endif
                if (n != SQLiteErrorCode.Ok) throw new SQLiteException(n, GetLastError(hdl, db));
            }
        }
    }

#if !INTEROP_LEGACY_CLOSE
    internal static void CloseConnectionV2(SQLiteConnectionHandle hdl, IntPtr db)
    {
        if ((hdl == null) || (db == IntPtr.Zero)) return;

        try
        {
            // do nothing.
        }
        finally /* NOTE: Thread.Abort() protection. */
        {
#if PLATFORM_COMPACTFRAMEWORK
            lock (hdl.syncRoot)
#else
            lock (hdl)
#endif
            {
#if !SQLITE_STANDARD
                SQLiteErrorCode n = UnsafeNativeMethods.sqlite3_close_interop(db);
#else
                ResetConnection(hdl, db, false);

                SQLiteErrorCode n = UnsafeNativeMethods.sqlite3_close_v2(db);
#endif
                if (n != SQLiteErrorCode.Ok) throw new SQLiteException(n, GetLastError(hdl, db));
            }
        }
    }
#endif

    internal static bool ResetConnection(SQLiteConnectionHandle hdl, IntPtr db, bool canThrow)
    {
        if ((hdl == null) || (db == IntPtr.Zero)) return false;

        bool result = false;

        try
        {
            // do nothing.
        }
        finally /* NOTE: Thread.Abort() protection. */
        {
#if PLATFORM_COMPACTFRAMEWORK
            lock (hdl.syncRoot)
#else
            lock (hdl)
#endif
            {
                if (canThrow && hdl.IsInvalid)
                    throw new InvalidOperationException("The connection handle is invalid.");

                if (canThrow && hdl.IsClosed)
                    throw new InvalidOperationException("The connection handle is closed.");

                if (!hdl.IsInvalid && !hdl.IsClosed)
                {
                    IntPtr stmt = IntPtr.Zero;
                    SQLiteErrorCode n;

                    do
                    {
                        stmt = UnsafeNativeMethods.sqlite3_next_stmt(db, stmt);
                        if (stmt != IntPtr.Zero)
                        {
#if !SQLITE_STANDARD
                            n = UnsafeNativeMethods.sqlite3_reset_interop(stmt);
#else
                            n = UnsafeNativeMethods.sqlite3_reset(stmt);
#endif
                        }
                    } while (stmt != IntPtr.Zero);

                    //
                    // NOTE: Is a transaction NOT pending on the connection?
                    //
                    if (IsAutocommit(hdl, db))
                    {
                        result = true;
                    }
                    else
                    {
                        n = UnsafeNativeMethods.sqlite3_exec(
                            db, ToUTF8("ROLLBACK"), IntPtr.Zero, IntPtr.Zero,
                            ref stmt);

                        if (n == SQLiteErrorCode.Ok)
                        {
                            result = true;
                        }
                        else if (canThrow)
                        {
                            throw new SQLiteException(n, GetLastError(hdl, db));
                        }
                    }
                }
            }
        }
        GC.KeepAlive(hdl);
        return result;
    }

    internal static bool IsAutocommit(SQLiteConnectionHandle hdl, IntPtr db)
    {
        if ((hdl == null) || (db == IntPtr.Zero)) return false;

        bool result = false;

        try
        {
            // do nothing.
        }
        finally /* NOTE: Thread.Abort() protection. */
        {
#if PLATFORM_COMPACTFRAMEWORK
            lock (hdl.syncRoot)
#else
            lock (hdl)
#endif
            {
                if (!hdl.IsInvalid && !hdl.IsClosed)
                    result = (UnsafeNativeMethods.sqlite3_get_autocommit(db) == 1);
            }
        }
        GC.KeepAlive(hdl); /* NOTE: Unreachable code. */
        return result;
    }
  }

  /// <summary>
  /// 
  /// </summary>
  public interface ISQLiteSchemaExtensions
  {
      /// <summary>
      /// Creates temporary tables on the connection so schema information can be queried.
      /// </summary>
      /// <param name="connection">
      /// The connection upon which to build the schema tables.
      /// </param>
      void BuildTempSchema(SQLiteConnection connection);
  }

  [Flags]
  internal enum SQLiteOpenFlagsEnum
  {
    None = 0,
    ReadOnly = 0x01,
    ReadWrite = 0x02,
    Create = 0x04,
    Uri = 0x40,
    SharedCache = 0x01000000,
    Default = 0x06,
  }

  /// <summary>
  /// The extra behavioral flags that can be applied to a connection.
  /// </summary>
  [Flags()]
  public enum SQLiteConnectionFlags
  {
      /// <summary>
      /// No extra flags.
      /// </summary>
      None = 0x0,

      /// <summary>
      /// Enable logging of all SQL statements to be prepared.
      /// </summary>
      LogPrepare = 0x1,

      /// <summary>
      /// Enable logging of all bound parameter types and raw values.
      /// </summary>
      LogPreBind = 0x2,

      /// <summary>
      /// Enable logging of all bound parameter strongly typed values.
      /// </summary>
      LogBind = 0x4,

      /// <summary>
      /// Enable logging of all exceptions caught from user-provided
      /// managed code called from native code via delegates.
      /// </summary>
      LogCallbackException = 0x8,

      /// <summary>
      /// Enable logging of backup API errors.
      /// </summary>
      LogBackup = 0x10,

      /// <summary>
      /// Skip adding the extension functions provided by the native
      /// interop assembly.
      /// </summary>
      NoExtensionFunctions = 0x20,

      /// <summary>
      /// When binding parameter values with the <see cref="UInt32" />
      /// type, use the interop method that accepts an <see cref="Int64" />
      /// value.
      /// </summary>
      BindUInt32AsInt64 = 0x40,

      /// <summary>
      /// When binding parameter values, always bind them as though they were
      /// plain text (i.e. no numeric, date/time, or other conversions should
      /// be attempted).
      /// </summary>
      BindAllAsText = 0x80,

      /// <summary>
      /// When returning column values, always return them as though they were
      /// plain text (i.e. no numeric, date/time, or other conversions should
      /// be attempted).
      /// </summary>
      GetAllAsText = 0x100,

      /// <summary>
      /// Prevent this <see cref="SQLiteConnection" /> object instance from
      /// loading extensions.
      /// </summary>
      NoLoadExtension = 0x200,

      /// <summary>
      /// Prevent this <see cref="SQLiteConnection" /> object instance from
      /// creating virtual table modules.
      /// </summary>
      NoCreateModule = 0x400,

      /// <summary>
      /// Skip binding any functions provided by other managed assemblies when
      /// opening the connection.
      /// </summary>
      NoBindFunctions = 0x800,

      /// <summary>
      /// Skip setting the logging related properties of the
      /// <see cref="SQLiteModule" /> object instance that was passed to
      /// the <see cref="SQLiteConnection.CreateModule" /> method.
      /// </summary>
      NoLogModule = 0x1000,

      /// <summary>
      /// Enable logging of all virtual table module errors seen by the
      /// <see cref="SQLiteModule.SetTableError(IntPtr,String)" /> method.
      /// </summary>
      LogModuleError = 0x2000,

      /// <summary>
      /// Enable logging of certain virtual table module exceptions that cannot
      /// be easily discovered via other means.
      /// </summary>
      LogModuleException = 0x4000,

      /// <summary>
      /// Enable tracing of potentially important [non-fatal] error conditions
      /// that cannot be easily reported through other means.
      /// </summary>
      TraceWarning = 0x8000,

      /// <summary>
      /// When binding parameter values, always use the invariant culture when
      /// converting their values from strings.
      /// </summary>
      ConvertInvariantText = 0x10000,

      /// <summary>
      /// When binding parameter values, always use the invariant culture when
      /// converting their values to strings.
      /// </summary>
      BindInvariantText = 0x20000,

      /// <summary>
      /// Disable using the connection pool by default.  If the "Pooling"
      /// connection string property is specified, its value will override
      /// this flag.  The precise outcome of combining this flag with the
      /// <see cref="UseConnectionPool" /> flag is unspecified; however,
      /// one of the flags will be in effect.
      /// </summary>
      NoConnectionPool = 0x40000,

      /// <summary>
      /// Enable using the connection pool by default.  If the "Pooling"
      /// connection string property is specified, its value will override
      /// this flag.  The precise outcome of combining this flag with the
      /// <see cref="NoConnectionPool" /> flag is unspecified; however,
      /// one of the flags will be in effect.
      /// </summary>
      UseConnectionPool = 0x80000,

      /// <summary>
      /// Enable using per-connection mappings between type names and
      /// <see cref="DbType" /> values.  Also see the
      /// <see cref="SQLiteConnection.ClearTypeMappings" />,
      /// <see cref="SQLiteConnection.GetTypeMappings" />, and
      /// <see cref="SQLiteConnection.AddTypeMapping" /> methods.  These
      /// per-connection mappings, when present, override the corresponding
      /// global mappings.
      /// </summary>
      UseConnectionTypes = 0x100000,

      /// <summary>
      /// Disable using global mappings between type names and
      /// <see cref="DbType" /> values.  This may be useful in some very narrow
      /// cases; however, if there are no per-connection type mappings, the
      /// fallback defaults will be used for both type names and their
      /// associated <see cref="DbType" /> values.  Therefore, use of this flag
      /// is not recommended.
      /// </summary>
      NoGlobalTypes = 0x200000,

      /// <summary>
      /// When the <see cref="SQLiteDataReader.HasRows" /> property is used, it
      /// should return non-zero if there were ever any rows in the associated
      /// result sets.
      /// </summary>
      StickyHasRows = 0x400000,

      /// <summary>
      /// Enable "strict" transaction enlistment semantics.  Setting this flag
      /// will cause an exception to be thrown if an attempt is made to enlist
      /// in a transaction with an unavailable or unsupported isolation level.
      /// In the future, more extensive checks may be enabled by this flag as
      /// well.
      /// </summary>
      StrictEnlistment = 0x800000,

      /// <summary>
      /// Enable mapping of unsupported transaction isolation levels to the
      /// closest supported transaction isolation level.
      /// </summary>
      MapIsolationLevels = 0x1000000,

      /// <summary>
      /// When returning column values, attempt to detect the affinity of
      /// textual values by checking if they fully conform to those of the
      /// <see cref="TypeAffinity.Null" />,
      /// <see cref="TypeAffinity.Int64" />,
      /// <see cref="TypeAffinity.Double" />,
      /// or <see cref="TypeAffinity.DateTime" /> types.
      /// </summary>
      DetectTextAffinity = 0x2000000,

      /// <summary>
      /// When returning column values, attempt to detect the type of
      /// string values by checking if they fully conform to those of
      /// the <see cref="TypeAffinity.Null" />,
      /// <see cref="TypeAffinity.Int64" />,
      /// <see cref="TypeAffinity.Double" />,
      /// or <see cref="TypeAffinity.DateTime" /> types.
      /// </summary>
      DetectStringType = 0x4000000,

      /// <summary>
      /// Skip querying runtime configuration settings for use by the
      /// <see cref="SQLiteConvert" /> class, including the default
      /// <see cref="DbType" /> value and default database type name.
      /// <b>NOTE: If the <see cref="SQLiteConnection.DefaultDbType" />
      /// and/or <see cref="SQLiteConnection.DefaultTypeName" />
      /// properties are not set explicitly nor set via their connection
      /// string properties and repeated calls to determine these runtime
      /// configuration settings are seen to be a problem, this flag
      /// should be set.</b>
      /// </summary>
      NoConvertSettings = 0x8000000,

      /// <summary>
      /// When binding parameter values or returning column values, always
      /// treat them as though they were plain text (i.e. no numeric,
      /// date/time, or other conversions should be attempted).
      /// </summary>
      BindAndGetAllAsText = BindAllAsText | GetAllAsText,

      /// <summary>
      /// When binding parameter values, always use the invariant culture when
      /// converting their values to strings or from strings.
      /// </summary>
      ConvertAndBindInvariantText = ConvertInvariantText | BindInvariantText,

      /// <summary>
      /// When binding parameter values or returning column values, always
      /// treat them as though they were plain text (i.e. no numeric,
      /// date/time, or other conversions should be attempted) and always
      /// use the invariant culture when converting their values to strings.
      /// </summary>
      BindAndGetAllAsInvariantText = BindAndGetAllAsText | BindInvariantText,

      /// <summary>
      /// When binding parameter values or returning column values, always
      /// treat them as though they were plain text (i.e. no numeric,
      /// date/time, or other conversions should be attempted) and always
      /// use the invariant culture when converting their values to strings
      /// or from strings.
      /// </summary>
      ConvertAndBindAndGetAllAsInvariantText = BindAndGetAllAsText |
                                               ConvertAndBindInvariantText,

      /// <summary>
      /// Enable all logging.
      /// </summary>
      LogAll = LogPrepare | LogPreBind | LogBind |
               LogCallbackException | LogBackup | LogModuleError |
               LogModuleException,

      /// <summary>
      /// The default extra flags for new connections.
      /// </summary>
      Default = LogCallbackException | LogModuleException,

      /// <summary>
      /// The default extra flags for new connections with all logging enabled.
      /// </summary>
      DefaultAndLogAll = Default | LogAll
  }

  // These are the options to the internal sqlite3_config call.
  internal enum SQLiteConfigOpsEnum
  {
    SQLITE_CONFIG_NONE = 0, // nil
    SQLITE_CONFIG_SINGLETHREAD = 1, // nil
    SQLITE_CONFIG_MULTITHREAD = 2, // nil
    SQLITE_CONFIG_SERIALIZED = 3, // nil
    SQLITE_CONFIG_MALLOC = 4, // sqlite3_mem_methods*
    SQLITE_CONFIG_GETMALLOC = 5, // sqlite3_mem_methods*
    SQLITE_CONFIG_SCRATCH = 6, // void*, int sz, int N
    SQLITE_CONFIG_PAGECACHE = 7, // void*, int sz, int N
    SQLITE_CONFIG_HEAP = 8, // void*, int nByte, int min
    SQLITE_CONFIG_MEMSTATUS = 9, // boolean
    SQLITE_CONFIG_MUTEX = 10, // sqlite3_mutex_methods*
    SQLITE_CONFIG_GETMUTEX = 11, // sqlite3_mutex_methods*
    // previously SQLITE_CONFIG_CHUNKALLOC 12 which is now unused
    SQLITE_CONFIG_LOOKASIDE = 13, // int int
    SQLITE_CONFIG_PCACHE = 14, // sqlite3_pcache_methods*
    SQLITE_CONFIG_GETPCACHE = 15, // sqlite3_pcache_methods*
    SQLITE_CONFIG_LOG = 16, // xFunc, void*
    SQLITE_CONFIG_URI = 17, // int
    SQLITE_CONFIG_PCACHE2 = 18, // sqlite3_pcache_methods2*
    SQLITE_CONFIG_GETPCACHE2 = 19, // sqlite3_pcache_methods2*
    SQLITE_CONFIG_COVERING_INDEX_SCAN = 20, // int
    SQLITE_CONFIG_SQLLOG = 21, // xSqllog, void*
    SQLITE_CONFIG_MMAP_SIZE = 22, // sqlite3_int64, sqlite3_int64
    SQLITE_CONFIG_WIN32_HEAPSIZE = 23 // int nByte
  }
}
