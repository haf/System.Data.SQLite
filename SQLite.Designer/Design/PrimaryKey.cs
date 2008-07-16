using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Data;
using System.Data.Common;

namespace SQLite.Designer.Design
{
  internal class PrimaryKey : Index, ICloneable
  {
    private bool _autoincrement;

    internal PrimaryKey(DbConnection cnn, Table table, DataRow row)
      : base(cnn, table, row)
    {
      if (String.IsNullOrEmpty(_name) == false && _name.StartsWith("sqlite_", StringComparison.OrdinalIgnoreCase))
        _name = null;
    }

    protected PrimaryKey(PrimaryKey source)
      : base(source)
    {
      _autoincrement = source._autoincrement;
    }

    public override IndexTypeEnum IndexType
    {
      get
      {
        return IndexTypeEnum.PrimaryKey;
      }
    }

    protected override string NamePrefix
    {
      get
      {
        return "PK";
      }
    }

    protected override string NewName
    {
      get
      {
        return Table.Name;
      }
    }

    [Browsable(false)]
    public override bool Unique
    {
      get
      {
        return true;
      }
      set
      {
        base.Unique = true;
      }
    }

    [DefaultValue(ConflictEnum.Abort)]
    [DisplayName("On Conflict")]
    public ConflictEnum Conflict
    {
      get { return _conflict; }
      set { _conflict = value; }
    }

    [DefaultValue(false)]
    public bool AutoIncrement
    {
      get
      {
        if (Columns.Count > 1) return false;
        if (Columns.Count == 1 && Columns[0].SortMode != ColumnSortMode.Ascending) return false;
        return _autoincrement;
      }
      set { _autoincrement = value; }
    }

    #region ICloneable Members

    object ICloneable.Clone()
    {
      return new PrimaryKey(this);
    }

    #endregion
  }
}
