/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Joe Mistachkin (joe@mistachkin.com)
 *
 * Released to the public domain, use at your own risk!
 ********************************************************/

namespace System.Data.SQLite
{
    public class SQLiteModuleNoop : SQLiteModuleBase
    {
        #region Public Constructors
        public SQLiteModuleNoop(string name)
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
            string[] argv,
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
            string[] argv,
            ref string error
            )
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode BestIndex(
            SQLiteIndex index
            )
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Disconnect()
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Destroy()
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Open(
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
            int idxNum,
            string idxStr,
            SQLiteValue[] argv
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
            SQLiteValue[] values,
            ref long rowId
            )
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Begin()
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Sync()
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Commit()
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Rollback()
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override bool FindFunction(
            int nArg,
            string zName,
            ref SQLiteFunction function,
            ref IntPtr pClientData
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Rename(
            string zNew
            )
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Savepoint(
            int iSavepoint
            )
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Release(
            int iSavepoint
            )
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode RollbackTo(
            int iSavepoint
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
                throw new ObjectDisposedException(typeof(SQLiteModuleNoop).Name);
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
