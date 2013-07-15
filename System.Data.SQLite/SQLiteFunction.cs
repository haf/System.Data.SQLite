/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 *
 * Released to the public domain, use at your own risk!
 ********************************************************/

namespace System.Data.SQLite
{
  using System;
  using System.Collections.Generic;
  using System.Runtime.InteropServices;
  using System.Globalization;

  /// <summary>
  /// This abstract class is designed to handle user-defined functions easily.  An instance of the derived class is made for each
  /// connection to the database.
  /// </summary>
  /// <remarks>
  /// Although there is one instance of a class derived from SQLiteFunction per database connection, the derived class has no access
  /// to the underlying connection.  This is necessary to deter implementers from thinking it would be a good idea to make database
  /// calls during processing.
  ///
  /// It is important to distinguish between a per-connection instance, and a per-SQL statement context.  One instance of this class
  /// services all SQL statements being stepped through on that connection, and there can be many.  One should never store per-statement
  /// information in member variables of user-defined function classes.
  ///
  /// For aggregate functions, always create and store your per-statement data in the contextData object on the 1st step.  This data will
  /// be automatically freed for you (and Dispose() called if the item supports IDisposable) when the statement completes.
  /// </remarks>
  public abstract class SQLiteFunction : IDisposable
  {
    private class AggregateData
    {
      internal int _count = 1;
      internal object _data;
    }

    /////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// The base connection this function is attached to
    /// </summary>
    internal SQLiteBase              _base;

    /// <summary>
    /// Internal array used to keep track of aggregate function context data
    /// </summary>
    private Dictionary<IntPtr, AggregateData> _contextDataList;

    /// <summary>
    /// The connection flags associated with this object (this should be the
    /// same value as the flags associated with the parent connection object).
    /// </summary>
    private SQLiteConnectionFlags _flags;

    /// <summary>
    /// Holds a reference to the callback function for user functions
    /// </summary>
    private SQLiteCallback  _InvokeFunc;
    /// <summary>
    /// Holds a reference to the callbakc function for stepping in an aggregate function
    /// </summary>
    private SQLiteCallback  _StepFunc;
    /// <summary>
    /// Holds a reference to the callback function for finalizing an aggregate function
    /// </summary>
    private SQLiteFinalCallback  _FinalFunc;
    /// <summary>
    /// Holds a reference to the callback function for collation sequences
    /// </summary>
    private SQLiteCollation _CompareFunc;

    private SQLiteCollation _CompareFunc16;

    /// <summary>
    /// Current context of the current callback.  Only valid during a callback
    /// </summary>
    internal IntPtr _context;

    /// <summary>
    /// This static list contains all the user-defined functions declared using the proper attributes.
    /// </summary>
    private static List<SQLiteFunctionAttribute> _registeredFunctions;

    /// <summary>
    /// Internal constructor, initializes the function's internal variables.
    /// </summary>
    protected SQLiteFunction()
    {
      _contextDataList = new Dictionary<IntPtr, AggregateData>();
    }

