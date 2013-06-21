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
    /* NOT SEALED */
    public class SQLiteVirtualTableCursorEnumerable : SQLiteVirtualTableCursor
    {
        #region Private Data
        private IEnumerator enumerator;
        private bool endOfEnumerator;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
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
        public virtual bool MoveNext()
        {
            if (enumerator == null)
                return false;

            endOfEnumerator = !enumerator.MoveNext();
            return !endOfEnumerator;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual object Current
        {
            get
            {
                if (enumerator == null)
                    return null;

                return enumerator.Current;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual void Reset()
        {
            if (enumerator == null)
                return;

            enumerator.Reset();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool EndOfEnumerator
        {
            get { return endOfEnumerator; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual void Close()
        {
            if (enumerator != null)
                enumerator = null;
        }
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteModuleEnumerable Class
    /* NOT SEALED */
    public class SQLiteModuleEnumerable : SQLiteModuleNoop
    {
        #region Private Constants
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
        protected virtual SQLiteErrorCode CursorTypeMismatchError(
            SQLiteVirtualTableCursor cursor
            )
        {
            SetCursorError(cursor, "not an \"enumerable\" cursor");
            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual SQLiteErrorCode CursorEndOfEnumeratorError(
            SQLiteVirtualTableCursor cursor
            )
        {
            SetCursorError(cursor, "already hit end of enumerator");
            return SQLiteErrorCode.Error; 
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual string GetStringFromObject(
            object value
            )
        {
            if (value == null)
                return null;

            return value.ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual long GetRowIdFromObject(
            object value
            )
        {
            if (value == null)
                return 0;

            return value.GetHashCode();
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual bool CodeToEofResult(
            SQLiteErrorCode returnCode
            )
        {
            return (returnCode == SQLiteErrorCode.Ok) ? false : true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISQLiteManagedModule Members
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

        public override SQLiteErrorCode BestIndex(
            SQLiteVirtualTable table,
            SQLiteIndex index
            )
        {
            CheckDisposed();

            if (!SetDefaultEstimatedCost(index))
            {
                SetTableError(table, "failed to set default estimated cost");
                return SQLiteErrorCode.Error;
            }

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Disconnect(
            SQLiteVirtualTable table
            )
        {
            CheckDisposed();

            table.Dispose();
            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Destroy(
            SQLiteVirtualTable table
            )
        {
            CheckDisposed();

            table.Dispose();
            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

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
