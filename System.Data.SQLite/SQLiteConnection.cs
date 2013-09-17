/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 *
 * Released to the public domain, use at your own risk!
 ********************************************************/

namespace System.Data.SQLite
{
  using System;
  using System.Data;
  using System.Data.Common;
  using System.Diagnostics;
  using System.Collections.Generic;
  using System.Globalization;
  using System.ComponentModel;
  using System.Reflection;
  using System.Runtime.InteropServices;
  using System.IO;
  using System.Text;

  /////////////////////////////////////////////////////////////////////////////////////////////////

  /// <summary>
  /// Event data for connection event handlers.
  /// </summary>
  public class ConnectionEventArgs : EventArgs
  {
      /// <summary>
      /// The type of event being raised.
      /// </summary>
      public readonly SQLiteConnectionEventType EventType;

      /// <summary>
      /// The <see cref="StateChangeEventArgs" /> associated with this event, if any.
      /// </summary>
      public readonly StateChangeEventArgs EventArgs;

      /// <summary>
      /// The transaction associated with this event, if any.
      /// </summary>
      public readonly IDbTransaction Transaction;

      /// <summary>
      /// The command associated with this event, if any.
      /// </summary>
      public readonly IDbCommand Command;

      /// <summary>
      /// The data reader associated with this event, if any.
      /// </summary>
      public readonly IDataReader DataReader;

      /// <summary>
      /// The critical handle associated with this event, if any.
      /// </summary>
#if !PLATFORM_COMPACTFRAMEWORK
      public readonly CriticalHandle CriticalHandle;
#else
      public readonly object CriticalHandle;
#endif

      /// <summary>
      /// Command or message text associated with this event, if any.
      /// </summary>
      public readonly string Text;

      /// <summary>
      /// Extra data associated with this event, if any.
      /// </summary>
      public readonly object Data;

      /// <summary>
      /// Constructs the object.
      /// </summary>
      /// <param name="eventType">The type of event being raised.</param>
      /// <param name="eventArgs">The base <see cref="EventArgs" /> associated
      /// with this event, if any.</param>
      /// <param name="transaction">The transaction associated with this event, if any.</param>
      /// <param name="command">The command associated with this event, if any.</param>
      /// <param name="dataReader">The data reader associated with this event, if any.</param>
      /// <param name="criticalHandle">The critical handle associated with this event, if any.</param>
      /// <param name="text">The command or message text, if any.</param>
      /// <param name="data">The extra data, if any.</param>
      internal ConnectionEventArgs(
          SQLiteConnectionEventType eventType,
          StateChangeEventArgs eventArgs,
          IDbTransaction transaction,
          IDbCommand command,
          IDataReader dataReader,
#if !PLATFORM_COMPACTFRAMEWORK
          CriticalHandle criticalHandle,
#else
          object criticalHandle,
#endif
          string text,
          object data
          )
      {
          EventType = eventType;
          EventArgs = eventArgs;
          Transaction = transaction;
          Command = command;
          DataReader = dataReader;
          CriticalHandle = criticalHandle;
          Text = text;
          Data = data;
      }
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////

  /// <summary>
  /// Raised when an event pertaining to a connection occurs.
  /// </summary>
  /// <param name="sender">The connection involved.</param>
  /// <param name="e">Extra information about the event.</param>
  public delegate void SQLiteConnectionEventHandler(object sender, ConnectionEventArgs e);

  /////////////////////////////////////////////////////////////////////////////////////////////////

  /// <summary>
  /// SQLite implentation of DbConnection.
  /// </summary>
  /// <remarks>
  /// The <see cref="ConnectionString" /> property can contain the following parameter(s), delimited with a semi-colon:
  /// <list type="table">
  /// <listheader>
  /// <term>Parameter</term>
  /// <term>Values</term>
  /// <term>Required</term>
  /// <term>Default</term>
  /// </listheader>
  /// <item>
  /// <description>Data Source</description>
  /// <description>
  /// This may be a file name, the string ":memory:", or any supported URI (starting with SQLite 3.7.7).
  /// Starting with release 1.0.86.0, in order to use more than one consecutive backslash (e.g. for a
  /// UNC path), each of the adjoining backslash characters must be doubled (e.g. "\\Network\Share\test.db"
  /// would become "\\\\Network\Share\test.db").
  /// </description>
  /// <description>Y</description>
  /// <description></description>
  /// </item>
  /// <item>
  /// <description>Version</description>
  /// <description>3</description>
  /// <description>N</description>
  /// <description>3</description>
  /// </item>
  /// <item>
  /// <description>UseUTF16Encoding</description>
  /// <description><b>True</b><br/><b>False</b></description>
  /// <description>N</description>
  /// <description>False</description>
  /// </item>
  /// <item>
  /// <description>DateTimeFormat</description>
  /// <description>
  /// <b>Ticks</b> - Use the value of DateTime.Ticks.<br/>
  /// <b>ISO8601</b> - Use the ISO-8601 format.  Uses the "yyyy-MM-dd HH:mm:ss.FFFFFFFK" format for UTC
  /// DateTime values and "yyyy-MM-dd HH:mm:ss.FFFFFFF" format for local DateTime values).<br/>
  /// <b>JulianDay</b> - The interval of time in days and fractions of a day since January 1, 4713 BC.<br/>
  /// <b>UnixEpoch</b> - The whole number of seconds since the Unix epoch (January 1, 1970).<br/>
  /// <b>InvariantCulture</b> - Any culture-independent string value that the .NET Framework can interpret as a valid DateTime.<br/>
  /// <b>CurrentCulture</b> - Any string value that the .NET Framework can interpret as a valid DateTime using the current culture.</description>
  /// <description>N</description>
  /// <description>ISO8601</description>
  /// </item>
  /// <item>
  /// <description>DateTimeKind</description>
  /// <description><b>Unspecified</b> - Not specified as either UTC or local time.<br/><b>Utc</b> - The time represented is UTC.<br/><b>Local</b> - The time represented is local time.</description>
  /// <description>N</description>
  /// <description>Unspecified</description>
  /// </item>
  /// <item>
  /// <description>DateTimeFormatString</description>
  /// <description>The exact DateTime format string to use for all formatting and parsing of all DateTime
  /// values for this connection.</description>
  /// <description>N</description>
  /// <description>null</description>
  /// </item>
  /// <item>
  /// <description>BaseSchemaName</description>
  /// <description>Some base data classes in the framework (e.g. those that build SQL queries dynamically)
  /// assume that an ADO.NET provider cannot support an alternate catalog (i.e. database) without supporting
  /// alternate schemas as well; however, SQLite does not fit into this model.  Therefore, this value is used
  /// as a placeholder and removed prior to preparing any SQL statements that may contain it.</description>
  /// <description>N</description>
  /// <description>sqlite_default_schema</description>
  /// </item>
  /// <item>
  /// <description>BinaryGUID</description>
  /// <description><b>True</b> - Store GUID columns in binary form<br/><b>False</b> - Store GUID columns as text</description>
  /// <description>N</description>
  /// <description>True</description>
  /// </item>
  /// <item>
  /// <description>Cache Size</description>
  /// <description>{size in bytes}</description>
  /// <description>N</description>
  /// <description>2000</description>
  /// </item>
  /// <item>
  /// <description>Synchronous</description>
  /// <description><b>Normal</b> - Normal file flushing behavior<br/><b>Full</b> - Full flushing after all writes<br/><b>Off</b> - Underlying OS flushes I/O's</description>
  /// <description>N</description>
  /// <description>Full</description>
  /// </item>
  /// <item>
  /// <description>Page Size</description>
  /// <description>{size in bytes}</description>
  /// <description>N</description>
  /// <description>1024</description>
  /// </item>
  /// <item>
  /// <description>Password</description>
  /// <description>{password} - Using this parameter requires that the CryptoAPI based codec be enabled at compile-time for both the native interop assembly and the core managed assemblies; otherwise, using this parameter may result in an exception being thrown when attempting to open the connection.</description>
  /// <description>N</description>
  /// <description></description>
  /// </item>
  /// <item>
  /// <description>HexPassword</description>
  /// <description>{hexPassword} - Must contain a sequence of zero or more hexadecimal encoded byte values without a leading "0x" prefix.  Using this parameter requires that the CryptoAPI based codec be enabled at compile-time for both the native interop assembly and the core managed assemblies; otherwise, using this parameter may result in an exception being thrown when attempting to open the connection.</description>
  /// <description>N</description>
  /// <description></description>
  /// </item>
  /// <item>
  /// <description>Enlist</description>
  /// <description><b>Y</b> - Automatically enlist in distributed transactions<br/><b>N</b> - No automatic enlistment</description>
  /// <description>N</description>
  /// <description>Y</description>
  /// </item>
  /// <item>
  /// <description>Pooling</description>
  /// <description>
  /// <b>True</b> - Use connection pooling.<br/>
  /// <b>False</b> - Do not use connection pooling.<br/><br/>
  /// <b>WARNING:</b> When using the default connection pool implementation,
  /// setting this property to True should be avoided by applications that make
  /// use of COM (either directly or indirectly) due to possible deadlocks that
  /// can occur during the finalization of some COM objects.
  /// </description>
  /// <description>N</description>
  /// <description>False</description>
  /// </item>
  /// <item>
  /// <description>FailIfMissing</description>
  /// <description><b>True</b> - Don't create the database if it does not exist, throw an error instead<br/><b>False</b> - Automatically create the database if it does not exist</description>
  /// <description>N</description>
  /// <description>False</description>
  /// </item>
  /// <item>
  /// <description>Max Page Count</description>
  /// <description>{size in pages} - Limits the maximum number of pages (limits the size) of the database</description>
  /// <description>N</description>
  /// <description>0</description>
  /// </item>
  /// <item>
  /// <description>Legacy Format</description>
  /// <description><b>True</b> - Use the more compatible legacy 3.x database format<br/><b>False</b> - Use the newer 3.3x database format which compresses numbers more effectively</description>
  /// <description>N</description>
  /// <description>False</description>
  /// </item>
  /// <item>
  /// <description>Default Timeout</description>
  /// <description>{time in seconds}<br/>The default command timeout</description>
  /// <description>N</description>
  /// <description>30</description>
  /// </item>
  /// <item>
  /// <description>Journal Mode</description>
  /// <description><b>Delete</b> - Delete the journal file after a commit<br/><b>Persist</b> - Zero out and leave the journal file on disk after a commit<br/><b>Off</b> - Disable the rollback journal entirely</description>
  /// <description>N</description>
  /// <description>Delete</description>
  /// </item>
  /// <item>
  /// <description>Read Only</description>
  /// <description><b>True</b> - Open the database for read only access<br/><b>False</b> - Open the database for normal read/write access</description>
  /// <description>N</description>
  /// <description>False</description>
  /// </item>
  /// <item>
  /// <description>Max Pool Size</description>
  /// <description>The maximum number of connections for the given connection string that can be in the connection pool</description>
  /// <description>N</description>
  /// <description>100</description>
  /// </item>
  /// <item>
  /// <description>Default IsolationLevel</description>
  /// <description>The default transaciton isolation level</description>
  /// <description>N</description>
  /// <description>Serializable</description>
  /// </item>
  /// <item>
  /// <description>Foreign Keys</description>
  /// <description>Enable foreign key constraints</description>
  /// <description>N</description>
  /// <description>False</description>
  /// </item>
  /// <item>
  /// <description>Flags</description>
  /// <description>Extra behavioral flags for the connection.  See the <see cref="SQLiteConnectionFlags" /> enumeration for possible values.</description>
  /// <description>N</description>
  /// <description>Default</description>
  /// </item>
  /// <item>
  /// <description>SetDefaults</description>
  /// <description>
  /// <b>True</b> - Apply the default connection settings to the opened database.<br/>
  /// <b>False</b> - Skip applying the default connection settings to the opened database.
  /// </description>
  /// <description>N</description>
  /// <description>True</description>
  /// </item>
  /// <item>
  /// <description>ToFullPath</description>
  /// <description>
  /// <b>True</b> - Attempt to expand the data source file name to a fully qualified path before opening.<br/>
  /// <b>False</b> - Skip attempting to expand the data source file name to a fully qualified path before opening.
  /// </description>
  /// <description>N</description>
  /// <description>True</description>
  /// </item>
  /// </list>
  /// </remarks>
  public sealed partial class SQLiteConnection : DbConnection, ICloneable
  {
    #region Private Constants
    /// <summary>
    /// The default "stub" (i.e. placeholder) base schema name to use when
    /// returning column schema information.  Used as the initial value of
    /// the BaseSchemaName property.  This should start with "sqlite_*"
    /// because those names are reserved for use by SQLite (i.e. they cannot
    /// be confused with the names of user objects).
    /// </summary>
    internal const string DefaultBaseSchemaName = "sqlite_default_schema";

    private const string MemoryFileName = ":memory:";

    private const SQLiteConnectionFlags DefaultFlags = SQLiteConnectionFlags.Default;
    private const SQLiteSynchronousEnum DefaultSynchronous = SQLiteSynchronousEnum.Default;
    private const SQLiteJournalModeEnum DefaultJournalMode = SQLiteJournalModeEnum.Default;
    private const IsolationLevel DefaultIsolationLevel = IsolationLevel.Serializable;
    private const SQLiteDateFormats DefaultDateTimeFormat = SQLiteDateFormats.ISO8601;
    private const DateTimeKind DefaultDateTimeKind = DateTimeKind.Unspecified;
    private const string DefaultDateTimeFormatString = null;
    private const string DefaultDataSource = null;
    private const string DefaultUri = null;
    private const string DefaultFullUri = null;
    private const string DefaultHexPassword = null;
    private const string DefaultPassword = null;
    private const int DefaultVersion = 3;
    private const int DefaultPageSize = 1024;
    private const int DefaultMaxPageCount = 0;
    private const int DefaultCacheSize = 2000;
    private const int DefaultMaxPoolSize = 100;
    private const int DefaultConnectionTimeout = 30;
    private const bool DefaultFailIfMissing = false;
    private const bool DefaultReadOnly = false;
    private const bool DefaultBinaryGUID = true;
    private const bool DefaultUseUTF16Encoding = false;
    private const bool DefaultToFullPath = true;
    private const bool DefaultPooling = false;
    private const bool DefaultLegacyFormat = false;
    private const bool DefaultForeignKeys = false;
    private const bool DefaultEnlist = true;
    private const bool DefaultSetDefaults = true;

    private const int SQLITE_FCNTL_WIN32_AV_RETRY = 9;

    private const string _dataDirectory = "|DataDirectory|";
    private const string _masterdb = "sqlite_master";
    private const string _tempmasterdb = "sqlite_temp_master";
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////

    #region Private Static Data
    /// <summary>
    /// The managed assembly containing this type.
    /// </summary>
    private static readonly Assembly _assembly = typeof(SQLiteConnection).Assembly;

    /// <summary>
    /// Object used to synchronize access to the static instance data
    /// for this class.
    /// </summary>
    private static readonly object _syncRoot = new object();

    /// <summary>
    /// Static variable to store the connection event handlers to call.
    /// </summary>
    private static event SQLiteConnectionEventHandler _handlers;

#if SQLITE_STANDARD && !PLATFORM_COMPACTFRAMEWORK
    /// <summary>
    /// Used to hold the active library version number of SQLite.
    /// </summary>
    private static int _versionNumber;
#endif
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////

    #region Private Data
    /// <summary>
    /// State of the current connection
    /// </summary>
    private ConnectionState _connectionState;
    /// <summary>
    /// The connection string
    /// </summary>
    private string _connectionString;
    /// <summary>
    /// Nesting level of the transactions open on the connection
    /// </summary>
    internal int _transactionLevel;

    /// <summary>
    /// If set, then the connection is currently being disposed.
    /// </summary>
    private bool _disposing;

    /// <summary>
    /// The default isolation level for new transactions
    /// </summary>
    private IsolationLevel _defaultIsolation;

#if !PLATFORM_COMPACTFRAMEWORK
    /// <summary>
    /// Whether or not the connection is enlisted in a distrubuted transaction
    /// </summary>
    internal SQLiteEnlistment _enlistment;
#endif

    /// <summary>
    /// The base SQLite object to interop with
    /// </summary>
    internal SQLiteBase _sql;
    /// <summary>
    /// The database filename minus path and extension
    /// </summary>
    private string _dataSource;

#if INTEROP_CODEC
    /// <summary>
    /// Temporary password storage, emptied after the database has been opened
    /// </summary>
    private byte[] _password;
#endif

    /// <summary>
    /// The "stub" (i.e. placeholder) base schema name to use when returning
    /// column schema information.
    /// </summary>
    internal string _baseSchemaName;

    /// <summary>
    /// The extra behavioral flags for this connection, if any.  See the
    /// <see cref="SQLiteConnectionFlags" /> enumeration for a list of
    /// possible values.
    /// </summary>
    private SQLiteConnectionFlags _flags;

    /// <summary>
    /// Default command timeout
    /// </summary>
    private int _defaultTimeout = 30;

    /// <summary>
    /// Non-zero if the built-in (i.e. framework provided) connection string
    /// parser should be used when opening the connection.
    /// </summary>
    private bool _parseViaFramework;

    internal bool _binaryGuid;

    internal long _version;

    private event SQLiteUpdateEventHandler _updateHandler;
    private event SQLiteCommitHandler _commitHandler;
    private event SQLiteTraceEventHandler _traceHandler;
    private event EventHandler _rollbackHandler;

    private SQLiteUpdateCallback _updateCallback;
    private SQLiteCommitCallback _commitCallback;
    private SQLiteTraceCallback _traceCallback;
    private SQLiteRollbackCallback _rollbackCallback;
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This event is raised whenever the database is opened or closed.
    /// </summary>
    public override event StateChangeEventHandler StateChange;

    ///////////////////////////////////////////////////////////////////////////////////////////////

    ///<overloads>
    /// Constructs a new SQLiteConnection object
    /// </overloads>
    /// <summary>
    /// Default constructor
    /// </summary>
    public SQLiteConnection()
      : this((string)null)
    {
    }

    /// <summary>
    /// Initializes the connection with the specified connection string.
    /// </summary>
    /// <param name="connectionString">The connection string to use.</param>
    public SQLiteConnection(string connectionString)
        : this(connectionString, false)
    {
        // do nothing.
    }

    /// <summary>
    /// Initializes the connection with a pre-existing native connection handle.
    /// This constructor overload is intended to be used only by the private
    /// <see cref="SQLiteModule.CreateOrConnect" /> method.
    /// </summary>
    /// <param name="db">
    /// The native connection handle to use.
    /// </param>
    /// <param name="fileName">
    /// The file name corresponding to the native connection handle.
    /// </param>
    /// <param name="ownHandle">
    /// Non-zero if this instance owns the native connection handle and
    /// should dispose of it when it is no longer needed.
    /// </param>
    internal SQLiteConnection(IntPtr db, string fileName, bool ownHandle)
        : this()
    {
        _sql = new SQLite3(
            SQLiteDateFormats.Default, DateTimeKind.Unspecified, null,
            db, fileName, ownHandle);

        _flags = SQLiteConnectionFlags.None;

        _connectionState = (db != IntPtr.Zero) ?
            ConnectionState.Open : ConnectionState.Closed;

        _connectionString = null; /* unknown */
    }

