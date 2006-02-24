namespace SQLite.Designer
{
  using System;
  using System.ComponentModel;
  using System.ComponentModel.Design;
  using System.Drawing.Design;
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

    public SQLiteDataAdapterToolboxItem(Type type, Bitmap bmp) : base(type)
    {
      DisplayName = "SQLiteDataAdapter";
    }

    private SQLiteDataAdapterToolboxItem(SerializationInfo info, StreamingContext context)
    {
      Deserialize(info, context);
    }

    protected override IComponent[] CreateComponentsCore(IDesignerHost host)
    {
      DbProviderFactory fact = DbProviderFactories.GetFactory("System.Data.SQLite");

      DbDataAdapter dataAdapter = fact.CreateDataAdapter();
      IContainer container = host.Container;
      
      using (DbCommand adapterCommand = fact.CreateCommand())
      {
        adapterCommand.DesignTimeVisible = false;
        dataAdapter.SelectCommand = (DbCommand)((ICloneable)adapterCommand).Clone();
        container.Add(dataAdapter.SelectCommand, GenerateName(container, "SelectCommand"));

        dataAdapter.InsertCommand = (DbCommand)((ICloneable)adapterCommand).Clone();
        container.Add(dataAdapter.InsertCommand, GenerateName(container, "InsertCommand"));

        dataAdapter.UpdateCommand = (DbCommand)((ICloneable)adapterCommand).Clone();
        container.Add(dataAdapter.UpdateCommand, GenerateName(container, "UpdateCommand"));

        dataAdapter.DeleteCommand = (DbCommand)((ICloneable)adapterCommand).Clone();
        container.Add(dataAdapter.DeleteCommand, GenerateName(container, "DeleteCommand"));
      }

      ITypeResolutionService typeResService = (ITypeResolutionService)host.GetService(typeof(ITypeResolutionService));
      if (typeResService != null)
      {
        typeResService.ReferenceAssembly(dataAdapter.GetType().Assembly.GetName());
      }

      container.Add(dataAdapter);

      List<IComponent> list = new List<IComponent>();
      list.Add(dataAdapter);

      if (_wizard != null)
      {
        using (Form wizard = (Form)Activator.CreateInstance(_wizard, new object[] { host, dataAdapter }))
        {
          wizard.ShowDialog();
        }
      }

      if (dataAdapter.SelectCommand != null) list.Add(dataAdapter.SelectCommand);
      if (dataAdapter.InsertCommand != null) list.Add(dataAdapter.InsertCommand);
      if (dataAdapter.DeleteCommand != null) list.Add(dataAdapter.DeleteCommand);
      if (dataAdapter.UpdateCommand != null) list.Add(dataAdapter.UpdateCommand);

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
