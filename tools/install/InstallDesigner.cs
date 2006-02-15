/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

namespace install
{
  using System;
  using System.ComponentModel;
  using System.Data;
  using System.Drawing;
  using System.Text;
  using System.Windows.Forms;
  using Microsoft.Win32;
  using System.IO;
  using System.GACManagedAccess;
  using System.Xml;

  public partial class InstallDesigner : Form
  {
    private static Guid standardDataProviderGuid = new Guid("{0EBAAB6E-CA80-4b4a-8DDF-CBE6BF058C70}");
    private static Guid standardDataSourcesGuid = new Guid("{0EBAAB6E-CA80-4b4a-8DDF-CBE6BF058C71}");
    private static Guid standardCFDataSourcesGuid = new Guid("{0EBAAB6E-CA80-4b4a-8DDF-CBE6BF058C72}");
    private static Guid oledbDataProviderGuid = new Guid("{7F041D59-D76A-44ed-9AA2-FBF6B0548B80}");
    private static Guid oledbAltDataProviderGuid = new Guid("{7F041D59-D76A-44ed-9AA2-FBF6B0548B81}");
    private static Guid jetDataSourcesGuid = new Guid("{466CE797-67A4-4495-B75C-A3FD282E7FC3}");
    private static Guid jetAltDataSourcesGuid = new Guid("{466CE797-67A4-4495-B75C-A3FD282E7FC4}");
    private static string[] compactFrameworks = new string[] { "PocketPC", "SmartPhone", "WindowsCE" };

    private string _regRoot = "8.0";
    private System.Reflection.Assembly _assm = null;
    private bool _ignoreChecks = true;
    private string _assmLocation;

    System.Reflection.Assembly SQLite
    {
      get
      {
        if (_assm == null)
        {
          Environment.CurrentDirectory = Path.GetDirectoryName(typeof(InstallDesigner).Assembly.Location);

          try
          {
            _assmLocation = Path.GetFullPath("..\\System.Data.SQLite.DLL");
            _assm = System.Reflection.Assembly.LoadFrom(_assmLocation);
          }
          catch
          {
          }
        }

        if (_assm == null)
        {
          try
          {
            _assmLocation = Path.GetFullPath("..\\x64\\System.Data.SQLite.DLL");
            _assm = System.Reflection.Assembly.LoadFrom(_assmLocation);
          }
          catch
          {
          }
        }

        if (_assm == null)
        {
          try
          {
            _assmLocation = Path.GetFullPath("..\\itanium\\System.Data.SQLite.DLL");
            _assm = System.Reflection.Assembly.LoadFrom(_assmLocation);
          }
          catch
          {
          }
        }

        OpenFileDialog dlg = new OpenFileDialog();
        while (_assm == null)
        {
          dlg.Multiselect = false;
          dlg.InitialDirectory = Environment.CurrentDirectory;
          dlg.FileName = "System.Data.SQLite.DLL";
          dlg.Filter = "System.Data.SQLite.DLL|System.Data.SQLite.DLL";
          if (dlg.ShowDialog() == DialogResult.OK)
          {
            try
            {
              _assmLocation = dlg.FileName;
              _assm = System.Reflection.Assembly.LoadFrom(dlg.FileName);
            }
            catch
            {
            }
          }
          else
            throw new ArgumentException("Unable to find or load System.Data.SQLite.DLL");
        }
        return _assm;
      }

      set
      {
        _assm = value;
      }
    }

    public InstallDesigner()
    {
      string[] args = Environment.GetCommandLineArgs();

      for (int n = 0; n < args.Length; n++)
      {
        if (String.Compare(args[n], "/regroot", true) == 0 ||
          String.Compare(args[n], "-regroot", true) == 0)
        {
          _regRoot = args[n + 1];
          break;
        }
      }

      InitializeComponent();
    }

    private void InstallDesigner_Load(object sender, EventArgs e)
    {
      RegistryKey key;

      using (key = Registry.LocalMachine.OpenSubKey("Software\\Microsoft"))
      {
        AddItem(key, "VisualStudio", "Visual Studio (full editions)", standardDataProviderGuid, null);
        AddItem(key, "VWDExpress", "Visual Web Developer Express Edition", standardDataProviderGuid, null);

        warningPanel.Visible = (AddItem(key, "VCSExpress", "Visual C# Express Edition *", oledbDataProviderGuid, oledbAltDataProviderGuid)
         | AddItem(key, "VCExpress", "Visual C++ Express Edition *", oledbDataProviderGuid, oledbAltDataProviderGuid)
         | AddItem(key, "VBExpress", "Visual Basic Express Edition *", oledbDataProviderGuid, oledbAltDataProviderGuid)
         | AddItem(key, "VJSExpress", "Visual J# Express Edition *", oledbDataProviderGuid, oledbAltDataProviderGuid));
      }

      CheckGac();

      _ignoreChecks = false;
    }

