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
  /// Passed during an Log callback
  /// </summary>
  public class LogEventArgs : EventArgs
  {
      /// <summary>
      /// The error code.
      /// </summary>
      public readonly int ErrorCode;

      /// <summary>
      /// SQL statement text as the statement first begins executing
      /// </summary>
      public readonly string Message;

      internal LogEventArgs(IntPtr puser, int err_code, string message)
      {
          // puser should be NULL
          ErrorCode = err_code;
          Message = message;
      }
  }

  /// <summary>
  /// Raised when a log event occurs.
  /// </summary>
  /// <param name="sender">The current connection</param>
  /// <param name="e">Event arguments of the trace</param>
  public delegate void SQLiteLogEventHandler(object sender, LogEventArgs e);


#if !PLATFORM_COMPACTFRAMEWORK
  /// <summary>
  /// SQLite implementation of DbProviderFactory.
  /// </summary>
  public sealed partial class SQLiteFactory : DbProviderFactory
  {
    /// <summary>
    /// Member variable to store the application log handler to call.
    /// </summary>
    internal event SQLiteLogEventHandler _logHandler;
    /// <summary>
    /// The log callback passed to SQLite engine.
    /// </summary>
    private SQLiteLogCallback _logCallback;
    /// <summary>
    /// The base SQLite object to interop with.
    /// </summary>
    internal SQLiteBase _sql;

    /// <summary>
    /// This event is raised whenever SQLite raises a logging event.
    /// Note that this should be set as one of the first things in the
    /// application.
    /// </summary>
    public event SQLiteLogEventHandler Log
    {
        add
        {
            // Remove any copies of this event handler from registered list.
            // This essentially means that a handler will be called only once
            // no matter how many times it is added.
            _logHandler -= value;
            // add this to the list of event handlers
            _logHandler += value;
        }
        remove
        {
            _logHandler -= value;
        }
    }

    /// <summary>
    /// Internal proxy function that calls any registered application log
    /// event handlers.
    /// </summary>
    private void LogCallback(IntPtr puser, int err_code, IntPtr message)
    {
      // if there are any registered event handlers
      if (_logHandler != null)
        // call them
        _logHandler(this,
                    new LogEventArgs(puser,
                                     err_code,
                                     SQLiteBase.UTF8ToString(message, -1)));
    }

    /// <overloads>
    /// Constructs a new SQLiteFactory object
    /// </overloads>
    /// <summary>
    /// Default constructor
    /// </summary>
    public SQLiteFactory()
    {
      if (_sql == null)
      {
        _sql = new SQLite3(SQLiteDateFormats.ISO8601);
        if (_sql != null)
        {
          // Create a single "global" callback to register with SQLite.
          // This callback will pass the event on to any registered
          // handler.  We only want to do this once.
          if (_logCallback == null)
          {
            _logCallback = new SQLiteLogCallback(LogCallback);
            if (_logCallback != null)
            {
              _sql.SetLogCallback(_logCallback);
            }
          }
        }
      }
    }

    /// <summary>
    /// Static instance member which returns an instanced SQLiteFactory class.
    /// </summary>
    public static readonly SQLiteFactory Instance = new SQLiteFactory();

    /// <summary>
    /// Returns a new SQLiteCommand object.
    /// </summary>
    /// <returns>A SQLiteCommand object.</returns>
    public override DbCommand CreateCommand()
    {
      return new SQLiteCommand();
    }

    /// <summary>
    /// Returns a new SQLiteCommandBuilder object.
    /// </summary>
    /// <returns>A SQLiteCommandBuilder object.</returns>
    public override DbCommandBuilder CreateCommandBuilder()
    {
      return new SQLiteCommandBuilder();
    }

    /// <summary>
    /// Creates a new SQLiteConnection.
    /// </summary>
    /// <returns>A SQLiteConnection object.</returns>
    public override DbConnection CreateConnection()
    {
      return new SQLiteConnection();
    }

    /// <summary>
    /// Creates a new SQLiteConnectionStringBuilder.
    /// </summary>
    /// <returns>A SQLiteConnectionStringBuilder object.</returns>
    public override DbConnectionStringBuilder CreateConnectionStringBuilder()
    {
      return new SQLiteConnectionStringBuilder();
    }

    /// <summary>
    /// Creates a new SQLiteDataAdapter.
    /// </summary>
    /// <returns>A SQLiteDataAdapter object.</returns>
    public override DbDataAdapter CreateDataAdapter()
    {
      return new SQLiteDataAdapter();
    }

    /// <summary>
    /// Creates a new SQLiteParameter.
    /// </summary>
    /// <returns>A SQLiteParameter object.</returns>
    public override DbParameter CreateParameter()
    {
      return new SQLiteParameter();
    }
  }
#endif
}
