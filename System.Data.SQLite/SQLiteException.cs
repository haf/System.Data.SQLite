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
  using System.Reflection;
  using System.Runtime.Serialization;
  using System.Security.Permissions;
#endif

  /// <summary>
  /// SQLite exception class.
  /// </summary>
#if !PLATFORM_COMPACTFRAMEWORK
  [Serializable()]
  public sealed class SQLiteException : DbException, ISerializable
#else
  public sealed class SQLiteException : Exception
#endif
  {
    private SQLiteErrorCode _errorCode;

#if !PLATFORM_COMPACTFRAMEWORK
    /// <summary>
    /// Private constructor for use with serialization.
    /// </summary>
    /// <param name="info">
    /// Holds the serialized object data about the exception being thrown.
    /// </param>
    /// <param name="context">
    /// Contains contextual information about the source or destination.
    /// </param>
    private SQLiteException(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
      _errorCode = (SQLiteErrorCode)info.GetInt32("errorCode");
    }
#endif

    /// <summary>
    /// Public constructor for generating a SQLite exception given the error
    /// code and message.
    /// </summary>
    /// <param name="errorCode">
    /// The SQLite return code to report.
    /// </param>
    /// <param name="message">
    /// Message text to go along with the return code message text.
    /// </param>
    public SQLiteException(SQLiteErrorCode errorCode, string message)
      : base(GetStockErrorMessage(errorCode, message))
    {
      _errorCode = errorCode;
    }

    /// <summary>
    /// Public constructor that uses the base class constructor for the error
    /// message.
    /// </summary>
    /// <param name="message">Error message text.</param>
    public SQLiteException(string message)
      : base(message)
    {
    }

    /// <summary>
    /// Public constructor that uses the default base class constructor.
    /// </summary>
    public SQLiteException()
    {
    }

    /// <summary>
    /// Public constructor that uses the base class constructor for the error
    /// message and inner exception.
    /// </summary>
    /// <param name="message">Error message text.</param>
    /// <param name="innerException">The original (inner) exception.</param>
    public SQLiteException(string message, Exception innerException)
      : base(message, innerException)
    {
    }

#if !PLATFORM_COMPACTFRAMEWORK
    /// <summary>
    /// Adds extra information to the serialized object data specific to this
    /// class type.  This is only used for serialization.
    /// </summary>
    /// <param name="info">
    /// Holds the serialized object data about the exception being thrown.
    /// </param>
    /// <param name="context">
    /// Contains contextual information about the source or destination.
    /// </param>
    [SecurityPermission(
      SecurityAction.LinkDemand,
      Flags = SecurityPermissionFlag.SerializationFormatter)]
    public override void GetObjectData(
      SerializationInfo info,
      StreamingContext context)
    {
      if (info != null)
        info.AddValue("errorCode", _errorCode);

      base.GetObjectData(info, context);
    }
#endif

    /// <summary>
    /// Gets the associated SQLite return code for this exception as a
    /// <see cref="SQLiteErrorCode" />.  This property returns the same
    /// underlying value as the <see cref="ErrorCode" /> property.
    /// </summary>
    public SQLiteErrorCode ReturnCode
    {
      get { return _errorCode; }
    }

    /// <summary>
    /// Gets the associated SQLite return code for this exception as an
    /// <see cref="Int32" />.  For desktop versions of the .NET Framework,
    /// this property overrides the property of the same name within the
    /// <see cref="System.Runtime.InteropServices.ExternalException" />
    /// class.  This property returns the same underlying value as the
    /// <see cref="ReturnCode" /> property.
    /// </summary>
#if !PLATFORM_COMPACTFRAMEWORK
    public override int ErrorCode
#else
    public int ErrorCode
#endif
    {
      get { return (int)_errorCode; }
    }

    /// <summary>
    /// Returns the error message for the specified SQLite return code.
    /// </summary>
    /// <param name="errorCode">The SQLite return code.</param>
    /// <returns>The error message or null if it cannot be found.</returns>
    private static string GetErrorString(
        SQLiteErrorCode errorCode
        )
    {
#if !PLATFORM_COMPACTFRAMEWORK
        //
        // HACK: This must be done via reflection in order to prevent
        //       the RuntimeHelpers.PrepareDelegate method from over-
        //       eagerly attempting to locate the new (and optional)
        //       sqlite3_errstr() function in the SQLite core library
        //       because it happens to be in the static call graph for
        //       the AppDomain.DomainUnload event handler registered
        //       by the SQLiteLog class.
        //
        BindingFlags flags = BindingFlags.Static |
            BindingFlags.NonPublic | BindingFlags.InvokeMethod;

        return typeof(SQLiteBase).InvokeMember("GetErrorString",
            flags, null, null, new object[] { errorCode }) as string;
#else
        return SQLiteBase.GetErrorString(errorCode);
#endif
    }

    /// <summary>
    /// Returns the composite error message based on the SQLite return code
    /// and the optional detailed error message.
    /// </summary>
    /// <param name="errorCode">The SQLite return code.</param>
    /// <param name="message">Optional detailed error message.</param>
    /// <returns>Error message text for the return code.</returns>
    private static string GetStockErrorMessage(
        SQLiteErrorCode errorCode,
        string message
        )
    {
        return String.Format("{0}{1}{2}",
            GetErrorString(errorCode),
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