    private bool AddItem(RegistryKey parent, string subkeyname, string itemName, Guid lookFor, object isChecked)
    {
      RegistryKey subkey;

      try
      {
        using (subkey = parent.OpenSubKey(String.Format("{0}\\{1}", subkeyname, _regRoot)))
        {
          ListViewItem item = new ListViewItem(itemName);

          item.Tag = subkeyname;

          using (RegistryKey subsubkey = subkey.OpenSubKey(String.Format("DataProviders\\{0}", (isChecked == null) ? lookFor.ToString("B") : ((Guid)isChecked).ToString("B"))))
          {
            if (subsubkey != null)
            {
              bool itemChecked = (subsubkey.GetValue(null) != null);
              DoInstallUninstall(item);
              item.Checked = itemChecked;
            }
            else
              DoInstallUninstall(item);
          }
          installList.Items.Add(item);
          if (item.Checked)
          {
            DoInstallUninstall(item);
          }
          return true;
        }
      }
      catch
      {
        return false;
      }
    }

    private void closeButton_Click(object sender, EventArgs e)
    {
      this.Close();
    }

    private void installList_ItemChecked(object sender, ItemCheckedEventArgs e)
    {
      if (_ignoreChecks) return;

      DoInstallUninstall(e.Item);

      CheckGac();
    }

    private void DoInstallUninstall(ListViewItem Item)
    {
      if (Item.Checked == false)
      {
        if (Item.Text.IndexOf('*') > -1)
          RestoreJet((string)Item.Tag);
        else
          Uninstall((string)Item.Tag, standardDataProviderGuid, standardDataSourcesGuid);
      }
      else
      {
        if (Item.Text.IndexOf('*') > -1)
          ReplaceJet((string)Item.Tag);
        else
          Install((string)Item.Tag, standardDataProviderGuid, standardDataSourcesGuid);
      }
    }

    private void CheckGac()
    {
      bool install = false;
      bool installed;

      try
      {
        string file = AssemblyCache.QueryAssemblyInfo("System.Data.SQLite");
        installed = true;
      }
      catch
      {
        installed = false;
      }

      for (int n = 0; n < installList.Items.Count; n++)
      {
        if (installList.Items[n].Checked == true)
        {
          install = true;
          break;
        }
      }

      try
      {
        if (install && !installed)
        {
          AssemblyCache.InstallAssembly(_assmLocation, null, AssemblyCommitFlags.Default);
        }
        else if (!install && installed)
        {
          AssemblyCacheUninstallDisposition disp;

          AssemblyCacheEnum entries = new AssemblyCacheEnum("System.Data.SQLite");

          string s;
          while (true)
          {
            s = entries.GetNextAssembly();
            if (String.IsNullOrEmpty(s)) break;

            AssemblyCache.UninstallAssembly(s, null, out disp);
          }
          SQLite = null;
        }
      }
      catch
      {
        throw;
      }
    }

    private void ReplaceJet(string keyname)
    {
      using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format("Software\\Microsoft\\{0}\\{1}\\DataProviders", keyname, _regRoot), true))
      {
        using (RegistryKey source = key.OpenSubKey(oledbDataProviderGuid.ToString("B")))
        {
          using (RegistryKey dest = key.CreateSubKey(oledbAltDataProviderGuid.ToString("B")))
          {
            if (source == null) return;
            CopyKey(source, dest);
          }
        }
        key.DeleteSubKeyTree(oledbDataProviderGuid.ToString("B"));
      }