    /// <summary>
    /// Initializes the connection with the specified connection string.
    /// </summary>
    /// <param name="connectionString">
    /// The connection string to use.
    /// </param>
    /// <param name="parseViaFramework">
    /// Non-zero to parse the connection string using the built-in (i.e.
    /// framework provided) parser when opening the connection.
    /// </param>
    public SQLiteConnection(string connectionString, bool parseViaFramework)
    {
#if (SQLITE_STANDARD || USE_INTEROP_DLL || PLATFORM_COMPACTFRAMEWORK) && PRELOAD_NATIVE_LIBRARY
      UnsafeNativeMethods.Initialize();
#endif

#if !INTEROP_LOG
      SQLiteLog.Initialize();
#endif

#if !PLATFORM_COMPACTFRAMEWORK && !INTEROP_LEGACY_CLOSE && SQLITE_STANDARD
      //
      // NOTE: Check if the sqlite3_close_v2() native API should be available
      //       to use.  This must be done dynamically because the delegate set
      //       here is used by the SQLiteConnectionHandle class, which is a
      //       CriticalHandle derived class (i.e. protected by a constrained
      //       execution region).  Therefore, if the underlying native entry
      //       point is unavailable, an exception will be raised even if it is
      //       never actually called (i.e. because the runtime eagerly prepares
      //       all the methods in the call graph of the constrained execution
      //       region).
      //
      lock (_syncRoot)
      {
          if (_versionNumber == 0)
          {
              _versionNumber = SQLite3.SQLiteVersionNumber;

              if (_versionNumber >= 3007014)
                  SQLiteConnectionHandle.closeConnection = SQLiteBase.CloseConnectionV2;
          }
      }
#endif

#if INTEROP_LOG
      if (UnsafeNativeMethods.sqlite3_config_log_interop() == SQLiteErrorCode.Ok)
      {
          UnsafeNativeMethods.sqlite3_log(
              SQLiteErrorCode.Ok, SQLiteConvert.ToUTF8("logging initialized."));
      }
#endif

      _parseViaFramework = parseViaFramework;
      _flags = SQLiteConnectionFlags.Default;
      _connectionState = ConnectionState.Closed;
      _connectionString = null;

      if (connectionString != null)
        ConnectionString = connectionString;
    }

