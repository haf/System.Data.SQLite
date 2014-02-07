/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Joe Mistachkin (joe@mistachkin.com)
 *
 * Released to the public domain, use at your own risk!
 ********************************************************/

using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Data.SQLite
{
    #region SQLiteContext Helper Class
    /// <summary>
    /// This class represents a context from the SQLite core library that can
    /// be passed to the sqlite3_result_*() and associated functions.
    /// </summary>
    public sealed class SQLiteContext : ISQLiteNativeHandle
    {
        #region Private Data
        /// <summary>
        /// The native context handle.
        /// </summary>
        private IntPtr pContext;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an instance of this class using the specified native
        /// context handle.
        /// </summary>
        /// <param name="pContext">
        /// The native context handle to use.
        /// </param>
        internal SQLiteContext(IntPtr pContext)
        {
            this.pContext = pContext;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISQLiteNativeHandle Members
        /// <summary>
        /// Returns the underlying SQLite native handle associated with this
        /// object instance.
        /// </summary>
        public IntPtr NativeHandle
        {
            get { return pContext; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// Sets the context result to NULL.
        /// </summary>
        public void SetNull()
        {
            if (pContext == IntPtr.Zero)
                throw new InvalidOperationException();

            UnsafeNativeMethods.sqlite3_result_null(pContext);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the context result to the specified <see cref="Double" />
        /// value.
        /// </summary>
        /// <param name="value">
        /// The <see cref="Double" /> value to use.
        /// </param>
        public void SetDouble(double value)
        {
            if (pContext == IntPtr.Zero)
                throw new InvalidOperationException();

#if !PLATFORM_COMPACTFRAMEWORK
            UnsafeNativeMethods.sqlite3_result_double(pContext, value);
#elif !SQLITE_STANDARD
            UnsafeNativeMethods.sqlite3_result_double_interop(pContext, ref value);
#else
            throw new NotImplementedException();
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the context result to the specified <see cref="Int32" />
        /// value.
        /// </summary>
        /// <param name="value">
        /// The <see cref="Int32" /> value to use.
        /// </param>
        public void SetInt(int value)
        {
            if (pContext == IntPtr.Zero)
                throw new InvalidOperationException();

            UnsafeNativeMethods.sqlite3_result_int(pContext, value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the context result to the specified <see cref="Int64" />
        /// value.
        /// </summary>
        /// <param name="value">
        /// The <see cref="Int64" /> value to use.
        /// </param>
        public void SetInt64(long value)
        {
            if (pContext == IntPtr.Zero)
                throw new InvalidOperationException();

#if !PLATFORM_COMPACTFRAMEWORK
            UnsafeNativeMethods.sqlite3_result_int64(pContext, value);
#elif !SQLITE_STANDARD
            UnsafeNativeMethods.sqlite3_result_int64_interop(pContext, ref value);
#else
            throw new NotImplementedException();
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the context result to the specified <see cref="String" />
        /// value.
        /// </summary>
        /// <param name="value">
        /// The <see cref="String" /> value to use.  This value will be
        /// converted to the UTF-8 encoding prior to being used.
        /// </param>
        public void SetString(string value)
        {
            if (pContext == IntPtr.Zero)
                throw new InvalidOperationException();

            byte[] bytes = SQLiteString.GetUtf8BytesFromString(value);

            if (bytes == null)
                throw new ArgumentNullException("value");

            UnsafeNativeMethods.sqlite3_result_text(
                pContext, bytes, bytes.Length, (IntPtr)(-1));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the context result to the specified <see cref="String" />
        /// value containing an error message.
        /// </summary>
        /// <param name="value">
        /// The <see cref="String" /> value containing the error message text.
        /// This value will be converted to the UTF-8 encoding prior to being
        /// used.
        /// </param>
        public void SetError(string value)
        {
            if (pContext == IntPtr.Zero)
                throw new InvalidOperationException();

            byte[] bytes = SQLiteString.GetUtf8BytesFromString(value);

            if (bytes == null)
                throw new ArgumentNullException("value");

            UnsafeNativeMethods.sqlite3_result_error(
                pContext, bytes, bytes.Length);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the context result to the specified <see cref="SQLiteErrorCode" />
        /// value.
        /// </summary>
        /// <param name="value">
        /// The <see cref="SQLiteErrorCode" /> value to use.
        /// </param>
        public void SetErrorCode(SQLiteErrorCode value)
        {
            if (pContext == IntPtr.Zero)
                throw new InvalidOperationException();

            UnsafeNativeMethods.sqlite3_result_error_code(pContext, value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the context result to contain the error code SQLITE_TOOBIG.
        /// </summary>
        public void SetErrorTooBig()
        {
            if (pContext == IntPtr.Zero)
                throw new InvalidOperationException();

            UnsafeNativeMethods.sqlite3_result_error_toobig(pContext);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the context result to contain the error code SQLITE_NOMEM.
        /// </summary>
        public void SetErrorNoMemory()
        {
            if (pContext == IntPtr.Zero)
                throw new InvalidOperationException();

            UnsafeNativeMethods.sqlite3_result_error_nomem(pContext);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the context result to the specified <see cref="Byte" /> array
        /// value.
        /// </summary>
        /// <param name="value">
        /// The <see cref="Byte" /> array value to use.
        /// </param>
        public void SetBlob(byte[] value)
        {
            if (pContext == IntPtr.Zero)
                throw new InvalidOperationException();

            if (value == null)
                throw new ArgumentNullException("value");

            UnsafeNativeMethods.sqlite3_result_blob(
                pContext, value, value.Length, (IntPtr)(-1));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the context result to a BLOB of zeros of the specified size.
        /// </summary>
        /// <param name="value">
        /// The number of zero bytes to use for the BLOB context result.
        /// </param>
        public void SetZeroBlob(int value)
        {
            if (pContext == IntPtr.Zero)
                throw new InvalidOperationException();

            UnsafeNativeMethods.sqlite3_result_zeroblob(pContext, value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the context result to the specified <see cref="SQLiteValue" />.
        /// </summary>
        /// <param name="value">
        /// The <see cref="SQLiteValue" /> to use.
        /// </param>
        public void SetValue(SQLiteValue value)
        {
            if (pContext == IntPtr.Zero)
                throw new InvalidOperationException();

            if (value == null)
                throw new ArgumentNullException("value");

            UnsafeNativeMethods.sqlite3_result_value(
                pContext, value.NativeHandle);
        }
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteValue Helper Class
    /// <summary>
    /// This class represents a value from the SQLite core library that can be
    /// passed to the sqlite3_value_*() and associated functions.
    /// </summary>
    public sealed class SQLiteValue : ISQLiteNativeHandle
    {
        #region Private Data
        /// <summary>
        /// The native value handle.
        /// </summary>
        private IntPtr pValue;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an instance of this class using the specified native
        /// value handle.
        /// </summary>
        /// <param name="pValue">
        /// The native value handle to use.
        /// </param>
        private SQLiteValue(IntPtr pValue)
        {
            this.pValue = pValue;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// Invalidates the native value handle, thereby preventing further
        /// access to it from this object instance.
        /// </summary>
        private void PreventNativeAccess()
        {
            pValue = IntPtr.Zero;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Marshal Helper Methods
        /// <summary>
        /// Converts a logical array of native pointers to native sqlite3_value
        /// structures into a managed array of <see cref="SQLiteValue" />
        /// object instances.
        /// </summary>
        /// <param name="argc">
        /// The number of elements in the logical array of native sqlite3_value
        /// structures.
        /// </param>
        /// <param name="argv">
        /// The native pointer to the logical array of native sqlite3_value
        /// structures to convert.
        /// </param>
        /// <returns>
        /// The managed array of <see cref="SQLiteValue" /> object instances or
        /// null upon failure.
        /// </returns>
        internal static SQLiteValue[] ArrayFromSizeAndIntPtr(
            int argc,
            IntPtr argv
            )
        {
            if (argc < 0)
                return null;

            if (argv == IntPtr.Zero)
                return null;

            SQLiteValue[] result = new SQLiteValue[argc];

            for (int index = 0, offset = 0;
                    index < result.Length;
                    index++, offset += IntPtr.Size)
            {
                IntPtr pArg = SQLiteMarshal.ReadIntPtr(argv, offset);

                result[index] = (pArg != IntPtr.Zero) ?
                    new SQLiteValue(pArg) : null;
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISQLiteNativeHandle Members
        /// <summary>
        /// Returns the underlying SQLite native handle associated with this
        /// object instance.
        /// </summary>
        public IntPtr NativeHandle
        {
            get { return pValue; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private bool persisted;
        /// <summary>
        /// Returns non-zero if the native SQLite value has been successfully
        /// persisted as a managed value within this object instance (i.e. the
        /// <see cref="Value" /> property may then be read successfully).
        /// </summary>
        public bool Persisted
        {
            get { return persisted; }
        }

        ///////////////////////////////////////////////////////////////////////

        private object value;
        /// <summary>
        /// If the managed value for this object instance is available (i.e. it
        /// has been previously persisted via the <see cref="Persist" />) method,
        /// that value is returned; otherwise, an exception is thrown.  The
        /// returned value may be null.
        /// </summary>
        public object Value
        {
            get
            {
                if (!persisted)
                {
                    throw new InvalidOperationException(
                        "value was not persisted");
                }

                return value;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// Gets and returns the type affinity associated with this value.
        /// </summary>
        /// <returns>
        /// The type affinity associated with this value.
        /// </returns>
        public TypeAffinity GetTypeAffinity()
        {
            if (pValue == IntPtr.Zero) return TypeAffinity.None;
            return UnsafeNativeMethods.sqlite3_value_type(pValue);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets and returns the number of bytes associated with this value, if
        /// it refers to a UTF-8 encoded string.
        /// </summary>
        /// <returns>
        /// The number of bytes associated with this value.  The returned value
        /// may be zero.
        /// </returns>
        public int GetBytes()
        {
            if (pValue == IntPtr.Zero) return 0;
            return UnsafeNativeMethods.sqlite3_value_bytes(pValue);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets and returns the <see cref="Int32" /> associated with this
        /// value.
        /// </summary>
        /// <returns>
        /// The <see cref="Int32" /> associated with this value.
        /// </returns>
        public int GetInt()
        {
            if (pValue == IntPtr.Zero) return default(int);
            return UnsafeNativeMethods.sqlite3_value_int(pValue);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets and returns the <see cref="Int64" /> associated with
        /// this value.
        /// </summary>
        /// <returns>
        /// The <see cref="Int64" /> associated with this value.
        /// </returns>
        public long GetInt64()
        {
            if (pValue == IntPtr.Zero) return default(long);

#if !PLATFORM_COMPACTFRAMEWORK
            return UnsafeNativeMethods.sqlite3_value_int64(pValue);
#elif !SQLITE_STANDARD
            long value;
            UnsafeNativeMethods.sqlite3_value_int64_interop(pValue, out value);
            return value;
#else
            throw new NotImplementedException();
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets and returns the <see cref="Double" /> associated with this
        /// value.
        /// </summary>
        /// <returns>
        /// The <see cref="Double" /> associated with this value.
        /// </returns>
        public double GetDouble()
        {
            if (pValue == IntPtr.Zero) return default(double);

#if !PLATFORM_COMPACTFRAMEWORK
            return UnsafeNativeMethods.sqlite3_value_double(pValue);
#elif !SQLITE_STANDARD
            double value;
            UnsafeNativeMethods.sqlite3_value_double_interop(pValue, out value);
            return value;
#else
            throw new NotImplementedException();
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets and returns the <see cref="String" /> associated with this
        /// value.
        /// </summary>
        /// <returns>
        /// The <see cref="String" /> associated with this value.  The value is
        /// converted from the UTF-8 encoding prior to being returned.
        /// </returns>
        public string GetString()
        {
            if (pValue == IntPtr.Zero) return null;
            return SQLiteString.StringFromUtf8IntPtr(pValue, GetBytes());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets and returns the <see cref="Byte" /> array associated with this
        /// value.
        /// </summary>
        /// <returns>
        /// The <see cref="Byte" /> array associated with this value.
        /// </returns>
        public byte[] GetBlob()
        {
            if (pValue == IntPtr.Zero) return null;
            return SQLiteBytes.FromIntPtr(pValue, GetBytes());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Uses the native value handle to obtain and store the managed value
        /// for this object instance, thus saving it for later use.  The type
        /// of the managed value is determined by the type affinity of the
        /// native value.  If the type affinity is not recognized by this
        /// method, no work is done and false is returned.
        /// </summary>
        /// <returns>
        /// Non-zero if the native value was persisted successfully.
        /// </returns>
        public bool Persist()
        {
            switch (GetTypeAffinity())
            {
                case TypeAffinity.Uninitialized:
                    {
                        value = null;
                        PreventNativeAccess();
                        return (persisted = true);
                    }
                case TypeAffinity.Int64:
                    {
                        value = GetInt64();
                        PreventNativeAccess();
                        return (persisted = true);
                    }
                case TypeAffinity.Double:
                    {
                        value = GetDouble();
                        PreventNativeAccess();
                        return (persisted = true);
                    }
                case TypeAffinity.Text:
                    {
                        value = GetString();
                        PreventNativeAccess();
                        return (persisted = true);
                    }
                case TypeAffinity.Blob:
                    {
                        value = GetBytes();
                        PreventNativeAccess();
                        return (persisted = true);
                    }
                case TypeAffinity.Null:
                    {
                        value = DBNull.Value;
                        PreventNativeAccess();
                        return (persisted = true);
                    }
                default:
                    {
                        return false;
                    }
            }
        }
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteIndexConstraintOp Enumeration
    /// <summary>
    /// These are the allowed values for the operators that are part of a
    /// constraint term in the WHERE clause of a query that uses a virtual
    /// table.
    /// </summary>
    public enum SQLiteIndexConstraintOp : byte
    {
        /// <summary>
        /// This value represents the equality operator.
        /// </summary>
        EqualTo = 2,

        /// <summary>
        /// This value represents the greater than operator.
        /// </summary>
        GreaterThan = 4,

        /// <summary>
        /// This value represents the less than or equal to operator.
        /// </summary>
        LessThanOrEqualTo = 8,

        /// <summary>
        /// This value represents the less than operator.
        /// </summary>
        LessThan = 16,

        /// <summary>
        /// This value represents the greater than or equal to operator.
        /// </summary>
        GreaterThanOrEqualTo = 32,

        /// <summary>
        /// This value represents the MATCH operator.
        /// </summary>
        Match = 64
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteIndexConstraint Helper Class
    /// <summary>
    /// This class represents the native sqlite3_index_constraint structure
    /// from the SQLite core library.
    /// </summary>
    public sealed class SQLiteIndexConstraint
    {
        #region Internal Constructors
        /// <summary>
        /// Constructs an instance of this class using the specified native
        /// sqlite3_index_constraint structure.
        /// </summary>
        /// <param name="constraint">
        /// The native sqlite3_index_constraint structure to use.
        /// </param>
        internal SQLiteIndexConstraint(
            UnsafeNativeMethods.sqlite3_index_constraint constraint
            )
            : this(constraint.iColumn, constraint.op, constraint.usable,
                   constraint.iTermOffset)
        {
            // do nothing.
        }
        #endregion

        //////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an instance of this class using the specified field
        /// values.
        /// </summary>
        /// <param name="iColumn">
        /// Column on left-hand side of constraint.
        /// </param>
        /// <param name="op">
        /// Constraint operator (<see cref="SQLiteIndexConstraintOp" />).
        /// </param>
        /// <param name="usable">
        /// True if this constraint is usable.
        /// </param>
        /// <param name="iTermOffset">
        /// Used internally - <see cref="ISQLiteManagedModule.BestIndex" />
        /// should ignore.
        /// </param>
        private SQLiteIndexConstraint(
            int iColumn,
            SQLiteIndexConstraintOp op,
            byte usable,
            int iTermOffset
            )
        {
            this.iColumn = iColumn;
            this.op = op;
            this.usable = usable;
            this.iTermOffset = iTermOffset;
        }
        #endregion

        //////////////////////////////////////////////////////////////////////

        #region Public Fields
        /// <summary>
        /// Column on left-hand side of constraint.
        /// </summary>
        public int iColumn;

        //////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constraint operator (<see cref="SQLiteIndexConstraintOp" />).
        /// </summary>
        public SQLiteIndexConstraintOp op;

        //////////////////////////////////////////////////////////////////////

        /// <summary>
        /// True if this constraint is usable.
        /// </summary>
        public byte usable;

        //////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Used internally - <see cref="ISQLiteManagedModule.BestIndex" />
        /// should ignore.
        /// </summary>
        public int iTermOffset;
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteIndexOrderBy Helper Class
    /// <summary>
    /// This class represents the native sqlite3_index_orderby structure from
    /// the SQLite core library.
    /// </summary>
    public sealed class SQLiteIndexOrderBy
    {
        #region Internal Constructors
        /// <summary>
        /// Constructs an instance of this class using the specified native
        /// sqlite3_index_orderby structure.
        /// </summary>
        /// <param name="orderBy">
        /// The native sqlite3_index_orderby structure to use.
        /// </param>
        internal SQLiteIndexOrderBy(
            UnsafeNativeMethods.sqlite3_index_orderby orderBy
            )
            : this(orderBy.iColumn, orderBy.desc)
        {
            // do nothing.
        }
        #endregion

        //////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an instance of this class using the specified field
        /// values.
        /// </summary>
        /// <param name="iColumn">
        /// Column number.
        /// </param>
        /// <param name="desc">
        /// True for DESC.  False for ASC.
        /// </param>
        private SQLiteIndexOrderBy(
            int iColumn,
            byte desc
            )
        {
            this.iColumn = iColumn;
            this.desc = desc;
        }
        #endregion

        //////////////////////////////////////////////////////////////////////

        #region Public Fields
        /// <summary>
        /// Column number.
        /// </summary>
        public int iColumn;

        //////////////////////////////////////////////////////////////////////

        /// <summary>
        /// True for DESC.  False for ASC.
        /// </summary>
        public byte desc;
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteIndexConstraintUsage Helper Class
    /// <summary>
    /// This class represents the native sqlite3_index_constraint_usage
    /// structure from the SQLite core library.
    /// </summary>
    public sealed class SQLiteIndexConstraintUsage
    {
        #region Internal Constructors
        /// <summary>
        /// Constructs an instance of this class using the specified native
        /// sqlite3_index_constraint_usage structure.
        /// </summary>
        /// <param name="constraintUsage">
        /// The native sqlite3_index_constraint_usage structure to use.
        /// </param>
        internal SQLiteIndexConstraintUsage(
            UnsafeNativeMethods.sqlite3_index_constraint_usage constraintUsage
            )
            : this(constraintUsage.argvIndex, constraintUsage.omit)
        {
            // do nothing.
        }
        #endregion

        //////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an instance of this class using the specified field
        /// values.
        /// </summary>
        /// <param name="argvIndex">
        /// If greater than 0, constraint is part of argv to xFilter.
        /// </param>
        /// <param name="omit">
        /// Do not code a test for this constraint.
        /// </param>
        private SQLiteIndexConstraintUsage(
            int argvIndex,
            byte omit
            )
        {
            this.argvIndex = argvIndex;
            this.omit = omit;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Fields
        /// <summary>
        /// If greater than 0, constraint is part of argv to xFilter.
        /// </summary>
        public int argvIndex;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Do not code a test for this constraint.
        /// </summary>
        public byte omit;
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteIndexInputs Helper Class
    /// <summary>
    /// This class represents the various inputs provided by the SQLite core
    /// library to the <see cref="ISQLiteManagedModule.BestIndex" /> method.
    /// </summary>
    public sealed class SQLiteIndexInputs
    {
        #region Internal Constructors
        /// <summary>
        /// Constructs an instance of this class.
        /// </summary>
        /// <param name="nConstraint">
        /// The number of <see cref="SQLiteIndexConstraint" /> instances to
        /// pre-allocate space for.
        /// </param>
        /// <param name="nOrderBy">
        /// The number of <see cref="SQLiteIndexOrderBy" /> instances to
        /// pre-allocate space for.
        /// </param>
        internal SQLiteIndexInputs(int nConstraint, int nOrderBy)
        {
            constraints = new SQLiteIndexConstraint[nConstraint];
            orderBys = new SQLiteIndexOrderBy[nOrderBy];
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private SQLiteIndexConstraint[] constraints;
        /// <summary>
        /// An array of <see cref="SQLiteIndexConstraint" /> object instances,
        /// each containing information supplied by the SQLite core library.
        /// </summary>
        public SQLiteIndexConstraint[] Constraints
        {
            get { return constraints; }
        }

        ///////////////////////////////////////////////////////////////////////

        private SQLiteIndexOrderBy[] orderBys;
        /// <summary>
        /// An array of <see cref="SQLiteIndexOrderBy" /> object instances,
        /// each containing information supplied by the SQLite core library.
        /// </summary>
        public SQLiteIndexOrderBy[] OrderBys
        {
            get { return orderBys; }
        }
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteIndexOutputs Helper Class
    /// <summary>
    /// This class represents the various outputs provided to the SQLite core
    /// library by the <see cref="ISQLiteManagedModule.BestIndex" /> method.
    /// </summary>
    public sealed class SQLiteIndexOutputs
    {
        #region Internal Constructors
        /// <summary>
        /// Constructs an instance of this class.
        /// </summary>
        /// <param name="nConstraint">
        /// The number of <see cref="SQLiteIndexConstraintUsage" /> instances
        /// to pre-allocate space for.
        /// </param>
        internal SQLiteIndexOutputs(int nConstraint)
        {
            constraintUsages = new SQLiteIndexConstraintUsage[nConstraint];
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines if the native estimatedRows field can be used, based on
        /// the available version of the SQLite core library.
        /// </summary>
        /// <returns>
        /// Non-zero if the <see cref="EstimatedRows" /> property is supported
        /// by the SQLite core library.
        /// </returns>
        public bool CanUseEstimatedRows()
        {
            if (UnsafeNativeMethods.sqlite3_libversion_number() >= 3008002)
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private SQLiteIndexConstraintUsage[] constraintUsages;
        /// <summary>
        /// An array of <see cref="SQLiteIndexConstraintUsage" /> object
        /// instances, each containing information to be supplied to the SQLite
        /// core library.
        /// </summary>
        public SQLiteIndexConstraintUsage[] ConstraintUsages
        {
            get { return constraintUsages; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int indexNumber;
        /// <summary>
        /// Number used to help identify the selected index.  This value will
        /// later be provided to the <see cref="ISQLiteManagedModule.Filter" />
        /// method.
        /// </summary>
        public int IndexNumber
        {
            get { return indexNumber; }
            set { indexNumber = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string indexString;
        /// <summary>
        /// String used to help identify the selected index.  This value will
        /// later be provided to the <see cref="ISQLiteManagedModule.Filter" />
        /// method.
        /// </summary>
        public string IndexString
        {
            get { return indexString; }
            set { indexString = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int needToFreeIndexString;
        /// <summary>
        /// Non-zero if the index string must be freed by the SQLite core
        /// library.
        /// </summary>
        public int NeedToFreeIndexString
        {
            get { return needToFreeIndexString; }
            set { needToFreeIndexString = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int orderByConsumed;
        /// <summary>
        /// True if output is already ordered.
        /// </summary>
        public int OrderByConsumed
        {
            get { return orderByConsumed; }
            set { orderByConsumed = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private double? estimatedCost;
        /// <summary>
        /// Estimated cost of using this index.  Using a null value here
        /// indicates that a default estimated cost value should be used.
        /// </summary>
        public double? EstimatedCost
        {
            get { return estimatedCost; }
            set { estimatedCost = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private long? estimatedRows;
        /// <summary>
        /// Estimated number of rows returned.  Using a null value here
        /// indicates that a default estimated rows value should be used.
        /// </summary>
        public long? EstimatedRows
        {
            get { return estimatedRows; }
            set { estimatedRows = value; }
        }
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteIndex Helper Class
    /// <summary>
    /// This class represents the various inputs and outputs used with the
    /// <see cref="ISQLiteManagedModule.BestIndex" /> method.
    /// </summary>
    public sealed class SQLiteIndex
    {
        #region Internal Constructors
        /// <summary>
        /// Constructs an instance of this class.
        /// </summary>
        /// <param name="nConstraint">
        /// The number of <see cref="SQLiteIndexConstraint" /> (and
        /// <see cref="SQLiteIndexConstraintUsage" />) instances to
        /// pre-allocate space for.
        /// </param>
        /// <param name="nOrderBy">
        /// The number of <see cref="SQLiteIndexOrderBy" /> instances to
        /// pre-allocate space for.
        /// </param>
        internal SQLiteIndex(
            int nConstraint,
            int nOrderBy
            )
        {
            inputs = new SQLiteIndexInputs(nConstraint, nOrderBy);
            outputs = new SQLiteIndexOutputs(nConstraint);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Marshal Helper Methods
        /// <summary>
        /// Converts a native pointer to a native sqlite3_index_info structure
        /// into a new <see cref="SQLiteIndex" /> object instance.
        /// </summary>
        /// <param name="pIndex">
        /// The native pointer to the native sqlite3_index_info structure to
        /// convert.
        /// </param>
        /// <param name="index">
        /// Upon success, this parameter will be modified to contain the newly
        /// created <see cref="SQLiteIndex" /> object instance.
        /// </param>
        internal static void FromIntPtr(
            IntPtr pIndex,
            ref SQLiteIndex index
            )
        {
            if (pIndex == IntPtr.Zero)
                return;

            int offset = 0;

            int nConstraint = SQLiteMarshal.ReadInt32(pIndex, offset);

            offset = SQLiteMarshal.NextOffsetOf(offset, sizeof(int),
                IntPtr.Size);

            IntPtr pConstraint = SQLiteMarshal.ReadIntPtr(pIndex, offset);

            offset = SQLiteMarshal.NextOffsetOf(offset, IntPtr.Size,
                sizeof(int));

            int nOrderBy = SQLiteMarshal.ReadInt32(pIndex, offset);

            offset = SQLiteMarshal.NextOffsetOf(offset, sizeof(int),
                IntPtr.Size);

            IntPtr pOrderBy = SQLiteMarshal.ReadIntPtr(pIndex, offset);

            index = new SQLiteIndex(nConstraint, nOrderBy);

            Type indexConstraintType = typeof(
                UnsafeNativeMethods.sqlite3_index_constraint);

            int sizeOfConstraintType = Marshal.SizeOf(indexConstraintType);

            for (int iConstraint = 0; iConstraint < nConstraint; iConstraint++)
            {
                IntPtr pOffset = SQLiteMarshal.IntPtrForOffset(
                    pConstraint, iConstraint * sizeOfConstraintType);

                UnsafeNativeMethods.sqlite3_index_constraint constraint =
                    (UnsafeNativeMethods.sqlite3_index_constraint)
                        Marshal.PtrToStructure(pOffset, indexConstraintType);

                index.Inputs.Constraints[iConstraint] =
                    new SQLiteIndexConstraint(constraint);
            }

            Type indexOrderByType = typeof(
                UnsafeNativeMethods.sqlite3_index_orderby);

            int sizeOfOrderByType = Marshal.SizeOf(indexOrderByType);

            for (int iOrderBy = 0; iOrderBy < nOrderBy; iOrderBy++)
            {
                IntPtr pOffset = SQLiteMarshal.IntPtrForOffset(
                    pOrderBy, iOrderBy * sizeOfOrderByType);

                UnsafeNativeMethods.sqlite3_index_orderby orderBy =
                    (UnsafeNativeMethods.sqlite3_index_orderby)
                        Marshal.PtrToStructure(pOffset, indexOrderByType);

                index.Inputs.OrderBys[iOrderBy] =
                    new SQLiteIndexOrderBy(orderBy);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Populates the outputs of a pre-allocated native sqlite3_index_info
        /// structure using an existing <see cref="SQLiteIndex" /> object
        /// instance.
        /// </summary>
        /// <param name="index">
        /// The existing <see cref="SQLiteIndex" /> object instance containing
        /// the output data to use.
        /// </param>
        /// <param name="pIndex">
        /// The native pointer to the pre-allocated native sqlite3_index_info
        /// structure.
        /// </param>
        internal static void ToIntPtr(
            SQLiteIndex index,
            IntPtr pIndex
            )
        {
            if ((index == null) || (index.Inputs == null) ||
                (index.Inputs.Constraints == null) ||
                (index.Outputs == null) ||
                (index.Outputs.ConstraintUsages == null))
            {
                return;
            }

            if (pIndex == IntPtr.Zero)
                return;

            int offset = 0;

            int nConstraint = SQLiteMarshal.ReadInt32(pIndex, offset);

            if (nConstraint != index.Inputs.Constraints.Length)
                return;

            if (nConstraint != index.Outputs.ConstraintUsages.Length)
                return;

            offset = SQLiteMarshal.NextOffsetOf(offset, sizeof(int),
                IntPtr.Size);

            offset = SQLiteMarshal.NextOffsetOf(offset, IntPtr.Size,
                sizeof(int));

            offset = SQLiteMarshal.NextOffsetOf(offset, sizeof(int),
                IntPtr.Size);

            offset = SQLiteMarshal.NextOffsetOf(offset, IntPtr.Size,
                sizeof(int));

            IntPtr pConstraintUsage = SQLiteMarshal.ReadIntPtr(pIndex, offset);

            int sizeOfConstraintUsageType = Marshal.SizeOf(typeof(
                UnsafeNativeMethods.sqlite3_index_constraint_usage));

            for (int iConstraint = 0; iConstraint < nConstraint; iConstraint++)
            {
                UnsafeNativeMethods.sqlite3_index_constraint_usage constraintUsage =
                    new UnsafeNativeMethods.sqlite3_index_constraint_usage(
                        index.Outputs.ConstraintUsages[iConstraint]);

                Marshal.StructureToPtr(
                    constraintUsage, SQLiteMarshal.IntPtrForOffset(
                    pConstraintUsage, iConstraint * sizeOfConstraintUsageType),
                    false);
            }

            offset = SQLiteMarshal.NextOffsetOf(offset, IntPtr.Size,
                sizeof(int));

            SQLiteMarshal.WriteInt32(pIndex, offset,
                index.Outputs.IndexNumber);

            offset = SQLiteMarshal.NextOffsetOf(offset, sizeof(int),
                IntPtr.Size);

            SQLiteMarshal.WriteIntPtr(pIndex, offset,
                SQLiteString.Utf8IntPtrFromString(index.Outputs.IndexString));

            offset = SQLiteMarshal.NextOffsetOf(offset, IntPtr.Size,
                sizeof(int));

            //
            // NOTE: We just allocated the IndexString field; therefore, we
            //       need to set the NeedToFreeIndexString field to non-zero.
            //
            SQLiteMarshal.WriteInt32(pIndex, offset, 1);

            offset = SQLiteMarshal.NextOffsetOf(offset, sizeof(int),
                sizeof(int));

            SQLiteMarshal.WriteInt32(pIndex, offset,
                index.Outputs.OrderByConsumed);

            offset = SQLiteMarshal.NextOffsetOf(offset, sizeof(int),
                sizeof(double));

            if (index.Outputs.EstimatedCost.HasValue)
            {
                SQLiteMarshal.WriteDouble(pIndex, offset,
                    index.Outputs.EstimatedCost.GetValueOrDefault());
            }

            if (index.Outputs.CanUseEstimatedRows() &&
                index.Outputs.EstimatedRows.HasValue)
            {
                SQLiteMarshal.WriteInt64(pIndex, offset,
                    index.Outputs.EstimatedRows.GetValueOrDefault());
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private SQLiteIndexInputs inputs;
        /// <summary>
        /// The <see cref="SQLiteIndexInputs" /> object instance containing
        /// the inputs to the <see cref="ISQLiteManagedModule.BestIndex" />
        /// method.
        /// </summary>
        public SQLiteIndexInputs Inputs
        {
            get { return inputs; }
        }

        ///////////////////////////////////////////////////////////////////////

        private SQLiteIndexOutputs outputs;
        /// <summary>
        /// The <see cref="SQLiteIndexOutputs" /> object instance containing
        /// the outputs from the <see cref="ISQLiteManagedModule.BestIndex" />
        /// method.
        /// </summary>
        public SQLiteIndexOutputs Outputs
        {
            get { return outputs; }
        }
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteVirtualTable Base Class
    /// <summary>
    /// This class represents a managed virtual table implementation.  It is
    /// not sealed and should be used as the base class for any user-defined
    /// virtual table classes implemented in managed code.
    /// </summary>
    public class SQLiteVirtualTable :
            ISQLiteNativeHandle, IDisposable /* NOT SEALED */
    {
        #region Private Constants
        /// <summary>
        /// The index within the array of strings provided to the
        /// <see cref="ISQLiteManagedModule.Create" /> and
        /// <see cref="ISQLiteManagedModule.Connect" /> methods containing the
        /// name of the module implementing this virtual table.
        /// </summary>
        private const int ModuleNameIndex = 0;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The index within the array of strings provided to the
        /// <see cref="ISQLiteManagedModule.Create" /> and
        /// <see cref="ISQLiteManagedModule.Connect" /> methods containing the
        /// name of the database containing this virtual table.
        /// </summary>
        private const int DatabaseNameIndex = 1;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The index within the array of strings provided to the
        /// <see cref="ISQLiteManagedModule.Create" /> and
        /// <see cref="ISQLiteManagedModule.Connect" /> methods containing the
        /// name of the virtual table.
        /// </summary>
        private const int TableNameIndex = 2;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class.
        /// </summary>
        /// <param name="arguments">
        /// The original array of strings provided to the
        /// <see cref="ISQLiteManagedModule.Create" /> and
        /// <see cref="ISQLiteManagedModule.Connect" /> methods.
        /// </param>
        public SQLiteVirtualTable(
            string[] arguments
            )
        {
            this.arguments = arguments;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private string[] arguments;
        /// <summary>
        /// The original array of strings provided to the
        /// <see cref="ISQLiteManagedModule.Create" /> and
        /// <see cref="ISQLiteManagedModule.Connect" /> methods.
        /// </summary>
        public virtual string[] Arguments
        {
            get { CheckDisposed(); return arguments; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the module implementing this virtual table.
        /// </summary>
        public virtual string ModuleName
        {
            get
            {
                CheckDisposed();

                string[] arguments = Arguments;

                if ((arguments != null) &&
                    (arguments.Length > ModuleNameIndex))
                {
                    return arguments[ModuleNameIndex];
                }
                else
                {
                    return null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the database containing this virtual table.
        /// </summary>
        public virtual string DatabaseName
        {
            get
            {
                CheckDisposed();

                string[] arguments = Arguments;

                if ((arguments != null) &&
                    (arguments.Length > DatabaseNameIndex))
                {
                    return arguments[DatabaseNameIndex];
                }
                else
                {
                    return null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the virtual table.
        /// </summary>
        public virtual string TableName
        {
            get
            {
                CheckDisposed();

                string[] arguments = Arguments;

                if ((arguments != null) &&
                    (arguments.Length > TableNameIndex))
                {
                    return arguments[TableNameIndex];
                }
                else
                {
                    return null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private SQLiteIndex index;
        /// <summary>
        /// The <see cref="SQLiteIndex" /> object instance containing all the
        /// data for the inputs and outputs relating to the most recent index
        /// selection.
        /// </summary>
        public virtual SQLiteIndex Index
        {
            get { CheckDisposed(); return index; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method should normally be used by the
        /// <see cref="ISQLiteManagedModule.BestIndex" /> method in order to
        /// perform index selection based on the constraints provided by the
        /// SQLite core library.
        /// </summary>
        /// <param name="index">
        /// The <see cref="SQLiteIndex" /> object instance containing all the
        /// data for the inputs and outputs relating to index selection.
        /// </param>
        /// <returns>
        /// Non-zero upon success.
        /// </returns>
        public virtual bool BestIndex(
            SQLiteIndex index
            )
        {
            CheckDisposed();

            this.index = index;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Attempts to record the renaming of the virtual table associated
        /// with this object instance.
        /// </summary>
        /// <param name="name">
        /// The new name for the virtual table.
        /// </param>
        /// <returns>
        /// Non-zero upon success.
        /// </returns>
        public virtual bool Rename(
            string name
            )
        {
            CheckDisposed();

            if ((arguments != null) &&
                (arguments.Length > TableNameIndex))
            {
                arguments[TableNameIndex] = name;
                return true;
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISQLiteNativeHandle Members
        private IntPtr nativeHandle;
        /// <summary>
        /// Returns the underlying SQLite native handle associated with this
        /// object instance.
        /// </summary>
        public virtual IntPtr NativeHandle
        {
            get { CheckDisposed(); return nativeHandle; }
            internal set { nativeHandle = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        /// <summary>
        /// Disposes of this object instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        /// <summary>
        /// Throws an <see cref="ObjectDisposedException" /> if this object
        /// instance has been disposed.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed)
            {
                throw new ObjectDisposedException(
                    typeof(SQLiteVirtualTable).Name);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Disposes of this object instance.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <see cref="Dispose()" /> method.  Zero if this method is being called
        /// from the finalizer.
        /// </param>
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

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes this object instance.
        /// </summary>
        ~SQLiteVirtualTable()
        {
            Dispose(false);
        }
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteVirtualTableCursor Base Class
    /// <summary>
    /// This class represents a managed virtual table cursor implementation.
    /// It is not sealed and should be used as the base class for any
    /// user-defined virtual table cursor classes implemented in managed code.
    /// </summary>
    public class SQLiteVirtualTableCursor :
            ISQLiteNativeHandle, IDisposable /* NOT SEALED */
    {
        #region Protected Constants
        /// <summary>
        /// This value represents an invalid integer row sequence number.
        /// </summary>
        protected static readonly int InvalidRowIndex = 0;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The field holds the integer row sequence number for the current row
        /// pointed to by this cursor object instance.
        /// </summary>
        private int rowIndex;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this object instance.
        /// </param>
        public SQLiteVirtualTableCursor(
            SQLiteVirtualTable table
            )
            : this()
        {
            this.table = table;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an instance of this class.
        /// </summary>
        private SQLiteVirtualTableCursor()
        {
            rowIndex = InvalidRowIndex;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private SQLiteVirtualTable table;
        /// <summary>
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this object instance.
        /// </summary>
        public virtual SQLiteVirtualTable Table
        {
            get { CheckDisposed(); return table; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int indexNumber;
        /// <summary>
        /// Number used to help identify the selected index.  This value will
        /// be set via the <see cref="Filter" /> method.
        /// </summary>
        public virtual int IndexNumber
        {
            get { CheckDisposed(); return indexNumber; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string indexString;
        /// <summary>
        /// String used to help identify the selected index.  This value will
        /// be set via the <see cref="Filter" /> method.
        /// </summary>
        public virtual string IndexString
        {
            get { CheckDisposed(); return indexString; }
        }

        ///////////////////////////////////////////////////////////////////////

        private SQLiteValue[] values;
        /// <summary>
        /// The values used to filter the rows returned via this cursor object
        /// instance.  This value will be set via the <see cref="Filter" />
        /// method.
        /// </summary>
        public virtual SQLiteValue[] Values
        {
            get { CheckDisposed(); return values; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Methods
        /// <summary>
        /// Attempts to persist the specified <see cref="SQLiteValue" /> object
        /// instances in order to make them available after the
        /// <see cref="ISQLiteManagedModule.Filter" /> method returns.
        /// </summary>
        /// <param name="values">
        /// The array of <see cref="SQLiteValue" /> object instances to be
        /// persisted.
        /// </param>
        /// <returns>
        /// The number of <see cref="SQLiteValue" /> object instances that were
        /// successfully persisted.
        /// </returns>
        protected virtual int TryPersistValues(
            SQLiteValue[] values
            )
        {
            int result = 0;

            if (values != null)
            {
                foreach (SQLiteValue value in values)
                {
                    if (value == null)
                        continue;

                    if (value.Persist())
                        result++;
                }
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method should normally be used by the
        /// <see cref="ISQLiteManagedModule.Filter" /> method in order to
        /// perform filtering of the result rows and/or to record the filtering
        /// criteria provided by the SQLite core library.
        /// </summary>
        /// <param name="indexNumber">
        /// Number used to help identify the selected index.
        /// </param>
        /// <param name="indexString">
        /// String used to help identify the selected index.
        /// </param>
        /// <param name="values">
        /// The values corresponding to each column in the selected index.
        /// </param>
        public virtual void Filter(
            int indexNumber,
            string indexString,
            SQLiteValue[] values
            )
        {
            CheckDisposed();

            if ((values != null) &&
                (TryPersistValues(values) != values.Length))
            {
                throw new SQLiteException(
                    "failed to persist one or more values");
            }

            this.indexNumber = indexNumber;
            this.indexString = indexString;
            this.values = values;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines the integer row sequence number for the current row.
        /// </summary>
        /// <returns>
        /// The integer row sequence number for the current row -OR- zero if
        /// it cannot be determined.
        /// </returns>
        public virtual int GetRowIndex()
        {
            return rowIndex;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adjusts the integer row sequence number so that it refers to the
        /// next row.
        /// </summary>
        public virtual void NextRowIndex()
        {
            rowIndex++;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISQLiteNativeHandle Members
        private IntPtr nativeHandle;
        /// <summary>
        /// Returns the underlying SQLite native handle associated with this
        /// object instance.
        /// </summary>
        public virtual IntPtr NativeHandle
        {
            get { CheckDisposed(); return nativeHandle; }
            internal set { nativeHandle = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        /// <summary>
        /// Disposes of this object instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        /// <summary>
        /// Throws an <see cref="ObjectDisposedException" /> if this object
        /// instance has been disposed.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed)
            {
                throw new ObjectDisposedException(
                    typeof(SQLiteVirtualTableCursor).Name);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Disposes of this object instance.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <see cref="Dispose()" /> method.  Zero if this method is being called
        /// from the finalizer.
        /// </param>
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

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes this object instance.
        /// </summary>
        ~SQLiteVirtualTableCursor()
        {
            Dispose(false);
        }
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region ISQLiteNativeHandle Interface
    /// <summary>
    /// This interface represents a native handle provided by the SQLite core
    /// library.
    /// </summary>
    public interface ISQLiteNativeHandle
    {
        /// <summary>
        /// The native handle value.
        /// </summary>
        IntPtr NativeHandle { get; }
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region ISQLiteNativeModule Interface
    /// <summary>
    /// This interface represents a virtual table implementation written in
    /// native code.
    /// </summary>
    public interface ISQLiteNativeModule
    {
        /// <summary>
        /// <para>
        /// This method is called to create a new instance of a virtual table
        /// in response to a CREATE VIRTUAL TABLE statement. The db parameter
        /// is a pointer to the SQLite database connection that is executing
        /// the CREATE VIRTUAL TABLE statement. The pAux argument is the copy
        /// of the client data pointer that was the fourth argument to the
        /// sqlite3_create_module() or sqlite3_create_module_v2() call that
        /// registered the virtual table module. The argv parameter is an
        /// array of argc pointers to null terminated strings. The first
        /// string, argv[0], is the name of the module being invoked. The
        /// module name is the name provided as the second argument to
        /// sqlite3_create_module() and as the argument to the USING clause of
        /// the CREATE VIRTUAL TABLE statement that is running. The second,
        /// argv[1], is the name of the database in which the new virtual table
        /// is being created. The database name is "main" for the primary
        /// database, or "temp" for TEMP database, or the name given at the
        /// end of the ATTACH statement for attached databases. The third
        /// element of the array, argv[2], is the name of the new virtual
        /// table, as specified following the TABLE keyword in the CREATE
        /// VIRTUAL TABLE statement. If present, the fourth and subsequent
        /// strings in the argv[] array report the arguments to the module name
        /// in the CREATE VIRTUAL TABLE statement.
        /// </para>
        /// <para>
        /// The job of this method is to construct the new virtual table object
        /// (an sqlite3_vtab object) and return a pointer to it in *ppVTab.
        /// </para>
        /// <para>
        /// As part of the task of creating a new sqlite3_vtab structure, this
        /// method must invoke sqlite3_declare_vtab() to tell the SQLite core
        /// about the columns and datatypes in the virtual table. The
        /// sqlite3_declare_vtab() API has the following prototype:
        /// </para>
        /// <para>
        /// <code>
        /// int sqlite3_declare_vtab(sqlite3 *db, const char *zCreateTable)
        /// </code>
        /// </para>
        /// <para>
        /// The first argument to sqlite3_declare_vtab() must be the same
        /// database connection pointer as the first parameter to this method.
        /// The second argument to sqlite3_declare_vtab() must a
        /// zero-terminated UTF-8 string that contains a well-formed CREATE
        /// TABLE statement that defines the columns in the virtual table and
        /// their data types. The name of the table in this CREATE TABLE
        /// statement is ignored, as are all constraints. Only the column names
        /// and datatypes matter. The CREATE TABLE statement string need not to
        /// be held in persistent memory. The string can be deallocated and/or
        /// reused as soon as the sqlite3_declare_vtab() routine returns.
        /// </para>
        /// </summary>
        /// <param name="pDb">
        /// The native database connection handle.
        /// </param>
        /// <param name="pAux">
        /// The original native pointer value that was provided to the
        /// sqlite3_create_module(), sqlite3_create_module_v2() or
        /// sqlite3_create_disposable_module() functions.
        /// </param>
        /// <param name="argc">
        /// The number of arguments from the CREATE VIRTUAL TABLE statement.
        /// </param>
        /// <param name="argv">
        /// The array of string arguments from the CREATE VIRTUAL TABLE
        /// statement.
        /// </param>
        /// <param name="pVtab">
        /// Upon success, this parameter must be modified to point to the newly
        /// created native sqlite3_vtab derived structure.
        /// </param>
        /// <param name="pError">
        /// Upon failure, this parameter must be modified to point to the error
        /// message, with the underlying memory having been obtained from the
        /// sqlite3_malloc() function.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode xCreate(
            IntPtr pDb,
            IntPtr pAux,
            int argc,
            IntPtr argv,
            ref IntPtr pVtab,
            ref IntPtr pError
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// <para>
        /// The xConnect method is very similar to xCreate. It has the same
        /// parameters and constructs a new sqlite3_vtab structure just like
        /// xCreate. And it must also call sqlite3_declare_vtab() like xCreate.
        /// </para>
        /// <para>
        /// The difference is that xConnect is called to establish a new
        /// connection to an existing virtual table whereas xCreate is called
        /// to create a new virtual table from scratch.
        /// </para>
        /// <para>
        /// The xCreate and xConnect methods are only different when the
        /// virtual table has some kind of backing store that must be
        /// initialized the first time the virtual table is created. The
        /// xCreate method creates and initializes the backing store. The
        /// xConnect method just connects to an existing backing store.
        /// </para>
        /// <para>
        /// As an example, consider a virtual table implementation that
        /// provides read-only access to existing comma-separated-value (CSV)
        /// files on disk. There is no backing store that needs to be created
        /// or initialized for such a virtual table (since the CSV files
        /// already exist on disk) so the xCreate and xConnect methods will be
        /// identical for that module.
        /// </para>
        /// <para>
        /// Another example is a virtual table that implements a full-text
        /// index. The xCreate method must create and initialize data
        /// structures to hold the dictionary and posting lists for that index.
        /// The xConnect method, on the other hand, only has to locate and use
        /// an existing dictionary and posting lists that were created by a
        /// prior xCreate call.
        /// </para>
        /// <para>
        /// The xConnect method must return SQLITE_OK if it is successful in
        /// creating the new virtual table, or SQLITE_ERROR if it is not
        /// successful. If not successful, the sqlite3_vtab structure must not
        /// be allocated. An error message may optionally be returned in *pzErr
        /// if unsuccessful. Space to hold the error message string must be
        /// allocated using an SQLite memory allocation function like
        /// sqlite3_malloc() or sqlite3_mprintf() as the SQLite core will
        /// attempt to free the space using sqlite3_free() after the error has
        /// been reported up to the application.
        /// </para>
        /// <para>
        /// The xConnect method is required for every virtual table
        /// implementation, though the xCreate and xConnect pointers of the
        /// sqlite3_module object may point to the same function the virtual
        /// table does not need to initialize backing store.
        /// </para>
        /// </summary>
        /// <param name="pDb">
        /// The native database connection handle.
        /// </param>
        /// <param name="pAux">
        /// The original native pointer value that was provided to the
        /// sqlite3_create_module(), sqlite3_create_module_v2() or
        /// sqlite3_create_disposable_module() functions.
        /// </param>
        /// <param name="argc">
        /// The number of arguments from the CREATE VIRTUAL TABLE statement.
        /// </param>
        /// <param name="argv">
        /// The array of string arguments from the CREATE VIRTUAL TABLE
        /// statement.
        /// </param>
        /// <param name="pVtab">
        /// Upon success, this parameter must be modified to point to the newly
        /// created native sqlite3_vtab derived structure.
        /// </param>
        /// <param name="pError">
        /// Upon failure, this parameter must be modified to point to the error
        /// message, with the underlying memory having been obtained from the
        /// sqlite3_malloc() function.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode xConnect(
            IntPtr pDb,
            IntPtr pAux,
            int argc,
            IntPtr argv,
            ref IntPtr pVtab,
            ref IntPtr pError
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// <para>
        /// SQLite uses the xBestIndex method of a virtual table module to
        /// determine the best way to access the virtual table. The xBestIndex
        /// method has a prototype like this:
        /// </para>
        /// <code>
        /// int (*xBestIndex)(sqlite3_vtab *pVTab, sqlite3_index_info*);
        /// </code>
        /// <para>
        /// The SQLite core communicates with the xBestIndex method by filling
        /// in certain fields of the sqlite3_index_info structure and passing a
        /// pointer to that structure into xBestIndex as the second parameter.
        /// The xBestIndex method fills out other fields of this structure
        /// which forms the reply. The sqlite3_index_info structure looks like
        /// this:
        /// </para>
        /// <code>
        ///  struct sqlite3_index_info {
        ///    /* Inputs */
        ///    const int nConstraint;   /* Number of entries in aConstraint */
        ///    const struct sqlite3_index_constraint {
        ///       int iColumn;          /* Column on left-hand side of
        ///                              * constraint */
        ///       unsigned char op;     /* Constraint operator */
        ///       unsigned char usable; /* True if this constraint is usable */
        ///       int iTermOffset;      /* Used internally - xBestIndex should
        ///                              * ignore */
        ///    } *const aConstraint;    /* Table of WHERE clause constraints */
        ///    const int nOrderBy;      /* Number of terms in the ORDER BY
        ///                              * clause */
        ///    const struct sqlite3_index_orderby {
        ///       int iColumn;          /* Column number */
        ///       unsigned char desc;   /* True for DESC.  False for ASC. */
        ///    } *const aOrderBy;       /* The ORDER BY clause */
        ///    /* Outputs */
        ///    struct sqlite3_index_constraint_usage {
        ///      int argvIndex;         /* if greater than zero, constraint is
        ///                              * part of argv to xFilter */
        ///      unsigned char omit;    /* Do not code a test for this
        ///                              * constraint */
        ///    } *const aConstraintUsage;
        ///    int idxNum;              /* Number used to identify the index */
        ///    char *idxStr;            /* String, possibly obtained from
        ///                              * sqlite3_malloc() */
        ///    int needToFreeIdxStr;    /* Free idxStr using sqlite3_free() if
        ///                              * true */
        ///    int orderByConsumed;     /* True if output is already ordered */
        ///    double estimatedCost;    /* Estimated cost of using this index */
        ///  };
        /// </code>
        /// <para>
        /// In addition, there are some defined constants:
        /// </para>
        /// <code>
        ///  #define SQLITE_INDEX_CONSTRAINT_EQ    2
        ///  #define SQLITE_INDEX_CONSTRAINT_GT    4
        ///  #define SQLITE_INDEX_CONSTRAINT_LE    8
        ///  #define SQLITE_INDEX_CONSTRAINT_LT    16
        ///  #define SQLITE_INDEX_CONSTRAINT_GE    32
        ///  #define SQLITE_INDEX_CONSTRAINT_MATCH 64
        /// </code>
        /// <para>
        /// The SQLite core calls the xBestIndex method when it is compiling a
        /// query that involves a virtual table. In other words, SQLite calls
        /// this method when it is running sqlite3_prepare() or the equivalent.
        /// By calling this method, the SQLite core is saying to the virtual
        /// table that it needs to access some subset of the rows in the
        /// virtual table and it wants to know the most efficient way to do
        /// that access. The xBestIndex method replies with information that
        /// the SQLite core can then use to conduct an efficient search of the
        /// virtual table.
        /// </para>
        /// <para>
        /// While compiling a single SQL query, the SQLite core might call
        /// xBestIndex multiple times with different settings in
        /// sqlite3_index_info. The SQLite core will then select the
        /// combination that appears to give the best performance.
        /// </para>
        /// <para>
        /// Before calling this method, the SQLite core initializes an instance
        /// of the sqlite3_index_info structure with information about the
        /// query that it is currently trying to process. This information
        /// derives mainly from the WHERE clause and ORDER BY or GROUP BY
        /// clauses of the query, but also from any ON or USING clauses if the
        /// query is a join. The information that the SQLite core provides to
        /// the xBestIndex method is held in the part of the structure that is
        /// marked as "Inputs". The "Outputs" section is initialized to zero.
        /// </para>
        /// <para>
        /// The information in the sqlite3_index_info structure is ephemeral
        /// and may be overwritten or deallocated as soon as the xBestIndex
        /// method returns. If the xBestIndex method needs to remember any part
        /// of the sqlite3_index_info structure, it should make a copy. Care
        /// must be take to store the copy in a place where it will be
        /// deallocated, such as in the idxStr field with needToFreeIdxStr set
        /// to 1.
        /// </para>
        /// <para>
        /// Note that xBestIndex will always be called before xFilter, since
        /// the idxNum and idxStr outputs from xBestIndex are required inputs
        /// to xFilter. However, there is no guarantee that xFilter will be
        /// called following a successful xBestIndex.
        /// </para>
        /// <para>
        /// The xBestIndex method is required for every virtual table
        /// implementation.
        /// </para>
        /// <para>
        /// 2.3.1 Inputs
        /// </para>
        /// <para>
        /// The main thing that the SQLite core is trying to communicate to the
        /// virtual table is the constraints that are available to limit the
        /// number of rows that need to be searched. The aConstraint[] array
        /// contains one entry for each constraint. There will be exactly
        /// nConstraint entries in that array.
        /// </para>
        /// <para>
        /// Each constraint will correspond to a term in the WHERE clause or in
        /// a USING or ON clause that is of the form
        /// </para>
        /// <code>
        ///     column OP EXPR
        /// </code>
        /// <para>
        /// Where "column" is a column in the virtual table, OP is an operator
        /// like "=" or "&lt;", and EXPR is an arbitrary expression. So, for
        /// example, if the WHERE clause contained a term like this:
        /// </para>
        /// <code>
        ///          a = 5
        /// </code>
        /// <para>
        /// Then one of the constraints would be on the "a" column with
        /// operator "=" and an expression of "5". Constraints need not have a
        /// literal representation of the WHERE clause. The query optimizer
        /// might make transformations to the WHERE clause in order to extract
        /// as many constraints as it can. So, for example, if the WHERE clause
        /// contained something like this:
        /// </para>
        /// <code>
        ///          x BETWEEN 10 AND 100 AND 999&gt;y
        /// </code>
        /// <para>
        /// The query optimizer might translate this into three separate
        /// constraints:
        /// </para>
        /// <code>
        ///          x &gt;= 10
        ///          x &lt;= 100
        ///          y &lt; 999
        /// </code>
        /// <para>
        /// For each constraint, the aConstraint[].iColumn field indicates
        /// which column appears on the left-hand side of the constraint. The
        /// first column of the virtual table is column 0. The rowid of the
        /// virtual table is column -1. The aConstraint[].op field indicates
        /// which operator is used. The SQLITE_INDEX_CONSTRAINT_* constants map
        /// integer constants into operator values. Columns occur in the order
        /// they were defined by the call to sqlite3_declare_vtab() in the
        /// xCreate or xConnect method. Hidden columns are counted when
        /// determining the column index.
        /// </para>
        /// <para>
        /// The aConstraint[] array contains information about all constraints
        /// that apply to the virtual table. But some of the constraints might
        /// not be usable because of the way tables are ordered in a join. The
        /// xBestIndex method must therefore only consider constraints that
        /// have an aConstraint[].usable flag which is true.
        /// </para>
        /// <para>
        /// In addition to WHERE clause constraints, the SQLite core also tells
        /// the xBestIndex method about the ORDER BY clause. (In an aggregate
        /// query, the SQLite core might put in GROUP BY clause information in
        /// place of the ORDER BY clause information, but this fact should not
        /// make any difference to the xBestIndex method.) If all terms of the
        /// ORDER BY clause are columns in the virtual table, then nOrderBy
        /// will be the number of terms in the ORDER BY clause and the
        /// aOrderBy[] array will identify the column for each term in the
        /// order by clause and whether or not that column is ASC or DESC.
        /// </para>
        /// <para>
        /// 2.3.2 Outputs
        /// </para>
        /// <para>
        /// Given all of the information above, the job of the xBestIndex
        /// method it to figure out the best way to search the virtual table.
        /// </para>
        /// <para>
        /// The xBestIndex method fills the idxNum and idxStr fields with
        /// information that communicates an indexing strategy to the xFilter
        /// method. The information in idxNum and idxStr is arbitrary as far as
        /// the SQLite core is concerned. The SQLite core just copies the
        /// information through to the xFilter method. Any desired meaning can
        /// be assigned to idxNum and idxStr as long as xBestIndex and xFilter
        /// agree on what that meaning is.
        /// </para>
        /// <para>
        /// The idxStr value may be a string obtained from an SQLite memory
        /// allocation function such as sqlite3_mprintf(). If this is the case,
        /// then the needToFreeIdxStr flag must be set to true so that the
        /// SQLite core will know to call sqlite3_free() on that string when it
        /// has finished with it, and thus avoid a memory leak.
        /// </para>
        /// <para>
        /// If the virtual table will output rows in the order specified by the
        /// ORDER BY clause, then the orderByConsumed flag may be set to true.
        /// If the output is not automatically in the correct order then
        /// orderByConsumed must be left in its default false setting. This
        /// will indicate to the SQLite core that it will need to do a separate
        /// sorting pass over the data after it comes out of the virtual table.
        /// </para>
        /// <para>
        /// The estimatedCost field should be set to the estimated number of
        /// disk access operations required to execute this query against the
        /// virtual table. The SQLite core will often call xBestIndex multiple
        /// times with different constraints, obtain multiple cost estimates,
        /// then choose the query plan that gives the lowest estimate.
        /// </para>
        /// <para>
        /// The aConstraintUsage[] array contains one element for each of the
        /// nConstraint constraints in the inputs section of the
        /// sqlite3_index_info structure. The aConstraintUsage[] array is used
        /// by xBestIndex to tell the core how it is using the constraints.
        /// </para>
        /// <para>
        /// The xBestIndex method may set aConstraintUsage[].argvIndex entries
        /// to values greater than one. Exactly one entry should be set to 1,
        /// another to 2, another to 3, and so forth up to as many or as few as
        /// the xBestIndex method wants. The EXPR of the corresponding
        /// constraints will then be passed in as the argv[] parameters to
        /// xFilter.
        /// </para>
        /// <para>
        /// For example, if the aConstraint[3].argvIndex is set to 1, then when
        /// xFilter is called, the argv[0] passed to xFilter will have the EXPR
        /// value of the aConstraint[3] constraint.
        /// </para>
        /// <para>
        /// By default, the SQLite core double checks all constraints on each
        /// row of the virtual table that it receives. If such a check is
        /// redundant, the xBestFilter method can suppress that double-check by
        /// setting aConstraintUsage[].omit.
        /// </para>
        /// </summary>
        /// <param name="pVtab">
        /// The native pointer to the sqlite3_vtab derived structure.
        /// </param>
        /// <param name="pIndex">
        /// The native pointer to the sqlite3_index_info structure.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode xBestIndex(
            IntPtr pVtab,
            IntPtr pIndex
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// <para>
        /// This method releases a connection to a virtual table. Only the
        /// sqlite3_vtab object is destroyed. The virtual table is not
        /// destroyed and any backing store associated with the virtual table
        /// persists. This method undoes the work of xConnect.
        /// </para>
        /// <para>
        /// This method is a destructor for a connection to the virtual table.
        /// Contrast this method with xDestroy. The xDestroy is a destructor
        /// for the entire virtual table.
        /// </para>
        /// <para>
        /// The xDisconnect method is required for every virtual table
        /// implementation, though it is acceptable for the xDisconnect and
        /// xDestroy methods to be the same function if that makes sense for
        /// the particular virtual table.
        /// </para>
        /// </summary>
        /// <param name="pVtab">
        /// The native pointer to the sqlite3_vtab derived structure.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode xDisconnect(
            IntPtr pVtab
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// <para>
        /// This method releases a connection to a virtual table, just like the
        /// xDisconnect method, and it also destroys the underlying table
        /// implementation. This method undoes the work of xCreate.
        /// </para>
        /// <para>
        /// The xDisconnect method is called whenever a database connection
        /// that uses a virtual table is closed. The xDestroy method is only
        /// called when a DROP TABLE statement is executed against the virtual
        /// table.
        /// </para>
        /// <para>
        /// The xDestroy method is required for every virtual table
        /// implementation, though it is acceptable for the xDisconnect and
        /// xDestroy methods to be the same function if that makes sense for
        /// the particular virtual table.
        /// </para>
        /// </summary>
        /// <param name="pVtab">
        /// The native pointer to the sqlite3_vtab derived structure.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode xDestroy(
            IntPtr pVtab
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// <para>
        /// The xOpen method creates a new cursor used for accessing (read
        /// and/or writing) a virtual table. A successful invocation of this
        /// method will allocate the memory for the sqlite3_vtab_cursor (or a
        /// subclass), initialize the new object, and make *ppCursor point to
        /// the new object. The successful call then returns SQLITE_OK.
        /// </para>
        /// <para>
        /// For every successful call to this method, the SQLite core will
        /// later invoke the xClose method to destroy the allocated cursor.
        /// </para>
        /// <para>
        /// The xOpen method need not initialize the pVtab field of the
        /// sqlite3_vtab_cursor structure. The SQLite core will take care of
        /// that chore automatically.
        /// </para>
        /// <para>
        /// A virtual table implementation must be able to support an arbitrary
        /// number of simultaneously open cursors.
        /// </para>
        /// <para>
        /// When initially opened, the cursor is in an undefined state. The
        /// SQLite core will invoke the xFilter method on the cursor prior to
        /// any attempt to position or read from the cursor.
        /// </para>
        /// <para>
        /// The xOpen method is required for every virtual table
        /// implementation.
        /// </para>
        /// </summary>
        /// <param name="pVtab">
        /// The native pointer to the sqlite3_vtab derived structure.
        /// </param>
        /// <param name="pCursor">
        /// Upon success, this parameter must be modified to point to the newly
        /// created native sqlite3_vtab_cursor derived structure.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode xOpen(
            IntPtr pVtab,
            ref IntPtr pCursor
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// <para>
        /// The xClose method closes a cursor previously opened by xOpen. The
        /// SQLite core will always call xClose once for each cursor opened
        /// using xOpen.
        /// </para>
        /// <para>
        /// This method must release all resources allocated by the
        /// corresponding xOpen call. The routine will not be called again even
        /// if it returns an error. The SQLite core will not use the
        /// sqlite3_vtab_cursor again after it has been closed.
        /// </para>
        /// <para>
        /// The xClose method is required for every virtual table
        /// implementation.
        /// </para>
        /// </summary>
        /// <param name="pCursor">
        /// The native pointer to the sqlite3_vtab_cursor derived structure.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode xClose(
            IntPtr pCursor
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// <para>
        /// This method begins a search of a virtual table. The first argument
        /// is a cursor opened by xOpen. The next two argument define a
        /// particular search index previously chosen by xBestIndex. The
        /// specific meanings of idxNum and idxStr are unimportant as long as
        /// xFilter and xBestIndex agree on what that meaning is.
        /// </para>
        /// <para>
        /// The xBestIndex function may have requested the values of certain
        /// expressions using the aConstraintUsage[].argvIndex values of the
        /// sqlite3_index_info structure. Those values are passed to xFilter
        /// using the argc and argv parameters.
        /// </para>
        /// <para>
        /// If the virtual table contains one or more rows that match the
        /// search criteria, then the cursor must be left point at the first
        /// row. Subsequent calls to xEof must return false (zero). If there
        /// are no rows match, then the cursor must be left in a state that
        /// will cause the xEof to return true (non-zero). The SQLite engine
        /// will use the xColumn and xRowid methods to access that row content.
        /// The xNext method will be used to advance to the next row.
        /// </para>
        /// <para>
        /// This method must return SQLITE_OK if successful, or an sqlite error
        /// code if an error occurs.
        /// </para>
        /// <para>
        /// The xFilter method is required for every virtual table
        /// implementation.
        /// </para>
        /// </summary>
        /// <param name="pCursor">
        /// The native pointer to the sqlite3_vtab_cursor derived structure.
        /// </param>
        /// <param name="idxNum">
        /// Number used to help identify the selected index.
        /// </param>
        /// <param name="idxStr">
        /// The native pointer to the UTF-8 encoded string containing the
        /// string used to help identify the selected index.
        /// </param>
        /// <param name="argc">
        /// The number of native pointers to sqlite3_value structures specified
        /// in <paramref name="argv" />.
        /// </param>
        /// <param name="argv">
        /// An array of native pointers to sqlite3_value structures containing
        /// filtering criteria for the selected index.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode xFilter(
            IntPtr pCursor,
            int idxNum,
            IntPtr idxStr,
            int argc,
            IntPtr argv
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// <para>
        /// The xNext method advances a virtual table cursor to the next row of
        /// a result set initiated by xFilter. If the cursor is already
        /// pointing at the last row when this routine is called, then the
        /// cursor no longer points to valid data and a subsequent call to the
        /// xEof method must return true (non-zero). If the cursor is
        /// successfully advanced to another row of content, then subsequent
        /// calls to xEof must return false (zero).
        /// </para>
        /// <para>
        /// This method must return SQLITE_OK if successful, or an sqlite error
        /// code if an error occurs.
        /// </para>
        /// <para>
        /// The xNext method is required for every virtual table
        /// implementation.
        /// </para>
        /// </summary>
        /// <param name="pCursor">
        /// The native pointer to the sqlite3_vtab_cursor derived structure.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode xNext(
            IntPtr pCursor
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// <para>
        /// The xEof method must return false (zero) if the specified cursor
        /// currently points to a valid row of data, or true (non-zero)
        /// otherwise. This method is called by the SQL engine immediately
        /// after each xFilter and xNext invocation.
        /// </para>
        /// <para>
        /// The xEof method is required for every virtual table implementation.
        /// </para>
        /// </summary>
        /// <param name="pCursor">
        /// The native pointer to the sqlite3_vtab_cursor derived structure.
        /// </param>
        /// <returns>
        /// Non-zero if no more rows are available; zero otherwise.
        /// </returns>
        int xEof(
            IntPtr pCursor
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// <para>
        /// The SQLite core invokes this method in order to find the value for
        /// the N-th column of the current row. N is zero-based so the first
        /// column is numbered 0. The xColumn method may return its result back
        /// to SQLite using one of the following interface:
        /// </para>
        /// <code>
        ///     sqlite3_result_blob()
        ///     sqlite3_result_double()
        ///     sqlite3_result_int()
        ///     sqlite3_result_int64()
        ///     sqlite3_result_null()
        ///     sqlite3_result_text()
        ///     sqlite3_result_text16()
        ///     sqlite3_result_text16le()
        ///     sqlite3_result_text16be()
        ///     sqlite3_result_zeroblob()
        /// </code>
        /// <para>
        /// If the xColumn method implementation calls none of the functions
        /// above, then the value of the column defaults to an SQL NULL.
        /// </para>
        /// <para>
        /// To raise an error, the xColumn method should use one of the
        /// result_text() methods to set the error message text, then return an
        /// appropriate error code. The xColumn method must return SQLITE_OK on
        /// success.
        /// </para>
        /// <para>
        /// The xColumn method is required for every virtual table
        /// implementation.
        /// </para>
        /// </summary>
        /// <param name="pCursor">
        /// The native pointer to the sqlite3_vtab_cursor derived structure.
        /// </param>
        /// <param name="pContext">
        /// The native pointer to the sqlite3_context structure to be used
        /// for returning the specified column value to the SQLite core
        /// library.
        /// </param>
        /// <param name="index">
        /// The zero-based index corresponding to the column containing the
        /// value to be returned.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode xColumn(
            IntPtr pCursor,
            IntPtr pContext,
            int index
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// <para>
        /// A successful invocation of this method will cause *pRowid to be
        /// filled with the rowid of row that the virtual table cursor pCur is
        /// currently pointing at. This method returns SQLITE_OK on success. It
        /// returns an appropriate error code on failure.
        /// </para>
        /// <para>
        /// The xRowid method is required for every virtual table
        /// implementation.
        /// </para>
        /// </summary>
        /// <param name="pCursor">
        /// The native pointer to the sqlite3_vtab_cursor derived structure.
        /// </param>
        /// <param name="rowId">
        /// Upon success, this parameter must be modified to contain the unique
        /// integer row identifier for the current row for the specified cursor.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode xRowId(
            IntPtr pCursor,
            ref long rowId
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// <para>
        /// All changes to a virtual table are made using the xUpdate method.
        /// This one method can be used to insert, delete, or update.
        /// </para>
        /// <para>
        /// The argc parameter specifies the number of entries in the argv
        /// array. The value of argc will be 1 for a pure delete operation or
        /// N+2 for an insert or replace or update where N is the number of
        /// columns in the table. In the previous sentence, N includes any
        /// hidden columns.
        /// </para>
        /// <para>
        /// Every argv entry will have a non-NULL value in C but may contain
        /// the SQL value NULL. In other words, it is always true that
        /// argv[i]!=0 for i between 0 and argc-1. However, it might be the
        /// case that sqlite3_value_type(argv[i])==SQLITE_NULL.
        /// </para>
        /// <para>
        /// The argv[0] parameter is the rowid of a row in the virtual table
        /// to be deleted. If argv[0] is an SQL NULL, then no deletion occurs.
        /// </para>
        /// <para>
        /// The argv[1] parameter is the rowid of a new row to be inserted into
        /// the virtual table. If argv[1] is an SQL NULL, then the
        /// implementation must choose a rowid for the newly inserted row.
        /// Subsequent argv[] entries contain values of the columns of the
        /// virtual table, in the order that the columns were declared. The
        /// number of columns will match the table declaration that the
        /// xConnect or xCreate method made using the sqlite3_declare_vtab()
        /// call. All hidden columns are included.
        /// </para>
        /// <para>
        /// When doing an insert without a rowid (argc>1, argv[1] is an SQL
        /// NULL), the implementation must set *pRowid to the rowid of the
        /// newly inserted row; this will become the value returned by the
        /// sqlite3_last_insert_rowid() function. Setting this value in all the
        /// other cases is a harmless no-op; the SQLite engine ignores the
        /// *pRowid return value if argc==1 or argv[1] is not an SQL NULL.
        /// </para>
        /// <para>
        /// Each call to xUpdate will fall into one of cases shown below. Note
        /// that references to argv[i] mean the SQL value held within the
        /// argv[i] object, not the argv[i] object itself.
        /// </para>
        /// <code>
        ///     argc = 1
        /// </code>
        /// <para>
        ///         The single row with rowid equal to argv[0] is deleted. No
        ///         insert occurs.
        /// </para>
        /// <code>
        ///     argc > 1
        ///     argv[0] = NULL
        /// </code>
        /// <para>
        ///         A new row is inserted with a rowid argv[1] and column
        ///         values in argv[2] and following. If argv[1] is an SQL NULL,
        ///         the a new unique rowid is generated automatically.
        /// </para>
        /// <code>
        ///     argc > 1
        ///     argv[0] ? NULL
        ///     argv[0] = argv[1]
        /// </code>
        /// <para>
        ///         The row with rowid argv[0] is updated with new values in
        ///         argv[2] and following parameters.
        /// </para>
        /// <code>
        ///     argc > 1
        ///     argv[0] ? NULL
        ///     argv[0] ? argv[1]
        /// </code>
        /// <para>
        ///         The row with rowid argv[0] is updated with rowid argv[1]
        ///         and new values in argv[2] and following parameters. This
        ///         will occur when an SQL statement updates a rowid, as in
        ///         the statement:
        /// </para>
        /// <code>
        ///             UPDATE table SET rowid=rowid+1 WHERE ...;
        /// </code>
        /// <para>
        /// The xUpdate method must return SQLITE_OK if and only if it is
        /// successful. If a failure occurs, the xUpdate must return an
        /// appropriate error code. On a failure, the pVTab->zErrMsg element
        /// may optionally be replaced with error message text stored in memory
        /// allocated from SQLite using functions such as sqlite3_mprintf() or
        /// sqlite3_malloc().
        /// </para>
        /// <para>
        /// If the xUpdate method violates some constraint of the virtual table
        /// (including, but not limited to, attempting to store a value of the
        /// wrong datatype, attempting to store a value that is too large or
        /// too small, or attempting to change a read-only value) then the
        /// xUpdate must fail with an appropriate error code.
        /// </para>
        /// <para>
        /// There might be one or more sqlite3_vtab_cursor objects open and in
        /// use on the virtual table instance and perhaps even on the row of
        /// the virtual table when the xUpdate method is invoked. The
        /// implementation of xUpdate must be prepared for attempts to delete
        /// or modify rows of the table out from other existing cursors. If the
        /// virtual table cannot accommodate such changes, the xUpdate method
        /// must return an error code.
        /// </para>
        /// <para>
        /// The xUpdate method is optional. If the xUpdate pointer in the
        /// sqlite3_module for a virtual table is a NULL pointer, then the
        /// virtual table is read-only.
        /// </para>
        /// </summary>
        /// <param name="pVtab">
        /// The native pointer to the sqlite3_vtab derived structure.
        /// </param>
        /// <param name="argc">
        /// The number of new or modified column values contained in
        /// <paramref name="argv" />.
        /// </param>
        /// <param name="argv">
        /// The array of native pointers to sqlite3_value structures containing
        /// the new or modified column values, if any.
        /// </param>
        /// <param name="rowId">
        /// Upon success, this parameter must be modified to contain the unique
        /// integer row identifier for the row that was inserted, if any.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode xUpdate(
            IntPtr pVtab,
            int argc,
            IntPtr argv,
            ref long rowId
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// <para>
        /// This method begins a transaction on a virtual table. This is method
        /// is optional. The xBegin pointer of sqlite3_module may be NULL.
        /// </para>
        /// <para>
        /// This method is always followed by one call to either the xCommit or
        /// xRollback method. Virtual table transactions do not nest, so the
        /// xBegin method will not be invoked more than once on a single
        /// virtual table without an intervening call to either xCommit or
        /// xRollback. Multiple calls to other methods can and likely will
        /// occur in between the xBegin and the corresponding xCommit or
        /// xRollback.
        /// </para>
        /// </summary>
        /// <param name="pVtab">
        /// The native pointer to the sqlite3_vtab derived structure.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode xBegin(
            IntPtr pVtab
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// <para>
        /// This method signals the start of a two-phase commit on a virtual
        /// table. This is method is optional. The xSync pointer of
        /// sqlite3_module may be NULL.
        /// </para>
        /// <para>
        /// This method is only invoked after call to the xBegin method and
        /// prior to an xCommit or xRollback. In order to implement two-phase
        /// commit, the xSync method on all virtual tables is invoked prior to
        /// invoking the xCommit method on any virtual table. If any of the
        /// xSync methods fail, the entire transaction is rolled back.
        /// </para>
        /// </summary>
        /// <param name="pVtab">
        /// The native pointer to the sqlite3_vtab derived structure.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode xSync(
            IntPtr pVtab
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// <para>
        /// This method causes a virtual table transaction to commit. This is
        /// method is optional. The xCommit pointer of sqlite3_module may be
        /// NULL.
        /// </para>
        /// <para>
        /// A call to this method always follows a prior call to xBegin and
        /// xSync.
        /// </para>
        /// </summary>
        /// <param name="pVtab">
        /// The native pointer to the sqlite3_vtab derived structure.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode xCommit(
            IntPtr pVtab
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// <para>
        /// This method causes a virtual table transaction to rollback. This is
        /// method is optional. The xRollback pointer of sqlite3_module may be
        /// NULL.
        /// </para>
        /// <para>
        /// A call to this method always follows a prior call to xBegin.
        /// </para>
        /// </summary>
        /// <param name="pVtab">
        /// The native pointer to the sqlite3_vtab derived structure.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode xRollback(
            IntPtr pVtab
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// <para>
        /// This method provides notification that the virtual table
        /// implementation that the virtual table will be given a new name. If
        /// this method returns SQLITE_OK then SQLite renames the table. If
        /// this method returns an error code then the renaming is prevented.
        /// </para>
        /// <para>
        /// The xRename method is required for every virtual table
        /// implementation.
        /// </para>
        /// </summary>
        /// <param name="pVtab">
        /// The native pointer to the sqlite3_vtab derived structure.
        /// </param>
        /// <param name="nArg">
        /// The number of arguments to the function being sought.
        /// </param>
        /// <param name="zName">
        /// The name of the function being sought.
        /// </param>
        /// <param name="callback">
        /// Upon success, this parameter must be modified to contain the
        /// delegate responsible for implementing the specified function.
        /// </param>
        /// <param name="pClientData">
        /// Upon success, this parameter must be modified to contain the
        /// native user-data pointer associated with
        /// <paramref name="callback" />.
        /// </param>
        /// <returns>
        /// Non-zero if the specified function was found; zero otherwise.
        /// </returns>
        int xFindFunction(
            IntPtr pVtab,
            int nArg,
            IntPtr zName,
            ref SQLiteCallback callback,
            ref IntPtr pClientData
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// <para>
        /// This method provides notification that the virtual table
        /// implementation that the virtual table will be given a new name. If
        /// this method returns SQLITE_OK then SQLite renames the table. If
        /// this method returns an error code then the renaming is prevented.
        /// </para>
        /// <para>
        /// The xRename method is required for every virtual table
        /// implementation.
        /// </para>
        /// </summary>
        /// <param name="pVtab">
        /// The native pointer to the sqlite3_vtab derived structure.
        /// </param>
        /// <param name="zNew">
        /// The native pointer to the UTF-8 encoded string containing the new
        /// name for the virtual table.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode xRename(
            IntPtr pVtab,
            IntPtr zNew
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// <para>
        /// These methods provide the virtual table implementation an
        /// opportunity to implement nested transactions. They are always
        /// optional and will only be called in SQLite version 3.7.7 and later.
        /// </para>
        /// <para>
        /// When xSavepoint(X,N) is invoked, that is a signal to the virtual
        /// table X that it should save its current state as savepoint N. A
        /// subsequent call to xRollbackTo(X,R) means that the state of the
        /// virtual table should return to what it was when xSavepoint(X,R) was
        /// last called. The call to xRollbackTo(X,R) will invalidate all
        /// savepoints with N>R; none of the invalided savepoints will be
        /// rolled back or released without first being reinitialized by a call
        /// to xSavepoint(). A call to xRelease(X,M) invalidates all savepoints
        /// where N>=M.
        /// </para>
        /// <para>
        /// None of the xSavepoint(), xRelease(), or xRollbackTo() methods will
        /// ever be called except in between calls to xBegin() and either
        /// xCommit() or xRollback().
        /// </para>
        /// </summary>
        /// <param name="pVtab">
        /// The native pointer to the sqlite3_vtab derived structure.
        /// </param>
        /// <param name="iSavepoint">
        /// This is an integer identifier under which the the current state of
        /// the virtual table should be saved.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode xSavepoint(
            IntPtr pVtab,
            int iSavepoint
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// <para>
        /// These methods provide the virtual table implementation an
        /// opportunity to implement nested transactions. They are always
        /// optional and will only be called in SQLite version 3.7.7 and later.
        /// </para>
        /// <para>
        /// When xSavepoint(X,N) is invoked, that is a signal to the virtual
        /// table X that it should save its current state as savepoint N. A
        /// subsequent call to xRollbackTo(X,R) means that the state of the
        /// virtual table should return to what it was when xSavepoint(X,R) was
        /// last called. The call to xRollbackTo(X,R) will invalidate all
        /// savepoints with N>R; none of the invalided savepoints will be
        /// rolled back or released without first being reinitialized by a call
        /// to xSavepoint(). A call to xRelease(X,M) invalidates all savepoints
        /// where N>=M.
        /// </para>
        /// <para>
        /// None of the xSavepoint(), xRelease(), or xRollbackTo() methods will
        /// ever be called except in between calls to xBegin() and either
        /// xCommit() or xRollback().
        /// </para>
        /// </summary>
        /// <param name="pVtab">
        /// The native pointer to the sqlite3_vtab derived structure.
        /// </param>
        /// <param name="iSavepoint">
        /// This is an integer used to indicate that any saved states with an
        /// identifier greater than or equal to this should be deleted by the
        /// virtual table.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode xRelease(
            IntPtr pVtab,
            int iSavepoint
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// <para>
        /// These methods provide the virtual table implementation an
        /// opportunity to implement nested transactions. They are always
        /// optional and will only be called in SQLite version 3.7.7 and later.
        /// </para>
        /// <para>
        /// When xSavepoint(X,N) is invoked, that is a signal to the virtual
        /// table X that it should save its current state as savepoint N. A
        /// subsequent call to xRollbackTo(X,R) means that the state of the
        /// virtual table should return to what it was when xSavepoint(X,R) was
        /// last called. The call to xRollbackTo(X,R) will invalidate all
        /// savepoints with N>R; none of the invalided savepoints will be
        /// rolled back or released without first being reinitialized by a call
        /// to xSavepoint(). A call to xRelease(X,M) invalidates all savepoints
        /// where N>=M.
        /// </para>
        /// <para>
        /// None of the xSavepoint(), xRelease(), or xRollbackTo() methods will
        /// ever be called except in between calls to xBegin() and either
        /// xCommit() or xRollback().
        /// </para>
        /// </summary>
        /// <param name="pVtab">
        /// The native pointer to the sqlite3_vtab derived structure.
        /// </param>
        /// <param name="iSavepoint">
        /// This is an integer identifier used to specify a specific saved
        /// state for the virtual table for it to restore itself back to, which
        /// should also have the effect of deleting all saved states with an
        /// integer identifier greater than this one.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode xRollbackTo(
            IntPtr pVtab,
            int iSavepoint
            );
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region ISQLiteManagedModule Interface
    /// <summary>
    /// This interface represents a virtual table implementation written in
    /// managed code.
    /// </summary>
    public interface ISQLiteManagedModule
    {
        /// <summary>
        /// Returns non-zero if the schema for the virtual table has been
        /// declared.
        /// </summary>
        bool Declared { get; }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns the name of the module as it was registered with the SQLite
        /// core library.
        /// </summary>
        string Name { get; }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xCreate" /> method.
        /// </summary>
        /// <param name="connection">
        /// The <see cref="SQLiteConnection" /> object instance associated with
        /// the virtual table.
        /// </param>
        /// <param name="pClientData">
        /// The native user-data pointer associated with this module, as it was
        /// provided to the SQLite core library when the native module instance
        /// was created.
        /// </param>
        /// <param name="arguments">
        /// The module name, database name, virtual table name, and all other
        /// arguments passed to the CREATE VIRTUAL TABLE statement.
        /// </param>
        /// <param name="table">
        /// Upon success, this parameter must be modified to contain the
        /// <see cref="SQLiteVirtualTable" /> object instance associated with
        /// the virtual table.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter must be modified to contain an error
        /// message.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode Create(
            SQLiteConnection connection,  /* in */
            IntPtr pClientData,           /* in */
            string[] arguments,           /* in */
            ref SQLiteVirtualTable table, /* out */
            ref string error              /* out */
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xConnect" /> method.
        /// </summary>
        /// <param name="connection">
        /// The <see cref="SQLiteConnection" /> object instance associated with
        /// the virtual table.
        /// </param>
        /// <param name="pClientData">
        /// The native user-data pointer associated with this module, as it was
        /// provided to the SQLite core library when the native module instance
        /// was created.
        /// </param>
        /// <param name="arguments">
        /// The module name, database name, virtual table name, and all other
        /// arguments passed to the CREATE VIRTUAL TABLE statement.
        /// </param>
        /// <param name="table">
        /// Upon success, this parameter must be modified to contain the
        /// <see cref="SQLiteVirtualTable" /> object instance associated with
        /// the virtual table.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter must be modified to contain an error
        /// message.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode Connect(
            SQLiteConnection connection,  /* in */
            IntPtr pClientData,           /* in */
            string[] arguments,           /* in */
            ref SQLiteVirtualTable table, /* out */
            ref string error              /* out */
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xBestIndex" /> method.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this virtual table.
        /// </param>
        /// <param name="index">
        /// The <see cref="SQLiteIndex" /> object instance containing all the
        /// data for the inputs and outputs relating to index selection.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode BestIndex(
            SQLiteVirtualTable table, /* in */
            SQLiteIndex index         /* in, out */
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xDisconnect" /> method.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this virtual table.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode Disconnect(
            SQLiteVirtualTable table /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xDestroy" /> method.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this virtual table.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode Destroy(
            SQLiteVirtualTable table /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xOpen" /> method.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this virtual table.
        /// </param>
        /// <param name="cursor">
        /// Upon success, this parameter must be modified to contain the
        /// <see cref="SQLiteVirtualTableCursor" /> object instance associated
        /// with the newly opened virtual table cursor.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode Open(
            SQLiteVirtualTable table,           /* in */
            ref SQLiteVirtualTableCursor cursor /* out */
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xClose" /> method.
        /// </summary>
        /// <param name="cursor">
        /// The <see cref="SQLiteVirtualTableCursor" /> object instance
        /// associated with the previously opened virtual table cursor to be
        /// used.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode Close(
            SQLiteVirtualTableCursor cursor /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xFilter" /> method.
        /// </summary>
        /// <param name="cursor">
        /// The <see cref="SQLiteVirtualTableCursor" /> object instance
        /// associated with the previously opened virtual table cursor to be
        /// used.
        /// </param>
        /// <param name="indexNumber">
        /// Number used to help identify the selected index.
        /// </param>
        /// <param name="indexString">
        /// String used to help identify the selected index.
        /// </param>
        /// <param name="values">
        /// The values corresponding to each column in the selected index.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode Filter(
            SQLiteVirtualTableCursor cursor, /* in */
            int indexNumber,                 /* in */
            string indexString,              /* in */
            SQLiteValue[] values             /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xNext" /> method.
        /// </summary>
        /// <param name="cursor">
        /// The <see cref="SQLiteVirtualTableCursor" /> object instance
        /// associated with the previously opened virtual table cursor to be
        /// used.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode Next(
            SQLiteVirtualTableCursor cursor /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xEof" /> method.
        /// </summary>
        /// <param name="cursor">
        /// The <see cref="SQLiteVirtualTableCursor" /> object instance
        /// associated with the previously opened virtual table cursor to be
        /// used.
        /// </param>
        /// <returns>
        /// Non-zero if no more rows are available; zero otherwise.
        /// </returns>
        bool Eof(
            SQLiteVirtualTableCursor cursor /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xColumn" /> method.
        /// </summary>
        /// <param name="cursor">
        /// The <see cref="SQLiteVirtualTableCursor" /> object instance
        /// associated with the previously opened virtual table cursor to be
        /// used.
        /// </param>
        /// <param name="context">
        /// The <see cref="SQLiteContext" /> object instance to be used for
        /// returning the specified column value to the SQLite core library.
        /// </param>
        /// <param name="index">
        /// The zero-based index corresponding to the column containing the
        /// value to be returned.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode Column(
            SQLiteVirtualTableCursor cursor, /* in */
            SQLiteContext context,           /* in */
            int index                        /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xRowId" /> method.
        /// </summary>
        /// <param name="cursor">
        /// The <see cref="SQLiteVirtualTableCursor" /> object instance
        /// associated with the previously opened virtual table cursor to be
        /// used.
        /// </param>
        /// <param name="rowId">
        /// Upon success, this parameter must be modified to contain the unique
        /// integer row identifier for the current row for the specified cursor.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode RowId(
            SQLiteVirtualTableCursor cursor, /* in */
            ref long rowId                   /* out */
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xUpdate" /> method.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this virtual table.
        /// </param>
        /// <param name="values">
        /// The array of <see cref="SQLiteValue" /> object instances containing
        /// the new or modified column values, if any.
        /// </param>
        /// <param name="rowId">
        /// Upon success, this parameter must be modified to contain the unique
        /// integer row identifier for the row that was inserted, if any.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode Update(
            SQLiteVirtualTable table, /* in */
            SQLiteValue[] values,     /* in */
            ref long rowId            /* out */
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xBegin" /> method.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this virtual table.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode Begin(
            SQLiteVirtualTable table /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xSync" /> method.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this virtual table.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode Sync(
            SQLiteVirtualTable table /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xCommit" /> method.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this virtual table.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode Commit(
            SQLiteVirtualTable table /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xRollback" /> method.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this virtual table.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode Rollback(
            SQLiteVirtualTable table /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xFindFunction" /> method.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this virtual table.
        /// </param>
        /// <param name="argumentCount">
        /// The number of arguments to the function being sought.
        /// </param>
        /// <param name="name">
        /// The name of the function being sought.
        /// </param>
        /// <param name="function">
        /// Upon success, this parameter must be modified to contain the
        /// <see cref="SQLiteFunction" /> object instance responsible for
        /// implementing the specified function.
        /// </param>
        /// <param name="pClientData">
        /// Upon success, this parameter must be modified to contain the
        /// native user-data pointer associated with
        /// <paramref name="function" />.
        /// </param>
        /// <returns>
        /// Non-zero if the specified function was found; zero otherwise.
        /// </returns>
        bool FindFunction(
            SQLiteVirtualTable table,    /* in */
            int argumentCount,           /* in */
            string name,                 /* in */
            ref SQLiteFunction function, /* out */
            ref IntPtr pClientData       /* out */
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xRename" /> method.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this virtual table.
        /// </param>
        /// <param name="newName">
        /// The new name for the virtual table.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode Rename(
            SQLiteVirtualTable table, /* in */
            string newName            /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xSavepoint" /> method.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this virtual table.
        /// </param>
        /// <param name="savepoint">
        /// This is an integer identifier under which the the current state of
        /// the virtual table should be saved.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode Savepoint(
            SQLiteVirtualTable table, /* in */
            int savepoint             /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xRelease" /> method.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this virtual table.
        /// </param>
        /// <param name="savepoint">
        /// This is an integer used to indicate that any saved states with an
        /// identifier greater than or equal to this should be deleted by the
        /// virtual table.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode Release(
            SQLiteVirtualTable table, /* in */
            int savepoint             /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xRollbackTo" /> method.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this virtual table.
        /// </param>
        /// <param name="savepoint">
        /// This is an integer identifier used to specify a specific saved
        /// state for the virtual table for it to restore itself back to, which
        /// should also have the effect of deleting all saved states with an
        /// integer identifier greater than this one.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        SQLiteErrorCode RollbackTo(
            SQLiteVirtualTable table, /* in */
            int savepoint             /* in */
            );
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteMemory Static Class
    /// <summary>
    /// This class contains static methods that are used to allocate,
    /// manipulate, and free native memory provided by the SQLite core library.
    /// </summary>
    internal static class SQLiteMemory
    {
        #region Private Data
#if TRACK_MEMORY_BYTES
        /// <summary>
        /// This object instance is used to synchronize access to the other
        /// static fields of this class.
        /// </summary>
        private static object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The total number of outstanding memory bytes allocated by this
        /// class using the SQLite core library.
        /// </summary>
        private static int bytesAllocated;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The maximum number of outstanding memory bytes ever allocated by
        /// this class using the SQLite core library.
        /// </summary>
        private static int maximumBytesAllocated;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Memory Allocation Helper Methods
        /// <summary>
        /// Allocates at least the specified number of bytes of native memory
        /// via the SQLite core library sqlite3_malloc() function and returns
        /// the resulting native pointer.
        /// </summary>
        /// <param name="size">
        /// The number of bytes to allocate.
        /// </param>
        /// <returns>
        /// The native pointer that points to a block of memory of at least the
        /// specified size -OR- <see cref="IntPtr.Zero" /> if the memory could
        /// not be allocated.
        /// </returns>
        public static IntPtr Allocate(int size)
        {
            IntPtr pMemory = UnsafeNativeMethods.sqlite3_malloc(size);

#if TRACK_MEMORY_BYTES
            if (pMemory != IntPtr.Zero)
            {
                int blockSize = Size(pMemory);

                if (blockSize > 0)
                {
                    lock (syncRoot)
                    {
                        bytesAllocated += blockSize;

                        if (bytesAllocated > maximumBytesAllocated)
                            maximumBytesAllocated = bytesAllocated;
                    }
                }
            }
#endif

            return pMemory;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets and returns the actual size of the specified memory block that
        /// was previously obtained from the <see cref="Allocate" /> method.
        /// </summary>
        /// <param name="pMemory">
        /// The native pointer to the memory block previously obtained from the
        /// <see cref="Allocate" /> method.
        /// </param>
        /// <returns>
        /// The actual size, in bytes, of the memory block specified via the
        /// native pointer.
        /// </returns>
        public static int Size(IntPtr pMemory)
        {
#if !SQLITE_STANDARD
            return UnsafeNativeMethods.sqlite3_malloc_size_interop(pMemory);
#elif TRACK_MEMORY_BYTES
            //
            // HACK: Ok, we cannot determine the size of the memory block;
            //       therefore, just track number of allocations instead.
            //
            return (pMemory != IntPtr.Zero) ? 1 : 0;
#else
            return 0;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Frees a memory block previously obtained from the
        /// <see cref="Allocate" /> method.
        /// </summary>
        /// <param name="pMemory">
        /// The native pointer to the memory block previously obtained from the
        /// <see cref="Allocate" /> method.
        /// </param>
        public static void Free(IntPtr pMemory)
        {
#if TRACK_MEMORY_BYTES
            if (pMemory != IntPtr.Zero)
            {
                int blockSize = Size(pMemory);

                if (blockSize > 0)
                {
                    lock (syncRoot)
                    {
                        bytesAllocated -= blockSize;
                    }
                }
            }
#endif

            UnsafeNativeMethods.sqlite3_free(pMemory);
        }
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteString Static Class
    /// <summary>
    /// This class contains static methods that are used to deal with native
    /// UTF-8 string pointers to be used with the SQLite core library.
    /// </summary>
    internal static class SQLiteString
    {
        #region Private Constants
        /// <summary>
        /// This is the maximum possible length for the native UTF-8 encoded
        /// strings used with the SQLite core library.
        /// </summary>
        private static int ThirtyBits = 0x3fffffff;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This is the <see cref="Encoding" /> object instance used to handle
        /// conversions from/to UTF-8.
        /// </summary>
        private static readonly Encoding Utf8Encoding = Encoding.UTF8;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region UTF-8 Encoding Helper Methods
        /// <summary>
        /// Converts the specified managed string into the UTF-8 encoding and
        /// returns the array of bytes containing its representation in that
        /// encoding.
        /// </summary>
        /// <param name="value">
        /// The managed string to convert.
        /// </param>
        /// <returns>
        /// The array of bytes containing the representation of the managed
        /// string in the UTF-8 encoding or null upon failure.
        /// </returns>
        public static byte[] GetUtf8BytesFromString(
            string value
            )
        {
            if (value == null)
                return null;

            return Utf8Encoding.GetBytes(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts the specified array of bytes representing a string in the
        /// UTF-8 encoding and returns a managed string.
        /// </summary>
        /// <param name="bytes">
        /// The array of bytes to convert.
        /// </param>
        /// <returns>
        /// The managed string or null upon failure.
        /// </returns>
        public static string GetStringFromUtf8Bytes(
            byte[] bytes
            )
        {
            if (bytes == null)
                return null;

#if !PLATFORM_COMPACTFRAMEWORK
            return Utf8Encoding.GetString(bytes);
#else
            return Utf8Encoding.GetString(bytes, 0, bytes.Length);
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region UTF-8 String Helper Methods
        /// <summary>
        /// Probes a native pointer to a string in the UTF-8 encoding for its
        /// terminating NUL character, within the specified length limit.
        /// </summary>
        /// <param name="pValue">
        /// The native NUL-terminated string pointer.
        /// </param>
        /// <param name="limit">
        /// The maximum length of the native string, in bytes.
        /// </param>
        /// <returns>
        /// The length of the native string, in bytes -OR- zero if the length
        /// could not be determined.
        /// </returns>
        public static int ProbeForUtf8ByteLength(
            IntPtr pValue,
            int limit
            )
        {
            int length = 0;

            if ((pValue != IntPtr.Zero) && (limit > 0))
            {
                do
                {
                    if (Marshal.ReadByte(pValue, length) == 0)
                        break;

                    if (length >= limit)
                        break;

                    length++;
                } while (true);
            }

            return length;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts the specified native NUL-terminated UTF-8 string pointer
        /// into a managed string.
        /// </summary>
        /// <param name="pValue">
        /// The native NUL-terminated UTF-8 string pointer.
        /// </param>
        /// <returns>
        /// The managed string or null upon failure.
        /// </returns>
        public static string StringFromUtf8IntPtr(
            IntPtr pValue
            )
        {
            return StringFromUtf8IntPtr(pValue,
                ProbeForUtf8ByteLength(pValue, ThirtyBits));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts the specified native UTF-8 string pointer of the specified
        /// length into a managed string.
        /// </summary>
        /// <param name="pValue">
        /// The native UTF-8 string pointer.
        /// </param>
        /// <param name="length">
        /// The length of the native string, in bytes.
        /// </param>
        /// <returns>
        /// The managed string or null upon failure.
        /// </returns>
        public static string StringFromUtf8IntPtr(
            IntPtr pValue,
            int length
            )
        {
            if (pValue == IntPtr.Zero)
                return null;

            if (length > 0)
            {
                byte[] bytes = new byte[length];

                Marshal.Copy(pValue, bytes, 0, length);

                return GetStringFromUtf8Bytes(bytes);
            }

            return String.Empty;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts the specified managed string into a native NUL-terminated
        /// UTF-8 string pointer using memory obtained from the SQLite core
        /// library.
        /// </summary>
        /// <param name="value">
        /// The managed string to convert.
        /// </param>
        /// <returns>
        /// The native NUL-terminated UTF-8 string pointer or
        /// <see cref="IntPtr.Zero" /> upon failure.
        /// </returns>
        public static IntPtr Utf8IntPtrFromString(
            string value
            )
        {
            if (value == null)
                return IntPtr.Zero;

            IntPtr result = IntPtr.Zero;
            byte[] bytes = GetUtf8BytesFromString(value);

            if (bytes == null)
                return IntPtr.Zero;

            int length = bytes.Length;

            result = SQLiteMemory.Allocate(length + 1);

            if (result == IntPtr.Zero)
                return IntPtr.Zero;

            Marshal.Copy(bytes, 0, result, length);
            Marshal.WriteByte(result, length, 0);

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region UTF-8 String Array Helper Methods
        /// <summary>
        /// Converts a logical array of native NUL-terminated UTF-8 string
        /// pointers into an array of managed strings.
        /// </summary>
        /// <param name="argc">
        /// The number of elements in the logical array of native
        /// NUL-terminated UTF-8 string pointers.
        /// </param>
        /// <param name="argv">
        /// The native pointer to the logical array of native NUL-terminated
        /// UTF-8 string pointers to convert.
        /// </param>
        /// <returns>
        /// The array of managed strings or null upon failure.
        /// </returns>
        public static string[] StringArrayFromUtf8SizeAndIntPtr(
            int argc,
            IntPtr argv
            )
        {
            if (argc < 0)
                return null;

            if (argv == IntPtr.Zero)
                return null;

            string[] result = new string[argc];

            for (int index = 0, offset = 0;
                    index < result.Length;
                    index++, offset += IntPtr.Size)
            {
                IntPtr pArg = SQLiteMarshal.ReadIntPtr(argv, offset);

                result[index] = (pArg != IntPtr.Zero) ?
                    StringFromUtf8IntPtr(pArg) : null;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts an array of managed strings into an array of native
        /// NUL-terminated UTF-8 string pointers.
        /// </summary>
        /// <param name="values">
        /// The array of managed strings to convert.
        /// </param>
        /// <returns>
        /// The array of native NUL-terminated UTF-8 string pointers or null
        /// upon failure.
        /// </returns>
        public static IntPtr[] Utf8IntPtrArrayFromStringArray(
            string[] values
            )
        {
            if (values == null)
                return null;

            IntPtr[] result = new IntPtr[values.Length];

            for (int index = 0; index < result.Length; index++)
                result[index] = Utf8IntPtrFromString(values[index]);

            return result;
        }
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteBytes Static Class
    /// <summary>
    /// This class contains static methods that are used to deal with native
    /// pointers to memory blocks that logically contain arrays of bytes to be
    /// used with the SQLite core library.
    /// </summary>
    internal static class SQLiteBytes
    {
        #region Byte Array Helper Methods
        /// <summary>
        /// Converts a native pointer to a logical array of bytes of the
        /// specified length into a managed byte array.
        /// </summary>
        /// <param name="pValue">
        /// The native pointer to the logical array of bytes to convert.
        /// </param>
        /// <param name="length">
        /// The length, in bytes, of the logical array of bytes to convert.
        /// </param>
        /// <returns>
        /// The managed byte array or null upon failure.
        /// </returns>
        public static byte[] FromIntPtr(
            IntPtr pValue,
            int length
            )
        {
            if (pValue == IntPtr.Zero)
                return null;

            if (length == 0)
                return new byte[0];

            byte[] result = new byte[length];

            Marshal.Copy(pValue, result, 0, length);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts a managed byte array into a native pointer to a logical
        /// array of bytes.
        /// </summary>
        /// <param name="value">
        /// The managed byte array to convert.
        /// </param>
        /// <returns>
        /// The native pointer to a logical byte array or null upon failure.
        /// </returns>
        public static IntPtr ToIntPtr(
            byte[] value
            )
        {
            if (value == null)
                return IntPtr.Zero;

            int length = value.Length;

            if (length == 0)
                return IntPtr.Zero;

            IntPtr result = SQLiteMemory.Allocate(length);

            if (result == IntPtr.Zero)
                return IntPtr.Zero;

            Marshal.Copy(value, 0, result, length);

            return result;
        }
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteMarshal Static Class
    /// <summary>
    /// This class contains static methods that are used to perform several
    /// low-level data marshalling tasks between native and managed code.
    /// </summary>
    internal static class SQLiteMarshal
    {
        #region IntPtr Helper Methods
        /// <summary>
        /// Returns a new <see cref="IntPtr" /> object instance based on the
        /// specified <see cref="IntPtr" /> object instance and an integer
        /// offset.
        /// </summary>
        /// <param name="pointer">
        /// The <see cref="IntPtr" /> object instance representing the base
        /// memory location.
        /// </param>
        /// <param name="offset">
        /// The integer offset from the base memory location that the new
        /// <see cref="IntPtr" /> object instance should point to.
        /// </param>
        /// <returns>
        /// The new <see cref="IntPtr" /> object instance.
        /// </returns>
        public static IntPtr IntPtrForOffset(
            IntPtr pointer,
            int offset
            )
        {
            return new IntPtr(pointer.ToInt64() + offset);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Rounds up an integer size to the next multiple of the alignment.
        /// </summary>
        /// <param name="size">
        /// The size, in bytes, to be rounded up.
        /// </param>
        /// <param name="alignment">
        /// The required alignment for the return value.
        /// </param>
        /// <returns>
        /// The size, in bytes, rounded up to the next multiple of the
        /// alignment.  This value may end up being the same as the original
        /// size.
        /// </returns>
        public static int RoundUp(
            int size,
            int alignment
            )
        {
            int alignmentMinusOne = alignment - 1;
            return ((size + alignmentMinusOne) & ~alignmentMinusOne);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines the offset, in bytes, of the next structure member.
        /// </summary>
        /// <param name="offset">
        /// The offset, in bytes, of the current structure member.
        /// </param>
        /// <param name="size">
        /// The size, in bytes, of the current structure member.
        /// </param>
        /// <param name="alignment">
        /// The alignment, in bytes, of the next structure member.
        /// </param>
        /// <returns>
        /// The offset, in bytes, of the next structure member.
        /// </returns>
        public static int NextOffsetOf(
            int offset,
            int size,
            int alignment
            )
        {
            return RoundUp(offset + size, alignment);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Marshal Read Helper Methods
        /// <summary>
        /// Reads a <see cref="Int32" /> value from the specified memory
        /// location.
        /// </summary>
        /// <param name="pointer">
        /// The <see cref="IntPtr" /> object instance representing the base
        /// memory location.
        /// </param>
        /// <param name="offset">
        /// The integer offset from the base memory location where the
        /// <see cref="Int32" /> value to be read is located.
        /// </param>
        /// <returns>
        /// The <see cref="Int32" /> value at the specified memory location.
        /// </returns>
        public static int ReadInt32(
            IntPtr pointer,
            int offset
            )
        {
#if !PLATFORM_COMPACTFRAMEWORK
            return Marshal.ReadInt32(pointer, offset);
#else
            return Marshal.ReadInt32(IntPtrForOffset(pointer, offset));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Reads a <see cref="Double" /> value from the specified memory
        /// location.
        /// </summary>
        /// <param name="pointer">
        /// The <see cref="IntPtr" /> object instance representing the base
        /// memory location.
        /// </param>
        /// <param name="offset">
        /// The integer offset from the base memory location where the
        /// <see cref="Double" /> to be read is located.
        /// </param>
        /// <returns>
        /// The <see cref="Double" /> value at the specified memory location.
        /// </returns>
        public static double ReadDouble(
            IntPtr pointer,
            int offset
            )
        {
#if !PLATFORM_COMPACTFRAMEWORK
            return BitConverter.Int64BitsToDouble(Marshal.ReadInt64(
                pointer, offset));
#else
            return BitConverter.ToDouble(BitConverter.GetBytes(
                Marshal.ReadInt64(IntPtrForOffset(pointer, offset))), 0);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Reads an <see cref="IntPtr" /> value from the specified memory
        /// location.
        /// </summary>
        /// <param name="pointer">
        /// The <see cref="IntPtr" /> object instance representing the base
        /// memory location.
        /// </param>
        /// <param name="offset">
        /// The integer offset from the base memory location where the
        /// <see cref="IntPtr" /> value to be read is located.
        /// </param>
        /// <returns>
        /// The <see cref="IntPtr" /> value at the specified memory location.
        /// </returns>
        public static IntPtr ReadIntPtr(
            IntPtr pointer,
            int offset
            )
        {
#if !PLATFORM_COMPACTFRAMEWORK
            return Marshal.ReadIntPtr(pointer, offset);
#else
            return Marshal.ReadIntPtr(IntPtrForOffset(pointer, offset));
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Marshal Write Helper Methods
        /// <summary>
        /// Writes an <see cref="Int32" /> value to the specified memory
        /// location.
        /// </summary>
        /// <param name="pointer">
        /// The <see cref="IntPtr" /> object instance representing the base
        /// memory location.
        /// </param>
        /// <param name="offset">
        /// The integer offset from the base memory location where the
        /// <see cref="Int32" /> value to be written is located.
        /// </param>
        /// <param name="value">
        /// The <see cref="Int32" /> value to write.
        /// </param>
        public static void WriteInt32(
            IntPtr pointer,
            int offset,
            int value
            )
        {
#if !PLATFORM_COMPACTFRAMEWORK
            Marshal.WriteInt32(pointer, offset, value);
#else
            Marshal.WriteInt32(IntPtrForOffset(pointer, offset), value);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Writes an <see cref="Int64" /> value to the specified memory
        /// location.
        /// </summary>
        /// <param name="pointer">
        /// The <see cref="IntPtr" /> object instance representing the base
        /// memory location.
        /// </param>
        /// <param name="offset">
        /// The integer offset from the base memory location where the
        /// <see cref="Int64" /> value to be written is located.
        /// </param>
        /// <param name="value">
        /// The <see cref="Int64" /> value to write.
        /// </param>
        public static void WriteInt64(
            IntPtr pointer,
            int offset,
            long value
            )
        {
#if !PLATFORM_COMPACTFRAMEWORK
            Marshal.WriteInt64(pointer, offset, value);
#else
            Marshal.WriteInt64(IntPtrForOffset(pointer, offset), value);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Writes a <see cref="Double" /> value to the specified memory
        /// location.
        /// </summary>
        /// <param name="pointer">
        /// The <see cref="IntPtr" /> object instance representing the base
        /// memory location.
        /// </param>
        /// <param name="offset">
        /// The integer offset from the base memory location where the
        /// <see cref="Double" /> value to be written is located.
        /// </param>
        /// <param name="value">
        /// The <see cref="Double" /> value to write.
        /// </param>
        public static void WriteDouble(
            IntPtr pointer,
            int offset,
            double value
            )
        {
#if !PLATFORM_COMPACTFRAMEWORK
            Marshal.WriteInt64(pointer, offset,
                BitConverter.DoubleToInt64Bits(value));
#else
            Marshal.WriteInt64(IntPtrForOffset(pointer, offset),
                BitConverter.ToInt64(BitConverter.GetBytes(value), 0));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Writes a <see cref="IntPtr" /> value to the specified memory
        /// location.
        /// </summary>
        /// <param name="pointer">
        /// The <see cref="IntPtr" /> object instance representing the base
        /// memory location.
        /// </param>
        /// <param name="offset">
        /// The integer offset from the base memory location where the
        /// <see cref="IntPtr" /> value to be written is located.
        /// </param>
        /// <param name="value">
        /// The <see cref="IntPtr" /> value to write.
        /// </param>
        public static void WriteIntPtr(
            IntPtr pointer,
            int offset,
            IntPtr value
            )
        {
#if !PLATFORM_COMPACTFRAMEWORK
            Marshal.WriteIntPtr(pointer, offset, value);
#else
            Marshal.WriteIntPtr(IntPtrForOffset(pointer, offset), value);
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Object Helper Methods
        /// <summary>
        /// Generates a hash code value for the object.
        /// </summary>
        /// <param name="value">
        /// The object instance used to calculate the hash code.
        /// </param>
        /// <param name="identity">
        /// Non-zero if different object instances with the same value should
        /// generate different hash codes, where applicable.  This parameter
        /// has no effect on the .NET Compact Framework.
        /// </param>
        /// <returns>
        /// The hash code value -OR- zero if the object is null.
        /// </returns>
        public static int GetHashCode(
            object value,
            bool identity
            )
        {
#if !PLATFORM_COMPACTFRAMEWORK
            if (identity)
                return RuntimeHelpers.GetHashCode(value);
#endif

            if (value == null) return 0;
            return value.GetHashCode();
        }
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteModule Base Class
    /// <summary>
    /// This class represents a managed virtual table module implementation.
    /// It is not sealed and must be used as the base class for any
    /// user-defined virtual table module classes implemented in managed code.
    /// </summary>
    public abstract class SQLiteModule :
            ISQLiteManagedModule, /*ISQLiteNativeModule,*/
            IDisposable /* NOT SEALED */
    {
        #region SQLiteNativeModule Private Class
        /// <summary>
        /// This class implements the <see cref="ISQLiteNativeModule" />
        /// interface by forwarding those method calls to the
        /// <see cref="SQLiteModule" /> object instance it contains.  If the
        /// contained <see cref="SQLiteModule" /> object instance is null, all
        /// the <see cref="ISQLiteNativeModule" /> methods simply generate an
        /// error.
        /// </summary>
        private sealed class SQLiteNativeModule :
                ISQLiteNativeModule, IDisposable
        {
            #region Private Constants
            /// <summary>
            /// This is the value that is always used for the "logErrors"
            /// parameter to the various static error handling methods provided
            /// by the <see cref="SQLiteModule" /> class.
            /// </summary>
            private const bool DefaultLogErrors = true;

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This is the value that is always used for the "logExceptions"
            /// parameter to the various static error handling methods provided
            /// by the <see cref="SQLiteModule" /> class.
            /// </summary>
            private const bool DefaultLogExceptions = true;

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This is the error message text used when the contained
            /// <see cref="SQLiteModule" /> object instance is not available
            /// for any reason.
            /// </summary>
            private const string ModuleNotAvailableErrorMessage =
                "native module implementation not available";
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Data
            /// <summary>
            /// The <see cref="SQLiteModule" /> object instance used to provide
            /// an implementation of the <see cref="ISQLiteNativeModule" />
            /// interface.
            /// </summary>
            private SQLiteModule module;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Constructors
            /// <summary>
            /// Constructs an instance of this class.
            /// </summary>
            /// <param name="module">
            /// The <see cref="SQLiteModule" /> object instance used to provide
            /// an implementation of the <see cref="ISQLiteNativeModule" />
            /// interface.
            /// </param>
            public SQLiteNativeModule(
                SQLiteModule module
                )
            {
                this.module = module;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Static Methods
            /// <summary>
            /// Sets the table error message to one that indicates the native
            /// module implementation is not available.
            /// </summary>
            /// <param name="pVtab">
            /// The native pointer to the sqlite3_vtab derived structure.
            /// </param>
            /// <returns>
            /// The value of <see cref="SQLiteErrorCode.Error" />.
            /// </returns>
            private static SQLiteErrorCode ModuleNotAvailableTableError(
                IntPtr pVtab
                )
            {
                SetTableError(null, pVtab, DefaultLogErrors,
                    DefaultLogExceptions, ModuleNotAvailableErrorMessage);

                return SQLiteErrorCode.Error;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Sets the table error message to one that indicates the native
            /// module implementation is not available.
            /// </summary>
            /// <param name="pCursor">
            /// The native pointer to the sqlite3_vtab_cursor derived
            /// structure.
            /// </param>
            /// <returns>
            /// The value of <see cref="SQLiteErrorCode.Error" />.
            /// </returns>
            private static SQLiteErrorCode ModuleNotAvailableCursorError(
                IntPtr pCursor
                )
            {
                SetCursorError(null, pCursor, DefaultLogErrors,
                    DefaultLogExceptions, ModuleNotAvailableErrorMessage);

                return SQLiteErrorCode.Error;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region ISQLiteNativeModule Members
            /// <summary>
            /// See the <see cref="ISQLiteNativeModule.xCreate" /> method.
            /// </summary>
            /// <param name="pDb">
            /// See the <see cref="ISQLiteNativeModule.xCreate" /> method.
            /// </param>
            /// <param name="pAux">
            /// See the <see cref="ISQLiteNativeModule.xCreate" /> method.
            /// </param>
            /// <param name="argc">
            /// See the <see cref="ISQLiteNativeModule.xCreate" /> method.
            /// </param>
            /// <param name="argv">
            /// See the <see cref="ISQLiteNativeModule.xCreate" /> method.
            /// </param>
            /// <param name="pVtab">
            /// See the <see cref="ISQLiteNativeModule.xCreate" /> method.
            /// </param>
            /// <param name="pError">
            /// See the <see cref="ISQLiteNativeModule.xCreate" /> method.
            /// </param>
            /// <returns>
            /// See the <see cref="ISQLiteNativeModule.xCreate" /> method.
            /// </returns>
            public SQLiteErrorCode xCreate(
                IntPtr pDb,
                IntPtr pAux,
                int argc,
                IntPtr argv,
                ref IntPtr pVtab,
                ref IntPtr pError
                )
            {
                //
                // NOTE: Called by native code.
                //
                // CheckDisposed(); /* EXEMPT */

                if (module == null)
                {
                    pError = SQLiteString.Utf8IntPtrFromString(
                        ModuleNotAvailableErrorMessage);

                    return SQLiteErrorCode.Error;
                }

                return module.xCreate(
                    pDb, pAux, argc, argv, ref pVtab, ref pError);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// See the <see cref="ISQLiteNativeModule.xConnect" /> method.
            /// </summary>
            /// <param name="pDb">
            /// See the <see cref="ISQLiteNativeModule.xConnect" /> method.
            /// </param>
            /// <param name="pAux">
            /// See the <see cref="ISQLiteNativeModule.xConnect" /> method.
            /// </param>
            /// <param name="argc">
            /// See the <see cref="ISQLiteNativeModule.xConnect" /> method.
            /// </param>
            /// <param name="argv">
            /// See the <see cref="ISQLiteNativeModule.xConnect" /> method.
            /// </param>
            /// <param name="pVtab">
            /// See the <see cref="ISQLiteNativeModule.xConnect" /> method.
            /// </param>
            /// <param name="pError">
            /// See the <see cref="ISQLiteNativeModule.xConnect" /> method.
            /// </param>
            /// <returns>
            /// See the <see cref="ISQLiteNativeModule.xConnect" /> method.
            /// </returns>
            public SQLiteErrorCode xConnect(
                IntPtr pDb,
                IntPtr pAux,
                int argc,
                IntPtr argv,
                ref IntPtr pVtab,
                ref IntPtr pError
                )
            {
                //
                // NOTE: Called by native code.
                //
                // CheckDisposed(); /* EXEMPT */

                if (module == null)
                {
                    pError = SQLiteString.Utf8IntPtrFromString(
                        ModuleNotAvailableErrorMessage);

                    return SQLiteErrorCode.Error;
                }

                return module.xConnect(
                    pDb, pAux, argc, argv, ref pVtab, ref pError);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// See the <see cref="ISQLiteNativeModule.xBestIndex" /> method.
            /// </summary>
            /// <param name="pVtab">
            /// See the <see cref="ISQLiteNativeModule.xBestIndex" /> method.
            /// </param>
            /// <param name="pIndex">
            /// See the <see cref="ISQLiteNativeModule.xBestIndex" /> method.
            /// </param>
            /// <returns>
            /// See the <see cref="ISQLiteNativeModule.xBestIndex" /> method.
            /// </returns>
            public SQLiteErrorCode xBestIndex(
                IntPtr pVtab,
                IntPtr pIndex
                )
            {
                //
                // NOTE: Called by native code.
                //
                // CheckDisposed(); /* EXEMPT */

                if (module == null)
                    return ModuleNotAvailableTableError(pVtab);

                return module.xBestIndex(pVtab, pIndex);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// See the <see cref="ISQLiteNativeModule.xDisconnect" /> method.
            /// </summary>
            /// <param name="pVtab">
            /// See the <see cref="ISQLiteNativeModule.xDisconnect" /> method.
            /// </param>
            /// <returns>
            /// See the <see cref="ISQLiteNativeModule.xDisconnect" /> method.
            /// </returns>
            public SQLiteErrorCode xDisconnect(
                IntPtr pVtab
                )
            {
                //
                // NOTE: Called by native code.
                //
                // CheckDisposed(); /* EXEMPT */

                if (module == null)
                    return ModuleNotAvailableTableError(pVtab);

                return module.xDisconnect(pVtab);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// See the <see cref="ISQLiteNativeModule.xDestroy" /> method.
            /// </summary>
            /// <param name="pVtab">
            /// See the <see cref="ISQLiteNativeModule.xDestroy" /> method.
            /// </param>
            /// <returns>
            /// See the <see cref="ISQLiteNativeModule.xDestroy" /> method.
            /// </returns>
            public SQLiteErrorCode xDestroy(
                IntPtr pVtab
                )
            {
                //
                // NOTE: Called by native code.
                //
                // CheckDisposed(); /* EXEMPT */

                if (module == null)
                    return ModuleNotAvailableTableError(pVtab);

                return module.xDestroy(pVtab);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// See the <see cref="ISQLiteNativeModule.xOpen" /> method.
            /// </summary>
            /// <param name="pVtab">
            /// See the <see cref="ISQLiteNativeModule.xOpen" /> method.
            /// </param>
            /// <param name="pCursor">
            /// See the <see cref="ISQLiteNativeModule.xOpen" /> method.
            /// </param>
            /// <returns>
            /// See the <see cref="ISQLiteNativeModule.xOpen" /> method.
            /// </returns>
            public SQLiteErrorCode xOpen(
                IntPtr pVtab,
                ref IntPtr pCursor
                )
            {
                //
                // NOTE: Called by native code.
                //
                // CheckDisposed(); /* EXEMPT */

                if (module == null)
                    return ModuleNotAvailableTableError(pVtab);

                return module.xOpen(pVtab, ref pCursor);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// See the <see cref="ISQLiteNativeModule.xClose" /> method.
            /// </summary>
            /// <param name="pCursor">
            /// See the <see cref="ISQLiteNativeModule.xClose" /> method.
            /// </param>
            /// <returns>
            /// See the <see cref="ISQLiteNativeModule.xClose" /> method.
            /// </returns>
            public SQLiteErrorCode xClose(
                IntPtr pCursor
                )
            {
                //
                // NOTE: Called by native code.
                //
                // CheckDisposed(); /* EXEMPT */

                if (module == null)
                    return ModuleNotAvailableCursorError(pCursor);

                return module.xClose(pCursor);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// See the <see cref="ISQLiteNativeModule.xFilter" /> method.
            /// </summary>
            /// <param name="pCursor">
            /// See the <see cref="ISQLiteNativeModule.xFilter" /> method.
            /// </param>
            /// <param name="idxNum">
            /// See the <see cref="ISQLiteNativeModule.xFilter" /> method.
            /// </param>
            /// <param name="idxStr">
            /// See the <see cref="ISQLiteNativeModule.xFilter" /> method.
            /// </param>
            /// <param name="argc">
            /// See the <see cref="ISQLiteNativeModule.xFilter" /> method.
            /// </param>
            /// <param name="argv">
            /// See the <see cref="ISQLiteNativeModule.xFilter" /> method.
            /// </param>
            /// <returns>
            /// See the <see cref="ISQLiteNativeModule.xFilter" /> method.
            /// </returns>
            public SQLiteErrorCode xFilter(
                IntPtr pCursor,
                int idxNum,
                IntPtr idxStr,
                int argc,
                IntPtr argv
                )
            {
                //
                // NOTE: Called by native code.
                //
                // CheckDisposed(); /* EXEMPT */

                if (module == null)
                    return ModuleNotAvailableCursorError(pCursor);

                return module.xFilter(pCursor, idxNum, idxStr, argc, argv);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// See the <see cref="ISQLiteNativeModule.xNext" /> method.
            /// </summary>
            /// <param name="pCursor">
            /// See the <see cref="ISQLiteNativeModule.xNext" /> method.
            /// </param>
            /// <returns>
            /// See the <see cref="ISQLiteNativeModule.xNext" /> method.
            /// </returns>
            public SQLiteErrorCode xNext(
                IntPtr pCursor
                )
            {
                //
                // NOTE: Called by native code.
                //
                // CheckDisposed(); /* EXEMPT */

                if (module == null)
                    return ModuleNotAvailableCursorError(pCursor);

                return module.xNext(pCursor);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// See the <see cref="ISQLiteNativeModule.xEof" /> method.
            /// </summary>
            /// <param name="pCursor">
            /// See the <see cref="ISQLiteNativeModule.xEof" /> method.
            /// </param>
            /// <returns>
            /// See the <see cref="ISQLiteNativeModule.xEof" /> method.
            /// </returns>
            public int xEof(
                IntPtr pCursor
                )
            {
                //
                // NOTE: Called by native code.
                //
                // CheckDisposed(); /* EXEMPT */

                if (module == null)
                {
                    ModuleNotAvailableCursorError(pCursor);
                    return 1;
                }

                return module.xEof(pCursor);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// See the <see cref="ISQLiteNativeModule.xColumn" /> method.
            /// </summary>
            /// <param name="pCursor">
            /// See the <see cref="ISQLiteNativeModule.xColumn" /> method.
            /// </param>
            /// <param name="pContext">
            /// See the <see cref="ISQLiteNativeModule.xColumn" /> method.
            /// </param>
            /// <param name="index">
            /// See the <see cref="ISQLiteNativeModule.xColumn" /> method.
            /// </param>
            /// <returns>
            /// See the <see cref="ISQLiteNativeModule.xColumn" /> method.
            /// </returns>
            public SQLiteErrorCode xColumn(
                IntPtr pCursor,
                IntPtr pContext,
                int index
                )
            {
                //
                // NOTE: Called by native code.
                //
                // CheckDisposed(); /* EXEMPT */

                if (module == null)
                    return ModuleNotAvailableCursorError(pCursor);

                return module.xColumn(pCursor, pContext, index);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// See the <see cref="ISQLiteNativeModule.xRowId" /> method.
            /// </summary>
            /// <param name="pCursor">
            /// See the <see cref="ISQLiteNativeModule.xRowId" /> method.
            /// </param>
            /// <param name="rowId">
            /// See the <see cref="ISQLiteNativeModule.xRowId" /> method.
            /// </param>
            /// <returns>
            /// See the <see cref="ISQLiteNativeModule.xRowId" /> method.
            /// </returns>
            public SQLiteErrorCode xRowId(
                IntPtr pCursor,
                ref long rowId
                )
            {
                //
                // NOTE: Called by native code.
                //
                // CheckDisposed(); /* EXEMPT */

                if (module == null)
                    return ModuleNotAvailableCursorError(pCursor);

                return module.xRowId(pCursor, ref rowId);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// See the <see cref="ISQLiteNativeModule.xUpdate" /> method.
            /// </summary>
            /// <param name="pVtab">
            /// See the <see cref="ISQLiteNativeModule.xUpdate" /> method.
            /// </param>
            /// <param name="argc">
            /// See the <see cref="ISQLiteNativeModule.xUpdate" /> method.
            /// </param>
            /// <param name="argv">
            /// See the <see cref="ISQLiteNativeModule.xUpdate" /> method.
            /// </param>
            /// <param name="rowId">
            /// See the <see cref="ISQLiteNativeModule.xUpdate" /> method.
            /// </param>
            /// <returns>
            /// See the <see cref="ISQLiteNativeModule.xUpdate" /> method.
            /// </returns>
            public SQLiteErrorCode xUpdate(
                IntPtr pVtab,
                int argc,
                IntPtr argv,
                ref long rowId
                )
            {
                //
                // NOTE: Called by native code.
                //
                // CheckDisposed(); /* EXEMPT */

                if (module == null)
                    return ModuleNotAvailableTableError(pVtab);

                return module.xUpdate(pVtab, argc, argv, ref rowId);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// See the <see cref="ISQLiteNativeModule.xBegin" /> method.
            /// </summary>
            /// <param name="pVtab">
            /// See the <see cref="ISQLiteNativeModule.xBegin" /> method.
            /// </param>
            /// <returns>
            /// See the <see cref="ISQLiteNativeModule.xBegin" /> method.
            /// </returns>
            public SQLiteErrorCode xBegin(
                IntPtr pVtab
                )
            {
                //
                // NOTE: Called by native code.
                //
                // CheckDisposed(); /* EXEMPT */

                if (module == null)
                    return ModuleNotAvailableTableError(pVtab);

                return module.xBegin(pVtab);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// See the <see cref="ISQLiteNativeModule.xSync" /> method.
            /// </summary>
            /// <param name="pVtab">
            /// See the <see cref="ISQLiteNativeModule.xSync" /> method.
            /// </param>
            /// <returns>
            /// See the <see cref="ISQLiteNativeModule.xSync" /> method.
            /// </returns>
            public SQLiteErrorCode xSync(
                IntPtr pVtab
                )
            {
                //
                // NOTE: Called by native code.
                //
                // CheckDisposed(); /* EXEMPT */

                if (module == null)
                    return ModuleNotAvailableTableError(pVtab);

                return module.xSync(pVtab);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// See the <see cref="ISQLiteNativeModule.xCommit" /> method.
            /// </summary>
            /// <param name="pVtab">
            /// See the <see cref="ISQLiteNativeModule.xCommit" /> method.
            /// </param>
            /// <returns>
            /// See the <see cref="ISQLiteNativeModule.xCommit" /> method.
            /// </returns>
            public SQLiteErrorCode xCommit(
                IntPtr pVtab
                )
            {
                //
                // NOTE: Called by native code.
                //
                // CheckDisposed(); /* EXEMPT */

                if (module == null)
                    return ModuleNotAvailableTableError(pVtab);

                return module.xCommit(pVtab);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// See the <see cref="ISQLiteNativeModule.xRollback" /> method.
            /// </summary>
            /// <param name="pVtab">
            /// See the <see cref="ISQLiteNativeModule.xRollback" /> method.
            /// </param>
            /// <returns>
            /// See the <see cref="ISQLiteNativeModule.xRollback" /> method.
            /// </returns>
            public SQLiteErrorCode xRollback(
                IntPtr pVtab
                )
            {
                //
                // NOTE: Called by native code.
                //
                // CheckDisposed(); /* EXEMPT */

                if (module == null)
                    return ModuleNotAvailableTableError(pVtab);

                return module.xRollback(pVtab);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// See the <see cref="ISQLiteNativeModule.xFindFunction" /> method.
            /// </summary>
            /// <param name="pVtab">
            /// See the <see cref="ISQLiteNativeModule.xFindFunction" /> method.
            /// </param>
            /// <param name="nArg">
            /// See the <see cref="ISQLiteNativeModule.xFindFunction" /> method.
            /// </param>
            /// <param name="zName">
            /// See the <see cref="ISQLiteNativeModule.xFindFunction" /> method.
            /// </param>
            /// <param name="callback">
            /// See the <see cref="ISQLiteNativeModule.xFindFunction" /> method.
            /// </param>
            /// <param name="pClientData">
            /// See the <see cref="ISQLiteNativeModule.xFindFunction" /> method.
            /// </param>
            /// <returns>
            /// See the <see cref="ISQLiteNativeModule.xFindFunction" /> method.
            /// </returns>
            public int xFindFunction(
                IntPtr pVtab,
                int nArg,
                IntPtr zName,
                ref SQLiteCallback callback,
                ref IntPtr pClientData
                )
            {
                //
                // NOTE: Called by native code.
                //
                // CheckDisposed(); /* EXEMPT */

                if (module == null)
                {
                    ModuleNotAvailableTableError(pVtab);
                    return 0;
                }

                return module.xFindFunction(
                    pVtab, nArg, zName, ref callback, ref pClientData);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// See the <see cref="ISQLiteNativeModule.xRename" /> method.
            /// </summary>
            /// <param name="pVtab">
            /// See the <see cref="ISQLiteNativeModule.xRename" /> method.
            /// </param>
            /// <param name="zNew">
            /// See the <see cref="ISQLiteNativeModule.xRename" /> method.
            /// </param>
            /// <returns>
            /// See the <see cref="ISQLiteNativeModule.xRename" /> method.
            /// </returns>
            public SQLiteErrorCode xRename(
                IntPtr pVtab,
                IntPtr zNew
                )
            {
                //
                // NOTE: Called by native code.
                //
                // CheckDisposed(); /* EXEMPT */

                if (module == null)
                    return ModuleNotAvailableTableError(pVtab);

                return module.xRename(pVtab, zNew);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// See the <see cref="ISQLiteNativeModule.xSavepoint" /> method.
            /// </summary>
            /// <param name="pVtab">
            /// See the <see cref="ISQLiteNativeModule.xSavepoint" /> method.
            /// </param>
            /// <param name="iSavepoint">
            /// See the <see cref="ISQLiteNativeModule.xSavepoint" /> method.
            /// </param>
            /// <returns>
            /// See the <see cref="ISQLiteNativeModule.xSavepoint" /> method.
            /// </returns>
            public SQLiteErrorCode xSavepoint(
                IntPtr pVtab,
                int iSavepoint
                )
            {
                //
                // NOTE: Called by native code.
                //
                // CheckDisposed(); /* EXEMPT */

                if (module == null)
                    return ModuleNotAvailableTableError(pVtab);

                return module.xSavepoint(pVtab, iSavepoint);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// See the <see cref="ISQLiteNativeModule.xRelease" /> method.
            /// </summary>
            /// <param name="pVtab">
            /// See the <see cref="ISQLiteNativeModule.xRelease" /> method.
            /// </param>
            /// <param name="iSavepoint">
            /// See the <see cref="ISQLiteNativeModule.xRelease" /> method.
            /// </param>
            /// <returns>
            /// See the <see cref="ISQLiteNativeModule.xRelease" /> method.
            /// </returns>
            public SQLiteErrorCode xRelease(
                IntPtr pVtab,
                int iSavepoint
                )
            {
                //
                // NOTE: Called by native code.
                //
                // CheckDisposed(); /* EXEMPT */

                if (module == null)
                    return ModuleNotAvailableTableError(pVtab);

                return module.xRelease(pVtab, iSavepoint);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// See the <see cref="ISQLiteNativeModule.xRollbackTo" /> method.
            /// </summary>
            /// <param name="pVtab">
            /// See the <see cref="ISQLiteNativeModule.xRollbackTo" /> method.
            /// </param>
            /// <param name="iSavepoint">
            /// See the <see cref="ISQLiteNativeModule.xRollbackTo" /> method.
            /// </param>
            /// <returns>
            /// See the <see cref="ISQLiteNativeModule.xRollbackTo" /> method.
            /// </returns>
            public SQLiteErrorCode xRollbackTo(
                IntPtr pVtab,
                int iSavepoint
                )
            {
                //
                // NOTE: Called by native code.
                //
                // CheckDisposed(); /* EXEMPT */

                if (module == null)
                    return ModuleNotAvailableTableError(pVtab);

                return module.xRollbackTo(pVtab, iSavepoint);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region IDisposable Members
            /// <summary>
            /// Disposes of this object instance.
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            /// <summary>
            /// Throws an <see cref="ObjectDisposedException" /> if this object
            /// instance has been disposed.
            /// </summary>
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed)
                {
                    throw new ObjectDisposedException(
                        typeof(SQLiteNativeModule).Name);
                }
#endif
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Disposes of this object instance.
            /// </summary>
            /// <param name="disposing">
            /// Non-zero if this method is being called from the
            /// <see cref="Dispose()" /> method.  Zero if this method is being
            /// called from the finalizer.
            /// </param>
            private /* protected virtual */ void Dispose(bool disposing)
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

                    //
                    // NOTE: The module is not owned by us; therefore, do not
                    //       dispose it.
                    //
                    if (module != null)
                        module = null;

                    disposed = true;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Destructor
            /// <summary>
            /// Finalizes this object instance.
            /// </summary>
            ~SQLiteNativeModule()
            {
                Dispose(false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        /// <summary>
        /// The default version of the native sqlite3_module structure in use.
        /// </summary>
        private static readonly int DefaultModuleVersion = 2;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// This field is used to store the native sqlite3_module structure
        /// associated with this object instance.
        /// </summary>
        private UnsafeNativeMethods.sqlite3_module nativeModule;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This field is used to store the destructor delegate to be passed to
        /// the SQLite core library via the sqlite3_create_disposable_module()
        /// function.
        /// </summary>
        private UnsafeNativeMethods.xDestroyModule destroyModule;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This field is used to store a pointer to the native sqlite3_module
        /// structure returned by the sqlite3_create_disposable_module
        /// function.
        /// </summary>
        private IntPtr disposableModule;

        ///////////////////////////////////////////////////////////////////////

#if PLATFORM_COMPACTFRAMEWORK
        /// <summary>
        /// This field is used to hold the block of native memory that contains
        /// the native sqlite3_module structure associated with this object
        /// instance when running on the .NET Compact Framework.
        /// </summary>
        private IntPtr pNativeModule;
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This field is used to store the virtual table instances associated
        /// with this module.  The native pointer to the sqlite3_vtab derived
        /// structure is used to key into this collection.
        /// </summary>
        private Dictionary<IntPtr, SQLiteVirtualTable> tables;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This field is used to store the virtual table cursor instances
        /// associated with this module.  The native pointer to the
        /// sqlite3_vtab_cursor derived structure is used to key into this
        /// collection.
        /// </summary>
        private Dictionary<IntPtr, SQLiteVirtualTableCursor> cursors;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This field is used to store the virtual table function instances
        /// associated with this module.  The case-insensitive function name
        /// and the number of arguments (with -1 meaning "any") are used to
        /// construct the string that is used to key into this collection.
        /// </summary>
        private Dictionary<string, SQLiteFunction> functions;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class.
        /// </summary>
        /// <param name="name">
        /// The name of the module.  This parameter cannot be null.
        /// </param>
        public SQLiteModule(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            this.name = name;
            this.tables = new Dictionary<IntPtr, SQLiteVirtualTable>();
            this.cursors = new Dictionary<IntPtr, SQLiteVirtualTableCursor>();
            this.functions = new Dictionary<string, SQLiteFunction>();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Methods
        /// <summary>
        /// Calls the native SQLite core library in order to create a new
        /// disposable module containing the implementation of a virtual table.
        /// </summary>
        /// <param name="pDb">
        /// The native database connection pointer to use.
        /// </param>
        /// <returns>
        /// Non-zero upon success.
        /// </returns>
        internal bool CreateDisposableModule(
            IntPtr pDb
            )
        {
            if (disposableModule != IntPtr.Zero)
                return true;

            IntPtr pName = IntPtr.Zero;

            try
            {
                pName = SQLiteString.Utf8IntPtrFromString(name);

                UnsafeNativeMethods.sqlite3_module nativeModule =
                    AllocateNativeModule();

                destroyModule = new UnsafeNativeMethods.xDestroyModule(
                    xDestroyModule);

#if !PLATFORM_COMPACTFRAMEWORK
                disposableModule =
                    UnsafeNativeMethods.sqlite3_create_disposable_module(
                        pDb, pName, ref nativeModule, IntPtr.Zero, destroyModule);

                return (disposableModule != IntPtr.Zero);
#elif !SQLITE_STANDARD
                disposableModule =
                    UnsafeNativeMethods.sqlite3_create_disposable_module_interop(
                       pDb, pName, AllocateNativeModuleInterop(),
                       nativeModule.iVersion, nativeModule.xCreate,
                       nativeModule.xConnect, nativeModule.xBestIndex,
                       nativeModule.xDisconnect, nativeModule.xDestroy,
                       nativeModule.xOpen, nativeModule.xClose,
                       nativeModule.xFilter, nativeModule.xNext,
                       nativeModule.xEof, nativeModule.xColumn,
                       nativeModule.xRowId, nativeModule.xUpdate,
                       nativeModule.xBegin, nativeModule.xSync,
                       nativeModule.xCommit, nativeModule.xRollback,
                       nativeModule.xFindFunction, nativeModule.xRename,
                       nativeModule.xSavepoint, nativeModule.xRelease,
                       nativeModule.xRollbackTo, IntPtr.Zero, destroyModule);

                return (disposableModule != IntPtr.Zero);
#else
                throw new NotImplementedException();
#endif
            }
            finally
            {
                if (pName != IntPtr.Zero)
                {
                    SQLiteMemory.Free(pName);
                    pName = IntPtr.Zero;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method is called by the SQLite core library when the native
        /// module associated with this object instance is being destroyed due
        /// to its parent connection being closed.  It may also be called by
        /// the "vtshim" module if/when the sqlite3_dispose_module() function
        /// is called.
        /// </summary>
        /// <param name="pClientData">
        /// The native user-data pointer associated with this module, as it was
        /// provided to the SQLite core library when the native module instance
        /// was created.
        /// </param>
        private void xDestroyModule(
            IntPtr pClientData /* NOT USED */
            )
        {
            //
            // NOTE: At this point, just make sure that this native module
            //       handle is not reused, nor passed into the native
            //       sqlite3_dispose_module() function later (i.e. if/when
            //       the Dispose() method of this object instance is called).
            //
            disposableModule = IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates and returns the native sqlite_module structure using the
        /// configured (or default) <see cref="ISQLiteNativeModule" />
        /// interface implementation.
        /// </summary>
        /// <returns>
        /// The native sqlite_module structure using the configured (or
        /// default) <see cref="ISQLiteNativeModule" /> interface
        /// implementation.
        /// </returns>
        private UnsafeNativeMethods.sqlite3_module AllocateNativeModule()
        {
            return AllocateNativeModule(GetNativeModuleImpl());
        }

        ///////////////////////////////////////////////////////////////////////

#if PLATFORM_COMPACTFRAMEWORK
        /// <summary>
        /// Creates and returns a memory block obtained from the SQLite core
        /// library used to store the native sqlite3_module structure for this
        /// object instance when running on the .NET Compact Framework.
        /// </summary>
        /// <returns>
        /// The native pointer to the native sqlite3_module structure.
        /// </returns>
        private IntPtr AllocateNativeModuleInterop()
        {
            if (pNativeModule == IntPtr.Zero)
            {
                //
                // HACK: No easy way to determine the size of the native
                //       sqlite_module structure when running on the .NET
                //       Compact Framework; therefore, just base the size
                //       on what we know:
                //
                //       There is one integer member.
                //       There are 22 function pointer members.
                //
                pNativeModule = SQLiteMemory.Allocate(23 * IntPtr.Size);

                if (pNativeModule == IntPtr.Zero)
                    throw new OutOfMemoryException("sqlite3_module");
            }

            return pNativeModule;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates and returns the native sqlite_module structure using the
        /// specified <see cref="ISQLiteNativeModule" /> interface
        /// implementation.
        /// </summary>
        /// <param name="module">
        /// The <see cref="ISQLiteNativeModule" /> interface implementation to
        /// use.
        /// </param>
        /// <returns>
        /// The native sqlite_module structure using the specified
        /// <see cref="ISQLiteNativeModule" /> interface implementation.
        /// </returns>
        private UnsafeNativeMethods.sqlite3_module AllocateNativeModule(
            ISQLiteNativeModule module
            )
        {
            nativeModule = new UnsafeNativeMethods.sqlite3_module();
            nativeModule.iVersion = DefaultModuleVersion;

            if (module != null)
            {
                nativeModule.xCreate = new UnsafeNativeMethods.xCreate(
                   module.xCreate);

                nativeModule.xConnect = new UnsafeNativeMethods.xConnect(
                    module.xConnect);

                nativeModule.xBestIndex = new UnsafeNativeMethods.xBestIndex(
                    module.xBestIndex);

                nativeModule.xDisconnect = new UnsafeNativeMethods.xDisconnect(
                    module.xDisconnect);

                nativeModule.xDestroy = new UnsafeNativeMethods.xDestroy(
                    module.xDestroy);

                nativeModule.xOpen = new UnsafeNativeMethods.xOpen(
                    module.xOpen);

                nativeModule.xClose = new UnsafeNativeMethods.xClose(
                    module.xClose);

                nativeModule.xFilter = new UnsafeNativeMethods.xFilter(
                    module.xFilter);

                nativeModule.xNext = new UnsafeNativeMethods.xNext(
                    module.xNext);

                nativeModule.xEof = new UnsafeNativeMethods.xEof(module.xEof);

                nativeModule.xColumn = new UnsafeNativeMethods.xColumn(
                    module.xColumn);

                nativeModule.xRowId = new UnsafeNativeMethods.xRowId(
                    module.xRowId);

                nativeModule.xUpdate = new UnsafeNativeMethods.xUpdate(
                    module.xUpdate);

                nativeModule.xBegin = new UnsafeNativeMethods.xBegin(
                    module.xBegin);

                nativeModule.xSync = new UnsafeNativeMethods.xSync(
                    module.xSync);

                nativeModule.xCommit = new UnsafeNativeMethods.xCommit(
                    module.xCommit);

                nativeModule.xRollback = new UnsafeNativeMethods.xRollback(
                    module.xRollback);

                nativeModule.xFindFunction = new UnsafeNativeMethods.xFindFunction(
                    module.xFindFunction);

                nativeModule.xRename = new UnsafeNativeMethods.xRename(
                    module.xRename);

                nativeModule.xSavepoint = new UnsafeNativeMethods.xSavepoint(
                    module.xSavepoint);

                nativeModule.xRelease = new UnsafeNativeMethods.xRelease(
                    module.xRelease);

                nativeModule.xRollbackTo = new UnsafeNativeMethods.xRollbackTo(
                    module.xRollbackTo);
            }
            else
            {
                nativeModule.xCreate = new UnsafeNativeMethods.xCreate(
                    xCreate);

                nativeModule.xConnect = new UnsafeNativeMethods.xConnect(
                    xConnect);

                nativeModule.xBestIndex = new UnsafeNativeMethods.xBestIndex(
                    xBestIndex);

                nativeModule.xDisconnect = new UnsafeNativeMethods.xDisconnect(
                    xDisconnect);

                nativeModule.xDestroy = new UnsafeNativeMethods.xDestroy(
                    xDestroy);

                nativeModule.xOpen = new UnsafeNativeMethods.xOpen(xOpen);
                nativeModule.xClose = new UnsafeNativeMethods.xClose(xClose);

                nativeModule.xFilter = new UnsafeNativeMethods.xFilter(
                    xFilter);

                nativeModule.xNext = new UnsafeNativeMethods.xNext(xNext);
                nativeModule.xEof = new UnsafeNativeMethods.xEof(xEof);

                nativeModule.xColumn = new UnsafeNativeMethods.xColumn(
                    xColumn);

                nativeModule.xRowId = new UnsafeNativeMethods.xRowId(xRowId);

                nativeModule.xUpdate = new UnsafeNativeMethods.xUpdate(
                    xUpdate);

                nativeModule.xBegin = new UnsafeNativeMethods.xBegin(xBegin);
                nativeModule.xSync = new UnsafeNativeMethods.xSync(xSync);

                nativeModule.xCommit = new UnsafeNativeMethods.xCommit(
                    xCommit);

                nativeModule.xRollback = new UnsafeNativeMethods.xRollback(
                    xRollback);

                nativeModule.xFindFunction = new UnsafeNativeMethods.xFindFunction(
                    xFindFunction);

                nativeModule.xRename = new UnsafeNativeMethods.xRename(
                    xRename);

                nativeModule.xSavepoint = new UnsafeNativeMethods.xSavepoint(
                    xSavepoint);

                nativeModule.xRelease = new UnsafeNativeMethods.xRelease(
                    xRelease);

                nativeModule.xRollbackTo = new UnsafeNativeMethods.xRollbackTo(
                    xRollbackTo);
            }

            return nativeModule;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a copy of the specified
        /// <see cref="UnsafeNativeMethods.sqlite3_module" /> object instance,
        /// using default implementations for the contained delegates when
        /// necessary.
        /// </summary>
        /// <param name="module">
        /// The <see cref="UnsafeNativeMethods.sqlite3_module" /> object
        /// instance to copy.
        /// </param>
        /// <returns>
        /// The new <see cref="UnsafeNativeMethods.sqlite3_module" /> object
        /// instance.
        /// </returns>
        private UnsafeNativeMethods.sqlite3_module CopyNativeModule(
            UnsafeNativeMethods.sqlite3_module module
            )
        {
            UnsafeNativeMethods.sqlite3_module newModule =
                new UnsafeNativeMethods.sqlite3_module();

            newModule.iVersion = module.iVersion;

            newModule.xCreate = new UnsafeNativeMethods.xCreate(
                (module.xCreate != null) ? module.xCreate : xCreate);

            newModule.xConnect = new UnsafeNativeMethods.xConnect(
                (module.xConnect != null) ? module.xConnect : xConnect);

            newModule.xBestIndex = new UnsafeNativeMethods.xBestIndex(
                (module.xBestIndex != null) ? module.xBestIndex : xBestIndex);

            newModule.xDisconnect = new UnsafeNativeMethods.xDisconnect(
                (module.xDisconnect != null) ? module.xDisconnect :
                xDisconnect);

            newModule.xDestroy = new UnsafeNativeMethods.xDestroy(
                (module.xDestroy != null) ? module.xDestroy : xDestroy);

            newModule.xOpen = new UnsafeNativeMethods.xOpen(
                (module.xOpen != null) ? module.xOpen : xOpen);

            newModule.xClose = new UnsafeNativeMethods.xClose(
                (module.xClose != null) ? module.xClose : xClose);

            newModule.xFilter = new UnsafeNativeMethods.xFilter(
                (module.xFilter != null) ? module.xFilter : xFilter);

            newModule.xNext = new UnsafeNativeMethods.xNext(
                (module.xNext != null) ? module.xNext : xNext);

            newModule.xEof = new UnsafeNativeMethods.xEof(
                (module.xEof != null) ? module.xEof : xEof);

            newModule.xColumn = new UnsafeNativeMethods.xColumn(
                (module.xColumn != null) ? module.xColumn : xColumn);

            newModule.xRowId = new UnsafeNativeMethods.xRowId(
                (module.xRowId != null) ? module.xRowId : xRowId);

            newModule.xUpdate = new UnsafeNativeMethods.xUpdate(
                (module.xUpdate != null) ? module.xUpdate : xUpdate);

            newModule.xBegin = new UnsafeNativeMethods.xBegin(
                (module.xBegin != null) ? module.xBegin : xBegin);

            newModule.xSync = new UnsafeNativeMethods.xSync(
                (module.xSync != null) ? module.xSync : xSync);

            newModule.xCommit = new UnsafeNativeMethods.xCommit(
                (module.xCommit != null) ? module.xCommit : xCommit);

            newModule.xRollback = new UnsafeNativeMethods.xRollback(
                (module.xRollback != null) ? module.xRollback : xRollback);

            newModule.xFindFunction = new UnsafeNativeMethods.xFindFunction(
                (module.xFindFunction != null) ? module.xFindFunction :
                xFindFunction);

            newModule.xRename = new UnsafeNativeMethods.xRename(
                (module.xRename != null) ? module.xRename : xRename);

            newModule.xSavepoint = new UnsafeNativeMethods.xSavepoint(
                (module.xSavepoint != null) ? module.xSavepoint : xSavepoint);

            newModule.xRelease = new UnsafeNativeMethods.xRelease(
                (module.xRelease != null) ? module.xRelease : xRelease);

            newModule.xRollbackTo = new UnsafeNativeMethods.xRollbackTo(
                (module.xRollbackTo != null) ? module.xRollbackTo :
                xRollbackTo);

            return newModule;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Calls one of the virtual table initialization methods.
        /// </summary>
        /// <param name="create">
        /// Non-zero to call the <see cref="ISQLiteManagedModule.Create" />
        /// method; otherwise, the <see cref="ISQLiteManagedModule.Connect" />
        /// method will be called.
        /// </param>
        /// <param name="pDb">
        /// The native database connection handle.
        /// </param>
        /// <param name="pAux">
        /// The original native pointer value that was provided to the
        /// sqlite3_create_module(), sqlite3_create_module_v2() or
        /// sqlite3_create_disposable_module() functions.
        /// </param>
        /// <param name="argc">
        /// The number of arguments from the CREATE VIRTUAL TABLE statement.
        /// </param>
        /// <param name="argv">
        /// The array of string arguments from the CREATE VIRTUAL TABLE
        /// statement.
        /// </param>
        /// <param name="pVtab">
        /// Upon success, this parameter must be modified to point to the newly
        /// created native sqlite3_vtab derived structure.
        /// </param>
        /// <param name="pError">
        /// Upon failure, this parameter must be modified to point to the error
        /// message, with the underlying memory having been obtained from the
        /// sqlite3_malloc() function.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        private SQLiteErrorCode CreateOrConnect(
            bool create,
            IntPtr pDb,
            IntPtr pAux,
            int argc,
            IntPtr argv,
            ref IntPtr pVtab,
            ref IntPtr pError
            )
        {
            try
            {
                string fileName = SQLiteString.StringFromUtf8IntPtr(
                    UnsafeNativeMethods.sqlite3_db_filename(pDb, IntPtr.Zero));

                using (SQLiteConnection connection = new SQLiteConnection(
                        pDb, fileName, false))
                {
                    SQLiteVirtualTable table = null;
                    string error = null;

                    if ((create && Create(connection, pAux,
                            SQLiteString.StringArrayFromUtf8SizeAndIntPtr(argc,
                            argv), ref table, ref error) == SQLiteErrorCode.Ok) ||
                        (!create && Connect(connection, pAux,
                            SQLiteString.StringArrayFromUtf8SizeAndIntPtr(argc,
                            argv), ref table, ref error) == SQLiteErrorCode.Ok))
                    {
                        if (table != null)
                        {
                            pVtab = TableToIntPtr(table);
                            return SQLiteErrorCode.Ok;
                        }
                        else
                        {
                            pError = SQLiteString.Utf8IntPtrFromString(
                                "no table was created");
                        }
                    }
                    else
                    {
                        pError = SQLiteString.Utf8IntPtrFromString(error);
                    }
                }
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                pError = SQLiteString.Utf8IntPtrFromString(e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Calls one of the virtual table finalization methods.
        /// </summary>
        /// <param name="destroy">
        /// Non-zero to call the <see cref="ISQLiteManagedModule.Destroy" />
        /// method; otherwise, the
        /// <see cref="ISQLiteManagedModule.Disconnect" /> method will be
        /// called.
        /// </param>
        /// <param name="pVtab">
        /// The native pointer to the sqlite3_vtab derived structure.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        private SQLiteErrorCode DestroyOrDisconnect(
            bool destroy,
            IntPtr pVtab
            )
        {
            try
            {
                SQLiteVirtualTable table = TableFromIntPtr(pVtab);

                if (table != null)
                {
                    if ((destroy && (Destroy(table) == SQLiteErrorCode.Ok)) ||
                        (!destroy && (Disconnect(table) == SQLiteErrorCode.Ok)))
                    {
                        if (tables != null)
                            tables.Remove(pVtab);

                        return SQLiteErrorCode.Ok;
                    }
                }
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                //
                // NOTE: At this point, there is no way to report the error
                //       condition back to the caller; therefore, use the
                //       logging facility instead.
                //
                try
                {
                    if (LogExceptionsNoThrow)
                    {
                        /* throw */
                        SQLiteLog.LogMessage(SQLiteBase.COR_E_EXCEPTION,
                            String.Format(CultureInfo.CurrentCulture,
                            "Caught exception in \"{0}\" method: {1}",
                            destroy ? "xDestroy" : "xDisconnect", e));
                    }
                }
                catch
                {
                    // do nothing.
                }
            }
            finally
            {
                FreeTable(pVtab);
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Static Error Handling Helper Methods
        /// <summary>
        /// Arranges for the specified error message to be placed into the
        /// zErrMsg field of a sqlite3_vtab derived structure, freeing the
        /// existing error message, if any.
        /// </summary>
        /// <param name="module">
        /// The <see cref="SQLiteModule" /> object instance to be used.
        /// </param>
        /// <param name="pVtab">
        /// The native pointer to the sqlite3_vtab derived structure.
        /// </param>
        /// <param name="logErrors">
        /// Non-zero if this error message should also be logged using the
        /// <see cref="SQLiteLog" /> class.
        /// </param>
        /// <param name="logExceptions">
        /// Non-zero if caught exceptions should be logged using the
        /// <see cref="SQLiteLog" /> class.
        /// </param>
        /// <param name="error">
        /// The error message.
        /// </param>
        /// <returns>
        /// Non-zero upon success.
        /// </returns>
        private static bool SetTableError(
            SQLiteModule module,
            IntPtr pVtab,
            bool logErrors,
            bool logExceptions,
            string error
            )
        {
            try
            {
                if (logErrors)
                {
                    SQLiteLog.LogMessage(SQLiteErrorCode.Error,
                        String.Format(CultureInfo.CurrentCulture,
                        "Virtual table error: {0}", error)); /* throw */
                }
            }
            catch
            {
                // do nothing.
            }

            bool success = false;
            IntPtr pNewError = IntPtr.Zero;

            try
            {
                if (pVtab == IntPtr.Zero)
                    return false;

                int offset = 0;

                offset = SQLiteMarshal.NextOffsetOf(offset, IntPtr.Size,
                    sizeof(int));

                offset = SQLiteMarshal.NextOffsetOf(offset, sizeof(int),
                    IntPtr.Size);

                IntPtr pOldError = SQLiteMarshal.ReadIntPtr(pVtab, offset);

                if (pOldError != IntPtr.Zero)
                {
                    SQLiteMemory.Free(pOldError); pOldError = IntPtr.Zero;
                    SQLiteMarshal.WriteIntPtr(pVtab, offset, pOldError);
                }

                if (error == null)
                    return true;

                pNewError = SQLiteString.Utf8IntPtrFromString(error);
                SQLiteMarshal.WriteIntPtr(pVtab, offset, pNewError);
                success = true;
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                try
                {
                    if (logExceptions)
                    {
                        SQLiteLog.LogMessage(SQLiteBase.COR_E_EXCEPTION,
                            String.Format(CultureInfo.CurrentCulture,
                            "Caught exception in \"SetTableError\" method: {0}",
                            e)); /* throw */
                    }
                }
                catch
                {
                    // do nothing.
                }
            }
            finally
            {
                if (!success && (pNewError != IntPtr.Zero))
                {
                    SQLiteMemory.Free(pNewError);
                    pNewError = IntPtr.Zero;
                }
            }

            return success;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Arranges for the specified error message to be placed into the
        /// zErrMsg field of a sqlite3_vtab derived structure, freeing the
        /// existing error message, if any.
        /// </summary>
        /// <param name="module">
        /// The <see cref="SQLiteModule" /> object instance to be used.
        /// </param>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance used to
        /// lookup the native pointer to the sqlite3_vtab derived structure.
        /// </param>
        /// <param name="logErrors">
        /// Non-zero if this error message should also be logged using the
        /// <see cref="SQLiteLog" /> class.
        /// </param>
        /// <param name="logExceptions">
        /// Non-zero if caught exceptions should be logged using the
        /// <see cref="SQLiteLog" /> class.
        /// </param>
        /// <param name="error">
        /// The error message.
        /// </param>
        /// <returns>
        /// Non-zero upon success.
        /// </returns>
        private static bool SetTableError(
            SQLiteModule module,
            SQLiteVirtualTable table,
            bool logErrors,
            bool logExceptions,
            string error
            )
        {
            if (table == null)
                return false;

            IntPtr pVtab = table.NativeHandle;

            if (pVtab == IntPtr.Zero)
                return false;

            return SetTableError(
                module, pVtab, logErrors, logExceptions, error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Arranges for the specified error message to be placed into the
        /// zErrMsg field of a sqlite3_vtab derived structure, freeing the
        /// existing error message, if any.
        /// </summary>
        /// <param name="module">
        /// The <see cref="SQLiteModule" /> object instance to be used.
        /// </param>
        /// <param name="pCursor">
        /// The native pointer to the sqlite3_vtab_cursor derived structure
        /// used to get the native pointer to the sqlite3_vtab derived
        /// structure.
        /// </param>
        /// <param name="logErrors">
        /// Non-zero if this error message should also be logged using the
        /// <see cref="SQLiteLog" /> class.
        /// </param>
        /// <param name="logExceptions">
        /// Non-zero if caught exceptions should be logged using the
        /// <see cref="SQLiteLog" /> class.
        /// </param>
        /// <param name="error">
        /// The error message.
        /// </param>
        /// <returns>
        /// Non-zero upon success.
        /// </returns>
        private static bool SetCursorError(
            SQLiteModule module,
            IntPtr pCursor,
            bool logErrors,
            bool logExceptions,
            string error
            )
        {
            if (pCursor == IntPtr.Zero)
                return false;

            IntPtr pVtab = TableFromCursor(module, pCursor);

            if (pVtab == IntPtr.Zero)
                return false;

            return SetTableError(
                module, pVtab, logErrors, logExceptions, error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Arranges for the specified error message to be placed into the
        /// zErrMsg field of a sqlite3_vtab derived structure, freeing the
        /// existing error message, if any.
        /// </summary>
        /// <param name="module">
        /// The <see cref="SQLiteModule" /> object instance to be used.
        /// </param>
        /// <param name="cursor">
        /// The <see cref="SQLiteVirtualTableCursor" /> object instance used to
        /// lookup the native pointer to the sqlite3_vtab derived structure.
        /// </param>
        /// <param name="logErrors">
        /// Non-zero if this error message should also be logged using the
        /// <see cref="SQLiteLog" /> class.
        /// </param>
        /// <param name="logExceptions">
        /// Non-zero if caught exceptions should be logged using the
        /// <see cref="SQLiteLog" /> class.
        /// </param>
        /// <param name="error">
        /// The error message.
        /// </param>
        /// <returns>
        /// Non-zero upon success.
        /// </returns>
        private static bool SetCursorError(
            SQLiteModule module,
            SQLiteVirtualTableCursor cursor,
            bool logErrors,
            bool logExceptions,
            string error
            )
        {
            if (cursor == null)
                return false;

            IntPtr pCursor = cursor.NativeHandle;

            if (pCursor == IntPtr.Zero)
                return false;

            return SetCursorError(
                module, pCursor, logErrors, logExceptions, error);
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Members
        #region Module Helper Methods
        /// <summary>
        /// Gets and returns the <see cref="ISQLiteNativeModule" /> interface
        /// implementation to be used when creating the native sqlite3_module
        /// structure.  Derived classes may override this method to supply an
        /// alternate implementation for the <see cref="ISQLiteNativeModule" />
        /// interface.
        /// </summary>
        /// <returns>
        /// The <see cref="ISQLiteNativeModule" /> interface implementation to
        /// be used when populating the native sqlite3_module structure.  If
        /// the returned value is null, the private methods provided by the
        /// <see cref="SQLiteModule" /> class and relating to the
        /// <see cref="ISQLiteNativeModule" /> interface  will be used to
        /// create the necessary delegates.
        /// </returns>
        protected virtual ISQLiteNativeModule GetNativeModuleImpl()
        {
            return null; /* NOTE: Use the built-in default delegates. */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates and returns the <see cref="ISQLiteNativeModule" />
        /// interface implementation corresponding to the current
        /// <see cref="SQLiteModule" /> object instance.
        /// </summary>
        /// <returns>
        /// The <see cref="ISQLiteNativeModule" /> interface implementation
        /// corresponding to the current <see cref="SQLiteModule" /> object
        /// instance.
        /// </returns>
        protected virtual ISQLiteNativeModule CreateNativeModuleImpl()
        {
            return new SQLiteNativeModule(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Native Table Helper Methods
        /// <summary>
        /// Allocates a native sqlite3_vtab derived structure and returns a
        /// native pointer to it.
        /// </summary>
        /// <returns>
        /// A native pointer to a native sqlite3_vtab derived structure.
        /// </returns>
        protected virtual IntPtr AllocateTable()
        {
            int size = Marshal.SizeOf(typeof(
                UnsafeNativeMethods.sqlite3_vtab));

            return SQLiteMemory.Allocate(size);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Zeros out the fields of a native sqlite3_vtab derived structure.
        /// </summary>
        /// <param name="pVtab">
        /// The native pointer to the native sqlite3_vtab derived structure to
        /// zero.
        /// </param>
        protected virtual void ZeroTable(
            IntPtr pVtab
            )
        {
            if (pVtab == IntPtr.Zero)
                return;

            int offset = 0;

            SQLiteMarshal.WriteIntPtr(pVtab, offset, IntPtr.Zero);

            offset = SQLiteMarshal.NextOffsetOf(offset, IntPtr.Size,
                sizeof(int));

            SQLiteMarshal.WriteInt32(pVtab, offset, 0);

            offset = SQLiteMarshal.NextOffsetOf(offset, sizeof(int),
                IntPtr.Size);

            SQLiteMarshal.WriteIntPtr(pVtab, offset, IntPtr.Zero);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Frees a native sqlite3_vtab structure using the provided native
        /// pointer to it.
        /// </summary>
        /// <param name="pVtab">
        /// A native pointer to a native sqlite3_vtab derived structure.
        /// </param>
        protected virtual void FreeTable(
            IntPtr pVtab
            )
        {
            SetTableError(pVtab, null);
            SQLiteMemory.Free(pVtab);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Native Cursor Helper Methods
        /// <summary>
        /// Allocates a native sqlite3_vtab_cursor derived structure and
        /// returns a native pointer to it.
        /// </summary>
        /// <returns>
        /// A native pointer to a native sqlite3_vtab_cursor derived structure.
        /// </returns>
        protected virtual IntPtr AllocateCursor()
        {
            int size = Marshal.SizeOf(typeof(
                UnsafeNativeMethods.sqlite3_vtab_cursor));

            return SQLiteMemory.Allocate(size);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Frees a native sqlite3_vtab_cursor structure using the provided
        /// native pointer to it.
        /// </summary>
        /// <param name="pCursor">
        /// A native pointer to a native sqlite3_vtab_cursor derived structure.
        /// </param>
        protected virtual void FreeCursor(
            IntPtr pCursor
            )
        {
            SQLiteMemory.Free(pCursor);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static Table Lookup Methods
        /// <summary>
        /// Reads and returns the native pointer to the sqlite3_vtab derived
        /// structure based on the native pointer to the sqlite3_vtab_cursor
        /// derived structure.
        /// </summary>
        /// <param name="module">
        /// The <see cref="SQLiteModule" /> object instance to be used.
        /// </param>
        /// <param name="pCursor">
        /// The native pointer to the sqlite3_vtab_cursor derived structure
        /// from which to read the native pointer to the sqlite3_vtab derived
        /// structure.
        /// </param>
        /// <returns>
        /// The native pointer to the sqlite3_vtab derived structure -OR-
        /// <see cref="IntPtr.Zero" /> if it cannot be determined.
        /// </returns>
        private static IntPtr TableFromCursor(
            SQLiteModule module,
            IntPtr pCursor
            )
        {
            if (pCursor == IntPtr.Zero)
                return IntPtr.Zero;

            return Marshal.ReadIntPtr(pCursor);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Table Lookup Methods
        /// <summary>
        /// Reads and returns the native pointer to the sqlite3_vtab derived
        /// structure based on the native pointer to the sqlite3_vtab_cursor
        /// derived structure.
        /// </summary>
        /// <param name="pCursor">
        /// The native pointer to the sqlite3_vtab_cursor derived structure
        /// from which to read the native pointer to the sqlite3_vtab derived
        /// structure.
        /// </param>
        /// <returns>
        /// The native pointer to the sqlite3_vtab derived structure -OR-
        /// <see cref="IntPtr.Zero" /> if it cannot be determined.
        /// </returns>
        protected virtual IntPtr TableFromCursor(
            IntPtr pCursor
            )
        {
            return TableFromCursor(this, pCursor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Looks up and returns the <see cref="SQLiteVirtualTable" /> object
        /// instance based on the native pointer to the sqlite3_vtab derived
        /// structure.
        /// </summary>
        /// <param name="pVtab">
        /// The native pointer to the sqlite3_vtab derived structure.
        /// </param>
        /// <returns>
        /// The <see cref="SQLiteVirtualTable" /> object instance or null if
        /// the corresponding one cannot be found.
        /// </returns>
        protected virtual SQLiteVirtualTable TableFromIntPtr(
            IntPtr pVtab
            )
        {
            if (pVtab == IntPtr.Zero)
            {
                SetTableError(pVtab, "invalid native table");
                return null;
            }

            SQLiteVirtualTable table;

            if ((tables != null) &&
                tables.TryGetValue(pVtab, out table))
            {
                return table;
            }

            SetTableError(pVtab, String.Format(
                CultureInfo.CurrentCulture,
                "managed table for {0} not found", pVtab));

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Allocates and returns a native pointer to a sqlite3_vtab derived
        /// structure and creates an association between it and the specified
        /// <see cref="SQLiteVirtualTable" /> object instance.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance to be used
        /// when creating the association.
        /// </param>
        /// <returns>
        /// The native pointer to a sqlite3_vtab derived structure or
        /// <see cref="IntPtr.Zero" /> if the method fails for any reason.
        /// </returns>
        protected virtual IntPtr TableToIntPtr(
            SQLiteVirtualTable table
            )
        {
            if ((table == null) || (tables == null))
                return IntPtr.Zero;

            IntPtr pVtab = IntPtr.Zero;
            bool success = false;

            try
            {
                pVtab = AllocateTable();

                if (pVtab != IntPtr.Zero)
                {
                    ZeroTable(pVtab);
                    table.NativeHandle = pVtab;
                    tables.Add(pVtab, table);
                    success = true;
                }
            }
            finally
            {
                if (!success && (pVtab != IntPtr.Zero))
                {
                    FreeTable(pVtab);
                    pVtab = IntPtr.Zero;
                }
            }

            return pVtab;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Cursor Lookup Methods
        /// <summary>
        /// Looks up and returns the <see cref="SQLiteVirtualTableCursor" />
        /// object instance based on the native pointer to the
        /// sqlite3_vtab_cursor derived structure.
        /// </summary>
        /// <param name="pVtab">
        /// The native pointer to the sqlite3_vtab derived structure.
        /// </param>
        /// <param name="pCursor">
        /// The native pointer to the sqlite3_vtab_cursor derived structure.
        /// </param>
        /// <returns>
        /// The <see cref="SQLiteVirtualTableCursor" /> object instance or null
        /// if the corresponding one cannot be found.
        /// </returns>
        protected virtual SQLiteVirtualTableCursor CursorFromIntPtr(
            IntPtr pVtab,
            IntPtr pCursor
            )
        {
            if (pCursor == IntPtr.Zero)
            {
                SetTableError(pVtab, "invalid native cursor");
                return null;
            }

            SQLiteVirtualTableCursor cursor;

            if ((cursors != null) &&
                cursors.TryGetValue(pCursor, out cursor))
            {
                return cursor;
            }

            SetTableError(pVtab, String.Format(
                CultureInfo.CurrentCulture,
                "managed cursor for {0} not found", pCursor));

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Allocates and returns a native pointer to a sqlite3_vtab_cursor
        /// derived structure and creates an association between it and the
        /// specified <see cref="SQLiteVirtualTableCursor" /> object instance.
        /// </summary>
        /// <param name="cursor">
        /// The <see cref="SQLiteVirtualTableCursor" /> object instance to be
        /// used when creating the association.
        /// </param>
        /// <returns>
        /// The native pointer to a sqlite3_vtab_cursor derived structure or
        /// <see cref="IntPtr.Zero" /> if the method fails for any reason.
        /// </returns>
        protected virtual IntPtr CursorToIntPtr(
            SQLiteVirtualTableCursor cursor
            )
        {
            if ((cursor == null) || (cursors == null))
                return IntPtr.Zero;

            IntPtr pCursor = IntPtr.Zero;
            bool success = false;

            try
            {
                pCursor = AllocateCursor();

                if (pCursor != IntPtr.Zero)
                {
                    cursor.NativeHandle = pCursor;
                    cursors.Add(pCursor, cursor);
                    success = true;
                }
            }
            finally
            {
                if (!success && (pCursor != IntPtr.Zero))
                {
                    FreeCursor(pCursor);
                    pCursor = IntPtr.Zero;
                }
            }

            return pCursor;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Function Lookup Methods
        /// <summary>
        /// Deterimines the key that should be used to identify and store the
        /// <see cref="SQLiteFunction" /> object instance for the virtual table
        /// (i.e. to be returned via the
        /// <see cref="ISQLiteNativeModule.xFindFunction" /> method).
        /// </summary>
        /// <param name="argumentCount">
        /// The number of arguments to the virtual table function.
        /// </param>
        /// <param name="name">
        /// The name of the virtual table function.
        /// </param>
        /// <param name="function">
        /// The <see cref="SQLiteFunction" /> object instance associated with
        /// this virtual table function.
        /// </param>
        /// <returns>
        /// The string that should be used to identify and store the virtual
        /// table function instance.  This method cannot return null.  If null
        /// is returned from this method, the behavior is undefined.
        /// </returns>
        protected virtual string GetFunctionKey(
            int argumentCount,
            string name,
            SQLiteFunction function
            )
        {
            return String.Format("{0}:{1}", argumentCount, name);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Table Declaration Helper Methods
        /// <summary>
        /// Attempts to declare the schema for the virtual table using the
        /// specified database connection.
        /// </summary>
        /// <param name="connection">
        /// The <see cref="SQLiteConnection" /> object instance to use when
        /// declaring the schema of the virtual table.  This parameter may not
        /// be null.
        /// </param>
        /// <param name="sql">
        /// The string containing the CREATE TABLE statement that completely
        /// describes the schema for the virtual table.  This parameter may not
        /// be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter must be modified to contain an error
        /// message.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        protected virtual SQLiteErrorCode DeclareTable(
            SQLiteConnection connection,
            string sql,
            ref string error
            )
        {
            if (connection == null)
            {
                error = "invalid connection";
                return SQLiteErrorCode.Error;
            }

            SQLiteBase sqliteBase = connection._sql;

            if (sqliteBase == null)
            {
                error = "connection has invalid handle";
                return SQLiteErrorCode.Error;
            }

            if (sql == null)
            {
                error = "invalid SQL statement";
                return SQLiteErrorCode.Error;
            }

            return sqliteBase.DeclareVirtualTable(this, sql, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Function Declaration Helper Methods
        /// <summary>
        /// Calls the native SQLite core library in order to declare a virtual
        /// table function in response to a call into the
        /// <see cref="ISQLiteNativeModule.xCreate" />
        /// or <see cref="ISQLiteNativeModule.xConnect" /> virtual table
        /// methods.
        /// </summary>
        /// <param name="connection">
        /// The <see cref="SQLiteConnection" /> object instance to use when
        /// declaring the schema of the virtual table.
        /// </param>
        /// <param name="argumentCount">
        /// The number of arguments to the function being declared.
        /// </param>
        /// <param name="name">
        /// The name of the function being declared.
        /// </param>
        /// <param name="error">
        /// Upon success, the contents of this parameter are undefined.  Upon
        /// failure, it should contain an appropriate error message.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        protected virtual SQLiteErrorCode DeclareFunction(
            SQLiteConnection connection,
            int argumentCount,
            string name,
            ref string error
            )
        {
            if (connection == null)
            {
                error = "invalid connection";
                return SQLiteErrorCode.Error;
            }

            SQLiteBase sqliteBase = connection._sql;

            if (sqliteBase == null)
            {
                error = "connection has invalid handle";
                return SQLiteErrorCode.Error;
            }

            return sqliteBase.DeclareVirtualFunction(
                this, argumentCount, name, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Error Handling Properties
        private bool logErrors;
        /// <summary>
        /// Returns or sets a boolean value indicating whether virtual table
        /// errors should be logged using the <see cref="SQLiteLog" /> class.
        /// </summary>
        protected virtual bool LogErrorsNoThrow
        {
            get { return logErrors; }
            set { logErrors = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool logExceptions;
        /// <summary>
        /// Returns or sets a boolean value indicating whether exceptions
        /// caught in the
        /// <see cref="ISQLiteNativeModule.xDisconnect" /> method,
        /// the <see cref="ISQLiteNativeModule.xDestroy" /> method,
        /// the <see cref="SetTableError(IntPtr,string)" /> method,
        /// the <see cref="SetTableError(SQLiteVirtualTable,string)" /> method,
        /// and the <see cref="Dispose()" /> method should be logged using the
        /// <see cref="SQLiteLog" /> class.
        /// </summary>
        protected virtual bool LogExceptionsNoThrow
        {
            get { return logExceptions; }
            set { logExceptions = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Error Handling Helper Methods
        /// <summary>
        /// Arranges for the specified error message to be placed into the
        /// zErrMsg field of a sqlite3_vtab derived structure, freeing the
        /// existing error message, if any.
        /// </summary>
        /// <param name="pVtab">
        /// The native pointer to the sqlite3_vtab derived structure.
        /// </param>
        /// <param name="error">
        /// The error message.
        /// </param>
        /// <returns>
        /// Non-zero upon success.
        /// </returns>
        protected virtual bool SetTableError(
            IntPtr pVtab,
            string error
            )
        {
            return SetTableError(
                this, pVtab, LogErrorsNoThrow, LogExceptionsNoThrow, error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Arranges for the specified error message to be placed into the
        /// zErrMsg field of a sqlite3_vtab derived structure, freeing the
        /// existing error message, if any.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance used to
        /// lookup the native pointer to the sqlite3_vtab derived structure.
        /// </param>
        /// <param name="error">
        /// The error message.
        /// </param>
        /// <returns>
        /// Non-zero upon success.
        /// </returns>
        protected virtual bool SetTableError(
            SQLiteVirtualTable table,
            string error
            )
        {
            return SetTableError(
                this, table, LogErrorsNoThrow, LogExceptionsNoThrow, error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Arranges for the specified error message to be placed into the
        /// zErrMsg field of a sqlite3_vtab derived structure, freeing the
        /// existing error message, if any.
        /// </summary>
        /// <param name="cursor">
        /// The <see cref="SQLiteVirtualTableCursor" /> object instance used to
        /// lookup the native pointer to the sqlite3_vtab derived structure.
        /// </param>
        /// <param name="error">
        /// The error message.
        /// </param>
        /// <returns>
        /// Non-zero upon success.
        /// </returns>
        protected virtual bool SetCursorError(
            SQLiteVirtualTableCursor cursor,
            string error
            )
        {
            return SetCursorError(
                this, cursor, LogErrorsNoThrow, LogExceptionsNoThrow, error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Index Handling Helper Methods
        /// <summary>
        /// Modifies the specified <see cref="SQLiteIndex" /> object instance
        /// to contain the specified estimated cost.
        /// </summary>
        /// <param name="index">
        /// The <see cref="SQLiteIndex" /> object instance to modify.
        /// </param>
        /// <param name="estimatedCost">
        /// The estimated cost value to use.  Using a null value means that the
        /// default value provided by the SQLite core library should be used.
        /// </param>
        /// <returns>
        /// Non-zero upon success.
        /// </returns>
        protected virtual bool SetEstimatedCost(
            SQLiteIndex index,
            double? estimatedCost
            )
        {
            if ((index == null) || (index.Outputs == null))
                return false;

            index.Outputs.EstimatedCost = estimatedCost;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Modifies the specified <see cref="SQLiteIndex" /> object instance
        /// to contain the default estimated cost.
        /// </summary>
        /// <param name="index">
        /// The <see cref="SQLiteIndex" /> object instance to modify.
        /// </param>
        /// <returns>
        /// Non-zero upon success.
        /// </returns>
        protected virtual bool SetEstimatedCost(
            SQLiteIndex index
            )
        {
            return SetEstimatedCost(index, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Modifies the specified <see cref="SQLiteIndex" /> object instance
        /// to contain the specified estimated rows.
        /// </summary>
        /// <param name="index">
        /// The <see cref="SQLiteIndex" /> object instance to modify.
        /// </param>
        /// <param name="estimatedRows">
        /// The estimated rows value to use.  Using a null value means that the
        /// default value provided by the SQLite core library should be used.
        /// </param>
        /// <returns>
        /// Non-zero upon success.
        /// </returns>
        protected virtual bool SetEstimatedRows(
            SQLiteIndex index,
            long? estimatedRows
            )
        {
            if ((index == null) || (index.Outputs == null))
                return false;

            index.Outputs.EstimatedRows = estimatedRows;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Modifies the specified <see cref="SQLiteIndex" /> object instance
        /// to contain the default estimated rows.
        /// </summary>
        /// <param name="index">
        /// The <see cref="SQLiteIndex" /> object instance to modify.
        /// </param>
        /// <returns>
        /// Non-zero upon success.
        /// </returns>
        protected virtual bool SetEstimatedRows(
            SQLiteIndex index
            )
        {
            return SetEstimatedRows(index, null);
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        /// <summary>
        /// Returns or sets a boolean value indicating whether virtual table
        /// errors should be logged using the <see cref="SQLiteLog" /> class.
        /// </summary>
        public virtual bool LogErrors
        {
            get { CheckDisposed(); return LogErrorsNoThrow; }
            set { CheckDisposed(); LogErrorsNoThrow = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns or sets a boolean value indicating whether exceptions
        /// caught in the
        /// <see cref="ISQLiteNativeModule.xDisconnect" /> method,
        /// <see cref="ISQLiteNativeModule.xDestroy" /> method, and the
        /// <see cref="Dispose()" /> method should be logged using the
        /// <see cref="SQLiteLog" /> class.
        /// </summary>
        public virtual bool LogExceptions
        {
            get { CheckDisposed(); return LogExceptionsNoThrow; }
            set { CheckDisposed(); LogExceptionsNoThrow = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISQLiteNativeModule Members
        /// <summary>
        /// See the <see cref="ISQLiteNativeModule.xCreate" /> method.
        /// </summary>
        /// <param name="pDb">
        /// See the <see cref="ISQLiteNativeModule.xCreate" /> method.
        /// </param>
        /// <param name="pAux">
        /// See the <see cref="ISQLiteNativeModule.xCreate" /> method.
        /// </param>
        /// <param name="argc">
        /// See the <see cref="ISQLiteNativeModule.xCreate" /> method.
        /// </param>
        /// <param name="argv">
        /// See the <see cref="ISQLiteNativeModule.xCreate" /> method.
        /// </param>
        /// <param name="pVtab">
        /// See the <see cref="ISQLiteNativeModule.xCreate" /> method.
        /// </param>
        /// <param name="pError">
        /// See the <see cref="ISQLiteNativeModule.xCreate" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteNativeModule.xCreate" /> method.
        /// </returns>
        private SQLiteErrorCode xCreate(
            IntPtr pDb,
            IntPtr pAux,
            int argc,
            IntPtr argv,
            ref IntPtr pVtab,
            ref IntPtr pError
            )
        {
            return CreateOrConnect(
                true, pDb, pAux, argc, argv, ref pVtab, ref pError);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteNativeModule.xConnect" /> method.
        /// </summary>
        /// <param name="pDb">
        /// See the <see cref="ISQLiteNativeModule.xConnect" /> method.
        /// </param>
        /// <param name="pAux">
        /// See the <see cref="ISQLiteNativeModule.xConnect" /> method.
        /// </param>
        /// <param name="argc">
        /// See the <see cref="ISQLiteNativeModule.xConnect" /> method.
        /// </param>
        /// <param name="argv">
        /// See the <see cref="ISQLiteNativeModule.xConnect" /> method.
        /// </param>
        /// <param name="pVtab">
        /// See the <see cref="ISQLiteNativeModule.xConnect" /> method.
        /// </param>
        /// <param name="pError">
        /// See the <see cref="ISQLiteNativeModule.xConnect" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteNativeModule.xConnect" /> method.
        /// </returns>
        private SQLiteErrorCode xConnect(
            IntPtr pDb,
            IntPtr pAux,
            int argc,
            IntPtr argv,
            ref IntPtr pVtab,
            ref IntPtr pError
            )
        {
            return CreateOrConnect(
                false, pDb, pAux, argc, argv, ref pVtab, ref pError);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteNativeModule.xBestIndex" /> method.
        /// </summary>
        /// <param name="pVtab">
        /// See the <see cref="ISQLiteNativeModule.xBestIndex" /> method.
        /// </param>
        /// <param name="pIndex">
        /// See the <see cref="ISQLiteNativeModule.xBestIndex" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteNativeModule.xBestIndex" /> method.
        /// </returns>
        private SQLiteErrorCode xBestIndex(
            IntPtr pVtab,
            IntPtr pIndex
            )
        {
            try
            {
                SQLiteVirtualTable table = TableFromIntPtr(pVtab);

                if (table != null)
                {
                    SQLiteIndex index = null;

                    SQLiteIndex.FromIntPtr(pIndex, ref index);

                    if (BestIndex(table, index) == SQLiteErrorCode.Ok)
                    {
                        SQLiteIndex.ToIntPtr(index, pIndex);
                        return SQLiteErrorCode.Ok;
                    }
                }
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteNativeModule.xDisconnect" /> method.
        /// </summary>
        /// <param name="pVtab">
        /// See the <see cref="ISQLiteNativeModule.xDisconnect" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteNativeModule.xDisconnect" /> method.
        /// </returns>
        private SQLiteErrorCode xDisconnect(
            IntPtr pVtab
            )
        {
            return DestroyOrDisconnect(false, pVtab);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteNativeModule.xDestroy" /> method.
        /// </summary>
        /// <param name="pVtab">
        /// See the <see cref="ISQLiteNativeModule.xDestroy" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteNativeModule.xDestroy" /> method.
        /// </returns>
        private SQLiteErrorCode xDestroy(
            IntPtr pVtab
            )
        {
            return DestroyOrDisconnect(true, pVtab);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteNativeModule.xOpen" /> method.
        /// </summary>
        /// <param name="pVtab">
        /// See the <see cref="ISQLiteNativeModule.xOpen" /> method.
        /// </param>
        /// <param name="pCursor">
        /// See the <see cref="ISQLiteNativeModule.xOpen" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteNativeModule.xOpen" /> method.
        /// </returns>
        private SQLiteErrorCode xOpen(
            IntPtr pVtab,
            ref IntPtr pCursor
            )
        {
            try
            {
                SQLiteVirtualTable table = TableFromIntPtr(pVtab);

                if (table != null)
                {
                    SQLiteVirtualTableCursor cursor = null;

                    if (Open(table, ref cursor) == SQLiteErrorCode.Ok)
                    {
                        if (cursor != null)
                        {
                            pCursor = CursorToIntPtr(cursor);

                            if (pCursor != IntPtr.Zero)
                            {
                                return SQLiteErrorCode.Ok;
                            }
                            else
                            {
                                SetTableError(pVtab,
                                    "no native cursor was created");
                            }
                        }
                        else
                        {
                            SetTableError(pVtab,
                                "no managed cursor was created");
                        }
                    }
                }
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteNativeModule.xClose" /> method.
        /// </summary>
        /// <param name="pCursor">
        /// See the <see cref="ISQLiteNativeModule.xClose" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteNativeModule.xClose" /> method.
        /// </returns>
        private SQLiteErrorCode xClose(
            IntPtr pCursor
            )
        {
            IntPtr pVtab = IntPtr.Zero;

            try
            {
                pVtab = TableFromCursor(pCursor);

                SQLiteVirtualTableCursor cursor = CursorFromIntPtr(
                    pVtab, pCursor);

                if (cursor != null)
                {
                    if (Close(cursor) == SQLiteErrorCode.Ok)
                    {
                        if (cursors != null)
                            cursors.Remove(pCursor);

                        return SQLiteErrorCode.Ok;
                    }
                }
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }
            finally
            {
                FreeCursor(pCursor);
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteNativeModule.xFilter" /> method.
        /// </summary>
        /// <param name="pCursor">
        /// See the <see cref="ISQLiteNativeModule.xFilter" /> method.
        /// </param>
        /// <param name="idxNum">
        /// See the <see cref="ISQLiteNativeModule.xFilter" /> method.
        /// </param>
        /// <param name="idxStr">
        /// See the <see cref="ISQLiteNativeModule.xFilter" /> method.
        /// </param>
        /// <param name="argc">
        /// See the <see cref="ISQLiteNativeModule.xFilter" /> method.
        /// </param>
        /// <param name="argv">
        /// See the <see cref="ISQLiteNativeModule.xFilter" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteNativeModule.xFilter" /> method.
        /// </returns>
        private SQLiteErrorCode xFilter(
            IntPtr pCursor,
            int idxNum,
            IntPtr idxStr,
            int argc,
            IntPtr argv
            )
        {
            IntPtr pVtab = IntPtr.Zero;

            try
            {
                pVtab = TableFromCursor(pCursor);

                SQLiteVirtualTableCursor cursor = CursorFromIntPtr(
                    pVtab, pCursor);

                if (cursor != null)
                {
                    if (Filter(cursor, idxNum,
                            SQLiteString.StringFromUtf8IntPtr(idxStr),
                            SQLiteValue.ArrayFromSizeAndIntPtr(argc,
                                argv)) == SQLiteErrorCode.Ok)
                    {
                        return SQLiteErrorCode.Ok;
                    }
                }
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteNativeModule.xNext" /> method.
        /// </summary>
        /// <param name="pCursor">
        /// See the <see cref="ISQLiteNativeModule.xNext" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteNativeModule.xNext" /> method.
        /// </returns>
        private SQLiteErrorCode xNext(
            IntPtr pCursor
            )
        {
            IntPtr pVtab = IntPtr.Zero;

            try
            {
                pVtab = TableFromCursor(pCursor);

                SQLiteVirtualTableCursor cursor = CursorFromIntPtr(
                    pVtab, pCursor);

                if (cursor != null)
                {
                    if (Next(cursor) == SQLiteErrorCode.Ok)
                        return SQLiteErrorCode.Ok;
                }
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteNativeModule.xEof" /> method.
        /// </summary>
        /// <param name="pCursor">
        /// See the <see cref="ISQLiteNativeModule.xEof" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteNativeModule.xEof" /> method.
        /// </returns>
        private int xEof(
            IntPtr pCursor
            )
        {
            IntPtr pVtab = IntPtr.Zero;

            try
            {
                pVtab = TableFromCursor(pCursor);

                SQLiteVirtualTableCursor cursor = CursorFromIntPtr(
                    pVtab, pCursor);

                if (cursor != null)
                    return Eof(cursor) ? 1 : 0;
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return 1; /* NOTE: On any error, return "no more rows". */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteNativeModule.xColumn" /> method.
        /// </summary>
        /// <param name="pCursor">
        /// See the <see cref="ISQLiteNativeModule.xColumn" /> method.
        /// </param>
        /// <param name="pContext">
        /// See the <see cref="ISQLiteNativeModule.xColumn" /> method.
        /// </param>
        /// <param name="index">
        /// See the <see cref="ISQLiteNativeModule.xColumn" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteNativeModule.xColumn" /> method.
        /// </returns>
        private SQLiteErrorCode xColumn(
            IntPtr pCursor,
            IntPtr pContext,
            int index
            )
        {
            IntPtr pVtab = IntPtr.Zero;

            try
            {
                pVtab = TableFromCursor(pCursor);

                SQLiteVirtualTableCursor cursor = CursorFromIntPtr(
                    pVtab, pCursor);

                if (cursor != null)
                {
                    SQLiteContext context = new SQLiteContext(pContext);

                    return Column(cursor, context, index);
                }
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteNativeModule.xRowId" /> method.
        /// </summary>
        /// <param name="pCursor">
        /// See the <see cref="ISQLiteNativeModule.xRowId" /> method.
        /// </param>
        /// <param name="rowId">
        /// See the <see cref="ISQLiteNativeModule.xRowId" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteNativeModule.xRowId" /> method.
        /// </returns>
        private SQLiteErrorCode xRowId(
            IntPtr pCursor,
            ref long rowId
            )
        {
            IntPtr pVtab = IntPtr.Zero;

            try
            {
                pVtab = TableFromCursor(pCursor);

                SQLiteVirtualTableCursor cursor = CursorFromIntPtr(
                    pVtab, pCursor);

                if (cursor != null)
                    return RowId(cursor, ref rowId);
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteNativeModule.xUpdate" /> method.
        /// </summary>
        /// <param name="pVtab">
        /// See the <see cref="ISQLiteNativeModule.xUpdate" /> method.
        /// </param>
        /// <param name="argc">
        /// See the <see cref="ISQLiteNativeModule.xUpdate" /> method.
        /// </param>
        /// <param name="argv">
        /// See the <see cref="ISQLiteNativeModule.xUpdate" /> method.
        /// </param>
        /// <param name="rowId">
        /// See the <see cref="ISQLiteNativeModule.xUpdate" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteNativeModule.xUpdate" /> method.
        /// </returns>
        private SQLiteErrorCode xUpdate(
            IntPtr pVtab,
            int argc,
            IntPtr argv,
            ref long rowId
            )
        {
            try
            {
                SQLiteVirtualTable table = TableFromIntPtr(pVtab);

                if (table != null)
                {
                    return Update(table,
                        SQLiteValue.ArrayFromSizeAndIntPtr(argc, argv),
                        ref rowId);
                }
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteNativeModule.xBegin" /> method.
        /// </summary>
        /// <param name="pVtab">
        /// See the <see cref="ISQLiteNativeModule.xBegin" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteNativeModule.xBegin" /> method.
        /// </returns>
        private SQLiteErrorCode xBegin(
            IntPtr pVtab
            )
        {
            try
            {
                SQLiteVirtualTable table = TableFromIntPtr(pVtab);

                if (table != null)
                    return Begin(table);
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteNativeModule.xSync" /> method.
        /// </summary>
        /// <param name="pVtab">
        /// See the <see cref="ISQLiteNativeModule.xSync" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteNativeModule.xSync" /> method.
        /// </returns>
        private SQLiteErrorCode xSync(
            IntPtr pVtab
            )
        {
            try
            {
                SQLiteVirtualTable table = TableFromIntPtr(pVtab);

                if (table != null)
                    return Sync(table);
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteNativeModule.xCommit" /> method.
        /// </summary>
        /// <param name="pVtab">
        /// See the <see cref="ISQLiteNativeModule.xCommit" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteNativeModule.xCommit" /> method.
        /// </returns>
        private SQLiteErrorCode xCommit(
            IntPtr pVtab
            )
        {
            try
            {
                SQLiteVirtualTable table = TableFromIntPtr(pVtab);

                if (table != null)
                    return Commit(table);
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteNativeModule.xRollback" /> method.
        /// </summary>
        /// <param name="pVtab">
        /// See the <see cref="ISQLiteNativeModule.xRollback" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteNativeModule.xRollback" /> method.
        /// </returns>
        private SQLiteErrorCode xRollback(
            IntPtr pVtab
            )
        {
            try
            {
                SQLiteVirtualTable table = TableFromIntPtr(pVtab);

                if (table != null)
                    return Rollback(table);
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteNativeModule.xFindFunction" /> method.
        /// </summary>
        /// <param name="pVtab">
        /// See the <see cref="ISQLiteNativeModule.xFindFunction" /> method.
        /// </param>
        /// <param name="nArg">
        /// See the <see cref="ISQLiteNativeModule.xFindFunction" /> method.
        /// </param>
        /// <param name="zName">
        /// See the <see cref="ISQLiteNativeModule.xFindFunction" /> method.
        /// </param>
        /// <param name="callback">
        /// See the <see cref="ISQLiteNativeModule.xFindFunction" /> method.
        /// </param>
        /// <param name="pClientData">
        /// See the <see cref="ISQLiteNativeModule.xFindFunction" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteNativeModule.xFindFunction" /> method.
        /// </returns>
        private int xFindFunction(
            IntPtr pVtab,
            int nArg,
            IntPtr zName,
            ref SQLiteCallback callback,
            ref IntPtr pClientData
            )
        {
            try
            {
                SQLiteVirtualTable table = TableFromIntPtr(pVtab);

                if (table != null)
                {
                    string name = SQLiteString.StringFromUtf8IntPtr(zName);
                    SQLiteFunction function = null;

                    if (FindFunction(
                            table, nArg, name, ref function, ref pClientData))
                    {
                        if (function != null)
                        {
                            string key = GetFunctionKey(nArg, name, function);

                            functions[key] = function;
                            callback = function.ScalarCallback;

                            return 1;
                        }
                        else
                        {
                            SetTableError(pVtab, "no function was created");
                        }
                    }
                }
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return 0; /* NOTE: On any error, return "no such function". */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteNativeModule.xRename" /> method.
        /// </summary>
        /// <param name="pVtab">
        /// See the <see cref="ISQLiteNativeModule.xRename" /> method.
        /// </param>
        /// <param name="zNew">
        /// See the <see cref="ISQLiteNativeModule.xRename" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteNativeModule.xRename" /> method.
        /// </returns>
        private SQLiteErrorCode xRename(
            IntPtr pVtab,
            IntPtr zNew
            )
        {
            try
            {
                SQLiteVirtualTable table = TableFromIntPtr(pVtab);

                if (table != null)
                {
                    return Rename(table,
                        SQLiteString.StringFromUtf8IntPtr(zNew));
                }
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteNativeModule.xSavepoint" /> method.
        /// </summary>
        /// <param name="pVtab">
        /// See the <see cref="ISQLiteNativeModule.xSavepoint" /> method.
        /// </param>
        /// <param name="iSavepoint">
        /// See the <see cref="ISQLiteNativeModule.xSavepoint" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteNativeModule.xSavepoint" /> method.
        /// </returns>
        private SQLiteErrorCode xSavepoint(
            IntPtr pVtab,
            int iSavepoint
            )
        {
            try
            {
                SQLiteVirtualTable table = TableFromIntPtr(pVtab);

                if (table != null)
                    return Savepoint(table, iSavepoint);
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteNativeModule.xRelease" /> method.
        /// </summary>
        /// <param name="pVtab">
        /// See the <see cref="ISQLiteNativeModule.xRelease" /> method.
        /// </param>
        /// <param name="iSavepoint">
        /// See the <see cref="ISQLiteNativeModule.xRelease" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteNativeModule.xRelease" /> method.
        /// </returns>
        private SQLiteErrorCode xRelease(
            IntPtr pVtab,
            int iSavepoint
            )
        {
            try
            {
                SQLiteVirtualTable table = TableFromIntPtr(pVtab);

                if (table != null)
                    return Release(table, iSavepoint);
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteNativeModule.xRollbackTo" /> method.
        /// </summary>
        /// <param name="pVtab">
        /// See the <see cref="ISQLiteNativeModule.xRollbackTo" /> method.
        /// </param>
        /// <param name="iSavepoint">
        /// See the <see cref="ISQLiteNativeModule.xRollbackTo" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteNativeModule.xRollbackTo" /> method.
        /// </returns>
        private SQLiteErrorCode xRollbackTo(
            IntPtr pVtab,
            int iSavepoint
            )
        {
            try
            {
                SQLiteVirtualTable table = TableFromIntPtr(pVtab);

                if (table != null)
                    return RollbackTo(table, iSavepoint);
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISQLiteManagedModule Members
        private bool declared;
        /// <summary>
        /// Returns non-zero if the schema for the virtual table has been
        /// declared.
        /// </summary>
        public virtual bool Declared
        {
            get { CheckDisposed(); return declared; }
            internal set { declared = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string name;
        /// <summary>
        /// Returns the name of the module as it was registered with the SQLite
        /// core library.
        /// </summary>
        public virtual string Name
        {
            get { CheckDisposed(); return name; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xCreate" /> method.
        /// </summary>
        /// <param name="connection">
        /// The <see cref="SQLiteConnection" /> object instance associated with
        /// the virtual table.
        /// </param>
        /// <param name="pClientData">
        /// The native user-data pointer associated with this module, as it was
        /// provided to the SQLite core library when the native module instance
        /// was created.
        /// </param>
        /// <param name="arguments">
        /// The module name, database name, virtual table name, and all other
        /// arguments passed to the CREATE VIRTUAL TABLE statement.
        /// </param>
        /// <param name="table">
        /// Upon success, this parameter must be modified to contain the
        /// <see cref="SQLiteVirtualTable" /> object instance associated with
        /// the virtual table.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter must be modified to contain an error
        /// message.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        public abstract SQLiteErrorCode Create(
            SQLiteConnection connection,
            IntPtr pClientData,
            string[] arguments,
            ref SQLiteVirtualTable table,
            ref string error
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xConnect" /> method.
        /// </summary>
        /// <param name="connection">
        /// The <see cref="SQLiteConnection" /> object instance associated with
        /// the virtual table.
        /// </param>
        /// <param name="pClientData">
        /// The native user-data pointer associated with this module, as it was
        /// provided to the SQLite core library when the native module instance
        /// was created.
        /// </param>
        /// <param name="arguments">
        /// The module name, database name, virtual table name, and all other
        /// arguments passed to the CREATE VIRTUAL TABLE statement.
        /// </param>
        /// <param name="table">
        /// Upon success, this parameter must be modified to contain the
        /// <see cref="SQLiteVirtualTable" /> object instance associated with
        /// the virtual table.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter must be modified to contain an error
        /// message.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        public abstract SQLiteErrorCode Connect(
            SQLiteConnection connection,
            IntPtr pClientData,
            string[] arguments,
            ref SQLiteVirtualTable table,
            ref string error
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xBestIndex" /> method.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this virtual table.
        /// </param>
        /// <param name="index">
        /// The <see cref="SQLiteIndex" /> object instance containing all the
        /// data for the inputs and outputs relating to index selection.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        public abstract SQLiteErrorCode BestIndex(
            SQLiteVirtualTable table,
            SQLiteIndex index
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xDisconnect" /> method.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this virtual table.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        public abstract SQLiteErrorCode Disconnect(
            SQLiteVirtualTable table
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xDestroy" /> method.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this virtual table.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        public abstract SQLiteErrorCode Destroy(
            SQLiteVirtualTable table
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xOpen" /> method.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this virtual table.
        /// </param>
        /// <param name="cursor">
        /// Upon success, this parameter must be modified to contain the
        /// <see cref="SQLiteVirtualTableCursor" /> object instance associated
        /// with the newly opened virtual table cursor.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        public abstract SQLiteErrorCode Open(
            SQLiteVirtualTable table,
            ref SQLiteVirtualTableCursor cursor
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xClose" /> method.
        /// </summary>
        /// <param name="cursor">
        /// The <see cref="SQLiteVirtualTableCursor" /> object instance
        /// associated with the previously opened virtual table cursor to be
        /// used.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        public abstract SQLiteErrorCode Close(
            SQLiteVirtualTableCursor cursor
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xFilter" /> method.
        /// </summary>
        /// <param name="cursor">
        /// The <see cref="SQLiteVirtualTableCursor" /> object instance
        /// associated with the previously opened virtual table cursor to be
        /// used.
        /// </param>
        /// <param name="indexNumber">
        /// Number used to help identify the selected index.
        /// </param>
        /// <param name="indexString">
        /// String used to help identify the selected index.
        /// </param>
        /// <param name="values">
        /// The values corresponding to each column in the selected index.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        public abstract SQLiteErrorCode Filter(
            SQLiteVirtualTableCursor cursor,
            int indexNumber,
            string indexString,
            SQLiteValue[] values
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xNext" /> method.
        /// </summary>
        /// <param name="cursor">
        /// The <see cref="SQLiteVirtualTableCursor" /> object instance
        /// associated with the previously opened virtual table cursor to be
        /// used.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        public abstract SQLiteErrorCode Next(
            SQLiteVirtualTableCursor cursor
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xEof" /> method.
        /// </summary>
        /// <param name="cursor">
        /// The <see cref="SQLiteVirtualTableCursor" /> object instance
        /// associated with the previously opened virtual table cursor to be
        /// used.
        /// </param>
        /// <returns>
        /// Non-zero if no more rows are available; zero otherwise.
        /// </returns>
        public abstract bool Eof(
            SQLiteVirtualTableCursor cursor
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xColumn" /> method.
        /// </summary>
        /// <param name="cursor">
        /// The <see cref="SQLiteVirtualTableCursor" /> object instance
        /// associated with the previously opened virtual table cursor to be
        /// used.
        /// </param>
        /// <param name="context">
        /// The <see cref="SQLiteContext" /> object instance to be used for
        /// returning the specified column value to the SQLite core library.
        /// </param>
        /// <param name="index">
        /// The zero-based index corresponding to the column containing the
        /// value to be returned.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        public abstract SQLiteErrorCode Column(
            SQLiteVirtualTableCursor cursor,
            SQLiteContext context,
            int index
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xRowId" /> method.
        /// </summary>
        /// <param name="cursor">
        /// The <see cref="SQLiteVirtualTableCursor" /> object instance
        /// associated with the previously opened virtual table cursor to be
        /// used.
        /// </param>
        /// <param name="rowId">
        /// Upon success, this parameter must be modified to contain the unique
        /// integer row identifier for the current row for the specified cursor.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        public abstract SQLiteErrorCode RowId(
            SQLiteVirtualTableCursor cursor,
            ref long rowId
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xUpdate" /> method.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this virtual table.
        /// </param>
        /// <param name="values">
        /// The array of <see cref="SQLiteValue" /> object instances containing
        /// the new or modified column values, if any.
        /// </param>
        /// <param name="rowId">
        /// Upon success, this parameter must be modified to contain the unique
        /// integer row identifier for the row that was inserted, if any.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        public abstract SQLiteErrorCode Update(
            SQLiteVirtualTable table,
            SQLiteValue[] values,
            ref long rowId
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xBegin" /> method.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this virtual table.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        public abstract SQLiteErrorCode Begin(
            SQLiteVirtualTable table
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xSync" /> method.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this virtual table.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        public abstract SQLiteErrorCode Sync(
            SQLiteVirtualTable table
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xCommit" /> method.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this virtual table.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        public abstract SQLiteErrorCode Commit(
            SQLiteVirtualTable table
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xRollback" /> method.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this virtual table.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        public abstract SQLiteErrorCode Rollback(
            SQLiteVirtualTable table
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xFindFunction" /> method.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this virtual table.
        /// </param>
        /// <param name="argumentCount">
        /// The number of arguments to the function being sought.
        /// </param>
        /// <param name="name">
        /// The name of the function being sought.
        /// </param>
        /// <param name="function">
        /// Upon success, this parameter must be modified to contain the
        /// <see cref="SQLiteFunction" /> object instance responsible for
        /// implementing the specified function.
        /// </param>
        /// <param name="pClientData">
        /// Upon success, this parameter must be modified to contain the
        /// native user-data pointer associated with
        /// <paramref name="function" />.
        /// </param>
        /// <returns>
        /// Non-zero if the specified function was found; zero otherwise.
        /// </returns>
        public abstract bool FindFunction(
            SQLiteVirtualTable table,
            int argumentCount,
            string name,
            ref SQLiteFunction function,
            ref IntPtr pClientData
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xRename" /> method.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this virtual table.
        /// </param>
        /// <param name="newName">
        /// The new name for the virtual table.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        public abstract SQLiteErrorCode Rename(
            SQLiteVirtualTable table,
            string newName
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xSavepoint" /> method.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this virtual table.
        /// </param>
        /// <param name="savepoint">
        /// This is an integer identifier under which the the current state of
        /// the virtual table should be saved.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        public abstract SQLiteErrorCode Savepoint(
            SQLiteVirtualTable table,
            int savepoint
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xRelease" /> method.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this virtual table.
        /// </param>
        /// <param name="savepoint">
        /// This is an integer used to indicate that any saved states with an
        /// identifier greater than or equal to this should be deleted by the
        /// virtual table.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        public abstract SQLiteErrorCode Release(
            SQLiteVirtualTable table,
            int savepoint
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called in response to the
        /// <see cref="ISQLiteNativeModule.xRollbackTo" /> method.
        /// </summary>
        /// <param name="table">
        /// The <see cref="SQLiteVirtualTable" /> object instance associated
        /// with this virtual table.
        /// </param>
        /// <param name="savepoint">
        /// This is an integer identifier used to specify a specific saved
        /// state for the virtual table for it to restore itself back to, which
        /// should also have the effect of deleting all saved states with an
        /// integer identifier greater than this one.
        /// </param>
        /// <returns>
        /// A standard SQLite return code.
        /// </returns>
        public abstract SQLiteErrorCode RollbackTo(
            SQLiteVirtualTable table,
            int savepoint
            );
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        /// <summary>
        /// Disposes of this object instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        /// <summary>
        /// Throws an <see cref="ObjectDisposedException" /> if this object
        /// instance has been disposed.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed)
            {
                throw new ObjectDisposedException(
                    typeof(SQLiteModule).Name);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Disposes of this object instance.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <see cref="Dispose()" /> method.  Zero if this method is being
        /// called from the finalizer.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    if (functions != null)
                        functions.Clear();
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                try
                {
                    if (disposableModule != IntPtr.Zero)
                    {
                        UnsafeNativeMethods.sqlite3_dispose_module(
                            disposableModule);

                        disposableModule = IntPtr.Zero;
                    }
                }
                catch (Exception e)
                {
                    try
                    {
                        if (LogExceptionsNoThrow)
                        {
                            SQLiteLog.LogMessage(SQLiteBase.COR_E_EXCEPTION,
                                String.Format(CultureInfo.CurrentCulture,
                                "Caught exception in \"Dispose\" method: {0}",
                                e)); /* throw */
                        }
                    }
                    catch
                    {
                        // do nothing.
                    }
                }
#if PLATFORM_COMPACTFRAMEWORK
                finally
                {
                    if (pNativeModule != IntPtr.Zero)
                    {
                        SQLiteMemory.Free(pNativeModule);
                        pNativeModule = IntPtr.Zero;
                    }
                }
#endif

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes this object instance.
        /// </summary>
        ~SQLiteModule()
        {
            Dispose(false);
        }
        #endregion
    }
    #endregion
}
