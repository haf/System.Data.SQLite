namespace SQLite.Designer
{
  using System;
  using System.ComponentModel;
  using System.ComponentModel.Design;
  using System.Data.Common;
  using System.Data;

  internal sealed class SQLiteCommandDesigner : ComponentDesigner, IExtenderProvider
  {
    public SQLiteCommandDesigner()
    {
    }

    public override void Initialize(IComponent component)
    {
      base.Initialize(component);
    }

    protected override void PreFilterAttributes(System.Collections.IDictionary attributes)
    {
      base.PreFilterAttributes(attributes);
      DesignTimeVisibleAttribute att = new DesignTimeVisibleAttribute(((DbCommand)Component).DesignTimeVisible);
      attributes[att.TypeId] = att;
    }

    #region IExtenderProvider Members

    public bool CanExtend(object extendee)
    {
      return (extendee is DbCommand);
    }

    #endregion
  }
}
