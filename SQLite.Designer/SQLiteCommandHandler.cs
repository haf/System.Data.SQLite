namespace SQLite.Designer
{
  using System;
  using Microsoft.VisualStudio.Data;
  using System.Windows.Forms.Design;

  internal sealed class SQLiteCommandHandler : DataViewCommandHandler
  {
    private static readonly Guid guidDataCmdSet = new Guid("501822E1-B5AF-11d0-B4DC-00A0C91506EF");
    private static readonly Guid guidSQLiteCmdSet = new Guid("814658EE-A28E-4b97-BC33-4B1BC81EBECB");
    private const int cmdidCreateTable = 256;

    public SQLiteCommandHandler()
    {
    }

    public override OleCommandStatus GetCommandStatus(int[] itemIds, OleCommand command, OleCommandTextType textType, OleCommandStatus status)
    {
      if (command.GroupGuid == guidSQLiteCmdSet)
      {
        switch (command.CommandId)
        {
          case cmdidCreateTable:
            status.Supported = true;
            status.Visible = true;
            status.Enabled = true;
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

    /// <summary>
    /// This method executes a specified command, potentially based
    /// on parameters passed in from the data view support XML.
    /// </summary>
    public override object ExecuteCommand(int itemId, OleCommand command, OleCommandExecutionOption executionOption, object arguments)
    {
      object returnValue = null;
      if (command.GroupGuid == guidSQLiteCmdSet)
      {
        switch (command.CommandId)
        {
          case cmdidCreateTable:
            CreateTable();
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
  }
}