    /// <summary>
    /// Constructs an instance of this class using the specified data-type
    /// conversion parameters.
    /// </summary>
    /// <param name="format">
    /// The DateTime format to be used when converting string values to a
    /// DateTime and binding DateTime parameters.
    /// </param>
    /// <param name="kind">
    /// The <see cref="DateTimeKind" /> to be used when creating DateTime
    /// values.
    /// </param>
    /// <param name="formatString">
    /// The format string to be used when parsing and formatting DateTime
    /// values.
    /// </param>
    /// <param name="utf16">
    /// Non-zero to create a UTF-16 data-type conversion context; otherwise,
    /// a UTF-8 data-type conversion context will be created.
    /// </param>
    protected SQLiteFunction(
        SQLiteDateFormats format,
        DateTimeKind kind,
        string formatString,
        bool utf16
        )
        : this()
    {
        if (utf16)
            _base = new SQLite3_UTF16(format, kind, formatString, IntPtr.Zero, null, false);
        else
            _base = new SQLite3(format, kind, formatString, IntPtr.Zero, null, false);
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    #region IDisposable Members
    /// <summary>
    /// Disposes of any active contextData variables that were not automatically cleaned up.  Sometimes this can happen if
    /// someone closes the connection while a DataReader is open.
    /// </summary>
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
            throw new ObjectDisposedException(typeof(SQLiteFunction).Name);
#endif
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Placeholder for a user-defined disposal routine
    /// </summary>
    /// <param name="disposing">True if the object is being disposed explicitly</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                ////////////////////////////////////
                // dispose managed resources here...
                ////////////////////////////////////

                IDisposable disp;

                foreach (KeyValuePair<IntPtr, AggregateData> kv in _contextDataList)
                {
                    disp = kv.Value._data as IDisposable;
                    if (disp != null)
                        disp.Dispose();
                }
                _contextDataList.Clear();
                _contextDataList = null;

                _flags = SQLiteConnectionFlags.None;

                _InvokeFunc = null;
                _StepFunc = null;
                _FinalFunc = null;
                _CompareFunc = null;
                _base = null;
            }

            //////////////////////////////////////
            // release unmanaged resources here...
            //////////////////////////////////////

            disposed = true;
        }
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////

