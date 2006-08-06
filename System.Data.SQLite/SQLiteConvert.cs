﻿/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

namespace System.Data.SQLite
{
  using System;
  using System.Runtime.InteropServices;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Globalization;
  using System.Text;

#if !PLATFORM_COMPACTFRAMEWORK 
  using System.ComponentModel.Design;
#endif

  /// <summary>
  /// SQLite has very limited types, and is inherently text-based.  The first 5 types below represent the sum of all types SQLite
  /// understands.  The DateTime extension to the spec is for internal use only.
  /// </summary>
  public enum TypeAffinity
  {
    /// <summary>
    /// Not used
    /// </summary>
    Uninitialized = 0,
    /// <summary>
    /// All integers in SQLite default to Int64
    /// </summary>
    Int64 = 1,
    /// <summary>
    /// All floating point numbers in SQLite default to double
    /// </summary>
    Double = 2,
    /// <summary>
    /// The default data type of SQLite is text
    /// </summary>
    Text = 3,
    /// <summary>
    /// Typically blob types are only seen when returned from a function
    /// </summary>
    Blob = 4,
    /// <summary>
    /// Null types can be returned from functions
    /// </summary>
    Null = 5,
    /// <summary>
    /// Used internally by this provider
    /// </summary>
    DateTime = 10,
    /// <summary>
    /// Used internally
    /// </summary>
    None = 11,
  }

  /// <summary>
  /// This implementation of SQLite for ADO.NET can process date/time fields in databases in only one of two formats.  Ticks and ISO8601.
  /// Ticks is inherently more accurate, but less compatible with 3rd party tools that query the database, and renders the DateTime field
  /// unreadable without post-processing.
  /// ISO8601 is more compatible, readable, fully-processable, but less accurate as it doesn't provide time down to fractions of a second.
  /// </summary>
  public enum SQLiteDateFormats
  {
    /// <summary>
    /// Using ticks is more accurate but less compatible with other viewers and utilities that access your database.
    /// </summary>
    Ticks = 0,
    /// <summary>
    /// The default format for this provider.
    /// </summary>
    ISO8601 = 1,
  }

  /// <summary>
  /// Struct used internally to determine the datatype of a column in a resultset
  /// </summary>
  internal struct SQLiteType
  {
    /// <summary>
    /// The DbType of the column, or DbType.Object if it cannot be determined
    /// </summary>
    internal DbType Type;
    /// <summary>
    /// The affinity of a column, used for expressions or when Type is DbType.Object
    /// </summary>
    internal TypeAffinity Affinity;
  }

  internal struct SQLiteTypeNames
  {
    internal SQLiteTypeNames(string newtypeName, DbType newdataType)
    {
      typeName = newtypeName;
      dataType = newdataType;
    }

    internal string typeName;
    internal DbType dataType;
  }

  /// <summary>
  /// This base class provides datatype conversion services for the SQLite provider.
  /// </summary>
  public abstract class SQLiteConvert
  {
    /// <summary>
    /// An array of ISO8601 datetime formats we support conversion from
    /// </summary>
    private static string[] _datetimeFormats = new string[] {
      "yyyy-MM-dd HH:mm:ss.fffffff",
      "yyyy-MM-dd HH:mm:ss",
      "yyyy-MM-dd HH:mm",                               
      "yyyyMMddHHmmss",
      "yyyyMMddHHmm",
      "yyyyMMddTHHmmssfffffff",
      "yyyy-MM-dd",
      "yy-MM-dd",
      "yyyyMMdd",
      "HH:mm:ss",
      "HH:mm",
      "THHmmss",
      "THHmm",
      "yyyy-MM-dd HH:mm:ss.fff",
      "yyyy-MM-ddTHH:mm",
      "yyyy-MM-ddTHH:mm:ss",
      "yyyy-MM-ddTHH:mm:ss.fff",
      "yyyy-MM-ddTHH:mm:ss.ffffff",
      "HH:mm:ss.fff"
    };

