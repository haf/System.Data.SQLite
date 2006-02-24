namespace SQLite.Designer
{
  using System;
  using System.ComponentModel;
  using System.ComponentModel.Design;
  using System.Data.Common;
  using System.Data;

  [ProvideProperty("CommandDesignTimeVisible", typeof(IDbCommand))]
  internal sealed class SQLiteCommandDesigner : ComponentDesigner, IExtenderProvider
  {
    private object _designer;
    private Type _designerType;
    private bool visible;

    public SQLiteCommandDesigner()
    {
      _designerType = SQLiteDataAdapterToolboxItem._vsdesigner.GetType("Microsoft.VSDesigner.Data.VS.DataCommandDesigner");
      _designer = Activator.CreateInstance(_designerType);
    }

    public override void Initialize(IComponent component)
    {
      visible = ((DbCommand)component).DesignTimeVisible;

      ((ComponentDesigner)_designer).Initialize(component);
      base.Initialize(component);
    }

    public bool GetCommandDesignTimeVisible(IDbCommand cmd)
    {
      return visible;
    }

    public void SetCommandDesignTimeVisible(IDbCommand cmd, bool value)
    {
      visible = value;
      TypeDescriptor.Refresh(cmd);
    }

    protected override void PreFilterAttributes(System.Collections.IDictionary attributes)
    {
      base.PreFilterAttributes(attributes);
      DesignTimeVisibleAttribute att = new DesignTimeVisibleAttribute(visible);
      attributes[att.TypeId] = att;
    }

    public override DesignerVerbCollection Verbs
    {
      get
      {
        return ((ComponentDesigner)_designer).Verbs;
      }
    }
    #region IExtenderProvider Members

    public bool CanExtend(object extendee)
    {
      return (extendee is DbCommand);
    }

    #endregion
  }
}
