/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Joe Mistachkin (joe@mistachkin.com)
 *
 * Released to the public domain, use at your own risk!
 ********************************************************/

namespace System.Data.SQLite
{
    public class SQLiteModuleNoop : SQLiteModule
    {
        #region Public Constructors
        public SQLiteModuleNoop(
            string name
            )
            : base(name)
        {
            // do nothing.
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

            return SQLiteErrorCode.Ok;
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

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode BestIndex(
            SQLiteVirtualTable table,
            SQLiteIndex index
            )
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Disconnect(
            SQLiteVirtualTable table
            )
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Destroy(
            SQLiteVirtualTable table
            )
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Open(
            SQLiteVirtualTable table,
            ref SQLiteVirtualTableCursor cursor
            )
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Close(
            SQLiteVirtualTableCursor cursor
            )
        {
            CheckDisposed();

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

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Next(
            SQLiteVirtualTableCursor cursor
            )
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override bool Eof(
            SQLiteVirtualTableCursor cursor
            )
        {
            CheckDisposed();

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Column(
            SQLiteVirtualTableCursor cursor,
            SQLiteContext context,
            int index
            )
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode RowId(
            SQLiteVirtualTableCursor cursor,
            ref long rowId
            )
        {
            CheckDisposed();

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

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Begin(
            SQLiteVirtualTable table
            )
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Sync(
            SQLiteVirtualTable table
            )
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Commit(
            SQLiteVirtualTable table
            )
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Rollback(
            SQLiteVirtualTable table
            )
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override bool FindFunction(
            SQLiteVirtualTable table,
            int argumentCount,
            string name,
            ref SQLiteFunction function,
            ref IntPtr pClientData
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Rename(
            SQLiteVirtualTable table,
            string newName
            )
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Savepoint(
            SQLiteVirtualTable table,
            int savepoint
            )
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Release(
            SQLiteVirtualTable table,
            int savepoint
            )
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode RollbackTo(
            SQLiteVirtualTable table,
            int savepoint
            )
        {
            CheckDisposed();

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
}
