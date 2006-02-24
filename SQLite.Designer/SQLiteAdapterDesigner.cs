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

      Assembly assm = Assembly.Load("Microsoft.VSDesigner, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
      if (assm != null)
      {
        Type type = assm.GetType("Microsoft.VSDesigner.Data.VS.SqlDataAdapterDesigner");
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
