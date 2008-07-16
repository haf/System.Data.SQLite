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
  internal class IndexEditor : CollectionEditor
  {
    Table _table;
    bool _cancel = false;
    object[] _items;

    internal IndexEditor(Table parent)
      : base(typeof(List<Index>))
    {
      _table = parent;
    }

    protected override object CreateInstance(Type itemType)
    {
      if (itemType == typeof(Index))
      {
        return new Index(null, _table, null);
      }
      throw new NotSupportedException();
    }

    protected override void CancelChanges()
    {
      _cancel = true;
    }

    protected override bool CanRemoveInstance(object value)
    {
      return !(value is PrimaryKey);
    }

    protected override object[] GetItems(object editValue)
    {
      if (_items == null)
      {
        List<Index> value = editValue as List<Index>;

        int extra = (_table.PrimaryKey.Columns.Count > 0) ? 1 : 0;

        _items = new object[value.Count + extra];
        for (int n = extra; n < _items.Length; n++)
          _items[n] = ((ICloneable)value[n - extra]).Clone();

        if (extra > 0)
          _items[0] = _table.PrimaryKey;
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
          Index idx = value[i] as Index;

          if (idx is PrimaryKey)
          {
            _table.PrimaryKey = (PrimaryKey)idx;
          }
          else
          {
            if (idx != null && idx.Columns.Count > 0)
              list.Add(value[i]);
          }
        }
      }

      if (_cancel == false)
        _table._owner.MakeDirty();

      return editValue;
    }
  }

  internal class IndexColumnEditor : CollectionEditor
  {
    Index _index;
    bool _cancel = false;
    object[] _items;

    public IndexColumnEditor() : base(typeof(List<IndexColumn>))
    {
    }

    public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
    {
      _index = context.Instance as Index;
      _items = null;
      _cancel = false;
      return base.EditValue(context, provider, value);
    }

    protected override object CreateInstance(Type itemType)
    {
      if (itemType == typeof(IndexColumn))
      {
        return new IndexColumn(_index, null);
      }
      throw new NotSupportedException();
    }

    protected override void CancelChanges()
    {
      _cancel = true;
    }

    protected override object[] GetItems(object editValue)
    {
      if (_items == null)
      {
        List<IndexColumn> value = editValue as List<IndexColumn>;
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
          IndexColumn idx = value[i] as IndexColumn;

          if (idx != null && String.IsNullOrEmpty(idx.Column) == false)
            list.Add(value[i]);
        }
      }

      if (_cancel == false)
      {
        if (_index.Columns.Count > 0 && String.IsNullOrEmpty(_index._name) == true)
          _index.Name = _index.Name;

        _index.Table._owner.MakeDirty();
      }

      return editValue;
    }
  }

  internal class IndexTypeConverter : TypeConverter
  {
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
      if (sourceType == typeof(string)) return true;
      return base.CanConvertFrom(context, sourceType);
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
      if (destinationType == typeof(string)) return true;
      return base.CanConvertTo(context, destinationType);
    }

    public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
    {
      if (destinationType == typeof(string))
      {
        StringBuilder builder = new StringBuilder();
        string separator = "";
        foreach (IndexColumn c in (List<IndexColumn>)value)
        {
          builder.AppendFormat("{0}[{1}]", separator, c.Column);
          if (c.SortMode != ColumnSortMode.Ascending)
            builder.Append(" DESC");
          if (c.Collate != "BINARY")
            builder.AppendFormat(" COLLATE {0}", c.Collate);

          separator = ", ";
        }
        return builder.ToString();
      }
      else
        return base.ConvertTo(context, culture, value, destinationType);
    }
  }

  internal enum ColumnSortMode
  {
    Ascending = 0,
    Descending = 1
  }

  [DefaultProperty("Column")]
  internal class IndexColumn : IHaveConnectionScope, ICloneable
  {
    private Index _parent;
    private string _column;
    private ColumnSortMode _mode = ColumnSortMode.Ascending;
    private string _collate = "BINARY";

    [Editor(typeof(ColumnsTypeEditor), typeof(UITypeEditor))]
    [DisplayName("Base Column")]
    public string Column
    {
      get { return _column; }
      set { _column = value; }
    }

    [DefaultValue(ColumnSortMode.Ascending)]
    public ColumnSortMode SortMode
    {
      get { return _mode; }
      set { _mode = value; }
    }

    [DefaultValue("BINARY")]
    public string Collate
    {
      get { return _collate; }
      set
      {
        if (String.IsNullOrEmpty(value)) _collate = "BINARY";
        else _collate = value;
      }
    }

    public override string ToString()
    {
      if (String.IsNullOrEmpty(_column) == true) return "(none)";
      return _column;
    }

    private IndexColumn(IndexColumn source)
    {
      _parent = source._parent;
      _column = source._column;
      _mode = source._mode;
      _collate = source._collate;
    }

    internal IndexColumn(Index parent, DataRow row)
    {
      _parent = parent;
      if (row != null)
      {
        _column = row["COLUMN_NAME"].ToString(); //.Trim().TrimStart('[').TrimEnd(']')
        if (row.IsNull("SORT_MODE") == false && (string)row["SORT_MODE"] != "ASC")
          _mode = ColumnSortMode.Descending;

        if (row.IsNull("COLLATION_NAME") == false && (string)row["COLLATION_NAME"] != "BINARY")
          _collate = row["COLLATION_NAME"].ToString();
      }
    }

    public object Clone()
    {
      return new IndexColumn(this);
    }

    #region IHaveConnectionScope Members

    [Browsable(false)]
    public string CatalogScope
    {
      get { return _parent.Table.Catalog; }
    }

    [Browsable(false)]
    public string TableScope
    {
      get { return _parent.Table.Name; }
    }

    #endregion

    #region IHaveConnection Members

    public DbConnection GetConnection()
    {
      return ((IHaveConnection)_parent).GetConnection();
    }

    #endregion
  }

  public enum IndexTypeEnum
  {
    Index = 0,
    PrimaryKey = 1,
  }

  [DefaultProperty("Columns")]
  internal class Index : IHaveConnection, ICloneable
  {
    private Table _table;
    internal string _name = null;
    private bool _unique;
    private List<IndexColumn> _columns = new List<IndexColumn>();
    private string _definition;
    private bool _calcname;
    internal ConflictEnum _conflict = ConflictEnum.Abort;

    protected Index(Index source)
    {
      _table = source._table;
      _name = source._name;
      _unique = source._unique;
      _columns = source._columns;
      _definition = source._definition;
      _conflict = source._conflict;
    }

    internal Index(DbConnection cnn, Table table, DataRow index)
    {
      _table = table;
      if (index != null)
      {
        _name = index["INDEX_NAME"].ToString();
        _unique = (bool)index["UNIQUE"];
        _definition = index["INDEX_DEFINITION"].ToString();

        using (DataTable tbl = cnn.GetSchema("IndexColumns", new string[] { table.Catalog, null, table.Name, Name }))
        {
          foreach (DataRow row in tbl.Rows)
          {
            _columns.Add(new IndexColumn(this, row));
            //builder.AppendFormat("{0}[{1}]", separator, row["COLUMN_NAME"]);
            //if (row.IsNull("SORT_MODE") == false && (string)row["SORT_MODE"] != "ASC")
            //  builder.AppendFormat(" {0}", row["SORT_MODE"]);

            //if (row.IsNull("COLLATION_NAME") == false && (string)row["COLLATION_NAME"] != "BINARY")
            //  builder.AppendFormat(" COLLATE {0}", row["COLLATION_NAME"]);

            //separator = ", ";
          }
        }
      }
    }

    [DisplayName("Index Type")]
    public virtual IndexTypeEnum IndexType
    {
      get { return IndexTypeEnum.Index; }
    }

    #region IHaveConnection Members

    public DbConnection GetConnection()
    {
      return ((IHaveConnection)_table).GetConnection();
    }

    #endregion

    internal virtual void WriteSql(StringBuilder builder)
    {
      string separator = "";
      builder.AppendFormat("CREATE {0}INDEX [{1}].[{2}] ON [{3}] (", (_unique == true) ? "UNIQUE " : "", _table.Catalog, Name, _table.Name);
      foreach (IndexColumn c in Columns)
      {
        builder.AppendFormat("{0}[{1}]", separator, c.Column);
        if (c.SortMode != ColumnSortMode.Ascending)
          builder.Append(" DESC");
        if (c.Collate != "BINARY")
          builder.AppendFormat(" COLLATE {0}", c.Collate);

        separator = ", ";
      }
      builder.AppendFormat(");");
    }

    [Browsable(false)]
    internal Table Table
    {
      get { return _table; }
    }

    [Browsable(false)]
    public string OriginalSql
    {
      get { return _definition; }
    }

    [DefaultValue(false)]
    public virtual bool Unique
    {
      get { return _unique; }
      set { _unique = value; }
    }

    [Browsable(false)]
    protected virtual string NamePrefix
    {
      get { return "IX"; }
    }

    [Browsable(false)]
    protected virtual string NewName
    {
      get { return "NewIndex"; }
    }

    [ParenthesizePropertyName(true)]
    [RefreshProperties(RefreshProperties.All)]
    public virtual string Name
    {
      get
      {
        if (String.IsNullOrEmpty(_name))
        {
          if (_calcname == true) return GetHashCode().ToString();

          string name = String.Format("{0}_{1}", NamePrefix, NewName);
          if (Columns.Count > 0 && NewName != Table.Name)
          {
            name = String.Format("{0}_", NamePrefix);
            for (int n = 0; n < Columns.Count; n++)
            {
              if (n > 0) name += "_";
              name += Columns[n].Column;
            }
          }
          int count = 0;
          string proposed = name;

          _calcname = true;
          for (int n = 0; n < _table.Indexes.Count; n++)
          {
            Index idx = _table.Indexes[n];
            proposed = string.Format("{0}{1}", name, (count > 0) ? count.ToString() : "");
            if (idx.Name == proposed)
            {
              count++;
              n = -1;
            }
          }
          _calcname = false;
          return proposed;
        }
        return _name;
      }
      set { _name = value; }
    }

    [TypeConverter(typeof(IndexTypeConverter))]
    [Editor(typeof(IndexColumnEditor), typeof(UITypeEditor))]
    [RefreshProperties(RefreshProperties.All)]
    public List<IndexColumn> Columns
    {
      get { return _columns; }
    }

    #region ICloneable Members

    object ICloneable.Clone()
    {
      return new Index(this);
    }

    #endregion
  }

  public class ColumnsMultiSelectEditor : UITypeEditor
  {
    private System.Windows.Forms.Design.IWindowsFormsEditorService _edSvc;
    private CheckedListBox _list;
    private bool _cancel;

    public ColumnsMultiSelectEditor()
    {
      // build selector list
      _list = new CheckedListBox();
      _list.BorderStyle = BorderStyle.FixedSingle;
      _list.CheckOnClick = true;
      _list.ThreeDCheckBoxes = false;
      _list.KeyPress += new KeyPressEventHandler(_list_KeyPress);
    }

    override public UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext ctx)
    {
      return UITypeEditorEditStyle.DropDown;
    }

    override public object EditValue(ITypeDescriptorContext ctx, IServiceProvider provider, object value)
    {
      Index idx = ctx.Instance as Index;
      Table parent = idx.Table;

      // initialize editor service
      _edSvc = (System.Windows.Forms.Design.IWindowsFormsEditorService)provider.GetService(typeof(System.Windows.Forms.Design.IWindowsFormsEditorService));
      if (_edSvc == null)
        return value;

      if (value == null) value = String.Empty;
      if (String.IsNullOrEmpty(value.ToString()) == true) value = String.Empty;

      string[] values = value.ToString().Split(',');
      
      // populate the list
      _list.Items.Clear();
      using (DataTable tbl = parent._connection.GetSchema("Columns", new string[] { parent.Catalog, null, parent.Name }))
      {
        foreach (DataRow item in tbl.Rows)
        {
          // add this item with the proper check state
          CheckState check = CheckState.Unchecked;
          for (int n = 0; n < values.Length; n++)
          {
            if (values[n].Trim() == String.Format("[{0}]", item["COLUMN_NAME"].ToString()))
            {
              check = CheckState.Checked;
              break;
            }
          }
          _list.Items.Add(item["COLUMN_NAME"].ToString(), check);
        }
      }
      _list.Height = Math.Min(300, (_list.Items.Count + 1) * _list.Font.Height);

      // show the list
      _cancel = false;
      _edSvc.DropDownControl(_list);

      // build return value from checked items on the list
      if (!_cancel)
      {
        // build a comma-delimited string with the checked items
        StringBuilder sb = new StringBuilder();
        foreach (object item in _list.CheckedItems)
        {
          if (sb.Length > 0) sb.Append(", ");
          sb.AppendFormat("[{0}]", item.ToString());
        }

        return sb.ToString();
      }

      // done
      return value;
    }

    // ** event handlers

    // close editor if the user presses enter or escape
    private void _list_KeyPress(object sender, KeyPressEventArgs e)
    {
      switch (e.KeyChar)
      {
        case (char)27:
          _cancel = true;
          _edSvc.CloseDropDown();
          break;
        case (char)13:
          _edSvc.CloseDropDown();
          break;
      }
    }
  }

  internal class ColumnsTypeEditor : ObjectSelectorEditor
  {
    public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
    {
      return UITypeEditorEditStyle.DropDown;
    }

    protected override void FillTreeWithData(Selector selector, ITypeDescriptorContext context, IServiceProvider provider)
    {
      base.FillTreeWithData(selector, context, provider);
      IHaveConnectionScope source = context.Instance as IHaveConnectionScope;

      if (source == null) return;

      using (DataTable table = source.GetConnection().GetSchema("Columns", new string[] { source.CatalogScope, null, source.TableScope }))
      {
        foreach (DataRow row in table.Rows)
        {
          selector.AddNode(row[3].ToString(), row[3], null);
        }
      }
    }
  }

  internal class TablesTypeEditor : ObjectSelectorEditor
  {
    public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
    {
      return UITypeEditorEditStyle.DropDown;
    }

    protected override void FillTreeWithData(Selector selector, ITypeDescriptorContext context, IServiceProvider provider)
    {
      base.FillTreeWithData(selector, context, provider);
      IHaveConnectionScope source = context.Instance as IHaveConnectionScope;

      if (source == null) return;

      using (DataTable table = source.GetConnection().GetSchema("Tables", new string[] { source.CatalogScope }))
      {
        foreach (DataRow row in table.Rows)
        {
          selector.AddNode(row[2].ToString(), row[2], null);
        }
      }
    }
  }
}
