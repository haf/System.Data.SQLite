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

  internal class View : ICustomTypeDescriptor, IHaveConnection
  {
    private string _name;
    private string _oldname;
    private string _sql;
    private string _oldsql;
    private ViewDesignerDoc _owner;
    private string _catalog;
    private DbConnection _connection;

    internal View(string viewName, DbConnection connection, ViewDesignerDoc parent)
    {
      _owner = parent;
      _name = viewName;
      _oldname = viewName;
      _catalog = connection.Database;
      _connection = connection;
      _owner.Name = _name;

      if (String.IsNullOrEmpty(viewName) == false)
      {
        using (DataTable tbl = connection.GetSchema("Views", new string[] { Catalog, null, Name }))
        {
          if (tbl.Rows.Count > 0)
          {
            _sql = tbl.Rows[0]["VIEW_DEFINITION"].ToString();
            _oldsql = _sql;
          }
          else
          {
            _oldname = null;
          }
        }
      }
    }

    public void Committed()
    {
      _oldsql = _sql;
      _oldname = _name;
    }

    public override string ToString()
    {
      return String.Format("[{0}].[{1}]", Catalog, Name);
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
    public string SqlText
    {
      get { return _sql; }
      set
      {
        if (String.Compare(_sql, value, StringComparison.OrdinalIgnoreCase) != 0)
        {
          _sql = value;
          _owner.MakeDirty();
        }
      }
    }

    [Browsable(false)]
    public string OriginalSql
    {
      get { return _oldsql; }
    }

    public string GetSqlText()
    {
      if (String.Compare(_sql, _oldsql, StringComparison.OrdinalIgnoreCase) == 0 && String.Compare(_name, _oldname, StringComparison.OrdinalIgnoreCase) == 0) return null;

      StringBuilder builder = new StringBuilder();

      if (String.IsNullOrEmpty(_oldname) == false)
        builder.AppendFormat("DROP VIEW [{0}].[{1}];\r\n", Catalog, _oldname);

      builder.AppendFormat("CREATE VIEW [{0}].[{1}] AS {2};\r\n", Catalog, Name, SqlText);

      return builder.ToString();
    }

    #region ICustomTypeDescriptor Members

    AttributeCollection ICustomTypeDescriptor.GetAttributes()
    {
      return TypeDescriptor.GetAttributes(GetType());
    }

    string ICustomTypeDescriptor.GetClassName()
    {
      return "View Design";
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

    #region IHaveConnection Members

    public DbConnection GetConnection()
    {
      return _connection;
    }

    #endregion
  }
}
