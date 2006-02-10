﻿/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

namespace System.Data.SQLite
{
  using System;
  using System.Data;
  using System.Data.Common;
  using System.Collections.Generic;

  /// <summary>
  /// SQLite implementation of DbCommand.
  /// </summary>
  public sealed class SQLiteCommand : DbCommand, ICloneable
  {
    /// <summary>
    /// The command text this command is based on
    /// </summary>
    private string _commandText;
    /// <summary>
    /// The connection the command is associated with
    /// </summary>
    private SQLiteConnection _cnn;
    /// <summary>
    /// Indicates whether or not a DataReader is active on the command.
    /// </summary>
    private SQLiteDataReader _activeReader;
    /// <summary>
    /// The timeout for the command, kludged because SQLite doesn't support per-command timeout values
    /// </summary>
    internal int _commandTimeout;
    /// <summary>
    /// Designer support
    /// </summary>
    private bool _designTimeVisible;
    /// <summary>
    /// Used by DbDataAdapter to determine updating behavior
    /// </summary>
    private UpdateRowSource _updateRowSource;
    /// <summary>
    /// The collection of parameters for the command
    /// </summary>
    private SQLiteParameterCollection _parameterCollection;
    /// <summary>
    /// The SQL command text, broken into individual SQL statements as they are executed
    /// </summary>
    internal List<SQLiteStatement> _statementList;
    /// <summary>
    /// Unprocessed SQL text that has not been executed
    /// </summary>
    internal string _remainingText;
    /// <summary>
    /// Transaction associated with this command
    /// </summary>
    private SQLiteTransaction _transaction;

    ///<overloads>
    /// Constructs a new SQLiteCommand
    /// </overloads>
    /// <summary>
    /// Default constructor
    /// </summary>
    public SQLiteCommand()
    {
      Initialize(null, null);
    }

    /// <summary>
    /// Initializes the command with the given command text
    /// </summary>
    /// <param name="commandText">The SQL command text</param>
    public SQLiteCommand(string commandText)
    {
      Initialize(commandText, null);
    }

    /// <summary>
    /// Initializes the command with the given SQL command text and attach the command to the specified
    /// connection.
    /// </summary>
    /// <param name="commandText">The SQL command text</param>
    /// <param name="cnn">The connection to associate with the command</param>
    public SQLiteCommand(string commandText, SQLiteConnection cnn)
    {
      Initialize(commandText, cnn);
    }

    /// <summary>
    /// Initializes the command and associates it with the specified connection.
    /// </summary>
    /// <param name="cnn">The connection to associate with the command</param>
    public SQLiteCommand(SQLiteConnection cnn)
    {
      Initialize(null, cnn);
    }

    /// <summary>
    /// Initializes a command with the given SQL, connection and transaction
    /// </summary>
    /// <param name="commandText">The SQL command text</param>
    /// <param name="cnn">The connection to associate with the command</param>
    /// <param name="trans">The transaction the command should be associated with</param>
    public SQLiteCommand(string commandText, SQLiteConnection cnn, SQLiteTransaction trans)
    {
      Initialize(commandText, cnn);
      Transaction = trans;
    }

    /// <summary>
    /// Initializes the command class
    /// </summary>
    /// <param name="strSql">The SQL command text</param>
    /// <param name="cnn">A connection to associate with the command</param>
    private void Initialize(string strSql, SQLiteConnection cnn)
    {
      _statementList = null;
      _activeReader = null;
      _commandTimeout = 30;
      _parameterCollection = new SQLiteParameterCollection(this);
      _designTimeVisible = true;
      _updateRowSource = UpdateRowSource.FirstReturnedRecord;
      _transaction = null;

      if (strSql != null)
        CommandText = strSql;

      if (cnn != null)
        DbConnection = cnn;
    }

    /// <summary>
    /// Disposes of the command and clears all member variables
    /// </summary>
    /// <param name="disposing">Whether or not the class is being explicitly or implicitly disposed</param>
    protected override void Dispose(bool disposing)
    {
      base.Dispose(disposing);

      // If a reader is active on this command, don't destroy it completely
      if (_activeReader != null)
      {
        _activeReader._disposeCommand = true;
        return;
      }

      Connection = null;
      _parameterCollection.Clear();
      _commandText = null;
    }

    /// <summary>
    /// Clears and destroys all statements currently prepared
    /// </summary>
    internal void ClearCommands()
    {
      if (_statementList == null) return;

      int x = _statementList.Count;
      for (int n = 0; n < x; n++)
        _statementList[n].Dispose();

      _statementList = null;

      _parameterCollection.Unbind();
    }