    /// <summary>
    /// Clones the settings and connection string from an existing connection.  If the existing connection is already open, this
    /// function will open its own connection, enumerate any attached databases of the original connection, and automatically
    /// attach to them.
    /// </summary>
    /// <param name="connection">The connection to copy the settings from.</param>
    public SQLiteConnection(SQLiteConnection connection)
      : this(connection.ConnectionString, connection.ParseViaFramework)
    {
      if (connection.State == ConnectionState.Open)
      {
        Open();

        // Reattach all attached databases from the existing connection
        using (DataTable tbl = connection.GetSchema("Catalogs"))
        {
          foreach (DataRow row in tbl.Rows)
          {
            string str = row[0].ToString();
            if (String.Compare(str, "main", StringComparison.OrdinalIgnoreCase) != 0
              && String.Compare(str, "temp", StringComparison.OrdinalIgnoreCase) != 0)
            {
              using (SQLiteCommand cmd = CreateCommand())
              {
                cmd.CommandText = String.Format(CultureInfo.InvariantCulture, "ATTACH DATABASE '{0}' AS [{1}]", row[1], row[0]);
                cmd.ExecuteNonQuery();
              }
            }
          }
        }
      }
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Raises the <see cref="Changed" /> event.
    /// </summary>
    /// <param name="connection">
    /// The connection associated with this event.  If this parameter is not
    /// null and the specified connection cannot raise events, then the
    /// registered event handlers will not be invoked.
    /// </param>
    /// <param name="e">
    /// A <see cref="ConnectionEventArgs" /> that contains the event data.
    /// </param>
    internal static void OnChanged(
        SQLiteConnection connection,
        ConnectionEventArgs e
        )
    {
#if !PLATFORM_COMPACTFRAMEWORK
        if ((connection != null) && !connection.CanRaiseEvents)
            return;
#endif

        SQLiteConnectionEventHandler handlers;

        lock (_syncRoot)
        {
            if (_handlers != null)
                handlers = _handlers.Clone() as SQLiteConnectionEventHandler;
            else
                handlers = null;
        }

        if (handlers != null) handlers(connection, e);
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This event is raised when events related to the lifecycle of a
    /// SQLiteConnection object occur.
    /// </summary>
    public static event SQLiteConnectionEventHandler Changed
    {
        add
        {
            lock (_syncRoot)
            {
                // Remove any copies of this event handler from registered
                // list.  This essentially means that a handler will be
                // called only once no matter how many times it is added.
                _handlers -= value;

                // Add this to the list of event handlers.
                _handlers += value;
            }
        }
        remove
        {
            lock (_syncRoot)
            {
                _handlers -= value;
            }
        }
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This property is used to obtain or set the custom connection pool
    /// implementation to use, if any.  Setting this property to null will
    /// cause the default connection pool implementation to be used.
    /// </summary>
    public static ISQLiteConnectionPool ConnectionPool
    {
        get { return SQLiteConnectionPool.GetConnectionPool(); }
        set { SQLiteConnectionPool.SetConnectionPool(value); }
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Creates and returns a new managed database connection handle.  This
    /// method is intended to be used by implementations of the
    /// <see cref="ISQLiteConnectionPool" /> interface only.  In theory, it
    /// could be used by other classes; however, that usage is not supported.
    /// </summary>
    /// <param name="nativeHandle">
    /// This must be a native database connection handle returned by the
    /// SQLite core library and it must remain valid and open during the
    /// entire duration of the calling method.
    /// </param>
    /// <returns>
    /// The new managed database connection handle or null if it cannot be
    /// created.
    /// </returns>
    public static object CreateHandle(
        IntPtr nativeHandle
        )
    {
        SQLiteConnectionHandle result;

        try
        {
            // do nothing.
        }
        finally /* NOTE: Thread.Abort() protection. */
        {
            result = (nativeHandle != IntPtr.Zero) ?
                new SQLiteConnectionHandle(nativeHandle, true) : null;
        }

        if (result != null)
        {
            SQLiteConnection.OnChanged(null, new ConnectionEventArgs(
                SQLiteConnectionEventType.NewCriticalHandle, null, null,
                null, null, result, null, new object[] { nativeHandle }));
        }

        return result;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    #region Backup API Members
    /// <summary>
    /// Backs up the database, using the specified database connection as the
    /// destination.
    /// </summary>
    /// <param name="destination">The destination database connection.</param>
    /// <param name="destinationName">The destination database name.</param>
    /// <param name="sourceName">The source database name.</param>
    /// <param name="pages">
    /// The number of pages to copy or negative to copy all remaining pages.
    /// </param>
    /// <param name="callback">
    /// The method to invoke between each step of the backup process.  This
    /// parameter may be null (i.e. no callbacks will be performed).
    /// </param>
    /// <param name="retryMilliseconds">
    /// The number of milliseconds to sleep after encountering a locking error
    /// during the backup process.  A value less than zero means that no sleep
    /// should be performed.
    /// </param>
    public void BackupDatabase(
        SQLiteConnection destination,
        string destinationName,
        string sourceName,
        int pages,
        SQLiteBackupCallback callback,
        int retryMilliseconds
        )
    {
        CheckDisposed();

        if (_connectionState != ConnectionState.Open)
            throw new InvalidOperationException(
                "Source database is not open.");

        if (destination == null)
            throw new ArgumentNullException("destination");

        if (destination._connectionState != ConnectionState.Open)
            throw new ArgumentException(
                "Destination database is not open.", "destination");

        if (destinationName == null)
            throw new ArgumentNullException("destinationName");

        if (sourceName == null)
            throw new ArgumentNullException("sourceName");

        SQLiteBase sqliteBase = _sql;

        if (sqliteBase == null)
            throw new InvalidOperationException(
                "Connection object has an invalid handle.");

        SQLiteBackup backup = null;

        try
        {
            backup = sqliteBase.InitializeBackup(
                destination, destinationName, sourceName); /* throw */

            bool retry;

            while (sqliteBase.StepBackup(backup, pages, out retry)) /* throw */
            {
                //
                // NOTE: If a callback was supplied by our caller, call it.
                //       If it returns false, halt the backup process.
                //
                if ((callback != null) && !callback(this, sourceName,
                        destination, destinationName, pages,
                        sqliteBase.RemainingBackup(backup),
                        sqliteBase.PageCountBackup(backup), retry))
                {
                    break;
                }

                //
                // NOTE: If we need to retry the previous operation, wait for
                //       the number of milliseconds specified by our caller
                //       unless the caller used a negative number, in that case
                //       skip sleeping at all because we do not want to block
                //       this thread forever.
                //
                if (retry && (retryMilliseconds >= 0))
                    System.Threading.Thread.Sleep(retryMilliseconds);

                //
                // NOTE: There is no point in calling the native API to copy
                //       zero pages as it does nothing; therefore, stop now.
                //
                if (pages == 0)
                    break;
            }
        }
        catch (Exception e)
        {
            if ((_flags & SQLiteConnectionFlags.LogBackup) == SQLiteConnectionFlags.LogBackup)
            {
                SQLiteLog.LogMessage(String.Format(
                    CultureInfo.CurrentCulture,
                    "Caught exception while backing up database: {0}", e));
            }

            throw;
        }
        finally
        {
            if (backup != null)
                sqliteBase.FinishBackup(backup); /* throw */
        }
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Attempts to bind the specified <see cref="SQLiteFunction" /> object
    /// instance to this connection.
    /// </summary>
    /// <param name="functionAttribute">
    /// The <see cref="SQLiteFunctionAttribute"/> object instance containing
    /// the metadata for the function to be bound.
    /// </param>
    /// <param name="function">
    /// The <see cref="SQLiteFunction"/> object instance that implements the
    /// function to be bound.
    /// </param>
    public void BindFunction(
        SQLiteFunctionAttribute functionAttribute,
        SQLiteFunction function
        )
    {
        CheckDisposed();

        if (_sql == null)
            throw new InvalidOperationException(
                "Database connection not valid for binding functions.");

        _sql.BindFunction(functionAttribute, function, _flags);
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Conditional("CHECK_STATE")]
    internal static void Check(SQLiteConnection connection)
    {
        if (connection == null)
            throw new ArgumentNullException("connection");

        connection.CheckDisposed();

        if (connection._connectionState != ConnectionState.Open)
            throw new InvalidOperationException("The connection is not open.");

        SQLite3 sql = connection._sql as SQLite3;

        if (sql == null)
            throw new InvalidOperationException("The connection handle wrapper is null.");

        SQLiteConnectionHandle handle = sql._sql;

        if (handle == null)
            throw new InvalidOperationException("The connection handle is null.");

        if (handle.IsInvalid)
            throw new InvalidOperationException("The connection handle is invalid.");

        if (handle.IsClosed)
            throw new InvalidOperationException("The connection handle is closed.");
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    private static SortedList<string, string> ParseConnectionString(
        string connectionString,
        bool parseViaFramework
        )
    {
        return parseViaFramework ?
            ParseConnectionStringViaFramework(connectionString, false) :
            ParseConnectionString(connectionString);
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    private void SetupSQLiteBase(SortedList<string, string> opts)
    {
        object enumValue;

        enumValue = TryParseEnum(
            typeof(SQLiteDateFormats), FindKey(opts, "DateTimeFormat",
            DefaultDateTimeFormat.ToString()), true);

        SQLiteDateFormats dateFormat = (enumValue is SQLiteDateFormats) ?
            (SQLiteDateFormats)enumValue : DefaultDateTimeFormat;

        enumValue = TryParseEnum(
            typeof(DateTimeKind), FindKey(opts, "DateTimeKind",
            DefaultDateTimeKind.ToString()), true);

        DateTimeKind kind = (enumValue is DateTimeKind) ?
            (DateTimeKind)enumValue : DefaultDateTimeKind;

        string dateTimeFormat = FindKey(opts, "DateTimeFormatString",
            DefaultDateTimeFormatString);

        //
        // NOTE: SQLite automatically sets the encoding of the database
        //       to UTF16 if called from sqlite3_open16().
        //
        if (SQLiteConvert.ToBoolean(FindKey(opts, "UseUTF16Encoding",
                  DefaultUseUTF16Encoding.ToString())))
        {
            _sql = new SQLite3_UTF16(
                dateFormat, kind, dateTimeFormat, IntPtr.Zero, null,
                false);
        }
        else
        {
            _sql = new SQLite3(
                dateFormat, kind, dateTimeFormat, IntPtr.Zero, null,
                false);
        }
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    #region IDisposable "Pattern" Members
    private bool disposed;
    private void CheckDisposed() /* throw */
    {
#if THROW_ON_DISPOSED
        if (disposed)
            throw new ObjectDisposedException(typeof(SQLiteConnection).Name);
#endif
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    protected override void Dispose(bool disposing)
    {
        _disposing = true;

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

                Close();
            }
        }
        finally
        {
            base.Dispose(disposing);

            //
            // NOTE: Everything should be fully disposed at this point.
            //
            disposed = true;
        }
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if PLATFORM_COMPACTFRAMEWORK
    /// <summary>
    /// Obsolete
    /// </summary>
    public override int ConnectionTimeout
    {
      get
      {
        CheckDisposed();
        return 30;
      }
    }
#endif

    /// <summary>
    /// Creates a clone of the connection.  All attached databases and user-defined functions are cloned.  If the existing connection is open, the cloned connection
    /// will also be opened.
    /// </summary>
    /// <returns></returns>
    public object Clone()
    {
      CheckDisposed();
      return new SQLiteConnection(this);
    }

    /// <summary>
    /// Creates a database file.  This just creates a zero-byte file which SQLite
    /// will turn into a database when the file is opened properly.
    /// </summary>
    /// <param name="databaseFileName">The file to create</param>
    static public void CreateFile(string databaseFileName)
    {
      FileStream fs = File.Create(databaseFileName);
      fs.Close();
    }

    /// <summary>
    /// Raises the state change event when the state of the connection changes
    /// </summary>
    /// <param name="newState">The new connection state.  If this is different
    /// from the previous state, the <see cref="StateChange" /> event is
    /// raised.</param>
    /// <param name="eventArgs">The event data created for the raised event, if
    /// it was actually raised.</param>
    internal void OnStateChange(
        ConnectionState newState,
        ref StateChangeEventArgs eventArgs
        )
    {
        ConnectionState oldState = _connectionState;

        _connectionState = newState;

        if ((StateChange != null) && (newState != oldState))
        {
            StateChangeEventArgs localEventArgs =
                new StateChangeEventArgs(oldState, newState);

            StateChange(this, localEventArgs);

            eventArgs = localEventArgs;
        }
    }

    /// <summary>
    /// OBSOLETE.  Creates a new SQLiteTransaction if one isn't already active on the connection.
    /// </summary>
    /// <param name="isolationLevel">This parameter is ignored.</param>
    /// <param name="deferredLock">When TRUE, SQLite defers obtaining a write lock until a write operation is requested.
    /// When FALSE, a writelock is obtained immediately.  The default is TRUE, but in a multi-threaded multi-writer
    /// environment, one may instead choose to lock the database immediately to avoid any possible writer deadlock.</param>
    /// <returns>Returns a SQLiteTransaction object.</returns>
    [Obsolete("Use one of the standard BeginTransaction methods, this one will be removed soon")]
    public SQLiteTransaction BeginTransaction(IsolationLevel isolationLevel, bool deferredLock)
    {
      CheckDisposed();
      return (SQLiteTransaction)BeginDbTransaction(deferredLock == false ? IsolationLevel.Serializable : IsolationLevel.ReadCommitted);
    }

    /// <summary>
    /// OBSOLETE.  Creates a new SQLiteTransaction if one isn't already active on the connection.
    /// </summary>
    /// <param name="deferredLock">When TRUE, SQLite defers obtaining a write lock until a write operation is requested.
    /// When FALSE, a writelock is obtained immediately.  The default is false, but in a multi-threaded multi-writer
    /// environment, one may instead choose to lock the database immediately to avoid any possible writer deadlock.</param>
    /// <returns>Returns a SQLiteTransaction object.</returns>
    [Obsolete("Use one of the standard BeginTransaction methods, this one will be removed soon")]
    public SQLiteTransaction BeginTransaction(bool deferredLock)
    {
      CheckDisposed();
      return (SQLiteTransaction)BeginDbTransaction(deferredLock == false ? IsolationLevel.Serializable : IsolationLevel.ReadCommitted);
    }

    /// <summary>
    /// Creates a new <see cref="SQLiteTransaction" /> if one isn't already active on the connection.
    /// </summary>
    /// <param name="isolationLevel">Supported isolation levels are Serializable, ReadCommitted and Unspecified.</param>
    /// <remarks>
    /// Unspecified will use the default isolation level specified in the connection string.  If no isolation level is specified in the
    /// connection string, Serializable is used.
    /// Serializable transactions are the default.  In this mode, the engine gets an immediate lock on the database, and no other threads
    /// may begin a transaction.  Other threads may read from the database, but not write.
    /// With a ReadCommitted isolation level, locks are deferred and elevated as needed.  It is possible for multiple threads to start
    /// a transaction in ReadCommitted mode, but if a thread attempts to commit a transaction while another thread
    /// has a ReadCommitted lock, it may timeout or cause a deadlock on both threads until both threads' CommandTimeout's are reached.
    /// </remarks>
    /// <returns>Returns a SQLiteTransaction object.</returns>
    public new SQLiteTransaction BeginTransaction(IsolationLevel isolationLevel)
    {
      CheckDisposed();
      return (SQLiteTransaction)BeginDbTransaction(isolationLevel);
    }

    /// <summary>
    /// Creates a new <see cref="SQLiteTransaction" /> if one isn't already
    /// active on the connection.
    /// </summary>
    /// <returns>Returns the new transaction object.</returns>
    public new SQLiteTransaction BeginTransaction()
    {
      CheckDisposed();
      return (SQLiteTransaction)BeginDbTransaction(_defaultIsolation);
    }

    /// <summary>
    /// Forwards to the local <see cref="BeginTransaction(IsolationLevel)" /> function
    /// </summary>
    /// <param name="isolationLevel">Supported isolation levels are Unspecified, Serializable, and ReadCommitted</param>
    /// <returns></returns>
    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
      if (_connectionState != ConnectionState.Open)
        throw new InvalidOperationException();

      if (isolationLevel == IsolationLevel.Unspecified) isolationLevel = _defaultIsolation;

      if (isolationLevel != IsolationLevel.Serializable && isolationLevel != IsolationLevel.ReadCommitted)
        throw new ArgumentException("isolationLevel");

      SQLiteTransaction transaction =
          new SQLiteTransaction(this, isolationLevel != IsolationLevel.Serializable);

      OnChanged(this, new ConnectionEventArgs(
          SQLiteConnectionEventType.NewTransaction, null, transaction,
          null, null, null, null, null));

      return transaction;
    }

    /// <summary>
    /// This method is not implemented; however, the <see cref="Changed" />
    /// event will still be raised.
    /// </summary>
    /// <param name="databaseName"></param>
    public override void ChangeDatabase(string databaseName)
    {
      CheckDisposed();

      OnChanged(this, new ConnectionEventArgs(
          SQLiteConnectionEventType.ChangeDatabase, null, null, null, null,
          null, databaseName, null));

      throw new NotImplementedException(); // NOTE: For legacy compatibility.
    }

    /// <summary>
    /// When the database connection is closed, all commands linked to this connection are automatically reset.
    /// </summary>
    public override void Close()
    {
      CheckDisposed();

      OnChanged(this, new ConnectionEventArgs(
          SQLiteConnectionEventType.Closing, null, null, null, null, null,
          null, null));

      if (_sql != null)
      {
#if !PLATFORM_COMPACTFRAMEWORK
        if (_enlistment != null)
        {
          // If the connection is enlisted in a transaction scope and the scope is still active,
          // we cannot truly shut down this connection until the scope has completed.  Therefore make a
          // hidden connection temporarily to hold open the connection until the scope has completed.
          SQLiteConnection cnn = new SQLiteConnection();
          cnn._sql = _sql;
          cnn._transactionLevel = _transactionLevel;
          cnn._enlistment = _enlistment;
          cnn._connectionState = _connectionState;
          cnn._version = _version;

          cnn._enlistment._transaction._cnn = cnn;
          cnn._enlistment._disposeConnection = true;

          _sql = null;
          _enlistment = null;
        }
#endif
        if (_sql != null)
        {
          _sql.Close(!_disposing);
          _sql = null;
        }
        _transactionLevel = 0;
      }

      StateChangeEventArgs eventArgs = null;
      OnStateChange(ConnectionState.Closed, ref eventArgs);

      OnChanged(this, new ConnectionEventArgs(
          SQLiteConnectionEventType.Closed, eventArgs, null, null, null,
          null, null, null));
    }

    /// <summary>
    /// Returns the number of pool entries for the file name associated with this connection.
    /// </summary>
    public int PoolCount
    {
        get
        {
            if (_sql == null) return 0;
            return _sql.CountPool();
        }
    }

    /// <summary>
    /// Clears the connection pool associated with the connection.  Any other active connections using the same database file
    /// will be discarded instead of returned to the pool when they are closed.
    /// </summary>
    /// <param name="connection"></param>
    public static void ClearPool(SQLiteConnection connection)
    {
      if (connection._sql == null) return;
      connection._sql.ClearPool();
    }

    /// <summary>
    /// Clears all connection pools.  Any active connections will be discarded instead of sent to the pool when they are closed.
    /// </summary>
    public static void ClearAllPools()
    {
      SQLiteConnectionPool.ClearAllPools();
    }

    /// <summary>
    /// The connection string containing the parameters for the connection
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <term>Parameter</term>
    /// <term>Values</term>
    /// <term>Required</term>
    /// <term>Default</term>
    /// </listheader>
    /// <item>
    /// <description>Data Source</description>
    /// <description>
    /// This may be a file name, the string ":memory:", or any supported URI (starting with SQLite 3.7.7).
    /// Starting with release 1.0.86.0, in order to use more than one consecutive backslash (e.g. for a
    /// UNC path), each of the adjoining backslash characters must be doubled (e.g. "\\Network\Share\test.db"
    /// would become "\\\\Network\Share\test.db").
    /// </description>
    /// <description>Y</description>
    /// <description></description>
    /// </item>
    /// <item>
    /// <description>Version</description>
    /// <description>3</description>
    /// <description>N</description>
    /// <description>3</description>
    /// </item>
    /// <item>
    /// <description>UseUTF16Encoding</description>
    /// <description><b>True</b><br/><b>False</b></description>
    /// <description>N</description>
    /// <description>False</description>
    /// </item>
    /// <item>
    /// <description>DateTimeFormat</description>
    /// <description>
    /// <b>Ticks</b> - Use the value of DateTime.Ticks.<br/>
    /// <b>ISO8601</b> - Use the ISO-8601 format.  Uses the "yyyy-MM-dd HH:mm:ss.FFFFFFFK" format for UTC
    /// DateTime values and "yyyy-MM-dd HH:mm:ss.FFFFFFF" format for local DateTime values).<br/>
    /// <b>JulianDay</b> - The interval of time in days and fractions of a day since January 1, 4713 BC.<br/>
    /// <b>UnixEpoch</b> - The whole number of seconds since the Unix epoch (January 1, 1970).<br/>
    /// <b>InvariantCulture</b> - Any culture-independent string value that the .NET Framework can interpret as a valid DateTime.<br/>
    /// <b>CurrentCulture</b> - Any string value that the .NET Framework can interpret as a valid DateTime using the current culture.</description>
    /// <description>N</description>
    /// <description>ISO8601</description>
    /// </item>
    /// <item>
    /// <description>DateTimeKind</description>
    /// <description><b>Unspecified</b> - Not specified as either UTC or local time.<br/><b>Utc</b> - The time represented is UTC.<br/><b>Local</b> - The time represented is local time.</description>
    /// <description>N</description>
    /// <description>Unspecified</description>
    /// </item>
    /// <item>
    /// <description>DateTimeFormatString</description>
    /// <description>The exact DateTime format string to use for all formatting and parsing of all DateTime
    /// values for this connection.</description>
    /// <description>N</description>
    /// <description>null</description>
    /// </item>
    /// <item>
    /// <description>BaseSchemaName</description>
    /// <description>Some base data classes in the framework (e.g. those that build SQL queries dynamically)
    /// assume that an ADO.NET provider cannot support an alternate catalog (i.e. database) without supporting
    /// alternate schemas as well; however, SQLite does not fit into this model.  Therefore, this value is used
    /// as a placeholder and removed prior to preparing any SQL statements that may contain it.</description>
    /// <description>N</description>
    /// <description>sqlite_default_schema</description>
    /// </item>
    /// <item>
    /// <description>BinaryGUID</description>
    /// <description><b>True</b> - Store GUID columns in binary form<br/><b>False</b> - Store GUID columns as text</description>
    /// <description>N</description>
    /// <description>True</description>
    /// </item>
    /// <item>
    /// <description>Cache Size</description>
    /// <description>{size in bytes}</description>
    /// <description>N</description>
    /// <description>2000</description>
    /// </item>
    /// <item>
    /// <description>Synchronous</description>
    /// <description><b>Normal</b> - Normal file flushing behavior<br/><b>Full</b> - Full flushing after all writes<br/><b>Off</b> - Underlying OS flushes I/O's</description>
    /// <description>N</description>
    /// <description>Full</description>
    /// </item>
    /// <item>
    /// <description>Page Size</description>
    /// <description>{size in bytes}</description>
    /// <description>N</description>
    /// <description>1024</description>
    /// </item>
    /// <item>
    /// <description>Password</description>
    /// <description>{password} - Using this parameter requires that the CryptoAPI based codec be enabled at compile-time for both the native interop assembly and the core managed assemblies; otherwise, using this parameter may result in an exception being thrown when attempting to open the connection.</description>
    /// <description>N</description>
    /// <description></description>
    /// </item>
    /// <item>
    /// <description>HexPassword</description>
    /// <description>{hexPassword} - Must contain a sequence of zero or more hexadecimal encoded byte values without a leading "0x" prefix.  Using this parameter requires that the CryptoAPI based codec be enabled at compile-time for both the native interop assembly and the core managed assemblies; otherwise, using this parameter may result in an exception being thrown when attempting to open the connection.</description>
    /// <description>N</description>
    /// <description></description>
    /// </item>
    /// <item>
    /// <description>Enlist</description>
    /// <description><b>Y</b> - Automatically enlist in distributed transactions<br/><b>N</b> - No automatic enlistment</description>
    /// <description>N</description>
    /// <description>Y</description>
    /// </item>
    /// <item>
    /// <description>Pooling</description>
    /// <description>
    /// <b>True</b> - Use connection pooling.<br/>
    /// <b>False</b> - Do not use connection pooling.<br/><br/>
    /// <b>WARNING:</b> When using the default connection pool implementation,
    /// setting this property to True should be avoided by applications that
    /// make use of COM (either directly or indirectly) due to possible
    /// deadlocks that can occur during the finalization of some COM objects.
    /// </description>
    /// <description>N</description>
    /// <description>False</description>
    /// </item>
    /// <item>
    /// <description>FailIfMissing</description>
    /// <description><b>True</b> - Don't create the database if it does not exist, throw an error instead<br/><b>False</b> - Automatically create the database if it does not exist</description>
    /// <description>N</description>
    /// <description>False</description>
    /// </item>
    /// <item>
    /// <description>Max Page Count</description>
    /// <description>{size in pages} - Limits the maximum number of pages (limits the size) of the database</description>
    /// <description>N</description>
    /// <description>0</description>
    /// </item>
    /// <item>
    /// <description>Legacy Format</description>
    /// <description><b>True</b> - Use the more compatible legacy 3.x database format<br/><b>False</b> - Use the newer 3.3x database format which compresses numbers more effectively</description>
    /// <description>N</description>
    /// <description>False</description>
    /// </item>
    /// <item>
    /// <description>Default Timeout</description>
    /// <description>{time in seconds}<br/>The default command timeout</description>
    /// <description>N</description>
    /// <description>30</description>
    /// </item>
    /// <item>
    /// <description>Journal Mode</description>
    /// <description><b>Delete</b> - Delete the journal file after a commit<br/><b>Persist</b> - Zero out and leave the journal file on disk after a commit<br/><b>Off</b> - Disable the rollback journal entirely</description>
    /// <description>N</description>
    /// <description>Delete</description>
    /// </item>
    /// <item>
    /// <description>Read Only</description>
    /// <description><b>True</b> - Open the database for read only access<br/><b>False</b> - Open the database for normal read/write access</description>
    /// <description>N</description>
    /// <description>False</description>
    /// </item>
    /// <item>
    /// <description>Max Pool Size</description>
    /// <description>The maximum number of connections for the given connection string that can be in the connection pool</description>
    /// <description>N</description>
    /// <description>100</description>
    /// </item>
    /// <item>
    /// <description>Default IsolationLevel</description>
    /// <description>The default transaciton isolation level</description>
    /// <description>N</description>
    /// <description>Serializable</description>
    /// </item>
    /// <item>
    /// <description>Foreign Keys</description>
    /// <description>Enable foreign key constraints</description>
    /// <description>N</description>
    /// <description>False</description>
    /// </item>
    /// <item>
    /// <description>Flags</description>
    /// <description>Extra behavioral flags for the connection.  See the <see cref="SQLiteConnectionFlags" /> enumeration for possible values.</description>
    /// <description>N</description>
    /// <description>Default</description>
    /// </item>
    /// <item>
    /// <description>SetDefaults</description>
    /// <description>
    /// <b>True</b> - Apply the default connection settings to the opened database.<br/>
    /// <b>False</b> - Skip applying the default connection settings to the opened database.
    /// </description>
    /// <description>N</description>
    /// <description>True</description>
    /// </item>
    /// <item>
    /// <description>ToFullPath</description>
    /// <description>
    /// <b>True</b> - Attempt to expand the data source file name to a fully qualified path before opening.<br/>
    /// <b>False</b> - Skip attempting to expand the data source file name to a fully qualified path before opening.
    /// </description>
    /// <description>N</description>
    /// <description>True</description>
    /// </item>
    /// </list>
    /// </remarks>
#if !PLATFORM_COMPACTFRAMEWORK
    [RefreshProperties(RefreshProperties.All), DefaultValue("")]
    [Editor("SQLite.Designer.SQLiteConnectionStringEditor, SQLite.Designer, Version=" + SQLite3.DesignerVersion + ", Culture=neutral, PublicKeyToken=db937bc2d44ff139", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
#endif
    public override string ConnectionString
    {
      get
      {
        CheckDisposed();
        return _connectionString;
      }
      set
      {
        CheckDisposed();

        if (value == null)
          throw new ArgumentNullException();

        else if (_connectionState != ConnectionState.Closed)
          throw new InvalidOperationException();

        _connectionString = value;
      }
    }

    /// <summary>
    /// Create a new <see cref="SQLiteCommand" /> and associate it with this connection.
    /// </summary>
    /// <returns>Returns a new command object already assigned to this connection.</returns>
    public new SQLiteCommand CreateCommand()
    {
      CheckDisposed();
      return new SQLiteCommand(this);
    }

    /// <summary>
    /// Forwards to the local <see cref="CreateCommand" /> function.
    /// </summary>
    /// <returns></returns>
    protected override DbCommand CreateDbCommand()
    {
      return CreateCommand();
    }

    /// <summary>
    /// Returns the data source file name without extension or path.
    /// </summary>
#if !PLATFORM_COMPACTFRAMEWORK
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
#endif
    public override string DataSource
    {
      get
      {
        CheckDisposed();
        return _dataSource;
      }
    }

    /// <summary>
    /// Returns the string "main".
    /// </summary>
#if !PLATFORM_COMPACTFRAMEWORK
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
#endif
    public override string Database
    {
      get
      {
        CheckDisposed();
        return "main";
      }
    }

    internal static string MapUriPath(string path)
    {
        if (path.StartsWith ("file://", StringComparison.OrdinalIgnoreCase))
            return path.Substring (7);
      else if (path.StartsWith ("file:", StringComparison.OrdinalIgnoreCase))
            return path.Substring (5);
      else if (path.StartsWith ("/", StringComparison.OrdinalIgnoreCase))
            return path;
      else
            throw new InvalidOperationException ("Invalid connection string: invalid URI");
    }

    /// <summary>
    /// Parses the connection string into component parts using the custom
    /// connection string parser.
    /// </summary>
    /// <param name="connectionString">The connection string to parse</param>
    /// <returns>An array of key-value pairs representing each parameter of the connection string</returns>
    internal static SortedList<string, string> ParseConnectionString(string connectionString)
    {
      string s = connectionString;
      int n;
      SortedList<string, string> ls = new SortedList<string, string>(StringComparer.OrdinalIgnoreCase);

      // First split into semi-colon delimited values.
      string error = null;
      string[] arParts;

#if !PLATFORM_COMPACTFRAMEWORK
      if (Environment.GetEnvironmentVariable("No_SQLiteConnectionNewParser") != null)
          arParts = SQLiteConvert.Split(s, ';');
      else
#endif
          arParts = SQLiteConvert.NewSplit(s, ';', true, ref error);

      if (arParts == null)
      {
          throw new ArgumentException(String.Format(CultureInfo.CurrentCulture,
              "Invalid ConnectionString format, cannot parse: {0}", (error != null) ?
              error : "could not split connection string into properties"));
      }

      int x = (arParts != null) ? arParts.Length : 0;
      // For each semi-colon piece, split into key and value pairs by the presence of the = sign
      for (n = 0; n < x; n++)
      {
        if (arParts[n] == null)
          continue;

        arParts[n] = arParts[n].Trim();

        if (arParts[n].Length == 0)
          continue;

        int indexOf = arParts[n].IndexOf('=');

        if (indexOf != -1)
          ls.Add(UnwrapString(arParts[n].Substring(0, indexOf).Trim()), UnwrapString(arParts[n].Substring(indexOf + 1).Trim()));
        else
          throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, "Invalid ConnectionString format for part \"{0}\", no equal sign found", arParts[n]));
      }
      return ls;
    }

    /// <summary>
    /// Parses a connection string using the built-in (i.e. framework provided)
    /// connection string parser class and returns the key/value pairs.  An
    /// exception may be thrown if the connection string is invalid or cannot be
    /// parsed.  When compiled for the .NET Compact Framework, the custom
    /// connection string parser is always used instead because the framework
    /// provided one is unavailable there.
    /// </summary>
    /// <param name="connectionString">
    /// The connection string to parse.
    /// </param>
    /// <param name="strict">
    /// Non-zero to throw an exception if any connection string values are not of
    /// the <see cref="String" /> type.
    /// </param>
    /// <returns>The list of key/value pairs.</returns>
    internal static SortedList<string, string> ParseConnectionStringViaFramework(
        string connectionString,
        bool strict
        )
    {
#if !PLATFORM_COMPACTFRAMEWORK
        DbConnectionStringBuilder connectionStringBuilder
            = new DbConnectionStringBuilder();

        connectionStringBuilder.ConnectionString = connectionString; /* throw */

        SortedList<string, string> result =
            new SortedList<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string keyName in connectionStringBuilder.Keys)
        {
            object value = connectionStringBuilder[keyName];
            string keyValue = null;

            if (value is string)
            {
                keyValue = (string)value;
            }
            else if (strict)
            {
                throw new ArgumentException(
                    "connection property value is not a string",
                    keyName);
            }
            else if (value != null)
            {
                keyValue = value.ToString();
            }

            result.Add(keyName, keyValue);
        }

        return result;
#else
        //
        // NOTE: On the .NET Compact Framework, always use our custom connection
        //       string parser as the built-in (i.e. framework provided) one is
        //       unavailable.
        //
        return ParseConnectionString(connectionString);
#endif
    }

#if !PLATFORM_COMPACTFRAMEWORK
    /// <summary>
    /// Manual distributed transaction enlistment support
    /// </summary>
    /// <param name="transaction">The distributed transaction to enlist in</param>
    public override void EnlistTransaction(System.Transactions.Transaction transaction)
    {
      CheckDisposed();

      if (_enlistment != null && transaction == _enlistment._scope)
        return;
      else if (_enlistment != null)
        throw new ArgumentException("Already enlisted in a transaction");

      if (_transactionLevel > 0 && transaction != null)
        throw new ArgumentException("Unable to enlist in transaction, a local transaction already exists");
      else if (transaction == null)
        throw new ArgumentNullException("Unable to enlist in transaction, it is null");

      _enlistment = new SQLiteEnlistment(this, transaction);

      OnChanged(this, new ConnectionEventArgs(
          SQLiteConnectionEventType.EnlistTransaction, null, null, null, null,
          null, null, new object[] { _enlistment }));
    }
#endif

    /// <summary>
    /// Looks for a key in the array of key/values of the parameter string.  If not found, return the specified default value
    /// </summary>
    /// <param name="items">The list to look in</param>
    /// <param name="key">The key to find</param>
    /// <param name="defValue">The default value to return if the key is not found</param>
    /// <returns>The value corresponding to the specified key, or the default value if not found.</returns>
    static internal string FindKey(SortedList<string, string> items, string key, string defValue)
    {
      string ret;

      if (items.TryGetValue(key, out ret)) return ret;

      return defValue;
    }

    /// <summary>
    /// Attempts to convert the string value to an enumerated value of the specified type.
    /// </summary>
    /// <param name="type">The enumerated type to convert the string value to.</param>
    /// <param name="value">The string value to be converted.</param>
    /// <param name="ignoreCase">Non-zero to make the conversion case-insensitive.</param>
    /// <returns>The enumerated value upon success or null upon error.</returns>
    private static object TryParseEnum(
        Type type,
        string value,
        bool ignoreCase
        )
    {
        try
        {
            return Enum.Parse(type, value, ignoreCase);
        }
        catch
        {
            // do nothing.
        }

        return null;
    }

    /// <summary>
    /// Attempts to convert an input string into a byte value.
    /// </summary>
    /// <param name="value">
    /// The string value to be converted.
    /// </param>
    /// <param name="style">
    /// The number styles to use for the conversion.
    /// </param>
    /// <param name="result">
    /// Upon sucess, this will contain the parsed byte value.
    /// Upon failure, the value of this parameter is undefined.
    /// </param>
    /// <returns>
    /// Non-zero upon success; zero on failure.
    /// </returns>
    private static bool TryParseByte(
        string value,
        NumberStyles style,
        out byte result
        )
    {
#if !PLATFORM_COMPACTFRAMEWORK
        return byte.TryParse(value, style, null, out result);
#else
        try
        {
            result = byte.Parse(value, style);
            return true;
        }
        catch
        {
            result = 0;
            return false;
        }
#endif
    }

    /// <summary>
    /// Enables or disabled extension loading.
    /// </summary>
    /// <param name="enable">
    /// True to enable loading of extensions, false to disable.
    /// </param>
    public void EnableExtensions(
        bool enable
        )
    {
        CheckDisposed();

        if (_sql == null)
            throw new InvalidOperationException(String.Format(
                CultureInfo.CurrentCulture,
                "Database connection not valid for {0} extensions.",
                enable ? "enabling" : "disabling"));

        if ((_flags & SQLiteConnectionFlags.NoLoadExtension) == SQLiteConnectionFlags.NoLoadExtension)
            throw new SQLiteException("Loading extensions is disabled for this database connection.");

        _sql.SetLoadExtension(enable);
    }

    /// <summary>
    /// Loads a SQLite extension library from the named dynamic link library file.
    /// </summary>
    /// <param name="fileName">
    /// The name of the dynamic link library file containing the extension.
    /// </param>
    public void LoadExtension(
        string fileName
        )
    {
        CheckDisposed();

        LoadExtension(fileName, null);
    }

    /// <summary>
    /// Loads a SQLite extension library from the named dynamic link library file.
    /// </summary>
    /// <param name="fileName">
    /// The name of the dynamic link library file containing the extension.
    /// </param>
    /// <param name="procName">
    /// The name of the exported function used to initialize the extension.
    /// If null, the default "sqlite3_extension_init" will be used.
    /// </param>
    public void LoadExtension(
        string fileName,
        string procName
        )
    {
        CheckDisposed();

        if (_sql == null)
            throw new InvalidOperationException(
                "Database connection not valid for loading extensions.");

        if ((_flags & SQLiteConnectionFlags.NoLoadExtension) == SQLiteConnectionFlags.NoLoadExtension)
            throw new SQLiteException("Loading extensions is disabled for this database connection.");

        _sql.LoadExtension(fileName, procName);
    }

#if INTEROP_VIRTUAL_TABLE
    /// <summary>
    /// Creates a disposable module containing the implementation of a virtual
    /// table.
    /// </summary>
    /// <param name="module">
    /// The module object to be used when creating the disposable module.
    /// </param>
    public void CreateModule(
        SQLiteModule module
        )
    {
        CheckDisposed();

        if (_sql == null)
            throw new InvalidOperationException(
                "Database connection not valid for creating modules.");

        if ((_flags & SQLiteConnectionFlags.NoCreateModule) == SQLiteConnectionFlags.NoCreateModule)
            throw new SQLiteException("Creating modules is disabled for this database connection.");

        _sql.CreateModule(module, _flags);
    }
#endif

    /// <summary>
    /// Parses a string containing a sequence of zero or more hexadecimal
    /// encoded byte values and returns the resulting byte array.  The
    /// "0x" prefix is not allowed on the input string.
    /// </summary>
    /// <param name="text">
    /// The input string containing zero or more hexadecimal encoded byte
    /// values.
    /// </param>
    /// <returns>
    /// A byte array containing the parsed byte values or null if an error
    /// was encountered.
    /// </returns>
    internal static byte[] FromHexString(
        string text
        )
    {
        string error = null;

        return FromHexString(text, ref error);
    }

    /// <summary>
    /// Creates and returns a string containing the hexadecimal encoded byte
    /// values from the input array.
    /// </summary>
    /// <param name="array">
    /// The input array of bytes.
    /// </param>
    /// <returns>
    /// The resulting string or null upon failure.
    /// </returns>
    internal static string ToHexString(
        byte[] array
        )
    {
        if (array == null)
            return null;

        StringBuilder result = new StringBuilder();

        int length = array.Length;

        for (int index = 0; index < length; index++)
#if NET_COMPACT_20
            result.Append(String.Format("{0:x2}", array[index]));
#else
            result.AppendFormat("{0:x2}", array[index]);
#endif

        return result.ToString();
    }

    /// <summary>
    /// Parses a string containing a sequence of zero or more hexadecimal
    /// encoded byte values and returns the resulting byte array.  The
    /// "0x" prefix is not allowed on the input string.
    /// </summary>
    /// <param name="text">
    /// The input string containing zero or more hexadecimal encoded byte
    /// values.
    /// </param>
    /// <param name="error">
    /// Upon failure, this will contain an appropriate error message.
    /// </param>
    /// <returns>
    /// A byte array containing the parsed byte values or null if an error
    /// was encountered.
    /// </returns>
    private static byte[] FromHexString(
        string text,
        ref string error
        )
    {
        if (String.IsNullOrEmpty(text))
        {
            error = "string is null or empty";
            return null;
        }

        if (text.Length % 2 != 0)
        {
            error = "string contains an odd number of characters";
            return null;
        }

        byte[] result = new byte[text.Length / 2];

        for (int index = 0; index < text.Length; index += 2)
        {
            string value = text.Substring(index, 2);

            if (!TryParseByte(value,
                    NumberStyles.HexNumber, out result[index / 2]))
            {
                error = String.Format(
                    CultureInfo.CurrentCulture,
                    "string contains \"{0}\", which cannot be converted to a byte value",
                    value);

                return null;
            }
        }

        return result;
    }

    /// <summary>
    /// Opens the connection using the parameters found in the <see cref="ConnectionString" />.
    /// </summary>
    public override void Open()
    {
      CheckDisposed();

      OnChanged(this, new ConnectionEventArgs(
          SQLiteConnectionEventType.Opening, null, null, null, null, null,
          null, null));

      if (_connectionState != ConnectionState.Closed)
        throw new InvalidOperationException();

      Close();

      SortedList<string, string> opts = ParseConnectionString(
          _connectionString, _parseViaFramework);

      OnChanged(this, new ConnectionEventArgs(
          SQLiteConnectionEventType.ConnectionString, null, null, null, null,
          null, _connectionString, new object[] { opts }));

      object enumValue;

      enumValue = TryParseEnum(typeof(SQLiteConnectionFlags), FindKey(opts, "Flags", DefaultFlags.ToString()), true);
      _flags = (enumValue is SQLiteConnectionFlags) ? (SQLiteConnectionFlags)enumValue : DefaultFlags;

      bool fullUri = false;
      string fileName;

      if (Convert.ToInt32(FindKey(opts, "Version", DefaultVersion.ToString()), CultureInfo.InvariantCulture) != DefaultVersion)
        throw new NotSupportedException(String.Format(CultureInfo.CurrentCulture, "Only SQLite Version {0} is supported at this time", DefaultVersion));

      fileName = FindKey(opts, "Data Source", DefaultDataSource);

      if (String.IsNullOrEmpty(fileName))
      {
        fileName = FindKey(opts, "Uri", DefaultUri);
        if (String.IsNullOrEmpty(fileName))
        {
          fileName = FindKey(opts, "FullUri", DefaultFullUri);
          if (String.IsNullOrEmpty(fileName))
            throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, "Data Source cannot be empty.  Use {0} to open an in-memory database", MemoryFileName));
          else
            fullUri = true;
        }
        else
          fileName = MapUriPath(fileName);
      }

      bool isMemory = (String.Compare(fileName, MemoryFileName, StringComparison.OrdinalIgnoreCase) == 0);

      if (!fullUri)
      {
        if (isMemory)
          fileName = MemoryFileName;
        else
        {
#if PLATFORM_COMPACTFRAMEWORK
          if (fileName.StartsWith("./") || fileName.StartsWith(".\\"))
            fileName = Path.GetDirectoryName(System.Reflection.Assembly.GetCallingAssembly().GetName().CodeBase) + fileName.Substring(1);
#endif
          bool toFullPath = SQLiteConvert.ToBoolean(FindKey(opts, "ToFullPath", DefaultToFullPath.ToString()));
          fileName = ExpandFileName(fileName, toFullPath);
        }
      }

      try
      {
        bool usePooling = SQLiteConvert.ToBoolean(FindKey(opts, "Pooling", DefaultPooling.ToString()));
        int maxPoolSize = Convert.ToInt32(FindKey(opts, "Max Pool Size", DefaultMaxPoolSize.ToString()), CultureInfo.InvariantCulture);

        _defaultTimeout = Convert.ToInt32(FindKey(opts, "Default Timeout", DefaultConnectionTimeout.ToString()), CultureInfo.InvariantCulture);

        enumValue = TryParseEnum(typeof(IsolationLevel), FindKey(opts, "Default IsolationLevel", DefaultIsolationLevel.ToString()), true);
        _defaultIsolation = (enumValue is IsolationLevel) ? (IsolationLevel)enumValue : DefaultIsolationLevel;

        if (_defaultIsolation != IsolationLevel.Serializable && _defaultIsolation != IsolationLevel.ReadCommitted)
          throw new NotSupportedException("Invalid Default IsolationLevel specified");

        _baseSchemaName = FindKey(opts, "BaseSchemaName", DefaultBaseSchemaName);

        if (_sql == null)
        {
            SetupSQLiteBase(opts);
        }

        SQLiteOpenFlagsEnum flags = SQLiteOpenFlagsEnum.None;

        if (!SQLiteConvert.ToBoolean(FindKey(opts, "FailIfMissing", DefaultFailIfMissing.ToString())))
          flags |= SQLiteOpenFlagsEnum.Create;

        if (SQLiteConvert.ToBoolean(FindKey(opts, "Read Only", DefaultReadOnly.ToString())))
        {
          flags |= SQLiteOpenFlagsEnum.ReadOnly;
          // SQLite will return SQLITE_MISUSE on ReadOnly and Create
          flags &= ~SQLiteOpenFlagsEnum.Create;
        }
        else
        {
          flags |= SQLiteOpenFlagsEnum.ReadWrite;
        }

        if (fullUri)
            flags |= SQLiteOpenFlagsEnum.Uri;

        _sql.Open(fileName, _flags, flags, maxPoolSize, usePooling);

        _binaryGuid = SQLiteConvert.ToBoolean(FindKey(opts, "BinaryGUID", DefaultBinaryGUID.ToString()));

#if INTEROP_CODEC
        string hexPassword = FindKey(opts, "HexPassword", DefaultHexPassword);

        if (!String.IsNullOrEmpty(hexPassword))
        {
            string error = null;
            byte[] hexPasswordBytes = FromHexString(hexPassword, ref error);

            if (hexPasswordBytes == null)
            {
                throw new FormatException(String.Format(
                    CultureInfo.CurrentCulture,
                    "Cannot parse 'HexPassword' property value into byte values: {0}",
                    error));
            }

            _sql.SetPassword(hexPasswordBytes);
        }
        else
        {
            string password = FindKey(opts, "Password", DefaultPassword);

            if (!String.IsNullOrEmpty(password))
                _sql.SetPassword(UTF8Encoding.UTF8.GetBytes(password));
            else if (_password != null)
                _sql.SetPassword(_password);
        }
        _password = null;
#endif

        if (!fullUri)
          _dataSource = Path.GetFileNameWithoutExtension(fileName);
        else
          _dataSource = fileName;

        _version++;

        ConnectionState oldstate = _connectionState;
        _connectionState = ConnectionState.Open;

        try
        {
          string strValue;
          bool boolValue;

          strValue = FindKey(opts, "SetDefaults", DefaultSetDefaults.ToString());
          boolValue = SQLiteConvert.ToBoolean(strValue);

          if (boolValue)
          {
              using (SQLiteCommand cmd = CreateCommand())
              {
                  int intValue;

                  if (!fullUri && !isMemory)
                  {
                      strValue = FindKey(opts, "Page Size", DefaultPageSize.ToString());
                      intValue = Convert.ToInt32(strValue, CultureInfo.InvariantCulture);
                      if (intValue != DefaultPageSize)
                      {
                          cmd.CommandText = String.Format(CultureInfo.InvariantCulture, "PRAGMA page_size={0}", intValue);
                          cmd.ExecuteNonQuery();
                      }
                  }

                  strValue = FindKey(opts, "Max Page Count", DefaultMaxPageCount.ToString());
                  intValue = Convert.ToInt32(strValue, CultureInfo.InvariantCulture);
                  if (intValue != DefaultMaxPageCount)
                  {
                      cmd.CommandText = String.Format(CultureInfo.InvariantCulture, "PRAGMA max_page_count={0}", intValue);
                      cmd.ExecuteNonQuery();
                  }

                  strValue = FindKey(opts, "Legacy Format", DefaultLegacyFormat.ToString());
                  boolValue = SQLiteConvert.ToBoolean(strValue);
                  if (boolValue != DefaultLegacyFormat)
                  {
                      cmd.CommandText = String.Format(CultureInfo.InvariantCulture, "PRAGMA legacy_file_format={0}", boolValue ? "ON" : "OFF");
                      cmd.ExecuteNonQuery();
                  }

                  strValue = FindKey(opts, "Synchronous", DefaultSynchronous.ToString());
                  enumValue = TryParseEnum(typeof(SQLiteSynchronousEnum), strValue, true);
                  if (!(enumValue is SQLiteSynchronousEnum) || ((SQLiteSynchronousEnum)enumValue != DefaultSynchronous))
                  {
                      cmd.CommandText = String.Format(CultureInfo.InvariantCulture, "PRAGMA synchronous={0}", strValue);
                      cmd.ExecuteNonQuery();
                  }

                  strValue = FindKey(opts, "Cache Size", DefaultCacheSize.ToString());
                  intValue = Convert.ToInt32(strValue, CultureInfo.InvariantCulture);
                  if (intValue != DefaultCacheSize)
                  {
                      cmd.CommandText = String.Format(CultureInfo.InvariantCulture, "PRAGMA cache_size={0}", intValue);
                      cmd.ExecuteNonQuery();
                  }

                  strValue = FindKey(opts, "Journal Mode", DefaultJournalMode.ToString());
                  enumValue = TryParseEnum(typeof(SQLiteJournalModeEnum), strValue, true);
                  if (!(enumValue is SQLiteJournalModeEnum) || ((SQLiteJournalModeEnum)enumValue != DefaultJournalMode))
                  {
                      cmd.CommandText = String.Format(CultureInfo.InvariantCulture, "PRAGMA journal_mode={0}", strValue);
                      cmd.ExecuteNonQuery();
                  }

                  strValue = FindKey(opts, "Foreign Keys", DefaultForeignKeys.ToString());
                  boolValue = Convert.ToBoolean(strValue, CultureInfo.InvariantCulture);
                  if (boolValue != DefaultForeignKeys)
                  {
                      cmd.CommandText = String.Format(CultureInfo.InvariantCulture, "PRAGMA foreign_keys={0}", boolValue ? "ON" : "OFF");
                      cmd.ExecuteNonQuery();
                  }
              }
          }

          if (_commitHandler != null)
            _sql.SetCommitHook(_commitCallback);

          if (_updateHandler != null)
            _sql.SetUpdateHook(_updateCallback);

          if (_rollbackHandler != null)
            _sql.SetRollbackHook(_rollbackCallback);

#if !PLATFORM_COMPACTFRAMEWORK
          System.Transactions.Transaction transaction = Transactions.Transaction.Current;

          if (transaction != null &&
              SQLiteConvert.ToBoolean(FindKey(opts, "Enlist", DefaultEnlist.ToString())))
          {
              EnlistTransaction(transaction);
          }
#endif

          _connectionState = oldstate;

          StateChangeEventArgs eventArgs = null;
          OnStateChange(ConnectionState.Open, ref eventArgs);

          OnChanged(this, new ConnectionEventArgs(
              SQLiteConnectionEventType.Opened, eventArgs, null, null, null,
              null, null, null));
        }
        catch
        {
          _connectionState = oldstate;
          throw;
        }
      }
      catch (SQLiteException)
      {
        Close();
        throw;
      }
    }

    /// <summary>
    /// Opens the connection using the parameters found in the <see cref="ConnectionString" /> and then returns it.
    /// </summary>
    /// <returns>The current connection object.</returns>
    public SQLiteConnection OpenAndReturn()
    {
        CheckDisposed(); Open(); return this;
    }

    /// <summary>
    /// Gets/sets the default command timeout for newly-created commands.  This is especially useful for
    /// commands used internally such as inside a SQLiteTransaction, where setting the timeout is not possible.
    /// This can also be set in the ConnectionString with "Default Timeout"
    /// </summary>
    public int DefaultTimeout
    {
      get { CheckDisposed(); return _defaultTimeout; }
      set { CheckDisposed(); _defaultTimeout = value; }
    }

    /// <summary>
    /// Non-zero if the built-in (i.e. framework provided) connection string
    /// parser should be used when opening the connection.
    /// </summary>
    public bool ParseViaFramework
    {
        get { CheckDisposed(); return _parseViaFramework; }
        set { CheckDisposed(); _parseViaFramework = value; }
    }

    /// <summary>
    /// Gets/sets the extra behavioral flags for this connection.  See the
    /// <see cref="SQLiteConnectionFlags" /> enumeration for a list of
    /// possible values.
    /// </summary>
    public SQLiteConnectionFlags Flags
    {
      get { CheckDisposed(); return _flags; }
      set { CheckDisposed(); _flags = value; }
    }

    /// <summary>
    /// Returns non-zero if the underlying native connection handle is
    /// owned by this instance.
    /// </summary>
    public bool OwnHandle
    {
        get
        {
            CheckDisposed();

            if (_sql == null)
                throw new InvalidOperationException("Database connection not valid for checking handle.");

            return _sql.OwnHandle;
        }
    }

    /// <summary>
    /// Returns the version of the underlying SQLite database engine
    /// </summary>
#if !PLATFORM_COMPACTFRAMEWORK
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
#endif
    public override string ServerVersion
    {
      get
      {
        CheckDisposed();
        return SQLiteVersion;
        //if (_connectionState != ConnectionState.Open)
        //  throw new InvalidOperationException();

        //return _sql.Version;
      }
    }

    /// <summary>
    /// Returns the rowid of the most recent successful INSERT into the database from this connection.
    /// </summary>
#if !PLATFORM_COMPACTFRAMEWORK
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
#endif
    public long LastInsertRowId
    {
      get
      {
        CheckDisposed();

        if (_sql == null)
          throw new InvalidOperationException("Database connection not valid for getting last insert rowid.");

        return _sql.LastInsertRowId;
      }
    }

    /// <summary>
    /// This method causes any pending database operation to abort and return at
    /// its earliest opportunity.  This routine is typically called in response
    /// to a user action such as pressing "Cancel" or Ctrl-C where the user wants
    /// a long query operation to halt immediately.  It is safe to call this
    /// routine from any thread.  However, it is not safe to call this routine
    /// with a database connection that is closed or might close before this method
    /// returns.
    /// </summary>
    public void Cancel()
    {
        CheckDisposed();

        if (_sql == null)
            throw new InvalidOperationException("Database connection not valid for query cancellation.");

        _sql.Cancel(); /* throw */
    }

    /// <summary>
    /// Returns the number of rows changed by the last INSERT, UPDATE, or DELETE statement executed on
    /// this connection.
    /// </summary>
#if !PLATFORM_COMPACTFRAMEWORK
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
#endif
    public int Changes
    {
      get
      {
        CheckDisposed();

        if (_sql == null)
          throw new InvalidOperationException("Database connection not valid for getting number of changes.");

        return _sql.Changes;
      }
    }

    /// <summary>
    /// Returns the amount of memory (in bytes) currently in use by the SQLite core library.
    /// </summary>
#if !PLATFORM_COMPACTFRAMEWORK
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
#endif
    public long MemoryUsed
    {
      get
      {
        CheckDisposed();

        if (_sql == null)
          throw new InvalidOperationException("Database connection not valid for getting memory used.");

        return _sql.MemoryUsed;
      }
    }

    /// <summary>
    /// Returns the maximum amount of memory (in bytes) used by the SQLite core library since the high-water mark was last reset.
    /// </summary>
#if !PLATFORM_COMPACTFRAMEWORK
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
#endif
    public long MemoryHighwater
    {
      get
      {
        CheckDisposed();

        if (_sql == null)
          throw new InvalidOperationException("Database connection not valid for getting maximum memory used.");

          return _sql.MemoryHighwater;
      }
    }

    /// <summary>
    /// Sets the status of the memory usage tracking subsystem in the SQLite core library.  By default, this is enabled.
    /// If this is disabled, memory usage tracking will not be performed.  This is not really a per-connection value, it is
    /// global to the process.
    /// </summary>
    /// <param name="value">Non-zero to enable memory usage tracking, zero otherwise.</param>
    /// <returns>A standard SQLite return code (i.e. zero for success and non-zero for failure).</returns>
    public static SQLiteErrorCode SetMemoryStatus(bool value)
    {
        return SQLite3.StaticSetMemoryStatus(value);
    }

    /// <summary>
    /// Returns a string containing the define constants (i.e. compile-time
    /// options) used to compile the core managed assembly, delimited with
    /// spaces.
    /// </summary>
    public static string DefineConstants
    {
        get { return SQLite3.DefineConstants; }
    }

    /// <summary>
    /// Returns the version of the underlying SQLite core library.
    /// </summary>
    public static string SQLiteVersion
    {
      get { return SQLite3.SQLiteVersion; }
    }

    /// <summary>
    /// This method returns the string whose value is the same as the
    /// SQLITE_SOURCE_ID C preprocessor macro used when compiling the
    /// SQLite core library.
    /// </summary>
    public static string SQLiteSourceId
    {
      get { return SQLite3.SQLiteSourceId; }
    }

    /// <summary>
    /// This method returns the version of the interop SQLite assembly
    /// used.  If the SQLite interop assembly is not in use or the
    /// necessary information cannot be obtained for any reason, a null
    /// value may be returned.
    /// </summary>
    public static string InteropVersion
    {
      get { return SQLite3.InteropVersion; }
    }

    /// <summary>
    /// This method returns the string whose value contains the unique
    /// identifier for the source checkout used to build the interop
    /// assembly.  If the SQLite interop assembly is not in use or the
    /// necessary information cannot be obtained for any reason, a null
    /// value may be returned.
    /// </summary>
    public static string InteropSourceId
    {
      get { return SQLite3.InteropSourceId; }
    }

    /// <summary>
    /// This method returns the version of the managed components used
    /// to interact with the SQLite core library.  If the necessary
    /// information cannot be obtained for any reason, a null value may
    /// be returned.
    /// </summary>
    public static string ProviderVersion
    {
        get
        {
            return (_assembly != null) ?
                _assembly.GetName().Version.ToString() : null;
        }
    }

    /// <summary>
    /// This method returns the string whose value contains the unique
    /// identifier for the source checkout used to build the managed
    /// components currently executing.  If the necessary information
    /// cannot be obtained for any reason, a null value may be returned.
    /// </summary>
    public static string ProviderSourceId
    {
        get
        {
            if (_assembly == null)
                return null;

            string sourceId = null;

            if (_assembly.IsDefined(typeof(AssemblySourceIdAttribute), false))
            {
                AssemblySourceIdAttribute attribute =
                    (AssemblySourceIdAttribute)_assembly.GetCustomAttributes(
                        typeof(AssemblySourceIdAttribute), false)[0];

                sourceId = attribute.SourceId;
            }

            string sourceTimeStamp = null;

            if (_assembly.IsDefined(typeof(AssemblySourceTimeStampAttribute), false))
            {
                AssemblySourceTimeStampAttribute attribute =
                    (AssemblySourceTimeStampAttribute)_assembly.GetCustomAttributes(
                        typeof(AssemblySourceTimeStampAttribute), false)[0];

                sourceTimeStamp = attribute.SourceTimeStamp;
            }

            if ((sourceId != null) || (sourceTimeStamp != null))
            {
                if (sourceId == null)
                    sourceId = "0000000000000000000000000000000000000000";

                if (sourceTimeStamp == null)
                    sourceTimeStamp = "0000-00-00 00:00:00 UTC";

                return String.Format("{0} {1}", sourceId, sourceTimeStamp);
            }
            else
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Returns the state of the connection.
    /// </summary>
#if !PLATFORM_COMPACTFRAMEWORK
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
#endif
    public override ConnectionState State
    {
      get
      {
        CheckDisposed();
        return _connectionState;
      }
    }

    /// Passes a shutdown request off to SQLite.
    public SQLiteErrorCode Shutdown()
    {
        CheckDisposed();

        // make sure we have an instance of the base class
        if (_sql == null)
        {
            SortedList<string, string> opts = ParseConnectionString(
                _connectionString, _parseViaFramework);

            SetupSQLiteBase(opts);
        }
        if (_sql != null) return _sql.Shutdown();
        throw new InvalidOperationException("Database connection not active.");
    }

    /// Enables or disabled extended result codes returned by SQLite
    public void SetExtendedResultCodes(bool bOnOff)
    {
      CheckDisposed();

      if (_sql != null) _sql.SetExtendedResultCodes(bOnOff);
    }
    /// Enables or disabled extended result codes returned by SQLite
    public SQLiteErrorCode ResultCode()
    {
      CheckDisposed();

      if (_sql == null)
        throw new InvalidOperationException("Database connection not valid for getting result code.");
      return _sql.ResultCode();
    }
    /// Enables or disabled extended result codes returned by SQLite
    public SQLiteErrorCode ExtendedResultCode()
    {
      CheckDisposed();

      if (_sql == null)
        throw new InvalidOperationException("Database connection not valid for getting extended result code.");
      return _sql.ExtendedResultCode();
    }

    /// Add a log message via the SQLite sqlite3_log interface.
    public void LogMessage(SQLiteErrorCode iErrCode, string zMessage)
    {
      CheckDisposed();

      if (_sql == null)
          throw new InvalidOperationException("Database connection not valid for logging message.");

      _sql.LogMessage(iErrCode, zMessage);
    }

    /// Add a log message via the SQLite sqlite3_log interface.
    public void LogMessage(int iErrCode, string zMessage)
    {
      CheckDisposed();

      if (_sql == null)
          throw new InvalidOperationException("Database connection not valid for logging message.");

      _sql.LogMessage((SQLiteErrorCode)iErrCode, zMessage);
    }

#if INTEROP_CODEC
    /// <summary>
    /// Change the password (or assign a password) to an open database.
    /// </summary>
    /// <remarks>
    /// No readers or writers may be active for this process.  The database must already be open
    /// and if it already was password protected, the existing password must already have been supplied.
    /// </remarks>
    /// <param name="newPassword">The new password to assign to the database</param>
    public void ChangePassword(string newPassword)
    {
      CheckDisposed();

      ChangePassword(String.IsNullOrEmpty(newPassword) ? null : UTF8Encoding.UTF8.GetBytes(newPassword));
    }

    /// <summary>
    /// Change the password (or assign a password) to an open database.
    /// </summary>
    /// <remarks>
    /// No readers or writers may be active for this process.  The database must already be open
    /// and if it already was password protected, the existing password must already have been supplied.
    /// </remarks>
    /// <param name="newPassword">The new password to assign to the database</param>
    public void ChangePassword(byte[] newPassword)
    {
      CheckDisposed();

      if (_connectionState != ConnectionState.Open)
        throw new InvalidOperationException("Database must be opened before changing the password.");

      _sql.ChangePassword(newPassword);
    }

    /// <summary>
    /// Sets the password for a password-protected database.  A password-protected database is
    /// unusable for any operation until the password has been set.
    /// </summary>
    /// <param name="databasePassword">The password for the database</param>
    public void SetPassword(string databasePassword)
    {
      CheckDisposed();

      SetPassword(String.IsNullOrEmpty(databasePassword) ? null : UTF8Encoding.UTF8.GetBytes(databasePassword));
    }

    /// <summary>
    /// Sets the password for a password-protected database.  A password-protected database is
    /// unusable for any operation until the password has been set.
    /// </summary>
    /// <param name="databasePassword">The password for the database</param>
    public void SetPassword(byte[] databasePassword)
    {
      CheckDisposed();

      if (_connectionState != ConnectionState.Closed)
        throw new InvalidOperationException("Password can only be set before the database is opened.");

      if (databasePassword != null)
        if (databasePassword.Length == 0) databasePassword = null;

      _password = databasePassword;
    }
#endif

    /// <summary>
    /// Queries or modifies the number of retries or the retry interval (in milliseconds) for
    /// certain I/O operations that may fail due to anti-virus software.
    /// </summary>
    /// <param name="count">The number of times to retry the I/O operation.  A negative value
    /// will cause the current count to be queried and replace that negative value.</param>
    /// <param name="interval">The number of milliseconds to wait before retrying the I/O
    /// operation.  This number is multiplied by the number of retry attempts so far to come
    /// up with the final number of milliseconds to wait.  A negative value will cause the
    /// current interval to be queried and replace that negative value.</param>
    /// <returns>Zero for success, non-zero for error.</returns>
    public SQLiteErrorCode SetAvRetry(ref int count, ref int interval)
    {
        CheckDisposed();

        if (_connectionState != ConnectionState.Open)
            throw new InvalidOperationException(
                "Database must be opened before changing the AV retry parameters.");

        SQLiteErrorCode rc;
        IntPtr pArg = IntPtr.Zero;

        try
        {
            pArg = Marshal.AllocHGlobal(sizeof(int) * 2);

            Marshal.WriteInt32(pArg, 0, count);
            Marshal.WriteInt32(pArg, sizeof(int), interval);

            rc = _sql.FileControl(null, SQLITE_FCNTL_WIN32_AV_RETRY, pArg);

            if (rc == 0)
            {
                count = Marshal.ReadInt32(pArg, 0);
                interval = Marshal.ReadInt32(pArg, sizeof(int));
            }
        }
        finally
        {
            if (pArg != IntPtr.Zero)
                Marshal.FreeHGlobal(pArg);
        }

        return rc;
    }

    /// <summary>
    /// Removes one set of surrounding single -OR- double quotes from the string
    /// value and returns the resulting string value.  If the string is null, empty,
    /// or contains quotes that are not balanced, nothing is done and the original
    /// string value will be returned.
    /// </summary>
    /// <param name="value">The string value to process.</param>
    /// <returns>
    /// The string value, modified to remove one set of surrounding single -OR-
    /// double quotes, if applicable.
    /// </returns>
    private static string UnwrapString(string value)
    {
        if (String.IsNullOrEmpty(value))
        {
            //
            // NOTE: The string is null or empty, return it verbatim.
            //
            return value;
        }

        int length = value.Length;

        if (((value[0] == '\'') && (value[length - 1] == '\'')) ||
            ((value[0] == '"') && (value[length - 1] == '"')))
        {
            //
            // NOTE: Remove the first and last character.
            //
            return value.Substring(1, length - 2);
        }

        //
        // NOTE: No match, return the input string verbatim.
        //
        return value;
    }

    /// <summary>
    /// Expand the filename of the data source, resolving the |DataDirectory|
    /// macro as appropriate.
    /// </summary>
    /// <param name="sourceFile">The database filename to expand</param>
    /// <param name="toFullPath">
    /// Non-zero if the returned file name should be converted to a full path
    /// (except when using the .NET Compact Framework).
    /// </param>
    /// <returns>The expanded path and filename of the filename</returns>
    private string ExpandFileName(string sourceFile, bool toFullPath)
    {
      if (String.IsNullOrEmpty(sourceFile)) return sourceFile;

      if (sourceFile.StartsWith(_dataDirectory, StringComparison.OrdinalIgnoreCase))
      {
        string dataDirectory;

#if PLATFORM_COMPACTFRAMEWORK
        dataDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetCallingAssembly().GetName().CodeBase);
#else
        dataDirectory = AppDomain.CurrentDomain.GetData("DataDirectory") as string;
        if (String.IsNullOrEmpty(dataDirectory))
          dataDirectory = AppDomain.CurrentDomain.BaseDirectory;
#endif

        if (sourceFile.Length > _dataDirectory.Length)
        {
          if (sourceFile[_dataDirectory.Length] == Path.DirectorySeparatorChar ||
              sourceFile[_dataDirectory.Length] == Path.AltDirectorySeparatorChar)
            sourceFile = sourceFile.Remove(_dataDirectory.Length, 1);
        }
        sourceFile = Path.Combine(dataDirectory, sourceFile.Substring(_dataDirectory.Length));
      }

#if !PLATFORM_COMPACTFRAMEWORK
      if (toFullPath)
        sourceFile = Path.GetFullPath(sourceFile);
#endif

      return sourceFile;
    }

    ///<overloads>
    /// The following commands are used to extract schema information out of the database.  Valid schema types are:
    /// <list type="bullet">
    /// <item>
    /// <description>MetaDataCollections</description>
    /// </item>
    /// <item>
    /// <description>DataSourceInformation</description>
    /// </item>
    /// <item>
    /// <description>Catalogs</description>
    /// </item>
    /// <item>
    /// <description>Columns</description>
    /// </item>
    /// <item>
    /// <description>ForeignKeys</description>
    /// </item>
    /// <item>
    /// <description>Indexes</description>
    /// </item>
    /// <item>
    /// <description>IndexColumns</description>
    /// </item>
    /// <item>
    /// <description>Tables</description>
    /// </item>
    /// <item>
    /// <description>Views</description>
    /// </item>
    /// <item>
    /// <description>ViewColumns</description>
    /// </item>
    /// </list>
    /// </overloads>
    /// <summary>
    /// Returns the MetaDataCollections schema
    /// </summary>
    /// <returns>A DataTable of the MetaDataCollections schema</returns>
    public override DataTable GetSchema()
    {
      CheckDisposed();
      return GetSchema("MetaDataCollections", null);
    }

    /// <summary>
    /// Returns schema information of the specified collection
    /// </summary>
    /// <param name="collectionName">The schema collection to retrieve</param>
    /// <returns>A DataTable of the specified collection</returns>
    public override DataTable GetSchema(string collectionName)
    {
      CheckDisposed();
      return GetSchema(collectionName, new string[0]);
    }

    /// <summary>
    /// Retrieves schema information using the specified constraint(s) for the specified collection
    /// </summary>
    /// <param name="collectionName">The collection to retrieve</param>
    /// <param name="restrictionValues">The restrictions to impose</param>
    /// <returns>A DataTable of the specified collection</returns>
    public override DataTable GetSchema(string collectionName, string[] restrictionValues)
    {
      CheckDisposed();

      if (_connectionState != ConnectionState.Open)
        throw new InvalidOperationException();

      string[] parms = new string[5];

      if (restrictionValues == null) restrictionValues = new string[0];
      restrictionValues.CopyTo(parms, 0);

      switch (collectionName.ToUpper(CultureInfo.InvariantCulture))
      {
        case "METADATACOLLECTIONS":
          return Schema_MetaDataCollections();
        case "DATASOURCEINFORMATION":
          return Schema_DataSourceInformation();
        case "DATATYPES":
          return Schema_DataTypes();
        case "COLUMNS":
        case "TABLECOLUMNS":
          return Schema_Columns(parms[0], parms[2], parms[3]);
        case "INDEXES":
          return Schema_Indexes(parms[0], parms[2], parms[3]);
        case "TRIGGERS":
          return Schema_Triggers(parms[0], parms[2], parms[3]);
        case "INDEXCOLUMNS":
          return Schema_IndexColumns(parms[0], parms[2], parms[3], parms[4]);
        case "TABLES":
          return Schema_Tables(parms[0], parms[2], parms[3]);
        case "VIEWS":
          return Schema_Views(parms[0], parms[2]);
        case "VIEWCOLUMNS":
          return Schema_ViewColumns(parms[0], parms[2], parms[3]);
        case "FOREIGNKEYS":
          return Schema_ForeignKeys(parms[0], parms[2], parms[3]);
        case "CATALOGS":
          return Schema_Catalogs(parms[0]);
        case "RESERVEDWORDS":
          return Schema_ReservedWords();
      }
      throw new NotSupportedException();
    }

    private static DataTable Schema_ReservedWords()
    {
      DataTable tbl = new DataTable("MetaDataCollections");

      tbl.Locale = CultureInfo.InvariantCulture;
      tbl.Columns.Add("ReservedWord", typeof(string));
      tbl.Columns.Add("MaximumVersion", typeof(string));
      tbl.Columns.Add("MinimumVersion", typeof(string));

      tbl.BeginLoadData();
      DataRow row;
      foreach (string word in SR.Keywords.Split(new char[] { ',' }))
      {
        row = tbl.NewRow();
        row[0] = word;
        tbl.Rows.Add(row);
      }

      tbl.AcceptChanges();
      tbl.EndLoadData();

      return tbl;
    }

    /// <summary>
    /// Builds a MetaDataCollections schema datatable
    /// </summary>
    /// <returns>DataTable</returns>
    private static DataTable Schema_MetaDataCollections()
    {
      DataTable tbl = new DataTable("MetaDataCollections");

      tbl.Locale = CultureInfo.InvariantCulture;
      tbl.Columns.Add("CollectionName", typeof(string));
      tbl.Columns.Add("NumberOfRestrictions", typeof(int));
      tbl.Columns.Add("NumberOfIdentifierParts", typeof(int));

      tbl.BeginLoadData();

      StringReader reader = new StringReader(SR.MetaDataCollections);
      tbl.ReadXml(reader);
      reader.Close();

      tbl.AcceptChanges();
      tbl.EndLoadData();

      return tbl;
    }

    /// <summary>
    /// Builds a DataSourceInformation datatable
    /// </summary>
    /// <returns>DataTable</returns>
    private DataTable Schema_DataSourceInformation()
    {
      DataTable tbl = new DataTable("DataSourceInformation");
      DataRow row;

      tbl.Locale = CultureInfo.InvariantCulture;
      tbl.Columns.Add(DbMetaDataColumnNames.CompositeIdentifierSeparatorPattern, typeof(string));
      tbl.Columns.Add(DbMetaDataColumnNames.DataSourceProductName, typeof(string));
      tbl.Columns.Add(DbMetaDataColumnNames.DataSourceProductVersion, typeof(string));
      tbl.Columns.Add(DbMetaDataColumnNames.DataSourceProductVersionNormalized, typeof(string));
      tbl.Columns.Add(DbMetaDataColumnNames.GroupByBehavior, typeof(int));
      tbl.Columns.Add(DbMetaDataColumnNames.IdentifierPattern, typeof(string));
      tbl.Columns.Add(DbMetaDataColumnNames.IdentifierCase, typeof(int));
      tbl.Columns.Add(DbMetaDataColumnNames.OrderByColumnsInSelect, typeof(bool));
      tbl.Columns.Add(DbMetaDataColumnNames.ParameterMarkerFormat, typeof(string));
      tbl.Columns.Add(DbMetaDataColumnNames.ParameterMarkerPattern, typeof(string));
      tbl.Columns.Add(DbMetaDataColumnNames.ParameterNameMaxLength, typeof(int));
      tbl.Columns.Add(DbMetaDataColumnNames.ParameterNamePattern, typeof(string));
      tbl.Columns.Add(DbMetaDataColumnNames.QuotedIdentifierPattern, typeof(string));
      tbl.Columns.Add(DbMetaDataColumnNames.QuotedIdentifierCase, typeof(int));
      tbl.Columns.Add(DbMetaDataColumnNames.StatementSeparatorPattern, typeof(string));
      tbl.Columns.Add(DbMetaDataColumnNames.StringLiteralPattern, typeof(string));
      tbl.Columns.Add(DbMetaDataColumnNames.SupportedJoinOperators, typeof(int));

      tbl.BeginLoadData();

      row = tbl.NewRow();
      row.ItemArray = new object[] {
        null,
        "SQLite",
        _sql.Version,
        _sql.Version,
        3,
        @"(^\[\p{Lo}\p{Lu}\p{Ll}_@#][\p{Lo}\p{Lu}\p{Ll}\p{Nd}@$#_]*$)|(^\[[^\]\0]|\]\]+\]$)|(^\""[^\""\0]|\""\""+\""$)",
        1,
        false,
        "{0}",
        @"@[\p{Lo}\p{Lu}\p{Ll}\p{Lm}_@#][\p{Lo}\p{Lu}\p{Ll}\p{Lm}\p{Nd}\uff3f_@#\$]*(?=\s+|$)",
        255,
        @"^[\p{Lo}\p{Lu}\p{Ll}\p{Lm}_@#][\p{Lo}\p{Lu}\p{Ll}\p{Lm}\p{Nd}\uff3f_@#\$]*(?=\s+|$)",
        @"(([^\[]|\]\])*)",
        1,
        ";",
        @"'(([^']|'')*)'",
        15
      };
      tbl.Rows.Add(row);

      tbl.AcceptChanges();
      tbl.EndLoadData();

      return tbl;
    }

    /// <summary>
    /// Build a Columns schema
    /// </summary>
    /// <param name="strCatalog">The catalog (attached database) to query, can be null</param>
    /// <param name="strTable">The table to retrieve schema information for, must not be null</param>
    /// <param name="strColumn">The column to retrieve schema information for, can be null</param>
    /// <returns>DataTable</returns>
    private DataTable Schema_Columns(string strCatalog, string strTable, string strColumn)
    {
      DataTable tbl = new DataTable("Columns");
      DataRow row;

      tbl.Locale = CultureInfo.InvariantCulture;
      tbl.Columns.Add("TABLE_CATALOG", typeof(string));
      tbl.Columns.Add("TABLE_SCHEMA", typeof(string));
      tbl.Columns.Add("TABLE_NAME", typeof(string));
      tbl.Columns.Add("COLUMN_NAME", typeof(string));
      tbl.Columns.Add("COLUMN_GUID", typeof(Guid));
      tbl.Columns.Add("COLUMN_PROPID", typeof(long));
      tbl.Columns.Add("ORDINAL_POSITION", typeof(int));
      tbl.Columns.Add("COLUMN_HASDEFAULT", typeof(bool));
      tbl.Columns.Add("COLUMN_DEFAULT", typeof(string));
      tbl.Columns.Add("COLUMN_FLAGS", typeof(long));
      tbl.Columns.Add("IS_NULLABLE", typeof(bool));
      tbl.Columns.Add("DATA_TYPE", typeof(string));
      tbl.Columns.Add("TYPE_GUID", typeof(Guid));
      tbl.Columns.Add("CHARACTER_MAXIMUM_LENGTH", typeof(int));
      tbl.Columns.Add("CHARACTER_OCTET_LENGTH", typeof(int));
      tbl.Columns.Add("NUMERIC_PRECISION", typeof(int));
      tbl.Columns.Add("NUMERIC_SCALE", typeof(int));
      tbl.Columns.Add("DATETIME_PRECISION", typeof(long));
      tbl.Columns.Add("CHARACTER_SET_CATALOG", typeof(string));
      tbl.Columns.Add("CHARACTER_SET_SCHEMA", typeof(string));
      tbl.Columns.Add("CHARACTER_SET_NAME", typeof(string));
      tbl.Columns.Add("COLLATION_CATALOG", typeof(string));
      tbl.Columns.Add("COLLATION_SCHEMA", typeof(string));
      tbl.Columns.Add("COLLATION_NAME", typeof(string));
      tbl.Columns.Add("DOMAIN_CATALOG", typeof(string));
      tbl.Columns.Add("DOMAIN_NAME", typeof(string));
      tbl.Columns.Add("DESCRIPTION", typeof(string));
      tbl.Columns.Add("PRIMARY_KEY", typeof(bool));
      tbl.Columns.Add("EDM_TYPE", typeof(string));
      tbl.Columns.Add("AUTOINCREMENT", typeof(bool));
      tbl.Columns.Add("UNIQUE", typeof(bool));

      tbl.BeginLoadData();

      if (String.IsNullOrEmpty(strCatalog)) strCatalog = "main";

      string master = (String.Compare(strCatalog, "temp", StringComparison.OrdinalIgnoreCase) == 0) ? _tempmasterdb : _masterdb;

      using (SQLiteCommand cmdTables = new SQLiteCommand(String.Format(CultureInfo.InvariantCulture, "SELECT * FROM [{0}].[{1}] WHERE [type] LIKE 'table' OR [type] LIKE 'view'", strCatalog, master), this))
      using (SQLiteDataReader rdTables = cmdTables.ExecuteReader())
      {
        while (rdTables.Read())
        {
          if (String.IsNullOrEmpty(strTable) || String.Compare(strTable, rdTables.GetString(2), StringComparison.OrdinalIgnoreCase) == 0)
          {
            try
            {
              using (SQLiteCommand cmd = new SQLiteCommand(String.Format(CultureInfo.InvariantCulture, "SELECT * FROM [{0}].[{1}]", strCatalog, rdTables.GetString(2)), this))
              using (SQLiteDataReader rd = (SQLiteDataReader)cmd.ExecuteReader(CommandBehavior.SchemaOnly))
              using (DataTable tblSchema = rd.GetSchemaTable(true, true))
              {
                foreach (DataRow schemaRow in tblSchema.Rows)
                {
                  if (String.Compare(schemaRow[SchemaTableColumn.ColumnName].ToString(), strColumn, StringComparison.OrdinalIgnoreCase) == 0
                    || strColumn == null)
                  {
                    row = tbl.NewRow();

                    row["NUMERIC_PRECISION"] = schemaRow[SchemaTableColumn.NumericPrecision];
                    row["NUMERIC_SCALE"] = schemaRow[SchemaTableColumn.NumericScale];
                    row["TABLE_NAME"] = rdTables.GetString(2);
                    row["COLUMN_NAME"] = schemaRow[SchemaTableColumn.ColumnName];
                    row["TABLE_CATALOG"] = strCatalog;
                    row["ORDINAL_POSITION"] = schemaRow[SchemaTableColumn.ColumnOrdinal];
                    row["COLUMN_HASDEFAULT"] = (schemaRow[SchemaTableOptionalColumn.DefaultValue] != DBNull.Value);
                    row["COLUMN_DEFAULT"] = schemaRow[SchemaTableOptionalColumn.DefaultValue];
                    row["IS_NULLABLE"] = schemaRow[SchemaTableColumn.AllowDBNull];
                    row["DATA_TYPE"] = schemaRow["DataTypeName"].ToString().ToLower(CultureInfo.InvariantCulture);
                    row["EDM_TYPE"] = SQLiteConvert.DbTypeToTypeName((DbType)schemaRow[SchemaTableColumn.ProviderType]).ToString().ToLower(CultureInfo.InvariantCulture);
                    row["CHARACTER_MAXIMUM_LENGTH"] = schemaRow[SchemaTableColumn.ColumnSize];
                    row["TABLE_SCHEMA"] = schemaRow[SchemaTableColumn.BaseSchemaName];
                    row["PRIMARY_KEY"] = schemaRow[SchemaTableColumn.IsKey];
                    row["AUTOINCREMENT"] = schemaRow[SchemaTableOptionalColumn.IsAutoIncrement];
                    row["COLLATION_NAME"] = schemaRow["CollationType"];
                    row["UNIQUE"] = schemaRow[SchemaTableColumn.IsUnique];
                    tbl.Rows.Add(row);
                  }
                }
              }
            }
            catch(SQLiteException)
            {
            }
          }
        }
      }

      tbl.AcceptChanges();
      tbl.EndLoadData();

      return tbl;
    }

    /// <summary>
    /// Returns index information for the given database and catalog
    /// </summary>
    /// <param name="strCatalog">The catalog (attached database) to query, can be null</param>
    /// <param name="strIndex">The name of the index to retrieve information for, can be null</param>
    /// <param name="strTable">The table to retrieve index information for, can be null</param>
    /// <returns>DataTable</returns>
    private DataTable Schema_Indexes(string strCatalog, string strTable, string strIndex)
    {
      DataTable tbl = new DataTable("Indexes");
      DataRow row;
      List<int> primaryKeys = new List<int>();
      bool maybeRowId;

      tbl.Locale = CultureInfo.InvariantCulture;
      tbl.Columns.Add("TABLE_CATALOG", typeof(string));
      tbl.Columns.Add("TABLE_SCHEMA", typeof(string));
      tbl.Columns.Add("TABLE_NAME", typeof(string));
      tbl.Columns.Add("INDEX_CATALOG", typeof(string));
      tbl.Columns.Add("INDEX_SCHEMA", typeof(string));
      tbl.Columns.Add("INDEX_NAME", typeof(string));
      tbl.Columns.Add("PRIMARY_KEY", typeof(bool));
      tbl.Columns.Add("UNIQUE", typeof(bool));
      tbl.Columns.Add("CLUSTERED", typeof(bool));
      tbl.Columns.Add("TYPE", typeof(int));
      tbl.Columns.Add("FILL_FACTOR", typeof(int));
      tbl.Columns.Add("INITIAL_SIZE", typeof(int));
      tbl.Columns.Add("NULLS", typeof(int));
      tbl.Columns.Add("SORT_BOOKMARKS", typeof(bool));
      tbl.Columns.Add("AUTO_UPDATE", typeof(bool));
      tbl.Columns.Add("NULL_COLLATION", typeof(int));
      tbl.Columns.Add("ORDINAL_POSITION", typeof(int));
      tbl.Columns.Add("COLUMN_NAME", typeof(string));
      tbl.Columns.Add("COLUMN_GUID", typeof(Guid));
      tbl.Columns.Add("COLUMN_PROPID", typeof(long));
      tbl.Columns.Add("COLLATION", typeof(short));
      tbl.Columns.Add("CARDINALITY", typeof(Decimal));
      tbl.Columns.Add("PAGES", typeof(int));
      tbl.Columns.Add("FILTER_CONDITION", typeof(string));
      tbl.Columns.Add("INTEGRATED", typeof(bool));
      tbl.Columns.Add("INDEX_DEFINITION", typeof(string));

      tbl.BeginLoadData();

      if (String.IsNullOrEmpty(strCatalog)) strCatalog = "main";

      string master = (String.Compare(strCatalog, "temp", StringComparison.OrdinalIgnoreCase) == 0) ? _tempmasterdb : _masterdb;

      using (SQLiteCommand cmdTables = new SQLiteCommand(String.Format(CultureInfo.InvariantCulture, "SELECT * FROM [{0}].[{1}] WHERE [type] LIKE 'table'", strCatalog, master), this))
      using (SQLiteDataReader rdTables = cmdTables.ExecuteReader())
      {
        while (rdTables.Read())
        {
          maybeRowId = false;
          primaryKeys.Clear();
          if (String.IsNullOrEmpty(strTable) || String.Compare(rdTables.GetString(2), strTable, StringComparison.OrdinalIgnoreCase) == 0)
          {
            // First, look for any rowid indexes -- which sqlite defines are INTEGER PRIMARY KEY columns.
            // Such indexes are not listed in the indexes list but count as indexes just the same.
            try
            {
              using (SQLiteCommand cmdTable = new SQLiteCommand(String.Format(CultureInfo.InvariantCulture, "PRAGMA [{0}].table_info([{1}])", strCatalog, rdTables.GetString(2)), this))
              using (SQLiteDataReader rdTable = cmdTable.ExecuteReader())
              {
                while (rdTable.Read())
                {
                  if (rdTable.GetInt32(5) != 0)
                  {
                    primaryKeys.Add(rdTable.GetInt32(0));

                    // If the primary key is of type INTEGER, then its a rowid and we need to make a fake index entry for it.
                    if (String.Compare(rdTable.GetString(2), "INTEGER", StringComparison.OrdinalIgnoreCase) == 0)
                      maybeRowId = true;
                  }
                }
              }
            }
            catch (SQLiteException)
            {
            }
            if (primaryKeys.Count == 1 && maybeRowId == true)
            {
              row = tbl.NewRow();

              row["TABLE_CATALOG"] = strCatalog;
              row["TABLE_NAME"] = rdTables.GetString(2);
              row["INDEX_CATALOG"] = strCatalog;
              row["PRIMARY_KEY"] = true;
              row["INDEX_NAME"] = String.Format(CultureInfo.InvariantCulture, "{1}_PK_{0}", rdTables.GetString(2), master);
              row["UNIQUE"] = true;

              if (String.Compare((string)row["INDEX_NAME"], strIndex, StringComparison.OrdinalIgnoreCase) == 0
              || strIndex == null)
              {
                tbl.Rows.Add(row);
              }

              primaryKeys.Clear();
            }

            // Now fetch all the rest of the indexes.
            try
            {
              using (SQLiteCommand cmd = new SQLiteCommand(String.Format(CultureInfo.InvariantCulture, "PRAGMA [{0}].index_list([{1}])", strCatalog, rdTables.GetString(2)), this))
              using (SQLiteDataReader rd = (SQLiteDataReader)cmd.ExecuteReader())
              {
                while (rd.Read())
                {
                  if (String.Compare(rd.GetString(1), strIndex, StringComparison.OrdinalIgnoreCase) == 0
                  || strIndex == null)
                  {
                    row = tbl.NewRow();

                    row["TABLE_CATALOG"] = strCatalog;
                    row["TABLE_NAME"] = rdTables.GetString(2);
                    row["INDEX_CATALOG"] = strCatalog;
                    row["INDEX_NAME"] = rd.GetString(1);
                    row["UNIQUE"] = rd.GetBoolean(2);
                    row["PRIMARY_KEY"] = false;

                    // get the index definition
                    using (SQLiteCommand cmdIndexes = new SQLiteCommand(String.Format(CultureInfo.InvariantCulture, "SELECT * FROM [{0}].[{2}] WHERE [type] LIKE 'index' AND [name] LIKE '{1}'", strCatalog, rd.GetString(1).Replace("'", "''"), master), this))
                    using (SQLiteDataReader rdIndexes = cmdIndexes.ExecuteReader())
                    {
                      while (rdIndexes.Read())
                      {
                        if (rdIndexes.IsDBNull(4) == false)
                          row["INDEX_DEFINITION"] = rdIndexes.GetString(4);
                        break;
                      }
                    }

                    // Now for the really hard work.  Figure out which index is the primary key index.
                    // The only way to figure it out is to check if the index was an autoindex and if we have a non-rowid
                    // primary key, and all the columns in the given index match the primary key columns
                    if (primaryKeys.Count > 0 && rd.GetString(1).StartsWith("sqlite_autoindex_" + rdTables.GetString(2), StringComparison.InvariantCultureIgnoreCase) == true)
                    {
                      using (SQLiteCommand cmdDetails = new SQLiteCommand(String.Format(CultureInfo.InvariantCulture, "PRAGMA [{0}].index_info([{1}])", strCatalog, rd.GetString(1)), this))
                      using (SQLiteDataReader rdDetails = cmdDetails.ExecuteReader())
                      {
                        int nMatches = 0;
                        while (rdDetails.Read())
                        {
                          if (primaryKeys.Contains(rdDetails.GetInt32(1)) == false)
                          {
                            nMatches = 0;
                            break;
                          }
                          nMatches++;
                        }
                        if (nMatches == primaryKeys.Count)
                        {
                          row["PRIMARY_KEY"] = true;
                          primaryKeys.Clear();
                        }
                      }
                    }

                    tbl.Rows.Add(row);
                  }
                }
              }
            }
            catch (SQLiteException)
            {
            }
          }
        }
      }

      tbl.AcceptChanges();
      tbl.EndLoadData();

      return tbl;
    }

    private DataTable Schema_Triggers(string catalog, string table, string triggerName)
    {
      DataTable tbl = new DataTable("Triggers");
      DataRow row;

      tbl.Locale = CultureInfo.InvariantCulture;
      tbl.Columns.Add("TABLE_CATALOG", typeof(string));
      tbl.Columns.Add("TABLE_SCHEMA", typeof(string));
      tbl.Columns.Add("TABLE_NAME", typeof(string));
      tbl.Columns.Add("TRIGGER_NAME", typeof(string));
      tbl.Columns.Add("TRIGGER_DEFINITION", typeof(string));

      tbl.BeginLoadData();

      if (String.IsNullOrEmpty(table)) table = null;
      if (String.IsNullOrEmpty(catalog)) catalog = "main";
      string master = (String.Compare(catalog, "temp", StringComparison.OrdinalIgnoreCase) == 0) ? _tempmasterdb : _masterdb;

      using (SQLiteCommand cmd = new SQLiteCommand(String.Format(CultureInfo.InvariantCulture, "SELECT [type], [name], [tbl_name], [rootpage], [sql], [rowid] FROM [{0}].[{1}] WHERE [type] LIKE 'trigger'", catalog, master), this))
      using (SQLiteDataReader rd = (SQLiteDataReader)cmd.ExecuteReader())
      {
        while (rd.Read())
        {
          if (String.Compare(rd.GetString(1), triggerName, StringComparison.OrdinalIgnoreCase) == 0
            || triggerName == null)
          {
            if (table == null || String.Compare(table, rd.GetString(2), StringComparison.OrdinalIgnoreCase) == 0)
            {
              row = tbl.NewRow();

              row["TABLE_CATALOG"] = catalog;
              row["TABLE_NAME"] = rd.GetString(2);
              row["TRIGGER_NAME"] = rd.GetString(1);
              row["TRIGGER_DEFINITION"] = rd.GetString(4);

              tbl.Rows.Add(row);
            }
          }
        }
      }
      tbl.AcceptChanges();
      tbl.EndLoadData();

      return tbl;
    }

    /// <summary>
    /// Retrieves table schema information for the database and catalog
    /// </summary>
    /// <param name="strCatalog">The catalog (attached database) to retrieve tables on</param>
    /// <param name="strTable">The table to retrieve, can be null</param>
    /// <param name="strType">The table type, can be null</param>
    /// <returns>DataTable</returns>
    private DataTable Schema_Tables(string strCatalog, string strTable, string strType)
    {
      DataTable tbl = new DataTable("Tables");
      DataRow row;
      string strItem;

      tbl.Locale = CultureInfo.InvariantCulture;
      tbl.Columns.Add("TABLE_CATALOG", typeof(string));
      tbl.Columns.Add("TABLE_SCHEMA", typeof(string));
      tbl.Columns.Add("TABLE_NAME", typeof(string));
      tbl.Columns.Add("TABLE_TYPE", typeof(string));
      tbl.Columns.Add("TABLE_ID", typeof(long));
      tbl.Columns.Add("TABLE_ROOTPAGE", typeof(int));
      tbl.Columns.Add("TABLE_DEFINITION", typeof(string));
      tbl.BeginLoadData();

      if (String.IsNullOrEmpty(strCatalog)) strCatalog = "main";

      string master = (String.Compare(strCatalog, "temp", StringComparison.OrdinalIgnoreCase) == 0) ? _tempmasterdb : _masterdb;

      using (SQLiteCommand cmd = new SQLiteCommand(String.Format(CultureInfo.InvariantCulture, "SELECT [type], [name], [tbl_name], [rootpage], [sql], [rowid] FROM [{0}].[{1}] WHERE [type] LIKE 'table'", strCatalog, master), this))
      using (SQLiteDataReader rd = (SQLiteDataReader)cmd.ExecuteReader())
      {
        while (rd.Read())
        {
          strItem = rd.GetString(0);
          if (String.Compare(rd.GetString(2), 0, "SQLITE_", 0, 7, StringComparison.OrdinalIgnoreCase) == 0)
            strItem = "SYSTEM_TABLE";

          if (String.Compare(strType, strItem, StringComparison.OrdinalIgnoreCase) == 0
            || strType == null)
          {
            if (String.Compare(rd.GetString(2), strTable, StringComparison.OrdinalIgnoreCase) == 0
              || strTable == null)
            {
              row = tbl.NewRow();

              row["TABLE_CATALOG"] = strCatalog;
              row["TABLE_NAME"] = rd.GetString(2);
              row["TABLE_TYPE"] = strItem;
              row["TABLE_ID"] = rd.GetInt64(5);
              row["TABLE_ROOTPAGE"] = rd.GetInt32(3);
              row["TABLE_DEFINITION"] = rd.GetString(4);

              tbl.Rows.Add(row);
            }
          }
        }
      }

      tbl.AcceptChanges();
      tbl.EndLoadData();

      return tbl;
    }

    /// <summary>
    /// Retrieves view schema information for the database
    /// </summary>
    /// <param name="strCatalog">The catalog (attached database) to retrieve views on</param>
    /// <param name="strView">The view name, can be null</param>
    /// <returns>DataTable</returns>
    private DataTable Schema_Views(string strCatalog, string strView)
    {
      DataTable tbl = new DataTable("Views");
      DataRow row;
      string strItem;
      int nPos;

      tbl.Locale = CultureInfo.InvariantCulture;
      tbl.Columns.Add("TABLE_CATALOG", typeof(string));
      tbl.Columns.Add("TABLE_SCHEMA", typeof(string));
      tbl.Columns.Add("TABLE_NAME", typeof(string));
      tbl.Columns.Add("VIEW_DEFINITION", typeof(string));
      tbl.Columns.Add("CHECK_OPTION", typeof(bool));
      tbl.Columns.Add("IS_UPDATABLE", typeof(bool));
      tbl.Columns.Add("DESCRIPTION", typeof(string));
      tbl.Columns.Add("DATE_CREATED", typeof(DateTime));
      tbl.Columns.Add("DATE_MODIFIED", typeof(DateTime));

      tbl.BeginLoadData();

      if (String.IsNullOrEmpty(strCatalog)) strCatalog = "main";

      string master = (String.Compare(strCatalog, "temp", StringComparison.OrdinalIgnoreCase) == 0) ? _tempmasterdb : _masterdb;

      using (SQLiteCommand cmd = new SQLiteCommand(String.Format(CultureInfo.InvariantCulture, "SELECT * FROM [{0}].[{1}] WHERE [type] LIKE 'view'", strCatalog, master), this))
      using (SQLiteDataReader rd = (SQLiteDataReader)cmd.ExecuteReader())
      {
        while (rd.Read())
        {
          if (String.Compare(rd.GetString(1), strView, StringComparison.OrdinalIgnoreCase) == 0
            || String.IsNullOrEmpty(strView))
          {
            strItem = rd.GetString(4).Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
            nPos = CultureInfo.InvariantCulture.CompareInfo.IndexOf(strItem, " AS ", CompareOptions.IgnoreCase);
            if (nPos > -1)
            {
              strItem = strItem.Substring(nPos + 4).Trim();
              row = tbl.NewRow();

              row["TABLE_CATALOG"] = strCatalog;
              row["TABLE_NAME"] = rd.GetString(2);
              row["IS_UPDATABLE"] = false;
              row["VIEW_DEFINITION"] = strItem;

              tbl.Rows.Add(row);
            }
          }
        }
      }

      tbl.AcceptChanges();
      tbl.EndLoadData();

      return tbl;
    }

    /// <summary>
    /// Retrieves catalog (attached databases) schema information for the database
    /// </summary>
    /// <param name="strCatalog">The catalog to retrieve, can be null</param>
    /// <returns>DataTable</returns>
    private DataTable Schema_Catalogs(string strCatalog)
    {
      DataTable tbl = new DataTable("Catalogs");
      DataRow row;

      tbl.Locale = CultureInfo.InvariantCulture;
      tbl.Columns.Add("CATALOG_NAME", typeof(string));
      tbl.Columns.Add("DESCRIPTION", typeof(string));
      tbl.Columns.Add("ID", typeof(long));

      tbl.BeginLoadData();

      using (SQLiteCommand cmd = new SQLiteCommand("PRAGMA database_list", this))
      using (SQLiteDataReader rd = (SQLiteDataReader)cmd.ExecuteReader())
      {
        while (rd.Read())
        {
          if (String.Compare(rd.GetString(1), strCatalog, StringComparison.OrdinalIgnoreCase) == 0
            || strCatalog == null)
          {
            row = tbl.NewRow();

            row["CATALOG_NAME"] = rd.GetString(1);
            row["DESCRIPTION"] = rd.GetString(2);
            row["ID"] = rd.GetInt64(0);

            tbl.Rows.Add(row);
          }
        }
      }

      tbl.AcceptChanges();
      tbl.EndLoadData();

      return tbl;
    }

    private DataTable Schema_DataTypes()
    {
      DataTable tbl = new DataTable("DataTypes");

      tbl.Locale = CultureInfo.InvariantCulture;
      tbl.Columns.Add("TypeName", typeof(String));
      tbl.Columns.Add("ProviderDbType", typeof(int));
      tbl.Columns.Add("ColumnSize", typeof(long));
      tbl.Columns.Add("CreateFormat", typeof(String));
      tbl.Columns.Add("CreateParameters", typeof(String));
      tbl.Columns.Add("DataType", typeof(String));
      tbl.Columns.Add("IsAutoIncrementable", typeof(bool));
      tbl.Columns.Add("IsBestMatch", typeof(bool));
      tbl.Columns.Add("IsCaseSensitive", typeof(bool));
      tbl.Columns.Add("IsFixedLength", typeof(bool));
      tbl.Columns.Add("IsFixedPrecisionScale", typeof(bool));
      tbl.Columns.Add("IsLong", typeof(bool));
      tbl.Columns.Add("IsNullable", typeof(bool));
      tbl.Columns.Add("IsSearchable", typeof(bool));
      tbl.Columns.Add("IsSearchableWithLike", typeof(bool));
      tbl.Columns.Add("IsLiteralSupported", typeof(bool));
      tbl.Columns.Add("LiteralPrefix", typeof(String));
      tbl.Columns.Add("LiteralSuffix", typeof(String));
      tbl.Columns.Add("IsUnsigned", typeof(bool));
      tbl.Columns.Add("MaximumScale", typeof(short));
      tbl.Columns.Add("MinimumScale", typeof(short));
      tbl.Columns.Add("IsConcurrencyType", typeof(bool));

      tbl.BeginLoadData();

      StringReader reader = new StringReader(SR.DataTypes);
      tbl.ReadXml(reader);
      reader.Close();

      tbl.AcceptChanges();
      tbl.EndLoadData();

      return tbl;
    }

    /// <summary>
    /// Returns the base column information for indexes in a database
    /// </summary>
    /// <param name="strCatalog">The catalog to retrieve indexes for (can be null)</param>
    /// <param name="strTable">The table to restrict index information by (can be null)</param>
    /// <param name="strIndex">The index to restrict index information by (can be null)</param>
    /// <param name="strColumn">The source column to restrict index information by (can be null)</param>
    /// <returns>A DataTable containing the results</returns>
    private DataTable Schema_IndexColumns(string strCatalog, string strTable, string strIndex, string strColumn)
    {
      DataTable tbl = new DataTable("IndexColumns");
      DataRow row;
      List<KeyValuePair<int, string>> primaryKeys = new List<KeyValuePair<int, string>>();
      bool maybeRowId;

      tbl.Locale = CultureInfo.InvariantCulture;
      tbl.Columns.Add("CONSTRAINT_CATALOG", typeof(string));
      tbl.Columns.Add("CONSTRAINT_SCHEMA", typeof(string));
      tbl.Columns.Add("CONSTRAINT_NAME", typeof(string));
      tbl.Columns.Add("TABLE_CATALOG", typeof(string));
      tbl.Columns.Add("TABLE_SCHEMA", typeof(string));
      tbl.Columns.Add("TABLE_NAME", typeof(string));
      tbl.Columns.Add("COLUMN_NAME", typeof(string));
      tbl.Columns.Add("ORDINAL_POSITION", typeof(int));
      tbl.Columns.Add("INDEX_NAME", typeof(string));
      tbl.Columns.Add("COLLATION_NAME", typeof(string));
      tbl.Columns.Add("SORT_MODE", typeof(string));
      tbl.Columns.Add("CONFLICT_OPTION", typeof(int));

      if (String.IsNullOrEmpty(strCatalog)) strCatalog = "main";

      string master = (String.Compare(strCatalog, "temp", StringComparison.OrdinalIgnoreCase) == 0) ? _tempmasterdb : _masterdb;

      tbl.BeginLoadData();

      using (SQLiteCommand cmdTables = new SQLiteCommand(String.Format(CultureInfo.InvariantCulture, "SELECT * FROM [{0}].[{1}] WHERE [type] LIKE 'table'", strCatalog, master), this))
      using (SQLiteDataReader rdTables = cmdTables.ExecuteReader())
      {
        while (rdTables.Read())
        {
          maybeRowId = false;
          primaryKeys.Clear();
          if (String.IsNullOrEmpty(strTable) || String.Compare(rdTables.GetString(2), strTable, StringComparison.OrdinalIgnoreCase) == 0)
          {
            try
            {
              using (SQLiteCommand cmdTable = new SQLiteCommand(String.Format(CultureInfo.InvariantCulture, "PRAGMA [{0}].table_info([{1}])", strCatalog, rdTables.GetString(2)), this))
              using (SQLiteDataReader rdTable = cmdTable.ExecuteReader())
              {
                while (rdTable.Read())
                {
                  if (rdTable.GetInt32(5) == 1) // is a primary key
                  {
                    primaryKeys.Add(new KeyValuePair<int, string>(rdTable.GetInt32(0), rdTable.GetString(1)));
                    // Is an integer -- could be a rowid if no other primary keys exist in the table
                    if (String.Compare(rdTable.GetString(2), "INTEGER", StringComparison.OrdinalIgnoreCase) == 0)
                      maybeRowId = true;
                  }
                }
              }
            }
            catch (SQLiteException)
            {
            }
            // This is a rowid row
            if (primaryKeys.Count == 1 && maybeRowId == true)
            {
              row = tbl.NewRow();
              row["CONSTRAINT_CATALOG"] = strCatalog;
              row["CONSTRAINT_NAME"] = String.Format(CultureInfo.InvariantCulture, "{1}_PK_{0}", rdTables.GetString(2), master);
              row["TABLE_CATALOG"] = strCatalog;
              row["TABLE_NAME"] = rdTables.GetString(2);
              row["COLUMN_NAME"] = primaryKeys[0].Value;
              row["INDEX_NAME"] = row["CONSTRAINT_NAME"];
              row["ORDINAL_POSITION"] = 0; // primaryKeys[0].Key;
              row["COLLATION_NAME"] = "BINARY";
              row["SORT_MODE"] = "ASC";
              row["CONFLICT_OPTION"] = 2;

              if (String.IsNullOrEmpty(strIndex) || String.Compare(strIndex, (string)row["INDEX_NAME"], StringComparison.OrdinalIgnoreCase) == 0)
                tbl.Rows.Add(row);
            }

            using (SQLiteCommand cmdIndexes = new SQLiteCommand(String.Format(CultureInfo.InvariantCulture, "SELECT * FROM [{0}].[{2}] WHERE [type] LIKE 'index' AND [tbl_name] LIKE '{1}'", strCatalog, rdTables.GetString(2).Replace("'", "''"), master), this))
            using (SQLiteDataReader rdIndexes = cmdIndexes.ExecuteReader())
            {
              while (rdIndexes.Read())
              {
                int ordinal = 0;
                if (String.IsNullOrEmpty(strIndex) || String.Compare(strIndex, rdIndexes.GetString(1), StringComparison.OrdinalIgnoreCase) == 0)
                {
                  try
                  {
                    using (SQLiteCommand cmdIndex = new SQLiteCommand(String.Format(CultureInfo.InvariantCulture, "PRAGMA [{0}].index_info([{1}])", strCatalog, rdIndexes.GetString(1)), this))
                    using (SQLiteDataReader rdIndex = cmdIndex.ExecuteReader())
                    {
                      while (rdIndex.Read())
                      {
                        row = tbl.NewRow();
                        row["CONSTRAINT_CATALOG"] = strCatalog;
                        row["CONSTRAINT_NAME"] = rdIndexes.GetString(1);
                        row["TABLE_CATALOG"] = strCatalog;
                        row["TABLE_NAME"] = rdIndexes.GetString(2);
                        row["COLUMN_NAME"] = rdIndex.GetString(2);
                        row["INDEX_NAME"] = rdIndexes.GetString(1);
                        row["ORDINAL_POSITION"] = ordinal; // rdIndex.GetInt32(1);

                        string collationSequence;
                        int sortMode;
                        int onError;
                        _sql.GetIndexColumnExtendedInfo(strCatalog, rdIndexes.GetString(1), rdIndex.GetString(2), out sortMode, out onError, out collationSequence);

                        if (String.IsNullOrEmpty(collationSequence) == false)
                          row["COLLATION_NAME"] = collationSequence;

                        row["SORT_MODE"] = (sortMode == 0) ? "ASC" : "DESC";
                        row["CONFLICT_OPTION"] = onError;

                        ordinal++;

                        if (String.IsNullOrEmpty(strColumn) || String.Compare(strColumn, row["COLUMN_NAME"].ToString(), StringComparison.OrdinalIgnoreCase) == 0)
                          tbl.Rows.Add(row);
                      }
                    }
                  }
                  catch (SQLiteException)
                  {
                  }
                }
              }
            }
          }
        }
      }

      tbl.EndLoadData();
      tbl.AcceptChanges();

      return tbl;
    }

    /// <summary>
    /// Returns detailed column information for a specified view
    /// </summary>
    /// <param name="strCatalog">The catalog to retrieve columns for (can be null)</param>
    /// <param name="strView">The view to restrict column information by (can be null)</param>
    /// <param name="strColumn">The source column to restrict column information by (can be null)</param>
    /// <returns>A DataTable containing the results</returns>
    private DataTable Schema_ViewColumns(string strCatalog, string strView, string strColumn)
    {
      DataTable tbl = new DataTable("ViewColumns");
      DataRow row;
      string strSql;
      int n;
      DataRow schemaRow;
      DataRow viewRow;

      tbl.Locale = CultureInfo.InvariantCulture;
      tbl.Columns.Add("VIEW_CATALOG", typeof(string));
      tbl.Columns.Add("VIEW_SCHEMA", typeof(string));
      tbl.Columns.Add("VIEW_NAME", typeof(string));
      tbl.Columns.Add("VIEW_COLUMN_NAME", typeof(String));
      tbl.Columns.Add("TABLE_CATALOG", typeof(string));
      tbl.Columns.Add("TABLE_SCHEMA", typeof(string));
      tbl.Columns.Add("TABLE_NAME", typeof(string));
      tbl.Columns.Add("COLUMN_NAME", typeof(string));
      tbl.Columns.Add("ORDINAL_POSITION", typeof(int));
      tbl.Columns.Add("COLUMN_HASDEFAULT", typeof(bool));
      tbl.Columns.Add("COLUMN_DEFAULT", typeof(string));
      tbl.Columns.Add("COLUMN_FLAGS", typeof(long));
      tbl.Columns.Add("IS_NULLABLE", typeof(bool));
      tbl.Columns.Add("DATA_TYPE", typeof(string));
      tbl.Columns.Add("CHARACTER_MAXIMUM_LENGTH", typeof(int));
      tbl.Columns.Add("NUMERIC_PRECISION", typeof(int));
      tbl.Columns.Add("NUMERIC_SCALE", typeof(int));
      tbl.Columns.Add("DATETIME_PRECISION", typeof(long));
      tbl.Columns.Add("CHARACTER_SET_CATALOG", typeof(string));
      tbl.Columns.Add("CHARACTER_SET_SCHEMA", typeof(string));
      tbl.Columns.Add("CHARACTER_SET_NAME", typeof(string));
      tbl.Columns.Add("COLLATION_CATALOG", typeof(string));
      tbl.Columns.Add("COLLATION_SCHEMA", typeof(string));
      tbl.Columns.Add("COLLATION_NAME", typeof(string));
      tbl.Columns.Add("PRIMARY_KEY", typeof(bool));
      tbl.Columns.Add("EDM_TYPE", typeof(string));
      tbl.Columns.Add("AUTOINCREMENT", typeof(bool));
      tbl.Columns.Add("UNIQUE", typeof(bool));

      if (String.IsNullOrEmpty(strCatalog)) strCatalog = "main";

      string master = (String.Compare(strCatalog, "temp", StringComparison.OrdinalIgnoreCase) == 0) ? _tempmasterdb : _masterdb;

      tbl.BeginLoadData();

      using (SQLiteCommand cmdViews = new SQLiteCommand(String.Format(CultureInfo.InvariantCulture, "SELECT * FROM [{0}].[{1}] WHERE [type] LIKE 'view'", strCatalog, master), this))
      using (SQLiteDataReader rdViews = cmdViews.ExecuteReader())
      {
        while (rdViews.Read())
        {
          if (String.IsNullOrEmpty(strView) || String.Compare(strView, rdViews.GetString(2), StringComparison.OrdinalIgnoreCase) == 0)
          {
            using (SQLiteCommand cmdViewSelect = new SQLiteCommand(String.Format(CultureInfo.InvariantCulture, "SELECT * FROM [{0}].[{1}]", strCatalog, rdViews.GetString(2)), this))
            {
              strSql = rdViews.GetString(4).Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
              n = CultureInfo.InvariantCulture.CompareInfo.IndexOf(strSql, " AS ", CompareOptions.IgnoreCase);
              if (n < 0)
                continue;

              strSql = strSql.Substring(n + 4);

              using (SQLiteCommand cmd = new SQLiteCommand(strSql, this))
              using (SQLiteDataReader rdViewSelect = cmdViewSelect.ExecuteReader(CommandBehavior.SchemaOnly))
              using (SQLiteDataReader rd = (SQLiteDataReader)cmd.ExecuteReader(CommandBehavior.SchemaOnly))
              using (DataTable tblSchemaView = rdViewSelect.GetSchemaTable(false, false))
              using (DataTable tblSchema = rd.GetSchemaTable(false, false))
              {
                for (n = 0; n < tblSchema.Rows.Count; n++)
                {
                  viewRow = tblSchemaView.Rows[n];
                  schemaRow = tblSchema.Rows[n];

                  if (String.Compare(viewRow[SchemaTableColumn.ColumnName].ToString(), strColumn, StringComparison.OrdinalIgnoreCase) == 0
                    || strColumn == null)
                  {
                    row = tbl.NewRow();

                    row["VIEW_CATALOG"] = strCatalog;
                    row["VIEW_NAME"] = rdViews.GetString(2);
                    row["TABLE_CATALOG"] = strCatalog;
                    row["TABLE_SCHEMA"] = schemaRow[SchemaTableColumn.BaseSchemaName];
                    row["TABLE_NAME"] = schemaRow[SchemaTableColumn.BaseTableName];
                    row["COLUMN_NAME"] = schemaRow[SchemaTableColumn.BaseColumnName];
                    row["VIEW_COLUMN_NAME"] = viewRow[SchemaTableColumn.ColumnName];
                    row["COLUMN_HASDEFAULT"] = (viewRow[SchemaTableOptionalColumn.DefaultValue] != DBNull.Value);
                    row["COLUMN_DEFAULT"] = viewRow[SchemaTableOptionalColumn.DefaultValue];
                    row["ORDINAL_POSITION"] = viewRow[SchemaTableColumn.ColumnOrdinal];
                    row["IS_NULLABLE"] = viewRow[SchemaTableColumn.AllowDBNull];
                    row["DATA_TYPE"] = viewRow["DataTypeName"]; // SQLiteConvert.DbTypeToType((DbType)viewRow[SchemaTableColumn.ProviderType]).ToString();
                    row["EDM_TYPE"] = SQLiteConvert.DbTypeToTypeName((DbType)viewRow[SchemaTableColumn.ProviderType]).ToString().ToLower(CultureInfo.InvariantCulture);
                    row["CHARACTER_MAXIMUM_LENGTH"] = viewRow[SchemaTableColumn.ColumnSize];
                    row["TABLE_SCHEMA"] = viewRow[SchemaTableColumn.BaseSchemaName];
                    row["PRIMARY_KEY"] = viewRow[SchemaTableColumn.IsKey];
                    row["AUTOINCREMENT"] = viewRow[SchemaTableOptionalColumn.IsAutoIncrement];
                    row["COLLATION_NAME"] = viewRow["CollationType"];
                    row["UNIQUE"] = viewRow[SchemaTableColumn.IsUnique];
                    tbl.Rows.Add(row);
                  }
                }
              }
            }
          }
        }
      }

      tbl.EndLoadData();
      tbl.AcceptChanges();

      return tbl;
    }

    /// <summary>
    /// Retrieves foreign key information from the specified set of filters
    /// </summary>
    /// <param name="strCatalog">An optional catalog to restrict results on</param>
    /// <param name="strTable">An optional table to restrict results on</param>
    /// <param name="strKeyName">An optional foreign key name to restrict results on</param>
    /// <returns>A DataTable with the results of the query</returns>
    private DataTable Schema_ForeignKeys(string strCatalog, string strTable, string strKeyName)
    {
      DataTable tbl = new DataTable("ForeignKeys");
      DataRow row;

      tbl.Locale = CultureInfo.InvariantCulture;
      tbl.Columns.Add("CONSTRAINT_CATALOG", typeof(string));
      tbl.Columns.Add("CONSTRAINT_SCHEMA", typeof(string));
      tbl.Columns.Add("CONSTRAINT_NAME", typeof(string));
      tbl.Columns.Add("TABLE_CATALOG", typeof(string));
      tbl.Columns.Add("TABLE_SCHEMA", typeof(string));
      tbl.Columns.Add("TABLE_NAME", typeof(string));
      tbl.Columns.Add("CONSTRAINT_TYPE", typeof(string));
      tbl.Columns.Add("IS_DEFERRABLE", typeof(bool));
      tbl.Columns.Add("INITIALLY_DEFERRED", typeof(bool));
      tbl.Columns.Add("FKEY_ID", typeof(int));
      tbl.Columns.Add("FKEY_FROM_COLUMN", typeof(string));
      tbl.Columns.Add("FKEY_FROM_ORDINAL_POSITION", typeof(int));
      tbl.Columns.Add("FKEY_TO_CATALOG", typeof(string));
      tbl.Columns.Add("FKEY_TO_SCHEMA", typeof(string));
      tbl.Columns.Add("FKEY_TO_TABLE", typeof(string));
      tbl.Columns.Add("FKEY_TO_COLUMN", typeof(string));
      tbl.Columns.Add("FKEY_ON_UPDATE", typeof(string));
      tbl.Columns.Add("FKEY_ON_DELETE", typeof(string));
      tbl.Columns.Add("FKEY_MATCH", typeof(string));

      if (String.IsNullOrEmpty(strCatalog)) strCatalog = "main";

      string master = (String.Compare(strCatalog, "temp", StringComparison.OrdinalIgnoreCase) == 0) ? _tempmasterdb : _masterdb;

      tbl.BeginLoadData();

      using (SQLiteCommand cmdTables = new SQLiteCommand(String.Format(CultureInfo.InvariantCulture, "SELECT * FROM [{0}].[{1}] WHERE [type] LIKE 'table'", strCatalog, master), this))
      using (SQLiteDataReader rdTables = cmdTables.ExecuteReader())
      {
        while (rdTables.Read())
        {
          if (String.IsNullOrEmpty(strTable) || String.Compare(strTable, rdTables.GetString(2), StringComparison.OrdinalIgnoreCase) == 0)
          {
            try
            {
              using (SQLiteCommandBuilder builder = new SQLiteCommandBuilder())
              using (SQLiteCommand cmdKey = new SQLiteCommand(String.Format(CultureInfo.InvariantCulture, "PRAGMA [{0}].foreign_key_list([{1}])", strCatalog, rdTables.GetString(2)), this))
              using (SQLiteDataReader rdKey = cmdKey.ExecuteReader())
              {
                while (rdKey.Read())
                {
                  row = tbl.NewRow();
                  row["CONSTRAINT_CATALOG"] = strCatalog;
                  row["CONSTRAINT_NAME"] = String.Format(CultureInfo.InvariantCulture, "FK_{0}_{1}_{2}", rdTables[2], rdKey.GetInt32(0), rdKey.GetInt32(1));
                  row["TABLE_CATALOG"] = strCatalog;
                  row["TABLE_NAME"] = builder.UnquoteIdentifier(rdTables.GetString(2));
                  row["CONSTRAINT_TYPE"] = "FOREIGN KEY";
                  row["IS_DEFERRABLE"] = false;
                  row["INITIALLY_DEFERRED"] = false;
                  row["FKEY_ID"] = rdKey[0];
                  row["FKEY_FROM_COLUMN"] = builder.UnquoteIdentifier(rdKey[3].ToString());
                  row["FKEY_TO_CATALOG"] = strCatalog;
                  row["FKEY_TO_TABLE"] = builder.UnquoteIdentifier(rdKey[2].ToString());
                  row["FKEY_TO_COLUMN"] = builder.UnquoteIdentifier(rdKey[4].ToString());
                  row["FKEY_FROM_ORDINAL_POSITION"] = rdKey[1];
                  row["FKEY_ON_UPDATE"] = (rdKey.FieldCount > 5) ? rdKey[5] : String.Empty;
                  row["FKEY_ON_DELETE"] = (rdKey.FieldCount > 6) ? rdKey[6] : String.Empty;
                  row["FKEY_MATCH"] = (rdKey.FieldCount > 7) ? rdKey[7] : String.Empty;

                  if (String.IsNullOrEmpty(strKeyName) || String.Compare(strKeyName, row["CONSTRAINT_NAME"].ToString(), StringComparison.OrdinalIgnoreCase) == 0)
                    tbl.Rows.Add(row);
                }
              }
            }
            catch (SQLiteException)
            {
            }
          }
        }
      }

      tbl.EndLoadData();
      tbl.AcceptChanges();

      return tbl;
    }

    /// <summary>
    /// This event is raised whenever SQLite makes an update/delete/insert into the database on
    /// this connection.  It only applies to the given connection.
    /// </summary>
    public event SQLiteUpdateEventHandler Update
    {
      add
      {
        CheckDisposed();

        if (_updateHandler == null)
        {
          _updateCallback = new SQLiteUpdateCallback(UpdateCallback);
          if (_sql != null) _sql.SetUpdateHook(_updateCallback);
        }
        _updateHandler += value;
      }
      remove
      {
        CheckDisposed();

        _updateHandler -= value;
        if (_updateHandler == null)
        {
          if (_sql != null) _sql.SetUpdateHook(null);
          _updateCallback = null;
        }
      }
    }

    private void UpdateCallback(IntPtr puser, int type, IntPtr database, IntPtr table, Int64 rowid)
    {
      _updateHandler(this, new UpdateEventArgs(
        SQLiteBase.UTF8ToString(database, -1),
        SQLiteBase.UTF8ToString(table, -1),
        (UpdateEventType)type,
        rowid));
    }

    /// <summary>
    /// This event is raised whenever SQLite is committing a transaction.
    /// Return non-zero to trigger a rollback.
    /// </summary>
    public event SQLiteCommitHandler Commit
    {
      add
      {
        CheckDisposed();

        if (_commitHandler == null)
        {
          _commitCallback = new SQLiteCommitCallback(CommitCallback);
          if (_sql != null) _sql.SetCommitHook(_commitCallback);
        }
        _commitHandler += value;
      }
      remove
      {
        CheckDisposed();

        _commitHandler -= value;
        if (_commitHandler == null)
        {
          if (_sql != null) _sql.SetCommitHook(null);
          _commitCallback = null;
        }
      }
    }

    /// <summary>
    /// This event is raised whenever SQLite statement first begins executing on
    /// this connection.  It only applies to the given connection.
    /// </summary>
    public event SQLiteTraceEventHandler Trace
    {
      add
      {
        CheckDisposed();

        if (_traceHandler == null)
        {
          _traceCallback = new SQLiteTraceCallback(TraceCallback);
          if (_sql != null) _sql.SetTraceCallback(_traceCallback);
        }
        _traceHandler += value;
      }
      remove
      {
        CheckDisposed();

        _traceHandler -= value;
        if (_traceHandler == null)
        {
          if (_sql != null) _sql.SetTraceCallback(null);
            _traceCallback = null;
        }
      }
    }

    private void TraceCallback(IntPtr puser, IntPtr statement)
    {
      _traceHandler(this, new TraceEventArgs(
        SQLiteBase.UTF8ToString(statement, -1)));
    }

    /// <summary>
    /// This event is raised whenever SQLite is rolling back a transaction.
    /// </summary>
    public event EventHandler RollBack
    {
      add
      {
        CheckDisposed();

        if (_rollbackHandler == null)
        {
          _rollbackCallback = new SQLiteRollbackCallback(RollbackCallback);
          if (_sql != null) _sql.SetRollbackHook(_rollbackCallback);
        }
        _rollbackHandler += value;
      }
      remove
      {
        CheckDisposed();

        _rollbackHandler -= value;
        if (_rollbackHandler == null)
        {
          if (_sql != null) _sql.SetRollbackHook(null);
          _rollbackCallback = null;
        }
      }
    }


    private int CommitCallback(IntPtr parg)
    {
      CommitEventArgs e = new CommitEventArgs();
      _commitHandler(this, e);
      return (e.AbortTransaction == true) ? 1 : 0;
    }

    private void RollbackCallback(IntPtr parg)
    {
      _rollbackHandler(this, EventArgs.Empty);
    }

  }

  /// <summary>
  /// The I/O file cache flushing behavior for the connection
  /// </summary>
  public enum SynchronizationModes
  {
    /// <summary>
    /// Normal file flushing at critical sections of the code
    /// </summary>
    Normal = 0,
    /// <summary>
    /// Full file flushing after every write operation
    /// </summary>
    Full = 1,
    /// <summary>
    /// Use the default operating system's file flushing, SQLite does not explicitly flush the file buffers after writing
    /// </summary>
    Off = 2,
  }

#if !PLATFORM_COMPACTFRAMEWORK
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
  internal delegate void SQLiteUpdateCallback(IntPtr puser, int type, IntPtr database, IntPtr table, Int64 rowid);

#if !PLATFORM_COMPACTFRAMEWORK
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
  internal delegate int SQLiteCommitCallback(IntPtr puser);

#if !PLATFORM_COMPACTFRAMEWORK
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
  internal delegate void SQLiteTraceCallback(IntPtr puser, IntPtr statement);

#if !PLATFORM_COMPACTFRAMEWORK
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
  internal delegate void SQLiteRollbackCallback(IntPtr puser);

  /// <summary>
  /// Raised when a transaction is about to be committed.  To roll back a transaction, set the
  /// rollbackTrans boolean value to true.
  /// </summary>
  /// <param name="sender">The connection committing the transaction</param>
  /// <param name="e">Event arguments on the transaction</param>
  public delegate void SQLiteCommitHandler(object sender, CommitEventArgs e);

  /// <summary>
  /// Raised when data is inserted, updated and deleted on a given connection
  /// </summary>
  /// <param name="sender">The connection committing the transaction</param>
  /// <param name="e">The event parameters which triggered the event</param>
  public delegate void SQLiteUpdateEventHandler(object sender, UpdateEventArgs e);

  /// <summary>
  /// Raised when a statement first begins executing on a given connection
  /// </summary>
  /// <param name="sender">The connection executing the statement</param>
  /// <param name="e">Event arguments of the trace</param>
  public delegate void SQLiteTraceEventHandler(object sender, TraceEventArgs e);

  ///////////////////////////////////////////////////////////////////////////////////////////////

  #region Backup API Members
  /// <summary>
  /// Raised between each backup step.
  /// </summary>
  /// <param name="source">
  /// The source database connection.
  /// </param>
  /// <param name="sourceName">
  /// The source database name.
  /// </param>
  /// <param name="destination">
  /// The destination database connection.
  /// </param>
  /// <param name="destinationName">
  /// The destination database name.
  /// </param>
  /// <param name="pages">
  /// The number of pages copied with each step.
  /// </param>
  /// <param name="remainingPages">
  /// The number of pages remaining to be copied.
  /// </param>
  /// <param name="totalPages">
  /// The total number of pages in the source database.
  /// </param>
  /// <param name="retry">
  /// Set to true if the operation needs to be retried due to database
  /// locking issues; otherwise, set to false.
  /// </param>
  /// <returns>
  /// True to continue with the backup process or false to halt the backup
  /// process, rolling back any changes that have been made so far.
  /// </returns>
  public delegate bool SQLiteBackupCallback(
    SQLiteConnection source,
    string sourceName,
    SQLiteConnection destination,
    string destinationName,
    int pages,
    int remainingPages,
    int totalPages,
    bool retry
  );
  #endregion

  ///////////////////////////////////////////////////////////////////////////////////////////////

  /// <summary>
  /// Whenever an update event is triggered on a connection, this enum will indicate
  /// exactly what type of operation is being performed.
  /// </summary>
  public enum UpdateEventType
  {
    /// <summary>
    /// A row is being deleted from the given database and table
    /// </summary>
    Delete = 9,
    /// <summary>
    /// A row is being inserted into the table.
    /// </summary>
    Insert = 18,
    /// <summary>
    /// A row is being updated in the table.
    /// </summary>
    Update = 23,
  }

  /// <summary>
  /// Passed during an Update callback, these event arguments detail the type of update operation being performed
  /// on the given connection.
  /// </summary>
  public class UpdateEventArgs : EventArgs
  {
    /// <summary>
    /// The name of the database being updated (usually "main" but can be any attached or temporary database)
    /// </summary>
    public readonly string Database;

    /// <summary>
    /// The name of the table being updated
    /// </summary>
    public readonly string Table;

    /// <summary>
    /// The type of update being performed (insert/update/delete)
    /// </summary>
    public readonly UpdateEventType Event;

    /// <summary>
    /// The RowId affected by this update.
    /// </summary>
    public readonly Int64 RowId;

    internal UpdateEventArgs(string database, string table, UpdateEventType eventType, Int64 rowid)
    {
      Database = database;
      Table = table;
      Event = eventType;
      RowId = rowid;
    }
  }

  /// <summary>
  /// Event arguments raised when a transaction is being committed
  /// </summary>
  public class CommitEventArgs : EventArgs
  {
    internal CommitEventArgs()
    {
    }

    /// <summary>
    /// Set to true to abort the transaction and trigger a rollback
    /// </summary>
    public bool AbortTransaction;
  }

  /// <summary>
  /// Passed during an Trace callback, these event arguments contain the UTF-8 rendering of the SQL statement text
  /// </summary>
  public class TraceEventArgs : EventArgs
  {
    /// <summary>
    /// SQL statement text as the statement first begins executing
    /// </summary>
    public readonly string Statement;

    internal TraceEventArgs(string statement)
    {
      Statement = statement;
    }
  }

}