      using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format("Software\\Microsoft\\{0}\\{1}\\DataSources", keyname, _regRoot), true))
      {
        using (RegistryKey source = key.OpenSubKey(jetDataSourcesGuid.ToString("B")))
        {
          using (RegistryKey dest = key.CreateSubKey(jetAltDataSourcesGuid.ToString("B")))
          {
            if (source == null) return;
            CopyKey(source, dest);
          }
        }
        key.DeleteSubKeyTree(jetDataSourcesGuid.ToString("B"));
      }

      Install(keyname, oledbDataProviderGuid, jetDataSourcesGuid);
    }

    private void RestoreJet(string keyname)
    {
      using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format("Software\\Microsoft\\{0}\\{1}\\DataProviders", keyname, _regRoot), true))
      {
        using (RegistryKey source = key.OpenSubKey(oledbAltDataProviderGuid.ToString("B")))
        {
          if (source == null) return;
        }
      }

      Uninstall(keyname, oledbDataProviderGuid, jetDataSourcesGuid);

      using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format("Software\\Microsoft\\{0}\\{1}\\DataProviders", keyname, _regRoot), true))
      {
        using (RegistryKey source = key.OpenSubKey(oledbAltDataProviderGuid.ToString("B")))
        {
          if (source != null)
          {
            using (RegistryKey dest = key.CreateSubKey(oledbDataProviderGuid.ToString("B")))
            {
              CopyKey(source, dest);
            }
            key.DeleteSubKeyTree(oledbAltDataProviderGuid.ToString("B"));
          }
        }
      }

      using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format("Software\\Microsoft\\{0}\\{1}\\DataSources", keyname, _regRoot), true))
      {
        using (RegistryKey source = key.OpenSubKey(jetAltDataSourcesGuid.ToString("B")))
        {
          if (source != null)
          {
            using (RegistryKey dest = key.CreateSubKey(jetDataSourcesGuid.ToString("B")))
            {
              CopyKey(source, dest);
            }
            key.DeleteSubKeyTree(jetAltDataSourcesGuid.ToString("B"));
          }
        }
      }
    }

    private void Install(string keyname, Guid provider, Guid source)
    {
      using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format("Software\\Microsoft\\{0}\\{1}\\DataProviders", keyname, _regRoot), true))
      {
        using (RegistryKey subkey = key.CreateSubKey(provider.ToString("B"), RegistryKeyPermissionCheck.ReadWriteSubTree))
        {
          subkey.SetValue(null, ".NET Framework Data Provider for SQLite");
          subkey.SetValue("InvariantName", "System.Data.SQLite");
          subkey.SetValue("Technology", "{77AB9A9D-78B9-4ba7-91AC-873F5338F1D2}");
          subkey.SetValue("CodeBase", Path.GetFullPath("SQLite.Designer.DLL"));
          
#if USEPACKAGE
           subkey.SetValue("FactoryService", "{DCBE6C8D-0E57-4099-A183-98FF74C64D9D}");
#endif
          using (RegistryKey subsubkey = subkey.CreateSubKey("SupportedObjects", RegistryKeyPermissionCheck.ReadWriteSubTree))
          {
            subsubkey.CreateSubKey("DataConnectionProperties").Close();
            subsubkey.CreateSubKey("DataObjectSupport").Close();
            subsubkey.CreateSubKey("DataViewSupport").Close();
            using (RegistryKey subsubsubkey = subsubkey.CreateSubKey("DataConnectionSupport", RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
#if !USEPACKAGE
              subsubsubkey.SetValue(null, "SQLite.Designer.SQLiteDataConnectionSupport");
#endif
            }
          }
        }
      }

      using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format("Software\\Microsoft\\{0}\\{1}\\DataSources", keyname, _regRoot), true))
      {
        using (RegistryKey subkey = key.CreateSubKey(source.ToString("B"), RegistryKeyPermissionCheck.ReadWriteSubTree))
        {
          subkey.SetValue(null, "SQLite Database File");
          using (RegistryKey subsubkey = subkey.CreateSubKey("SupportingProviders", RegistryKeyPermissionCheck.ReadWriteSubTree))
          {
            subsubkey.CreateSubKey(provider.ToString("B")).Close();
          }
        }
      }

      for (int n = 0; n < compactFrameworks.Length; n++)
      {
        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format("Software\\Microsoft\\.NETCompactFramework\\v2.0.0.0\\{0}\\DataProviders", compactFrameworks[n]), true))
        {
          if (key != null)
          {
            using (RegistryKey subkey = key.CreateSubKey(standardDataProviderGuid.ToString("B"), RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
              subkey.SetValue(null, ".NET Framework Data Provider for SQLite");
              subkey.SetValue("InvariantName", "System.Data.SQLite");
              subkey.SetValue("RuntimeAssembly", "System.Data.SQLite.DLL");
            }
          }
        }
      }

      string path = Path.GetDirectoryName(_assmLocation);

      while (String.IsNullOrEmpty(path) == false)
      {
        if (File.Exists(path + "\\CompactFramework\\System.Data.SQLite.DLL") == false)
        {
          path = Path.GetDirectoryName(path);
        }
        else break;
      }

      if (String.IsNullOrEmpty(path) == false)
      {
        path += "\\CompactFramework\\";

        for (int n = 0; n < compactFrameworks.Length; n++)
        {
          using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format("Software\\Microsoft\\.NETCompactFramework\\v2.0.0.0\\{0}\\AssemblyFoldersEx", compactFrameworks[n]), true))
          {

            if (key != null)
            {
              using (RegistryKey subkey = key.CreateSubKey("SQLite", RegistryKeyPermissionCheck.ReadWriteSubTree))
              {
                subkey.SetValue(null, path);
              }
            }

          }
        }
      }

