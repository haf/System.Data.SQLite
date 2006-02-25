namespace SQLite.Designer
{
  using System;
  using System.Reflection;
  using System.Data;
  using System.Data.Common;
  using System.ComponentModel.Design;
  using System.ComponentModel;

  internal sealed class SQLiteConnectionStringEditor : ObjectSelectorEditor
  {
    private ObjectSelectorEditor.Selector _selector = null;

    private Type _managerType = null;

    public SQLiteConnectionStringEditor()
    {
      Assembly assm = Assembly.Load("Microsoft.VSDesigner, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
      if (assm != null)
      {
        _managerType = assm.GetType("Microsoft.VSDesigner.Data.VS.VsConnectionManager");
      }
    }

    public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
    {
      if (provider == null || context == null) return value;
      if (context.Instance == null) return value;

      try
      {
        context.OnComponentChanging();
        object newConnection = base.EditValue(context, provider, value);
        string connectionString = newConnection as string;
        int index = -1;

        if (connectionString == null && newConnection != null)
        {
          if (_managerType != null)
          {
            object manager = Activator.CreateInstance(_managerType, new object[] { provider });
            if (manager != null)
            {
              index = (int)_managerType.InvokeMember("AddNewConnection", BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public, null, manager, new object[] { "System.Data.SQLite" });
              if (index > -1 && _selector != null)
              {
                connectionString = (string)_managerType.InvokeMember("GetConnectionString", BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public, null, manager, new object[] { index });
                _selector.SelectedNode = _selector.AddNode((string)_managerType.InvokeMember("GetConnectionName", BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public, null, manager, new object[] { index }), connectionString, null);
              }
            }
          }
        }

        if (String.IsNullOrEmpty(connectionString) == false)
        {
          value = connectionString;
        }
        context.OnComponentChanged();
      }
      catch
      {
      }
      return value;
    }

    protected override void FillTreeWithData(Selector selector, ITypeDescriptorContext context, IServiceProvider provider)
    {
      object manager = Activator.CreateInstance(_managerType, new object[] { provider });
      DbConnection connection = (DbConnection)context.Instance;
      ObjectSelectorEditor.SelectorNode node;

      _selector = selector;

      _selector.Clear();

      if (manager != null)
      {
        int items = (int)_managerType.InvokeMember("GetConnectionCount", BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public, null, manager, null);
        string dataProvider;
        string connectionString;
        string connectionName;

        for (int n = 0; n < items; n++)
        {
          connectionString = (string)_managerType.InvokeMember("GetConnectionString", BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public, null, manager, new object[] { n });
          connectionName = (string)_managerType.InvokeMember("GetConnectionName", BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public, null, manager, new object[] { n });
          dataProvider = (string)_managerType.InvokeMember("GetProvider", BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public, null, manager, new object[] { n });
          if (String.Compare(dataProvider, "System.Data.SQLite", true) == 0)
          {
            node = selector.AddNode(connectionName, connectionString, null);
            
            if (String.Compare(connectionString, connection.ConnectionString, true) == 0)
              selector.SelectedNode = node;
          }
        }
        selector.AddNode("<New Connection...>", this, null);
      }
    }
  }
}
