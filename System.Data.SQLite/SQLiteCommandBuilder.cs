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
  using System.Globalization;
  using System.ComponentModel;

  /// <summary>
  /// SQLite implementation of DbCommandBuilder.
  /// </summary>
  public sealed class SQLiteCommandBuilder : DbCommandBuilder
  {
    private EventHandler<RowUpdatingEventArgs> _handler;

    /// <summary>
    /// Default constructor
    /// </summary>
    public SQLiteCommandBuilder() : this(null)
    {
    }

    /// <summary>
    /// Initializes the command builder and associates it with the specified data adapter.
    /// </summary>
    /// <param name="adp"></param>
    public SQLiteCommandBuilder(SQLiteDataAdapter adp)
    {
      QuotePrefix = "[";
      QuoteSuffix = "]";
      DataAdapter = adp;
    }

    /// <summary>
    /// Minimal amount of parameter processing.  Primarily sets the DbType for the parameter equal to the provider type in the schema
    /// </summary>
    /// <param name="parameter">The parameter to use in applying custom behaviors to a row</param>
    /// <param name="row">The row to apply the parameter to</param>
    /// <param name="statementType">The type of statement</param>
    /// <param name="whereClause">Whether the application of the parameter is part of a WHERE clause</param>
    protected override void ApplyParameterInfo(DbParameter parameter, DataRow row, StatementType statementType, bool whereClause)
    {
      SQLiteParameter param = (SQLiteParameter)parameter;
      param.DbType = (DbType)row[SchemaTableColumn.ProviderType];
    }

    /// <summary>
    /// Returns a valid named parameter
    /// </summary>
    /// <param name="parameterName">The name of the parameter</param>
    /// <returns>Error</returns>
    protected override string GetParameterName(string parameterName)
    {
      return String.Format(CultureInfo.InvariantCulture, "@{0}", parameterName);
    }

    /// <summary>
    /// Returns a named parameter for the given ordinal
    /// </summary>
    /// <param name="parameterOrdinal">The i of the parameter</param>
    /// <returns>Error</returns>
    protected override string GetParameterName(int parameterOrdinal)
    {
      return String.Format(CultureInfo.InvariantCulture, "@param{0}", parameterOrdinal);
    }

    /// <summary>
    /// Returns a placeholder character for the specified parameter i.
    /// </summary>
    /// <param name="parameterOrdinal">The index of the parameter to provide a placeholder for</param>
    /// <returns>Returns a named parameter</returns>
    protected override string GetParameterPlaceholder(int parameterOrdinal)
    {
      return GetParameterName(parameterOrdinal);
    }

    /// <summary>
    /// Sets the handler for receiving row updating events.  Used by the DbCommandBuilder to autogenerate SQL
    /// statements that may not have previously been generated.
    /// </summary>
    /// <param name="adapter">A data adapter to receive events on.</param>
    protected override void SetRowUpdatingHandler(DbDataAdapter adapter)
    {
      SQLiteDataAdapter adp = (SQLiteDataAdapter)adapter;

      _handler = new EventHandler<RowUpdatingEventArgs>(RowUpdatingEventHandler);
      adp.RowUpdating += _handler;
    }

    private void RowUpdatingEventHandler(object sender, RowUpdatingEventArgs e)
    {
      base.RowUpdatingHandler(e);
    }

    /// <summary>
    /// Gets/sets the DataAdapter for this CommandBuilder
    /// </summary>
    public new SQLiteDataAdapter DataAdapter
    {
      get { return (SQLiteDataAdapter)base.DataAdapter; }
      set { base.DataAdapter = value; }
    }

    /// <summary>
    /// Returns the automatically-generated SQLite command to delete rows from the database
    /// </summary>
    /// <returns></returns>
    public new SQLiteCommand GetDeleteCommand()
    {
      return (SQLiteCommand)base.GetDeleteCommand();
    }

    /// <summary>
    /// Returns the automatically-generated SQLite command to delete rows from the database
    /// </summary>
    /// <param name="useColumnsForParameterNames"></param>
    /// <returns></returns>
    public new SQLiteCommand GetDeleteCommand(bool useColumnsForParameterNames)
    {
      return (SQLiteCommand)base.GetDeleteCommand(useColumnsForParameterNames);
    }

    /// <summary>
    /// Returns the automatically-generated SQLite command to update rows in the database
    /// </summary>
    /// <returns></returns>
    public new SQLiteCommand GetUpdateCommand()
    {
      return (SQLiteCommand)base.GetUpdateCommand();
    }

    /// <summary>
    /// Returns the automatically-generated SQLite command to update rows in the database
    /// </summary>
    /// <param name="useColumnsForParameterNames"></param>
    /// <returns></returns>
    public new SQLiteCommand GetUpdateCommand(bool useColumnsForParameterNames)
    {
      return (SQLiteCommand)base.GetUpdateCommand(useColumnsForParameterNames);
    }

    /// <summary>
    /// Returns the automatically-generated SQLite command to insert rows into the database
    /// </summary>
    /// <returns></returns>
    public new SQLiteCommand GetInsertCommand()
    {
      return (SQLiteCommand)base.GetInsertCommand();
    }

    /// <summary>
    /// Returns the automatically-generated SQLite command to insert rows into the database
    /// </summary>
    /// <param name="useColumnsForParameterNames"></param>
    /// <returns></returns>
    public new SQLiteCommand GetInsertCommand(bool useColumnsForParameterNames)
    {
      return (SQLiteCommand)base.GetInsertCommand(useColumnsForParameterNames);
    }

    /// <summary>
    /// Overridden to hide its property from the designer
    /// </summary>
#if !PLATFORM_COMPACTFRAMEWORK
    [Browsable(false)]
#endif
    public override CatalogLocation CatalogLocation
    {
      get
      {
        return base.CatalogLocation;
      }
      set
      {
        base.CatalogLocation = value;
      }
    }

    /// <summary>
    /// Overridden to hide its property from the designer
    /// </summary>
#if !PLATFORM_COMPACTFRAMEWORK
    [Browsable(false)]
#endif
    public override string CatalogSeparator
    {
      get
      {
        return base.CatalogSeparator;
      }
      set
      {
        base.CatalogSeparator = value;
      }
    }

    /// <summary>
    /// Overridden to hide its property from the designer
    /// </summary>
#if !PLATFORM_COMPACTFRAMEWORK
    [Browsable(false)]
#endif
    [DefaultValue("[")]
    public override string QuotePrefix
    {
      get
      {
        return base.QuotePrefix;
      }
      set
      {
        base.QuotePrefix = value;
      }
    }

    /// <summary>
    /// Overridden to hide its property from the designer
    /// </summary>
#if !PLATFORM_COMPACTFRAMEWORK
    [Browsable(false)]
#endif
    public override string QuoteSuffix
    {
      get
      {
        return base.QuoteSuffix;
      }
      set
      {
        base.QuoteSuffix = value;
      }
    }

    /// <summary>
    /// Places brackets around an identifier
    /// </summary>
    /// <param name="unquotedIdentifier">The identifier to quote</param>
    /// <returns>The bracketed identifier</returns>
    public override string QuoteIdentifier(string unquotedIdentifier)
    {
      if (String.IsNullOrEmpty(QuotePrefix)
        || String.IsNullOrEmpty(QuoteSuffix)
        || String.IsNullOrEmpty(unquotedIdentifier))
        return unquotedIdentifier;

      return QuotePrefix + unquotedIdentifier.Replace(QuoteSuffix, QuoteSuffix + QuoteSuffix) + QuoteSuffix;
    }

    /// <summary>
    /// Removes brackets around an identifier
    /// </summary>
    /// <param name="quotedIdentifier">The quoted (bracketed) identifier</param>
    /// <returns>The undecorated identifier</returns>
    public override string UnquoteIdentifier(string quotedIdentifier)
    {
      if (String.IsNullOrEmpty(QuotePrefix)
        || String.IsNullOrEmpty(QuoteSuffix)
        || String.IsNullOrEmpty(quotedIdentifier))
        return quotedIdentifier;

      if (quotedIdentifier.StartsWith(QuotePrefix, StringComparison.InvariantCultureIgnoreCase) == false
        || quotedIdentifier.EndsWith(QuoteSuffix, StringComparison.InvariantCultureIgnoreCase) == false)
        return quotedIdentifier;

      return quotedIdentifier.Substring(QuotePrefix.Length, quotedIdentifier.Length - (QuotePrefix.Length + QuoteSuffix.Length)).Replace(QuoteSuffix + QuoteSuffix, QuoteSuffix);
    }

    /// <summary>
    /// Overridden to hide its property from the designer
    /// </summary>
#if !PLATFORM_COMPACTFRAMEWORK
    [Browsable(false)]
#endif
    public override string SchemaSeparator
    {
      get
      {
        return base.SchemaSeparator;
      }
      set
      {
        base.SchemaSeparator = value;
      }
    }
  }
}
