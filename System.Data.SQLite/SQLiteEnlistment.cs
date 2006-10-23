/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

#if !PLATFORM_COMPACTFRAMEWORK
namespace System.Data.SQLite
{
  using System;
  using System.Data;
  using System.Data.Common;
  using System.Transactions;

  internal class SQLiteEnlistment : IEnlistmentNotification
  {
    internal SQLiteTransaction _transaction;
    internal Transaction _scope;
    internal bool _disposeConnection;

    internal SQLiteEnlistment(SQLiteConnection cnn, Transaction scope)
    {
      _transaction = cnn.BeginTransaction();
      _scope = scope;
      _disposeConnection = false;

      _scope.EnlistVolatile(this, System.Transactions.EnlistmentOptions.None);
    }

    private void Cleanup(SQLiteConnection cnn)
    {
      if (_disposeConnection)
        cnn.Dispose();

      _transaction = null;
      _scope = null;
    }

    #region IEnlistmentNotification Members

    public void Commit(Enlistment enlistment)
    {
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

    public void InDoubt(Enlistment enlistment)
    {
      enlistment.Done();
    }

    public void Prepare(PreparingEnlistment preparingEnlistment)
    {
      if (_transaction.IsValid(false) == false)
        preparingEnlistment.ForceRollback();
      else
        preparingEnlistment.Prepared();
    }

    public void Rollback(Enlistment enlistment)
    {
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