    /// <summary>
    /// An UTF-8 Encoding instance, so we can convert strings to and from UTF-8
    /// </summary>
    private Encoding _utf8 = new UTF8Encoding();
    /// <summary>
    /// The default DateTime format for this instance
    /// </summary>
    private SQLiteDateFormats _datetimeFormat;
    /// <summary>
    /// Initializes the conversion class
    /// </summary>
    /// <param name="fmt">The default date/time format to use for this instance</param>
    internal SQLiteConvert(SQLiteDateFormats fmt)
    {
      _datetimeFormat = fmt;
    }

    #region UTF-8 Conversion Functions
    /// <summary>
    /// Converts a string to a UTF-8 encoded byte array sized to include a null-terminating character.
    /// </summary>
    /// <param name="sourceText">The string to convert to UTF-8</param>
    /// <returns>A byte array containing the converted string plus an extra 0 terminating byte at the end of the array.</returns>
    public byte[] ToUTF8(string sourceText)
    {
      Byte[] byteArray;
      int nlen = _utf8.GetByteCount(sourceText) + 1;

      byteArray = new byte[nlen];
      nlen = _utf8.GetBytes(sourceText, 0, sourceText.Length, byteArray, 0);
      byteArray[nlen] = 0;

      return byteArray;
    }

    /// <summary>
    /// Convert a DateTime to a UTF-8 encoded, zero-terminated byte array.
    /// </summary>
    /// <remarks>
    /// This function is a convenience function, which first calls ToString() on the DateTime, and then calls ToUTF8() with the
    /// string result.
    /// </remarks>
    /// <param name="dateTimeValue">The DateTime to convert.</param>
    /// <returns>The UTF-8 encoded string, including a 0 terminating byte at the end of the array.</returns>
    public byte[] ToUTF8(DateTime dateTimeValue)
    {
      return ToUTF8(ToString(dateTimeValue));
    }

    /// <summary>
    /// Converts a UTF-8 encoded IntPtr of the specified length into a .NET string
    /// </summary>
    /// <param name="nativestring">The pointer to the memory where the UTF-8 string is encoded</param>
    /// <param name="nativestringlen">The number of bytes to decode</param>
    /// <returns>A string containing the translated character(s)</returns>
    public virtual string ToString(IntPtr nativestring, int nativestringlen)
    {
      return UTF8ToString(nativestring, nativestringlen);
    }

    /// <summary>
    /// Converts a UTF-8 encoded IntPtr of the specified length into a .NET string
    /// </summary>
    /// <param name="nativestring">The pointer to the memory where the UTF-8 string is encoded</param>
    /// <param name="nativestringlen">The number of bytes to decode</param>
    /// <returns>A string containing the translated character(s)</returns>
    public virtual string UTF8ToString(IntPtr nativestring, int nativestringlen)
    {
      if (nativestringlen == 0) return "";

      byte[] byteArray = new byte[nativestringlen];

      Marshal.Copy(nativestring, byteArray, 0, nativestringlen);

      return _utf8.GetString(byteArray, 0, nativestringlen);
    }


    #endregion

    #region DateTime Conversion Functions
    /// <summary>
    /// Converts a string into a DateTime, using the current DateTimeFormat specified for the connection when it was opened.
    /// </summary>
    /// <remarks>
    /// Acceptable ISO8601 DateTime formats are:
    ///   yyyy-MM-dd HH:mm:ss
    ///   yyyyMMddHHmmss
    ///   yyyyMMddTHHmmssfffffff
    ///   yyyy-MM-dd
    ///   yy-MM-dd
    ///   yyyyMMdd
    ///   HH:mm:ss
    ///   THHmmss
    /// </remarks>
    /// <param name="dateText">The string containing either a Tick value or an ISO8601-format string</param>
    /// <returns>A DateTime value</returns>
    public DateTime ToDateTime(string dateText)
    {
      switch (_datetimeFormat)
      {
        case SQLiteDateFormats.Ticks:
          return new DateTime(Convert.ToInt64(dateText, CultureInfo.InvariantCulture));
        default:
          return DateTime.ParseExact(dateText, _datetimeFormats, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None);
      }
    }

