namespace SQLite.Designer
{
  using System;
  using System.ComponentModel;
  using System.ComponentModel.Design;
  using System.Data.Common;
  using System.Collections;
  using System.Reflection;

  internal sealed class SQLiteAdapterDesigner : ComponentDesigner, IExtenderProvider
  {
    private ComponentDesigner _designer = null;

    public SQLiteAdapterDesigner()
    {
    }

    public override void Initialize(IComponent component)
    {
      base.Initialize(component);

      if (SQLiteDataAdapterToolboxItem._vsdesigner != null)
      {
        Type type = SQLiteDataAdapterToolboxItem._vsdesigner.GetType("Microsoft.VSDesigner.Data.VS.SqlDataAdapterDesigner");
        if (type != null)
        {
          _designer = (ComponentDesigner)Activator.CreateInstance(type);
          _designer.Initialize(component);
        }
      }
    }

    protected override void Dispose(bool disposing)
    {
      if (_designer != null)
        ((IDisposable)_designer).Dispose();

      base.Dispose(disposing);
    }

    public override DesignerVerbCollection Verbs
    {
      get
      {
        return (_designer != null) ? _designer.Verbs : null;
      }
    }

    public override ICollection AssociatedComponents
    {
      get
      {
        return (_designer != null) ? _designer.AssociatedComponents : null;
      }
    }

    #region IExtenderProvider Members

    public bool CanExtend(object extendee)
    {
      return (extendee is DbDataAdapter);
    }

    #endregion
  }
}