#if USEPACKAGE
      using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format("Software\\Microsoft\\{0}\\8.0\\Packages", keyname), true))
      {
        using (RegistryKey subkey = key.CreateSubKey("{DCBE6C8D-0E57-4099-A183-98FF74C64D9C}", RegistryKeyPermissionCheck.ReadWriteSubTree))
        {
          subkey.SetValue(null, "SQLite Designer Package");
          subkey.SetValue("Class", "SQLite.Designer.SQLitePackage");
          subkey.SetValue("CodeBase", Path.GetFullPath("SQLite.Designer.DLL"));
          subkey.SetValue("ID", 400);
          subkey.SetValue("InprocServer32", "mscoree.dll");
          subkey.SetValue("CompanyName", "Black Castle Software, LLC");
          subkey.SetValue("MinEdition", "standard");
          subkey.SetValue("ProductName", "SQLite Data Provider");
          subkey.SetValue("ProductVersion", "1.0");
        }
      }

      using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format("Software\\Microsoft\\{0}\\8.0\\Services", keyname), true))
      {
        using (RegistryKey subkey = key.CreateSubKey("{DCBE6C8D-0E57-4099-A183-98FF74C64D9D}", RegistryKeyPermissionCheck.ReadWriteSubTree))
        {
          subkey.SetValue(null, "{DCBE6C8D-0E57-4099-A183-98FF74C64D9C}");
          subkey.SetValue("Name", "SQLite Provider Object Factory");
        }
      }
