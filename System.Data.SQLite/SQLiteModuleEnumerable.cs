/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Joe Mistachkin (joe@mistachkin.com)
 *
 * Released to the public domain, use at your own risk!
 ********************************************************/

using System.Collections;
using System.Globalization;

namespace System.Data.SQLite
{
    #region SQLiteVirtualTableCursorEnumerable Class
    /// <summary>
    /// This class represents a virtual table cursor to be used with the
    /// <see cref="SQLiteModuleEnumerable" /> class.  It is not sealed and may
    /// be used as the base class for any user-defined virtual table cursor
    /// class that wraps an <see cref="IEnumerator" /> object instance.
    /// </summary>
    public class SQLiteVirtualTableCursorEnumerable :
            SQLiteVirtualTableCursor /* NOT SEALED */
    {
        #region Private Data
        /// <summary>
        /// The <see cref="IEnumerator" /> instance provided when this cursor
        /// was created.
        /// </summary>
        private IEnumerator enumerator;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This value will be non-zero if false has been returned from the
        /// <see cref="IEnumerator.MoveNext"/> method.
        /// </summary>
        private bool endOfEnumerator;
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
        /// <param name="enumerator">
        /// The <see cref="IEnumerator" /> instance to expose as a virtual
        /// table cursor.
        /// </param>
        public SQLiteVirtualTableCursorEnumerable(
            SQLiteVirtualTable table,
            IEnumerator enumerator
            )
            : base(table)
        {
            this.enumerator = enumerator;
            this.endOfEnumerator = true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Members
        /// <summary>
        /// Advances to the next row of the virtual table cursor using the
        /// <see cref="IEnumerator.MoveNext" /> method of the
        /// <see cref="IEnumerator" /> object instance.
        /// </summary>
        /// <returns>
        /// Non-zero if the current row is valid; zero otherwise.  If zero is
        /// returned, no further rows are available.
        /// </returns>
        public virtual bool MoveNext()
        {
            CheckDisposed();

            if (enumerator == null)
                return false;

            endOfEnumerator = !enumerator.MoveNext();
            return !endOfEnumerator;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns the value for the current row of the virtual table cursor
        /// using the <see cref="IEnumerator.Current" /> property of the
        /// <see cref="IEnumerator" /> object instance.
        /// </summary>
        public virtual object Current
        {
            get
            {
                CheckDisposed();

                if (enumerator == null)
                    return null;

                return enumerator.Current;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resets the virtual table cursor position, also invalidating the
        /// current row, using the <see cref="IEnumerator.Reset" /> method of
        /// the <see cref="IEnumerator" /> object instance.
        /// </summary>
        public virtual void Reset()
        {
            CheckDisposed();

            if (enumerator == null)
                return;

            enumerator.Reset();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns non-zero if the end of the virtual table cursor has been
        /// seen (i.e. no more rows are available, including the current one).
        /// </summary>
        public virtual bool EndOfEnumerator
        {
            get { CheckDisposed(); return endOfEnumerator; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Closes the virtual table cursor.
        /// </summary>
        public virtual void Close()
        {
            // CheckDisposed();

            if (enumerator != null)
                enumerator = null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/> if this object
        /// instance has been disposed.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed)
            {
                throw new ObjectDisposedException(
                    typeof(SQLiteVirtualTableCursorEnumerable).Name);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Disposes of this object instance.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <see cref="IDisposable.Dispose" /> method.  Zero if this method is
        /// being called from the finalizer.
        /// </param>
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

                    Close();

                    disposed = true;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteModuleEnumerable Class
    /// <summary>
    /// This class implements a virtual table module that exposes an
    /// IEnumerable instance as a read-only virtual table.  It is not sealed
    /// and may be used as the base class for any user-defined virtual table
    /// class that wraps an <see cref="IEnumerable" /> object instance.
    /// </summary>
    public class SQLiteModuleEnumerable : SQLiteModuleNoop /* NOT SEALED */
    {
        #region Private Constants
        /// <summary>
        /// The CREATE TABLE statement used to declare the schema for the
        /// virtual table.
        /// </summary>
        private static readonly string declareSql = String.Format(
            CultureInfo.CurrentCulture, "CREATE TABLE {0}(x);",
            typeof(SQLiteModuleEnumerable).Name);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The <see cref="IEnumerable" /> instance containing the backing data
        /// for the virtual table.
        /// </summary>
        private IEnumerable enumerable;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class.
        /// </summary>
        /// <param name="name">
        /// The name of the module.  This parameter cannot be null.
        /// </param>
        /// <param name="enumerable">
        /// The <see cref="IEnumerable" /> instance to expose as a virtual
        /// table.  This parameter cannot be null.
        /// </param>
        public SQLiteModuleEnumerable(
            string name,
            IEnumerable enumerable
            )
            : base(name)
        {
            if (enumerable == null)
                throw new ArgumentNullException("enumerable");

            this.enumerable = enumerable;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Methods
        /// <summary>
        /// Sets the table error message to one that indicates the virtual
        /// table cursor is of the wrong type.
        /// </summary>
        /// <param name="cursor">
        /// The <see cref="SQLiteVirtualTableCursor" /> object instance.
        /// </param>
        /// <returns>
        /// The value of <see cref="SQLiteErrorCode.Error"/>.
        /// </returns>
        protected virtual SQLiteErrorCode CursorTypeMismatchError(
            SQLiteVirtualTableCursor cursor
            )
        {
            SetCursorError(cursor, "not an \"enumerable\" cursor");
            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the table error message to one that indicates the virtual
        /// table cursor has no current row.
        /// </summary>
        /// <param name="cursor">
        /// The <see cref="SQLiteVirtualTableCursor" /> object instance.
        /// </param>
        /// <returns>
        /// The value of <see cref="SQLiteErrorCode.Error"/>.
        /// </returns>
        protected virtual SQLiteErrorCode CursorEndOfEnumeratorError(
            SQLiteVirtualTableCursor cursor
            )
        {
            SetCursorError(cursor, "already hit end of enumerator");
            return SQLiteErrorCode.Error; 
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines the string to return as the column value for the object
        /// instance value.
        /// </summary>
        /// <param name="value">
        /// The object instance to return a string representation for.
        /// </param>
        /// <returns>
        /// The string representation of the specified object instance or null
        /// upon failure.
        /// </returns>
        protected virtual string GetStringFromObject(
            object value
            )
        {
            if (value == null)
                return null;

            return value.ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines the unique row identifier for the object instance value.
        /// </summary>
        /// <param name="value">
        /// The object instance to return a unique row identifier for.
        /// </param>
        /// <returns>
        /// The unique row identifier or zero upon failure.
        /// </returns>
        protected virtual long GetRowIdFromObject(
            object value
            )
        {
            if (value == null)
                return 0;

            return value.GetHashCode();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts a <see cref="SQLiteErrorCode" /> into a boolean return
        /// value for use with the <see cref="ISQLiteManagedModule.Eof" />
        /// method.
        /// </summary>
        /// <param name="returnCode">
        /// The <see cref="SQLiteErrorCode" /> value to convert.
        /// </param>
        /// <returns>
        /// The <see cref="Boolean" /> value.
        /// </returns>
        protected virtual bool CodeToEofResult(
            SQLiteErrorCode returnCode
            )
        {
            return (returnCode == SQLiteErrorCode.Ok) ? false : true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISQLiteManagedModule Members
        /// <summary>
        /// See the <see cref="ISQLiteManagedModule.Create" /> method.
        /// </summary>
        /// <param name="connection">
        /// See the <see cref="ISQLiteManagedModule.Create" /> method.
        /// </param>
        /// <param name="pClientData">
        /// See the <see cref="ISQLiteManagedModule.Create" /> method.
        /// </param>
        /// <param name="arguments">
        /// See the <see cref="ISQLiteManagedModule.Create" /> method.
        /// </param>
        /// <param name="table">
        /// See the <see cref="ISQLiteManagedModule.Create" /> method.
        /// </param>
        /// <param name="error">
        /// See the <see cref="ISQLiteManagedModule.Create" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteManagedModule.Create" /> method.
        /// </returns>
        public override SQLiteErrorCode Create(
            SQLiteConnection connection,
            IntPtr pClientData,
            string[] arguments,
            ref SQLiteVirtualTable table,
            ref string error
            )
        {
            CheckDisposed();

            if (DeclareTable(
                    connection, declareSql,
                    ref error) == SQLiteErrorCode.Ok)
            {
                table = new SQLiteVirtualTable(arguments);
                return SQLiteErrorCode.Ok;
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteManagedModule.Connect" /> method.
        /// </summary>
        /// <param name="connection">
        /// See the <see cref="ISQLiteManagedModule.Connect" /> method.
        /// </param>
        /// <param name="pClientData">
        /// See the <see cref="ISQLiteManagedModule.Connect" /> method.
        /// </param>
        /// <param name="arguments">
        /// See the <see cref="ISQLiteManagedModule.Connect" /> method.
        /// </param>
        /// <param name="table">
        /// See the <see cref="ISQLiteManagedModule.Connect" /> method.
        /// </param>
        /// <param name="error">
        /// See the <see cref="ISQLiteManagedModule.Connect" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteManagedModule.Connect" /> method.
        /// </returns>
        public override SQLiteErrorCode Connect(
            SQLiteConnection connection,
            IntPtr pClientData,
            string[] arguments,
            ref SQLiteVirtualTable table,
            ref string error
            )
        {
            CheckDisposed();

            if (DeclareTable(
                    connection, declareSql,
                    ref error) == SQLiteErrorCode.Ok)
            {
                table = new SQLiteVirtualTable(arguments);
                return SQLiteErrorCode.Ok;
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteManagedModule.BestIndex" /> method.
        /// </summary>
        /// <param name="table">
        /// See the <see cref="ISQLiteManagedModule.BestIndex" /> method.
        /// </param>
        /// <param name="index">
        /// See the <see cref="ISQLiteManagedModule.BestIndex" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteManagedModule.BestIndex" /> method.
        /// </returns>
        public override SQLiteErrorCode BestIndex(
            SQLiteVirtualTable table,
            SQLiteIndex index
            )
        {
            CheckDisposed();

            if (!SetEstimatedCost(index))
            {
                SetTableError(table, "failed to set estimated cost");
                return SQLiteErrorCode.Error;
            }

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteManagedModule.Disconnect" /> method.
        /// </summary>
        /// <param name="table">
        /// See the <see cref="ISQLiteManagedModule.Disconnect" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteManagedModule.Disconnect" /> method.
        /// </returns>
        public override SQLiteErrorCode Disconnect(
            SQLiteVirtualTable table
            )
        {
            CheckDisposed();

            table.Dispose();
            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteManagedModule.Destroy" /> method.
        /// </summary>
        /// <param name="table">
        /// See the <see cref="ISQLiteManagedModule.Destroy" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteManagedModule.Destroy" /> method.
        /// </returns>
        public override SQLiteErrorCode Destroy(
            SQLiteVirtualTable table
            )
        {
            CheckDisposed();

            table.Dispose();
            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteManagedModule.Open" /> method.
        /// </summary>
        /// <param name="table">
        /// See the <see cref="ISQLiteManagedModule.Open" /> method.
        /// </param>
        /// <param name="cursor">
        /// See the <see cref="ISQLiteManagedModule.Open" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteManagedModule.Open" /> method.
        /// </returns>
        public override SQLiteErrorCode Open(
            SQLiteVirtualTable table,
            ref SQLiteVirtualTableCursor cursor
            )
        {
            CheckDisposed();

            cursor = new SQLiteVirtualTableCursorEnumerable(
                table, enumerable.GetEnumerator());

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteManagedModule.Close" /> method.
        /// </summary>
        /// <param name="cursor">
        /// See the <see cref="ISQLiteManagedModule.Close" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteManagedModule.Close" /> method.
        /// </returns>
        public override SQLiteErrorCode Close(
            SQLiteVirtualTableCursor cursor
            )
        {
            CheckDisposed();

            SQLiteVirtualTableCursorEnumerable enumerableCursor =
                cursor as SQLiteVirtualTableCursorEnumerable;

            if (enumerableCursor == null)
                return CursorTypeMismatchError(cursor);

            enumerableCursor.Close();
            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteManagedModule.Filter" /> method.
        /// </summary>
        /// <param name="cursor">
        /// See the <see cref="ISQLiteManagedModule.Filter" /> method.
        /// </param>
        /// <param name="indexNumber">
        /// See the <see cref="ISQLiteManagedModule.Filter" /> method.
        /// </param>
        /// <param name="indexString">
        /// See the <see cref="ISQLiteManagedModule.Filter" /> method.
        /// </param>
        /// <param name="values">
        /// See the <see cref="ISQLiteManagedModule.Filter" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteManagedModule.Filter" /> method.
        /// </returns>
        public override SQLiteErrorCode Filter(
            SQLiteVirtualTableCursor cursor,
            int indexNumber,
            string indexString,
            SQLiteValue[] values
            )
        {
            CheckDisposed();

            SQLiteVirtualTableCursorEnumerable enumerableCursor =
                cursor as SQLiteVirtualTableCursorEnumerable;

            if (enumerableCursor == null)
                return CursorTypeMismatchError(cursor);

            enumerableCursor.Filter(indexNumber, indexString, values);
            enumerableCursor.Reset(); /* NO RESULT */
            enumerableCursor.MoveNext(); /* IGNORED */

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteManagedModule.Next" /> method.
        /// </summary>
        /// <param name="cursor">
        /// See the <see cref="ISQLiteManagedModule.Next" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteManagedModule.Next" /> method.
        /// </returns>
        public override SQLiteErrorCode Next(
            SQLiteVirtualTableCursor cursor
            )
        {
            CheckDisposed();

            SQLiteVirtualTableCursorEnumerable enumerableCursor =
                cursor as SQLiteVirtualTableCursorEnumerable;

            if (enumerableCursor == null)
                return CursorTypeMismatchError(cursor);

            if (enumerableCursor.EndOfEnumerator)
                return CursorEndOfEnumeratorError(cursor);

            enumerableCursor.MoveNext(); /* IGNORED */
            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteManagedModule.Eof" /> method.
        /// </summary>
        /// <param name="cursor">
        /// See the <see cref="ISQLiteManagedModule.Eof" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteManagedModule.Eof" /> method.
        /// </returns>
        public override bool Eof(
            SQLiteVirtualTableCursor cursor
            )
        {
            CheckDisposed();

            SQLiteVirtualTableCursorEnumerable enumerableCursor =
                cursor as SQLiteVirtualTableCursorEnumerable;

            if (enumerableCursor == null)
                return CodeToEofResult(CursorTypeMismatchError(cursor));

            return enumerableCursor.EndOfEnumerator;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteManagedModule.Column" /> method.
        /// </summary>
        /// <param name="cursor">
        /// See the <see cref="ISQLiteManagedModule.Column" /> method.
        /// </param>
        /// <param name="context">
        /// See the <see cref="ISQLiteManagedModule.Column" /> method.
        /// </param>
        /// <param name="index">
        /// See the <see cref="ISQLiteManagedModule.Column" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteManagedModule.Column" /> method.
        /// </returns>
        public override SQLiteErrorCode Column(
            SQLiteVirtualTableCursor cursor,
            SQLiteContext context,
            int index
            )
        {
            CheckDisposed();

            SQLiteVirtualTableCursorEnumerable enumerableCursor =
                cursor as SQLiteVirtualTableCursorEnumerable;

            if (enumerableCursor == null)
                return CursorTypeMismatchError(cursor);

            if (enumerableCursor.EndOfEnumerator)
                return CursorEndOfEnumeratorError(cursor);

            object current = enumerableCursor.Current;

            if (current != null)
                context.SetString(GetStringFromObject(current));
            else
                context.SetNull();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteManagedModule.RowId" /> method.
        /// </summary>
        /// <param name="cursor">
        /// See the <see cref="ISQLiteManagedModule.RowId" /> method.
        /// </param>
        /// <param name="rowId">
        /// See the <see cref="ISQLiteManagedModule.RowId" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteManagedModule.RowId" /> method.
        /// </returns>
        public override SQLiteErrorCode RowId(
            SQLiteVirtualTableCursor cursor,
            ref long rowId
            )
        {
            CheckDisposed();

            SQLiteVirtualTableCursorEnumerable enumerableCursor =
                cursor as SQLiteVirtualTableCursorEnumerable;

            if (enumerableCursor == null)
                return CursorTypeMismatchError(cursor);

            if (enumerableCursor.EndOfEnumerator)
                return CursorEndOfEnumeratorError(cursor);

            object current = enumerableCursor.Current;

            rowId = GetRowIdFromObject(current);
            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteManagedModule.Update" /> method.
        /// </summary>
        /// <param name="table">
        /// See the <see cref="ISQLiteManagedModule.Update" /> method.
        /// </param>
        /// <param name="values">
        /// See the <see cref="ISQLiteManagedModule.Update" /> method.
        /// </param>
        /// <param name="rowId">
        /// See the <see cref="ISQLiteManagedModule.Update" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteManagedModule.Update" /> method.
        /// </returns>
        public override SQLiteErrorCode Update(
            SQLiteVirtualTable table,
            SQLiteValue[] values,
            ref long rowId
            )
        {
            CheckDisposed();

            SetTableError(table, String.Format(CultureInfo.CurrentCulture,
                "virtual table \"{0}\" is read-only", table.TableName));

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteManagedModule.Rename" /> method.
        /// </summary>
        /// <param name="table">
        /// See the <see cref="ISQLiteManagedModule.Rename" /> method.
        /// </param>
        /// <param name="newName">
        /// See the <see cref="ISQLiteManagedModule.Rename" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteManagedModule.Rename" /> method.
        /// </returns>
        public override SQLiteErrorCode Rename(
            SQLiteVirtualTable table,
            string newName
            )
        {
            CheckDisposed();

            if (!table.Rename(newName))
            {
                SetTableError(table, String.Format(CultureInfo.CurrentCulture,
                    "failed to rename virtual table from \"{0}\" to \"{1}\"",
                    table.TableName, newName));

                return SQLiteErrorCode.Error;
            }

            return SQLiteErrorCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/> if this object
        /// instance has been disposed.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed)
            {
                throw new ObjectDisposedException(
                    typeof(SQLiteModuleNoop).Name);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Disposes of this object instance.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <see cref="IDisposable.Dispose" /> method.  Zero if this method is
        /// being called from the finalizer.
        /// </param>
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

                    disposed = true;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
        #endregion
    }
    #endregion
}
