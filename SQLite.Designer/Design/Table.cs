namespace SQLite.Designer.Design
{
  using System;
  using System.Data.Common;
  using System.ComponentModel.Design;
  using System.ComponentModel;
  using System.Drawing.Design;
  using System.Collections.Generic;
  using System.Data;
  using System.Text;
  using SQLite.Designer.Editors;

  internal class Table : ICustomTypeDescriptor, IHaveConnection
  {
    private string _name;
    private string _oldname;
    private string _catalog;
    private List<Column> _columns = new List<Column>();
    private bool _exists = false;
    private bool _hascheck = false;
    private string _origSql = String.Empty;
    private List<Index> _indexes = new List<Index>();
    private List<Index> _oldindexes = new List<Index>();
    private List<ForeignKey> _fkeys = new List<ForeignKey>();
    private List<ForeignKey> _oldfkeys = new List<ForeignKey>();
    private PrimaryKey _key;
    internal TableDesignerDoc _owner;
    internal DbConnection _connection;

    internal Table(string tableName, DbConnection connection, TableDesignerDoc owner)
    {
      _owner = owner;
      _oldname = tableName;
      _connection = connection;
      _name = tableName;
      _owner.Name = _name;
      _catalog = _connection.Database; // main

      ReloadDefinition();

      if (_key == null) _key = new PrimaryKey(_connection, this, null);

      if (_exists)
      {
        using (DataTable tbl = connection.GetSchema("ForeignKeys", new string[] { Catalog, null, Name }))
        {
          foreach (DataRow row in tbl.Rows)
          {
            _fkeys.Add(new ForeignKey(connection, this, row));
            _oldfkeys.Add(new ForeignKey(connection, this, row));
          }
        }
      }

      using (DataTable tbl = connection.GetSchema("Columns", new string[] { Catalog, null, Name }))
      {
        foreach (DataRow row in tbl.Rows)
        {
          _columns.Add(new Column(row, this));
        }
      }
    }

    private void ReloadDefinition()
    {
      using (DataTable tbl = _connection.GetSchema("Tables", new string[] { Catalog, null, Name }))
      {
        if (tbl.Rows.Count > 0)
        {
          _exists = true;
          _hascheck = (bool)tbl.Rows[0]["HAS_CHECKCONSTRAINTS"];
          _origSql = tbl.Rows[0]["TABLE_DEFINITION"].ToString().Trim().TrimEnd(';');
        }
        else
        {
          _exists = false;
          return;
        }
      }
      
      _indexes.Clear();
      _oldindexes.Clear();

      using (DataTable tbl = _connection.GetSchema("Indexes", new string[] { Catalog, null, Name }))
      {
        foreach (DataRow row in tbl.Rows)
        {
          if ((bool)row["PRIMARY_KEY"] == false)
          {
            if (row["INDEX_NAME"].ToString().StartsWith("sqlite_", StringComparison.OrdinalIgnoreCase) == false)
            {
              _indexes.Add(new Index(_connection, this, row));
              _oldindexes.Add(new Index(_connection, this, row));
            }
          }
          else if (_key == null)
          {
            _key = new PrimaryKey(_connection, this, row);
          }
        }
      }

      StringBuilder builder = new StringBuilder();
      builder.Append(_origSql);
      builder.AppendLine(";");
      foreach (Index idx in _oldindexes)
      {
        builder.AppendFormat("{0};\r\n", idx.OriginalSql);
      }

      _origSql = builder.ToString();
    }

    internal void Committed()
    {
      _exists = true;
      ReloadDefinition();
    }

    [Browsable(false)]
    public List<Index> Indexes
    {
      get { return _indexes; }
    }

    [Browsable(false)]
    public PrimaryKey PrimaryKey
    {
      get { return _key; }
      set
      {
        _key = value;
        _owner.Invalidate();
      }
    }

    [Browsable(false)]
    public List<ForeignKey> ForeignKeys
    {
      get { return _fkeys; }
    }

    [Browsable(false)]
    public string OriginalSql
    {
      get { return _origSql; }
    }

    [Browsable(false)]
    public bool HasCheck
    {
      get { return _hascheck; }
    }

    [Category("Storage")]
    [RefreshProperties(RefreshProperties.All)]
    [ParenthesizePropertyName(true)]
    public string Name
    {
      get { return _name; }
      set
      {
        if (_name != value)
        {
          _name = value;
          _owner.Name = value;
          _owner.MakeDirty();
        }
      }
    }

    public override string ToString()
    {
      return String.Format("[{0}].[{1}]", Catalog, Name);
    }

    [Category("Storage")]
    [Editor(typeof(CatalogTypeEditor), typeof(UITypeEditor))]
    [DefaultValue("main")]
    [RefreshProperties(RefreshProperties.All)]
    public string Catalog
    {
      get { return _catalog; }
      //set
      //{
      //  string catalogs = "";
      //  using (DataTable table = _connection.GetSchema("Catalogs"))
      //  {
      //    foreach (DataRow row in table.Rows)
      //    {
      //      catalogs += (row[0].ToString() + ",");
      //    }
      //  }

      //  if (catalogs.IndexOf(value + ",", StringComparison.OrdinalIgnoreCase) == -1)
      //    throw new ArgumentOutOfRangeException("Unrecognized catalog!");

      //  _catalog = value;
      //}
    }
    [Category("Storage")]
    public string Database
    {
      get { return _connection.DataSource; }
    }

    [Browsable(false)]
    public List<Column> Columns
    {
      get { return _columns; }
    }

    public string GetSql()
    {
      StringBuilder builder = new StringBuilder();
      string altName = null;

      if (_exists)
      {
        Guid g = Guid.NewGuid();
        altName = String.Format("{0}_{1}", Name, g.ToString("N"));

        foreach (Index idx in _oldindexes)
        {
          builder.AppendFormat("DROP INDEX [{0}].[{1}];\r\n", _catalog, idx.Name);
        }
        builder.AppendFormat("ALTER TABLE [{0}].[{1}] RENAME TO [{2}];\r\n", _catalog, _oldname, altName);
      }

      builder.AppendFormat("CREATE TABLE [{0}].[{1}] (\r\n", _catalog, Name);
      string separator = "    ";

      foreach (Column c in Columns)
      {
        builder.Append(separator);
        c.WriteSql(builder);
        separator = ",\r\n    ";
      }

      if (_key.Columns.Count > 1)
      {
        string innersep = "";
        builder.AppendFormat("{0}CONSTRAINT PK_{1} PRIMARY KEY (", separator, Name);
        foreach (IndexColumn c in _key.Columns)
        {
          builder.AppendFormat("{0}[{1}]{2}", innersep, c.Column, (c.SortMode == ColumnSortMode.Ascending) ? "" : " DESC");
          innersep = ", ";
        }
        builder.Append(")");

        if (_key.Conflict != ConflictEnum.Abort)
          builder.AppendFormat(" ON CONFLICT {0}", _key.Conflict.ToString());
      }

      foreach (ForeignKey fkey in ForeignKeys)
      {
        builder.Append(separator);
        fkey.WriteSql(builder);
      }

      builder.Append("\r\n);\r\n");

      // Rebuilding an existing table
      if (altName != null)
      {
        separator = "";
        builder.AppendFormat("INSERT INTO [{0}].[{1}] (", _catalog, Name);
        foreach (Column c in Columns)
        {
          if (String.IsNullOrEmpty(c.OriginalName) == false)
          {
            builder.AppendFormat("{1}[{0}]", c.ColumnName, separator);
            separator = ", ";
          }
        }
        builder.Append(") SELECT ");
        separator = "";
        foreach (Column c in Columns)
        {
          if (String.IsNullOrEmpty(c.OriginalName) == false)
          {
            builder.AppendFormat("{1}[{0}]", c.OriginalName, separator);
            separator = ", ";
          }
        }
        builder.AppendFormat(" FROM [{0}].[{1}];\r\n", _catalog, altName);

        builder.AppendFormat("DROP TABLE [{0}].[{1}];\r\n", _catalog, altName);
      }
      separator = "";
      foreach (Index idx in _indexes)
      {
        builder.Append(separator);
        idx.WriteSql(builder);
        separator = "\r\n";
      }

      return builder.ToString();
    }

    #region IHaveConnection Members

    public DbConnection GetConnection()
    {
      return _connection;
    }

    #endregion

    #region ICustomTypeDescriptor Members

    AttributeCollection ICustomTypeDescriptor.GetAttributes()
    {
      return TypeDescriptor.GetAttributes(GetType());
    }

    string ICustomTypeDescriptor.GetClassName()
    {
      return "Table Design";
    }

    string ICustomTypeDescriptor.GetComponentName()
    {
      return ToString();
    }

    TypeConverter ICustomTypeDescriptor.GetConverter()
    {
      return TypeDescriptor.GetConverter(GetType());
    }

    EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
    {
      return TypeDescriptor.GetDefaultEvent(GetType());
    }

    PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
    {
      return TypeDescriptor.GetDefaultProperty(GetType());
    }

    object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
    {
      return TypeDescriptor.GetEditor(GetType(), editorBaseType);
    }

    EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
    {
      return TypeDescriptor.GetEvents(GetType(), attributes);
    }

    EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
    {
      return TypeDescriptor.GetEvents(GetType());
    }

    PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
    {
      return TypeDescriptor.GetProperties(GetType(), attributes);
    }

    PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
    {
      return TypeDescriptor.GetProperties(GetType());
    }

    object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
    {
      return this;
    }

    #endregion
  }

  internal interface IHaveConnection
  {
    DbConnection GetConnection();
  }

  internal interface IHaveConnectionScope : IHaveConnection
  {
    [Browsable(false)]
    string CatalogScope { get; }
    [Browsable(false)]
    string TableScope { get; }
  }

  internal class CatalogTypeEditor : ObjectSelectorEditor
  {
    public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
    {
      return UITypeEditorEditStyle.DropDown;
    }

    protected override void FillTreeWithData(Selector selector, ITypeDescriptorContext context, IServiceProvider provider)
    {
      base.FillTreeWithData(selector, context, provider);
      IHaveConnection source = context.Instance as IHaveConnection;

      if (source == null) return;

      using (DataTable table = source.GetConnection().GetSchema("Catalogs"))
      {
        foreach (DataRow row in table.Rows)
        {
          selector.AddNode(row[0].ToString(), row[0], null);
        }
      }
    }
  }
}