    /// <summary>
    /// Converts a DateTime to a string value, using the current DateTimeFormat specified for the connection when it was opened.
    /// </summary>
    /// <param name="dateValue">The DateTime value to convert</param>
    /// <returns>Either a string consisting of the tick count for DateTimeFormat.Ticks, or a date/time in ISO8601 format.</returns>
    public string ToString(DateTime dateValue)
    {
      switch (_datetimeFormat)
      {
        case SQLiteDateFormats.Ticks:
          return dateValue.Ticks.ToString(CultureInfo.InvariantCulture);
        default:
          return dateValue.ToString(_datetimeFormats[0], CultureInfo.InvariantCulture);
      }
    }

    /// <summary>
    /// Internal function to convert a UTF-8 encoded IntPtr of the specified length to a DateTime.
    /// </summary>
    /// <remarks>
    /// This is a convenience function, which first calls ToString() on the IntPtr to convert it to a string, then calls
    /// ToDateTime() on the string to return a DateTime.
    /// </remarks>
    /// <param name="ptr">A pointer to the UTF-8 encoded string</param>
    /// <param name="len">The length in bytes of the string</param>
    /// <returns>The parsed DateTime value</returns>
    internal DateTime ToDateTime(IntPtr ptr, int len)
    {
      return ToDateTime(ToString(ptr, len));
    }
    #endregion

    /// <summary>
    /// Smart method of splitting a string.  Skips quoted elements, removes the quotes.
    /// </summary>
    /// <remarks>
    /// This split function works somewhat like the String.Split() function in that it breaks apart a string into
    /// pieces and returns the pieces as an array.  The primary differences are:
    /// <list type="bullet">
    /// <item><description>Only one character can be provided as a separator character</description></item>
    /// <item><description>Quoted text inside the string is skipped over when searching for the separator, and the quotes are removed.</description></item>
    /// </list>
    /// Thus, if splitting the following string looking for a comma:<br/>
    /// One,Two, "Three, Four", Five<br/>
    /// <br/>
    /// The resulting array would contain<br/>
    /// [0] One<br/>
    /// [1] Two<br/>
    /// [2] Three, Four<br/>
    /// [3] Five<br/>
    /// <br/>
    /// Note that the leading and trailing spaces were removed from each item during the split.
    /// </remarks>
    /// <param name="source">Source string to split apart</param>
    /// <param name="separator">Separator character</param>
    /// <returns>A string array of the split up elements</returns>
    public static string[] Split(string source, char separator)
    {
      char[] toks = new char[2] { '\"', separator };
      char[] quot = new char[1] { '\"' };
      int n = 0;
      List<string> ls = new List<string>();
      string s;

      while (source.Length > 0)
      {
        n = source.IndexOfAny(toks, n);
        if (n == -1) break;
        if (source[n] == toks[0])
        {
          source = source.Remove(n, 1);
          n = source.IndexOfAny(quot, n);
          if (n == -1)
          {
            source = "\"" + source;
            break;
          }
          source = source.Remove(n, 1);
        }
        else
        {
          s = source.Substring(0, n).Trim();
          source = source.Substring(n + 1).Trim();
          if (s.Length > 0) ls.Add(s);
          n = 0;
        }
      }
      if (source.Length > 0) ls.Add(source);

      string[] ar = new string[ls.Count];
      ls.CopyTo(ar, 0);

      return ar;
    }

    #region Type Conversions
    /// <summary>
    /// Determines the data type of a column in a statement
    /// </summary>
    /// <param name="stmt">The statement to retrieve information for</param>
    /// <param name="i">The column to retrieve type information on</param>
    /// <returns>Returns a SQLiteType struct</returns>
    internal static SQLiteType ColumnToType(SQLiteStatement stmt, int i)
    {
      SQLiteType typ;

      typ.Type = TypeNameToDbType(stmt._sql.ColumnType(stmt, i, out typ.Affinity));

      return typ;
    }

    /// <summary>
    /// Converts a SQLiteType to a .NET Type object
    /// </summary>
    /// <param name="t">The SQLiteType to convert</param>
    /// <returns>Returns a .NET Type object</returns>
    internal static Type SQLiteTypeToType(SQLiteType t)
    {
      if (t.Type != DbType.Object)
        return SQLiteConvert.DbTypeToType(t.Type);

      return _typeaffinities[(int)t.Affinity];
    }

