namespace SQLite.Designer
{
  using System;
  using System.ComponentModel;
  using System.ComponentModel.Design;
  using System.Drawing.Design;
  using System.Data.SQLite;
  using System.Data.Common;
  using System.Reflection;
  using System.Collections.Generic;
  using System.Windows.Forms;
  using System.Drawing;
  using System.Runtime.Serialization;

  [Serializable]
  [ToolboxItem(typeof(SQLiteDataAdapterToolboxItem))]
  internal sealed class SQLiteDataAdapterToolboxItem : ToolboxItem
  {
    private static Type _wizard = null;
    internal static Assembly _vsdesigner = null;

    static SQLiteDataAdapterToolboxItem()
    {
      _vsdesigner = Assembly.Load("Microsoft.VSDesigner, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

      _wizard = _vsdesigner.GetType("Microsoft.VSDesigner.Data.VS.DataAdapterWizard");
    }

    public SQLiteDataAdapterToolboxItem(Type type) : this(type, (Bitmap)null)
    {
    }


    public SQLiteDataAdapterToolboxItem(Type type, Bitmap bmp) : base(typeof(SQLiteDataAdapter))
    {
      DisplayName = "SQLiteDataAdapter";
    }

    private SQLiteDataAdapterToolboxItem(SerializationInfo info, StreamingContext context) : base(typeof(SQLiteDataAdapter))
    {
      Deserialize(info, context);
    }

    protected override IComponent[] CreateComponentsCore(IDesignerHost host)
    {
      SQLiteDataAdapter adp = new SQLiteDataAdapter();

      if (adp == null) return null;

      IContainer container = host.Container;

      adp.SelectCommand = new SQLiteCommand();
      adp.SelectCommand.DesignTimeVisible = false;
      container.Add(adp.SelectCommand, GenerateName(container, "SelectCommand"));

      adp.InsertCommand = new SQLiteCommand();
      adp.InsertCommand.DesignTimeVisible = false;
      container.Add(adp.InsertCommand, GenerateName(container, "InsertCommand"));

      adp.UpdateCommand = new SQLiteCommand();
      adp.UpdateCommand.DesignTimeVisible = false;
      container.Add(adp.UpdateCommand, GenerateName(container, "UpdateCommand"));

      adp.DeleteCommand = new SQLiteCommand();
      adp.DeleteCommand.DesignTimeVisible = false;
      container.Add(adp.DeleteCommand, GenerateName(container, "DeleteCommand"));

      ITypeResolutionService res = (ITypeResolutionService)host.GetService(typeof(ITypeResolutionService));
      if (res != null)
      {
        res.ReferenceAssembly(typeof(SQLiteDataAdapter).Assembly.GetName());
      }
      container.Add(adp);

      List<IComponent> list = new List<IComponent>();
      list.Add(adp);

      if (_wizard != null)
      {
        using (Form wizard = (Form)Activator.CreateInstance(_wizard, new object[] { host, adp }))
        {
          wizard.ShowDialog();
        }
      }

      if (adp.SelectCommand != null) list.Add(adp.SelectCommand);
      if (adp.InsertCommand != null) list.Add(adp.InsertCommand);
      if (adp.DeleteCommand != null) list.Add(adp.DeleteCommand);
      if (adp.UpdateCommand != null) list.Add(adp.UpdateCommand);

      return list.ToArray();      
    }

    private static string GenerateName(IContainer container, string baseName)
    {
      ComponentCollection coll = container.Components;
      string uniqueName;
      int n = 1;
      do
      {
        uniqueName = String.Format("sqlite{0}{1}", baseName, n++);
      } while (coll[uniqueName] != null);

      return uniqueName;
    }
  }
}
