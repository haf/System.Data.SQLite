using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Data;
using System.Data.Common;

namespace SQLite.Designer.Design
{
  [TypeConverter(typeof(ExpandableObjectConverter))]
  [DefaultProperty("Enabled")]
  internal class Unique : IHaveConnection
  {
    private bool _isUnique;
    private ConflictEnum _conflict = ConflictEnum.Abort;
    private Column _column;

    internal Unique(Column col)
      : this(col, null)
    {
    }

    internal Unique(Column col, DataRow row)
    {
      _column = col;
      if (row != null)
      {
        _isUnique = (row.IsNull("UNIQUE") == false) ? (bool)row["UNIQUE"] : false;
      }
    }

    #region IHaveConnection Members

    public DbConnection GetConnection()
    {
      return ((IHaveConnection)_column).GetConnection();
    }

    #endregion


    [DefaultValue(false)]
    [DisplayName("Enabled")]
    public bool Enabled
    {
      get { return _isUnique; }
      set
      {
        if (value != _isUnique)
        {
          _isUnique = value;
          _column.Table._owner.MakeDirty();
        }
      }
    }

    [DefaultValue(ConflictEnum.Abort)]
    [DisplayName("On Conflict")]
    public ConflictEnum Conflict
    {
      get { return _conflict; }
      set
      {
        if (_conflict != value)
        {
          _conflict = value;
          _column.Table._owner.MakeDirty();
        }
      }
    }

    public override string ToString()
    {
      if (_isUnique == false)
        return Convert.ToString(false);
      else
        return String.Format("{0} ({1})", Convert.ToString(true), Convert.ToString(Conflict));
        //return Convert.ToString(true);
    }
  }

  public enum ConflictEnum
  {
    Abort = 2,
    Rollback = 0,
    Fail = 3,
    Ignore = 4,
    Replace = 5,
  }
}
