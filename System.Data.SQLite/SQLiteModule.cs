/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Joe Mistachkin (joe@mistachkin.com)
 *
 * Released to the public domain, use at your own risk!
 ********************************************************/

using System.Collections.Generic;
using System.Globalization;
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
        /// Sets the context result to the specified <see cref="System.Double" />
        /// value.
        /// </summary>
        /// <param name="value">
        /// The <see cref="System.Double" /> value to use.
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
        /// Sets the context result to the specified <see cref="System.Int32" />
        /// value.
        /// </summary>
        /// <param name="value">
        /// The <see cref="System.Int32" /> value to use.
        /// </param>
        public void SetInt(int value)
        {
            if (pContext == IntPtr.Zero)
                throw new InvalidOperationException();

            UnsafeNativeMethods.sqlite3_result_int(pContext, value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the context result to the specified <see cref="System.Int64" />
        /// value.
        /// </summary>
        /// <param name="value">
        /// The <see cref="System.Int64" /> value to use.
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
        /// Sets the context result to the specified <see cref="System.String" />
        /// value.
        /// </summary>
        /// <param name="value">
        /// The <see cref="System.String" /> value to use.  This value will be
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
        /// Sets the context result to the specified <see cref="System.String" />
        /// value containing an error message.
        /// </summary>
        /// <param name="value">
        /// The <see cref="System.String" /> value containing the error message
        /// text.  This value will be converted to the UTF-8 encoding prior to
        /// being used.
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
        /// Sets the context result to the specified <see cref="System.Byte" />
        /// array value.
        /// </summary>
        /// <param name="value">
        /// The <see cref="System.Byte" /> array value to use.
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
        internal SQLiteValue(IntPtr pValue)
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
        /// has been previously persisted via the <see cref="Persist"/>) method,
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
        /// Gets and returns the <see cref="System.Int32" /> associated with
        /// this value.
        /// </summary>
        /// <returns>
        /// The <see cref="System.Int32" /> associated with this value.
        /// </returns>
        public int GetInt()
        {
            if (pValue == IntPtr.Zero) return default(int);
            return UnsafeNativeMethods.sqlite3_value_int(pValue);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets and returns the <see cref="System.Int64" /> associated with
        /// this value.
        /// </summary>
        /// <returns>
        /// The <see cref="System.Int64" /> associated with this value.
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
        /// Gets and returns the <see cref="System.Double" /> associated with
        /// this value.
        /// </summary>
        /// <returns>
        /// The <see cref="System.Double" /> associated with this value.
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
        /// Gets and returns the <see cref="System.String" /> associated with
        /// this value.
        /// </summary>
        /// <returns>
        /// The <see cref="System.String" /> associated with this value.  The
        /// value is converted from the UTF-8 encoding prior to being returned.
        /// </returns>
        public string GetString()
        {
            if (pValue == IntPtr.Zero) return null;
            return SQLiteString.StringFromUtf8IntPtr(pValue, GetBytes());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets and returns the <see cref="System.Byte" /> array associated
        /// with this value.
        /// </summary>
        /// <returns>
        /// The <see cref="System.Byte" /> array associated with this value.
        /// </returns>
        public byte[] GetBlob()
        {
            if (pValue == IntPtr.Zero) return null;
            return SQLiteMarshal.BytesFromIntPtr(pValue, GetBytes());
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

        private double estimatedCost;
        /// <summary>
        /// Estimated cost of using this index.
        /// </summary>
        public double EstimatedCost
        {
            get { return estimatedCost; }
            set { estimatedCost = value; }
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
    /// virtual tables implemented in managed code.
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
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
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
        /// Throws an <see cref="System.ObjectDisposedException"/> if this
        /// object instance has been disposed.
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
    /// user-defined virtual table cursor implemented in managed code.
    /// </summary>
    public class SQLiteVirtualTableCursor :
            ISQLiteNativeHandle, IDisposable /* NOT SEALED */
    {
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
        {
            this.table = table;
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
        /// Throws an <see cref="System.ObjectDisposedException"/> if this
        /// object instance has been disposed.
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
        ///
        /// The job of this method is to construct the new virtual table object
        /// (an sqlite3_vtab object) and return a pointer to it in *ppVTab.
        ///
        /// As part of the task of creating a new sqlite3_vtab structure, this
        /// method must invoke sqlite3_declare_vtab() to tell the SQLite core
        /// about the columns and datatypes in the virtual table. The
        /// sqlite3_declare_vtab() API has the following prototype:
        ///
        /// int sqlite3_declare_vtab(sqlite3 *db, const char *zCreateTable)
        ///
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
            IntPtr[] argv,
            ref IntPtr pVtab,
            ref IntPtr pError
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The xConnect method is very similar to xCreate. It has the same
        /// parameters and constructs a new sqlite3_vtab structure just like
        /// xCreate. And it must also call sqlite3_declare_vtab() like xCreate.
        ///
        /// The difference is that xConnect is called to establish a new
        /// connection to an existing virtual table whereas xCreate is called
        /// to create a new virtual table from scratch.
        ///
        /// The xCreate and xConnect methods are only different when the
        /// virtual table has some kind of backing store that must be
        /// initialized the first time the virtual table is created. The
        /// xCreate method creates and initializes the backing store. The
        /// xConnect method just connects to an existing backing store.
        ///
        /// As an example, consider a virtual table implementation that
        /// provides read-only access to existing comma-separated-value (CSV)
        /// files on disk. There is no backing store that needs to be created
        /// or initialized for such a virtual table (since the CSV files
        /// already exist on disk) so the xCreate and xConnect methods will be
        /// identical for that module.
        ///
        /// Another example is a virtual table that implements a full-text
        /// index. The xCreate method must create and initialize data
        /// structures to hold the dictionary and posting lists for that index.
        /// The xConnect method, on the other hand, only has to locate and use
        /// an existing dictionary and posting lists that were created by a
        /// prior xCreate call.
        ///
        /// The xConnect method must return SQLITE_OK if it is successful in
        /// creating the new virtual table, or SQLITE_ERROR if it is not
        /// successful. If not successful, the sqlite3_vtab structure must not
        /// be allocated. An error message may optionally be returned in *pzErr
        /// if unsuccessful. Space to hold the error message string must be
        /// allocated using an SQLite memory allocation function like
        /// sqlite3_malloc() or sqlite3_mprintf() as the SQLite core will
        /// attempt to free the space using sqlite3_free() after the error has
        /// been reported up to the application.
        ///
        /// The xConnect method is required for every virtual table
        /// implementation, though the xCreate and xConnect pointers of the
        /// sqlite3_module object may point to the same function the virtual
        /// table does not need to initialize backing store.
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
            IntPtr[] argv,
            ref IntPtr pVtab,
            ref IntPtr pError
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// SQLite uses the xBestIndex method of a virtual table module to
        /// determine the best way to access the virtual table. The xBestIndex
        /// method has a prototype like this:
        ///
        ///  int (*xBestIndex)(sqlite3_vtab *pVTab, sqlite3_index_info*);
        ///
        /// The SQLite core communicates with the xBestIndex method by filling
        /// in certain fields of the sqlite3_index_info structure and passing a
        /// pointer to that structure into xBestIndex as the second parameter.
        /// The xBestIndex method fills out other fields of this structure
        /// which forms the reply. The sqlite3_index_info structure looks like
        /// this:
        ///
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
        ///
        /// In addition, there are some defined constants:
        ///
        ///  #define SQLITE_INDEX_CONSTRAINT_EQ    2
        ///  #define SQLITE_INDEX_CONSTRAINT_GT    4
        ///  #define SQLITE_INDEX_CONSTRAINT_LE    8
        ///  #define SQLITE_INDEX_CONSTRAINT_LT    16
        ///  #define SQLITE_INDEX_CONSTRAINT_GE    32
        ///  #define SQLITE_INDEX_CONSTRAINT_MATCH 64
        ///
        /// The SQLite core calls the xBestIndex method when it is compiling a
        /// query that involves a virtual table. In other words, SQLite calls
        /// this method when it is running sqlite3_prepare() or the equivalent.
        /// By calling this method, the SQLite core is saying to the virtual
        /// table that it needs to access some subset of the rows in the
        /// virtual table and it wants to know the most efficient way to do
        /// that access. The xBestIndex method replies with information that
        /// the SQLite core can then use to conduct an efficient search of the
        /// virtual table.
        ///
        /// While compiling a single SQL query, the SQLite core might call
        /// xBestIndex multiple times with different settings in
        /// sqlite3_index_info. The SQLite core will then select the
        /// combination that appears to give the best performance.
        ///
        /// Before calling this method, the SQLite core initializes an instance
        /// of the sqlite3_index_info structure with information about the
        /// query that it is currently trying to process. This information
        /// derives mainly from the WHERE clause and ORDER BY or GROUP BY
        /// clauses of the query, but also from any ON or USING clauses if the
        /// query is a join. The information that the SQLite core provides to
        /// the xBestIndex method is held in the part of the structure that is
        /// marked as "Inputs". The "Outputs" section is initialized to zero.
        ///
        /// The information in the sqlite3_index_info structure is ephemeral
        /// and may be overwritten or deallocated as soon as the xBestIndex
        /// method returns. If the xBestIndex method needs to remember any part
        /// of the sqlite3_index_info structure, it should make a copy. Care
        /// must be take to store the copy in a place where it will be
        /// deallocated, such as in the idxStr field with needToFreeIdxStr set
        /// to 1.
        ///
        /// Note that xBestIndex will always be called before xFilter, since
        /// the idxNum and idxStr outputs from xBestIndex are required inputs
        /// to xFilter. However, there is no guarantee that xFilter will be
        /// called following a successful xBestIndex.
        ///
        /// The xBestIndex method is required for every virtual table
        /// implementation.
        /// 
        /// 2.3.1 Inputs
        ///
        /// The main thing that the SQLite core is trying to communicate to the
        /// virtual table is the constraints that are available to limit the
        /// number of rows that need to be searched. The aConstraint[] array
        /// contains one entry for each constraint. There will be exactly
        /// nConstraint entries in that array.
        ///
        /// Each constraint will correspond to a term in the WHERE clause or in
        /// a USING or ON clause that is of the form
        ///
        ///     column OP EXPR
        ///
        /// Where "column" is a column in the virtual table, OP is an operator
        /// like "=" or "&lt;", and EXPR is an arbitrary expression. So, for
        /// example, if the WHERE clause contained a term like this:
        ///
        ///          a = 5
        ///
        /// Then one of the constraints would be on the "a" column with
        /// operator "=" and an expression of "5". Constraints need not have a
        /// literal representation of the WHERE clause. The query optimizer
        /// might make transformations to the WHERE clause in order to extract
        /// as many constraints as it can. So, for example, if the WHERE clause
        /// contained something like this:
        ///
        ///          x BETWEEN 10 AND 100 AND 999&gt;y
        ///
        /// The query optimizer might translate this into three separate
        /// constraints:
        ///
        ///          x &gt;= 10
        ///          x &lt;= 100
        ///          y &lt; 999
        ///
        /// For each constraint, the aConstraint[].iColumn field indicates
        /// which column appears on the left-hand side of the constraint. The
        /// first column of the virtual table is column 0. The rowid of the
        /// virtual table is column -1. The aConstraint[].op field indicates
        /// which operator is used. The SQLITE_INDEX_CONSTRAINT_* constants map
        /// integer constants into operator values. Columns occur in the order
        /// they were defined by the call to sqlite3_declare_vtab() in the
        /// xCreate or xConnect method. Hidden columns are counted when
        /// determining the column index.
        ///
        /// The aConstraint[] array contains information about all constraints
        /// that apply to the virtual table. But some of the constraints might
        /// not be usable because of the way tables are ordered in a join. The
        /// xBestIndex method must therefore only consider constraints that
        /// have an aConstraint[].usable flag which is true.
        ///
        /// In addition to WHERE clause constraints, the SQLite core also tells
        /// the xBestIndex method about the ORDER BY clause. (In an aggregate
        /// query, the SQLite core might put in GROUP BY clause information in
        /// place of the ORDER BY clause information, but this fact should not
        /// make any difference to the xBestIndex method.) If all terms of the
        /// ORDER BY clause are columns in the virtual table, then nOrderBy
        /// will be the number of terms in the ORDER BY clause and the
        /// aOrderBy[] array will identify the column for each term in the
        /// order by clause and whether or not that column is ASC or DESC.
        /// 
        /// 2.3.2 Outputs
        ///
        /// Given all of the information above, the job of the xBestIndex
        /// method it to figure out the best way to search the virtual table.
        ///
        /// The xBestIndex method fills the idxNum and idxStr fields with
        /// information that communicates an indexing strategy to the xFilter
        /// method. The information in idxNum and idxStr is arbitrary as far as
        /// the SQLite core is concerned. The SQLite core just copies the
        /// information through to the xFilter method. Any desired meaning can
        /// be assigned to idxNum and idxStr as long as xBestIndex and xFilter
        /// agree on what that meaning is.
        ///
        /// The idxStr value may be a string obtained from an SQLite memory
        /// allocation function such as sqlite3_mprintf(). If this is the case,
        /// then the needToFreeIdxStr flag must be set to true so that the
        /// SQLite core will know to call sqlite3_free() on that string when it
        /// has finished with it, and thus avoid a memory leak.
        ///
        /// If the virtual table will output rows in the order specified by the
        /// ORDER BY clause, then the orderByConsumed flag may be set to true.
        /// If the output is not automatically in the correct order then
        /// orderByConsumed must be left in its default false setting. This
        /// will indicate to the SQLite core that it will need to do a separate
        /// sorting pass over the data after it comes out of the virtual table.
        ///
        /// The estimatedCost field should be set to the estimated number of
        /// disk access operations required to execute this query against the
        /// virtual table. The SQLite core will often call xBestIndex multiple
        /// times with different constraints, obtain multiple cost estimates,
        /// then choose the query plan that gives the lowest estimate.
        ///
        /// The aConstraintUsage[] array contains one element for each of the
        /// nConstraint constraints in the inputs section of the
        /// sqlite3_index_info structure. The aConstraintUsage[] array is used
        /// by xBestIndex to tell the core how it is using the constraints.
        ///
        /// The xBestIndex method may set aConstraintUsage[].argvIndex entries
        /// to values greater than one. Exactly one entry should be set to 1,
        /// another to 2, another to 3, and so forth up to as many or as few as
        /// the xBestIndex method wants. The EXPR of the corresponding
        /// constraints will then be passed in as the argv[] parameters to
        /// xFilter.
        ///
        /// For example, if the aConstraint[3].argvIndex is set to 1, then when
        /// xFilter is called, the argv[0] passed to xFilter will have the EXPR
        /// value of the aConstraint[3] constraint.
        ///
        /// By default, the SQLite core double checks all constraints on each
        /// row of the virtual table that it receives. If such a check is
        /// redundant, the xBestFilter method can suppress that double-check by
        /// setting aConstraintUsage[].omit.
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
        /// This method releases a connection to a virtual table. Only the
        /// sqlite3_vtab object is destroyed. The virtual table is not
        /// destroyed and any backing store associated with the virtual table
        /// persists. This method undoes the work of xConnect.
        ///
        /// This method is a destructor for a connection to the virtual table.
        /// Contrast this method with xDestroy. The xDestroy is a destructor
        /// for the entire virtual table.
        ///
        /// The xDisconnect method is required for every virtual table
        /// implementation, though it is acceptable for the xDisconnect and
        /// xDestroy methods to be the same function if that makes sense for
        /// the particular virtual table.
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
        /// This method releases a connection to a virtual table, just like the
        /// xDisconnect method, and it also destroys the underlying table
        /// implementation. This method undoes the work of xCreate.
        ///
        /// The xDisconnect method is called whenever a database connection
        /// that uses a virtual table is closed. The xDestroy method is only
        /// called when a DROP TABLE statement is executed against the virtual
        /// table.
        ///
        /// The xDestroy method is required for every virtual table
        /// implementation, though it is acceptable for the xDisconnect and
        /// xDestroy methods to be the same function if that makes sense for
        /// the particular virtual table.
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
        /// The xOpen method creates a new cursor used for accessing (read
        /// and/or writing) a virtual table. A successful invocation of this
        /// method will allocate the memory for the sqlite3_vtab_cursor (or a
        /// subclass), initialize the new object, and make *ppCursor point to
        /// the new object. The successful call then returns SQLITE_OK.
        ///
        /// For every successful call to this method, the SQLite core will
        /// later invoke the xClose method to destroy the allocated cursor.
        ///
        /// The xOpen method need not initialize the pVtab field of the
        /// sqlite3_vtab_cursor structure. The SQLite core will take care of
        /// that chore automatically.
        ///
        /// A virtual table implementation must be able to support an arbitrary
        /// number of simultaneously open cursors.
        ///
        /// When initially opened, the cursor is in an undefined state. The
        /// SQLite core will invoke the xFilter method on the cursor prior to
        /// any attempt to position or read from the cursor.
        ///
        /// The xOpen method is required for every virtual table
        /// implementation.
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
        /// The xClose method closes a cursor previously opened by xOpen. The
        /// SQLite core will always call xClose once for each cursor opened
        /// using xOpen.
        ///
        /// This method must release all resources allocated by the
        /// corresponding xOpen call. The routine will not be called again even
        /// if it returns an error. The SQLite core will not use the
        /// sqlite3_vtab_cursor again after it has been closed.
        ///
        /// The xClose method is required for every virtual table
        /// implementation.
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
        /// This method begins a search of a virtual table. The first argument
        /// is a cursor opened by xOpen. The next two argument define a
        /// particular search index previously chosen by xBestIndex. The
        /// specific meanings of idxNum and idxStr are unimportant as long as
        /// xFilter and xBestIndex agree on what that meaning is.
        ///
        /// The xBestIndex function may have requested the values of certain
        /// expressions using the aConstraintUsage[].argvIndex values of the
        /// sqlite3_index_info structure. Those values are passed to xFilter
        /// using the argc and argv parameters.
        ///
        /// If the virtual table contains one or more rows that match the
        /// search criteria, then the cursor must be left point at the first
        /// row. Subsequent calls to xEof must return false (zero). If there
        /// are no rows match, then the cursor must be left in a state that
        /// will cause the xEof to return true (non-zero). The SQLite engine
        /// will use the xColumn and xRowid methods to access that row content.
        /// The xNext method will be used to advance to the next row.
        ///
        /// This method must return SQLITE_OK if successful, or an sqlite error
        /// code if an error occurs.
        ///
        /// The xFilter method is required for every virtual table
        /// implementation.
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
            IntPtr[] argv
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// 
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
        /// 
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
        /// The SQLite core invokes this method in order to find the value for
        /// the N-th column of the current row. N is zero-based so the first
        /// column is numbered 0. The xColumn method may return its result back
        /// to SQLite using one of the following interface:
        ///
        ///   * sqlite3_result_blob()
        ///   * sqlite3_result_double()
        ///   * sqlite3_result_int()
        ///   * sqlite3_result_int64()
        ///   * sqlite3_result_null()
        ///   * sqlite3_result_text()
        ///   * sqlite3_result_text16()
        ///   * sqlite3_result_text16le()
        ///   * sqlite3_result_text16be()
        ///   * sqlite3_result_zeroblob()
        ///
        /// If the xColumn method implementation calls none of the functions
        /// above, then the value of the column defaults to an SQL NULL.
        ///
        /// To raise an error, the xColumn method should use one of the
        /// result_text() methods to set the error message text, then return an
        /// appropriate error code. The xColumn method must return SQLITE_OK on
        /// success.
        ///
        /// The xColumn method is required for every virtual table
        /// implementation.
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
        /// A successful invocation of this method will cause *pRowid to be
        /// filled with the rowid of row that the virtual table cursor pCur is
        /// currently pointing at. This method returns SQLITE_OK on success. It
        /// returns an appropriate error code on failure.
        ///
        /// The xRowid method is required for every virtual table
        /// implementation.
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
        /// All changes to a virtual table are made using the xUpdate method.
        /// This one method can be used to insert, delete, or update.
        ///
        /// The argc parameter specifies the number of entries in the argv
        /// array. The value of argc will be 1 for a pure delete operation or
        /// N+2 for an insert or replace or update where N is the number of
        /// columns in the table. In the previous sentence, N includes any
        /// hidden columns.
        ///
        /// Every argv entry will have a non-NULL value in C but may contain
        /// the SQL value NULL. In other words, it is always true that
        /// argv[i]!=0 for i between 0 and argc-1. However, it might be the
        /// case that sqlite3_value_type(argv[i])==SQLITE_NULL.
        ///
        /// The argv[0] parameter is the rowid of a row in the virtual table
        /// to be deleted. If argv[0] is an SQL NULL, then no deletion occurs.
        ///
        /// The argv[1] parameter is the rowid of a new row to be inserted into
        /// the virtual table. If argv[1] is an SQL NULL, then the
        /// implementation must choose a rowid for the newly inserted row.
        /// Subsequent argv[] entries contain values of the columns of the
        /// virtual table, in the order that the columns were declared. The
        /// number of columns will match the table declaration that the
        /// xConnect or xCreate method made using the sqlite3_declare_vtab()
        /// call. All hidden columns are included.
        ///
        /// When doing an insert without a rowid (argc>1, argv[1] is an SQL
        /// NULL), the implementation must set *pRowid to the rowid of the
        /// newly inserted row; this will become the value returned by the
        /// sqlite3_last_insert_rowid() function. Setting this value in all the
        /// other cases is a harmless no-op; the SQLite engine ignores the
        /// *pRowid return value if argc==1 or argv[1] is not an SQL NULL.
        ///
        /// Each call to xUpdate will fall into one of cases shown below. Note
        /// that references to argv[i] mean the SQL value held within the
        /// argv[i] object, not the argv[i] object itself.
        ///
        ///     argc = 1
        ///
        ///         The single row with rowid equal to argv[0] is deleted. No
        ///         insert occurs.
        ///
        ///     argc > 1
        ///     argv[0] = NULL
        ///
        ///         A new row is inserted with a rowid argv[1] and column
        ///         values in argv[2] and following. If argv[1] is an SQL NULL,
        ///         the a new unique rowid is generated automatically.
        ///
        ///     argc > 1
        ///     argv[0] ? NULL
        ///     argv[0] = argv[1]
        ///
        ///         The row with rowid argv[0] is updated with new values in
        ///         argv[2] and following parameters.
        ///
        ///     argc > 1
        ///     argv[0] ? NULL
        ///     argv[0] ? argv[1]
        ///
        ///         The row with rowid argv[0] is updated with rowid argv[1]
        ///         and new values in argv[2] and following parameters. This
        ///         will occur when an SQL statement updates a rowid, as in
        ///         the statement:
        ///
        ///             UPDATE table SET rowid=rowid+1 WHERE ...;
        ///
        /// The xUpdate method must return SQLITE_OK if and only if it is
        /// successful. If a failure occurs, the xUpdate must return an
        /// appropriate error code. On a failure, the pVTab->zErrMsg element
        /// may optionally be replaced with error message text stored in memory
        /// allocated from SQLite using functions such as sqlite3_mprintf() or
        /// sqlite3_malloc().
        ///
        /// If the xUpdate method violates some constraint of the virtual table
        /// (including, but not limited to, attempting to store a value of the
        /// wrong datatype, attempting to store a value that is too large or
        /// too small, or attempting to change a read-only value) then the
        /// xUpdate must fail with an appropriate error code.
        ///
        /// There might be one or more sqlite3_vtab_cursor objects open and in
        /// use on the virtual table instance and perhaps even on the row of
        /// the virtual table when the xUpdate method is invoked. The
        /// implementation of xUpdate must be prepared for attempts to delete
        /// or modify rows of the table out from other existing cursors. If the
        /// virtual table cannot accommodate such changes, the xUpdate method
        /// must return an error code.
        ///
        /// The xUpdate method is optional. If the xUpdate pointer in the
        /// sqlite3_module for a virtual table is a NULL pointer, then the
        /// virtual table is read-only.
        /// </summary>
        /// <param name="pVtab">
        /// The native pointer to the sqlite3_vtab derived structure.
        /// </param>
        /// <param name="nData">
        /// The number of new or modified column values contained in
        /// <paramref name="apData" />.
        /// </param>
        /// <param name="apData">
        /// 
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
            int nData,
            IntPtr apData,
            ref long rowId
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// 
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
        /// 
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
        /// 
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
        /// 
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
        /// 
        /// </summary>
        /// <param name="pVtab">
        /// The native pointer to the sqlite3_vtab derived structure.
        /// </param>
        /// <param name="nArg">
        /// 
        /// </param>
        /// <param name="zName">
        /// 
        /// </param>
        /// <param name="callback">
        /// 
        /// </param>
        /// <param name="pClientData">
        /// 
        /// </param>
        /// <returns>
        /// 
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
        /// 
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
        /// 
        /// </summary>
        /// <param name="pVtab">
        /// The native pointer to the sqlite3_vtab derived structure.
        /// </param>
        /// <param name="iSavepoint">
        /// 
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
        /// 
        /// </summary>
        /// <param name="pVtab">
        /// The native pointer to the sqlite3_vtab derived structure.
        /// </param>
        /// <param name="iSavepoint">
        /// 
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
        /// 
        /// </summary>
        /// <param name="pVtab">
        /// The native pointer to the sqlite3_vtab derived structure.
        /// </param>
        /// <param name="iSavepoint">
        /// 
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
        bool Declared { get; }

        ///////////////////////////////////////////////////////////////////////

        string Name { get; }

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Create(
            SQLiteConnection connection,  /* in */
            IntPtr pClientData,           /* in */
            string[] arguments,           /* in */
            ref SQLiteVirtualTable table, /* out */
            ref string error              /* out */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Connect(
            SQLiteConnection connection,  /* in */
            IntPtr pClientData,           /* in */
            string[] arguments,           /* in */
            ref SQLiteVirtualTable table, /* out */
            ref string error              /* out */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode BestIndex(
            SQLiteVirtualTable table, /* in */
            SQLiteIndex index         /* in, out */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Disconnect(
            SQLiteVirtualTable table /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Destroy(
            SQLiteVirtualTable table /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Open(
            SQLiteVirtualTable table,           /* in */
            ref SQLiteVirtualTableCursor cursor /* out */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Close(
            SQLiteVirtualTableCursor cursor /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Filter(
            SQLiteVirtualTableCursor cursor, /* in */
            int indexNumber,                 /* in */
            string indexString,              /* in */
            SQLiteValue[] values             /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Next(
            SQLiteVirtualTableCursor cursor /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        bool Eof(
            SQLiteVirtualTableCursor cursor /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Column(
            SQLiteVirtualTableCursor cursor, /* in */
            SQLiteContext context,           /* in */
            int index                        /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode RowId(
            SQLiteVirtualTableCursor cursor, /* in */
            ref long rowId                   /* out */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Update(
            SQLiteVirtualTable table, /* in */
            SQLiteValue[] values,     /* in */
            ref long rowId            /* in, out */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Begin(
            SQLiteVirtualTable table /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Sync(
            SQLiteVirtualTable table /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Commit(
            SQLiteVirtualTable table /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Rollback(
            SQLiteVirtualTable table /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        bool FindFunction(
            SQLiteVirtualTable table,    /* in */
            int argumentCount,           /* in */
            string name,                 /* in */
            ref SQLiteFunction function, /* out */
            ref IntPtr pClientData       /* out */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Rename(
            SQLiteVirtualTable table, /* in */
            string newName            /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Savepoint(
            SQLiteVirtualTable table, /* in */
            int savepoint             /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Release(
            SQLiteVirtualTable table, /* in */
            int savepoint             /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode RollbackTo(
            SQLiteVirtualTable table, /* in */
            int savepoint             /* in */
            );
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteMemory Static Class
    internal static class SQLiteMemory
    {
        #region Private Data
#if TRACK_MEMORY_BYTES
        private static object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        private static int bytesAllocated;
        private static int maximumBytesAllocated;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Memory Allocation Helper Methods
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

        public static int Size(IntPtr pMemory)
        {
#if !SQLITE_STANDARD
            return UnsafeNativeMethods.sqlite3_malloc_size_interop(pMemory);
#else
            return 0;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

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
    internal static class SQLiteString
    {
        #region Private Constants
        private static int ThirtyBits = 0x3fffffff;
        private static readonly Encoding Utf8Encoding = Encoding.UTF8;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region UTF-8 Encoding Helper Methods
        public static byte[] GetUtf8BytesFromString(
            string value
            )
        {
            if (value == null)
                return null;

            return Utf8Encoding.GetBytes(value);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static int ProbeForUtf8ByteLength(
            IntPtr pValue,
            int limit
            )
        {
            int length = 0;

            if (pValue != IntPtr.Zero)
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

        public static string StringFromUtf8IntPtr(
            IntPtr pValue
            )
        {
            return StringFromUtf8IntPtr(pValue,
                ProbeForUtf8ByteLength(pValue, ThirtyBits));
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static string[] StringArrayFromUtf8IntPtrArray(
            IntPtr[] pValues
            )
        {
            if (pValues == null)
                return null;

            string[] result = new string[pValues.Length];

            for (int index = 0; index < result.Length; index++)
                result[index] = StringFromUtf8IntPtr(pValues[index]);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

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

    #region SQLiteMarshal Static Class
    internal static class SQLiteMarshal
    {
        #region IntPtr Helper Methods
        public static IntPtr IntPtrForOffset(
            IntPtr pointer,
            int offset
            )
        {
            return new IntPtr(pointer.ToInt64() + offset);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Marshal Read Helper Methods
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

        #region Byte Array Helper Methods
        public static byte[] BytesFromIntPtr(
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

        public static IntPtr BytesToIntPtr(
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

        ///////////////////////////////////////////////////////////////////////

        #region SQLiteValue Helper Methods
        public static SQLiteValue[] ValueArrayFromSizeAndIntPtr(
            int nData,
            IntPtr apData
            )
        {
            if (nData < 0)
                return null;

            if (apData == IntPtr.Zero)
                return null;

            SQLiteValue[] result = new SQLiteValue[nData];

            for (int index = 0, offset = 0;
                    index < result.Length;
                    index++, offset += IntPtr.Size)
            {
                IntPtr pData = ReadIntPtr(apData, offset);

                result[index] = (pData != IntPtr.Zero) ?
                    new SQLiteValue(pData) : null;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static SQLiteValue[] ValueArrayFromIntPtrArray(
            IntPtr[] values
            )
        {
            if (values == null)
                return null;

            SQLiteValue[] result = new SQLiteValue[values.Length];

            for (int index = 0; index < result.Length; index++)
                result[index] = new SQLiteValue(values[index]);

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region SQLiteIndex Helper Methods
        public static void IndexFromIntPtr(
            IntPtr pIndex,
            ref SQLiteIndex index
            )
        {
            if (pIndex == IntPtr.Zero)
                return;

            int offset = 0;

            int nConstraint = ReadInt32(pIndex, offset);

            offset += sizeof(int);

            IntPtr pConstraint = ReadIntPtr(pIndex, offset);

            offset += IntPtr.Size;

            int nOrderBy = ReadInt32(pIndex, offset);

            index = new SQLiteIndex(nConstraint, nOrderBy);

            offset += sizeof(int);

            IntPtr pOrderBy = ReadIntPtr(pIndex, offset);

            offset += IntPtr.Size;

            IntPtr pConstraintUsage = ReadIntPtr(pIndex, offset);

            offset += IntPtr.Size;

            index.Outputs.IndexNumber = ReadInt32(pIndex, offset);

            offset += sizeof(int);

            index.Outputs.IndexString = SQLiteString.StringFromUtf8IntPtr(
                IntPtrForOffset(pIndex, offset));

            offset += IntPtr.Size;

            index.Outputs.NeedToFreeIndexString = ReadInt32(pIndex, offset);

            offset += sizeof(int);

            index.Outputs.OrderByConsumed = ReadInt32(pIndex, offset);

            offset += sizeof(int);

            index.Outputs.EstimatedCost = ReadDouble(pIndex, offset);

            int sizeOfConstraintType = Marshal.SizeOf(typeof(
                UnsafeNativeMethods.sqlite3_index_constraint));

            for (int iConstraint = 0; iConstraint < nConstraint; iConstraint++)
            {
                UnsafeNativeMethods.sqlite3_index_constraint constraint =
                    new UnsafeNativeMethods.sqlite3_index_constraint();

                Marshal.PtrToStructure(IntPtrForOffset(pConstraint,
                    iConstraint * sizeOfConstraintType), constraint);

                index.Inputs.Constraints[iConstraint] =
                    new SQLiteIndexConstraint(constraint);
            }

            int sizeOfOrderByType = Marshal.SizeOf(typeof(
                UnsafeNativeMethods.sqlite3_index_orderby));

            for (int iOrderBy = 0; iOrderBy < nOrderBy; iOrderBy++)
            {
                UnsafeNativeMethods.sqlite3_index_orderby orderBy =
                    new UnsafeNativeMethods.sqlite3_index_orderby();

                Marshal.PtrToStructure(IntPtrForOffset(pOrderBy,
                    iOrderBy * sizeOfOrderByType), orderBy);

                index.Inputs.OrderBys[iOrderBy] =
                    new SQLiteIndexOrderBy(orderBy);
            }

            int sizeOfConstraintUsageType = Marshal.SizeOf(typeof(
                UnsafeNativeMethods.sqlite3_index_constraint_usage));

            for (int iConstraint = 0; iConstraint < nConstraint; iConstraint++)
            {
                UnsafeNativeMethods.sqlite3_index_constraint_usage constraintUsage =
                    new UnsafeNativeMethods.sqlite3_index_constraint_usage();

                Marshal.PtrToStructure(IntPtrForOffset(pConstraintUsage,
                    iConstraint * sizeOfConstraintUsageType), constraintUsage);

                index.Outputs.ConstraintUsages[iConstraint] =
                    new SQLiteIndexConstraintUsage(constraintUsage);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void IndexToIntPtr(
            SQLiteIndex index,
            IntPtr pIndex
            )
        {
            if ((index == null) || (index.Inputs == null) ||
                (index.Inputs.Constraints == null) ||
                (index.Inputs.OrderBys == null) || (index.Outputs == null) ||
                (index.Outputs.ConstraintUsages == null))
            {
                return;
            }

            if (pIndex == IntPtr.Zero)
                return;

            int offset = 0;

            int nConstraint = ReadInt32(pIndex, offset);

            if (nConstraint != index.Inputs.Constraints.Length)
                return;

            if (nConstraint != index.Outputs.ConstraintUsages.Length)
                return;

            offset += sizeof(int);

            IntPtr pConstraint = ReadIntPtr(pIndex, offset);

            offset += IntPtr.Size;

            int nOrderBy = ReadInt32(pIndex, offset);

            offset += sizeof(int);

            IntPtr pOrderBy = ReadIntPtr(pIndex, offset);

            offset += IntPtr.Size;

            IntPtr pConstraintUsage = ReadIntPtr(pIndex, offset);

            int sizeOfConstraintType = Marshal.SizeOf(typeof(
                UnsafeNativeMethods.sqlite3_index_constraint));

            for (int iConstraint = 0; iConstraint < nConstraint; iConstraint++)
            {
                UnsafeNativeMethods.sqlite3_index_constraint constraint =
                    new UnsafeNativeMethods.sqlite3_index_constraint(
                        index.Inputs.Constraints[iConstraint]);

                Marshal.StructureToPtr(
                    constraint, IntPtrForOffset(pConstraint,
                    iConstraint * sizeOfConstraintType), false);

                index.Inputs.Constraints[iConstraint] =
                    new SQLiteIndexConstraint(constraint);
            }

            int sizeOfOrderByType = Marshal.SizeOf(typeof(
                UnsafeNativeMethods.sqlite3_index_orderby));

            for (int iOrderBy = 0; iOrderBy < nOrderBy; iOrderBy++)
            {
                UnsafeNativeMethods.sqlite3_index_orderby orderBy =
                    new UnsafeNativeMethods.sqlite3_index_orderby(
                        index.Inputs.OrderBys[iOrderBy]);

                Marshal.StructureToPtr(
                    orderBy, IntPtrForOffset(pOrderBy,
                    iOrderBy * sizeOfOrderByType), false);

                index.Inputs.OrderBys[iOrderBy] =
                    new SQLiteIndexOrderBy(orderBy);
            }

            int sizeOfConstraintUsageType = Marshal.SizeOf(typeof(
                UnsafeNativeMethods.sqlite3_index_constraint_usage));

            for (int iConstraint = 0; iConstraint < nConstraint; iConstraint++)
            {
                UnsafeNativeMethods.sqlite3_index_constraint_usage constraintUsage =
                    new UnsafeNativeMethods.sqlite3_index_constraint_usage(
                        index.Outputs.ConstraintUsages[iConstraint]);

                Marshal.StructureToPtr(
                    constraintUsage, IntPtrForOffset(pConstraintUsage,
                    iConstraint * sizeOfConstraintUsageType), false);

                index.Outputs.ConstraintUsages[iConstraint] =
                    new SQLiteIndexConstraintUsage(constraintUsage);
            }
        }
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteModule Base Class
    /* NOT SEALED */
    public abstract class SQLiteModule :
            ISQLiteManagedModule, /*ISQLiteNativeModule,*/ IDisposable
    {
        #region Private Constants
        private const double DefaultCost = double.MaxValue;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private UnsafeNativeMethods.sqlite3_module nativeModule;
        private Dictionary<IntPtr, SQLiteVirtualTable> tables;
        private Dictionary<IntPtr, SQLiteVirtualTableCursor> cursors;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Methods
        internal UnsafeNativeMethods.sqlite3_module CreateNativeModule()
        {
            return CreateNativeModule(CreateNativeModuleImpl());
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public SQLiteModule(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            this.name = name;
            this.tables = new Dictionary<IntPtr, SQLiteVirtualTable>();
            this.cursors = new Dictionary<IntPtr, SQLiteVirtualTableCursor>();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private UnsafeNativeMethods.sqlite3_module CreateNativeModule(
            ISQLiteNativeModule module
            )
        {
            nativeModule = new UnsafeNativeMethods.sqlite3_module();
            nativeModule.iVersion = 2;

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
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Members
        #region Module Helper Methods
        protected virtual ISQLiteNativeModule CreateNativeModuleImpl()
        {
            return null; /* NOTE: Use built-in defaults. */
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Native Table Helper Methods
        protected virtual IntPtr AllocateTable()
        {
            int size = Marshal.SizeOf(typeof(
                UnsafeNativeMethods.sqlite3_vtab));

            return SQLiteMemory.Allocate(size);
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual void ZeroTable(
            IntPtr pVtab
            )
        {
            if (pVtab == IntPtr.Zero)
                return;

            int offset = 0;

            SQLiteMarshal.WriteIntPtr(pVtab, offset, IntPtr.Zero);

            offset += IntPtr.Size;

            SQLiteMarshal.WriteInt32(pVtab, offset, 0);

            offset += sizeof(int);

            SQLiteMarshal.WriteIntPtr(pVtab, offset, IntPtr.Zero);
        }

        ///////////////////////////////////////////////////////////////////////

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
        protected virtual IntPtr AllocateCursor()
        {
            int size = Marshal.SizeOf(typeof(
                UnsafeNativeMethods.sqlite3_vtab_cursor));

            return SQLiteMemory.Allocate(size);
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual void FreeCursor(
            IntPtr pCursor
            )
        {
            SQLiteMemory.Free(pCursor);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Table Lookup Methods
        protected virtual IntPtr TableFromCursor(
            IntPtr pCursor
            )
        {
            if (pCursor == IntPtr.Zero)
                return IntPtr.Zero;

            return Marshal.ReadIntPtr(pCursor);
        }

        ///////////////////////////////////////////////////////////////////////

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

        #region Table Declaration Helper Methods
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

            return sqliteBase.DeclareVirtualTable(this, sql, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Error Handling Helper Methods
        protected virtual bool SetTableError(
            IntPtr pVtab,
            string error
            )
        {
            try
            {
                if (LogErrors)
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

            if (pVtab == IntPtr.Zero)
                return false;

            int offset = IntPtr.Size + sizeof(int);
            IntPtr pError = SQLiteMarshal.ReadIntPtr(pVtab, offset);

            if (pError != IntPtr.Zero)
            {
                SQLiteMemory.Free(pError); pError = IntPtr.Zero;
                SQLiteMarshal.WriteIntPtr(pVtab, offset, pError);
            }

            if (error == null)
                return true;

            bool success = false;

            try
            {
                pError = SQLiteString.Utf8IntPtrFromString(error);
                SQLiteMarshal.WriteIntPtr(pVtab, offset, pError);
                success = true;
            }
            finally
            {
                if (!success && (pError != IntPtr.Zero))
                {
                    SQLiteMemory.Free(pError);
                    pError = IntPtr.Zero;
                }
            }

            return success;
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual bool SetTableError(
            SQLiteVirtualTable table,
            string error
            )
        {
            if (table == null)
                return false;

            IntPtr pVtab = table.NativeHandle;

            if (pVtab == IntPtr.Zero)
                return false;

            return SetTableError(pVtab, error);
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual bool SetCursorError(
            SQLiteVirtualTableCursor cursor,
            string error
            )
        {
            if (cursor == null)
                return false;

            IntPtr pCursor = cursor.NativeHandle;

            if (pCursor == IntPtr.Zero)
                return false;

            IntPtr pVtab = TableFromCursor(pCursor);

            if (pVtab == IntPtr.Zero)
                return false;

            return SetTableError(pVtab, error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Index Handling Helper Methods
        protected virtual bool SetEstimatedCost(
            SQLiteIndex index,
            double estimatedCost
            )
        {
            if ((index == null) || (index.Outputs == null))
                return false;

            index.Outputs.EstimatedCost = estimatedCost;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual bool SetEstimatedCost(
            SQLiteIndex index
            )
        {
            return SetEstimatedCost(index, DefaultCost);
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private bool logErrors;
        public virtual bool LogErrors
        {
            get { CheckDisposed(); return logErrors; }
            set { CheckDisposed(); logErrors = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool logExceptions;
        public virtual bool LogExceptions
        {
            get { CheckDisposed(); return logExceptions; }
            set { CheckDisposed(); logExceptions = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISQLiteNativeModule Members
        private SQLiteErrorCode xCreate(
            IntPtr pDb,
            IntPtr pAux,
            int argc,
            IntPtr[] argv,
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

                    if (Create(connection, pAux,
                            SQLiteString.StringArrayFromUtf8IntPtrArray(argv),
                            ref table, ref error) == SQLiteErrorCode.Ok)
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

        private SQLiteErrorCode xConnect(
            IntPtr pDb,
            IntPtr pAux,
            int argc,
            IntPtr[] argv,
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

                    if (Connect(connection, pAux,
                            SQLiteString.StringArrayFromUtf8IntPtrArray(argv),
                            ref table, ref error) == SQLiteErrorCode.Ok)
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

                    SQLiteMarshal.IndexFromIntPtr(pIndex, ref index);

                    if (BestIndex(table, index) == SQLiteErrorCode.Ok)
                    {
                        SQLiteMarshal.IndexToIntPtr(index, pIndex);
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

        private SQLiteErrorCode xDisconnect(
            IntPtr pVtab
            )
        {
            try
            {
                SQLiteVirtualTable table = TableFromIntPtr(pVtab);

                if (table != null)
                {
                    if (Disconnect(table) == SQLiteErrorCode.Ok)
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
                    if (LogExceptions)
                    {
                        SQLiteLog.LogMessage(SQLiteBase.COR_E_EXCEPTION,
                            String.Format(CultureInfo.CurrentCulture,
                            "Caught exception in \"xDisconnect\" method: {0}",
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
                FreeTable(pVtab);
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private SQLiteErrorCode xDestroy(
            IntPtr pVtab
            )
        {
            try
            {
                SQLiteVirtualTable table = TableFromIntPtr(pVtab);

                if (table != null)
                {
                    if (Destroy(table) == SQLiteErrorCode.Ok)
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
                    if (LogExceptions)
                    {
                        SQLiteLog.LogMessage(SQLiteBase.COR_E_EXCEPTION,
                            String.Format(CultureInfo.CurrentCulture,
                            "Caught exception in \"xDestroy\" method: {0}",
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
                FreeTable(pVtab);
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

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

        private SQLiteErrorCode xFilter(
            IntPtr pCursor,
            int idxNum,
            IntPtr idxStr,
            int argc,
            IntPtr[] argv
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
                            SQLiteMarshal.ValueArrayFromIntPtrArray(
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

            return 1;
        }

        ///////////////////////////////////////////////////////////////////////

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

        private SQLiteErrorCode xUpdate(
            IntPtr pVtab,
            int nData,
            IntPtr apData,
            ref long rowId
            )
        {
            try
            {
                SQLiteVirtualTable table = TableFromIntPtr(pVtab);

                if (table != null)
                {
                    SQLiteValue[] values =
                        SQLiteMarshal.ValueArrayFromSizeAndIntPtr(
                            nData, apData);

                    return Update(table, values, ref rowId);
                }
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

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
                    SQLiteFunction function = null;

                    if (FindFunction(
                            table, nArg,
                            SQLiteString.StringFromUtf8IntPtr(zName),
                            ref function, ref pClientData))
                    {
                        if (function != null)
                        {
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

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////

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
        public virtual bool Declared
        {
            get { CheckDisposed(); return declared; }
            internal set { declared = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string name;
        public virtual string Name
        {
            get { CheckDisposed(); return name; }
        }

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Create(
            SQLiteConnection connection,
            IntPtr pClientData,
            string[] arguments,
            ref SQLiteVirtualTable table,
            ref string error
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Connect(
            SQLiteConnection connection,
            IntPtr pClientData,
            string[] arguments,
            ref SQLiteVirtualTable table,
            ref string error
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode BestIndex(
            SQLiteVirtualTable table,
            SQLiteIndex index
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Disconnect(
            SQLiteVirtualTable table
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Destroy(
            SQLiteVirtualTable table
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Open(
            SQLiteVirtualTable table,
            ref SQLiteVirtualTableCursor cursor
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Close(
            SQLiteVirtualTableCursor cursor
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Filter(
            SQLiteVirtualTableCursor cursor,
            int indexNumber,
            string indexString,
            SQLiteValue[] values
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Next(
            SQLiteVirtualTableCursor cursor
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract bool Eof(
            SQLiteVirtualTableCursor cursor
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Column(
            SQLiteVirtualTableCursor cursor,
            SQLiteContext context,
            int index
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode RowId(
            SQLiteVirtualTableCursor cursor,
            ref long rowId
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Update(
            SQLiteVirtualTable table,
            SQLiteValue[] values,
            ref long rowId
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Begin(
            SQLiteVirtualTable table
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Sync(
            SQLiteVirtualTable table
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Commit(
            SQLiteVirtualTable table
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Rollback(
            SQLiteVirtualTable table
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract bool FindFunction(
            SQLiteVirtualTable table,
            int argumentCount,
            string name,
            ref SQLiteFunction function,
            ref IntPtr pClientData
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Rename(
            SQLiteVirtualTable table,
            string newName
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Savepoint(
            SQLiteVirtualTable table,
            int savepoint
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Release(
            SQLiteVirtualTable table,
            int savepoint
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode RollbackTo(
            SQLiteVirtualTable table,
            int savepoint
            );
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
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

                try
                {
                    UnsafeNativeMethods.sqlite3_dispose_module(
                        ref nativeModule);
                }
                catch (Exception e)
                {
                    try
                    {
                        if (LogExceptions)
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

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~SQLiteModule()
        {
            Dispose(false);
        }
        #endregion
    }
    #endregion
}