    #region Destructor
    ~SQLiteFunction()
    {
        Dispose(false);
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Returns a reference to the underlying connection's SQLiteConvert class, which can be used to convert
    /// strings and DateTime's into the current connection's encoding schema.
    /// </summary>
    public SQLiteConvert SQLiteConvert
    {
      get
      {
        CheckDisposed();
        return _base;
      }
    }

    /// <summary>
    /// Scalar functions override this method to do their magic.
    /// </summary>
    /// <remarks>
    /// Parameters passed to functions have only an affinity for a certain data type, there is no underlying schema available
    /// to force them into a certain type.  Therefore the only types you will ever see as parameters are
    /// DBNull.Value, Int64, Double, String or byte[] array.
    /// </remarks>
    /// <param name="args">The arguments for the command to process</param>
    /// <returns>You may return most simple types as a return value, null or DBNull.Value to return null, DateTime, or
    /// you may return an Exception-derived class if you wish to return an error to SQLite.  Do not actually throw the error,
    /// just return it!</returns>
    public virtual object Invoke(object[] args)
    {
      CheckDisposed();
      return null;
    }

    /// <summary>
    /// Aggregate functions override this method to do their magic.
    /// </summary>
    /// <remarks>
    /// Typically you'll be updating whatever you've placed in the contextData field and returning as quickly as possible.
    /// </remarks>
    /// <param name="args">The arguments for the command to process</param>
    /// <param name="stepNumber">The 1-based step number.  This is incrememted each time the step method is called.</param>
    /// <param name="contextData">A placeholder for implementers to store contextual data pertaining to the current context.</param>
    public virtual void Step(object[] args, int stepNumber, ref object contextData)
    {
      CheckDisposed();
    }

    /// <summary>
    /// Aggregate functions override this method to finish their aggregate processing.
    /// </summary>
    /// <remarks>
    /// If you implemented your aggregate function properly,
    /// you've been recording and keeping track of your data in the contextData object provided, and now at this stage you should have
    /// all the information you need in there to figure out what to return.
    /// NOTE:  It is possible to arrive here without receiving a previous call to Step(), in which case the contextData will
    /// be null.  This can happen when no rows were returned.  You can either return null, or 0 or some other custom return value
    /// if that is the case.
    /// </remarks>
    /// <param name="contextData">Your own assigned contextData, provided for you so you can return your final results.</param>
    /// <returns>You may return most simple types as a return value, null or DBNull.Value to return null, DateTime, or
    /// you may return an Exception-derived class if you wish to return an error to SQLite.  Do not actually throw the error,
    /// just return it!
    /// </returns>
    public virtual object Final(object contextData)
    {
      CheckDisposed();
      return null;
    }

    /// <summary>
    /// User-defined collation sequences override this method to provide a custom string sorting algorithm.
    /// </summary>
    /// <param name="param1">The first string to compare</param>
    /// <param name="param2">The second strnig to compare</param>
    /// <returns>1 if param1 is greater than param2, 0 if they are equal, or -1 if param1 is less than param2</returns>
    public virtual int Compare(string param1, string param2)
    {
      CheckDisposed();
      return 0;
    }

    /// <summary>
    /// Converts an IntPtr array of context arguments to an object array containing the resolved parameters the pointers point to.
    /// </summary>
    /// <remarks>
    /// Parameters passed to functions have only an affinity for a certain data type, there is no underlying schema available
    /// to force them into a certain type.  Therefore the only types you will ever see as parameters are
    /// DBNull.Value, Int64, Double, String or byte[] array.
    /// </remarks>
    /// <param name="nArgs">The number of arguments</param>
    /// <param name="argsptr">A pointer to the array of arguments</param>
    /// <returns>An object array of the arguments once they've been converted to .NET values</returns>
    internal object[] ConvertParams(int nArgs, IntPtr argsptr)
    {
      object[] parms = new object[nArgs];
#if !PLATFORM_COMPACTFRAMEWORK
      IntPtr[] argint = new IntPtr[nArgs];
#else
      int[] argint = new int[nArgs];
#endif
      Marshal.Copy(argsptr, argint, 0, nArgs);

      for (int n = 0; n < nArgs; n++)
      {
        switch (_base.GetParamValueType((IntPtr)argint[n]))
        {
          case TypeAffinity.Null:
            parms[n] = DBNull.Value;
            break;
          case TypeAffinity.Int64:
            parms[n] = _base.GetParamValueInt64((IntPtr)argint[n]);
            break;
          case TypeAffinity.Double:
            parms[n] = _base.GetParamValueDouble((IntPtr)argint[n]);
            break;
          case TypeAffinity.Text:
            parms[n] = _base.GetParamValueText((IntPtr)argint[n]);
            break;
          case TypeAffinity.Blob:
            {
              int x;
              byte[] blob;

              x = (int)_base.GetParamValueBytes((IntPtr)argint[n], 0, null, 0, 0);
              blob = new byte[x];
              _base.GetParamValueBytes((IntPtr)argint[n], 0, blob, 0, x);
              parms[n] = blob;
            }
            break;
          case TypeAffinity.DateTime: // Never happens here but what the heck, maybe it will one day.
            parms[n] = _base.ToDateTime(_base.GetParamValueText((IntPtr)argint[n]));
            break;
        }
      }
      return parms;
    }

    /// <summary>
    /// Takes the return value from Invoke() and Final() and figures out how to return it to SQLite's context.
    /// </summary>
    /// <param name="context">The context the return value applies to</param>
    /// <param name="returnValue">The parameter to return to SQLite</param>
    private void SetReturnValue(IntPtr context, object returnValue)
    {
      if (returnValue == null || returnValue == DBNull.Value)
      {
        _base.ReturnNull(context);
        return;
      }

      Type t = returnValue.GetType();
      if (t == typeof(DateTime))
      {
        _base.ReturnText(context, _base.ToString((DateTime)returnValue));
        return;
      }
      else
      {
        Exception r = returnValue as Exception;

        if (r != null)
        {
          _base.ReturnError(context, r.Message);
          return;
        }
      }

      switch (SQLiteConvert.TypeToAffinity(t))
      {
        case TypeAffinity.Null:
          _base.ReturnNull(context);
          return;
        case TypeAffinity.Int64:
          _base.ReturnInt64(context, Convert.ToInt64(returnValue, CultureInfo.CurrentCulture));
          return;
        case TypeAffinity.Double:
          _base.ReturnDouble(context, Convert.ToDouble(returnValue, CultureInfo.CurrentCulture));
          return;
        case TypeAffinity.Text:
          _base.ReturnText(context, returnValue.ToString());
          return;
        case TypeAffinity.Blob:
          _base.ReturnBlob(context, (byte[])returnValue);
          return;
      }
    }

    /// <summary>
    /// Internal scalar callback function, which wraps the raw context pointer and calls the virtual Invoke() method.
    /// WARNING: Must not throw exceptions.
    /// </summary>
    /// <param name="context">A raw context pointer</param>
    /// <param name="nArgs">Number of arguments passed in</param>
    /// <param name="argsptr">A pointer to the array of arguments</param>
    internal void ScalarCallback(IntPtr context, int nArgs, IntPtr argsptr)
    {
        try
        {
            _context = context;
            SetReturnValue(context,
                Invoke(ConvertParams(nArgs, argsptr))); /* throw */
        }
        catch (Exception e) /* NOTE: Must catch ALL. */
        {
            try
            {
                if ((_flags & SQLiteConnectionFlags.LogCallbackException) ==
                        SQLiteConnectionFlags.LogCallbackException)
                {
                    SQLiteLog.LogMessage(SQLiteBase.COR_E_EXCEPTION,
                        String.Format(CultureInfo.CurrentCulture,
                        "Caught exception in \"Invoke\" method: {0}",
                        e)); /* throw */
                }
            }
            catch
            {
                // do nothing.
            }
        }
    }

    /// <summary>
    /// Internal collation sequence function, which wraps up the raw string pointers and executes the Compare() virtual function.
    /// WARNING: Must not throw exceptions.
    /// </summary>
    /// <param name="ptr">Not used</param>
    /// <param name="len1">Length of the string pv1</param>
    /// <param name="ptr1">Pointer to the first string to compare</param>
    /// <param name="len2">Length of the string pv2</param>
    /// <param name="ptr2">Pointer to the second string to compare</param>
    /// <returns>Returns -1 if the first string is less than the second.  0 if they are equal, or 1 if the first string is greater
    /// than the second.  Returns 0 if an exception is caught.</returns>
    internal int CompareCallback(IntPtr ptr, int len1, IntPtr ptr1, int len2, IntPtr ptr2)
    {
        try
        {
            return Compare(SQLiteConvert.UTF8ToString(ptr1, len1),
                SQLiteConvert.UTF8ToString(ptr2, len2)); /* throw */
        }
        catch (Exception e) /* NOTE: Must catch ALL. */
        {
            try
            {
                if ((_flags & SQLiteConnectionFlags.LogCallbackException) ==
                        SQLiteConnectionFlags.LogCallbackException)
                {
                    SQLiteLog.LogMessage(SQLiteBase.COR_E_EXCEPTION,
                        String.Format(CultureInfo.CurrentCulture,
                        "Caught exception in \"Compare\" (UTF8) method: {0}",
                        e)); /* throw */
                }
            }
            catch
            {
                // do nothing.
            }
        }

        //
        // NOTE: This must be done to prevent the core SQLite library from
        //       using our (invalid) result.
        //
        if ((_base != null) && _base.IsOpen())
            _base.Cancel();

        return 0;
    }

    /// <summary>
    /// Internal collation sequence function, which wraps up the raw string pointers and executes the Compare() virtual function.
    /// WARNING: Must not throw exceptions.
    /// </summary>
    /// <param name="ptr">Not used</param>
    /// <param name="len1">Length of the string pv1</param>
    /// <param name="ptr1">Pointer to the first string to compare</param>
    /// <param name="len2">Length of the string pv2</param>
    /// <param name="ptr2">Pointer to the second string to compare</param>
    /// <returns>Returns -1 if the first string is less than the second.  0 if they are equal, or 1 if the first string is greater
    /// than the second.  Returns 0 if an exception is caught.</returns>
    internal int CompareCallback16(IntPtr ptr, int len1, IntPtr ptr1, int len2, IntPtr ptr2)
    {
        try
        {
            return Compare(SQLite3_UTF16.UTF16ToString(ptr1, len1),
                SQLite3_UTF16.UTF16ToString(ptr2, len2)); /* throw */
        }
        catch (Exception e) /* NOTE: Must catch ALL. */
        {
            try
            {
                if ((_flags & SQLiteConnectionFlags.LogCallbackException) ==
                        SQLiteConnectionFlags.LogCallbackException)
                {
                    SQLiteLog.LogMessage(SQLiteBase.COR_E_EXCEPTION,
                        String.Format(CultureInfo.CurrentCulture,
                        "Caught exception in \"Compare\" (UTF16) method: {0}",
                        e)); /* throw */
                }
            }
            catch
            {
                // do nothing.
            }
        }

        //
        // NOTE: This must be done to prevent the core SQLite library from
        //       using our (invalid) result.
        //
        if ((_base != null) && _base.IsOpen())
            _base.Cancel();

        return 0;
    }

    /// <summary>
    /// The internal aggregate Step function callback, which wraps the raw context pointer and calls the virtual Step() method.
    /// WARNING: Must not throw exceptions.
    /// </summary>
    /// <remarks>
    /// This function takes care of doing the lookups and getting the important information put together to call the Step() function.
    /// That includes pulling out the user's contextData and updating it after the call is made.  We use a sorted list for this so
    /// binary searches can be done to find the data.
    /// </remarks>
    /// <param name="context">A raw context pointer</param>
    /// <param name="nArgs">Number of arguments passed in</param>
    /// <param name="argsptr">A pointer to the array of arguments</param>
    internal void StepCallback(IntPtr context, int nArgs, IntPtr argsptr)
    {
        try
        {
            AggregateData data = null;

            if (_base != null)
            {
                IntPtr nAux = _base.AggregateContext(context);

                if ((_contextDataList != null) &&
                    !_contextDataList.TryGetValue(nAux, out data))
                {
                    data = new AggregateData();
                    _contextDataList[nAux] = data;
                }
            }

            if (data == null)
                data = new AggregateData();

            try
            {
                _context = context;
                Step(ConvertParams(nArgs, argsptr),
                    data._count, ref data._data); /* throw */
            }
            finally
            {
                data._count++;
            }
        }
        catch (Exception e) /* NOTE: Must catch ALL. */
        {
            try
            {
                if ((_flags & SQLiteConnectionFlags.LogCallbackException) ==
                        SQLiteConnectionFlags.LogCallbackException)
                {
                    SQLiteLog.LogMessage(SQLiteBase.COR_E_EXCEPTION,
                        String.Format(CultureInfo.CurrentCulture,
                        "Caught exception in \"Step\" method: {1}",
                        e)); /* throw */
                }
            }
            catch
            {
                // do nothing.
            }
        }
    }

    /// <summary>
    /// An internal aggregate Final function callback, which wraps the context pointer and calls the virtual Final() method.
    /// WARNING: Must not throw exceptions.
    /// </summary>
    /// <param name="context">A raw context pointer</param>
    internal void FinalCallback(IntPtr context)
    {
        try
        {
            object obj = null;

            if (_base != null)
            {
                IntPtr n = _base.AggregateContext(context);
                AggregateData aggData;

                if ((_contextDataList != null) &&
                    _contextDataList.TryGetValue(n, out aggData))
                {
                    obj = aggData._data;
                    _contextDataList.Remove(n);
                }
            }

            try
            {
                _context = context;
                SetReturnValue(context, Final(obj)); /* throw */
            }
            finally
            {
                IDisposable disp = obj as IDisposable;
                if (disp != null) disp.Dispose(); /* throw */
            }
        }
        catch (Exception e) /* NOTE: Must catch ALL. */
        {
            try
            {
                if ((_flags & SQLiteConnectionFlags.LogCallbackException) ==
                        SQLiteConnectionFlags.LogCallbackException)
                {
                    SQLiteLog.LogMessage(SQLiteBase.COR_E_EXCEPTION,
                        String.Format(CultureInfo.CurrentCulture,
                        "Caught exception in \"Final\" method: {1}",
                        e)); /* throw */
                }
            }
            catch
            {
                // do nothing.
            }
        }
    }

    /// <summary>
    /// Using reflection, enumerate all assemblies in the current appdomain looking for classes that
    /// have a SQLiteFunctionAttribute attribute, and registering them accordingly.
    /// </summary>
#if !PLATFORM_COMPACTFRAMEWORK
    [Security.Permissions.FileIOPermission(Security.Permissions.SecurityAction.Assert, AllFiles = Security.Permissions.FileIOPermissionAccess.PathDiscovery)]
#endif
    static SQLiteFunction()
    {
      _registeredFunctions = new List<SQLiteFunctionAttribute>();
      try
      {
#if !PLATFORM_COMPACTFRAMEWORK
        //
        // NOTE: If the "No_SQLiteFunctions" environment variable is set,
        //       skip all our special code and simply return.
        //
        if (Environment.GetEnvironmentVariable("No_SQLiteFunctions") != null)
          return;

        SQLiteFunctionAttribute at;
        System.Reflection.Assembly[] arAssemblies = System.AppDomain.CurrentDomain.GetAssemblies();
        int w = arAssemblies.Length;
        System.Reflection.AssemblyName sqlite = System.Reflection.Assembly.GetCallingAssembly().GetName();

        for (int n = 0; n < w; n++)
        {
          Type[] arTypes;
          bool found = false;
          System.Reflection.AssemblyName[] references;
          try
          {
            // Inspect only assemblies that reference SQLite
            references = arAssemblies[n].GetReferencedAssemblies();
            int t = references.Length;
            for (int z = 0; z < t; z++)
            {
              if (references[z].Name == sqlite.Name)
              {
                found = true;
                break;
              }
            }

            if (found == false)
              continue;

            arTypes = arAssemblies[n].GetTypes();
          }
          catch (Reflection.ReflectionTypeLoadException e)
          {
            arTypes = e.Types;
          }

          int v = arTypes.Length;
          for (int x = 0; x < v; x++)
          {
            if (arTypes[x] == null) continue;

            object[] arAtt = arTypes[x].GetCustomAttributes(typeof(SQLiteFunctionAttribute), false);
            int u = arAtt.Length;
            for (int y = 0; y < u; y++)
            {
              at = arAtt[y] as SQLiteFunctionAttribute;
              if (at != null)
              {
                at.InstanceType = arTypes[x];
                _registeredFunctions.Add(at);
              }
            }
          }
        }
#endif
      }
      catch // SQLite provider can continue without being able to find built-in functions
      {
      }
    }

    /// <summary>
    /// Manual method of registering a function.  The type must still have the SQLiteFunctionAttributes in order to work
    /// properly, but this is a workaround for the Compact Framework where enumerating assemblies is not currently supported.
    /// </summary>
    /// <param name="typ">The type of the function to register</param>
    public static void RegisterFunction(Type typ)
    {
      object[] arAtt = typ.GetCustomAttributes(typeof(SQLiteFunctionAttribute), false);
      int u = arAtt.Length;
      SQLiteFunctionAttribute at;

      for (int y = 0; y < u; y++)
      {
        at = arAtt[y] as SQLiteFunctionAttribute;
        if (at != null)
        {
          at.InstanceType = typ;
          _registeredFunctions.Add(at);
        }
      }
    }

    /// <summary>
    /// Called by SQLiteBase derived classes, this function binds all user-defined functions to a connection.
    /// It is done this way so that all user-defined functions will access the database using the same encoding scheme
    /// as the connection (UTF-8 or UTF-16).
    /// </summary>
    /// <remarks>
    /// The wrapper functions that interop with SQLite will create a unique cookie value, which internally is a pointer to
    /// all the wrapped callback functions.  The interop function uses it to map CDecl callbacks to StdCall callbacks.
    /// </remarks>
    /// <param name="sqlbase">The base object on which the functions are to bind</param>
    /// <param name="flags">The flags associated with the parent connection object</param>
    /// <returns>Returns a logical list of functions which the connection should retain until it is closed.</returns>
    internal static IEnumerable<SQLiteFunction> BindFunctions(SQLiteBase sqlbase, SQLiteConnectionFlags flags)
    {
        List<SQLiteFunction> lFunctions = new List<SQLiteFunction>();

        foreach (SQLiteFunctionAttribute pr in _registeredFunctions)
        {
            SQLiteFunction f = (SQLiteFunction)Activator.CreateInstance(pr.InstanceType);
            BindFunction(sqlbase, pr, f, flags);
            lFunctions.Add(f);
        }

        return lFunctions;
    }

    /// <summary>
    /// This function binds a user-defined functions to a connection.
    /// </summary>
    /// <param name="sqliteBase">
    /// The <see cref="SQLiteBase" /> object instance associated with the
    /// <see cref="SQLiteConnection" /> that the function should be bound to.
    /// </param>
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
    internal static void BindFunction(
        SQLiteBase sqliteBase,
        SQLiteFunctionAttribute functionAttribute,
        SQLiteFunction function,
        SQLiteConnectionFlags flags
        )
    {
        if (sqliteBase == null)
            throw new ArgumentNullException("sqliteBase");

        if (functionAttribute == null)
            throw new ArgumentNullException("functionAttribute");

        if (function == null)
            throw new ArgumentNullException("function");

        FunctionType functionType = functionAttribute.FuncType;

        function._base = sqliteBase;
        function._flags = flags;

        function._InvokeFunc = (functionType == FunctionType.Scalar) ?
            new SQLiteCallback(function.ScalarCallback) : null;

        function._StepFunc = (functionType == FunctionType.Aggregate) ?
            new SQLiteCallback(function.StepCallback) : null;

        function._FinalFunc = (functionType == FunctionType.Aggregate) ?
            new SQLiteFinalCallback(function.FinalCallback) : null;

        function._CompareFunc = (functionType == FunctionType.Collation) ?
            new SQLiteCollation(function.CompareCallback) : null;

        function._CompareFunc16 = (functionType == FunctionType.Collation) ?
            new SQLiteCollation(function.CompareCallback16) : null;

        string name = functionAttribute.Name;

        if (functionType != FunctionType.Collation)
        {
            bool needCollSeq = (function is SQLiteFunctionEx);

            sqliteBase.CreateFunction(
                name, functionAttribute.Arguments, needCollSeq,
                function._InvokeFunc, function._StepFunc,
                function._FinalFunc);
        }
        else
        {
            sqliteBase.CreateCollation(
                name, function._CompareFunc, function._CompareFunc16);
        }
    }
  }

