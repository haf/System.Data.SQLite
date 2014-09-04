/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

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
  using System.Globalization;

  /// <summary>
  /// Provides a toolboxitem for a SQLiteDataAdapter.  This is required in order for us to 
  /// pop up the connection wizard when you drop the tool on a form, and to create the hidden commands
  /// that are assigned to the data adapter and keep them hidden.  The hiding at runtime of the controls
  /// is accomplished both here during the creation of the components and in the SQLiteCommandDesigner
  /// which provides properties to hide the objects when they're supposed to be hidden.
  /// 
  /// The connection wizard is instantiated in the VSDesigner through reflection.
  /// </summary>
  [Serializable]
  [ToolboxItem(typeof(SQLiteDataAdapterToolboxItem))]
  internal sealed class SQLiteDataAdapterToolboxItem : ToolboxItem
  {
    private static Type _wizard;
    
    internal static Assembly _vsdesigner;

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

    /// <summary>
    /// Creates the necessary components associated with this data adapter instance
    /// </summary>
    /// <param name="host">The designer host</param>
    /// <returns>The components created by this toolbox item</returns>
    protected override IComponent[] CreateComponentsCore(IDesignerHost host)
    {
        List<IComponent> list = new List<IComponent>();
        DbProviderFactory fact;

        if (SQLiteOptions.GetProviderFactory(SQLiteOptions.GetProviderName(), true, out fact))
        {
            DbDataAdapter dataAdapter = fact.CreateDataAdapter();
            IContainer container = host.Container;

            using (DbCommand adapterCommand = fact.CreateCommand())
            {
                ICloneable adapter = (ICloneable)adapterCommand;

                adapterCommand.DesignTimeVisible = false;
                dataAdapter.SelectCommand = (DbCommand)adapter.Clone();
                container.Add(dataAdapter.SelectCommand, GenerateName(container, "SelectCommand"));

                dataAdapter.InsertCommand = (DbCommand)adapter.Clone();
                container.Add(dataAdapter.InsertCommand, GenerateName(container, "InsertCommand"));

                dataAdapter.UpdateCommand = (DbCommand)adapter.Clone();
                container.Add(dataAdapter.UpdateCommand, GenerateName(container, "UpdateCommand"));

                dataAdapter.DeleteCommand = (DbCommand)adapter.Clone();
                container.Add(dataAdapter.DeleteCommand, GenerateName(container, "DeleteCommand"));
            }

            ITypeResolutionService typeResService = (ITypeResolutionService)host.GetService(typeof(ITypeResolutionService));
            if (typeResService != null)
            {
                typeResService.ReferenceAssembly(dataAdapter.GetType().Assembly.GetName());
            }

            container.Add(dataAdapter);

            list.Add(dataAdapter);

            // Show the connection wizard if we have a type for it
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
        }

        return list.ToArray();
    }

    /// <summary>
    /// Generates a unique name for the given object
    /// </summary>
    /// <param name="container">The container where we're being instantiated</param>
    /// <param name="baseName">The core name of the object to create a unique instance of</param>
    /// <returns>A unique name within the given container</returns>
    private static string GenerateName(IContainer container, string baseName)
    {
      ComponentCollection coll = container.Components;
      string uniqueName;
      int n = 1;
      do
      {
        uniqueName = String.Format(CultureInfo.InvariantCulture, "sqlite{0}{1}", baseName, n++);
      } while (coll[uniqueName] != null);

      return uniqueName;
    }
  }
}
