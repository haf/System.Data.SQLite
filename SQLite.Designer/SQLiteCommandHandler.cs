namespace SQLite.Designer
{
  using System;
  using Microsoft.VisualStudio.Data;
  using System.Windows.Forms.Design;
  using Microsoft.VisualStudio.Shell.Interop;
  using Microsoft.VisualStudio;

  enum cmdid
  {
    CreateTable = 256,
    AlterTable = 257,
    DropTable = 258,
    CreateIndex = 259,
    DropIndex = 260,
    DropView = 261,
    Vacuum = 262,
    Rekey = 263,
  }

  internal sealed class SQLiteCommandHandler : DataViewCommandHandler
  {
    private static readonly Guid guidDataCmdSet = new Guid("501822E1-B5AF-11d0-B4DC-00A0C91506EF");
    private static readonly Guid guidSQLiteCmdSet = new Guid("814658EE-A28E-4b97-BC33-4B1BC81EBECB");
    private static readonly Guid guidIFCmdId = new Guid("{74d21311-2aee-11d1-8bfb-00a0c90f26f7}");

    public SQLiteCommandHandler()
    {
    }

    public override OleCommandStatus GetCommandStatus(int[] itemIds, OleCommand command, OleCommandTextType textType, OleCommandStatus status)
    {
      if (command.GroupGuid == guidSQLiteCmdSet)
      {
        switch ((cmdid)command.CommandId)
        {
          case cmdid.CreateTable:
          case cmdid.CreateIndex:
          case cmdid.DropView:
          case cmdid.Vacuum:
//          case cmdid.Rekey:
            status.Supported = true;
            status.Visible = true;
            status.Enabled = true;
            break;
          case cmdid.AlterTable:
          case cmdid.DropTable:
            status.Supported = true;
            status.Visible = true;
            status.Enabled = (SystemTableSelected == false);
            break;
          case cmdid.DropIndex:
            status.Supported = true;
            status.Visible = true;
            status.Enabled = (SystemIndexSelected == false);
            break;
          default:
            base.GetCommandStatus(itemIds, command, textType, status);
            break;
        }
      }
      else
      {
        base.GetCommandStatus(itemIds, command, textType, status);
      }
      return status;
    }

    private bool SystemTableSelected
    {
      get
      {
        int[] items = DataViewHierarchyAccessor.GetSelectedItems();
        int n;
        object[] parts;

        for (n = 0; n < items.Length; n++)
        {
          parts = DataViewHierarchyAccessor.GetObjectIdentifier(items[n]);
          if (parts == null) return true;

          if (parts[2].ToString().StartsWith("sqlite_", StringComparison.InvariantCultureIgnoreCase))
            return true;
        }
        return false;
      }
    }

    private bool SystemIndexSelected
    {
      get
      {
        int[] items = DataViewHierarchyAccessor.GetSelectedItems();
        int n;
        object[] parts;

        for (n = 0; n < items.Length; n++)
        {
          parts = DataViewHierarchyAccessor.GetObjectIdentifier(items[n]);
          if (parts == null) return true;

          if (parts[2].ToString().StartsWith("sqlite_", StringComparison.InvariantCultureIgnoreCase))
            return true;

          if (parts.Length > 3)
          {
            if (parts[3].ToString().StartsWith("sqlite_autoindex_", StringComparison.InvariantCultureIgnoreCase)
              || parts[3].ToString().StartsWith("sqlite_master_PK_", StringComparison.InvariantCultureIgnoreCase))
              return true;
          }
        }
        return false;
      }
    }

    /// <summary>
    /// This method executes a specified command, potentially based
    /// on parameters passed in from the data view support XML.
    /// </summary>
    public override object ExecuteCommand(int itemId, OleCommand command, OleCommandExecutionOption executionOption, object arguments)
    {
      object returnValue = null;
      if (command.GroupGuid == guidSQLiteCmdSet)
      {
        switch ((cmdid)command.CommandId)
        {
          case cmdid.CreateTable:
            CreateTable();
            break;
          case cmdid.DropTable:
            DropSelectedTables();
            break;
          case cmdid.DropIndex:
            DropSelectedIndexes();
            break;
          case cmdid.DropView:
            DropSelectedViews();
            break;
          case cmdid.Vacuum:
            Vacuum();
            break;
          case cmdid.Rekey:
            ChangePassword();
            break;
          default:
            returnValue = base.ExecuteCommand(itemId, command, executionOption, arguments);
            break;
        }
      }
      else
      {
        returnValue = base.ExecuteCommand(itemId, command, executionOption, arguments);
      }
      return returnValue;
    }

    private void CreateTable()
    {
    }

    private void DropSelectedTables()
    {
      int[] items = DataViewHierarchyAccessor.GetSelectedItems();
      int n;
      object[] parts;

      for (n = 0; n < items.Length; n++)
      {
        parts = DataViewHierarchyAccessor.GetObjectIdentifier(items[n]);
        if (parts == null) continue;

        if (System.Windows.Forms.MessageBox.Show(String.Format("Drop table {0} ({1}), are you sure?", parts[2], parts[0]), "Confirm delete", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
        {
          string sql = String.Format("DROP TABLE [{0}].[{1}]", parts[0], parts[2]);

          DataViewHierarchyAccessor.Connection.Command.ExecuteWithoutResults(sql, (int)System.Data.CommandType.Text, null, 0);
          DataViewHierarchyAccessor.DropObjectNode(items[n]);
        }
      }
    }

    private void DropSelectedViews()
    {
      int[] items = DataViewHierarchyAccessor.GetSelectedItems();
      int n;
      object[] parts;

      for (n = 0; n < items.Length; n++)
      {
        parts = DataViewHierarchyAccessor.GetObjectIdentifier(items[n]);
        if (parts == null) continue;

        if (System.Windows.Forms.MessageBox.Show(String.Format("Drop view {0} ({1}), are you sure?", parts[2], parts[0]), "Confirm delete", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
        {
          string sql = String.Format("DROP VIEW [{0}].[{1}]", parts[0], parts[2]);

          DataViewHierarchyAccessor.Connection.Command.ExecuteWithoutResults(sql, (int)System.Data.CommandType.Text, null, 0);
          DataViewHierarchyAccessor.DropObjectNode(items[n]);
        }
      }
    }

    private void DropSelectedIndexes()
    {
      int[] items = DataViewHierarchyAccessor.GetSelectedItems();
      int n;
      object[] parts;

      for (n = 0; n < items.Length; n++)
      {
        parts = DataViewHierarchyAccessor.GetObjectIdentifier(items[n]);
        if (parts == null) continue;

        if (System.Windows.Forms.MessageBox.Show(String.Format("Drop index {0} ({1}), are you sure?", parts[3], parts[0]), "Confirm delete", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
        {
          string sql = String.Format("DROP INDEX [{0}].[{1}]", parts[0], parts[3]);

          DataViewHierarchyAccessor.Connection.Command.ExecuteWithoutResults(sql, (int)System.Data.CommandType.Text, null, 0);
          DataViewHierarchyAccessor.DropObjectNode(items[n]);
        }
      }
    }

    private void Vacuum()
    {
      DataViewHierarchyAccessor.Connection.Command.ExecuteWithoutResults("VACUUM", (int)System.Data.CommandType.Text, null, 0);
    }

    private void ChangePassword()
    {      
    }

    private void Refresh(int itemId)
    {
      IVsUIHierarchy hier = DataViewHierarchyAccessor.Hierarchy as IVsUIHierarchy;

      Guid g = VSConstants.GUID_VSStandardCommandSet97;
      hier.ExecCommand((uint)itemId, ref g, (uint)0xbd, (uint)OleCommandExecutionOption.DoDefault, IntPtr.Zero, IntPtr.Zero);
    }
  }
}