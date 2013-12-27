/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 *
 * Released to the public domain, use at your own risk!
 ********************************************************/

namespace System.Data.SQLite
{
  using System;
  using System.Globalization;

#if !NET_COMPACT_20 && (TRACE_PRELOAD || TRACE_HANDLE)
  using System.Diagnostics;
#endif

#if PRELOAD_NATIVE_LIBRARY
  using System.Collections.Generic;
  using System.IO;
  using System.Reflection;
#endif

#if !PLATFORM_COMPACTFRAMEWORK && !DEBUG
  using System.Security;
#endif

  using System.Runtime.InteropServices;

#if !PLATFORM_COMPACTFRAMEWORK || COUNT_HANDLE
  using System.Threading;
#endif

  using System.Xml;

#if !PLATFORM_COMPACTFRAMEWORK && !DEBUG
  [SuppressUnmanagedCodeSecurity]
#endif
  internal static class UnsafeNativeMethods
  {
      #region Critical Handle Counts (Debug Build Only)
#if COUNT_HANDLE
      //
      // NOTE: These counts represent the total number of outstanding
      //       (non-disposed) CriticalHandle derived object instances
      //       created by this library and are primarily for use by
      //       the test suite.  These counts are incremented by the
      //       associated constructors and are decremented upon the
      //       successful completion of the associated ReleaseHandle
      //       methods.
      //
      internal static int connectionCount;
      internal static int statementCount;
      internal static int backupCount;
#endif
      #endregion

      /////////////////////////////////////////////////////////////////////////

      #region Shared Native SQLite Library Pre-Loading Code
      private static readonly string DllFileExtension = ".dll";
      private static readonly string ConfigFileExtension = ".config";

      /////////////////////////////////////////////////////////////////////////

      //
      // NOTE: This is the name of the XML configuration file specific to the
      //       System.Data.SQLite assembly.
      //
      private static readonly string XmlConfigFileName =
          typeof(UnsafeNativeMethods).Namespace + DllFileExtension +
          ConfigFileExtension;

      /////////////////////////////////////////////////////////////////////////
      /// <summary>
      /// Queries and returns the XML configuration file name for the assembly
      /// containing the managed System.Data.SQLite components.
      /// </summary>
      /// <returns>
      /// The XML configuration file name -OR- null if it cannot be determined
      /// or does not exist.
      /// </returns>
      private static string GetXmlConfigFileName()
      {
          string directory;
          string fileName;

#if !PLATFORM_COMPACTFRAMEWORK
          directory = AppDomain.CurrentDomain.BaseDirectory;
          fileName = Path.Combine(directory, XmlConfigFileName);

          if (File.Exists(fileName))
              return fileName;
#endif

          directory = GetAssemblyDirectory();
          fileName = Path.Combine(directory, XmlConfigFileName);

          if (File.Exists(fileName))
              return fileName;

          return null;
      }

      /////////////////////////////////////////////////////////////////////////
      /// <summary>
      /// Queries and returns the value of the specified setting, using the XML
      /// configuration file and/or the environment variables for the current
      /// process and/or the current system, when available.
      /// </summary>
      /// <param name="name">
      /// The name of the setting.
      /// </param>
      /// <param name="default">
      /// The value to be returned if the setting has not been set explicitly
      /// or cannot be determined.
      /// </param>
      /// <returns>
      /// The value of the setting -OR- the default value specified by
      /// <paramref name="default" /> if it has not been set explicitly or
      /// cannot be determined.  By default, all references to existing
      /// environment variables will be expanded to their corresponding values
      /// within the value to be returned unless either the "No_Expand" or
      /// "No_Expand_<paramref name="name" />" environment variable is set [to
      /// anything].
      /// </returns>
      internal static string GetSettingValue(
          string name,    /* in */
          string @default /* in */
          )
      {
          if (name == null)
              return @default;

          string value = null;

#if !PLATFORM_COMPACTFRAMEWORK
          bool expand = true;

          if (Environment.GetEnvironmentVariable("No_Expand") != null)
          {
              expand = false;
          }
          else if (Environment.GetEnvironmentVariable(String.Format(
                  "No_Expand_{0}", name)) != null)
          {
              expand = false;
          }

          value = Environment.GetEnvironmentVariable(name);

          if (expand && !String.IsNullOrEmpty(value))
              value = Environment.ExpandEnvironmentVariables(value);

          if (value != null)
              return value;
#endif

          try
          {
              string fileName = GetXmlConfigFileName();

              if (fileName == null)
                  return @default;

              XmlDocument document = new XmlDocument();

              document.Load(fileName);

              XmlElement element = document.SelectSingleNode(String.Format(
                  "/configuration/appSettings/add[@key='{0}']", name)) as
                  XmlElement;

              if (element != null)
              {
                  if (element.HasAttribute("value"))
                      value = element.GetAttribute("value");

#if !PLATFORM_COMPACTFRAMEWORK
                  if (expand && !String.IsNullOrEmpty(value))
                      value = Environment.ExpandEnvironmentVariables(value);
#endif

                  if (value != null)
                      return value;
              }
          }
#if !NET_COMPACT_20 && TRACE_SHARED
          catch (Exception e)
#else
          catch (Exception)
#endif
          {
#if !NET_COMPACT_20 && TRACE_SHARED
              try
              {
                  Trace.WriteLine(String.Format(
                      CultureInfo.CurrentCulture,
                      "Native library pre-loader failed to get variable " +
                      "\"{0}\" value: {1}", name, e)); /* throw */
              }
              catch
              {
                  // do nothing.
              }
#endif
          }

          return @default;
      }

      /////////////////////////////////////////////////////////////////////////
      /// <summary>
      /// Queries and returns the directory for the assembly currently being
      /// executed.
      /// </summary>
      /// <returns>
      /// The directory for the assembly currently being executed -OR- null if
      /// it cannot be determined.
      /// </returns>
      private static string GetAssemblyDirectory()
      {
          try
          {
              Assembly assembly = Assembly.GetExecutingAssembly();

              if (assembly == null)
                  return null;

              string fileName;

#if PLATFORM_COMPACTFRAMEWORK
              AssemblyName assemblyName = assembly.GetName();

              if (assemblyName == null)
                  return null;

              fileName = assemblyName.CodeBase;
#else
              fileName = assembly.Location;
#endif

              if (String.IsNullOrEmpty(fileName))
                  return null;

              string directory = Path.GetDirectoryName(fileName);

              if (String.IsNullOrEmpty(directory))
                  return null;

              return directory;
          }
#if !NET_COMPACT_20 && TRACE_SHARED
          catch (Exception e)
#else
          catch (Exception)
#endif
          {
#if !NET_COMPACT_20 && TRACE_SHARED
              try
              {
                  Trace.WriteLine(String.Format(
                      CultureInfo.CurrentCulture,
                      "Native library pre-loader failed to get directory " +
                      "for currently executing assembly: {0}", e)); /* throw */
              }
              catch
              {
                  // do nothing.
              }
#endif
          }

          return null;
      }
      #endregion

      /////////////////////////////////////////////////////////////////////////

      #region Optional Native SQLite Library Pre-Loading Code
      //
      // NOTE: If we are looking for the standard SQLite DLL ("sqlite3.dll"),
      //       the interop DLL ("SQLite.Interop.dll"), or we are running on the
      //       .NET Compact Framework, we should include this code (only if the
      //       feature has actually been enabled).  This code would be totally
      //       redundant if this module has been bundled into the mixed-mode
      //       assembly.
      //
#if SQLITE_STANDARD || USE_INTEROP_DLL || PLATFORM_COMPACTFRAMEWORK

      //
      // NOTE: Only compile in the native library pre-load code if the feature
      //       has been enabled for this build.
      //
#if PRELOAD_NATIVE_LIBRARY
      /// <summary>
      /// The name of the environment variable containing the processor
      /// architecture of the current process.
      /// </summary>
      private static readonly string PROCESSOR_ARCHITECTURE =
          "PROCESSOR_ARCHITECTURE";