    /// <summary>
    /// Builds an array of prepared statements for each complete SQL statement in the command text
    /// </summary>
    internal SQLiteStatement BuildNextCommand()
    {
      SQLiteStatement stmt;

      try
      {
        if (_statementList == null)
          _remainingText = _commandText;

        stmt = _cnn._sql.Prepare(_remainingText, (_statementList == null) ? null : _statementList[_statementList.Count - 1], out _remainingText);
        if (stmt != null)
        {
          stmt._command = this;
          if (_statementList == null)
            _statementList = new List<SQLiteStatement>();

          _statementList.Add(stmt);
          _parameterCollection.MapParameters(stmt);
          stmt.BindParameters();
        }        
        return stmt;
      }
      catch (Exception)
      {
        ClearCommands();
        throw;
      }
    }

    internal SQLiteStatement GetStatement(int index)
    {
      // Haven't built any statements yet
      if (_statementList == null) return BuildNextCommand();

      // If we're at the last built statement and want the next unbuilt statement, then build it
      if (index == _statementList.Count)
      {
        if (String.IsNullOrEmpty(_remainingText) == false) return BuildNextCommand();
        else return null; // No more commands
      }

      SQLiteStatement stmt = _statementList[index];
      stmt.BindParameters();

      return stmt;
    }

    /// <summary>
    /// Not implemented
    /// </summary>
    public override void Cancel()
    {
    }

    /// <summary>
    /// The SQL command text associated with the command
    /// </summary>
    public override string CommandText
    {
      get
      {
        return _commandText;
      }
      set
      {
        if (_commandText == value) return;

        if (_activeReader != null)
        {
          throw new InvalidOperationException("Cannot set CommandText while a DataReader is active");
        }

        ClearCommands();
        _commandText = value;

        if (_cnn == null) return;
      }
    }

    /// <summary>
    /// The amount of time to wait for the connection to become available before erroring out
    /// </summary>
    public override int CommandTimeout
    {
      get
      {
        return _commandTimeout;
      }
      set
      {
        _commandTimeout = value;
      }
    }

    /// <summary>
    /// The type of the command.  SQLite only supports CommandType.Text
    /// </summary>
    public override CommandType CommandType
    {
      get
      {
        return CommandType.Text;
      }
      set
      {
        if (value != CommandType.Text)
        {
          throw new NotSupportedException();
        }
      }
    }

    /// <summary>
    /// Forwards to the local CreateParameter() function
    /// </summary>
    /// <returns></returns>
    protected override DbParameter CreateDbParameter()
    {
      return CreateParameter();
    }

    /// <summary>
    /// Create a new parameter
    /// </summary>
    /// <returns></returns>
    public new SQLiteParameter CreateParameter()
    {
      return new SQLiteParameter();
    }

    /// <summary>
    /// The connection associated with this command
    /// </summary>
    public new SQLiteConnection Connection
    {
      get { return _cnn; }
      set
      {
        if (_activeReader != null)
          throw new InvalidOperationException("Cannot set Connection while a DataReader is active");

        if (_cnn != null)
        {
          ClearCommands();
          _cnn._commandList.Remove(this);
        }

        _cnn = value;

        if (_cnn != null)
          _cnn._commandList.Add(this);
      }
    }

    /// <summary>
    /// Forwards to the local Connection property
    /// </summary>
    protected override DbConnection DbConnection
    {
      get
      {
        return Connection;
      }
      set
      {
        Connection = (SQLiteConnection)value;
      }
    }

    /// <summary>
    /// Returns the SQLiteParameterCollection for the given command
    /// </summary>
    public new SQLiteParameterCollection Parameters
    {
      get { return _parameterCollection; }
    }

    /// <summary>
    /// Forwards to the local Parameters property
    /// </summary>
    protected override DbParameterCollection DbParameterCollection
    {
      get
      {
        return Parameters;
      }
    }

    /// <summary>
    /// The transaction associated with this command.  SQLite only supports one transaction per connection, so this property forwards to the
    /// command's underlying connection.
    /// </summary>
    public new SQLiteTransaction Transaction
    {
      get { return _transaction; }
      set
      {
        if (_cnn != null)
        {
          if (_activeReader != null)
            throw new InvalidOperationException("Cannot set Transaction while a DataReader is active");

          if (value != null)
          {
            if (value._cnn != _cnn)
              throw new ArgumentException("Transaction is not associated with the command's connection");
          }
          _transaction = value;
        }
        else if (value != null)
          throw new ArgumentOutOfRangeException("SQLiteTransaction", "Not associated with a connection");
      }
    }

    /// <summary>
    /// Forwards to the local Transaction property
    /// </summary>
    protected override DbTransaction DbTransaction
    {
      get
      {
        return Transaction;
      }
      set
      {
        Transaction = (SQLiteTransaction)value;
      }
    }

