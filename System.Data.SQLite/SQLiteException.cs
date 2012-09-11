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

#if !PLATFORM_COMPACTFRAMEWORK
  using System.Runtime.Serialization;
#endif

  /// <summary>
  /// SQLite exception class.
  /// </summary>
#if !PLATFORM_COMPACTFRAMEWORK
  [Serializable]
  public sealed class SQLiteException : DbException
#else
  public sealed class SQLiteException : Exception
#endif
  {
    private SQLiteErrorCode _errorCode;

#if !PLATFORM_COMPACTFRAMEWORK
    private SQLiteException(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }
#endif

    /// <summary>
    /// Public constructor for generating a SQLite error given the base error code
    /// </summary>
    /// <param name="errorCode">The SQLite error code to report</param>
    /// <param name="message">Extra text to go along with the error message text</param>
    public SQLiteException(SQLiteErrorCode errorCode, string message)
      : base(GetStockErrorMessage(errorCode, message))
    {
      _errorCode = errorCode;
    }

    /// <summary>
    /// Various public constructors that just pass along to the base Exception
    /// </summary>
    /// <param name="message">Passed verbatim to Exception</param>
    public SQLiteException(string message)
      : base(message)
    {
    }

    /// <summary>
    /// Various public constructors that just pass along to the base Exception
    /// </summary>
    public SQLiteException()
    {
    }

    /// <summary>
    /// Various public constructors that just pass along to the base Exception
    /// <param name="message">Passed to Exception</param>
    /// <param name="innerException">Passed to Exception</param>
    /// </summary>
    public SQLiteException(string message, Exception innerException)
      : base(message, innerException)
    {
    }

    /// <summary>
    /// Retrieves the underlying SQLite error code for this exception
    /// </summary>
#if !PLATFORM_COMPACTFRAMEWORK
    public new SQLiteErrorCode ErrorCode
#else
    public SQLiteErrorCode ErrorCode
#endif
    {
      get { return _errorCode; }
    }

    /// <summary>
    /// Initializes the exception class with the SQLite error code.
    /// </summary>
    /// <param name="errorCode">The SQLite error code</param>
    /// <param name="message">A detailed error message</param>
    /// <returns>An error message string</returns>
    private static string GetStockErrorMessage(
        SQLiteErrorCode errorCode,
        string message
        )
    {
        return String.Format("{0}{1}{2}",
            SQLiteBase.GetErrorString(errorCode),
            Environment.NewLine, message).Trim();
    }
  }

  /// <summary>
  /// SQLite error codes.  Actually, this enumeration represents a return code,
  /// which may also indicate success in one of several ways (e.g. SQLITE_OK,
  /// SQLITE_ROW, and SQLITE_DONE).  Therefore, the name of this enumeration is
  /// something of a misnomer.
  /// </summary>
  public enum SQLiteErrorCode
  {
    /// <summary>
    /// Successful result
    /// </summary>
    Ok /* 0 */,
    /// <summary>
    /// SQL error or missing database
    /// </summary>
    Error /* 1 */,
    /// <summary>
    /// Internal logic error in SQLite
    /// </summary>
    Internal /* 2 */,
    /// <summary>
    /// Access permission denied
    /// </summary>
    Perm /* 3 */,
    /// <summary>
    /// Callback routine requested an abort
    /// </summary>
    Abort /* 4 */,
    /// <summary>
    /// The database file is locked
    /// </summary>
    Busy /* 5 */,
    /// <summary>
    /// A table in the database is locked
    /// </summary>
    Locked /* 6 */,
    /// <summary>
    /// A malloc() failed
    /// </summary>
    NoMem /* 7 */,
    /// <summary>
    /// Attempt to write a readonly database
    /// </summary>
    ReadOnly /* 8 */,
    /// <summary>
    /// Operation terminated by sqlite3_interrupt()
    /// </summary>
    Interrupt /* 9 */,
    /// <summary>
    /// Some kind of disk I/O error occurred
    /// </summary>
    IoErr /* 10 */,
    /// <summary>
    /// The database disk image is malformed
    /// </summary>
    Corrupt /* 11 */,
    /// <summary>
    /// Unknown opcode in sqlite3_file_control()
    /// </summary>
    NotFound /* 12 */,
    /// <summary>
    /// Insertion failed because database is full
    /// </summary>
    Full /* 13 */,
    /// <summary>
    /// Unable to open the database file
    /// </summary>
    CantOpen /* 14 */,
    /// <summary>
    /// Database lock protocol error
    /// </summary>
    Protocol /* 15 */,
    /// <summary>
    /// Database is empty
    /// </summary>
    Empty /* 16 */,
    /// <summary>
    /// The database schema changed
    /// </summary>
    Schema /* 17 */,
    /// <summary>
    /// String or BLOB exceeds size limit
    /// </summary>
    TooBig /* 18 */,
    /// <summary>
    /// Abort due to constraint violation
    /// </summary>
    Constraint /* 19 */,
    /// <summary>
    /// Data type mismatch
    /// </summary>
    Mismatch /* 20 */,
    /// <summary>
    /// Library used incorrectly
    /// </summary>
    Misuse /* 21 */,
    /// <summary>
    /// Uses OS features not supported on host
    /// </summary>
    NoLfs /* 22 */,
    /// <summary>
    /// Authorization denied
    /// </summary>
    Auth /* 23 */,
    /// <summary>
    /// Auxiliary database format error
    /// </summary>
    Format /* 24 */,
    /// <summary>
    /// 2nd parameter to sqlite3_bind out of range
    /// </summary>
    Range /* 25 */,
    /// <summary>
    /// File opened that is not a database file
    /// </summary>
    NotADb /* 26 */,
    /// <summary>
    /// sqlite3_step() has another row ready
    /// </summary>
    Row = 100,
    /// <summary>
    /// sqlite3_step() has finished executing
    /// </summary>
    Done /* 101 */
  }
}