      /////////////////////////////////////////////////////////////////////////
      /// <summary>
      /// This is the P/Invoke method that wraps the native Win32 LoadLibrary
      /// function.  See the MSDN documentation for full details on what it
      /// does.
      /// </summary>
      /// <param name="fileName">
      /// The name of the executable library.
      /// </param>
      /// <returns>
      /// The native module handle upon success -OR- IntPtr.Zero on failure.
      /// </returns>
#if !PLATFORM_COMPACTFRAMEWORK
      [DllImport("kernel32",
#else
      [DllImport("coredll",
#endif
          CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto,
#if !PLATFORM_COMPACTFRAMEWORK
          BestFitMapping = false, ThrowOnUnmappableChar = true,
#endif
          SetLastError = true)]
      private static extern IntPtr LoadLibrary(string fileName);

      /////////////////////////////////////////////////////////////////////////

#if PLATFORM_COMPACTFRAMEWORK
      /// <summary>
      /// This is the P/Invoke method that wraps the native Win32 GetSystemInfo
      /// function.  See the MSDN documentation for full details on what it
      /// does.
      /// </summary>
      /// <param name="systemInfo">
      /// The system information structure to be filled in by the function.
      /// </param>
      [DllImport("coredll", CallingConvention = CallingConvention.Winapi)]
      private static extern void GetSystemInfo(out SYSTEM_INFO systemInfo);

      /////////////////////////////////////////////////////////////////////////
      /// <summary>
      /// This enumeration contains the possible values for the processor
      /// architecture field of the system information structure.
      /// </summary>
      private enum ProcessorArchitecture : ushort /* COMPAT: Win32. */
      {
          Intel = 0,
          MIPS = 1,
          Alpha = 2,
          PowerPC = 3,
          SHx = 4,
          ARM = 5,
          IA64 = 6,
          Alpha64 = 7,
          MSIL = 8,
          AMD64 = 9,
          IA32_on_Win64 = 10,
          Unknown = 0xFFFF
      }

      /////////////////////////////////////////////////////////////////////////
      /// <summary>
      /// This structure contains information about the current computer. This
      /// includes the processor type, page size, memory addresses, etc.
      /// </summary>
      [StructLayout(LayoutKind.Sequential)]
      private struct SYSTEM_INFO
      {
          public ProcessorArchitecture wProcessorArchitecture;
          public ushort wReserved; /* NOT USED */
          public uint dwPageSize; /* NOT USED */
          public IntPtr lpMinimumApplicationAddress; /* NOT USED */
          public IntPtr lpMaximumApplicationAddress; /* NOT USED */
          public uint dwActiveProcessorMask; /* NOT USED */
          public uint dwNumberOfProcessors; /* NOT USED */
          public uint dwProcessorType; /* NOT USED */
          public uint dwAllocationGranularity; /* NOT USED */
          public ushort wProcessorLevel; /* NOT USED */
          public ushort wProcessorRevision; /* NOT USED */
      }
#endif

      /////////////////////////////////////////////////////////////////////////
      /// <summary>
      /// This lock is used to protect the static _SQLiteModule and
      /// processorArchitecturePlatforms fields, below.
      /// </summary>
      private static readonly object staticSyncRoot = new object();

      /////////////////////////////////////////////////////////////////////////
      /// <summary>
      /// Stores the mappings between processor architecture names and platform
      /// names.
      /// </summary>
      private static Dictionary<string, string> processorArchitecturePlatforms;

      /////////////////////////////////////////////////////////////////////////
      /// <summary>
      /// The native module file name for the native SQLite library or null.
      /// </summary>
      private static string _SQLiteNativeModuleFileName = null;

      /////////////////////////////////////////////////////////////////////////
      /// <summary>
      /// The native module handle for the native SQLite library or the value
      /// IntPtr.Zero.
      /// </summary>
      private static IntPtr _SQLiteNativeModuleHandle = IntPtr.Zero;

      /////////////////////////////////////////////////////////////////////////
      /// <summary>
      /// For now, this method simply calls the Initialize method.
      /// </summary>
      static UnsafeNativeMethods()
      {
          Initialize();
      }

      /////////////////////////////////////////////////////////////////////////
      /// <summary>
      /// Attempts to initialize this class by pre-loading the native SQLite
      /// library for the processor architecture of the current process.
      /// </summary>
      internal static void Initialize()
      {
          //
          // NOTE: If the "No_PreLoadSQLite" environment variable is set (to
          //       anything), skip all our special code and simply return.
          //
          if (GetSettingValue("No_PreLoadSQLite", null) != null)
              return;

          lock (staticSyncRoot)
          {
              //
              // TODO: Make sure this list is updated if the supported
              //       processor architecture names and/or platform names
              //       changes.
              //
              if (processorArchitecturePlatforms == null)
              {
                  //
                  // NOTE: Create the map of processor architecture names
                  //       to platform names using a case-insensitive string
                  //       comparer.
                  //
                  processorArchitecturePlatforms =
                      new Dictionary<string, string>(
                          StringComparer.OrdinalIgnoreCase);

                  //
                  // NOTE: Setup the list of platform names associated with
                  //       the supported processor architectures.
                  //
                  processorArchitecturePlatforms.Add("x86", "Win32");
                  processorArchitecturePlatforms.Add("AMD64", "x64");
                  processorArchitecturePlatforms.Add("IA64", "Itanium");
                  processorArchitecturePlatforms.Add("ARM", "WinCE");
              }

              //
              // BUGBUG: What about other application domains?
              //
              if (_SQLiteNativeModuleHandle == IntPtr.Zero)
              {
                  string baseDirectory = null;
                  string processorArchitecture = null;

                  /* IGNORED */
                  SearchForDirectory(
                      ref baseDirectory, ref processorArchitecture);

                  //
                  // NOTE: Attempt to pre-load the SQLite core library (or
                  //       interop assembly) and store both the file name
                  //       and native module handle for later usage.
                  //
                  /* IGNORED */
                  PreLoadSQLiteDll(
                      baseDirectory, processorArchitecture,
                      ref _SQLiteNativeModuleFileName,
                      ref _SQLiteNativeModuleHandle);
              }
          }
      }

      /////////////////////////////////////////////////////////////////////////
      /// <summary>
      /// Searches for the native SQLite library in the directory containing
      /// the assembly currently being executed as well as the base directory
      /// for the current application domain.
      /// </summary>
      /// <param name="baseDirectory">
      /// Upon success, this parameter will be modified to refer to the base
      /// directory containing the native SQLite library.
      /// </param>
      /// <param name="processorArchitecture">
      /// Upon success, this parameter will be modified to refer to the name
      /// of the immediate directory (i.e. the offset from the base directory)
      /// containing the native SQLite library.
      /// </param>
      /// <returns>
      /// Non-zero (success) if the native SQLite library was found; otherwise,
      /// zero (failure).
      /// </returns>
      private static bool SearchForDirectory(
          ref string baseDirectory,        /* out */
          ref string processorArchitecture /* out */
          )
      {
          if (GetSettingValue(
                "PreLoadSQLite_SearchForDirectory", null) == null)
          {
              return false; /* DISABLED */
          }

          //
          // NOTE: Build the list of base directories and processor/platform
          //       names.  These lists will be used to help locate the native
          //       SQLite core library (or interop assembly) to pre-load into
          //       this process.
          //
          string[] directories = {
              GetAssemblyDirectory(),
#if !PLATFORM_COMPACTFRAMEWORK
              AppDomain.CurrentDomain.BaseDirectory,
#endif
          };

          string[] subDirectories = {
              GetProcessorArchitecture(), GetPlatformName(null)
          };

          foreach (string directory in directories)
          {
              if (directory == null)
                  continue;

              foreach (string subDirectory in subDirectories)
              {
                  if (subDirectory == null)
                      continue;

                  string fileName = Path.Combine(Path.Combine(
                      directory, subDirectory), SQLITE_DLL);

                  //
                  // NOTE: If the SQLite DLL file exists, return success.
                  //       Prior to returning, set the base directory and
                  //       processor architecture to reflect the location
                  //       where it was found.
                  //
                  if (File.Exists(fileName))
                  {
                      baseDirectory = directory;
                      processorArchitecture = subDirectory;
                      return true; /* FOUND */
                  }
              }
          }

          return false; /* NOT FOUND */
      }

      /////////////////////////////////////////////////////////////////////////
      /// <summary>
      /// Queries and returns the base directory of the current application
      /// domain.
      /// </summary>
      /// <returns>
      /// The base directory for the current application domain -OR- null if it
      /// cannot be determined.
      /// </returns>
      private static string GetBaseDirectory()
      {
          //
          // NOTE: If the "PreLoadSQLite_BaseDirectory" environment variable
          //       is set, use it verbatim for the base directory.
          //
          string directory = GetSettingValue("PreLoadSQLite_BaseDirectory",
              null);

          if (directory != null)
              return directory;

#if !PLATFORM_COMPACTFRAMEWORK
          //
          // NOTE: If the "PreLoadSQLite_UseAssemblyDirectory" environment
          //       variable is set (to anything), then attempt to use the
          //       directory containing the currently executing assembly
          //       (i.e. System.Data.SQLite) intsead of the application
          //       domain base directory.
          //
          if (GetSettingValue(
                  "PreLoadSQLite_UseAssemblyDirectory", null) != null)
          {
              directory = GetAssemblyDirectory();

              if (directory != null)
                  return directory;
          }

          //
          // NOTE: Otherwise, fallback on using the base directory of the
          //       current application domain.
          //
          return AppDomain.CurrentDomain.BaseDirectory;
#else
          //
          // NOTE: Otherwise, fallback on using the directory containing
          //       the currently executing assembly.
          //
          return GetAssemblyDirectory();
#endif
      }

      /////////////////////////////////////////////////////////////////////////
      /// <summary>
      /// Determines if the dynamic link library file name requires a suffix
      /// and adds it if necessary.
      /// </summary>
      /// <param name="fileName">
      /// The original dynamic link library file name to inspect.
      /// </param>
      /// <returns>
      /// The dynamic link library file name, possibly modified to include an
      /// extension.
      /// </returns>
      private static string FixUpDllFileName(
          string fileName /* in */
          )
      {
          if (!String.IsNullOrEmpty(fileName))
          {
              PlatformID platformId = Environment.OSVersion.Platform;

              if ((platformId == PlatformID.Win32S) ||
                  (platformId == PlatformID.Win32Windows) ||
                  (platformId == PlatformID.Win32NT) ||
                  (platformId == PlatformID.WinCE))
              {
                  if (!fileName.EndsWith(DllFileExtension,
                          StringComparison.OrdinalIgnoreCase))
                  {
                      return fileName + DllFileExtension;
                  }
              }
          }

          return fileName;
      }

      /////////////////////////////////////////////////////////////////////////
      /// <summary>
      /// Queries and returns the processor architecture of the current
      /// process.
      /// </summary>
      /// <returns>
      /// The processor architecture of the current process -OR- null if it
      /// cannot be determined.
      /// </returns>
      private static string GetProcessorArchitecture()
      {
          //
          // NOTE: If the "PreLoadSQLite_ProcessorArchitecture" environment
          //       variable is set, use it verbatim for the current processor
          //       architecture.
          //
          string processorArchitecture = GetSettingValue(
              "PreLoadSQLite_ProcessorArchitecture", null);

          if (processorArchitecture != null)
              return processorArchitecture;

          //
          // BUGBUG: Will this always be reliable?
          //
          processorArchitecture = GetSettingValue(PROCESSOR_ARCHITECTURE, null);

          /////////////////////////////////////////////////////////////////////

#if !PLATFORM_COMPACTFRAMEWORK
          //
          // HACK: Check for an "impossible" situation.  If the pointer size
          //       is 32-bits, the processor architecture cannot be "AMD64".
          //       In that case, we are almost certainly hitting a bug in the
          //       operating system and/or Visual Studio that causes the
          //       PROCESSOR_ARCHITECTURE environment variable to contain the
          //       wrong value in some circumstances.  Please refer to ticket
          //       [9ac9862611] for further information.
          //
          if ((IntPtr.Size == sizeof(int)) &&
              String.Equals(processorArchitecture, "AMD64",
                  StringComparison.OrdinalIgnoreCase))
          {
#if !NET_COMPACT_20 && TRACE_PRELOAD
              //
              // NOTE: When tracing is enabled, save the originally detected
              //       processor architecture before changing it.
              //
              string savedProcessorArchitecture = processorArchitecture;
#endif

              //
              // NOTE: We know that operating systems that return "AMD64" as
              //       the processor architecture are actually a superset of
              //       the "x86" processor architecture; therefore, return
              //       "x86" when the pointer size is 32-bits.
              //
              processorArchitecture = "x86";

#if !NET_COMPACT_20 && TRACE_PRELOAD
              try
              {
                  //
                  // NOTE: Show that we hit a fairly unusual situation (i.e. the
                  //       "wrong" processor architecture was detected).
                  //
                  Trace.WriteLine(String.Format(
                      CultureInfo.CurrentCulture,
                      "Native library pre-loader detected {0}-bit pointer " +
                      "size with processor architecture \"{1}\", using " +
                      "processor architecture \"{2}\" instead...",
                      IntPtr.Size * 8 /* bits */, savedProcessorArchitecture,
                      processorArchitecture)); /* throw */
              }
              catch
              {
                  // do nothing.
              }
#endif
          }
#else
          if (processorArchitecture == null)
          {
              //
              // NOTE: On the .NET Compact Framework, attempt to use the native
              //       Win32 API function (via P/Invoke) that can provide us
              //       with the processor architecture.
              //
              try
              {
                  //
                  // NOTE: The output of the GetSystemInfo function will be
                  //       placed here.  Only the processor architecture field
                  //       is used by this method.
                  //
                  SYSTEM_INFO systemInfo;

                  //
                  // NOTE: Query the system information via P/Invoke, thus
                  //       filling the structure.
                  //
                  GetSystemInfo(out systemInfo);

                  //
                  // NOTE: Return the processor architecture value as a string.
                  //
                  processorArchitecture =
                      systemInfo.wProcessorArchitecture.ToString();
              }
              catch
              {
                  // do nothing.
              }

              //
              // NOTE: Upon failure, return an empty string.  This will prevent
              //       the calling method from considering this method call a
              //       "failure".
              //
              processorArchitecture = String.Empty;
          }
#endif

          /////////////////////////////////////////////////////////////////////

          return processorArchitecture;
      }

      /////////////////////////////////////////////////////////////////////////
      /// <summary>
      /// Given the processor architecture, returns the name of the platform.
      /// </summary>
      /// <param name="processorArchitecture">
      /// The processor architecture to be translated to a platform name.
      /// </param>
      /// <returns>
      /// The platform name for the specified processor architecture -OR- null
      /// if it cannot be determined.
      /// </returns>
      private static string GetPlatformName(
          string processorArchitecture /* in */
          )
      {
          if (processorArchitecture == null)
              processorArchitecture = GetProcessorArchitecture();

          if (String.IsNullOrEmpty(processorArchitecture))
              return null;

          lock (staticSyncRoot)
          {
              if (processorArchitecturePlatforms == null)
                  return null;

              string platformName;

              if (processorArchitecturePlatforms.TryGetValue(
                      processorArchitecture, out platformName))
              {
                  return platformName;
              }
          }

          return null;
      }

      /////////////////////////////////////////////////////////////////////////
      /// <summary>
      /// Attempts to load the native SQLite library based on the specified
      /// directory and processor architecture.
      /// </summary>
      /// <param name="baseDirectory">
      /// The base directory to use, null for default (the base directory of
      /// the current application domain).  This directory should contain the
      /// processor architecture specific sub-directories.
      /// </param>
      /// <param name="processorArchitecture">
      /// The requested processor architecture, null for default (the
      /// processor architecture of the current process).  This caller should
      /// almost always specify null for this parameter.
      /// </param>
      /// <param name="nativeModuleFileName">
      /// The candidate native module file name to load will be stored here,
      /// if necessary.
      /// </param>
      /// <param name="nativeModuleHandle">
      /// The native module handle as returned by LoadLibrary will be stored
      /// here, if necessary.  This value will be IntPtr.Zero if the call to
      /// LoadLibrary fails.
      /// </param>
      /// <returns>
      /// Non-zero if the native module was loaded successfully; otherwise,
      /// zero.
      /// </returns>
      private static bool PreLoadSQLiteDll(
          string baseDirectory,            /* in */
          string processorArchitecture,    /* in */
          ref string nativeModuleFileName, /* out */
          ref IntPtr nativeModuleHandle    /* out */
          )
      {
          //
          // NOTE: If the specified base directory is null, use the default
          //       (i.e. attempt to automatically detect it).
          //
          if (baseDirectory == null)
              baseDirectory = GetBaseDirectory();

          //
          // NOTE: If we failed to query the base directory, stop now.
          //
          if (baseDirectory == null)
              return false;

          //
          // NOTE: If the native SQLite library exists in the base directory
          //       itself, stop now.
          //
          string fileName = FixUpDllFileName(Path.Combine(baseDirectory,
              SQLITE_DLL));

          if (File.Exists(fileName))
              return false;

          //
          // NOTE: If the specified processor architecture is null, use the
          //       default.
          //
          if (processorArchitecture == null)
              processorArchitecture = GetProcessorArchitecture();

          //
          // NOTE: If we failed to query the processor architecture, stop now.
          //
          if (processorArchitecture == null)
              return false;

          //
          // NOTE: Build the full path and file name for the native SQLite
          //       library using the processor architecture name.
          //
          fileName = FixUpDllFileName(Path.Combine(Path.Combine(baseDirectory,
              processorArchitecture), SQLITE_DLL));

          //
          // NOTE: If the file name based on the processor architecture name
          // is not found, try using the associated platform name.
          //
          if (!File.Exists(fileName))
          {
              //
              // NOTE: Attempt to translate the processor architecture to a
              //       platform name.
              //
              string platformName = GetPlatformName(processorArchitecture);

              //
              // NOTE: If we failed to translate the platform name, stop now.
              //
              if (platformName == null)
                  return false;

              //
              // NOTE: Build the full path and file name for the native SQLite
              //       library using the platform name.
              //
              fileName = FixUpDllFileName(Path.Combine(Path.Combine(
                  baseDirectory, platformName), SQLITE_DLL));

              //
              // NOTE: If the file does not exist, skip trying to load it.
              //
              if (!File.Exists(fileName))
                  return false;
          }

          try
          {
#if !NET_COMPACT_20 && TRACE_PRELOAD
              try
              {
                  //
                  // NOTE: Show exactly where we are trying to load the native
                  //       SQLite library from.
                  //
                  Trace.WriteLine(String.Format(
                      CultureInfo.CurrentCulture,
                      "Native library pre-loader is trying to load native " +
                      "SQLite library \"{0}\"...", fileName)); /* throw */
              }
              catch
              {
                  // do nothing.
              }
#endif

              //
              // NOTE: Attempt to load the native library.  This will either
              //       return a valid native module handle, return IntPtr.Zero,
              //       or throw an exception.
              //
              nativeModuleFileName = fileName;
              nativeModuleHandle = LoadLibrary(fileName);

              return (nativeModuleHandle != IntPtr.Zero);
          }
#if !NET_COMPACT_20 && TRACE_PRELOAD
          catch (Exception e)
#else
          catch (Exception)
#endif
          {
#if !NET_COMPACT_20 && TRACE_PRELOAD
              try
              {
                  //
                  // NOTE: First, grab the last Win32 error number.
                  //
                  int lastError = Marshal.GetLastWin32Error(); /* throw */

                  //
                  // NOTE: Show where we failed to load the native SQLite
                  //       library from along with the Win32 error code and
                  //       exception information.
                  //
                  Trace.WriteLine(String.Format(
                      CultureInfo.CurrentCulture,
                      "Native library pre-loader failed to load native " +
                      "SQLite library \"{0}\" (getLastError = {1}): {2}",
                      fileName, lastError, e)); /* throw */
              }
              catch
              {
                  // do nothing.
              }
#endif
          }

          return false;
      }
#endif
#endif
      #endregion

      /////////////////////////////////////////////////////////////////////////

#if PLATFORM_COMPACTFRAMEWORK
    //
    // NOTE: On the .NET Compact Framework, the native interop assembly must
    //       be used because it provides several workarounds to .NET Compact
    //       Framework limitations important for proper operation of the core
    //       System.Data.SQLite functionality (e.g. being able to bind
    //       parameters and handle column values of types Int64 and Double).
    //
    internal const string SQLITE_DLL = "SQLite.Interop.091.dll";
#elif SQLITE_STANDARD
    //
    // NOTE: Otherwise, if the standard SQLite library is enabled, use it.
    //
    internal const string SQLITE_DLL = "sqlite3";
#elif USE_INTEROP_DLL
      //
    // NOTE: Otherwise, if the native SQLite interop assembly is enabled,
    //       use it.
    //
    internal const string SQLITE_DLL = "SQLite.Interop.dll";
#else
    //
    // NOTE: Finally, assume that the mixed-mode assembly is being used.
    //
    internal const string SQLITE_DLL = "System.Data.SQLite.dll";
#endif

    // This section uses interop calls that also fetch text length to optimize conversion.
    // When using the standard dll, we can replace these calls with normal sqlite calls and
    // do unoptimized conversions instead afterwards
    #region interop added textlength calls

#if !SQLITE_STANDARD

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_bind_parameter_name_interop(IntPtr stmt, int index, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_column_database_name_interop(IntPtr stmt, int index, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_column_database_name16_interop(IntPtr stmt, int index, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_column_decltype_interop(IntPtr stmt, int index, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_column_decltype16_interop(IntPtr stmt, int index, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_column_name_interop(IntPtr stmt, int index, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_column_name16_interop(IntPtr stmt, int index, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_column_origin_name_interop(IntPtr stmt, int index, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_column_origin_name16_interop(IntPtr stmt, int index, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_column_table_name_interop(IntPtr stmt, int index, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_column_table_name16_interop(IntPtr stmt, int index, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_column_text_interop(IntPtr stmt, int index, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_column_text16_interop(IntPtr stmt, int index, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_errmsg_interop(IntPtr db, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern SQLiteErrorCode sqlite3_prepare_interop(IntPtr db, IntPtr pSql, int nBytes, out IntPtr stmt, out IntPtr ptrRemain, out int nRemain);

    [DllImport(SQLITE_DLL)]
    internal static extern SQLiteErrorCode sqlite3_table_column_metadata_interop(IntPtr db, byte[] dbName, byte[] tblName, byte[] colName, out IntPtr ptrDataType, out IntPtr ptrCollSeq, out int notNull, out int primaryKey, out int autoInc, out int dtLen, out int csLen);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_value_text_interop(IntPtr p, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_value_text16_interop(IntPtr p, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_malloc_size_interop(IntPtr p);

#if INTEROP_LOG
    [DllImport(SQLITE_DLL)]
    internal static extern SQLiteErrorCode sqlite3_config_log_interop();
#endif
#endif
// !SQLITE_STANDARD

    #endregion

    // These functions add existing functionality on top of SQLite and require a little effort to
    // get working when using the standard SQLite library.
    #region interop added functionality

#if !SQLITE_STANDARD

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_libversion_interop();

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_sourceid_interop();

    [DllImport(SQLITE_DLL)]
    internal static extern SQLiteErrorCode sqlite3_close_interop(IntPtr db);

    [DllImport(SQLITE_DLL)]
    internal static extern SQLiteErrorCode sqlite3_create_function_interop(IntPtr db, byte[] strName, int nArgs, int nType, IntPtr pvUser, SQLiteCallback func, SQLiteCallback fstep, SQLiteFinalCallback ffinal, int needCollSeq);

    [DllImport(SQLITE_DLL)]
    internal static extern SQLiteErrorCode sqlite3_finalize_interop(IntPtr stmt);

    [DllImport(SQLITE_DLL)]
    internal static extern SQLiteErrorCode sqlite3_backup_finish_interop(IntPtr backup);

    [DllImport(SQLITE_DLL)]
    internal static extern SQLiteErrorCode sqlite3_open_interop(byte[] utf8Filename, SQLiteOpenFlagsEnum flags, out IntPtr db);

    [DllImport(SQLITE_DLL)]
    internal static extern SQLiteErrorCode sqlite3_open16_interop(byte[] utf8Filename, SQLiteOpenFlagsEnum flags, out IntPtr db);

    [DllImport(SQLITE_DLL)]
    internal static extern SQLiteErrorCode sqlite3_reset_interop(IntPtr stmt);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_changes_interop(IntPtr db);
#endif
// !SQLITE_STANDARD

    #endregion

    // The standard api call equivalents of the above interop calls
    #region standard versions of interop functions

#if SQLITE_STANDARD

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_close(IntPtr db);

#if !INTEROP_LEGACY_CLOSE
#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_close_v2(IntPtr db); /* 3.7.14+ */
#endif

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_create_function(IntPtr db, byte[] strName, int nArgs, int nType, IntPtr pvUser, SQLiteCallback func, SQLiteCallback fstep, SQLiteFinalCallback ffinal);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_finalize(IntPtr stmt);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_backup_finish(IntPtr backup);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_reset(IntPtr stmt);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_bind_parameter_name(IntPtr stmt, int index);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_column_database_name(IntPtr stmt, int index);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_column_database_name16(IntPtr stmt, int index);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_column_decltype(IntPtr stmt, int index);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_column_decltype16(IntPtr stmt, int index);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_column_name(IntPtr stmt, int index);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_column_name16(IntPtr stmt, int index);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_column_origin_name(IntPtr stmt, int index);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_column_origin_name16(IntPtr stmt, int index);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_column_table_name(IntPtr stmt, int index);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_column_table_name16(IntPtr stmt, int index);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_column_text(IntPtr stmt, int index);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_column_text16(IntPtr stmt, int index);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_errmsg(IntPtr db);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_prepare(IntPtr db, IntPtr pSql, int nBytes, out IntPtr stmt, out IntPtr ptrRemain);

#if USE_PREPARE_V2
#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_prepare_v2(IntPtr db, IntPtr pSql, int nBytes, out IntPtr stmt, out IntPtr ptrRemain);
#endif

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_table_column_metadata(IntPtr db, byte[] dbName, byte[] tblName, byte[] colName, out IntPtr ptrDataType, out IntPtr ptrCollSeq, out int notNull, out int primaryKey, out int autoInc);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_value_text(IntPtr p);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_value_text16(IntPtr p);

#endif
    // SQLITE_STANDARD

    #endregion

    // These functions are custom and have no equivalent standard library method.
    // All of them are "nice to haves" and not necessarily "need to haves".
    #region no equivalent standard method

#if !SQLITE_STANDARD

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_context_collseq_interop(IntPtr context, out int type, out int enc, out int len);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_context_collcompare_interop(IntPtr context, byte[] p1, int p1len, byte[] p2, int p2len);

    [DllImport(SQLITE_DLL)]
    internal static extern SQLiteErrorCode sqlite3_cursor_rowid_interop(IntPtr stmt, int cursor, out long rowid);

    [DllImport(SQLITE_DLL)]
    internal static extern SQLiteErrorCode sqlite3_index_column_info_interop(IntPtr db, byte[] catalog, byte[] IndexName, byte[] ColumnName, out int sortOrder, out int onError, out IntPtr Collation, out int colllen);

    [DllImport(SQLITE_DLL)]
    internal static extern void sqlite3_resetall_interop(IntPtr db);

    [DllImport(SQLITE_DLL)]
    internal static extern int sqlite3_table_cursor_interop(IntPtr stmt, int db, int tableRootPage);

#endif
// !SQLITE_STANDARD

    #endregion

    // Standard API calls global across versions.  There are a few instances of interop calls
    // scattered in here, but they are only active when PLATFORM_COMPACTFRAMEWORK is declared.
    #region standard sqlite api calls
#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_enable_load_extension(
        IntPtr db, int enable);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_load_extension(
        IntPtr db, byte[] fileName, byte[] procName, ref IntPtr pError);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_overload_function(IntPtr db, IntPtr zName, int nArgs);

#if WINDOWS
#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
#else
    [DllImport(SQLITE_DLL, CharSet = CharSet.Unicode)]
#endif
    //
    // NOTE: The "sqlite3_win32_set_directory" SQLite core library function is
    //       only supported on Windows.
    //
    internal static extern SQLiteErrorCode sqlite3_win32_set_directory(uint type, string value);

#if !DEBUG // NOTE: Should be "WIN32HEAP && !MEMDEBUG"
#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    //
    // NOTE: The "sqlite3_win32_reset_heap" SQLite core library function is
    //       only supported on Windows when the Win32 native allocator is in
    //       use (i.e. by default, in "Release" builds of System.Data.SQLite
    //       only).  By default, in "Debug" builds of System.Data.SQLite, the
    //       MEMDEBUG allocator is used.
    //
    internal static extern SQLiteErrorCode sqlite3_win32_reset_heap();

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    //
    // NOTE: The "sqlite3_win32_compact_heap" SQLite core library function is
    //       only supported on Windows when the Win32 native allocator is in
    //       use (i.e. by default, in "Release" builds of System.Data.SQLite
    //       only).  By default, in "Debug" builds of System.Data.SQLite, the
    //       MEMDEBUG allocator is used.
    //
    internal static extern SQLiteErrorCode sqlite3_win32_compact_heap(ref uint largest);
#endif
#endif

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_libversion();

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern int sqlite3_libversion_number();

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_sourceid();

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_malloc(int n);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_realloc(IntPtr p, int n);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern void sqlite3_free(IntPtr p);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_open_v2(byte[] utf8Filename, out IntPtr db, SQLiteOpenFlagsEnum flags, IntPtr vfs);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
#else
    [DllImport(SQLITE_DLL, CharSet = CharSet.Unicode)]
#endif
    internal static extern SQLiteErrorCode sqlite3_open16(string fileName, out IntPtr db);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern void sqlite3_interrupt(IntPtr db);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
    internal static extern long sqlite3_last_insert_rowid(IntPtr db);
#endif

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern int sqlite3_changes(IntPtr db);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
    internal static extern long sqlite3_memory_used();
#endif

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
    internal static extern long sqlite3_memory_highwater(int resetFlag);
#endif

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_shutdown();

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_busy_timeout(IntPtr db, int ms);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_bind_blob(IntPtr stmt, int index, Byte[] value, int nSize, IntPtr nTransient);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
    internal static extern SQLiteErrorCode sqlite3_bind_double(IntPtr stmt, int index, double value);
#endif

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_bind_int(IntPtr stmt, int index, int value);

    //
    // NOTE: This really just calls "sqlite3_bind_int"; however, it has the
    //       correct type signature for an unsigned (32-bit) integer.
    //
#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, EntryPoint = "sqlite3_bind_int", CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL, EntryPoint = "sqlite3_bind_int")]
#endif
    internal static extern SQLiteErrorCode sqlite3_bind_uint(IntPtr stmt, int index, uint value);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
    internal static extern SQLiteErrorCode sqlite3_bind_int64(IntPtr stmt, int index, long value);
#endif

    //
    // NOTE: This really just calls "sqlite3_bind_int64"; however, it has the
    //       correct type signature for an unsigned long (64-bit) integer.
    //
#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, EntryPoint = "sqlite3_bind_int64", CallingConvention = CallingConvention.Cdecl)]
    internal static extern SQLiteErrorCode sqlite3_bind_uint64(IntPtr stmt, int index, ulong value);
#endif

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_bind_null(IntPtr stmt, int index);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_bind_text(IntPtr stmt, int index, byte[] value, int nlen, IntPtr pvReserved);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern int sqlite3_bind_parameter_count(IntPtr stmt);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern int sqlite3_bind_parameter_index(IntPtr stmt, byte[] strName);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern int sqlite3_column_count(IntPtr stmt);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_step(IntPtr stmt);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
    internal static extern double sqlite3_column_double(IntPtr stmt, int index);
#endif

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern int sqlite3_column_int(IntPtr stmt, int index);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
    internal static extern long sqlite3_column_int64(IntPtr stmt, int index);
#endif

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_column_blob(IntPtr stmt, int index);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern int sqlite3_column_bytes(IntPtr stmt, int index);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern int sqlite3_column_bytes16(IntPtr stmt, int index);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern TypeAffinity sqlite3_column_type(IntPtr stmt, int index);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_create_collation(IntPtr db, byte[] strName, int nType, IntPtr pvUser, SQLiteCollation func);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern int sqlite3_aggregate_count(IntPtr context);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_value_blob(IntPtr p);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern int sqlite3_value_bytes(IntPtr p);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern int sqlite3_value_bytes16(IntPtr p);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
    internal static extern double sqlite3_value_double(IntPtr p);
#endif

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern int sqlite3_value_int(IntPtr p);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
    internal static extern long sqlite3_value_int64(IntPtr p);
#endif

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern TypeAffinity sqlite3_value_type(IntPtr p);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern void sqlite3_result_blob(IntPtr context, byte[] value, int nSize, IntPtr pvReserved);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void sqlite3_result_double(IntPtr context, double value);
#endif

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern void sqlite3_result_error(IntPtr context, byte[] strErr, int nLen);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern void sqlite3_result_error_code(IntPtr context, SQLiteErrorCode value);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern void sqlite3_result_error_toobig(IntPtr context);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern void sqlite3_result_error_nomem(IntPtr context);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern void sqlite3_result_value(IntPtr context, IntPtr value);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern void sqlite3_result_zeroblob(IntPtr context, int nLen);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern void sqlite3_result_int(IntPtr context, int value);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void sqlite3_result_int64(IntPtr context, long value);
#endif

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern void sqlite3_result_null(IntPtr context);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern void sqlite3_result_text(IntPtr context, byte[] value, int nLen, IntPtr pvReserved);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_aggregate_context(IntPtr context, int nBytes);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
#else
    [DllImport(SQLITE_DLL, CharSet = CharSet.Unicode)]
#endif
    internal static extern SQLiteErrorCode sqlite3_bind_text16(IntPtr stmt, int index, string value, int nlen, IntPtr pvReserved);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
#else
    [DllImport(SQLITE_DLL, CharSet = CharSet.Unicode)]
#endif
    internal static extern void sqlite3_result_error16(IntPtr context, string strName, int nLen);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
#else
    [DllImport(SQLITE_DLL, CharSet = CharSet.Unicode)]
#endif
    internal static extern void sqlite3_result_text16(IntPtr context, string strName, int nLen, IntPtr pvReserved);

#if INTEROP_CODEC
#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_key(IntPtr db, byte[] key, int keylen);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_rekey(IntPtr db, byte[] key, int keylen);
#endif

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_set_authorizer(IntPtr db, SQLiteAuthorizerCallback func, IntPtr pvUser);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_update_hook(IntPtr db, SQLiteUpdateCallback func, IntPtr pvUser);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_commit_hook(IntPtr db, SQLiteCommitCallback func, IntPtr pvUser);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_trace(IntPtr db, SQLiteTraceCallback func, IntPtr pvUser);

    // Since sqlite3_config() takes a variable argument list, we have to overload declarations
    // for all possible calls that we want to use.
#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, EntryPoint = "sqlite3_config", CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL, EntryPoint = "sqlite3_config")]
#endif
    internal static extern SQLiteErrorCode sqlite3_config_none(SQLiteConfigOpsEnum op);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, EntryPoint = "sqlite3_config", CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL, EntryPoint = "sqlite3_config")]
#endif
    internal static extern SQLiteErrorCode sqlite3_config_int(SQLiteConfigOpsEnum op, int value);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, EntryPoint = "sqlite3_config", CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL, EntryPoint = "sqlite3_config")]
#endif
    internal static extern SQLiteErrorCode sqlite3_config_log(SQLiteConfigOpsEnum op, SQLiteLogCallback func, IntPtr pvUser);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_rollback_hook(IntPtr db, SQLiteRollbackCallback func, IntPtr pvUser);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_db_handle(IntPtr stmt);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_db_release_memory(IntPtr db);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_db_filename(IntPtr db, IntPtr dbName);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_next_stmt(IntPtr db, IntPtr stmt);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_exec(IntPtr db, byte[] strSql, IntPtr pvCallback, IntPtr pvParam, out IntPtr errMsg);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern int sqlite3_release_memory(int nBytes);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern int sqlite3_get_autocommit(IntPtr db);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_extended_result_codes(IntPtr db, int onoff);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_errcode(IntPtr db);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_extended_errcode(IntPtr db);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_errstr(SQLiteErrorCode rc); /* 3.7.15+ */

    // Since sqlite3_log() takes a variable argument list, we have to overload declarations
    // for all possible calls.  For now, we are only exposing a single string, and
    // depend on the caller to format the string.
#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern void sqlite3_log(SQLiteErrorCode iErrCode, byte[] zFormat);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_file_control(IntPtr db, byte[] zDbName, int op, IntPtr pArg);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_backup_init(IntPtr destDb, byte[] zDestName, IntPtr sourceDb, byte[] zSourceName);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_backup_step(IntPtr backup, int nPage);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern int sqlite3_backup_remaining(IntPtr backup);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern int sqlite3_backup_pagecount(IntPtr backup);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern SQLiteErrorCode sqlite3_declare_vtab(IntPtr db, IntPtr zSQL);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_mprintf(IntPtr format, __arglist);
    #endregion

    // SQLite API calls that are provided by "well-known" extensions that may be statically
    // linked with the SQLite core native library currently in use.
    #region extension sqlite api calls
#if INTEROP_VIRTUAL_TABLE
#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern IntPtr sqlite3_create_disposable_module(IntPtr db, IntPtr name, ref sqlite3_module module, IntPtr pClientData, xDestroyModule xDestroy);

#if !PLATFORM_COMPACTFRAMEWORK
    [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(SQLITE_DLL)]
#endif
    internal static extern void sqlite3_dispose_module(IntPtr pModule);
#endif
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region sqlite interop api calls (.NET Compact Framework only)
#if PLATFORM_COMPACTFRAMEWORK && !SQLITE_STANDARD
    [DllImport(SQLITE_DLL)]
    internal static extern void sqlite3_last_insert_rowid_interop(IntPtr db, ref long rowId);

    [DllImport(SQLITE_DLL)]
    internal static extern void sqlite3_memory_used_interop(ref long bytes);

    [DllImport(SQLITE_DLL)]
    internal static extern void sqlite3_memory_highwater_interop(int resetFlag, ref long bytes);

    [DllImport(SQLITE_DLL)]
    internal static extern SQLiteErrorCode sqlite3_bind_double_interop(IntPtr stmt, int index, ref double value);

    [DllImport(SQLITE_DLL)]
    internal static extern SQLiteErrorCode sqlite3_bind_int64_interop(IntPtr stmt, int index, ref long value);

    [DllImport(SQLITE_DLL, EntryPoint = "sqlite3_bind_int64_interop")]
    internal static extern SQLiteErrorCode sqlite3_bind_uint64_interop(IntPtr stmt, int index, ref ulong value);

    [DllImport(SQLITE_DLL)]
    internal static extern void sqlite3_column_double_interop(IntPtr stmt, int index, out double value);

    [DllImport(SQLITE_DLL)]
    internal static extern void sqlite3_column_int64_interop(IntPtr stmt, int index, out long value);

    [DllImport(SQLITE_DLL)]
    internal static extern void sqlite3_value_double_interop(IntPtr p, out double value);

    [DllImport(SQLITE_DLL)]
    internal static extern void sqlite3_value_int64_interop(IntPtr p, out Int64 value);

    [DllImport(SQLITE_DLL)]
    internal static extern void sqlite3_result_double_interop(IntPtr context, ref double value);

    [DllImport(SQLITE_DLL)]
    internal static extern void sqlite3_result_int64_interop(IntPtr context, ref Int64 value);

    [DllImport(SQLITE_DLL)]
    internal static extern IntPtr sqlite3_create_disposable_module_interop(
        IntPtr db, IntPtr name, IntPtr pModule, int iVersion, xCreate xCreate,
        xConnect xConnect, xBestIndex xBestIndex, xDisconnect xDisconnect,
        xDestroy xDestroy, xOpen xOpen, xClose xClose, xFilter xFilter,
        xNext xNext, xEof xEof, xColumn xColumn, xRowId xRowId, xUpdate xUpdate,
        xBegin xBegin, xSync xSync, xCommit xCommit, xRollback xRollback,
        xFindFunction xFindFunction, xRename xRename, xSavepoint xSavepoint,
        xRelease xRelease, xRollbackTo xRollbackTo, IntPtr pClientData,
        xDestroyModule xDestroyModule);
#endif
    // PLATFORM_COMPACTFRAMEWORK && !SQLITE_STANDARD
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Native Delegates
#if INTEROP_VIRTUAL_TABLE
#if !PLATFORM_COMPACTFRAMEWORK
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
    public delegate SQLiteErrorCode xCreate(
        IntPtr pDb,
        IntPtr pAux,
        int argc,
        IntPtr argv,
        ref IntPtr pVtab,
        ref IntPtr pError
    );

    ///////////////////////////////////////////////////////////////////////////

#if !PLATFORM_COMPACTFRAMEWORK
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
    public delegate SQLiteErrorCode xConnect(
        IntPtr pDb,
        IntPtr pAux,
        int argc,
        IntPtr argv,
        ref IntPtr pVtab,
        ref IntPtr pError
    );

    ///////////////////////////////////////////////////////////////////////////

#if !PLATFORM_COMPACTFRAMEWORK
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
    public delegate SQLiteErrorCode xBestIndex(
        IntPtr pVtab,
        IntPtr pIndex
    );

    ///////////////////////////////////////////////////////////////////////////

#if !PLATFORM_COMPACTFRAMEWORK
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
    public delegate SQLiteErrorCode xDisconnect(
        IntPtr pVtab
    );

    ///////////////////////////////////////////////////////////////////////////

#if !PLATFORM_COMPACTFRAMEWORK
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
    public delegate SQLiteErrorCode xDestroy(
        IntPtr pVtab
    );

    ///////////////////////////////////////////////////////////////////////////

#if !PLATFORM_COMPACTFRAMEWORK
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
    public delegate SQLiteErrorCode xOpen(
        IntPtr pVtab,
        ref IntPtr pCursor
    );

    ///////////////////////////////////////////////////////////////////////////

#if !PLATFORM_COMPACTFRAMEWORK
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
    public delegate SQLiteErrorCode xClose(
        IntPtr pCursor
    );

    ///////////////////////////////////////////////////////////////////////////

#if !PLATFORM_COMPACTFRAMEWORK
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
    public delegate SQLiteErrorCode xFilter(
        IntPtr pCursor,
        int idxNum,
        IntPtr idxStr,
        int argc,
        IntPtr argv
    );

    ///////////////////////////////////////////////////////////////////////////

#if !PLATFORM_COMPACTFRAMEWORK
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
    public delegate SQLiteErrorCode xNext(
        IntPtr pCursor
    );

    ///////////////////////////////////////////////////////////////////////////

#if !PLATFORM_COMPACTFRAMEWORK
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
    public delegate int xEof(
        IntPtr pCursor
    );

    ///////////////////////////////////////////////////////////////////////////

#if !PLATFORM_COMPACTFRAMEWORK
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
    public delegate SQLiteErrorCode xColumn(
        IntPtr pCursor,
        IntPtr pContext,
        int index
    );

    ///////////////////////////////////////////////////////////////////////////

#if !PLATFORM_COMPACTFRAMEWORK
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
    public delegate SQLiteErrorCode xRowId(
        IntPtr pCursor,
        ref long rowId
    );

    ///////////////////////////////////////////////////////////////////////////

#if !PLATFORM_COMPACTFRAMEWORK
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
    public delegate SQLiteErrorCode xUpdate(
        IntPtr pVtab,
        int argc,
        IntPtr argv,
        ref long rowId
    );

    ///////////////////////////////////////////////////////////////////////////

#if !PLATFORM_COMPACTFRAMEWORK
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
    public delegate SQLiteErrorCode xBegin(
        IntPtr pVtab
    );

    ///////////////////////////////////////////////////////////////////////////

#if !PLATFORM_COMPACTFRAMEWORK
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
    public delegate SQLiteErrorCode xSync(
        IntPtr pVtab
    );

    ///////////////////////////////////////////////////////////////////////////

#if !PLATFORM_COMPACTFRAMEWORK
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
    public delegate SQLiteErrorCode xCommit(
        IntPtr pVtab
    );

    ///////////////////////////////////////////////////////////////////////////

#if !PLATFORM_COMPACTFRAMEWORK
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
    public delegate SQLiteErrorCode xRollback(
        IntPtr pVtab
    );

    ///////////////////////////////////////////////////////////////////////////

#if !PLATFORM_COMPACTFRAMEWORK
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
    public delegate int xFindFunction(
        IntPtr pVtab,
        int nArg,
        IntPtr zName,
        ref SQLiteCallback callback,
        ref IntPtr pUserData
    );

    ///////////////////////////////////////////////////////////////////////////

#if !PLATFORM_COMPACTFRAMEWORK
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
    public delegate SQLiteErrorCode xRename(
        IntPtr pVtab,
        IntPtr zNew
    );

    ///////////////////////////////////////////////////////////////////////////

#if !PLATFORM_COMPACTFRAMEWORK
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
    public delegate SQLiteErrorCode xSavepoint(
        IntPtr pVtab,
        int iSavepoint
    );

    ///////////////////////////////////////////////////////////////////////////

#if !PLATFORM_COMPACTFRAMEWORK
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
    public delegate SQLiteErrorCode xRelease(
        IntPtr pVtab,
        int iSavepoint
    );

    ///////////////////////////////////////////////////////////////////////////

#if !PLATFORM_COMPACTFRAMEWORK
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
    public delegate SQLiteErrorCode xRollbackTo(
        IntPtr pVtab,
        int iSavepoint
    );

    ///////////////////////////////////////////////////////////////////////////

#if !PLATFORM_COMPACTFRAMEWORK
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
    public delegate void xDestroyModule(IntPtr pClientData);
#endif
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Native Structures
#if INTEROP_VIRTUAL_TABLE
    [StructLayout(LayoutKind.Sequential)]
    internal struct sqlite3_module
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

    ///////////////////////////////////////////////////////////////////////////

    [StructLayout(LayoutKind.Sequential)]
    internal struct sqlite3_vtab
    {
        public IntPtr pModule;
        public int nRef; /* NO LONGER USED */
        public IntPtr zErrMsg;
    }

    ///////////////////////////////////////////////////////////////////////////

    [StructLayout(LayoutKind.Sequential)]
    internal struct sqlite3_vtab_cursor
    {
        public IntPtr pVTab;
    }

    ///////////////////////////////////////////////////////////////////////////

    [StructLayout(LayoutKind.Sequential)]
    internal struct sqlite3_index_constraint
    {
        public sqlite3_index_constraint(
            SQLiteIndexConstraint constraint
            )
            : this()
        {
            if (constraint != null)
            {
                iColumn = constraint.iColumn;
                op = constraint.op;
                usable = constraint.usable;
                iTermOffset = constraint.iTermOffset;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public int iColumn;
        public SQLiteIndexConstraintOp op;
        public byte usable;
        public int iTermOffset;
    }

    ///////////////////////////////////////////////////////////////////////////

    [StructLayout(LayoutKind.Sequential)]
    internal struct sqlite3_index_orderby
    {
        public sqlite3_index_orderby(
            SQLiteIndexOrderBy orderBy
            )
            : this()
        {
            if (orderBy != null)
            {
                iColumn = orderBy.iColumn;
                desc = orderBy.desc;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public int iColumn; /* Column number */
        public byte desc;   /* True for DESC.  False for ASC. */
    }

    ///////////////////////////////////////////////////////////////////////////

    [StructLayout(LayoutKind.Sequential)]
    internal struct sqlite3_index_constraint_usage
    {
        public sqlite3_index_constraint_usage(
            SQLiteIndexConstraintUsage constraintUsage
            )
            : this()
        {
            if (constraintUsage != null)
            {
                argvIndex = constraintUsage.argvIndex;
                omit = constraintUsage.omit;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public int argvIndex; /* if >0, constraint is part of argv to xFilter */
        public byte omit;     /* Do not code a test for this constraint */
    }

    ///////////////////////////////////////////////////////////////////////////

    [StructLayout(LayoutKind.Sequential)]
    internal struct sqlite3_index_info
    {
        /* Inputs */
        public int nConstraint; /* Number of entries in aConstraint */
        public IntPtr aConstraint;
        public int nOrderBy;
        public IntPtr aOrderBy;
        /* Outputs */
        public IntPtr aConstraintUsage;
        public int idxNum;           /* Number used to identify the index */
        public string idxStr;        /* String, possibly obtained from sqlite3_malloc */
        public int needToFreeIdxStr; /* Free idxStr using sqlite3_free() if true */
        public int orderByConsumed;  /* True if output is already ordered */
        public double estimatedCost; /* Estimated cost of using this index */
    }
#endif
    #endregion
  }

  /////////////////////////////////////////////////////////////////////////////

#if PLATFORM_COMPACTFRAMEWORK
  internal abstract class CriticalHandle : IDisposable
  {
    private bool _isClosed;
    protected IntPtr handle;

    protected CriticalHandle(IntPtr invalidHandleValue)
    {
      handle = invalidHandleValue;
      _isClosed = false;
    }

    ~CriticalHandle()
    {
      Dispose(false);
    }

    private void Cleanup()
    {
      if (!IsClosed)
      {
        this._isClosed = true;
        if (!IsInvalid)
        {
          ReleaseHandle();
          GC.SuppressFinalize(this);
        }
      }
    }

    public void Close()
    {
      Dispose(true);
    }

    public void Dispose()
    {
      Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
      Cleanup();
    }

    protected abstract bool ReleaseHandle();

    protected void SetHandle(IntPtr value)
    {
      handle = value;
    }

    public void SetHandleAsInvalid()
    {
      _isClosed = true;
      GC.SuppressFinalize(this);
    }

    public bool IsClosed
    {
      get { return _isClosed; }
    }

    public abstract bool IsInvalid
    {
      get;
    }

  }

#endif

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteConnectionHandle Class
    // Handles the unmanaged database pointer, and provides finalization
    // support for it.
    internal sealed class SQLiteConnectionHandle : CriticalHandle
    {
#if SQLITE_STANDARD && !PLATFORM_COMPACTFRAMEWORK
        internal delegate void CloseConnectionCallback(
            SQLiteConnectionHandle hdl, IntPtr db);

        internal static CloseConnectionCallback closeConnection =
            SQLiteBase.CloseConnection;
#endif

        ///////////////////////////////////////////////////////////////////////

#if PLATFORM_COMPACTFRAMEWORK
        internal readonly object syncRoot = new object();
#endif

        ///////////////////////////////////////////////////////////////////////

        private bool ownHandle;

        ///////////////////////////////////////////////////////////////////////

        public static implicit operator IntPtr(SQLiteConnectionHandle db)
        {
            if (db != null)
            {
#if PLATFORM_COMPACTFRAMEWORK
                lock (db.syncRoot)
#endif
                {
                    return db.handle;
                }
            }
            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////

        internal SQLiteConnectionHandle(IntPtr db, bool ownHandle)
            : this(ownHandle)
        {
#if PLATFORM_COMPACTFRAMEWORK
            lock (syncRoot)
#endif
            {
                this.ownHandle = ownHandle;
                SetHandle(db);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private SQLiteConnectionHandle(bool ownHandle)
            : base(IntPtr.Zero)
        {
#if COUNT_HANDLE
            if (ownHandle)
                Interlocked.Increment(ref UnsafeNativeMethods.connectionCount);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        protected override bool ReleaseHandle()
        {
#if PLATFORM_COMPACTFRAMEWORK
            lock (syncRoot)
#endif
            {
                if (!ownHandle) return true;
            }

            try
            {
#if !PLATFORM_COMPACTFRAMEWORK
                IntPtr localHandle = Interlocked.Exchange(
                    ref handle, IntPtr.Zero);

#if SQLITE_STANDARD
                if (localHandle != IntPtr.Zero)
                    closeConnection(this, localHandle);
#else
                if (localHandle != IntPtr.Zero)
                    SQLiteBase.CloseConnection(this, localHandle);
#endif

#if !NET_COMPACT_20 && TRACE_HANDLE
                try
                {
                    Trace.WriteLine(String.Format(
                        "CloseConnection: {0}", localHandle)); /* throw */
                }
                catch
                {
                }
#endif
#else
                lock (syncRoot)
                {
                    if (handle != IntPtr.Zero)
                    {
                        SQLiteBase.CloseConnection(this, handle);
                        SetHandle(IntPtr.Zero);
                    }
                }
#endif
#if COUNT_HANDLE
                Interlocked.Decrement(
                    ref UnsafeNativeMethods.connectionCount);
#endif
#if DEBUG
                return true;
#endif
            }
#if !NET_COMPACT_20 && TRACE_HANDLE
            catch (SQLiteException e)
#else
            catch (SQLiteException)
#endif
            {
#if !NET_COMPACT_20 && TRACE_HANDLE
                try
                {
                    Trace.WriteLine(String.Format(
                        "CloseConnection: {0}, exception: {1}",
                        handle, e)); /* throw */
                }
                catch
                {
                }
#endif
            }
            finally
            {
#if PLATFORM_COMPACTFRAMEWORK
                lock (syncRoot)
#endif
                {
                    SetHandleAsInvalid();
                }
            }
#if DEBUG
            return false;
#else
            return true;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

#if COUNT_HANDLE
        public int WasReleasedOk()
        {
            return Interlocked.Decrement(
                ref UnsafeNativeMethods.connectionCount);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public bool OwnHandle
        {
            get
            {
#if PLATFORM_COMPACTFRAMEWORK
                lock (syncRoot)
#endif
                {
                    return ownHandle;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public override bool IsInvalid
        {
            get
            {
#if PLATFORM_COMPACTFRAMEWORK
                lock (syncRoot)
#endif
                {
                    return (handle == IntPtr.Zero);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if DEBUG
        public override string ToString()
        {
#if PLATFORM_COMPACTFRAMEWORK
            lock (syncRoot)
#endif
            {
                return handle.ToString();
            }
        }
#endif
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteStatementHandle Class
    // Provides finalization support for unmanaged SQLite statements.
    internal sealed class SQLiteStatementHandle : CriticalHandle
    {
#if PLATFORM_COMPACTFRAMEWORK
        internal readonly object syncRoot = new object();
#endif

        ///////////////////////////////////////////////////////////////////////

        private SQLiteConnectionHandle cnn;

        ///////////////////////////////////////////////////////////////////////

        public static implicit operator IntPtr(SQLiteStatementHandle stmt)
        {
            if (stmt != null)
            {
#if PLATFORM_COMPACTFRAMEWORK
                lock (stmt.syncRoot)
#endif
                {
                    return stmt.handle;
                }
            }
            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////

        internal SQLiteStatementHandle(SQLiteConnectionHandle cnn, IntPtr stmt)
            : this()
        {
#if PLATFORM_COMPACTFRAMEWORK
            lock (syncRoot)
#endif
            {
                this.cnn = cnn;
                SetHandle(stmt);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private SQLiteStatementHandle()
            : base(IntPtr.Zero)
        {
#if COUNT_HANDLE
            Interlocked.Increment(
                ref UnsafeNativeMethods.statementCount);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        protected override bool ReleaseHandle()
        {
            try
            {
#if !PLATFORM_COMPACTFRAMEWORK
                IntPtr localHandle = Interlocked.Exchange(
                    ref handle, IntPtr.Zero);

                if (localHandle != IntPtr.Zero)
                    SQLiteBase.FinalizeStatement(cnn, localHandle);

#if !NET_COMPACT_20 && TRACE_HANDLE
                try
                {
                    Trace.WriteLine(String.Format(
                        "FinalizeStatement: {0}", localHandle)); /* throw */
                }
                catch
                {
                }
#endif
#else
                lock (syncRoot)
                {
                    if (handle != IntPtr.Zero)
                    {
                        SQLiteBase.FinalizeStatement(cnn, handle);
                        SetHandle(IntPtr.Zero);
                    }
                }
#endif
#if COUNT_HANDLE
                Interlocked.Decrement(
                    ref UnsafeNativeMethods.statementCount);
#endif
#if DEBUG
                return true;
#endif
            }
#if !NET_COMPACT_20 && TRACE_HANDLE
            catch (SQLiteException e)
#else
            catch (SQLiteException)
#endif
            {
#if !NET_COMPACT_20 && TRACE_HANDLE
                try
                {
                    Trace.WriteLine(String.Format(
                        "FinalizeStatement: {0}, exception: {1}",
                        handle, e)); /* throw */
                }
                catch
                {
                }
#endif
            }
            finally
            {
#if PLATFORM_COMPACTFRAMEWORK
                lock (syncRoot)
#endif
                {
                    SetHandleAsInvalid();
                }
            }
#if DEBUG
            return false;
#else
            return true;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

#if COUNT_HANDLE
        public int WasReleasedOk()
        {
            return Interlocked.Decrement(
                ref UnsafeNativeMethods.statementCount);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public override bool IsInvalid
        {
            get
            {
#if PLATFORM_COMPACTFRAMEWORK
                lock (syncRoot)
#endif
                {
                    return (handle == IntPtr.Zero);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if DEBUG
        public override string ToString()
        {
#if PLATFORM_COMPACTFRAMEWORK
            lock (syncRoot)
#endif
            {
                return handle.ToString();
            }
        }
#endif
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region SQLiteBackupHandle Class
    // Provides finalization support for unmanaged SQLite backup objects.
    internal sealed class SQLiteBackupHandle : CriticalHandle
    {
#if PLATFORM_COMPACTFRAMEWORK
        internal readonly object syncRoot = new object();
#endif

        ///////////////////////////////////////////////////////////////////////

        private SQLiteConnectionHandle cnn;

        ///////////////////////////////////////////////////////////////////////

        public static implicit operator IntPtr(SQLiteBackupHandle backup)
        {
            if (backup != null)
            {
#if PLATFORM_COMPACTFRAMEWORK
                lock (backup.syncRoot)
#endif
                {
                    return backup.handle;
                }
            }
            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////

        internal SQLiteBackupHandle(SQLiteConnectionHandle cnn, IntPtr backup)
            : this()
        {
#if PLATFORM_COMPACTFRAMEWORK
            lock (syncRoot)
#endif
            {
                this.cnn = cnn;
                SetHandle(backup);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private SQLiteBackupHandle()
            : base(IntPtr.Zero)
        {
#if COUNT_HANDLE
            Interlocked.Increment(
                ref UnsafeNativeMethods.backupCount);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        protected override bool ReleaseHandle()
        {
            try
            {
#if !PLATFORM_COMPACTFRAMEWORK
                IntPtr localHandle = Interlocked.Exchange(
                    ref handle, IntPtr.Zero);

                if (localHandle != IntPtr.Zero)
                    SQLiteBase.FinishBackup(cnn, localHandle);

#if !NET_COMPACT_20 && TRACE_HANDLE
                try
                {
                    Trace.WriteLine(String.Format(
                        "FinishBackup: {0}", localHandle)); /* throw */
                }
                catch
                {
                }
#endif
#else
                lock (syncRoot)
                {
                    if (handle != IntPtr.Zero)
                    {
                        SQLiteBase.FinishBackup(cnn, handle);
                        SetHandle(IntPtr.Zero);
                    }
                }
#endif
#if COUNT_HANDLE
                Interlocked.Decrement(
                    ref UnsafeNativeMethods.backupCount);
#endif
#if DEBUG
                return true;
#endif
            }
#if !NET_COMPACT_20 && TRACE_HANDLE
            catch (SQLiteException e)
#else
            catch (SQLiteException)
#endif
            {
#if !NET_COMPACT_20 && TRACE_HANDLE
                try
                {
                    Trace.WriteLine(String.Format(
                        "FinishBackup: {0}, exception: {1}",
                        handle, e)); /* throw */
                }
                catch
                {
                }
#endif
            }
            finally
            {
#if PLATFORM_COMPACTFRAMEWORK
                lock (syncRoot)
#endif
                {
                    SetHandleAsInvalid();
                }
            }
#if DEBUG
            return false;
#else
            return true;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

#if COUNT_HANDLE
        public int WasReleasedOk()
        {
            return Interlocked.Decrement(
                ref UnsafeNativeMethods.backupCount);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public override bool IsInvalid
        {
            get
            {
#if PLATFORM_COMPACTFRAMEWORK
                lock (syncRoot)
#endif
                {
                    return (handle == IntPtr.Zero);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if DEBUG
        public override string ToString()
        {
#if PLATFORM_COMPACTFRAMEWORK
            lock (syncRoot)
#endif
            {
                return handle.ToString();
            }
        }
#endif
    }
    #endregion
}