    /// <summary>
    /// This function ensures there are no active readers, that we have a valid connection,
    /// that the connection is open, that all statements are prepared and all parameters are assigned
    /// in preparation for allocating a data reader.
    /// </summary>
    private void InitializeForReader()
    {
      if (_activeReader != null)
        throw new InvalidOperationException("DataReader already active on this command");

      if (_cnn == null)
        throw new InvalidOperationException("No connection associated with this command");

      if (_cnn.State != ConnectionState.Open)
        throw new InvalidOperationException("Database is not open");

      // Map all parameters for statements already built
      _parameterCollection.MapParameters(null);

      // Set the default command timeout
      _cnn._sql.SetTimeout(_commandTimeout * 1000);
    }

    /// <summary>
    /// Creates a new SQLiteDataReader to execute/iterate the array of SQLite prepared statements
    /// </summary>
    /// <param name="behavior">The behavior the data reader should adopt</param>
    /// <returns>Returns a SQLiteDataReader object</returns>
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
      return ExecuteReader(behavior);
    }

    /// <summary>
    /// Overrides the default behavior to return a SQLiteDataReader specialization class
    /// </summary>
    /// <param name="behavior">The flags to be associated with the reader</param>
    /// <returns>A SQLiteDataReader</returns>
    public new SQLiteDataReader ExecuteReader(CommandBehavior behavior)
    {
      InitializeForReader();

      SQLiteDataReader rd = new SQLiteDataReader(this, behavior);
      _activeReader = rd;

      return rd;
    }

    /// <summary>
    /// Overrides the default behavior of DbDataReader to return a specialized SQLiteDataReader class
    /// </summary>
    /// <returns>A SQLiteDataReader</returns>
    public new SQLiteDataReader ExecuteReader()
    {
      return ExecuteReader(CommandBehavior.Default);
    }

    /// <summary>
    /// Called by the SQLiteDataReader when the data reader is closed.
    /// </summary>
    internal void ClearDataReader()
    {
      _activeReader = null;
    }

    /// <summary>
    /// Execute the command and return the number of rows inserted/updated affected by it.
    /// </summary>
    /// <returns></returns>
    public override int ExecuteNonQuery()
    {
      InitializeForReader();

      int nAffected = 0;
      int x = 0;
      SQLiteStatement stmt;

      for(;;)
      {
        stmt = GetStatement(x);
        x++;
        if (stmt == null) break;

        _cnn._sql.Step(stmt);
        nAffected += _cnn._sql.Changes;
        _cnn._sql.Reset(stmt);
      }

      return nAffected;
    }

    /// <summary>
    /// Execute the command and return the first column of the first row of the resultset
    /// (if present), or null if no resultset was returned.
    /// </summary>
    /// <returns>The first column of the first row of the first resultset from the query</returns>
    public override object ExecuteScalar()
    {
      InitializeForReader();

      int x = 0;
      object ret = null;
      SQLiteType typ = new SQLiteType();
      SQLiteStatement stmt;

      // We step through every statement in the command, but only grab the first row of the first resultset.
      // We keep going even after obtaining it.
      for (;;)
      {
        stmt = GetStatement(x);
        x++;
        if (stmt == null) break;

        if (_cnn._sql.Step(stmt) == true && ret == null)
        {
          ret = _cnn._sql.GetValue(stmt, 0, ref typ);
        }
        _cnn._sql.Reset(stmt);
      }

      if (ret == null) ret = DBNull.Value;

      return ret;
    }

    /// <summary>
    /// Does nothing.  Commands are prepared as they are executed the first time, and kept in prepared state afterwards.
    /// </summary>
    public override void Prepare()
    {
    }

    /// <summary>
    /// Sets the method the SQLiteCommandBuilder uses to determine how to update inserted or updated rows in a DataTable.
    /// </summary>
    public override UpdateRowSource UpdatedRowSource
    {
      get
      {
        return _updateRowSource;
      }
      set
      {
        _updateRowSource = value;
      }
    }

    /// <summary>
    /// Determines if the command is visible at design time.  Defaults to True.
    /// </summary>
    public override bool DesignTimeVisible
    {
      get
      {
        return _designTimeVisible;
      }
      set
      {
        _designTimeVisible = value;
      }
    }

    /// <summary>
    /// Clones a command, including all its parameters
    /// </summary>
    /// <returns>A new SQLiteCommand with the same commandtext, connection and parameters</returns>
    public object Clone()
    {
      SQLiteCommand newcommand = new SQLiteCommand(CommandText, Connection as SQLiteConnection);
      foreach (SQLiteParameter param in _parameterCollection)
      {
        newcommand.Parameters.Add(param.Clone());
      }
      return newcommand;
    }
  }
}