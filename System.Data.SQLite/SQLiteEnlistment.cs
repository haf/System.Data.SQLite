/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 *
 * Released to the public domain, use at your own risk!
 ********************************************************/

#if !PLATFORM_COMPACTFRAMEWORK
namespace System.Data.SQLite
{
    using System.Globalization;
    using System.Transactions;

  internal sealed class SQLiteEnlistment : IDisposable, IEnlistmentNotification
  {
    internal SQLiteTransaction _transaction;
    internal Transaction _scope;
    internal bool _disposeConnection;

    internal SQLiteEnlistment(
        SQLiteConnection cnn,
        Transaction scope,
        System.Data.IsolationLevel defaultIsolationLevel,
        bool throwOnUnavailable,
        bool throwOnUnsupported
        )
    {
      _transaction = cnn.BeginTransaction(GetSystemDataIsolationLevel(
          cnn, scope, defaultIsolationLevel, throwOnUnavailable,
          throwOnUnsupported));

      _scope = scope;

      _scope.EnlistVolatile(this, System.Transactions.EnlistmentOptions.None);
    }

    ///////////////////////////////////////////////////////////////////////////

    #region Private Methods
    private System.Data.IsolationLevel GetSystemDataIsolationLevel(
        SQLiteConnection connection,
        Transaction transaction,
        System.Data.IsolationLevel defaultIsolationLevel,
        bool throwOnUnavailable,
        bool throwOnUnsupported
        )
    {
        if (transaction == null)
        {
            //
            // NOTE: If neither the transaction nor connection isolation
            //       level is available, throw an exception if instructed
            //       by the caller.
            //
            if (connection != null)
                return connection.GetDefaultIsolationLevel();

            if (throwOnUnavailable)
            {
                throw new InvalidOperationException(
                    "isolation level is unavailable");
            }

            return defaultIsolationLevel;
        }

        System.Transactions.IsolationLevel isolationLevel =
            transaction.IsolationLevel;

        //
        // TODO: Are these isolation level mappings actually correct?
        //
        switch (isolationLevel)
        {
            case IsolationLevel.Unspecified:
                return System.Data.IsolationLevel.Unspecified;
            case IsolationLevel.Chaos:
                return System.Data.IsolationLevel.Chaos;
            case IsolationLevel.ReadUncommitted:
                return System.Data.IsolationLevel.ReadUncommitted;
            case IsolationLevel.ReadCommitted:
                return System.Data.IsolationLevel.ReadCommitted;
            case IsolationLevel.RepeatableRead:
                return System.Data.IsolationLevel.RepeatableRead;
            case IsolationLevel.Serializable:
                return System.Data.IsolationLevel.Serializable;
            case IsolationLevel.Snapshot:
                return System.Data.IsolationLevel.Snapshot;
        }

        //
        // NOTE: When in "strict" mode, throw an exception if the isolation
        //       level is not recognized; otherwise, fallback to the default
        //       isolation level specified by the caller.
        //
        if (throwOnUnsupported)
        {
            throw new InvalidOperationException(
                String.Format(CultureInfo.InvariantCulture,
                "unsupported isolation level {0}", isolationLevel));
        }

        return defaultIsolationLevel;
    }

    ///////////////////////////////////////////////////////////////////////////

    private void Cleanup(SQLiteConnection cnn)
    {
        if (_disposeConnection)
            cnn.Dispose();

        _transaction = null;
        _scope = null;
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region IDisposable Members
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region IDisposable "Pattern" Members
    private bool disposed;
    private void CheckDisposed() /* throw */
    {
#if THROW_ON_DISPOSED
        if (disposed)
            throw new ObjectDisposedException(typeof(SQLiteEnlistment).Name);
#endif
    }

    ///////////////////////////////////////////////////////////////////////////

    private /* protected virtual */ void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                ////////////////////////////////////
                // dispose managed resources here...
                ////////////////////////////////////

                if (_transaction != null)
                {
                    _transaction.Dispose();
                    _transaction = null;
                }

                if (_scope != null)
                {
                    // _scope.Dispose(); // NOTE: Not "owned" by us.
                    _scope = null;
                }
            }

            //////////////////////////////////////
            // release unmanaged resources here...
            //////////////////////////////////////

            disposed = true;
        }
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Destructor
    ~SQLiteEnlistment()
    {
        Dispose(false);
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region IEnlistmentNotification Members
    public void Commit(Enlistment enlistment)
    {
      CheckDisposed();

      SQLiteConnection cnn = _transaction.Connection;
      cnn._enlistment = null;

      try
      {
        _transaction.IsValid(true);
        _transaction.Connection._transactionLevel = 1;
        _transaction.Commit();

        enlistment.Done();
      }
      finally
      {
        Cleanup(cnn);
      }
    }

    ///////////////////////////////////////////////////////////////////////////

    public void InDoubt(Enlistment enlistment)
    {
      CheckDisposed();
      enlistment.Done();
    }

    ///////////////////////////////////////////////////////////////////////////

    public void Prepare(PreparingEnlistment preparingEnlistment)
    {
      CheckDisposed();

      if (_transaction.IsValid(false) == false)
        preparingEnlistment.ForceRollback();
      else
        preparingEnlistment.Prepared();
    }

    ///////////////////////////////////////////////////////////////////////////

    public void Rollback(Enlistment enlistment)
    {
      CheckDisposed();

      SQLiteConnection cnn = _transaction.Connection;
      cnn._enlistment = null;

      try
      {
        _transaction.Rollback();
        enlistment.Done();
      }
      finally
      {
        Cleanup(cnn);
      }
    }
    #endregion
  }
}
#endif // !PLATFORM_COMPACT_FRAMEWORK