    static Type[] _typeaffinities = {
      null,
      typeof(Int64),
      typeof(Double),
      typeof(string),
      typeof(byte[]),
      typeof(DBNull),
      null,
      null,
      null,
      null,
      typeof(DateTime),
      null,
    };

    /// <summary>
    /// For a given intrinsic type, return a DbType
    /// </summary>
    /// <param name="typ">The native type to convert</param>
    /// <returns>The corresponding (closest match) DbType</returns>
    internal static DbType TypeToDbType(Type typ)
    {
      TypeCode tc = Type.GetTypeCode(typ);
      if (tc == TypeCode.Object)
      {
        if (typ == typeof(byte[])) return DbType.Binary;
        if (typ == typeof(Guid)) return DbType.Guid;
        return DbType.String;
      }
      return _typetodbtype[(int)tc];
    }

    private static DbType[] _typetodbtype = {
      DbType.Object,
      DbType.Binary,
      DbType.Object,
      DbType.Boolean,
      DbType.SByte,
      DbType.SByte,
      DbType.Byte,
      DbType.Int16, // 7
      DbType.UInt16,
      DbType.Int32,
      DbType.UInt32,
      DbType.Int64, // 11
      DbType.UInt64,
      DbType.Single,
      DbType.Double,
      DbType.Decimal,
      DbType.DateTime,
      DbType.Object,
      DbType.String,
    };

    /// <summary>
    /// Returns the ColumnSize for the given DbType
    /// </summary>
    /// <param name="typ">The DbType to get the size of</param>
    /// <returns></returns>
    internal static int DbTypeToColumnSize(DbType typ)
    {
      return _dbtypetocolumnsize[(int)typ];
    }

    private static int[] _dbtypetocolumnsize = {
      2147483647,   // 0
      2147483647,   // 1
      1,     // 2
      1,     // 3
      8,  // 4
      8, // 5
      8, // 6
      8,  // 7
      8,   // 8
      16,     // 9
      2,
      4,
      8,
      2147483647,
      1,
      4,
      2147483647,
      8,
      2,
      4,
      8,
      8,
      2147483647,
      2147483647,
      2147483647,
      2147483647,   // 25 (Xml)
    };

    /// <summary>
    /// Convert a DbType to a Type
    /// </summary>
    /// <param name="typ">The DbType to convert from</param>
    /// <returns>The closest-match .NET type</returns>
    internal static Type DbTypeToType(DbType typ)
    {
      return _dbtypeToType[(int)typ];
    }

    private static Type[] _dbtypeToType = {
      typeof(string),   // 0
      typeof(byte[]),   // 1
      typeof(byte),     // 2
      typeof(bool),     // 3
      typeof(decimal),  // 4
      typeof(DateTime), // 5
      typeof(DateTime), // 6
      typeof(decimal),  // 7
      typeof(double),   // 8
      typeof(Guid),     // 9
      typeof(Int16),
      typeof(Int32),
      typeof(Int64),
      typeof(object),
      typeof(sbyte),
      typeof(float),
      typeof(string),
      typeof(DateTime),
      typeof(UInt16),
      typeof(UInt32),
      typeof(UInt64),
      typeof(double),
      typeof(string),
      typeof(string),
      typeof(string),
      typeof(string),   // 25 (Xml)
    };

    /// <summary>
    /// For a given type, return the closest-match SQLite TypeAffinity, which only understands a very limited subset of types.
    /// </summary>
    /// <param name="typ">The type to evaluate</param>
    /// <returns>The SQLite type affinity for that type.</returns>
    internal static TypeAffinity TypeToAffinity(Type typ)
    {
      TypeCode tc = Type.GetTypeCode(typ);
      if (tc == TypeCode.Object)
      {
        if (typ == typeof(byte[]) || typ == typeof(Guid))
          return TypeAffinity.Blob;
        else
          return TypeAffinity.Text;
      }
      return _typecodeAffinities[(int)tc];
    }

