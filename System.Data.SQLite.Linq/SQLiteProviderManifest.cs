//---------------------------------------------------------------------
// <copyright file="SampleProviderManifest.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//---------------------------------------------------------------------

using System.Collections.Generic;
using System.Xml;
using System.Collections.ObjectModel;
using System.Data.EntityModel.SchemaObjectModel;
using System.Data.EntityModel;
using System.Data.Common;
using System.Diagnostics;
using System.Data.Entity;
using System.Data.Metadata.Edm;
using System;
using System.Data;
using System.Reflection;
using System.IO;

namespace System.Data.SQLite
{
  /// <summary>
  /// The Provider Manifest for SQL Server
  /// </summary>
  internal class SQLiteProviderManifest : DbXmlEnabledProviderManifest
  {
    #region Private Fields

    /// <summary>
    /// maximum size of sql server unicode 
    /// </summary>
    private const int varcharMaxSize = Int32.MaxValue;
    private const int nvarcharMaxSize = Int32.MaxValue;
    private const int binaryMaxSize = Int32.MaxValue;
    internal SQLiteDateFormats _dateFormat;

    private System.Collections.ObjectModel.ReadOnlyCollection<PrimitiveType> _primitiveTypes = null;
    private System.Collections.ObjectModel.ReadOnlyCollection<EdmFunction> _functions = null;

    #endregion

    #region Constructors

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="manifestToken">A token used to infer the capabilities of the store</param>
    public SQLiteProviderManifest(string manifestToken)
      : base(SQLiteProviderManifest.GetProviderManifest())
    {
      _dateFormat = (SQLiteDateFormats)Enum.Parse(typeof(SQLiteDateFormats), manifestToken, true);
    }

    #endregion

    internal static XmlReader GetProviderManifest()
    {
      return GetXmlResource("System.Data.SQLite.Resources.SQLiteProviderServices.ProviderManifest.xml");
    }

    /// <summary>
    /// Providers should override this to return information specific to their provider.  
    /// 
    /// This method should never return null.
    /// </summary>
    /// <param name="informationType">The name of the information to be retrieved.</param>
    /// <returns>An XmlReader at the begining of the information requested.</returns>
    protected override XmlReader GetDbInformation(string informationType)
    {
      if (informationType == DbProviderManifest.StoreSchemaDefinition)
      {
        return GetStoreSchemaDescription();
      }

      if (informationType == DbProviderManifest.StoreSchemaMapping)
      {
        return GetStoreSchemaMapping();
      }

      throw new ProviderIncompatibleException(String.Format("The provider returned null for the informationType '{0}'.", informationType));
    }

    public override System.Collections.ObjectModel.ReadOnlyCollection<PrimitiveType> GetStoreTypes()
    {
      if (_primitiveTypes == null)
      {
        List<PrimitiveType> primitiveTypes = new List<PrimitiveType>(base.GetStoreTypes());
        PrimitiveType newt;

        newt = PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int64);

        _primitiveTypes = primitiveTypes.AsReadOnly();
      }
      return _primitiveTypes;
    }

    public override System.Collections.ObjectModel.ReadOnlyCollection<EdmFunction> GetStoreFunctions()
    {
      if (this._functions == null)
      {
        this._functions = base.GetStoreFunctions();
      }
      return this._functions;
    }

    /// <summary>
    /// This method takes a type and a set of facets and returns the best mapped equivalent type 
    /// in EDM.
    /// </summary>
    /// <param name="storeType">A TypeUsage encapsulating a store type and a set of facets</param>
    /// <returns>A TypeUsage encapsulating an EDM type and a set of facets</returns>
    public override TypeUsage GetEdmType(TypeUsage storeType)
    {
      if (storeType == null)
      {
        throw new ArgumentNullException("storeType");
      }

      string storeTypeName = storeType.EdmType.Name.ToLowerInvariant();
      if (!base.StoreTypeNameToEdmPrimitiveType.ContainsKey(storeTypeName))
      {
        switch (storeTypeName)
        {
          case "integer":
            return TypeUsage.CreateDefaultTypeUsage(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int64));
          default:
            throw new ArgumentException(String.Format("The underlying provider does not support the type '{0}'.", storeTypeName));
        }
      }

