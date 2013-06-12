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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode BestIndex(
            SQLiteIndex index
            )
        {
            CheckDisposed();
            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Disconnect()
        {
            CheckDisposed();
            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Destroy()
        {
            CheckDisposed();
            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Open(
            ref SQLiteVirtualTableCursor cursor
            )
        {
            CheckDisposed();
            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Close(
            SQLiteVirtualTableCursor cursor
            )
        {
            CheckDisposed();
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Next(
            SQLiteVirtualTableCursor cursor
            )
        {
            CheckDisposed();
            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Eof(
            SQLiteVirtualTableCursor cursor
            )
        {
            CheckDisposed();
            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Column(
            SQLiteVirtualTableCursor cursor,
            SQLiteContext context,
            int index
            )
        {
            CheckDisposed();
            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode RowId(
            SQLiteVirtualTableCursor cursor,
            ref long rowId
            )
        {
            CheckDisposed();
            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Update(
            SQLiteValue[] values,
            ref long rowId
            )
        {
            CheckDisposed();
            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Begin()
        {
            CheckDisposed();
            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Sync()
        {
            CheckDisposed();
            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Commit()
        {
            CheckDisposed();
            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Rollback()
        {
            CheckDisposed();
            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode FindFunction(
            string zName,
            ref SQLiteFunction function,
            object[] args
            )
        {
            CheckDisposed();
            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Rename(
            string zNew
            )
        {
            CheckDisposed();
            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Savepoint(
            int iSavepoint
            )
        {
            CheckDisposed();
            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode Release(
            int iSavepoint
            )
        {
            CheckDisposed();
            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public override SQLiteErrorCode RollbackTo(
            int iSavepoint
            )
        {
            CheckDisposed();
            throw new NotImplementedException();
        }
        #endregion
    }
}
