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
    public sealed class SQLiteContext
    {

    }

    ///////////////////////////////////////////////////////////////////////////

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
            return UnsafeNativeMethods.sqlite3_value_int64(pValue);
        }

        ///////////////////////////////////////////////////////////////////////

        public double GetDouble()
        {
            if (pValue == IntPtr.Zero) return default(double);
            return UnsafeNativeMethods.sqlite3_value_double(pValue);
        }

        ///////////////////////////////////////////////////////////////////////

        public string GetString()
        {
            if (pValue == IntPtr.Zero) return null;
            return SQLiteModuleBase.StringFromUtf8IntPtr(pValue, GetBytes());
        }

        ///////////////////////////////////////////////////////////////////////

        public byte[] GetBlob()
        {
            if (pValue == IntPtr.Zero) return null;
            return SQLiteModuleBase.BytesFromIntPtr(pValue, GetBytes());
        }
        #endregion
    }

    ///////////////////////////////////////////////////////////////////////////

    public class SQLiteIndexConstraint
    {
        private SQLiteModuleBase.UnsafeNativeMethods2.sqlite3_index_constraint constraint;
    }

    ///////////////////////////////////////////////////////////////////////////

    public class SQLiteIndexOrderBy
    {
        private SQLiteModuleBase.UnsafeNativeMethods2.sqlite3_index_orderby orderBy;
    }

    ///////////////////////////////////////////////////////////////////////////

    public class SQLiteIndexConstraintUsage
    {
        private SQLiteModuleBase.UnsafeNativeMethods2.sqlite3_index_constraint_usage constraintUsage;
    }

    ///////////////////////////////////////////////////////////////////////////

    public class SQLiteIndex
    {
        SQLiteIndexConstraint[] Constraints;
        SQLiteIndexOrderBy[] OrderBys;


        SQLiteIndexConstraintUsage[] ConstraintUsages;

        int idxNum;           /* Number used to identify the index */
        string idxStr;        /* String, possibly obtained from sqlite3_malloc */
        int needToFreeIdxStr; /* Free idxStr using sqlite3_free() if true */
        int orderByConsumed;  /* True if output is already ordered */
        double estimatedCost; /* Estimated cost of using this index */
    }

    ///////////////////////////////////////////////////////////////////////////

    public class SQLiteVirtualTableCursor
    {
        internal SQLiteModuleBase.UnsafeNativeMethods2.sqlite3_vtab_cursor cursor;
    }

    ///////////////////////////////////////////////////////////////////////////

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int xFunc(
        IntPtr pContext,
        int argc,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]
        IntPtr[] argv
    );

    ///////////////////////////////////////////////////////////////////////////

    public interface ISQLiteNativeModule
    {
        SQLiteErrorCode xCreate(IntPtr pDb, IntPtr pAux, int argc, ref IntPtr[] argv, ref IntPtr pVtab, ref IntPtr pError);
        SQLiteErrorCode xConnect(IntPtr pDb, IntPtr pAux, int argc, ref IntPtr[] argv, ref IntPtr pVtab, ref IntPtr pError);
        SQLiteErrorCode xBestIndex(IntPtr pVtab, IntPtr index);
        SQLiteErrorCode xDisconnect(IntPtr pVtab);
        SQLiteErrorCode xDestroy(IntPtr pVtab);
        SQLiteErrorCode xOpen(IntPtr pVtab, ref IntPtr pCursor);
        SQLiteErrorCode xClose(IntPtr pCursor);
        SQLiteErrorCode xFilter(IntPtr pCursor, int idxNum, IntPtr idxStr, int argc, IntPtr[] argv);
        SQLiteErrorCode xNext(IntPtr pCursor);
        SQLiteErrorCode xEof(IntPtr pCursor);
        SQLiteErrorCode xColumn(IntPtr pCursor, IntPtr pContext, int index);
        SQLiteErrorCode xRowId(IntPtr pCursor, ref long rowId);
        SQLiteErrorCode xUpdate(IntPtr pVtab, int nData, ref IntPtr apData, ref long rowId);
        SQLiteErrorCode xBegin(IntPtr pVtab);
        SQLiteErrorCode xSync(IntPtr pVtab);
        SQLiteErrorCode xCommit(IntPtr pVtab);
        SQLiteErrorCode xRollback(IntPtr pVtab);
        SQLiteErrorCode xFindFunction(IntPtr pVtab, int nArg, IntPtr zName, ref xFunc pxFunc, ref IntPtr ppArg);
        SQLiteErrorCode xRename(IntPtr pVtab, IntPtr zNew);
        SQLiteErrorCode xSavepoint(IntPtr pVtab, int iSavepoint);
        SQLiteErrorCode xRelease(IntPtr pVtab, int iSavepoint);
        SQLiteErrorCode xRollbackTo(IntPtr pVtab, int iSavepoint);
    }

    ///////////////////////////////////////////////////////////////////////////

    public interface ISQLiteManagedModule
    {
        bool Declared { get; }

        SQLiteErrorCode Create(SQLiteConnection connection, IntPtr pClientData, string[] argv, ref string error);
        SQLiteErrorCode Connect(SQLiteConnection connection, IntPtr pClientData, string[] argv, ref string error);
        SQLiteErrorCode BestIndex(ref SQLiteIndex index);
        SQLiteErrorCode Disconnect();
        SQLiteErrorCode Destroy();
        SQLiteErrorCode Open(ref SQLiteVirtualTableCursor cursor);
        SQLiteErrorCode Close(SQLiteVirtualTableCursor cursor);
        SQLiteErrorCode Filter(SQLiteVirtualTableCursor cursor, int idxNum, string idxStr, SQLiteValue[] argv);
        SQLiteErrorCode Next(SQLiteVirtualTableCursor cursor);
        SQLiteErrorCode Eof(SQLiteVirtualTableCursor cursor);
        SQLiteErrorCode Column(SQLiteVirtualTableCursor cursor, SQLiteContext context, int index);
        SQLiteErrorCode RowId(SQLiteVirtualTableCursor cursor, ref long rowId);
        SQLiteErrorCode Update(SQLiteValue[] values, ref long rowId);
        SQLiteErrorCode Begin();
        SQLiteErrorCode Sync();
        SQLiteErrorCode Commit();
        SQLiteErrorCode Rollback();
        SQLiteErrorCode FindFunction(string zName, ref SQLiteFunction function, object[] args);
        SQLiteErrorCode Rename(string zNew);
        SQLiteErrorCode Savepoint(int iSavepoint);
        SQLiteErrorCode Release(int iSavepoint);
        SQLiteErrorCode RollbackTo(int iSavepoint);
    }

    ///////////////////////////////////////////////////////////////////////////

