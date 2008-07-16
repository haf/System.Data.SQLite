using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Windows.Forms;

namespace SQLite.Designer.Design
{
  internal class ForeignKeyEditor : CollectionEditor
  {
    Table _table;
    bool _cancel = false;
    object[] _items;

    internal ForeignKeyEditor(Table parent)
      : base(typeof(List<ForeignKey>))
    {
      _table = parent;
    }

    protected override object CreateInstance(Type itemType)
    {
      if (itemType == typeof(ForeignKey))
      {
        return new ForeignKey(null, _table, null);
      }
      throw new NotSupportedException();
    }

    protected override object[] GetItems(object editValue)
    {
      if (_items == null)
      {
        List<ForeignKey> value = editValue as List<ForeignKey>;
        _items = new object[value.Count];
        for (int n = 0; n < _items.Length; n++)
          _items[n] = value[n].Clone();
      }
      return _items;
    }

    protected override object SetItems(object editValue, object[] value)
    {
      if (editValue != null)
      {
        int length = this.GetItems(editValue).Length;
        int num2 = value.Length;
        if (!(editValue is IList))
        {
          return editValue;
        }
        IList list = (IList)editValue;
        list.Clear();
        for (int i = 0; i < value.Length; i++)
        {
          ForeignKey fkey = value[i] as ForeignKey;

          if (fkey != null && String.IsNullOrEmpty(fkey.From.Column) == false && String.IsNullOrEmpty(fkey.To.Catalog) == false &&
            String.IsNullOrEmpty(fkey.To.Table) == false && String.IsNullOrEmpty(fkey.To.Column) == false)
            list.Add(value[i]);
        }
      }
      
      if (_cancel == false)
        _table._owner.MakeDirty();

      return editValue;
    }

    protected override void CancelChanges()
    {
      _cancel = true;
    }
  }

  internal class ForeignKeyItem : IHaveConnectionScope
  {
    private string _catalog;
    private string _table;
    private string _column;
    private ForeignKey _fkey;

    internal ForeignKeyItem(ForeignKey fkey, string catalog, string table, string column)
    {
      _catalog = catalog;
      _table = table;
      _column = column;
      _fkey = fkey;
    }

    public override string ToString()
    {
      return String.Format("[{0}].[{1}].[{2}]", _catalog, _table, _column);
    }

    #region IHaveConnection Members

    public DbConnection GetConnection()
    {
      return ((IHaveConnection)_fkey).GetConnection();
    }

    [Browsable(false)]
    public string TableScope
    {
      get { return _table; }
    }

    [Browsable(false)]
    public string CatalogScope
    {
      get { return _catalog; }
    }

    #endregion

    [Browsable(false)]
    [Editor(typeof(CatalogTypeEditor), typeof(UITypeEditor))]
    public virtual string Catalog
    {
      get { return _catalog; }
    }

    [Editor(typeof(TablesTypeEditor), typeof(UITypeEditor))]
    public virtual string Table
    {
      get { return _table; }
    }

    [Editor(typeof(ColumnsTypeEditor), typeof(UITypeEditor))]
    public virtual string Column
    {
      get { return _column; }
    }

    protected void SetCatalog(string value)
    {
      _catalog = value;
    }

    protected void SetTable(string value)
    {
      _table = value;
    }

    protected void SetColumn(string value)
    {
      _column = value;
    }
  }

  [TypeConverter(typeof(ExpandableObjectConverter))]
  internal class ForeignKeyFromItem : ForeignKeyItem
  {
    internal ForeignKeyFromItem(ForeignKey fkey, string column)
      : base(fkey, fkey._table.Catalog, fkey._table.Name, column)
    {
    }

    [Editor(typeof(ColumnsTypeEditor), typeof(UITypeEditor))]
    public new string Column
    {
      get { return base.Column; }
      set { SetColumn(value); }
    }

    [Browsable(false)]
    public override string Catalog
    {
      get
      {
        return base.Catalog;
      }
    }

    [Browsable(false)]
    public override string Table
    {
      get
      {
        return base.Table;
      }
    }
  }

  [TypeConverter(typeof(ExpandableObjectConverter))]
  internal class ForeignKeyToItem : ForeignKeyItem
  {
    internal ForeignKeyToItem(ForeignKey fkey, string catalog, string table, string column)
      : base(fkey, catalog, table, column)
    {
    }

    [Browsable(false)]
    [Editor(typeof(CatalogTypeEditor), typeof(UITypeEditor))]
    public new string Catalog
    {
      get
      {
        return base.Catalog;
      }
      set
      {
        SetCatalog(value);
      }
    }

    [DisplayName("Base Table")]
    [Editor(typeof(TablesTypeEditor), typeof(UITypeEditor))]
    public new string Table
    {
      get { return base.Table; }
      set { SetTable(value); }
    }

    [Editor(typeof(ColumnsTypeEditor), typeof(UITypeEditor))]
    public new string Column
    {
      get { return base.Column; }
      set { SetColumn(value); }
    }
  }

  [DefaultProperty("From")]
  internal class ForeignKey : IHaveConnection, ICloneable
  {
    internal Table _table;
    internal ForeignKeyFromItem _from;
    internal ForeignKeyToItem _to;
    internal string _name;

    private ForeignKey(ForeignKey source)
    {
      _table = source._table;
      _from = new ForeignKeyFromItem(this, source._from.Column);
      _to = new ForeignKeyToItem(this, source._to.Catalog, source._to.Table, source._to.Column);
      _name = source._name;
    }

    internal ForeignKey(DbConnection cnn, Table table, DataRow row)
    {
      _table = table;
      if (row != null)
      {
        _from = new ForeignKeyFromItem(this, row["FKEY_FROM_COLUMN"].ToString());
        _to = new ForeignKeyToItem(this, row["FKEY_TO_CATALOG"].ToString(), row["FKEY_TO_TABLE"].ToString(), row["FKEY_TO_COLUMN"].ToString());
        _name = row["CONSTRAINT_NAME"].ToString();
      }
      else
      {
        _name = null;
        _from = new ForeignKeyFromItem(this, "");
        _to = new ForeignKeyToItem(this, _table.Catalog, "", "");
      }
    }

    internal void WriteSql(StringBuilder builder)
    {
      if (String.IsNullOrEmpty(_from.Column) == false && String.IsNullOrEmpty(_to.Catalog) == false &&
        String.IsNullOrEmpty(_to.Table) == false && String.IsNullOrEmpty(_to.Column) == false)
      {
        builder.AppendFormat("CONSTRAINT [{0}] FOREIGN KEY ([{1}]) REFERENCES [{3}] ([{4}])", Name, _from.Column, _to.Catalog, _to.Table, _to.Column);
      }
    }

    [ParenthesizePropertyName(true)]
    [Category("Identity")]
    public string Name
    {
      get
      {
        if (String.IsNullOrEmpty(_name) == false) return _name;

        return String.Format("FK_{0}_{1}_{2}_{3}", _from.Table, _from.Column, _to.Table, _to.Column);
      }
      set { _name = value; }
    }

    #region IHaveConnection Members

    public DbConnection GetConnection()
    {
      return ((IHaveConnection)_table).GetConnection();
    }

    #endregion

    [DisplayName("Primary/Unique Key")]
    [Category("Source")]
    public ForeignKeyFromItem From
    {
      get { return _from; }
    }

    [DisplayName("Foreign Key")]
    [Category("Target")]
    public ForeignKeyToItem To
    {
      get { return _to; }
    }

    #region ICloneable Members

    public object Clone()
    {
      return new ForeignKey(this);
    }

    #endregion
  }
}