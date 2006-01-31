/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

namespace SQLite.Designer
{
  using System;
  using System.Collections.Generic;
  using System.Text;
  using Microsoft.VisualStudio.Data;
  using Microsoft.VisualStudio.OLE.Interop;

  internal class SQLiteDataObjectIdentifierResolver : DataObjectIdentifierResolver, IObjectWithSite
  {
    private DataConnection _connection;

    public SQLiteDataObjectIdentifierResolver()
    {
    }

    internal SQLiteDataObjectIdentifierResolver(object site)
    {
      _connection = site as DataConnection;
    }

    protected override object[] QuickExpandIdentifier(string typeName, object[] partialIdentifier)
    {
      if (typeName == null)
      {
        throw new ArgumentNullException("typeName");
      }

      // Create an identifier array of the correct full length based on
      // the object type
      object[] identifier = null;
      int length = 0;

      switch (typeName.ToLowerInvariant())
      {
        case "":
          length = 0;
          break;
        case "table":
        case "view":
          length = 3;
          break;
        case "column":
        case "index":
        case "foreignkey":
        case "viewcolumn":
          length = 4;
          break;
        case "indexcolumn":
        case "foreignkeycolumn":
          length = 5;
          break;
        default:
          throw new NotSupportedException();
      }
      identifier = new object[length];

      // If the input identifier is not null, copy it to the full
      // identifier array.  If the input identifier's length is less
      // than the full length we assume the more specific parts are
      // specified and thus copy into the rightmost portion of the
      // full identifier array.
      if (partialIdentifier != null)
      {
        if (partialIdentifier.Length > length)
        {
          throw new InvalidOperationException();
        }
        partialIdentifier.CopyTo(identifier, length - partialIdentifier.Length);
      }

      if (length > 0)
      {
        // Fill in the current database if not specified
        if (!(identifier[0] is string))
        {
          identifier[0] = _connection.SourceInformation[DataSourceInformation.DefaultCatalog] as string;
        }
      }

      if (length > 1)
      {
        identifier[1] = null;
      }

      return identifier;
    }

    protected override object[] QuickContractIdentifier(string typeName, object[] fullIdentifier)
    {
      int length = fullIdentifier.Length;
      object[] identifier = new object[length];

      fullIdentifier.CopyTo(identifier, 0);

      if (length > 0)
      {
        if (!(identifier[0] is string))
          identifier[0] = null;
      }

      if (length > 1)
      {
        identifier[1] = null;
      }

      return identifier;
    }

    /// <summary>
    /// GetSite does not need to be implemented since
    /// DDEX only calls SetSite to site the object.
    /// </summary>
    void IObjectWithSite.GetSite(ref Guid riid, out IntPtr ppvSite)
    {
      ppvSite = IntPtr.Zero;
      throw new NotImplementedException();
    }

    void IObjectWithSite.SetSite(object pUnkSite)
    {
      _connection = (DataConnection)pUnkSite;
    }
  }
}