    private static TypeAffinity[] _typecodeAffinities = {
      TypeAffinity.Null,
      TypeAffinity.Blob,
      TypeAffinity.Null,
      TypeAffinity.Int64,
      TypeAffinity.Int64,
      TypeAffinity.Int64,
      TypeAffinity.Int64,
      TypeAffinity.Int64, // 7
      TypeAffinity.Int64,
      TypeAffinity.Int64,
      TypeAffinity.Int64,
      TypeAffinity.Int64, // 11
      TypeAffinity.Int64,
      TypeAffinity.Double,
      TypeAffinity.Double,
      TypeAffinity.Double,
      TypeAffinity.DateTime,
      TypeAffinity.Null,
      TypeAffinity.Text,
    };

    /// <summary>
    /// For a given type name, return a closest-match .NET type
    /// </summary>
    /// <param name="Name">The name of the type to match</param>
    /// <returns>The .NET DBType the text evaluates to.</returns>
    internal static DbType TypeNameToDbType(string Name)
    {
      if (String.IsNullOrEmpty(Name)) return DbType.Object;

      int x = _typeNames.Length;
      for (int n = 0; n < x; n++)
      {
        if (String.Compare(Name, 0, _typeNames[n].typeName, 0, _typeNames[n].typeName.Length, true, CultureInfo.InvariantCulture) == 0)
          return _typeNames[n].dataType; 
      }
      return DbType.Object;
    }
    #endregion

    private static SQLiteTypeNames[] _typeNames = {
      new SQLiteTypeNames("COUNTER", DbType.Int64),
      new SQLiteTypeNames("AUTOINCREMENT", DbType.Int64),
      new SQLiteTypeNames("IDENTITY", DbType.Int64),
      new SQLiteTypeNames("LONGTEXT", DbType.String),
      new SQLiteTypeNames("LONGCHAR", DbType.String),
      new SQLiteTypeNames("LONG", DbType.Int64),
      new SQLiteTypeNames("TINYINT", DbType.Byte),
      new SQLiteTypeNames("INTEGER", DbType.Int64),
      new SQLiteTypeNames("INT", DbType.Int32),
      new SQLiteTypeNames("VARCHAR", DbType.String),
      new SQLiteTypeNames("NVARCHAR", DbType.String),
      new SQLiteTypeNames("CHAR", DbType.String),
      new SQLiteTypeNames("NCHAR", DbType.String),
      new SQLiteTypeNames("TEXT", DbType.String),
      new SQLiteTypeNames("NTEXT", DbType.String),
      new SQLiteTypeNames("STRING", DbType.String),
      new SQLiteTypeNames("DOUBLE", DbType.Double),
      new SQLiteTypeNames("FLOAT", DbType.Double),
      new SQLiteTypeNames("REAL", DbType.Single),          
      new SQLiteTypeNames("BIT", DbType.Boolean),
      new SQLiteTypeNames("YESNO", DbType.Boolean),
      new SQLiteTypeNames("LOGICAL", DbType.Boolean),
      new SQLiteTypeNames("BOOL", DbType.Boolean),
      new SQLiteTypeNames("NUMERIC", DbType.Decimal),
      new SQLiteTypeNames("DECIMAL", DbType.Decimal),
      new SQLiteTypeNames("MONEY", DbType.Decimal),
      new SQLiteTypeNames("CURRENCY", DbType.Decimal),
      new SQLiteTypeNames("TIME", DbType.DateTime),
      new SQLiteTypeNames("DATE", DbType.DateTime),
      new SQLiteTypeNames("BLOB", DbType.Binary),
      new SQLiteTypeNames("BINARY", DbType.Binary),
      new SQLiteTypeNames("VARBINARY", DbType.Binary),
      new SQLiteTypeNames("IMAGE", DbType.Binary),
      new SQLiteTypeNames("GENERAL", DbType.Binary),
      new SQLiteTypeNames("OLEOBJECT", DbType.Binary),
      new SQLiteTypeNames("GUID", DbType.Guid),
      new SQLiteTypeNames("UNIQUEIDENTIFIER", DbType.Guid),
      new SQLiteTypeNames("MEMO", DbType.String),
      new SQLiteTypeNames("NOTE", DbType.String),
      new SQLiteTypeNames("SMALLINT", DbType.Int16),
      new SQLiteTypeNames("BIGINT", DbType.Int64),
    };
  }
}