      PrimitiveType edmPrimitiveType = base.StoreTypeNameToEdmPrimitiveType[storeTypeName];

      int maxLength = 0;
      bool isUnicode = true;
      bool isFixedLen = false;
      bool isUnbounded = true;

      PrimitiveTypeKind newPrimitiveTypeKind;

      switch (storeTypeName)
      {
        // for some types we just go with simple type usage with no facets
        case "tinyint":
        case "smallint":
        case "integer":
        case "bit":
        case "uniqueidentifier":
        case "int":
          return TypeUsage.CreateDefaultTypeUsage(edmPrimitiveType);

        case "varchar":
          newPrimitiveTypeKind = PrimitiveTypeKind.String;
          isUnbounded = !TypeHelpers.TryGetMaxLength(storeType, out maxLength);
          isUnicode = false;
          isFixedLen = false;
          break;

        case "char":
          newPrimitiveTypeKind = PrimitiveTypeKind.String;
          isUnbounded = !TypeHelpers.TryGetMaxLength(storeType, out maxLength);
          isUnicode = false;
          isFixedLen = true;
          break;

        case "nvarchar":
          newPrimitiveTypeKind = PrimitiveTypeKind.String;
          isUnbounded = !TypeHelpers.TryGetMaxLength(storeType, out maxLength);
          isUnicode = true;
          isFixedLen = false;
          break;

        case "nchar":
          newPrimitiveTypeKind = PrimitiveTypeKind.String;
          isUnbounded = !TypeHelpers.TryGetMaxLength(storeType, out maxLength);
          isUnicode = true;
          isFixedLen = true;
          break;

        case "blob":
          newPrimitiveTypeKind = PrimitiveTypeKind.Binary;
          isUnbounded = !TypeHelpers.TryGetMaxLength(storeType, out maxLength);
          isFixedLen = false;
          break;

        case "float":
        case "real":
          return TypeUsage.CreateDefaultTypeUsage(edmPrimitiveType);

        case "decimal":
          {
            byte precision;
            byte scale;
            if (TypeHelpers.TryGetPrecision(storeType, out precision) && TypeHelpers.TryGetScale(storeType, out scale))
            {
              return TypeUsage.CreateDecimalTypeUsage(edmPrimitiveType, precision, scale);
            }
            else
            {
              return TypeUsage.CreateDecimalTypeUsage(edmPrimitiveType);
            }
          }

        case "datetime":
          return TypeUsage.CreateDateTimeTypeUsage(edmPrimitiveType, null);

        default:
          throw new NotSupportedException(String.Format("The underlying provider does not support the type '{0}'.", storeTypeName));
      }

      Debug.Assert(newPrimitiveTypeKind == PrimitiveTypeKind.String || newPrimitiveTypeKind == PrimitiveTypeKind.Binary, "at this point only string and binary types should be present");