#endif

      // We used to add factory support to the machine.config -- but its not desirable.
      // Now, we use the development environment's config file instead.
      string xmlFileName;
      XmlDocument xmlDoc = GetConfig(keyname, out xmlFileName);

      if (xmlDoc == null) return;

      XmlNode xmlNode = xmlDoc.SelectSingleNode("configuration/system.data/DbProviderFactories/add[@invariant=\"System.Data.SQLite\"]");
      if (xmlNode == null)
      {
        XmlNode xmlConfig = xmlDoc.SelectSingleNode("configuration");
        if (xmlConfig != null)
        {
          XmlNode xmlData = xmlConfig.SelectSingleNode("system.data");
          if (xmlData == null)
          {
            xmlData = xmlDoc.CreateNode(XmlNodeType.Element, "system.data", "");
            xmlConfig.AppendChild(xmlData);
          }
          XmlNode xmlParent = xmlData.SelectSingleNode("DbProviderFactories");
          if (xmlParent == null)
          {
            xmlParent = xmlDoc.CreateNode(XmlNodeType.Element, "DbProviderFactories", "");
            xmlData.AppendChild(xmlParent);
          }

          //xmlNode = xmlDoc.CreateNode(XmlNodeType.Element, "remove", "");
          //xmlNode.Attributes.SetNamedItem(xmlDoc.CreateAttribute("invariant"));
          //xmlParent.AppendChild(xmlNode);
          //xmlNode.Attributes.GetNamedItem("invariant").Value = "System.Data.SQLite";

          xmlNode = xmlDoc.CreateNode(XmlNodeType.Element, "add", "");
          xmlNode.Attributes.SetNamedItem(xmlDoc.CreateAttribute("name"));
          xmlNode.Attributes.SetNamedItem(xmlDoc.CreateAttribute("invariant"));
          xmlNode.Attributes.SetNamedItem(xmlDoc.CreateAttribute("description"));
          xmlNode.Attributes.SetNamedItem(xmlDoc.CreateAttribute("type"));
          xmlParent.AppendChild(xmlNode);
        }
      }
      xmlNode.Attributes.GetNamedItem("name").Value = "SQLite Data Provider";
      xmlNode.Attributes.GetNamedItem("invariant").Value = "System.Data.SQLite";
      xmlNode.Attributes.GetNamedItem("description").Value = ".Net Framework Data Provider for SQLite";
      xmlNode.Attributes.GetNamedItem("type").Value = "System.Data.SQLite.SQLiteFactory, " + SQLite.GetName().FullName;

      xmlDoc.Save(xmlFileName);
    }

    private XmlDocument GetConfig(string keyname, out string xmlFileName)
    {
      // xmlFileName = Environment.ExpandEnvironmentVariables("%WinDir%\\Microsoft.NET\\Framework\\v2.0.50727\\CONFIG\\machine.config");

      try
      {
        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format("Software\\Microsoft\\{0}\\{1}", keyname, _regRoot), true))
        {
          xmlFileName = (string)key.GetValue("InstallDir");
          if (String.Compare(keyname, "VisualStudio", true) == 0)
            xmlFileName += "devenv.exe.config";
          else
            xmlFileName += keyname + ".exe.config";
        }

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.PreserveWhitespace = true;
        xmlDoc.Load(xmlFileName);

        return xmlDoc;
      }
      catch
      {
        xmlFileName = null;
      }
      return null;
    }

    private void Uninstall(string keyname, Guid provider, Guid source)
    {
      try
      {
        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format("Software\\Microsoft\\{0}\\{1}\\DataProviders", keyname, _regRoot), true))
        {
          if (key != null) key.DeleteSubKeyTree(provider.ToString("B"));
        }
        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format("Software\\Microsoft\\{0}\\{1}\\DataSources", keyname, _regRoot), true))
        {
          if (key != null) key.DeleteSubKeyTree(source.ToString("B"));
        }

        for (int n = 0; n < compactFrameworks.Length; n++)
        {
          using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format("Software\\Microsoft\\.NETCompactFramework\\v2.0.0.0\\{0}\\DataProviders", compactFrameworks[n]), true))
          {
            try
            {
              if (key != null) key.DeleteSubKey(standardDataProviderGuid.ToString("B"));
            }
            catch
            {
            }
          }
        }

        for (int n = 0; n < compactFrameworks.Length; n++)
        {
          using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format("Software\\Microsoft\\.NETCompactFramework\\v2.0.0.0\\{0}\\AssemblyFoldersEx", compactFrameworks[n]), true))
          {
            try
            {
              if (key != null) key.DeleteSubKey("SQLite");
            }
            catch
            {
            }
          }
        }
      }
      catch
      {
      }

      // Remove factory support from the development environment config file
      string xmlFileName;
      XmlDocument xmlDoc = GetConfig(keyname, out xmlFileName);

      if (xmlDoc == null) return;

      XmlNode xmlNode = xmlDoc.SelectSingleNode("configuration/system.data/DbProviderFactories/add[@invariant=\"System.Data.SQLite\"]");
      if (xmlNode != null)
        xmlNode.ParentNode.RemoveChild(xmlNode);

      xmlNode = xmlDoc.SelectSingleNode("configuration/system.data/DbProviderFactories/remove[@invariant=\"System.Data.SQLite\"]");
      if (xmlNode != null)
        xmlNode.ParentNode.RemoveChild(xmlNode);

      xmlDoc.Save(xmlFileName);

      // Remove any entries in the machine.config if they're still there
      xmlFileName = Environment.ExpandEnvironmentVariables("%WinDir%\\Microsoft.NET\\Framework\\v2.0.50727\\CONFIG\\machine.config");
      xmlDoc = new XmlDocument();
      xmlDoc.PreserveWhitespace = true;
      xmlDoc.Load(xmlFileName);

      xmlNode = xmlDoc.SelectSingleNode("configuration/system.data/DbProviderFactories/add[@invariant=\"System.Data.SQLite\"]");
      
      if (xmlNode != null)
        xmlNode.ParentNode.RemoveChild(xmlNode);

      xmlNode = xmlDoc.SelectSingleNode("configuration/system.data/DbProviderFactories/remove[@invariant=\"System.Data.SQLite\"]");
      if (xmlNode != null)
        xmlNode.ParentNode.RemoveChild(xmlNode);

      xmlDoc.Save(xmlFileName);
    }


    private static void CopyKey(RegistryKey keySource, RegistryKey keyDest)
    {
      if (keySource.SubKeyCount > 0)
      {
        string[] subkeys = keySource.GetSubKeyNames();
        for (int n = 0; n < subkeys.Length; n++)
        {
          using (RegistryKey subkeysource = keySource.OpenSubKey(subkeys[n]))
          {
            using (RegistryKey subkeydest = keyDest.CreateSubKey(subkeys[n], RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
              CopyKey(subkeysource, subkeydest);
            }
          }
        }
      }
      string[] values = keySource.GetValueNames();
      for (int n = 0; n < values.Length; n++)
      {
        keyDest.SetValue(values[n], keySource.GetValue(values[n]), keySource.GetValueKind(values[n]));
      }
    }
  }
}