  /// <summary>
  /// Extends SQLiteFunction and allows an inherited class to obtain the collating sequence associated with a function call.
  /// </summary>
  /// <remarks>
  /// User-defined functions can call the GetCollationSequence() method in this class and use it to compare strings and char arrays.
  /// </remarks>
  public class SQLiteFunctionEx : SQLiteFunction
  {
    /// <summary>
    /// Obtains the collating sequence in effect for the given function.
    /// </summary>
    /// <returns></returns>
    protected CollationSequence GetCollationSequence()
    {
      return _base.GetCollationSequence(this, _context);
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    #region IDisposable "Pattern" Members
    private bool disposed;
    private void CheckDisposed() /* throw */
    {
#if THROW_ON_DISPOSED
        if (disposed)
            throw new ObjectDisposedException(typeof(SQLiteFunctionEx).Name);
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
  }

  /// <summary>
  /// The type of user-defined function to declare
  /// </summary>
  public enum FunctionType
  {
    /// <summary>
    /// Scalar functions are designed to be called and return a result immediately.  Examples include ABS(), Upper(), Lower(), etc.
    /// </summary>
    Scalar = 0,
    /// <summary>
    /// Aggregate functions are designed to accumulate data until the end of a call and then return a result gleaned from the accumulated data.
    /// Examples include SUM(), COUNT(), AVG(), etc.
    /// </summary>
    Aggregate = 1,
    /// <summary>
    /// Collation sequences are used to sort textual data in a custom manner, and appear in an ORDER BY clause.  Typically text in an ORDER BY is
    /// sorted using a straight case-insensitive comparison function.  Custom collating sequences can be used to alter the behavior of text sorting
    /// in a user-defined manner.
    /// </summary>
    Collation = 2,
  }

  /// <summary>
  /// An internal callback delegate declaration.
  /// </summary>
  /// <param name="context">Raw native context pointer for the user function.</param>
  /// <param name="argc">Total number of arguments to the user function.</param>
  /// <param name="argv">Raw native pointer to the array of raw native argument pointers.</param>
#if !PLATFORM_COMPACTFRAMEWORK
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
  public delegate void SQLiteCallback(IntPtr context, int argc, IntPtr argv);
  /// <summary>
  /// An internal final callback delegate declaration.
  /// </summary>
  /// <param name="context">Raw context pointer for the user function</param>
#if !PLATFORM_COMPACTFRAMEWORK
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
  internal delegate void SQLiteFinalCallback(IntPtr context);
  /// <summary>
  /// Internal callback delegate for implementing collation sequences
  /// </summary>
  /// <param name="puser">Not used</param>
  /// <param name="len1">Length of the string pv1</param>
  /// <param name="pv1">Pointer to the first string to compare</param>
  /// <param name="len2">Length of the string pv2</param>
  /// <param name="pv2">Pointer to the second string to compare</param>
  /// <returns>Returns -1 if the first string is less than the second.  0 if they are equal, or 1 if the first string is greater
  /// than the second.</returns>
#if !PLATFORM_COMPACTFRAMEWORK
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
  internal delegate int SQLiteCollation(IntPtr puser, int len1, IntPtr pv1, int len2, IntPtr pv2);

  /// <summary>
  /// The type of collating sequence
  /// </summary>
  public enum CollationTypeEnum
  {
    /// <summary>
    /// The built-in BINARY collating sequence
    /// </summary>
    Binary = 1,
    /// <summary>
    /// The built-in NOCASE collating sequence
    /// </summary>
    NoCase = 2,
    /// <summary>
    /// The built-in REVERSE collating sequence
    /// </summary>
    Reverse = 3,
    /// <summary>
    /// A custom user-defined collating sequence
    /// </summary>
    Custom = 0,
  }

  /// <summary>
  /// The encoding type the collation sequence uses
  /// </summary>
  public enum CollationEncodingEnum
  {
    /// <summary>
    /// The collation sequence is UTF8
    /// </summary>
    UTF8 = 1,
    /// <summary>
    /// The collation sequence is UTF16 little-endian
    /// </summary>
    UTF16LE = 2,
    /// <summary>
    /// The collation sequence is UTF16 big-endian
    /// </summary>
    UTF16BE = 3,
  }

  /// <summary>
  /// A struct describing the collating sequence a function is executing in
  /// </summary>
  public struct CollationSequence
  {
    /// <summary>
    /// The name of the collating sequence
    /// </summary>
    public string Name;
    /// <summary>
    /// The type of collating sequence
    /// </summary>
    public CollationTypeEnum Type;

    /// <summary>
    /// The text encoding of the collation sequence
    /// </summary>
    public CollationEncodingEnum Encoding;

    /// <summary>
    /// Context of the function that requested the collating sequence
    /// </summary>
    internal SQLiteFunction _func;

    /// <summary>
    /// Calls the base collating sequence to compare two strings
    /// </summary>
    /// <param name="s1">The first string to compare</param>
    /// <param name="s2">The second string to compare</param>
    /// <returns>-1 if s1 is less than s2, 0 if s1 is equal to s2, and 1 if s1 is greater than s2</returns>
    public int Compare(string s1, string s2)
    {
      return _func._base.ContextCollateCompare(Encoding, _func._context, s1, s2);
    }

    /// <summary>
    /// Calls the base collating sequence to compare two character arrays
    /// </summary>
    /// <param name="c1">The first array to compare</param>
    /// <param name="c2">The second array to compare</param>
    /// <returns>-1 if c1 is less than c2, 0 if c1 is equal to c2, and 1 if c1 is greater than c2</returns>
    public int Compare(char[] c1, char[] c2)
    {
      return _func._base.ContextCollateCompare(Encoding, _func._context, c1, c2);
    }
  }
}