#if NET_40
    [SecurityCritical()]
#else
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
    public abstract class SQLiteModuleBase : ISQLiteManagedModule, ISQLiteNativeModule,  IDisposable
    {
        private static Encoding Utf8Encoding = Encoding.UTF8;

        #region Unsafe Native Methods Class
        [SuppressUnmanagedCodeSecurity()]
        internal static class UnsafeNativeMethods2
        {
            // https://www.sqlite.org/vtab.html


            [StructLayout(LayoutKind.Sequential)]
            public struct sqlite3_module
            {
                public int iVersion;
                public xCreate xCreate;
                public xConnect xConnect;
                public xBestIndex xBestIndex;
                public xDisconnect xDisconnect;
                public xDestroy xDestroy;
                public xOpen xOpen;
                public xClose xClose;
                public xFilter xFilter;
                public xNext xNext;
                public xEof xEof;
                public xColumn xColumn;
                public xRowId xRowId;
                public xUpdate xUpdate;
                public xBegin xBegin;
                public xSync xSync;
                public xCommit xCommit;
                public xRollback xRollback;
                public xFindFunction xFindFunction;
                public xRename xRename;
                /* The methods above are in version 1 of the sqlite3_module
                 * object.  Those below are for version 2 and greater. */
                public xSavepoint xSavepoint;
                public xRelease xRelease;
                public xRollbackTo xRollbackTo;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct sqlite3_vtab
            {
                [MarshalAs(UnmanagedType.LPStruct)]
                sqlite3_module pModule;
                int nRef; /* NO LONGER USED */
                IntPtr zErrMsg;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct sqlite3_vtab_cursor
            {
                [MarshalAs(UnmanagedType.LPStruct)]
                sqlite3_vtab pVTab;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct sqlite3_index_constraint
            {
                int iColumn;
                byte op;
                byte usable;
                int iTermOffset;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct sqlite3_index_orderby
            {
                int iColumn; /* Column number */
                byte desc;   /* True for DESC.  False for ASC. */
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct sqlite3_index_constraint_usage
            {
                int argvIndex; /* if >0, constraint is part of argv to xFilter */
                byte omit;     /* Do not code a test for this constraint */
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct sqlite3_index_info
            {
                /* Inputs */
                int nConstraint;           /* Number of entries in aConstraint */
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]
                sqlite3_index_constraint[] aConstraint;
                int nOrderBy;
                sqlite3_index_orderby[] aOrderBy;
                /* Outputs */
                sqlite3_index_constraint_usage[] aConstraintUsage;
                int idxNum;                /* Number used to identify the index */
                string idxStr;              /* String, possibly obtained from sqlite3_malloc */
                int needToFreeIdxStr;      /* Free idxStr using sqlite3_free() if true */
                int orderByConsumed;       /* True if output is already ordered */
                double estimatedCost;      /* Estimated cost of using this index */
            }





            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate SQLiteErrorCode xCreate(
                IntPtr pDb,
                IntPtr pAux,
                int argc,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
                ref IntPtr[] argv,
                ref IntPtr pVtab,
                ref IntPtr pError
            );

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate SQLiteErrorCode xConnect(
                IntPtr pDb,
                IntPtr pAux,
                int argc,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
                ref IntPtr[] argv,
                ref IntPtr pVtab,
                ref IntPtr pError
            );

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate SQLiteErrorCode xBestIndex(
                IntPtr pVtab,
                IntPtr index
            );

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate SQLiteErrorCode xDisconnect(
                IntPtr pVtab
            );

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate SQLiteErrorCode xDestroy(
                IntPtr pVtab
            );

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate SQLiteErrorCode xOpen(
                IntPtr pVtab,
                ref IntPtr pCursor
            );

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate SQLiteErrorCode xClose(
                IntPtr pCursor
            );

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate SQLiteErrorCode xFilter(
                IntPtr pCursor,
                int idxNum,
                IntPtr idxStr,
                int argc,
                IntPtr[] argv
            );

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate SQLiteErrorCode xNext(
                IntPtr pCursor
            );

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate SQLiteErrorCode xEof(
                IntPtr pCursor
            );

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate SQLiteErrorCode xColumn(
                IntPtr pCursor,
                IntPtr pContext,
                int index
            );

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate SQLiteErrorCode xRowId(
                IntPtr pCursor,
                ref long rowId
            );

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate SQLiteErrorCode xUpdate(
                IntPtr pVtab,
                int nData,
                ref IntPtr apData,
                ref long rowId
            );

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate SQLiteErrorCode xBegin(
                IntPtr pVtab
            );

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate SQLiteErrorCode xSync(
                IntPtr pVtab
            );

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate SQLiteErrorCode xCommit(
                IntPtr pVtab
            );

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate SQLiteErrorCode xRollback(
                IntPtr pVtab
            );

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate SQLiteErrorCode xFindFunction(
                IntPtr pVtab,
                int nArg,
                IntPtr zName,
                ref xFunc pxFunc,
                ref IntPtr ppArg
            );

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate SQLiteErrorCode xRename(
                IntPtr pVtab,
                IntPtr zNew
            );

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate SQLiteErrorCode xSavepoint(
                IntPtr pVtab,
                int iSavepoint
            );

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate SQLiteErrorCode xRelease(
                IntPtr pVtab,
                int iSavepoint
            );

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate SQLiteErrorCode xRollbackTo(
                IntPtr pVtab,
                int iSavepoint
            );

            private static readonly int SQLITE_INDEX_CONSTRAINT_EQ = 2;
            private static readonly int SQLITE_INDEX_CONSTRAINT_GT = 4;
            private static readonly int SQLITE_INDEX_CONSTRAINT_LE = 8;
            private static readonly int SQLITE_INDEX_CONSTRAINT_LT = 16;
            private static readonly int SQLITE_INDEX_CONSTRAINT_GE = 32;
            private static readonly int SQLITE_INDEX_CONSTRAINT_MATCH = 64;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private UnsafeNativeMethods2.sqlite3_module CreateNativeModule()
        {
            UnsafeNativeMethods2.sqlite3_module module =
                new UnsafeNativeMethods2.sqlite3_module();

            module.iVersion = 2;
            module.xCreate = new UnsafeNativeMethods2.xCreate(xCreate);
            module.xConnect = new UnsafeNativeMethods2.xConnect(xConnect);
            module.xBestIndex = new UnsafeNativeMethods2.xBestIndex(xBestIndex);
            module.xDisconnect = new UnsafeNativeMethods2.xDisconnect(xDisconnect);
            module.xDestroy = new UnsafeNativeMethods2.xDestroy(xDestroy);
            module.xOpen = new UnsafeNativeMethods2.xOpen(xOpen);
            module.xClose = new UnsafeNativeMethods2.xClose(xClose);
            module.xFilter = new UnsafeNativeMethods2.xFilter(xFilter);
            module.xNext = new UnsafeNativeMethods2.xNext(xNext);
            module.xEof = new UnsafeNativeMethods2.xEof(xEof);
            module.xColumn = new UnsafeNativeMethods2.xColumn(xColumn);
            module.xRowId = new UnsafeNativeMethods2.xRowId(xRowId);
            module.xUpdate = new UnsafeNativeMethods2.xUpdate(xUpdate);
            module.xBegin = new UnsafeNativeMethods2.xBegin(xBegin);
            module.xSync = new UnsafeNativeMethods2.xSync(xSync);
            module.xCommit = new UnsafeNativeMethods2.xCommit(xCommit);
            module.xRollback = new UnsafeNativeMethods2.xRollback(xRollback);
            module.xFindFunction = new UnsafeNativeMethods2.xFindFunction(xFindFunction);
            module.xRename = new UnsafeNativeMethods2.xRename(xRename);
            module.xSavepoint = new UnsafeNativeMethods2.xSavepoint(xSavepoint);
            module.xRelease = new UnsafeNativeMethods2.xRelease(xRelease);
            module.xRollbackTo = new UnsafeNativeMethods2.xRollbackTo(xRollbackTo);

            return module;
        }

        private static int ThirtyBits = 0x3fffffff;

        ///////////////////////////////////////////////////////////////////////

        private static byte[] GetUtf8BytesFromString(string value)
        {
            if (value == null)
                return null;

            return Utf8Encoding.GetBytes(value);
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetStringFromUtf8Bytes(byte[] bytes)
        {
            if (bytes == null)
                return null;

            return Utf8Encoding.GetString(bytes);
        }

        ///////////////////////////////////////////////////////////////////////

        private static IntPtr Allocate(int size)
        {
            return UnsafeNativeMethods.sqlite3_malloc(size);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void Free(IntPtr pMemory)
        {
            UnsafeNativeMethods.sqlite3_free(pMemory);
        }

        ///////////////////////////////////////////////////////////////////////

        internal static byte[] BytesFromIntPtr(IntPtr pValue, int length)
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

        private static int ProbeForUtf8ByteLength(IntPtr pValue, int limit)
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

        internal static string StringFromUtf8IntPtr(IntPtr pValue)
        {
            return StringFromUtf8IntPtr(pValue,
                ProbeForUtf8ByteLength(pValue, ThirtyBits));
        }

        ///////////////////////////////////////////////////////////////////////

        internal static string StringFromUtf8IntPtr(IntPtr pValue, int length)
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

        private static string[] StringArrayFromUtf8IntPtrArray(
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

        internal static IntPtr Utf8IntPtrFromString(string value)
        {
            if (value == null)
                return IntPtr.Zero;

            IntPtr result = IntPtr.Zero;
            byte[] bytes = GetUtf8BytesFromString(value);

            if (bytes == null)
                return IntPtr.Zero;

            int length = bytes.Length;

            result = Allocate(length + 1);

            Marshal.Copy(bytes, 0, result, length);
            Marshal.WriteByte(result, length, 0);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        private static IntPtr[] Utf8IntPtrArrayFromStringArray(
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

        #region Public Constructors
        public SQLiteModuleBase()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Members
        protected virtual IntPtr AllocateTable()
        {
            int size = Marshal.SizeOf(typeof(
                UnsafeNativeMethods2.sqlite3_vtab));

            return Allocate(size);
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual void FreeTable(IntPtr pVtab)
        {
            Free(pVtab);
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual IntPtr AllocateCursor()
        {
            int size = Marshal.SizeOf(typeof(
                UnsafeNativeMethods2.sqlite3_vtab_cursor));

            return Allocate(size);
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

            SQLiteVirtualTableCursor result = new SQLiteVirtualTableCursor();
            Marshal.PtrToStructure(pCursor, result.cursor);

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
                {
                    Marshal.StructureToPtr(cursor.cursor, result, false);
                    success = true;
                }
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



        ///////////////////////////////////////////////////////////////////////

        protected virtual void FreeCursor(IntPtr pCursor)
        {
            Free(pCursor);
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
                UnsafeNativeMethods2.sqlite3_module)) + sizeof(int);

            IntPtr pError = Marshal.ReadIntPtr(pVtab, offset);

            if (pError != IntPtr.Zero)
            {
                Free(pError); pError = IntPtr.Zero;
                Marshal.WriteIntPtr(pVtab, offset, pError);
            }

            Marshal.WriteIntPtr(pVtab, offset, Utf8IntPtrFromString(error));
            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISQLiteNativeModule Members
        public SQLiteErrorCode xCreate(
            IntPtr pDb,
            IntPtr pAux,
            int argc,
            ref IntPtr[] argv,
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
                            StringArrayFromUtf8IntPtrArray(argv),
                            ref error) == SQLiteErrorCode.Ok)
                    {
                        pVtab = AllocateTable();
                        return SQLiteErrorCode.Ok;
                    }
                    else
                    {
                        pError = Utf8IntPtrFromString(error);
                    }
                }
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                pError = Utf8IntPtrFromString(e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xConnect(
            IntPtr pDb,
            IntPtr pAux,
            int argc,
            ref IntPtr[] argv,
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
                            StringArrayFromUtf8IntPtrArray(argv),
                            ref error) == SQLiteErrorCode.Ok)
                    {
                        pVtab = AllocateTable();
                        return SQLiteErrorCode.Ok;
                    }
                    else
                    {
                        pError = Utf8IntPtrFromString(error);
                    }
                }
            }
            catch (Exception e) /* NOTE: Must catch ALL. */
            {
                pError = Utf8IntPtrFromString(e.ToString());
            }

            return SQLiteErrorCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xBestIndex(
            IntPtr pVtab,
            IntPtr index
            )
        {
            return SQLiteErrorCode.Ok;
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
            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xNext(
            IntPtr pCursor
            )
        {
            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xEof(
            IntPtr pCursor
            )
        {
            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xColumn(
            IntPtr pCursor,
            IntPtr pContext,
            int index
            )
        {
            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xRowId(
            IntPtr pCursor,
            ref long rowId
            )
        {
            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xUpdate(
            IntPtr pVtab,
            int nData,
            ref IntPtr apData,
            ref long rowId
            )
        {
            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xBegin(
            IntPtr pVtab
            )
        {
            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xSync(
            IntPtr pVtab
            )
        {
            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xCommit(
            IntPtr pVtab
            )
        {
            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xRollback(
            IntPtr pVtab
            )
        {
            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xFindFunction(
            IntPtr pVtab,
            int nArg,
            IntPtr zName,
            ref xFunc pxFunc,
            ref IntPtr ppArg
            )
        {
            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xRename(
            IntPtr pVtab,
            IntPtr zNew
            )
        {
            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xSavepoint(
            IntPtr pVtab,
            int iSavepoint
            )
        {
            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xRelease(
            IntPtr pVtab,
            int iSavepoint
            )
        {
            return SQLiteErrorCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public SQLiteErrorCode xRollbackTo(
            IntPtr pVtab,
            int iSavepoint
            )
        {
            return SQLiteErrorCode.Ok;
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
            ref SQLiteIndex index
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

        public abstract SQLiteErrorCode Eof(
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

        public abstract SQLiteErrorCode FindFunction(
            string zName,
            ref SQLiteFunction function,
            object[] args
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
}
