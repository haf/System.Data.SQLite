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

  internal sealed class SQLiteDataAdapterToolboxItem : ToolboxItem
  {
    public SQLiteDataAdapterToolboxItem()
    {
    }

    protected override IComponent[] CreateComponentsCore(IDesignerHost host)
    {
      SQLiteDataAdapter adp = new SQLiteDataAdapter();

      if (adp == null) return null;

      List<IComponent> list = new List<IComponent>();
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
