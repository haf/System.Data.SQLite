/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Joe Mistachkin (joe@mistachkin.com)
 *
 * Released to the public domain, use at your own risk!
 ********************************************************/

namespace System.Data.SQLite
{
    /// <summary>
    /// This class implements a virtual table module that does nothing.
    /// </summary>
    public class SQLiteModuleNoop : SQLiteModule /* NOT SEALED */
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class.
        /// </summary>
        /// <param name="name">
        /// The name of the module.  This parameter cannot be null.
        /// </param>
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

            return SQLiteErrorCode.Ok;
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

            return SQLiteErrorCode.Ok;
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

            return true;
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

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteManagedModule.Begin" /> method.
        /// </summary>
        /// <param name="table">
        /// See the <see cref="ISQLiteManagedModule.Begin" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteManagedModule.Begin" /> method.
        /// </returns>
        public override SQLiteErrorCode Begin(
            SQLiteVirtualTable table
            )
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteManagedModule.Sync" /> method.
        /// </summary>
        /// <param name="table">
        /// See the <see cref="ISQLiteManagedModule.Sync" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteManagedModule.Sync" /> method.
        /// </returns>
        public override SQLiteErrorCode Sync(
            SQLiteVirtualTable table
            )
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteManagedModule.Commit" /> method.
        /// </summary>
        /// <param name="table">
        /// See the <see cref="ISQLiteManagedModule.Commit" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteManagedModule.Commit" /> method.
        /// </returns>
        public override SQLiteErrorCode Commit(
            SQLiteVirtualTable table
            )
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteManagedModule.Rollback" /> method.
        /// </summary>
        /// <param name="table">
        /// See the <see cref="ISQLiteManagedModule.Rollback" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteManagedModule.Rollback" /> method.
        /// </returns>
        public override SQLiteErrorCode Rollback(
            SQLiteVirtualTable table
            )
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteManagedModule.FindFunction" /> method.
        /// </summary>
        /// <param name="table">
        /// See the <see cref="ISQLiteManagedModule.FindFunction" /> method.
        /// </param>
        /// <param name="argumentCount">
        /// See the <see cref="ISQLiteManagedModule.FindFunction" /> method.
        /// </param>
        /// <param name="name">
        /// See the <see cref="ISQLiteManagedModule.FindFunction" /> method.
        /// </param>
        /// <param name="function">
        /// See the <see cref="ISQLiteManagedModule.FindFunction" /> method.
        /// </param>
        /// <param name="pClientData">
        /// See the <see cref="ISQLiteManagedModule.FindFunction" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteManagedModule.FindFunction" /> method.
        /// </returns>
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

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteManagedModule.Savepoint" /> method.
        /// </summary>
        /// <param name="table">
        /// See the <see cref="ISQLiteManagedModule.Savepoint" /> method.
        /// </param>
        /// <param name="savepoint">
        /// See the <see cref="ISQLiteManagedModule.Savepoint" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteManagedModule.Savepoint" /> method.
        /// </returns>
        public override SQLiteErrorCode Savepoint(
            SQLiteVirtualTable table,
            int savepoint
            )
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteManagedModule.Release" /> method.
        /// </summary>
        /// <param name="table">
        /// See the <see cref="ISQLiteManagedModule.Release" /> method.
        /// </param>
        /// <param name="savepoint">
        /// See the <see cref="ISQLiteManagedModule.Release" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteManagedModule.Release" /> method.
        /// </returns>
        public override SQLiteErrorCode Release(
            SQLiteVirtualTable table,
            int savepoint
            )
        {
            CheckDisposed();

            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// See the <see cref="ISQLiteManagedModule.RollbackTo" /> method.
        /// </summary>
        /// <param name="table">
        /// See the <see cref="ISQLiteManagedModule.RollbackTo" /> method.
        /// </param>
        /// <param name="savepoint">
        /// See the <see cref="ISQLiteManagedModule.RollbackTo" /> method.
        /// </param>
        /// <returns>
        /// See the <see cref="ISQLiteManagedModule.RollbackTo" /> method.
        /// </returns>
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
}
