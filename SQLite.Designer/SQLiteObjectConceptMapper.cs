
namespace SQLite.Designer
{
  using System;
  using Microsoft.VisualStudio.Data.AdoDotNet;
  using System.Data;

  internal class SQLiteObjectConceptMapper : AdoDotNetObjectConceptMapper
  {
    public SQLiteObjectConceptMapper()
    {
    }

    protected override DbType GetDbTypeFromNativeType(string nativeType)
    {
      DbType ret = base.GetDbTypeFromNativeType(nativeType);
      return ret;
    }

    protected override Type GetFrameworkTypeFromNativeType(string nativeType)
    {
      Type ret = base.GetFrameworkTypeFromNativeType(nativeType);
      return ret;
    }

    protected override int GetProviderTypeFromNativeType(string nativeType)
    {
      int ret = base.GetProviderTypeFromNativeType(nativeType);
      return ret;
    }
  }
}