      switch (newPrimitiveTypeKind)
      {
        case PrimitiveTypeKind.String:
          if (!isUnbounded)
          {
            return TypeUsage.CreateStringTypeUsage(edmPrimitiveType, isUnicode, isFixedLen, maxLength);
          }
          else
          {
            return TypeUsage.CreateStringTypeUsage(edmPrimitiveType, isUnicode, isFixedLen);
          }
        case PrimitiveTypeKind.Binary:
          if (!isUnbounded)
          {
            return TypeUsage.CreateBinaryTypeUsage(edmPrimitiveType, isFixedLen, maxLength);
          }
          else
          {
            return TypeUsage.CreateBinaryTypeUsage(edmPrimitiveType, isFixedLen);
          }
        default:
          throw new NotSupportedException(String.Format("The underlying provider does not support the type '{0}'.", storeTypeName));
      }
    }

    /// <summary>
    /// This method takes a type and a set of facets and returns the best mapped equivalent type 
    /// in SQL Server, taking the store version into consideration.
    /// </summary>
    /// <param name="storeType">A TypeUsage encapsulating an EDM type and a set of facets</param>
    /// <returns>A TypeUsage encapsulating a store type and a set of facets</returns>
    public override TypeUsage GetStoreType(TypeUsage edmType)
    {
      if (edmType == null)
      {
        throw new ArgumentNullException("edmType");
      }
      System.Diagnostics.Debug.Assert(edmType.EdmType.BuiltInTypeKind == BuiltInTypeKind.PrimitiveType);

      PrimitiveType primitiveType = edmType.EdmType as PrimitiveType;
      if (primitiveType == null)
      {
        throw new ArgumentException(String.Format("The underlying provider does not support the type '{0}'.", edmType));
      }

      ReadOnlyMetadataCollection<Facet> facets = edmType.Facets;

      switch (primitiveType.PrimitiveTypeKind)
      {
        case PrimitiveTypeKind.Boolean:
          return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["bit"]);

        case PrimitiveTypeKind.Byte:
          return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["tinyint"]);

        case PrimitiveTypeKind.Int16:
          return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["smallint"]);

        case PrimitiveTypeKind.Int32:
          return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["int"]);

        case PrimitiveTypeKind.Int64:
          return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["integer"]);

        case PrimitiveTypeKind.Guid:
          return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["uniqueidentifier"]);

        case PrimitiveTypeKind.Double:
          return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["float"]);

        case PrimitiveTypeKind.Single:
          return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["real"]);

        case PrimitiveTypeKind.Decimal: // decimal, numeric, smallmoney, money
          {
            byte precision;
            if (!TypeHelpers.TryGetPrecision(edmType, out precision))
            {
              precision = 18;
            }

            byte scale;
            if (!TypeHelpers.TryGetScale(edmType, out scale))
            {
              scale = 0;
            }

            return TypeUsage.CreateDecimalTypeUsage(StoreTypeNameToStorePrimitiveType["extDecimal"], precision, scale);
          }

        case PrimitiveTypeKind.Binary: // binary, varbinary, varbinary(max), image, timestamp, rowversion
          {
            bool isFixedLength = null != facets["FixedLength"].Value && (bool)facets["FixedLength"].Value;
            Facet f = facets["MaxLength"];

            bool isMaxLength = f.IsUnbounded || null == f.Value || (int)f.Value > binaryMaxSize;
            int maxLength = !isMaxLength ? (int)f.Value : Int32.MinValue;

            TypeUsage tu;
            if (isFixedLength)
            {
              tu = TypeUsage.CreateBinaryTypeUsage(StoreTypeNameToStorePrimitiveType["binary"], true, maxLength);
            }
            else
            {
              if (isMaxLength)
              {
                tu = TypeUsage.CreateBinaryTypeUsage(StoreTypeNameToStorePrimitiveType["blob"], false);
                System.Diagnostics.Debug.Assert(tu.Facets["MaxLength"].Description.IsConstant, "varbinary(max) is not constant!");
              }
              else
              {
                tu = TypeUsage.CreateBinaryTypeUsage(StoreTypeNameToStorePrimitiveType["blob"], false, maxLength);
              }
            }
            return tu;
          }

        case PrimitiveTypeKind.String:
          // char, nchar, varchar, nvarchar, varchar(max), nvarchar(max), ntext, text, xml
          {
            bool isUnicode = null == facets["Unicode"].Value || (bool)facets["Unicode"].Value;
            bool isFixedLength = null != facets["FixedLength"].Value && (bool)facets["FixedLength"].Value;
            Facet f = facets["MaxLength"];
            // maxlen is true if facet value is unbounded, the value is bigger than the limited string sizes *or* the facet
            // value is null. this is needed since functions still have maxlength facet value as null
            bool isMaxLength = f.IsUnbounded || null == f.Value || (int)f.Value > (isUnicode ? nvarcharMaxSize : varcharMaxSize);
            int maxLength = !isMaxLength ? (int)f.Value : Int32.MinValue;

            TypeUsage tu;

            if (isUnicode)
            {
              if (isFixedLength)
              {
                tu = TypeUsage.CreateStringTypeUsage(StoreTypeNameToStorePrimitiveType["nchar"], true, true, maxLength);
              }
              else
              {
                if (isMaxLength)
                {
                  tu = TypeUsage.CreateStringTypeUsage(StoreTypeNameToStorePrimitiveType["nvarchar"], true, false);
                }
                else
                {
                  tu = TypeUsage.CreateStringTypeUsage(StoreTypeNameToStorePrimitiveType["nvarchar"], true, false, maxLength);
                }
              }
            }
            else
            {
              if (isFixedLength)
              {
                tu = TypeUsage.CreateStringTypeUsage(StoreTypeNameToStorePrimitiveType["char"], false, true, maxLength);
              }
              else
              {
                if (isMaxLength)
                {
                  tu = TypeUsage.CreateStringTypeUsage(StoreTypeNameToStorePrimitiveType["varchar"], false, false);
                }
                else
                {
                  tu = TypeUsage.CreateStringTypeUsage(StoreTypeNameToStorePrimitiveType["varchar"], false, false, maxLength);
                }
              }
            }
            return tu;
          }

        case PrimitiveTypeKind.DateTime: // datetime, smalldatetime

          Facet preserveSecondsFacet;
          bool preserveSeconds;
          if (edmType.Facets.TryGetValue("PreserveSeconds", true, out preserveSecondsFacet) && null != preserveSecondsFacet.Value)
          {
            preserveSeconds = (bool)preserveSecondsFacet.Value;
          }
          else
          {
            preserveSeconds = true;
          }

          return TypeUsage.CreateDefaultTypeUsage(preserveSeconds ? StoreTypeNameToStorePrimitiveType["datetime"] : StoreTypeNameToStorePrimitiveType["smalldatetime"]);

        default:
          throw new NotSupportedException(String.Format("There is no store type corresponding to the EDM type '{0}' of primitive type '{1}'.", edmType, primitiveType.PrimitiveTypeKind));
      }
    }

    private XmlReader GetStoreSchemaMapping()
    {
      return GetXmlResource("System.Data.SQLite.Resources.SQLiteProviderServices.StoreSchemaMapping.msl");
    }

    private XmlReader GetStoreSchemaDescription()
    {
      return GetXmlResource("System.Data.SQLite.Resources.SQLiteProviderServices.StoreSchemaDefinition.ssdl");
    }

    internal static XmlReader GetXmlResource(string resourceName)
    {
      Assembly executingAssembly = Assembly.GetExecutingAssembly();
      Stream stream = executingAssembly.GetManifestResourceStream(resourceName);
      return XmlReader.Create(stream);
    }

    class TypeHelpers
    {
      public static bool TryGetPrecision(TypeUsage tu, out byte precision)
      {
        Facet f;

        precision = 0;
        if (tu.Facets.TryGetValue("Precision", false, out f))
        {
          if (!f.IsUnbounded && f.Value != null)
          {
            precision = (byte)f.Value;
            return true;
          }
        }
        return false;
      }

      public static bool TryGetMaxLength(TypeUsage tu, out int maxLength)
      {
        Facet f;

        maxLength = 0;
        if (tu.Facets.TryGetValue("MaxLength", false, out f))
        {
          if (!f.IsUnbounded && f.Value != null)
          {
            maxLength = (int)f.Value;
            return true;
          }
        }
        return false;
      }

      public static bool TryGetScale(TypeUsage tu, out byte scale)
      {
        Facet f;

        scale = 0;
        if (tu.Facets.TryGetValue("Scale", false, out f))
        {
          if (!f.IsUnbounded && f.Value != null)
          {
            scale = (byte)f.Value;
            return true;
          }
        }
        return false;
      }
    }
  }
}