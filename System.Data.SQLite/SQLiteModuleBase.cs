/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Joe Mistachkin (joe@mistachkin.com)
 *
 * Released to the public domain, use at your own risk!
 ********************************************************/

using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;

#if !NET_40
using System.Security.Permissions;
#endif

using System.Text;

namespace System.Data.SQLite
{
    #region SQLite Context Helper Class
    public sealed class SQLiteContext
    {
        #region Private Data
        private IntPtr pContext;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        internal SQLiteContext(IntPtr pContext)
        {
            this.pContext = pContext;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public void SetNull()
        {
            if (pContext == IntPtr.Zero)
                throw new InvalidOperationException();

            UnsafeNativeMethods.sqlite3_result_null(pContext);
        }

        ///////////////////////////////////////////////////////////////////////

        public void SetDouble(double value)
        {
            if (pContext == IntPtr.Zero)
                throw new InvalidOperationException();

#if !PLATFORM_COMPACTFRAMEWORK
            UnsafeNativeMethods.sqlite3_result_double(pContext, value);
#else
            UnsafeNativeMethods.sqlite3_result_double_interop(pContext, ref value);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        public void SetInt(int value)
        {
            if (pContext == IntPtr.Zero)
                throw new InvalidOperationException();

            UnsafeNativeMethods.sqlite3_result_int(pContext, value);
        }

        ///////////////////////////////////////////////////////////////////////

        public void SetInt64(long value)
        {
            if (pContext == IntPtr.Zero)
                throw new InvalidOperationException();

#if !PLATFORM_COMPACTFRAMEWORK
            UnsafeNativeMethods.sqlite3_result_int64(pContext, value);
#else
            UnsafeNativeMethods.sqlite3_result_int64_interop(pContext, ref value);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        public void SetString(string value)
        {
            if (pContext == IntPtr.Zero)
                throw new InvalidOperationException();

            byte[] bytes = SQLiteMarshal.GetUtf8BytesFromString(value);

            if (bytes == null)
                throw new ArgumentNullException("value");

            UnsafeNativeMethods.sqlite3_result_text(
                pContext, bytes, bytes.Length, (IntPtr)(-1));
        }

        ///////////////////////////////////////////////////////////////////////

        public void SetError(string value)
        {
            if (pContext == IntPtr.Zero)
                throw new InvalidOperationException();

            byte[] bytes = SQLiteMarshal.GetUtf8BytesFromString(value);

            if (bytes == null)
                throw new ArgumentNullException("value");

            UnsafeNativeMethods.sqlite3_result_error(
                pContext, bytes, bytes.Length);
        }

        ///////////////////////////////////////////////////////////////////////

        public void SetErrorCode(SQLiteErrorCode value)
        {
            if (pContext == IntPtr.Zero)
                throw new InvalidOperationException();

            UnsafeNativeMethods.sqlite3_result_error_code(pContext, value);
        }

        ///////////////////////////////////////////////////////////////////////

        public void SetErrorTooBig()
        {
            if (pContext == IntPtr.Zero)
                throw new InvalidOperationException();

            UnsafeNativeMethods.sqlite3_result_error_toobig(pContext);
        }

        ///////////////////////////////////////////////////////////////////////

        public void SetErrorNoMemory()
        {
            if (pContext == IntPtr.Zero)
                throw new InvalidOperationException();

            UnsafeNativeMethods.sqlite3_result_error_nomem(pContext);
        }

        ///////////////////////////////////////////////////////////////////////

        public void SetBlob(byte[] value)
        {
            if (pContext == IntPtr.Zero)
                throw new InvalidOperationException();

            if (value == null)
                throw new ArgumentNullException("value");

            UnsafeNativeMethods.sqlite3_result_blob(
                pContext, value, value.Length, (IntPtr)(-1));
        }

        ///////////////////////////////////////////////////////////////////////

        public void SetZeroBlob(int value)
        {
            if (pContext == IntPtr.Zero)
                throw new InvalidOperationException();

            UnsafeNativeMethods.sqlite3_result_zeroblob(pContext, value);
        }

        ///////////////////////////////////////////////////////////////////////

        public void SetValue(IntPtr pValue)
        {
            if (pContext == IntPtr.Zero)
                throw new InvalidOperationException();

            UnsafeNativeMethods.sqlite3_result_value(pContext, pValue);
        }
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLite Value Helper Class
    public sealed class SQLiteValue
    {
        #region Private Data
        private IntPtr pValue;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        internal SQLiteValue(IntPtr pValue)
        {
            this.pValue = pValue;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public TypeAffinity GetTypeAffinity()
        {
            if (pValue == IntPtr.Zero) return TypeAffinity.None;
            return UnsafeNativeMethods.sqlite3_value_type(pValue);
        }

        ///////////////////////////////////////////////////////////////////////

        public int GetBytes()
        {
            if (pValue == IntPtr.Zero) return 0;
            return UnsafeNativeMethods.sqlite3_value_bytes(pValue);
        }

        ///////////////////////////////////////////////////////////////////////

        public int GetInt()
        {
            if (pValue == IntPtr.Zero) return default(int);
            return UnsafeNativeMethods.sqlite3_value_int(pValue);
        }

        ///////////////////////////////////////////////////////////////////////

        public long GetInt64()
        {
            if (pValue == IntPtr.Zero) return default(long);

#if !PLATFORM_COMPACTFRAMEWORK
            return UnsafeNativeMethods.sqlite3_value_int64(pValue);
#else
            long value;
            UnsafeNativeMethods.sqlite3_value_int64_interop(pValue, out value);
            return value;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        public double GetDouble()
        {
            if (pValue == IntPtr.Zero) return default(double);

#if !PLATFORM_COMPACTFRAMEWORK
            return UnsafeNativeMethods.sqlite3_value_double(pValue);
#else
            double value;
            UnsafeNativeMethods.sqlite3_value_double_interop(pValue, out value);
            return value;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        public string GetString()
        {
            if (pValue == IntPtr.Zero) return null;
            return SQLiteMarshal.StringFromUtf8IntPtr(pValue, GetBytes());
        }

        ///////////////////////////////////////////////////////////////////////

        public byte[] GetBlob()
        {
            if (pValue == IntPtr.Zero) return null;
            return SQLiteMarshal.BytesFromIntPtr(pValue, GetBytes());
        }
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteIndexConstraintOp Enumeration
    /* [Flags()] */
    public enum SQLiteIndexConstraintOp : byte
    {
        EqualTo = 2,
        GreaterThan = 4,
        LessThanOrEqualTo = 8,
        LessThan = 16,
        GreaterThanOrEqualTo = 32,
        Match = 64
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteIndexConstraint Class
    public sealed class SQLiteIndexConstraint
    {
        #region Internal Constructors
        internal SQLiteIndexConstraint(
            UnsafeNativeMethods.sqlite3_index_constraint constraint
            )
            : this(constraint.iColumn, constraint.op, constraint.usable,
                   constraint.iTermOffset)
        {
            // do nothing.
        }
        #endregion

        //////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private SQLiteIndexConstraint(
            int iColumn,
            SQLiteIndexConstraintOp op,
            byte usable,
            int iTermOffset
            )
        {
            this.iColumn = iColumn;
            this.op = op;
            this.usable = usable;
            this.iTermOffset = iTermOffset;
        }
        #endregion

        //////////////////////////////////////////////////////////////////////

        #region Public Fields
        public int iColumn;

        //////////////////////////////////////////////////////////////////////

        public SQLiteIndexConstraintOp op;

        //////////////////////////////////////////////////////////////////////

        public byte usable;

        //////////////////////////////////////////////////////////////////////

        public int iTermOffset;
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteIndexOrderBy Class
    public sealed class SQLiteIndexOrderBy
    {
        #region Internal Constructors
        internal SQLiteIndexOrderBy(
            UnsafeNativeMethods.sqlite3_index_orderby orderBy
            )
            : this(orderBy.iColumn, orderBy.desc)
        {
            // do nothing.
        }
        #endregion

        //////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private SQLiteIndexOrderBy(
            int iColumn,
            byte desc
            )
        {
            this.iColumn = iColumn;
            this.desc = desc;
        }
        #endregion

        //////////////////////////////////////////////////////////////////////

        #region Public Fields
        public int iColumn; /* Column number */

        //////////////////////////////////////////////////////////////////////

        public byte desc;   /* True for DESC.  False for ASC. */
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteIndexConstraintUsage Class
    public sealed class SQLiteIndexConstraintUsage
    {
        #region Internal Constructors
        internal SQLiteIndexConstraintUsage(
            UnsafeNativeMethods.sqlite3_index_constraint_usage constraintUsage
            )
            : this(constraintUsage.argvIndex, constraintUsage.omit)
        {
            // do nothing.
        }
        #endregion

        //////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private SQLiteIndexConstraintUsage(
            int argvIndex,
            byte omit
            )
        {
            this.argvIndex = argvIndex;
            this.omit = omit;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Fields
        public int argvIndex;

        ///////////////////////////////////////////////////////////////////////

        public byte omit;
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteIndexInputs Class
    public sealed class SQLiteIndexInputs
    {
        #region Internal Constructors
        internal SQLiteIndexInputs(int nConstraint, int nOrderBy)
        {
            constraints = new SQLiteIndexConstraint[nConstraint];
            orderBys = new SQLiteIndexOrderBy[nOrderBy];
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private SQLiteIndexConstraint[] constraints;
        public SQLiteIndexConstraint[] Constraints
        {
            get { return constraints; }
        }

        ///////////////////////////////////////////////////////////////////////

        private SQLiteIndexOrderBy[] orderBys;
        public SQLiteIndexOrderBy[] OrderBys
        {
            get { return orderBys; }
        }
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteIndexOutputs Class
    public sealed class SQLiteIndexOutputs
    {
        #region Internal Constructors
        internal SQLiteIndexOutputs(int nConstraint)
        {
            constraintUsages = new SQLiteIndexConstraintUsage[nConstraint];
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private SQLiteIndexConstraintUsage[] constraintUsages;
        public SQLiteIndexConstraintUsage[] ConstraintUsages
        {
            get { return constraintUsages; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int idxNum;
        public int IdxNum
        {
            get { return idxNum; }
            set { idxNum = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string idxStr;
        public string IdxStr
        {
            get { return idxStr; }
            set { idxStr = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int needToFreeIdxStr;
        public int NeedToFreeIdxStr
        {
            get { return needToFreeIdxStr; }
            set { needToFreeIdxStr = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int orderByConsumed;
        public int OrderByConsumed
        {
            get { return orderByConsumed; }
            set { orderByConsumed = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private double estimatedCost;
        public double EstimatedCost
        {
            get { return estimatedCost; }
            set { estimatedCost = value; }
        }
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteIndex Class
    public sealed class SQLiteIndex
    {
        #region Internal Constructors
        internal SQLiteIndex(int nConstraint, int nOrderBy)
        {
            inputs = new SQLiteIndexInputs(nConstraint, nOrderBy);
            outputs = new SQLiteIndexOutputs(nConstraint);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private SQLiteIndexInputs inputs;
        public SQLiteIndexInputs Inputs
        {
            get { return inputs; }
        }

        ///////////////////////////////////////////////////////////////////////

        private SQLiteIndexOutputs outputs;
        public SQLiteIndexOutputs Outputs
        {
            get { return outputs; }
        }
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteVirtualTableCursor Class
    public class SQLiteVirtualTableCursor : ISQLiteNativeHandle
    {
        #region Public Constructors
        public SQLiteVirtualTableCursor(
            IntPtr nativeHandle
            )
        {
            this.nativeHandle = nativeHandle;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISQLiteNativeHandle Members
        private IntPtr nativeHandle;
        public IntPtr NativeHandle
        {
            get { return nativeHandle; }
        }
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region ISQLiteNativeHandle Interface
    public interface ISQLiteNativeHandle
    {
        IntPtr NativeHandle { get; }
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region ISQLiteNativeModule Interface
    public interface ISQLiteNativeModule
    {
        SQLiteErrorCode xCreate(
            IntPtr pDb,
            IntPtr pAux,
            int argc,
            IntPtr[] argv,
            ref IntPtr pVtab,
            ref IntPtr pError
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode xConnect(
            IntPtr pDb,
            IntPtr pAux,
            int argc,
            IntPtr[] argv,
            ref IntPtr pVtab,
            ref IntPtr pError
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode xBestIndex(
            IntPtr pVtab,
            IntPtr pIndex
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode xDisconnect(
            IntPtr pVtab
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode xDestroy(
            IntPtr pVtab
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode xOpen(
            IntPtr pVtab,
            ref IntPtr pCursor
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode xClose(
            IntPtr pCursor
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode xFilter(
            IntPtr pCursor,
            int idxNum,
            IntPtr idxStr,
            int argc,
            IntPtr[] argv
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode xNext(
            IntPtr pCursor
            );

        ///////////////////////////////////////////////////////////////////////

        int xEof(
            IntPtr pCursor
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode xColumn(
            IntPtr pCursor,
            IntPtr pContext,
            int index
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode xRowId(
            IntPtr pCursor,
            ref long rowId
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode xUpdate(
            IntPtr pVtab,
            int nData,
            IntPtr apData,
            ref long rowId
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode xBegin(
            IntPtr pVtab
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode xSync(
            IntPtr pVtab
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode xCommit(
            IntPtr pVtab
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode xRollback(
            IntPtr pVtab
            );

        ///////////////////////////////////////////////////////////////////////

        int xFindFunction(
            IntPtr pVtab,
            int nArg,
            IntPtr zName,
            ref SQLiteCallback callback,
            ref IntPtr pClientData
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode xRename(
            IntPtr pVtab,
            IntPtr zNew
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode xSavepoint(
            IntPtr pVtab,
            int iSavepoint
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode xRelease(
            IntPtr pVtab,
            int iSavepoint
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode xRollbackTo(
            IntPtr pVtab,
            int iSavepoint
            );
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region ISQLiteManagedModule Interface
    public interface ISQLiteManagedModule
    {
        bool Declared { get; }
        string Name { get; }

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Create(
            SQLiteConnection connection, /* in */
            IntPtr pClientData,          /* in */
            string[] argv,               /* in */
            ref string error             /* out */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Connect(
            SQLiteConnection connection, /* in */
            IntPtr pClientData,          /* in */
            string[] argv,               /* in */
            ref string error             /* out */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode BestIndex(
            SQLiteIndex index /* in, out */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Disconnect();

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Destroy();

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Open(
            ref SQLiteVirtualTableCursor cursor /* out */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Close(
            SQLiteVirtualTableCursor cursor /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Filter(
            SQLiteVirtualTableCursor cursor, /* in */
            int idxNum,                      /* in */
            string idxStr,                   /* in */
            SQLiteValue[] argv               /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Next(
            SQLiteVirtualTableCursor cursor /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        bool Eof(
            SQLiteVirtualTableCursor cursor /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Column(
            SQLiteVirtualTableCursor cursor, /* in */
            SQLiteContext context,           /* in */
            int index                        /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode RowId(
            SQLiteVirtualTableCursor cursor, /* in */
            ref long rowId                   /* out */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Update(
            SQLiteValue[] values, /* in */
            ref long rowId        /* in, out */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Begin();

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Sync();

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Commit();

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Rollback();

        ///////////////////////////////////////////////////////////////////////

        bool FindFunction(
            int nArg,                    /* in */
            string zName,                /* in */
            ref SQLiteFunction function, /* out */
            ref IntPtr pClientData       /* out */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Rename(
            string zNew /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Savepoint(
            int iSavepoint /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode Release(
            int iSavepoint /* in */
            );

        ///////////////////////////////////////////////////////////////////////

        SQLiteErrorCode RollbackTo(
            int iSavepoint /* in */
            );
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteMarshal Class
    internal static class SQLiteMarshal
    {
        #region Private Constants
        private static int ThirtyBits = 0x3fffffff;
        private static readonly Encoding Utf8Encoding = Encoding.UTF8;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IntPtr Helper Methods
        internal static IntPtr IntPtrForOffset(
            IntPtr pointer,
            int offset
            )
        {
            return new IntPtr(pointer.ToInt64() + offset);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Marshal Read Helper Methods
        internal static int ReadInt32(
            IntPtr pointer,
            int offset
            )
        {
#if !PLATFORM_COMPACTFRAMEWORK
            return Marshal.ReadInt32(pointer, offset);
#else
            return Marshal.ReadInt32(IntPtrForOffset(pointer, offset));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        internal static double ReadDouble(
            IntPtr pointer,
            int offset
            )
        {
#if !PLATFORM_COMPACTFRAMEWORK
            return BitConverter.Int64BitsToDouble(Marshal.ReadInt64(
                pointer, offset));
#else
            return BitConverter.ToDouble(BitConverter.GetBytes(
                Marshal.ReadInt64(IntPtrForOffset(pointer, offset))), 0);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        internal static IntPtr ReadIntPtr(
            IntPtr pointer,
            int offset
            )
        {
#if !PLATFORM_COMPACTFRAMEWORK
            return Marshal.ReadIntPtr(pointer, offset);
#else
            return Marshal.ReadIntPtr(IntPtrForOffset(pointer, offset));
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Marshal Write Helper Methods
        internal static void WriteInt32(
            IntPtr pointer,
            int offset,
            int value
            )
        {
#if !PLATFORM_COMPACTFRAMEWORK
            Marshal.WriteInt32(pointer, offset, value);
#else
            Marshal.WriteInt32(IntPtrForOffset(pointer, offset), value);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        internal static void WriteDouble(
            IntPtr pointer,
            int offset,
            double value
            )
        {
#if !PLATFORM_COMPACTFRAMEWORK
            Marshal.WriteInt64(pointer, offset,
                BitConverter.DoubleToInt64Bits(value));
#else
            Marshal.WriteInt64(IntPtrForOffset(pointer, offset),
                BitConverter.ToInt64(BitConverter.GetBytes(value), 0));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        internal static void WriteIntPtr(
            IntPtr pointer,
            int offset,
            IntPtr value
            )
        {
#if !PLATFORM_COMPACTFRAMEWORK
            Marshal.WriteIntPtr(pointer, offset, value);
#else
            Marshal.WriteIntPtr(IntPtrForOffset(pointer, offset), value);
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Memory Allocation Helper Methods
        internal static IntPtr Allocate(int size)
        {
            return UnsafeNativeMethods.sqlite3_malloc(size);
        }

        ///////////////////////////////////////////////////////////////////////

        internal static void Free(IntPtr pMemory)
        {
            UnsafeNativeMethods.sqlite3_free(pMemory);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Byte Array Helper Methods
        internal static byte[] BytesFromIntPtr(
            IntPtr pValue,
            int length
            )
        {
            if (pValue == IntPtr.Zero)
                return null;

            if (length == 0)
                return new byte[0];

            byte[] result = new byte[length];

            Marshal.Copy(pValue, result, 0, length);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        internal static IntPtr BytesToIntPtr(
            byte[] value
            )
        {
            if (value == null)
                return IntPtr.Zero;

            int length = value.Length;

            if (length == 0)
                return IntPtr.Zero;

            IntPtr result = Allocate(length);

            if (result == IntPtr.Zero)
                return IntPtr.Zero;

            Marshal.Copy(value, 0, result, length);

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region UTF-8 Encoding Helper Methods
        internal static byte[] GetUtf8BytesFromString(
            string value
            )
        {
            if (value == null)
                return null;

            return Utf8Encoding.GetBytes(value);
        }

        ///////////////////////////////////////////////////////////////////////

        internal static string GetStringFromUtf8Bytes(
            byte[] bytes
            )
        {
            if (bytes == null)
                return null;

#if !PLATFORM_COMPACTFRAMEWORK
            return Utf8Encoding.GetString(bytes);
#else
            return Utf8Encoding.GetString(bytes, 0, bytes.Length);
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region UTF-8 String Helper Methods
        internal static int ProbeForUtf8ByteLength(
            IntPtr pValue,
            int limit
            )
        {
            int length = 0;

            if (pValue != IntPtr.Zero)
            {
                do
                {
                    if (Marshal.ReadByte(pValue, length) == 0)
                        break;

                    if (length >= limit)
                        break;

                    length++;
                } while (true);
            }

            return length;
        }

        ///////////////////////////////////////////////////////////////////////

        internal static string StringFromUtf8IntPtr(
            IntPtr pValue
            )
        {
            return StringFromUtf8IntPtr(pValue,
                ProbeForUtf8ByteLength(pValue, ThirtyBits));
        }

        ///////////////////////////////////////////////////////////////////////

        internal static string StringFromUtf8IntPtr(
            IntPtr pValue,
            int length
            )
        {
            if (pValue == IntPtr.Zero)
                return null;

            if (length > 0)
            {
                byte[] bytes = new byte[length];

                Marshal.Copy(pValue, bytes, 0, length);

                return GetStringFromUtf8Bytes(bytes);
            }

            return String.Empty;
        }

        ///////////////////////////////////////////////////////////////////////

        internal static IntPtr Utf8IntPtrFromString(
            string value
            )
        {
            if (value == null)
                return IntPtr.Zero;

            IntPtr result = IntPtr.Zero;
            byte[] bytes = GetUtf8BytesFromString(value);

            if (bytes == null)
                return IntPtr.Zero;

            int length = bytes.Length;

            result = Allocate(length + 1);

            if (result == IntPtr.Zero)
                return IntPtr.Zero;

            Marshal.Copy(bytes, 0, result, length);
            Marshal.WriteByte(result, length, 0);

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region UTF-8 String Array Helper Methods
        internal static string[] StringArrayFromUtf8IntPtrArray(
            IntPtr[] pValues
            )
        {
            if (pValues == null)
                return null;

            string[] result = new string[pValues.Length];

            for (int index = 0; index < result.Length; index++)
                result[index] = StringFromUtf8IntPtr(pValues[index]);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        internal static IntPtr[] Utf8IntPtrArrayFromStringArray(
            string[] values
            )
        {
            if (values == null)
                return null;

            IntPtr[] result = new IntPtr[values.Length];

            for (int index = 0; index < result.Length; index++)
                result[index] = Utf8IntPtrFromString(values[index]);

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region SQLiteValue Helper Methods
        internal static SQLiteValue[] ValueArrayFromSizeAndIntPtr(
            int nData,
            IntPtr apData
            )
        {
            if (nData < 0)
                return null;

            if (apData == IntPtr.Zero)
                return null;

            SQLiteValue[] result = new SQLiteValue[nData];

            for (int index = 0, offset = 0;
                    index < result.Length;
                    index++, offset += IntPtr.Size)
            {
                IntPtr pData = ReadIntPtr(apData, offset);

                result[index] = (pData != IntPtr.Zero) ?
                    new SQLiteValue(pData) : null;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        internal static SQLiteValue[] ValueArrayFromIntPtrArray(
            IntPtr[] values
            )
        {
            if (values == null)
                return null;

            SQLiteValue[] result = new SQLiteValue[values.Length];

            for (int index = 0; index < result.Length; index++)
                result[index] = new SQLiteValue(values[index]);

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region SQLiteIndex Helper Methods
        internal static void IndexFromIntPtr(
            IntPtr pIndex,
            ref SQLiteIndex index
            )
        {
            if (pIndex == IntPtr.Zero)
                return;

            int offset = 0;

            int nConstraint = ReadInt32(pIndex, offset);

            offset += sizeof(int);

            IntPtr pConstraint = ReadIntPtr(pIndex, offset);

            offset += IntPtr.Size;

            int nOrderBy = ReadInt32(pIndex, offset);

            index = new SQLiteIndex(nConstraint, nOrderBy);

            offset += sizeof(int);

            IntPtr pOrderBy = ReadIntPtr(pIndex, offset);

            offset += IntPtr.Size;

            IntPtr pConstraintUsage = ReadIntPtr(pIndex, offset);

            offset += IntPtr.Size;

            index.Outputs.IdxNum = ReadInt32(pIndex, offset);

            offset += sizeof(int);

            index.Outputs.IdxStr = StringFromUtf8IntPtr(IntPtrForOffset(
                pIndex, offset));

            offset += IntPtr.Size;

            index.Outputs.NeedToFreeIdxStr = ReadInt32(pIndex, offset);

            offset += sizeof(int);

            index.Outputs.OrderByConsumed = ReadInt32(pIndex, offset);

            offset += sizeof(int);

            index.Outputs.EstimatedCost = ReadDouble(pIndex, offset);

            int sizeOfConstraintType = Marshal.SizeOf(typeof(
                UnsafeNativeMethods.sqlite3_index_constraint));

            for (int iConstraint = 0; iConstraint < nConstraint; iConstraint++)
            {
                UnsafeNativeMethods.sqlite3_index_constraint constraint =
                    new UnsafeNativeMethods.sqlite3_index_constraint();

                Marshal.PtrToStructure(IntPtrForOffset(pConstraint,
                    iConstraint * sizeOfConstraintType), constraint);

                index.Inputs.Constraints[iConstraint] =
                    new SQLiteIndexConstraint(constraint);
            }

            int sizeOfOrderByType = Marshal.SizeOf(typeof(
                UnsafeNativeMethods.sqlite3_index_orderby));

            for (int iOrderBy = 0; iOrderBy < nOrderBy; iOrderBy++)
            {
                UnsafeNativeMethods.sqlite3_index_orderby orderBy =
                    new UnsafeNativeMethods.sqlite3_index_orderby();

                Marshal.PtrToStructure(IntPtrForOffset(pOrderBy,
                    iOrderBy * sizeOfOrderByType), orderBy);

                index.Inputs.OrderBys[iOrderBy] =
                    new SQLiteIndexOrderBy(orderBy);
            }

            int sizeOfConstraintUsageType = Marshal.SizeOf(typeof(
                UnsafeNativeMethods.sqlite3_index_constraint_usage));

            for (int iConstraint = 0; iConstraint < nConstraint; iConstraint++)
            {
                UnsafeNativeMethods.sqlite3_index_constraint_usage constraintUsage =
                    new UnsafeNativeMethods.sqlite3_index_constraint_usage();

                Marshal.PtrToStructure(IntPtrForOffset(pConstraintUsage,
                    iConstraint * sizeOfConstraintUsageType), constraintUsage);

                index.Outputs.ConstraintUsages[iConstraint] =
                    new SQLiteIndexConstraintUsage(constraintUsage);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        internal static void IndexToIntPtr(
            SQLiteIndex index,
            IntPtr pIndex
            )
        {
            if ((index == null) || (index.Inputs == null) ||
                (index.Inputs.Constraints == null) ||
                (index.Inputs.OrderBys == null) || (index.Outputs == null) ||
                (index.Outputs.ConstraintUsages == null))
            {
                return;
            }

            if (pIndex == IntPtr.Zero)
                return;

            int offset = 0;

            int nConstraint = ReadInt32(pIndex, offset);

            if (nConstraint != index.Inputs.Constraints.Length)
                return;

            if (nConstraint != index.Outputs.ConstraintUsages.Length)
                return;

            offset += sizeof(int);

            IntPtr pConstraint = ReadIntPtr(pIndex, offset);

            offset += IntPtr.Size;

            int nOrderBy = ReadInt32(pIndex, offset);

            index = new SQLiteIndex(nConstraint, nOrderBy);

            offset += sizeof(int);

            IntPtr pOrderBy = ReadIntPtr(pIndex, offset);

            offset += IntPtr.Size;

            IntPtr pConstraintUsage = ReadIntPtr(pIndex, offset);

            int sizeOfConstraintType = Marshal.SizeOf(typeof(
                UnsafeNativeMethods.sqlite3_index_constraint));

            for (int iConstraint = 0; iConstraint < nConstraint; iConstraint++)
            {
                UnsafeNativeMethods.sqlite3_index_constraint constraint =
                    new UnsafeNativeMethods.sqlite3_index_constraint(
                        index.Inputs.Constraints[iConstraint]);

                Marshal.StructureToPtr(
                    constraint, IntPtrForOffset(pConstraint,
                    iConstraint * sizeOfConstraintType), false);

                index.Inputs.Constraints[iConstraint] =
                    new SQLiteIndexConstraint(constraint);
            }

            int sizeOfOrderByType = Marshal.SizeOf(typeof(
                UnsafeNativeMethods.sqlite3_index_orderby));

            for (int iOrderBy = 0; iOrderBy < nOrderBy; iOrderBy++)
            {
                UnsafeNativeMethods.sqlite3_index_orderby orderBy =
                    new UnsafeNativeMethods.sqlite3_index_orderby(
                        index.Inputs.OrderBys[iOrderBy]);

                Marshal.StructureToPtr(
                    orderBy, IntPtrForOffset(pOrderBy,
                    iOrderBy * sizeOfOrderByType), false);

                index.Inputs.OrderBys[iOrderBy] =
                    new SQLiteIndexOrderBy(orderBy);
            }

            int sizeOfConstraintUsageType = Marshal.SizeOf(typeof(
                UnsafeNativeMethods.sqlite3_index_constraint_usage));

            for (int iConstraint = 0; iConstraint < nConstraint; iConstraint++)
            {
                UnsafeNativeMethods.sqlite3_index_constraint_usage constraintUsage =
                    new UnsafeNativeMethods.sqlite3_index_constraint_usage(
                        index.Outputs.ConstraintUsages[iConstraint]);

                Marshal.StructureToPtr(
                    constraintUsage, IntPtrForOffset(pConstraintUsage,
                    iConstraint * sizeOfConstraintUsageType), false);

                index.Outputs.ConstraintUsages[iConstraint] =
                    new SQLiteIndexConstraintUsage(constraintUsage);
            }
        }
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteModuleBase Class
    public abstract class SQLiteModuleBase :
            ISQLiteManagedModule, ISQLiteNativeModule,  IDisposable
    {
        #region Private Data
        private UnsafeNativeMethods.sqlite3_module nativeModule;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Methods
        internal UnsafeNativeMethods.sqlite3_module GetNativeModule()
        {
            return nativeModule;
        }

        ///////////////////////////////////////////////////////////////////////

        internal UnsafeNativeMethods.sqlite3_module CreateNativeModule()
        {
            if (nativeModule.iVersion != 0)
                return nativeModule;

            nativeModule = new UnsafeNativeMethods.sqlite3_module();
            nativeModule.iVersion = 2;
            nativeModule.xCreate = new UnsafeNativeMethods.xCreate(xCreate);
            nativeModule.xConnect = new UnsafeNativeMethods.xConnect(xConnect);
            nativeModule.xBestIndex = new UnsafeNativeMethods.xBestIndex(xBestIndex);
            nativeModule.xDisconnect = new UnsafeNativeMethods.xDisconnect(xDisconnect);
            nativeModule.xDestroy = new UnsafeNativeMethods.xDestroy(xDestroy);
            nativeModule.xOpen = new UnsafeNativeMethods.xOpen(xOpen);
            nativeModule.xClose = new UnsafeNativeMethods.xClose(xClose);
            nativeModule.xFilter = new UnsafeNativeMethods.xFilter(xFilter);
            nativeModule.xNext = new UnsafeNativeMethods.xNext(xNext);
            nativeModule.xEof = new UnsafeNativeMethods.xEof(xEof);
            nativeModule.xColumn = new UnsafeNativeMethods.xColumn(xColumn);
            nativeModule.xRowId = new UnsafeNativeMethods.xRowId(xRowId);
            nativeModule.xUpdate = new UnsafeNativeMethods.xUpdate(xUpdate);
            nativeModule.xBegin = new UnsafeNativeMethods.xBegin(xBegin);
            nativeModule.xSync = new UnsafeNativeMethods.xSync(xSync);
            nativeModule.xCommit = new UnsafeNativeMethods.xCommit(xCommit);
            nativeModule.xRollback = new UnsafeNativeMethods.xRollback(xRollback);
            nativeModule.xFindFunction = new UnsafeNativeMethods.xFindFunction(xFindFunction);
            nativeModule.xRename = new UnsafeNativeMethods.xRename(xRename);
            nativeModule.xSavepoint = new UnsafeNativeMethods.xSavepoint(xSavepoint);
            nativeModule.xRelease = new UnsafeNativeMethods.xRelease(xRelease);
            nativeModule.xRollbackTo = new UnsafeNativeMethods.xRollbackTo(xRollbackTo);

            return nativeModule;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public SQLiteModuleBase(string name)
        {
            this.name = name;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Members
        protected virtual IntPtr AllocateTable()
        {
            int size = Marshal.SizeOf(typeof(
                UnsafeNativeMethods.sqlite3_vtab));

            return SQLiteMarshal.Allocate(size);
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual void FreeTable(IntPtr pVtab)
        {
            SQLiteMarshal.Free(pVtab);
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual IntPtr AllocateCursor()
        {
            int size = Marshal.SizeOf(typeof(
                UnsafeNativeMethods.sqlite3_vtab_cursor));

            return SQLiteMarshal.Allocate(size);
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual IntPtr GetTableFromCursor(
            IntPtr pCursor
            )
        {
            if (pCursor == IntPtr.Zero)
                return IntPtr.Zero;

            return Marshal.ReadIntPtr(pCursor);
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual SQLiteVirtualTableCursor MarshalCursorFromIntPtr(
            IntPtr pCursor
            )
        {
            if (pCursor == IntPtr.Zero)
                return null;

            SQLiteVirtualTableCursor result =
                new SQLiteVirtualTableCursor(pCursor);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual IntPtr MarshalCursorToIntPtr(
            SQLiteVirtualTableCursor cursor
            )
        {
            if (cursor == null)
                return IntPtr.Zero;

            IntPtr result = IntPtr.Zero;
            bool success = false;

            try
            {
                result = AllocateCursor();

                if (result != IntPtr.Zero)
                    success = true;
            }
            finally
            {
                if (!success && (result != IntPtr.Zero))
                {
                    FreeCursor(result);
                    result = IntPtr.Zero;
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual void FreeCursor(IntPtr pCursor)
        {
            SQLiteMarshal.Free(pCursor);
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual SQLiteErrorCode DeclareTable(
            SQLiteConnection connection,
            string sql,
            ref string error
            )
        {
            if (connection == null)
            {
                error = "invalid connection";
                return SQLiteErrorCode.Error;
            }

            SQLiteBase sqliteBase = connection._sql;

            if (sqliteBase == null)
            {
                error = "connection has invalid handle";
                return SQLiteErrorCode.Error;
            }

            return sqliteBase.DeclareVirtualTable(this, sql, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual bool SetTableError(
            IntPtr pVtab,
            string error
            )
        {
            if (pVtab == IntPtr.Zero)
                return false;

            int offset = Marshal.SizeOf(typeof(
                UnsafeNativeMethods.sqlite3_module)) + sizeof(int);

            IntPtr pError = SQLiteMarshal.ReadIntPtr(pVtab, offset);

            if (pError != IntPtr.Zero)
            {
                SQLiteMarshal.Free(pError); pError = IntPtr.Zero;
                SQLiteMarshal.WriteIntPtr(pVtab, offset, pError);
            }

            SQLiteMarshal.WriteIntPtr(pVtab, offset,
                SQLiteMarshal.Utf8IntPtrFromString(error));

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISQLiteNativeModule Members
        public SQLiteErrorCode xCreate(
            IntPtr pDb,
            IntPtr pAux,
            int argc,
            IntPtr[] argv,
            ref IntPtr pVtab,
            ref IntPtr pError
            )
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(
                        pDb, false))
                {
                    string error = null;

                    if (Create(connection, pAux,
                            SQLiteMarshal.StringArrayFromUtf8IntPtrArray(argv),
                            ref error) == SQLiteErrorCode.Ok)
                    {
                        pVtab = AllocateTable();
                        return SQLiteErrorCode.Ok;
                    }
                    else
                    {
                        pError = SQLiteMarshal.Utf8IntPtrFromString(error);
                    }
                }
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                pError = SQLiteMarshal.Utf8IntPtrFromString(e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xConnect(
            IntPtr pDb,
            IntPtr pAux,
            int argc,
            IntPtr[] argv,
            ref IntPtr pVtab,
            ref IntPtr pError
            )
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(
                        pDb, false))
                {
                    string error = null;

                    if (Connect(connection, pAux,
                            SQLiteMarshal.StringArrayFromUtf8IntPtrArray(argv),
                            ref error) == SQLiteErrorCode.Ok)
                    {
                        pVtab = AllocateTable();
                        return SQLiteErrorCode.Ok;
                    }
                    else
                    {
                        pError = SQLiteMarshal.Utf8IntPtrFromString(error);
                    }
                }
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                pError = SQLiteMarshal.Utf8IntPtrFromString(e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xBestIndex(
            IntPtr pVtab,
            IntPtr pIndex
            )
        {
            try
            {
                SQLiteIndex index = null;

                SQLiteMarshal.IndexFromIntPtr(pIndex, ref index);

                if (BestIndex(index) == SQLiteErrorCode.Ok)
                {
                    SQLiteMarshal.IndexToIntPtr(index, pIndex);
                    return SQLiteErrorCode.Ok;
                }
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xDisconnect(
            IntPtr pVtab
            )
        {
            try
            {
                if (Disconnect() == SQLiteErrorCode.Ok)
                    return SQLiteErrorCode.Ok;
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                //
                // NOTE: At this point, there is no way to report the error
                //       condition back to the caller; therefore, use the
                //       logging facility instead.
                //
                try
                {
                    SQLiteLog.LogMessage(SQLiteBase.COR_E_EXCEPTION,
                        String.Format(CultureInfo.CurrentCulture,
                        "Caught exception in \"xDisconnect\" method: {0}",
                        e)); /* throw */
                }
                catch
                {
                    // do nothing.
                }
            }
            finally
            {
                FreeTable(pVtab);
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xDestroy(
            IntPtr pVtab
            )
        {
            try
            {
                if (Destroy() == SQLiteErrorCode.Ok)
                    return SQLiteErrorCode.Ok;
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                //
                // NOTE: At this point, there is no way to report the error
                //       condition back to the caller; therefore, use the
                //       logging facility instead.
                //
                try
                {
                    SQLiteLog.LogMessage(SQLiteBase.COR_E_EXCEPTION,
                        String.Format(CultureInfo.CurrentCulture,
                        "Caught exception in \"xDestroy\" method: {0}",
                        e)); /* throw */
                }
                catch
                {
                    // do nothing.
                }
            }
            finally
            {
                FreeTable(pVtab);
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xOpen(
            IntPtr pVtab,
            ref IntPtr pCursor
            )
        {
            try
            {
                SQLiteVirtualTableCursor cursor = null;

                if (Open(ref cursor) == SQLiteErrorCode.Ok)
                {
                    if (cursor != null)
                    {
                        pCursor = MarshalCursorToIntPtr(cursor);
                        return SQLiteErrorCode.Ok;
                    }
                    else
                    {
                        SetTableError(pVtab, "no cursor was created");
                    }
                }
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }
            finally
            {
                FreeCursor(pCursor);
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xClose(
            IntPtr pCursor
            )
        {
            IntPtr pVtab = IntPtr.Zero;

            try
            {
                pVtab = GetTableFromCursor(pCursor);

                SQLiteVirtualTableCursor cursor = MarshalCursorFromIntPtr(
                    pCursor);

                if (Close(cursor) == SQLiteErrorCode.Ok)
                    return SQLiteErrorCode.Ok;
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }
            finally
            {
                FreeCursor(pCursor);
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xFilter(
            IntPtr pCursor,
            int idxNum,
            IntPtr idxStr,
            int argc,
            IntPtr[] argv
            )
        {
            IntPtr pVtab = IntPtr.Zero;

            try
            {
                pVtab = GetTableFromCursor(pCursor);

                SQLiteVirtualTableCursor cursor = MarshalCursorFromIntPtr(
                    pCursor);

                if (Filter(
                        cursor, idxNum, SQLiteMarshal.StringFromUtf8IntPtr(idxStr),
                        SQLiteMarshal.ValueArrayFromIntPtrArray(argv)) == SQLiteErrorCode.Ok)
                {
                    return SQLiteErrorCode.Ok;
                }
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xNext(
            IntPtr pCursor
            )
        {
            IntPtr pVtab = IntPtr.Zero;

            try
            {
                pVtab = GetTableFromCursor(pCursor);

                SQLiteVirtualTableCursor cursor = MarshalCursorFromIntPtr(
                    pCursor);

                if (Next(cursor) == SQLiteErrorCode.Ok)
                    return SQLiteErrorCode.Ok;
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public int xEof(
            IntPtr pCursor
            )
        {
            IntPtr pVtab = IntPtr.Zero;

            try
            {
                pVtab = GetTableFromCursor(pCursor);

                SQLiteVirtualTableCursor cursor = MarshalCursorFromIntPtr(
                    pCursor);

                return Eof(cursor) ? 1 : 0;
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return 1;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xColumn(
            IntPtr pCursor,
            IntPtr pContext,
            int index
            )
        {
            IntPtr pVtab = IntPtr.Zero;

            try
            {
                pVtab = GetTableFromCursor(pCursor);

                SQLiteVirtualTableCursor cursor = MarshalCursorFromIntPtr(
                    pCursor);

                SQLiteContext context = new SQLiteContext(pContext);

                return Column(cursor, context, index);
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xRowId(
            IntPtr pCursor,
            ref long rowId
            )
        {
            IntPtr pVtab = IntPtr.Zero;

            try
            {
                pVtab = GetTableFromCursor(pCursor);

                SQLiteVirtualTableCursor cursor = MarshalCursorFromIntPtr(
                    pCursor);

                return RowId(cursor, ref rowId);
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xUpdate(
            IntPtr pVtab,
            int nData,
            IntPtr apData,
            ref long rowId
            )
        {
            try
            {
                SQLiteValue[] values = SQLiteMarshal.ValueArrayFromSizeAndIntPtr(
                    nData, apData);

                return Update(values, ref rowId);
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xBegin(
            IntPtr pVtab
            )
        {
            try
            {
                return Begin();
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xSync(
            IntPtr pVtab
            )
        {
            try
            {
                return Sync();
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xCommit(
            IntPtr pVtab
            )
        {
            try
            {
                return Commit();
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xRollback(
            IntPtr pVtab
            )
        {
            try
            {
                return Rollback();
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public int xFindFunction(
            IntPtr pVtab,
            int nArg,
            IntPtr zName,
            ref SQLiteCallback callback,
            ref IntPtr pClientData
            )
        {
            try
            {
                SQLiteFunction function = null;

                if (FindFunction(
                        nArg, SQLiteMarshal.StringFromUtf8IntPtr(zName),
                        ref function, ref pClientData))
                {
                    if (function != null)
                    {
                        callback = function.ScalarCallback;
                        return 1;
                    }
                    else
                    {
                        SetTableError(pVtab, "no function was created");
                    }
                }
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xRename(
            IntPtr pVtab,
            IntPtr zNew
            )
        {
            try
            {
                return Rename(SQLiteMarshal.StringFromUtf8IntPtr(zNew));
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xSavepoint(
            IntPtr pVtab,
            int iSavepoint
            )
        {
            try
            {
                return Savepoint(iSavepoint);
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xRelease(
            IntPtr pVtab,
            int iSavepoint
            )
        {
            try
            {
                return Release(iSavepoint);
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xRollbackTo(
            IntPtr pVtab,
            int iSavepoint
            )
        {
            try
            {
                return RollbackTo(iSavepoint);
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                SetTableError(pVtab, e.ToString());
            }

            return SQLiteErrorCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISQLiteManagedModule Members
        private bool declared;
        public bool Declared
        {
            get { return declared; }
            internal set { declared = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string name;
        public string Name
        {
            get { return name; }
        }

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Create(
            SQLiteConnection connection,
            IntPtr pClientData,
            string[] argv,
            ref string error
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Connect(
            SQLiteConnection connection,
            IntPtr pClientData,
            string[] argv,
            ref string error
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode BestIndex(
            SQLiteIndex index
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Disconnect();

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Destroy();

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Open(
            ref SQLiteVirtualTableCursor cursor
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Close(
            SQLiteVirtualTableCursor cursor
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Filter(
            SQLiteVirtualTableCursor cursor,
            int idxNum,
            string idxStr,
            SQLiteValue[] values
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Next(
            SQLiteVirtualTableCursor cursor
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract bool Eof(
            SQLiteVirtualTableCursor cursor
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Column(
            SQLiteVirtualTableCursor cursor,
            SQLiteContext context,
            int index
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode RowId(
            SQLiteVirtualTableCursor cursor,
            ref long rowId
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Update(
            SQLiteValue[] values,
            ref long rowId
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Begin();

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Sync();

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Commit();

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Rollback();

        ///////////////////////////////////////////////////////////////////////

        public abstract bool FindFunction(
            int nArg,
            string zName,
            ref SQLiteFunction function,
            ref IntPtr pClientData
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Rename(
            string zNew
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Savepoint(
            int iSavepoint
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode Release(
            int iSavepoint
            );

        ///////////////////////////////////////////////////////////////////////

        public abstract SQLiteErrorCode RollbackTo(
            int iSavepoint
            );
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed)
                throw new ObjectDisposedException(typeof(SQLiteModuleBase).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                //if (disposing)
                //{
                //    ////////////////////////////////////
                //    // dispose managed resources here...
                //    ////////////////////////////////////
                //}

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~SQLiteModuleBase()
        {
            Dispose(false);
        }
        #endregion
    }
    #endregion
}
