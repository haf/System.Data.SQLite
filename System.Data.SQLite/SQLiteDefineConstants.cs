/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 *
 * Released to the public domain, use at your own risk!
 ********************************************************/

using System.Collections.Generic;

namespace System.Data.SQLite
{
    internal static class SQLiteDefineConstants
    {
        public static readonly IList<string> OptionList = new List<string>(new string[] {
#if CHECK_STATE
            "CHECK_STATE",
#endif

#if DEBUG
            "DEBUG",
#endif

#if INTEROP_CODEC
            "INTEROP_CODEC",
#endif

#if INTEROP_EXTENSION_FUNCTIONS
            "INTEROP_EXTENSION_FUNCTIONS",
#endif

#if NET_20
            "NET_20",
#endif

#if NET_COMPACT_20
            "NET_COMPACT_20",
#endif

#if PLATFORM_COMPACTFRAMEWORK
            "PLATFORM_COMPACTFRAMEWORK",
#endif

#if PRELOAD_NATIVE_LIBRARY
            "PRELOAD_NATIVE_LIBRARY",
#endif

#if RETARGETABLE
            "RETARGETABLE",
#endif

#if SQLITE_STANDARD
            "SQLITE_STANDARD",
#endif

#if THROW_ON_DISPOSED
            "THROW_ON_DISPOSED",
#endif

#if TRACE
            "TRACE",
#endif

#if TRACE_CONNECTION
            "TRACE_CONNECTION",
#endif

#if TRACE_HANDLE
            "TRACE_HANDLE",
#endif

#if TRACE_PRELOAD
            "TRACE_PRELOAD",
#endif

#if TRACE_STATEMENT
            "TRACE_STATEMENT",
#endif

#if TRACE_WARNING
            "TRACE_WARNING",
#endif

#if USE_INTEROP_DLL
            "USE_INTEROP_DLL",
#endif

            null
        });
    }
}
