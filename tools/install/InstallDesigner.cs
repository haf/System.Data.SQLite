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

namespace install
{
  public partial class InstallDesigner : Form
  {
    private static Guid standardDataProviderGuid = new Guid("{0EBAAB6E-CA80-4b4a-8DDF-CBE6BF058C70}");
    private static Guid standardDataSourcesGuid = new Guid("{0EBAAB6E-CA80-4b4a-8DDF-CBE6BF058C71}");
    private static Guid oledbDataProviderGuid = new Guid("{7F041D59-D76A-44ed-9AA2-FBF6B0548B80}");
    private static Guid oledbAltDataProviderGuid = new Guid("{7F041D59-D76A-44ed-9AA2-FBF6B0548B81}");
    private static Guid jetDataSourcesGuid = new Guid("{466CE797-67A4-4495-B75C-A3FD282E7FC3}");
    private static Guid jetAltDataSourcesGuid = new Guid("{466CE797-67A4-4495-B75C-A3FD282E7FC4}");
    private static System.Reflection.Assembly assm = System.Reflection.Assembly.LoadFrom("..\\System.Data.SQLite.DLL");

    private bool _ignoreChecks = true;

    public InstallDesigner()
    {
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
        using (subkey = parent.OpenSubKey(String.Format("{0}\\8.0", subkeyname)))
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

      if (install && !installed)
      {
        AssemblyCache.InstallAssembly("..\\System.Data.SQLite.DLL", null, AssemblyCommitFlags.Default);
      }
      else if (!install && installed)
      {
        System.Reflection.AssemblyName name = assm.GetName();
        AssemblyCacheUninstallDisposition disp;
        AssemblyCache.UninstallAssembly(name.FullName + ", ProcessorArchitecture=" + name.ProcessorArchitecture, null, out disp);
      }
    }

    private void ReplaceJet(string keyname)
    {
      using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format("Software\\Microsoft\\{0}\\8.0\\DataProviders", keyname), true))
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

      using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format("Software\\Microsoft\\{0}\\8.0\\DataSources", keyname), true))
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
      using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format("Software\\Microsoft\\{0}\\8.0\\DataProviders", keyname), true))
      {
        using (RegistryKey source = key.OpenSubKey(oledbAltDataProviderGuid.ToString("B")))
        {
          if (source == null) return;
        }
      }

      Uninstall(keyname, oledbDataProviderGuid, jetDataSourcesGuid);

      using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format("Software\\Microsoft\\{0}\\8.0\\DataProviders", keyname), true))
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

      using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format("Software\\Microsoft\\{0}\\8.0\\DataSources", keyname), true))
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
      using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format("Software\\Microsoft\\{0}\\8.0\\DataProviders", keyname), true))
      {
        using (RegistryKey subkey = key.CreateSubKey(provider.ToString("B"), RegistryKeyPermissionCheck.ReadWriteSubTree))
        {
          subkey.SetValue(null, ".NET Framework Data Provider for SQLite");
          subkey.SetValue("InvariantName", "System.Data.SQLite");
          subkey.SetValue("Technology", "{77AB9A9D-78B9-4ba7-91AC-873F5338F1D2}");
          subkey.SetValue("CodeBase", Path.GetFullPath("SQLite.Designer.DLL"));
          
          // Uncomment this line when using the VSPackage
          // subkey.SetValue("FactoryService", "{DCBE6C8D-0E57-4099-A183-98FF74C64D9D}");

          using (RegistryKey subsubkey = subkey.CreateSubKey("SupportedObjects", RegistryKeyPermissionCheck.ReadWriteSubTree))
          {
            subsubkey.CreateSubKey("DataConnectionProperties").Close();
            subsubkey.CreateSubKey("DataObjectSupport").Close();
            subsubkey.CreateSubKey("DataViewSupport").Close();
            using (RegistryKey subsubsubkey = subsubkey.CreateSubKey("DataConnectionSupport", RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
              // Comment out this line when using the VSPackage
              subsubsubkey.SetValue(null, "SQLite.Designer.SQLiteDataConnectionSupport");
            }
          }
        }
      }

      using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format("Software\\Microsoft\\{0}\\8.0\\DataSources", keyname), true))
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

      /*
      // Uncomment this section to use the VSPackage
      using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format("Software\\Microsoft\\{0}\\8.0\\Packages", keyname), true))
      {
        using (RegistryKey subkey = key.CreateSubKey("{DCBE6C8D-0E57-4099-A183-98FF74C64D9C}", RegistryKeyPermissionCheck.ReadWriteSubTree))
        {
          subkey.SetValue(null, "SQLite Designer Package");
          subkey.SetValue("Class", "SQLite.Designer.SQLitePackage");
          subkey.SetValue("CodeBase", Path.GetFullPath("SQLite.Designer.DLL"));
          subkey.SetValue("ID", 1);
          subkey.SetValue("InprocServer32", "mscoree.dll");
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
      */

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
      xmlNode.Attributes.GetNamedItem("type").Value = "System.Data.SQLite.SQLiteFactory, " + assm.GetName().FullName;

      xmlDoc.Save(xmlFileName);
    }

    private static XmlDocument GetConfig(string keyname, out string xmlFileName)
    {
      // xmlFileName = Environment.ExpandEnvironmentVariables("%WinDir%\\Microsoft.NET\\Framework\\v2.0.50727\\CONFIG\\machine.config");

      try
      {
        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format("Software\\Microsoft\\{0}\\8.0", keyname), true))
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
        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format("Software\\Microsoft\\{0}\\8.0\\DataProviders", keyname), true))
        {
          if (key != null) key.DeleteSubKeyTree(provider.ToString("B"));
        }
        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format("Software\\Microsoft\\{0}\\8.0\\DataSources", keyname), true))
        {
          if (key != null) key.DeleteSubKeyTree(source.ToString("B"));
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