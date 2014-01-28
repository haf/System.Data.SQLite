/*
 * Installer.cs --
 *
 * Written by Joe Mistachkin.
 * Released to the public domain, use at your own risk!
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.EnterpriseServices.Internal;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

#if WINDOWS
using System.Runtime.InteropServices;
#endif

using System.Security;

#if NET_20 || NET_35
using System.Security.Permissions;
#endif

using System.Threading;
using System.Windows.Forms;
using System.Xml;
using Microsoft.Win32;

namespace System.Data.SQLite
{
    #region Public Delegates
    internal delegate void TraceCallback(
        string message, /* in */
        string category /* in */
    );

    ///////////////////////////////////////////////////////////////////////////

    internal delegate bool FrameworkConfigCallback(
        string fileName,           /* in */
        string invariant,          /* in */
        string name,               /* in */
        string description,        /* in */
        string typeName,           /* in */
        AssemblyName assemblyName, /* in */
        string directory,          /* in */
        object clientData,         /* in */
        bool perUser,              /* in */
        bool wow64,                /* in */
        bool throwOnMissing,       /* in */
        bool whatIf,               /* in */
        bool verbose,              /* in */
        ref bool saved,            /* out */
        ref string error           /* out */
    );

    ///////////////////////////////////////////////////////////////////////////

    internal delegate bool FrameworkRegistryCallback(
        Installer.MockRegistryKey rootKey, /* in */
        string frameworkName,              /* in */
        Version frameworkVersion,          /* in */
        string platformName,               /* in */
        string directory,                  /* in */
        object clientData,                 /* in */
        bool perUser,                      /* in */
        bool wow64,                        /* in */
        bool throwOnMissing,               /* in */
        bool whatIf,                       /* in */
        bool verbose,                      /* in */
        ref string error                   /* out */
    );

    ///////////////////////////////////////////////////////////////////////////

    internal delegate bool VisualStudioRegistryCallback(
        Installer.MockRegistryKey rootKey, /* in */
        Version vsVersion,                 /* in */
        string suffix,                     /* in, optional */
        Installer.Package package,         /* in */
        string directory,                  /* in */
        object clientData,                 /* in */
        bool perUser,                      /* in */
        bool wow64,                        /* in */
        bool throwOnMissing,               /* in */
        bool whatIf,                       /* in */
        bool verbose,                      /* in */
        ref string error                   /* out */
    );
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Public Enumerations
    [Flags()]
    public enum InstallFlags
    {
        #region Normal Flags
        None = 0x0,
        GlobalAssemblyCache = 0x1,
        AssemblyFolders = 0x2,
        DbProviderFactory = 0x4,
        VsPackage = 0x8,
        VsPackageGlobalAssemblyCache = 0x10,
        VsDataSource = 0x20,
        VsDataProvider = 0x40,
        VsDevEnvSetup = 0x80,
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Composite Flags
        Framework = GlobalAssemblyCache | AssemblyFolders |
                    DbProviderFactory,

        ///////////////////////////////////////////////////////////////////////

        Vs = VsPackage | VsPackageGlobalAssemblyCache | VsDataSource |
             VsDataProvider | VsDevEnvSetup,

        ///////////////////////////////////////////////////////////////////////

        All = Framework | Vs,

        ///////////////////////////////////////////////////////////////////////

        AllExceptGlobalAssemblyCache = All & ~(GlobalAssemblyCache |
                                       VsPackageGlobalAssemblyCache),
        #endregion

        ///////////////////////////////////////////////////////////////////////

        Default = All
    }

    ///////////////////////////////////////////////////////////////////////////

    [Flags()]
    public enum TracePriority
    {
        None = 0x0,
        Lowest = 0x1,
        Lower = 0x2,
        Low = 0x4,
        MediumLow = 0x8,
        Medium = 0x10,
        MediumHigh = 0x20,
        High = 0x40,
        Higher = 0x80,
        Highest = 0x100,
        Default = Medium
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Installer Class
#if NET_40 || NET_45 || NET_451
    [SecurityCritical()]
#else
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
    internal static class Installer
    {
        #region Unsafe Native Methods Class
        [SuppressUnmanagedCodeSecurity()]
        private sealed class UnsafeNativeMethods
        {
#if WINDOWS
            #region Native Win32 Constants
            private const int MAX_PATH = 260;

            ///////////////////////////////////////////////////////////////////

            private const int CSIDL_SYSTEMX86 = 0x0029;

            ///////////////////////////////////////////////////////////////////

            private const int SHGFP_TYPE_CURRENT = 0;

            ///////////////////////////////////////////////////////////////////

            private const int S_OK = 0; /* HRESULT */
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Native Windows Methods
            [DllImport("shfolder.dll",
                CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, BestFitMapping = false,
                ThrowOnUnmappableChar = true, SetLastError = true)]
            private static extern int SHGetFolderPath(
                IntPtr hWndOwner, int nFolder, IntPtr hToken, uint flags,
                IntPtr buffer /* >= MAX_PATH */);
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Wrapper Methods
            public static string GetSystemDirectory()
            {
                IntPtr buffer = IntPtr.Zero;

                try
                {
                    buffer = Marshal.AllocCoTaskMem(
                        sizeof(char) * (MAX_PATH + 1));

                    if (buffer != IntPtr.Zero)
                    {
                        if (SHGetFolderPath(IntPtr.Zero,
                                CSIDL_SYSTEMX86, IntPtr.Zero,
                                SHGFP_TYPE_CURRENT, buffer) == S_OK)
                        {
                            return Marshal.PtrToStringAuto(buffer);
                        }
                    }
                }
                catch (Exception e)
                {
                    //
                    // TODO: Is this the right error handling solution
                    //       to use at this point?
                    //
                    TraceOps.DebugAndTrace(TracePriority.MediumHigh,
                        debugCallback, traceCallback, String.Format(
                        "Could not get system directory: {0}", e),
                        traceCategory);

                    throw;
                }
                finally
                {
                    if (buffer != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(buffer);
                        buffer = IntPtr.Zero;
                    }
                }

                return null;
            }
            #endregion
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Helper Classes
        #region AnyPair Class
        private sealed class AnyPair<T1, T2>
        {
            #region Public Constructors
            //
            // WARNING: This constructor produces an immutable "empty" pair
            //          object.
            //
            public AnyPair()
                : base()
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////

            public AnyPair(T1 x)
                : this()
            {
                this.x = x;
            }

            ///////////////////////////////////////////////////////////////////

            public AnyPair(T1 x, T2 y)
                : this(x)
            {
                this.y = y;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Properties
            private T1 x;
            public T1 X
            {
                get { return x; }
            }

            ///////////////////////////////////////////////////////////////////

            private T2 y;
            public T2 Y
            {
                get { return y; }
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region TraceOps Class
        private static class TraceOps
        {
            #region Private Constants
            private const string DefaultDebugFormat = "#{0:000} @ {1}: {2}";
            private const string DefaultTraceFormat = "#{0:000} @ {1}: {2}";

            private const string Iso8601DateTimeOutputFormat =
                "yyyy.MM.ddTHH:mm:ss.fffffff";
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Static Data
            private static object syncRoot = new object();
            private static long nextDebugId;
            private static long nextTraceId;
            private static IList<TraceListener> debugListeners;
            private static TracePriority debugPriority = TracePriority.Default;
            private static TracePriority tracePriority = TracePriority.Default;
            private static string debugFormat = DefaultDebugFormat;
            private static string traceFormat = DefaultTraceFormat;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Static Properties
            public static TracePriority DebugPriority
            {
                get { lock (syncRoot) { return debugPriority; } }
                set { lock (syncRoot) { debugPriority = value; } }
            }

            ///////////////////////////////////////////////////////////////////

            public static TracePriority TracePriority
            {
                get { lock (syncRoot) { return tracePriority; } }
                set { lock (syncRoot) { tracePriority = value; } }
            }

            ///////////////////////////////////////////////////////////////////

            public static string DebugFormat
            {
                get { lock (syncRoot) { return debugFormat; } }
                set { lock (syncRoot) { debugFormat = value; } }
            }

            ///////////////////////////////////////////////////////////////////

            public static string TraceFormat
            {
                get { lock (syncRoot) { return traceFormat; } }
                set { lock (syncRoot) { traceFormat = value; } }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Interactive Support Methods
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static DialogResult ShowMessage(
                TracePriority tracePriority,
                TraceCallback debugCallback,
                TraceCallback traceCallback,
                Assembly assembly,
                string message,
                string category,
                MessageBoxButtons buttons,
                MessageBoxIcon icon
                )
            {
                DialogResult result = DialogResult.OK;

                DebugAndTrace(tracePriority,
                    debugCallback, traceCallback, message, category);

                if (SystemInformation.UserInteractive)
                {
                    string title = GetAssemblyTitle(assembly);

                    if (title == null)
                        title = Application.ProductName;

                    result = MessageBox.Show(message, title, buttons, icon);

                    DebugAndTrace(tracePriority,
                        debugCallback, traceCallback, String.Format(
                        "User choice of {0}.", ForDisplay(result)),
                        category);

                    return result;
                }

                DebugAndTrace(tracePriority,
                    debugCallback, traceCallback, String.Format(
                    "Default choice of {0}.", ForDisplay(result)),
                    category);

                return result;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Tracing Support Methods
            public static void SetupDebugListeners()
            {
                if (debugListeners == null)
                    debugListeners = new List<TraceListener>();

                debugListeners.Add(new ConsoleTraceListener());
            }

            ///////////////////////////////////////////////////////////////////

            public static long NextDebugId()
            {
                return Interlocked.Increment(ref nextDebugId);
            }

            ///////////////////////////////////////////////////////////////////

            public static long NextTraceId()
            {
                return Interlocked.Increment(ref nextTraceId);
            }

            ///////////////////////////////////////////////////////////////////

            public static string TimeStamp(DateTime dateTime)
            {
                return dateTime.ToString(Iso8601DateTimeOutputFormat);
            }

            ///////////////////////////////////////////////////////////////////

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static string GetMethodName(
                StackTrace stackTrace,
                int level
                )
            {
                try
                {
                    //
                    // NOTE: If a valid stack trace was not supplied by the
                    //       caller, create one now based on the current
                    //       execution stack.
                    //
                    if (stackTrace == null)
                    {
                        //
                        // NOTE: Grab the current execution stack.
                        //
                        stackTrace = new StackTrace();

                        //
                        // NOTE: Always skip this call frame when we capture
                        //       the stack trace.
                        //
                        level++;
                    }

                    //
                    // NOTE: Get the specified stack frame (always add one to
                    //       skip this method).
                    //
                    StackFrame stackFrame = stackTrace.GetFrame(level);

                    //
                    // NOTE: Get the method for the stack frame.
                    //
                    MethodBase methodBase = stackFrame.GetMethod();

                    //
                    // NOTE: Get the type for the method.
                    //
                    Type type = methodBase.DeclaringType;

                    //
                    // NOTE: Get the name of the method.
                    //
                    string name = methodBase.Name;

                    //
                    // NOTE: Return the properly formatted result.
                    //
                    return String.Format(
                        "{0}{1}{2}", type.Name, Type.Delimiter, name);
                }
                catch
                {
                    // do nothing.
                }

                return null;
            }

            ///////////////////////////////////////////////////////////////////

            public static void DebugCore(
                string message,
                string category
                )
            {
                lock (syncRoot)
                {
                    if (debugListeners != null)
                    {
                        foreach (TraceListener listener in debugListeners)
                        {
                            listener.WriteLine(message, category);
                            listener.Flush();
                        }
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////

            public static void TraceCore(
                string message,
                string category
                )
            {
                lock (syncRoot)
                {
                    //
                    // NOTE: Write the message to all the active trace
                    //       listeners.
                    //
                    Trace.WriteLine(message, category);
                    Trace.Flush();
                }
            }

            ///////////////////////////////////////////////////////////////////

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static string DebugAndTrace(
                TracePriority tracePriority,
                TraceCallback debugCallback,
                TraceCallback traceCallback,
                Exception exception,
                string category
                )
            {
                if (exception != null)
                    return DebugAndTrace(tracePriority, debugCallback,
                        traceCallback, new StackTrace(exception, true), 0,
                        exception.ToString(), category);

                return null;
            }

            ///////////////////////////////////////////////////////////////////

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static string DebugAndTrace(
                TracePriority tracePriority,
                TraceCallback debugCallback,
                TraceCallback traceCallback,
                string message,
                string category
                )
            {
                return DebugAndTrace(
                    tracePriority, debugCallback, traceCallback, null, 1,
                    message, category);
            }

            ///////////////////////////////////////////////////////////////////

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static string DebugAndTrace(
                TracePriority tracePriority,
                TraceCallback debugCallback,
                TraceCallback traceCallback,
                StackTrace stackTrace,
                int level,
                string message,
                string category
                )
            {
                //
                // NOTE: Always skip this call frame if the stack trace is
                //       going to be captured by GetMethodName.
                //
                if (stackTrace == null)
                    level++;

                //
                // NOTE: Format the message for display (once).
                //
                string formatted = String.Format("{0}: {1}",
                    GetMethodName(stackTrace, level), message);

                //
                // NOTE: If the debug callback is invalid or the trace priority
                //       of this message is less than what we currently want to
                //       debug, skip it.
                //
                if ((debugCallback != null) &&
                    (tracePriority >= DebugPriority))
                {
                    //
                    // NOTE: Invoke the debug callback with the formatted
                    //       message and the category specified by the
                    //       caller.
                    //
                    debugCallback(formatted, category);
                }

                //
                // NOTE: If the trace callback is invalid or the trace priority
                //       of this message is less than what we currently want to
                //       trace, skip it.
                //
                if ((traceCallback != null) &&
                    (tracePriority >= TracePriority))
                {
                    //
                    // NOTE: Invoke the trace callback with the formatted
                    //       message and the category specified by the
                    //       caller.
                    //
                    traceCallback(formatted, category);
                }

                return message;
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region MockRegistry Class
        private sealed class MockRegistry : IDisposable
        {
            #region Public Constructors
            public MockRegistry()
            {
                whatIf = true;
                readOnly = true;
                safe = true;
            }

            ///////////////////////////////////////////////////////////////////

            public MockRegistry(
                bool whatIf
                )
                : this()
            {
                this.whatIf = whatIf;
            }

            ///////////////////////////////////////////////////////////////////

            public MockRegistry(
                bool whatIf,
                bool readOnly
                )
                : this(whatIf)
            {
                this.readOnly = readOnly;
            }

            ///////////////////////////////////////////////////////////////////

            public MockRegistry(
                bool whatIf,
                bool readOnly,
                bool safe
                )
                : this(whatIf, readOnly)
            {
                this.safe = safe;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Properties
            private bool whatIf;
            public bool WhatIf
            {
                get { CheckDisposed(); return whatIf; }
                set { CheckDisposed(); whatIf = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool readOnly;
            public bool ReadOnly
            {
                get { CheckDisposed(); return readOnly; }
                set { CheckDisposed(); readOnly = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool safe;
            public bool Safe
            {
                get { CheckDisposed(); return safe; }
                set { CheckDisposed(); safe = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private MockRegistryKey classesRoot;
            public MockRegistryKey ClassesRoot
            {
                get
                {
                    CheckDisposed();

                    if (classesRoot == null)
                        classesRoot = new MockRegistryKey(
                            Registry.ClassesRoot, whatIf, readOnly, safe);

                    return classesRoot;
                }
            }

            ///////////////////////////////////////////////////////////////////

            private MockRegistryKey currentConfig;
            public MockRegistryKey CurrentConfig
            {
                get
                {
                    CheckDisposed();

                    if (currentConfig == null)
                        currentConfig = new MockRegistryKey(
                            Registry.CurrentConfig, whatIf, readOnly, safe);

                    return currentConfig;
                }
            }

            ///////////////////////////////////////////////////////////////////

            private MockRegistryKey currentUser;
            public MockRegistryKey CurrentUser
            {
                get
                {
                    CheckDisposed();

                    if (currentUser == null)
                        currentUser = new MockRegistryKey(
                            Registry.CurrentUser, whatIf, readOnly, safe);

                    return currentUser;
                }
            }

            ///////////////////////////////////////////////////////////////////

            private MockRegistryKey dynData;
            public MockRegistryKey DynData
            {
                get
                {
                    CheckDisposed();

                    if (dynData == null)
                        dynData = new MockRegistryKey(
                            Registry.DynData, whatIf, readOnly, safe);

                    return dynData;
                }
            }

            ///////////////////////////////////////////////////////////////////

            private MockRegistryKey localMachine;
            public MockRegistryKey LocalMachine
            {
                get
                {
                    CheckDisposed();

                    if (localMachine == null)
                        localMachine = new MockRegistryKey(
                            Registry.LocalMachine, whatIf, readOnly, safe);

                    return localMachine;
                }
            }

            ///////////////////////////////////////////////////////////////////

            private MockRegistryKey performanceData;
            public MockRegistryKey PerformanceData
            {
                get
                {
                    CheckDisposed();

                    if (performanceData == null)
                        performanceData = new MockRegistryKey(
                            Registry.PerformanceData, whatIf, readOnly, safe);

                    return performanceData;
                }
            }

            ///////////////////////////////////////////////////////////////////

            private MockRegistryKey users;
            public MockRegistryKey Users
            {
                get
                {
                    CheckDisposed();

                    if (users == null)
                        users = new MockRegistryKey(
                            Registry.Users, whatIf, readOnly, safe);

                    return users;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public "Registry" Methods
            public object GetValue(
                string keyName,
                string valueName,
                object defaultValue
                )
            {
                CheckDisposed();

                return Registry.GetValue(keyName, valueName, defaultValue);
            }

            ///////////////////////////////////////////////////////////////////

            public void SetValue(
                string keyName,
                string valueName,
                object value
                )
            {
                CheckDisposed();
                CheckReadOnly();

                if (!whatIf)
                    Registry.SetValue(keyName, valueName, value);
            }

            ///////////////////////////////////////////////////////////////////

            public void SetValue(
                string keyName,
                string valueName,
                object value,
                RegistryValueKind valueKind
                )
            {
                CheckDisposed();
                CheckReadOnly();

                if (!whatIf)
                    Registry.SetValue(keyName, valueName, value, valueKind);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Methods
            private void CheckReadOnly()
            {
                //
                // NOTE: In "read-only" mode, we disallow all write access.
                //
                if (!readOnly)
                    return;

                throw new InvalidOperationException();
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
                if (!disposed)
                    return;

                throw new ObjectDisposedException(
                    typeof(MockRegistry).Name);
            }

            ///////////////////////////////////////////////////////////////////

            private /* protected virtual */ void Dispose(
                bool disposing
                )
            {
                if (!disposed)
                {
                    if (disposing)
                    {
                        ////////////////////////////////////
                        // dispose managed resources here...
                        ////////////////////////////////////

                        if (classesRoot != null)
                        {
                            classesRoot.Close();
                            classesRoot = null;
                        }

                        if (currentConfig != null)
                        {
                            currentConfig.Close();
                            currentConfig = null;
                        }

                        if (currentUser != null)
                        {
                            currentUser.Close();
                            currentUser = null;
                        }

                        if (dynData != null)
                        {
                            dynData.Close();
                            dynData = null;
                        }

                        if (localMachine != null)
                        {
                            localMachine.Close();
                            localMachine = null;
                        }

                        if (performanceData != null)
                        {
                            performanceData.Close();
                            performanceData = null;
                        }

                        if (users != null)
                        {
                            users.Close();
                            users = null;
                        }
                    }

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////

                    //
                    // NOTE: This object is now disposed.
                    //
                    disposed = true;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region IDisposable Members
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Destructor
            ~MockRegistry()
            {
                Dispose(false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region MockRegistryKey Class
        internal sealed class MockRegistryKey : IDisposable
        {
            #region Private Constructors
            private MockRegistryKey()
            {
                whatIf = true;
                readOnly = true;
                safe = true;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Constructors
            public MockRegistryKey(
                RegistryKey key
                )
                : this()
            {
                this.key = key;
            }

            ///////////////////////////////////////////////////////////////////

            public MockRegistryKey(
                RegistryKey key,
                string subKeyName
                )
                : this(key)
            {
                this.subKeyName = subKeyName;
            }

            ///////////////////////////////////////////////////////////////////

            public MockRegistryKey(
                RegistryKey key,
                string subKeyName,
                bool whatIf
                )
                : this(key, subKeyName)
            {
                this.whatIf = whatIf;
            }

            ///////////////////////////////////////////////////////////////////

            public MockRegistryKey(
                RegistryKey key,
                string subKeyName,
                bool whatIf,
                bool readOnly
                )
                : this(key, subKeyName, whatIf)
            {
                this.readOnly = readOnly;
            }

            ///////////////////////////////////////////////////////////////////

            public MockRegistryKey(
                RegistryKey key,
                string subKeyName,
                bool whatIf,
                bool readOnly,
                bool safe
                )
                : this(key, subKeyName, whatIf, readOnly)
            {
                this.safe = safe;
            }

            ///////////////////////////////////////////////////////////////////

            public MockRegistryKey(
                RegistryKey key,
                bool whatIf
                )
                : this(key, null, whatIf)
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////

            public MockRegistryKey(
                RegistryKey key,
                bool whatIf,
                bool readOnly
                )
                : this(key, null, whatIf, readOnly)
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////

            public MockRegistryKey(
                RegistryKey key,
                bool whatIf,
                bool readOnly,
                bool safe
                )
                : this(key, null, whatIf, readOnly, safe)
            {
                // do nothing.
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Methods
            public void Close()
            {
                //
                // NOTE: No disposed check here because calling this method
                //       should be just like calling Dispose.
                //
                Dispose(true);
            }

            ///////////////////////////////////////////////////////////////////

            public MockRegistryKey CreateSubKey(
                string subKeyName
                )
            {
                CheckDisposed();
                CheckReadOnly();

                if (key == null)
                    return null;

                if (whatIf)
                {
                    //
                    // HACK: Attempt to open the specified sub-key.  If this
                    //       fails, we will simply return the wrapped root key
                    //       itself since no writes are allowed in "what-if"
                    //       mode anyhow.
                    //
                    RegistryKey subKey = key.OpenSubKey(subKeyName);

                    return (subKey != null) ?
                        new MockRegistryKey(
                                subKey, whatIf, readOnly, safe) :
                        new MockRegistryKey(
                                key, subKeyName, whatIf, readOnly, safe);
                }
                else
                {
                    return new MockRegistryKey(
                        key.CreateSubKey(subKeyName), whatIf, readOnly, safe);
                }
            }

            ///////////////////////////////////////////////////////////////////

            public void DeleteSubKey(
                string subKeyName,
                bool throwOnMissing
                )
            {
                CheckDisposed();
                CheckReadOnly();

                if (key == null)
                    return;

                if (!whatIf)
                    key.DeleteSubKey(subKeyName, throwOnMissing);
            }

            ///////////////////////////////////////////////////////////////////

            public void DeleteSubKeyTree(
                string subKeyName
                )
            {
                CheckDisposed();
                CheckReadOnly();

                if (key == null)
                    return;

                if (!whatIf)
                    key.DeleteSubKeyTree(subKeyName);
            }

            ///////////////////////////////////////////////////////////////////

            public void DeleteValue(
                string name,
                bool throwOnMissing
                )
            {
                CheckDisposed();
                CheckReadOnly();

                if (key == null)
                    return;

                if (!whatIf)
                    key.DeleteValue(name, throwOnMissing);
            }

            ///////////////////////////////////////////////////////////////////

            public string[] GetSubKeyNames()
            {
                CheckDisposed();

                if (key == null)
                    return null;

                return key.GetSubKeyNames();
            }

            ///////////////////////////////////////////////////////////////////

            public object GetValue(
                string name,
                object defaultValue
                )
            {
                CheckDisposed();

                if (key == null)
                    return null;

                return key.GetValue(name, defaultValue);
            }

            ///////////////////////////////////////////////////////////////////

            public MockRegistryKey OpenSubKey(
                string subKeyName
                )
            {
                CheckDisposed();

                return OpenSubKey(subKeyName, false);
            }

            ///////////////////////////////////////////////////////////////////

            public MockRegistryKey OpenSubKey(
                string subKeyName,
                bool writable
                )
            {
                CheckDisposed();

                if (writable)
                    CheckReadOnly();

                if (key == null)
                    return null;

                RegistryKey subKey = key.OpenSubKey(
                    subKeyName, whatIf ? false : writable);

                return (subKey != null) ?
                    new MockRegistryKey(subKey, whatIf, readOnly, safe) : null;
            }

            ///////////////////////////////////////////////////////////////////

            public void SetValue(
                string name,
                object value
                )
            {
                CheckDisposed();
                CheckReadOnly();

                if (key == null)
                    return;

                if (!whatIf)
                    key.SetValue(name, value);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Properties
            public string Name
            {
                get
                {
                    CheckDisposed();

                    if (key == null)
                        return null;

                    return !String.IsNullOrEmpty(subKeyName) ?
                        String.Format("{0}\\{1}", key.Name, subKeyName) :
                        key.Name;
                }
            }

            ///////////////////////////////////////////////////////////////////

            private RegistryKey key;
            public RegistryKey Key
            {
                get { CheckDisposed(); CheckSafe(); return key; }
            }

            ///////////////////////////////////////////////////////////////////

            private string subKeyName;
            public string SubKeyName
            {
                get { CheckDisposed(); return subKeyName; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool whatIf;
            public bool WhatIf
            {
                get { CheckDisposed(); return whatIf; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool readOnly;
            public bool ReadOnly
            {
                get { CheckDisposed(); return readOnly; }
            }

            ///////////////////////////////////////////////////////////////////

            public bool safe;
            public bool Safe
            {
                get { CheckDisposed(); return safe; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Methods
            private void CheckReadOnly()
            {
                //
                // NOTE: In "read-only" mode, we disallow all write access.
                //
                if (!readOnly)
                    return;

                throw new InvalidOperationException();
            }

            ///////////////////////////////////////////////////////////////////

            private void CheckSafe()
            {
                //
                // NOTE: In "safe" mode, we disallow all direct access to the
                //       contained registry key.
                //
                if (!safe)
                    return;

                throw new InvalidOperationException();
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region System.Object Overrides
            public override string ToString()
            {
                CheckDisposed();

                return this.Name;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Implicit Conversion Operators
            //
            // BUGBUG: Remove me?  This should be safe because in "what-if"
            //         mode all keys are opened read-only.
            //
            public static implicit operator RegistryKey(
                MockRegistryKey key
                )
            {
                return (key != null) ? key.Key : null;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
                if (!disposed)
                    return;

                throw new ObjectDisposedException(
                    typeof(MockRegistryKey).Name);
            }

            ///////////////////////////////////////////////////////////////////

            private /* protected virtual */ void Dispose(
                bool disposing
                )
            {
                if (!disposed)
                {
                    if (disposing)
                    {
                        ////////////////////////////////////
                        // dispose managed resources here...
                        ////////////////////////////////////

                        if (key != null)
                        {
                            key.Close();
                            key = null;
                        }
                    }

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////

                    //
                    // NOTE: This object is now disposed.
                    //
                    disposed = true;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region IDisposable Members
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Destructor
            ~MockRegistryKey()
            {
                Dispose(false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region RegistryHelper Class
        private static class RegistryHelper
        {
            #region Public Static Properties
            private static int subKeysCreated;
            public static int SubKeysCreated
            {
                get { return subKeysCreated; }
            }

            ///////////////////////////////////////////////////////////////////

            private static int subKeysDeleted;
            public static int SubKeysDeleted
            {
                get { return subKeysDeleted; }
            }

            ///////////////////////////////////////////////////////////////////

            private static int keyValuesSet;
            public static int KeyValuesSet
            {
                get { return keyValuesSet; }
            }

            ///////////////////////////////////////////////////////////////////

            private static int keyValuesDeleted;
            public static int KeyValuesDeleted
            {
                get { return keyValuesDeleted; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Static Methods
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static MockRegistryKey OpenSubKey(
                MockRegistryKey rootKey,
                string subKeyName,
                bool writable,
                bool whatIf,
                bool verbose
                )
            {
                if (rootKey == null)
                    return null;

                if (verbose)
                    TraceOps.DebugAndTrace(writable ?
                        TracePriority.Highest : TracePriority.Higher,
                        debugCallback, traceCallback, String.Format(
                        "rootKey = {0}, subKeyName = {1}, writable = {2}",
                        ForDisplay(rootKey), ForDisplay(subKeyName),
                        ForDisplay(writable)), traceCategory);

                //
                // HACK: Always forbid writable access when operating in
                //       "what-if" mode.
                //
                MockRegistryKey key = rootKey.OpenSubKey(
                    subKeyName, whatIf ? false : writable);

                return (key != null) ?
                    new MockRegistryKey(key, whatIf, false, false) : null;
            }

            ///////////////////////////////////////////////////////////////////

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static MockRegistryKey CreateSubKey(
                MockRegistryKey rootKey,
                string subKeyName,
                bool whatIf,
                bool verbose
                )
            {
                if (rootKey == null)
                    return null;

                if (verbose)
                    TraceOps.DebugAndTrace(TracePriority.Highest,
                        debugCallback, traceCallback, String.Format(
                        "rootKey = {0}, subKeyName = {1}",
                        ForDisplay(rootKey), ForDisplay(subKeyName)),
                        traceCategory);

                try
                {
                    //
                    // HACK: Always open a key, rather than creating one when
                    //       operating in "what-if" mode.
                    //
                    if (whatIf)
                    {
                        //
                        // HACK: Attempt to open the specified sub-key.  If
                        //       this fails, we will simply return the root
                        //       key itself since no writes are allowed in
                        //       "what-if" mode anyhow.
                        //
                        MockRegistryKey key = rootKey.OpenSubKey(subKeyName);

                        return (key != null) ?
                            key : new MockRegistryKey(
                                rootKey, subKeyName, whatIf, false, false);
                    }
                    else
                    {
                        return new MockRegistryKey(
                            rootKey.CreateSubKey(subKeyName), whatIf, false,
                            false);
                    }
                }
                finally
                {
                    subKeysCreated++;
                }
            }

            ///////////////////////////////////////////////////////////////////

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void DeleteSubKey(
                MockRegistryKey rootKey,
                string subKeyName,
                bool throwOnMissing,
                bool whatIf,
                bool verbose
                )
            {
                if (rootKey == null)
                    return;

                if (verbose)
                    TraceOps.DebugAndTrace(TracePriority.Highest,
                        debugCallback, traceCallback, String.Format(
                        "rootKey = {0}, subKeyName = {1}",
                        ForDisplay(rootKey), ForDisplay(subKeyName)),
                        traceCategory);

                if (!whatIf)
                    rootKey.DeleteSubKey(subKeyName, throwOnMissing);

                subKeysDeleted++;
            }

            ///////////////////////////////////////////////////////////////////

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void DeleteSubKeyTree(
                MockRegistryKey rootKey,
                string subKeyName,
                bool whatIf,
                bool verbose
                )
            {
                if (rootKey == null)
                    return;

                if (verbose)
                    TraceOps.DebugAndTrace(TracePriority.Highest,
                        debugCallback, traceCallback, String.Format(
                        "rootKey = {0}, subKeyName = {1}",
                        ForDisplay(rootKey), ForDisplay(subKeyName)),
                        traceCategory);

                if (!whatIf)
                    rootKey.DeleteSubKeyTree(subKeyName);

                subKeysDeleted++;
            }

            ///////////////////////////////////////////////////////////////////

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static string[] GetSubKeyNames(
                MockRegistryKey key,
                bool whatIf,
                bool verbose
                )
            {
                if (key == null)
                    return null;

                if (verbose)
                    TraceOps.DebugAndTrace(TracePriority.High,
                        debugCallback, traceCallback, String.Format(
                        "key = {0}", ForDisplay(key)), traceCategory);

                return key.GetSubKeyNames();
            }

            ///////////////////////////////////////////////////////////////////

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static object GetValue(
                MockRegistryKey key,
                string name,
                object defaultValue,
                bool whatIf,
                bool verbose
                )
            {
                if (key == null)
                    return null;

                if (verbose)
                    TraceOps.DebugAndTrace(TracePriority.High,
                        debugCallback, traceCallback, String.Format(
                        "key = {0}, name = {1}, defaultValue = {2}",
                        ForDisplay(key), ForDisplay(name),
                        ForDisplay(defaultValue)), traceCategory);

                return key.GetValue(name, defaultValue);
            }

            ///////////////////////////////////////////////////////////////////

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void SetValue(
                MockRegistryKey key,
                string name,
                object value,
                bool whatIf,
                bool verbose
                )
            {
                if (key == null)
                    return;

                if (verbose)
                    TraceOps.DebugAndTrace(TracePriority.Highest,
                        debugCallback, traceCallback, String.Format(
                        "key = {0}, name = {1}, value = {2}",
                        ForDisplay(key), ForDisplay(name), ForDisplay(value)),
                        traceCategory);

                if (!whatIf)
                    key.SetValue(name, value);

                keyValuesSet++;
            }

            ///////////////////////////////////////////////////////////////////

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void DeleteValue(
                MockRegistryKey key,
                string name,
                bool throwOnMissing,
                bool whatIf,
                bool verbose
                )
            {
                if (key == null)
                    return;

                if (verbose)
                    TraceOps.DebugAndTrace(TracePriority.Highest,
                        debugCallback, traceCallback, String.Format(
                        "key = {0}, name = {1}", ForDisplay(key),
                        ForDisplay(name)), traceCategory);

                if (!whatIf)
                    key.DeleteValue(name, throwOnMissing);

                keyValuesDeleted++;
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region StringList Class
        private sealed class StringList : List<string>
        {
            public StringList()
                : base()
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////

            public StringList(IEnumerable<string> collection)
                : base(collection)
            {
                // do nothing.
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region StringDictionary Class
        private sealed class StringDictionary : Dictionary<string, string>
        {
            public StringDictionary()
            {
                // do nothing.
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region VersionList Class
        private sealed class VersionList : List<Version>
        {
            public VersionList()
                : base()
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////

            public VersionList(IEnumerable<Version> collection)
                : base(collection)
            {
                // do nothing.
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region VersionMap Class
        private sealed class VersionMap : Dictionary<string, VersionList>
        {
            public VersionMap()
            {
                // do nothing.
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Package Class
        internal sealed class Package
        {
            #region Public Constructors
            public Package()
            {
                // do nothing.
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Properties
            private AssemblyName assemblyName;
            public AssemblyName AssemblyName
            {
                get { return assemblyName; }
                set { assemblyName = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool globalAssemblyCache;
            public bool GlobalAssemblyCache
            {
                get { return globalAssemblyCache; }
                set { globalAssemblyCache = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private Guid packageId;
            public Guid PackageId
            {
                get { return packageId; }
                set { packageId = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private Guid serviceId;
            public Guid ServiceId
            {
                get { return serviceId; }
                set { serviceId = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private Guid dataSourceId;
            public Guid DataSourceId
            {
                get { return dataSourceId; }
                set { dataSourceId = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private Guid dataProviderId;
            public Guid DataProviderId
            {
                get { return dataProviderId; }
                set { dataProviderId = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private Guid adoNetTechnologyId;
            public Guid AdoNetTechnologyId
            {
                get { return adoNetTechnologyId; }
                set { adoNetTechnologyId = value; }
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Configuration Class
        private sealed class Configuration
        {
            #region Private Constants
            private const char Switch = '-';
            private const char AltSwitch = '/';

            ///////////////////////////////////////////////////////////////////

            private static readonly char[] SwitchChars = {
                Switch, AltSwitch
            };
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Constructors
            private Configuration(
                Assembly assembly,
                string logFileName,
                string directory,
                string coreFileName,
                string linqFileName,
                string ef6FileName,
                string designerFileName,
                string registryVersion,
                string configVersion,
                string vsVersionSuffix,
                string debugFormat,
                string traceFormat,
                InstallFlags installFlags,
                TracePriority debugPriority,
                TracePriority tracePriority,
                bool perUser,
                bool install,
                bool wow64,
                bool noRuntimeVersion,
                bool noDesktop,
                bool noCompact,
                bool noNetFx20,
                bool noNetFx35,
                bool noNetFx40,
                bool noNetFx45,
                bool noNetFx451,
                bool noVs2005,
                bool noVs2008,
                bool noVs2010,
                bool noVs2012,
                bool noVs2013,
                bool noTrace,
                bool noConsole,
                bool noLog,
                bool throwOnMissing,
                bool whatIf,
                bool debug,
                bool verbose,
                bool confirm
                )
            {
                this.assembly = assembly;
                this.logFileName = logFileName;
                this.directory = directory;
                this.coreFileName = coreFileName;
                this.linqFileName = linqFileName;
                this.ef6FileName = ef6FileName;
                this.designerFileName = designerFileName;
                this.registryVersion = registryVersion;
                this.configVersion = configVersion;
                this.vsVersionSuffix = vsVersionSuffix;
                this.debugFormat = debugFormat;
                this.traceFormat = traceFormat;
                this.installFlags = installFlags;
                this.debugPriority = debugPriority;
                this.tracePriority = tracePriority;
                this.perUser = perUser;
                this.install = install;
                this.wow64 = wow64;
                this.noRuntimeVersion = noRuntimeVersion;
                this.noDesktop = noDesktop;
                this.noCompact = noCompact;
                this.noNetFx20 = noNetFx20;
                this.noNetFx35 = noNetFx35;
                this.noNetFx40 = noNetFx40;
                this.noNetFx45 = noNetFx45;
                this.noNetFx451 = noNetFx451;
                this.noVs2005 = noVs2005;
                this.noVs2008 = noVs2008;
                this.noVs2010 = noVs2010;
                this.noVs2012 = noVs2012;
                this.noVs2013 = noVs2013;
                this.noTrace = noTrace;
                this.noConsole = noConsole;
                this.noLog = noLog;
                this.throwOnMissing = throwOnMissing;
                this.whatIf = whatIf;
                this.debug = debug;
                this.verbose = verbose;
                this.confirm = confirm;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Static Methods
            private static void GetDefaultFileNames(
                ref string directory,
                ref string coreFileName,
                ref string linqFileName,
                ref string ef6FileName,
                ref string designerFileName
                )
            {
                if (thisAssembly == null)
                    return;

                directory = Path.GetDirectoryName(thisAssembly.Location);

                if (String.IsNullOrEmpty(directory))
                    return;

                coreFileName = Path.Combine(directory,
                    Installer.CoreFileName);

                linqFileName = Path.Combine(directory,
                    Installer.LinqFileName);

                ef6FileName = Path.Combine(directory,
                    Installer.Ef6FileName);

                designerFileName = Path.Combine(directory,
                    Installer.DesignerFileName);
            }

            ///////////////////////////////////////////////////////////////////

            private static bool CheckOption(
                ref string arg
                )
            {
                string result = arg;

                if (!String.IsNullOrEmpty(result))
                {
                    //
                    // NOTE: Remove all leading switch chars.
                    //
                    result = result.TrimStart(SwitchChars);

                    //
                    // NOTE: How many chars were removed?
                    //
                    int count = arg.Length - result.Length;

                    //
                    // NOTE: Was there at least one?
                    //
                    if (count > 0)
                    {
                        //
                        // NOTE: Ok, replace their original
                        //       argument.
                        //
                        arg = result;

                        //
                        // NOTE: Yes, this is a switch.
                        //
                        return true;
                    }
                }

                return false;
            }

            ///////////////////////////////////////////////////////////////////

            private static bool MatchOption(
                string arg,
                string option
                )
            {
                if ((arg == null) || (option == null))
                    return false;

                return String.Compare(arg, 0, option, 0,
                    arg.Length, StringComparison.OrdinalIgnoreCase) == 0;
            }

            ///////////////////////////////////////////////////////////////////

            private static bool? ParseBoolean(
                string text
                )
            {
                if (!String.IsNullOrEmpty(text))
                {
                    bool value;

                    if (bool.TryParse(text, out value))
                        return value;
                }

                return null;
            }

            ///////////////////////////////////////////////////////////////////

            private static object ParseEnum(
                Type enumType,
                string text,
                bool noCase
                )
            {
                if ((enumType == null) || !enumType.IsEnum)
                    return null;

                if (!String.IsNullOrEmpty(text))
                {
                    try
                    {
                        return Enum.Parse(enumType, text, noCase);
                    }
                    catch
                    {
                        // do nothing.
                    }
                }

                return null;
            }

            ///////////////////////////////////////////////////////////////////

            private static bool IsEf6AssemblyAvailable()
            {
                try
                {
                    Assembly assembly = Assembly.ReflectionOnlyLoad(
                        Ef6AssemblyName);

                    if (assembly != null)
                    {
                        TraceOps.DebugAndTrace(TracePriority.Highest,
                            debugCallback, traceCallback,
                            "Entity Framework 6 assembly was resolved.",
                            traceCategory);

                        return true;
                    }
                }
                catch
                {
                    // do nothing.
                }

                TraceOps.DebugAndTrace(TracePriority.Highest,
                    debugCallback, traceCallback,
                    "Entity Framework 6 assembly was not resolved.",
                    traceCategory);

                return false;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Static Methods
            public static Configuration CreateDefault()
            {
                string directory = null;
                string coreFileName = null;
                string linqFileName = null;
                string ef6FileName = null;
                string designerFileName = null;

                GetDefaultFileNames(
                    ref directory, ref coreFileName, ref linqFileName,
                    ref ef6FileName, ref designerFileName);

                return new Configuration(
                    thisAssembly, null, directory, coreFileName, linqFileName,
                    ef6FileName, designerFileName, null, null, null,
                    TraceOps.DebugFormat, TraceOps.TraceFormat,
                    InstallFlags.Default, TracePriority.Default,
                    TracePriority.Default, false, true, false, false, false,
                    false, false, false, false, false, false, false, false,
                    false, false, false, false, false, false, true, true,
                    false, false, false);
            }

            ///////////////////////////////////////////////////////////////////

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static bool FromArgs(
                string[] args,
                bool strict,
                ref Configuration configuration,
                ref string error
                )
            {
                try
                {
                    if (args == null)
                        return true;

                    if (configuration == null)
                        configuration = Configuration.CreateDefault();

                    int length = args.Length;

                    for (int index = 0; index < length; index++)
                    {
                        string arg = args[index];

                        //
                        // NOTE: Skip any argument that is null (?) or an empty
                        //       string.
                        //
                        if (String.IsNullOrEmpty(arg))
                            continue;

                        //
                        // NOTE: We are going to modify the original argument
                        //       by removing any leading option characters;
                        //       therefore, we use a new string to hold the
                        //       modified argument.
                        //
                        string newArg = arg;

                        //
                        // NOTE: All the supported command line options must
                        //       begin with an option character (e.g. a minus
                        //       or forward slash); attempt to validate that
                        //       now.  If we fail in strict mode, we are done;
                        //       otherwise, just skip this argument and advance
                        //       to the next one.
                        //
                        if (!CheckOption(ref newArg))
                        {
                            error = TraceOps.DebugAndTrace(
                                TracePriority.Lowest, debugCallback,
                                traceCallback, String.Format(
                                "Unsupported command line argument: {0}",
                                ForDisplay(arg)), traceCategory);

                            if (strict)
                                return false;

                            continue;
                        }

                        //
                        // NOTE: All the supported command line options must
                        //       have a value; therefore, attempt to advance
                        //       to it now.  If we fail, we are done.
                        //
                        index++;

                        if (index >= length)
                        {
                            error = TraceOps.DebugAndTrace(
                                TracePriority.Lowest, debugCallback,
                                traceCallback, String.Format(
                                "Missing value for option: {0}",
                                ForDisplay(arg)), traceCategory);

                            if (strict)
                                return false;

                            break;
                        }

                        //
                        // NOTE: Grab the textual value of this command line
                        //       option.
                        //
                        string text = args[index];

                        //
                        // NOTE: Figure out which command line option this is
                        //       (based on a partial name match) and then try
                        //       to interpret the textual value as the correct
                        //       type.
                        //
                        if (MatchOption(newArg, "configVersion"))
                        {
                            configuration.configVersion = text;
                        }
                        else if (MatchOption(newArg, "confirm"))
                        {
                            bool? value = ParseBoolean(text);

                            if (value == null)
                            {
                                error = TraceOps.DebugAndTrace(
                                    TracePriority.Lowest, debugCallback,
                                    traceCallback, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    ForDisplay(arg), ForDisplay(text)),
                                    traceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.confirm = (bool)value;
                        }
                        else if (MatchOption(newArg, "coreFileName"))
                        {
                            configuration.coreFileName = text;
                        }
                        else if (MatchOption(newArg, "debug"))
                        {
                            bool? value = ParseBoolean(text);

                            if (value == null)
                            {
                                error = TraceOps.DebugAndTrace(
                                    TracePriority.Lowest, debugCallback,
                                    traceCallback, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    ForDisplay(arg), ForDisplay(text)),
                                    traceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.debug = (bool)value;
                        }
                        else if (MatchOption(newArg, "debugFormat"))
                        {
                            configuration.debugFormat = text;
                            TraceOps.DebugFormat = configuration.debugFormat;
                        }
                        else if (MatchOption(newArg, "debugPriority"))
                        {
                            object value = ParseEnum(
                                typeof(TracePriority), text, true);

                            if (value == null)
                            {
                                error = TraceOps.DebugAndTrace(
                                    TracePriority.Lowest, debugCallback,
                                    traceCallback, String.Format(
                                    "Invalid {0} value: {1}",
                                    ForDisplay(arg), ForDisplay(text)),
                                    traceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.debugPriority = (TracePriority)value;
                            TraceOps.DebugPriority = configuration.debugPriority;
                        }
                        else if (MatchOption(newArg, "designerFileName"))
                        {
                            configuration.designerFileName = text;
                        }
                        else if (MatchOption(newArg, "directory"))
                        {
                            configuration.directory = text;

                            //
                            // NOTE: *SPECIAL* Must refresh the file names
                            //       here because the underlying directory
                            //       has changed.
                            //
                            string coreFileName = configuration.coreFileName;

                            if (!String.IsNullOrEmpty(coreFileName))
                                coreFileName = Path.GetFileName(coreFileName);

                            if (String.IsNullOrEmpty(coreFileName))
                                coreFileName = Installer.CoreFileName;

                            configuration.coreFileName = Path.Combine(
                                configuration.directory, coreFileName);

                            string linqFileName = configuration.linqFileName;

                            if (!String.IsNullOrEmpty(linqFileName))
                                linqFileName = Path.GetFileName(linqFileName);

                            if (String.IsNullOrEmpty(linqFileName))
                                linqFileName = Installer.LinqFileName;

                            configuration.linqFileName = Path.Combine(
                                configuration.directory, linqFileName);

                            string ef6FileName = configuration.ef6FileName;

                            if (!String.IsNullOrEmpty(ef6FileName))
                                ef6FileName = Path.GetFileName(ef6FileName);

                            if (String.IsNullOrEmpty(ef6FileName))
                                ef6FileName = Installer.Ef6FileName;

                            configuration.ef6FileName = Path.Combine(
                                configuration.directory, ef6FileName);

                            string designerFileName = configuration.designerFileName;

                            if (!String.IsNullOrEmpty(designerFileName))
                                designerFileName = Path.GetFileName(designerFileName);

                            if (String.IsNullOrEmpty(designerFileName))
                                designerFileName = Installer.DesignerFileName;

                            configuration.designerFileName = Path.Combine(
                                configuration.directory, designerFileName);
                        }
                        else if (MatchOption(newArg, "ef6FileName"))
                        {
                            configuration.ef6FileName = text;
                        }
                        else if (MatchOption(newArg, "install"))
                        {
                            bool? value = ParseBoolean(text);

                            if (value == null)
                            {
                                error = TraceOps.DebugAndTrace(
                                    TracePriority.Lowest, debugCallback,
                                    traceCallback, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    ForDisplay(arg), ForDisplay(text)),
                                    traceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.install = (bool)value;
                        }
                        else if (MatchOption(newArg, "installFlags"))
                        {
                            object value = ParseEnum(
                                typeof(InstallFlags), text, true);

                            if (value == null)
                            {
                                error = TraceOps.DebugAndTrace(
                                    TracePriority.Lowest, debugCallback,
                                    traceCallback, String.Format(
                                    "Invalid install flags value: {0}",
                                    ForDisplay(text)), traceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.installFlags = (InstallFlags)value;
                        }
                        else if (MatchOption(newArg, "linqFileName"))
                        {
                            configuration.linqFileName = text;
                        }
                        else if (MatchOption(newArg, "logFileName"))
                        {
                            configuration.logFileName = text;
                        }
                        else if (MatchOption(newArg, "noCompact"))
                        {
                            bool? value = ParseBoolean(text);

                            if (value == null)
                            {
                                error = TraceOps.DebugAndTrace(
                                    TracePriority.Lowest, debugCallback,
                                    traceCallback, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    ForDisplay(arg), ForDisplay(text)),
                                    traceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.noCompact = (bool)value;
                        }
                        else if (MatchOption(newArg, "noConsole"))
                        {
                            bool? value = ParseBoolean(text);

                            if (value == null)
                            {
                                error = TraceOps.DebugAndTrace(
                                    TracePriority.Lowest, debugCallback,
                                    traceCallback, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    ForDisplay(arg), ForDisplay(text)),
                                    traceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.noConsole = (bool)value;
                        }
                        else if (MatchOption(newArg, "noDesktop"))
                        {
                            bool? value = ParseBoolean(text);

                            if (value == null)
                            {
                                error = TraceOps.DebugAndTrace(
                                    TracePriority.Lowest, debugCallback,
                                    traceCallback, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    ForDisplay(arg), ForDisplay(text)),
                                    traceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.noDesktop = (bool)value;
                        }
                        else if (MatchOption(newArg, "noLog"))
                        {
                            bool? value = ParseBoolean(text);

                            if (value == null)
                            {
                                error = TraceOps.DebugAndTrace(
                                    TracePriority.Lowest, debugCallback,
                                    traceCallback, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    ForDisplay(arg), ForDisplay(text)),
                                    traceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.noLog = (bool)value;
                        }
                        else if (MatchOption(newArg, "noNetFx20"))
                        {
                            bool? value = ParseBoolean(text);

                            if (value == null)
                            {
                                error = TraceOps.DebugAndTrace(
                                    TracePriority.Lowest, debugCallback,
                                    traceCallback, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    ForDisplay(arg), ForDisplay(text)),
                                    traceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.noNetFx20 = (bool)value;
                        }
                        else if (MatchOption(newArg, "noNetFx35"))
                        {
                            bool? value = ParseBoolean(text);

                            if (value == null)
                            {
                                error = TraceOps.DebugAndTrace(
                                    TracePriority.Lowest, debugCallback,
                                    traceCallback, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    ForDisplay(arg), ForDisplay(text)),
                                    traceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.noNetFx35 = (bool)value;
                        }
                        else if (MatchOption(newArg, "noNetFx40"))
                        {
                            bool? value = ParseBoolean(text);

                            if (value == null)
                            {
                                error = TraceOps.DebugAndTrace(
                                    TracePriority.Lowest, debugCallback,
                                    traceCallback, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    ForDisplay(arg), ForDisplay(text)),
                                    traceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.noNetFx40 = (bool)value;
                        }
                        else if (MatchOption(newArg, "noNetFx45"))
                        {
                            bool? value = ParseBoolean(text);

                            if (value == null)
                            {
                                error = TraceOps.DebugAndTrace(
                                    TracePriority.Lowest, debugCallback,
                                    traceCallback, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    ForDisplay(arg), ForDisplay(text)),
                                    traceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.noNetFx45 = (bool)value;
                        }
                        else if (MatchOption(newArg, "noNetFx451"))
                        {
                            bool? value = ParseBoolean(text);

                            if (value == null)
                            {
                                error = TraceOps.DebugAndTrace(
                                    TracePriority.Lowest, debugCallback,
                                    traceCallback, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    ForDisplay(arg), ForDisplay(text)),
                                    traceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.noNetFx451 = (bool)value;
                        }
                        else if (MatchOption(newArg, "noRuntimeVersion"))
                        {
                            bool? value = ParseBoolean(text);

                            if (value == null)
                            {
                                error = TraceOps.DebugAndTrace(
                                    TracePriority.Lowest, debugCallback,
                                    traceCallback, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    ForDisplay(arg), ForDisplay(text)),
                                    traceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.noRuntimeVersion = (bool)value;
                        }
                        else if (MatchOption(newArg, "noTrace"))
                        {
                            bool? value = ParseBoolean(text);

                            if (value == null)
                            {
                                error = TraceOps.DebugAndTrace(
                                    TracePriority.Lowest, debugCallback,
                                    traceCallback, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    ForDisplay(arg), ForDisplay(text)),
                                    traceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.noTrace = (bool)value;
                        }
                        else if (MatchOption(newArg, "noVs2005"))
                        {
                            bool? value = ParseBoolean(text);

                            if (value == null)
                            {
                                error = TraceOps.DebugAndTrace(
                                    TracePriority.Lowest, debugCallback,
                                    traceCallback, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    ForDisplay(arg), ForDisplay(text)),
                                    traceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.noVs2005 = (bool)value;
                        }
                        else if (MatchOption(newArg, "noVs2008"))
                        {
                            bool? value = ParseBoolean(text);

                            if (value == null)
                            {
                                error = TraceOps.DebugAndTrace(
                                    TracePriority.Lowest, debugCallback,
                                    traceCallback, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    ForDisplay(arg), ForDisplay(text)),
                                    traceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.noVs2008 = (bool)value;
                        }
                        else if (MatchOption(newArg, "noVs2010"))
                        {
                            bool? value = ParseBoolean(text);

                            if (value == null)
                            {
                                error = TraceOps.DebugAndTrace(
                                    TracePriority.Lowest, debugCallback,
                                    traceCallback, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    ForDisplay(arg), ForDisplay(text)),
                                    traceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.noVs2010 = (bool)value;
                        }
                        else if (MatchOption(newArg, "noVs2012"))
                        {
                            bool? value = ParseBoolean(text);

                            if (value == null)
                            {
                                error = TraceOps.DebugAndTrace(
                                    TracePriority.Lowest, debugCallback,
                                    traceCallback, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    ForDisplay(arg), ForDisplay(text)),
                                    traceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.noVs2012 = (bool)value;
                        }
                        else if (MatchOption(newArg, "noVs2013"))
                        {
                            bool? value = ParseBoolean(text);

                            if (value == null)
                            {
                                error = TraceOps.DebugAndTrace(
                                    TracePriority.Lowest, debugCallback,
                                    traceCallback, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    ForDisplay(arg), ForDisplay(text)),
                                    traceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.noVs2013 = (bool)value;
                        }
                        else if (MatchOption(newArg, "perUser"))
                        {
                            bool? value = ParseBoolean(text);

                            if (value == null)
                            {
                                error = TraceOps.DebugAndTrace(
                                    TracePriority.Lowest, debugCallback,
                                    traceCallback, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    ForDisplay(arg), ForDisplay(text)),
                                    traceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.perUser = (bool)value;
                        }
                        else if (MatchOption(newArg, "registryVersion"))
                        {
                            configuration.registryVersion = text;
                        }
                        else if (MatchOption(newArg, "strict"))
                        {
                            bool? value = ParseBoolean(text);

                            if (value == null)
                            {
                                error = TraceOps.DebugAndTrace(
                                    TracePriority.Lowest, debugCallback,
                                    traceCallback, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    ForDisplay(arg), ForDisplay(text)),
                                    traceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            //
                            // NOTE: Allow the command line arguments to
                            //       override the "strictness" setting
                            //       provided by our caller.
                            //
                            strict = (bool)value;
                        }
                        else if (MatchOption(newArg, "throwOnMissing"))
                        {
                            bool? value = ParseBoolean(text);

                            if (value == null)
                            {
                                error = TraceOps.DebugAndTrace(
                                    TracePriority.Lowest, debugCallback,
                                    traceCallback, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    ForDisplay(arg), ForDisplay(text)),
                                    traceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.throwOnMissing = (bool)value;
                        }
                        else if (MatchOption(newArg, "traceFormat"))
                        {
                            configuration.traceFormat = text;
                            TraceOps.TraceFormat = configuration.traceFormat;
                        }
                        else if (MatchOption(newArg, "tracePriority"))
                        {
                            object value = ParseEnum(
                                typeof(TracePriority), text, true);

                            if (value == null)
                            {
                                error = TraceOps.DebugAndTrace(
                                    TracePriority.Lowest, debugCallback,
                                    traceCallback, String.Format(
                                    "Invalid {0} value: {1}",
                                    ForDisplay(arg), ForDisplay(text)),
                                    traceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.tracePriority = (TracePriority)value;
                            TraceOps.TracePriority = configuration.tracePriority;
                        }
                        else if (MatchOption(newArg, "verbose"))
                        {
                            bool? value = ParseBoolean(text);

                            if (value == null)
                            {
                                error = TraceOps.DebugAndTrace(
                                    TracePriority.Lowest, debugCallback,
                                    traceCallback, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    ForDisplay(arg), ForDisplay(text)),
                                    traceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.verbose = (bool)value;
                        }
                        else if (MatchOption(newArg, "vsVersionSuffix"))
                        {
                            configuration.vsVersionSuffix = text;
                        }
                        else if (MatchOption(newArg, "whatIf"))
                        {
                            bool? value = ParseBoolean(text);

                            if (value == null)
                            {
                                error = TraceOps.DebugAndTrace(
                                    TracePriority.Lowest, debugCallback,
                                    traceCallback, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    ForDisplay(arg), ForDisplay(text)),
                                    traceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.whatIf = (bool)value;
                        }
                        else if (MatchOption(newArg, "wow64"))
                        {
                            bool? value = ParseBoolean(text);

                            if (value == null)
                            {
                                error = TraceOps.DebugAndTrace(
                                    TracePriority.Lowest, debugCallback,
                                    traceCallback, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    ForDisplay(arg), ForDisplay(text)),
                                    traceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.wow64 = (bool)value;
                        }
                        else
                        {
                            error = TraceOps.DebugAndTrace(
                                TracePriority.Lowest, debugCallback,
                                traceCallback, String.Format(
                                "Unsupported command line option: {0}",
                                ForDisplay(arg)), traceCategory);

                            if (strict)
                                return false;
                        }
                    }

                    return true;
                }
                catch (Exception e)
                {
                    TraceOps.DebugAndTrace(TracePriority.Highest,
                        debugCallback, traceCallback, e, traceCategory);

                    error = "Failed to modify configuration.";
                }

                return false;
            }

            ///////////////////////////////////////////////////////////////////

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static bool Process(
                string[] args,
                Configuration configuration,
                bool strict,
                ref string error
                )
            {
                try
                {
                    if (configuration == null)
                    {
                        error = "Invalid configuration.";
                        return false;
                    }

                    Assembly assembly = configuration.assembly;

                    if (assembly == null)
                    {
                        error = "Invalid assembly.";
                        return false;
                    }

                    if (!configuration.noTrace)
                    {
                        if (!configuration.noLog &&
                            String.IsNullOrEmpty(configuration.logFileName))
                        {
                            //
                            // NOTE: Use the default log file name.
                            //
                            configuration.logFileName = GetLogFileName(
                                "trace");
                        }

                        ///////////////////////////////////////////////////////

                        if (!configuration.noConsole)
                        {
                            //
                            // NOTE: In verbose mode, debug output (that meets
                            //       the configured priority criteria) will be
                            //       displayed to the console; otherwise, trace
                            //       output (that meets the configured priority
                            //       criteria) will be displayed to the console.
                            //
                            if (configuration.debug)
                            {
                                //
                                // NOTE: Add the console trace listener to the
                                //       list of trace listeners maintained by
                                //       the TraceOps class (i.e. only messages
                                //       that meet the debug priority will be
                                //       seen on the console).
                                //
                                TraceOps.SetupDebugListeners();
                            }
                            else
                            {
                                //
                                // NOTE: Add the console trace listener to the
                                //       list of built-in trace listeners (i.e.
                                //       only messages that meet the trace
                                //       priority will be seen on the console).
                                //
                                Trace.Listeners.Add(new ConsoleTraceListener());
                            }
                        }

                        ///////////////////////////////////////////////////////

                        if (!configuration.noLog &&
                            !String.IsNullOrEmpty(configuration.logFileName))
                        {
                            Trace.Listeners.Add(new TextWriterTraceListener(
                                configuration.logFileName));

                            //
                            // NOTE: Technically, we created the log file.
                            //
                            filesCreated++;
                        }
                    }

                    //
                    // NOTE: Dump the configuration now in case we need to
                    //       troubleshoot any issues.
                    //
                    if (configuration.debugPriority <= TracePriority.Medium)
                        configuration.Dump(debugCallback);

                    if (configuration.tracePriority <= TracePriority.Medium)
                        configuration.Dump(traceCallback);

                    //
                    // NOTE: Show where we are running from and how we were
                    //       invoked.
                    //
                    string location = assembly.Location;

                    TraceOps.DebugAndTrace(TracePriority.MediumLow,
                        debugCallback, traceCallback, String.Format(
                        "Running executable is: {0}", ForDisplay(location)),
                        traceCategory);

                    TraceOps.DebugAndTrace(TracePriority.MediumLow,
                        debugCallback, traceCallback, String.Format(
                        "Original command line is: {0}",
                        Environment.CommandLine), traceCategory);

                    TraceOps.DebugAndTrace(TracePriority.MediumLow,
                        debugCallback, traceCallback, String.Format(
                        "Running process is {0}.", Is64BitProcess() ?
                            "64-bit" : "32-bit"), traceCategory);

                    if (!configuration.whatIf)
                    {
                        //
                        // NOTE: If the debugger is attached and "what-if"
                        //       mode is [now] disabled, issue a warning.
                        //
                        if (Debugger.IsAttached)
                            TraceOps.DebugAndTrace(TracePriority.MediumHigh,
                                debugCallback, traceCallback,
                                "Forced to disable \"what-if\" mode with " +
                                "debugger attached.", traceCategory);
                    }
                    else
                    {
                        TraceOps.DebugAndTrace(TracePriority.MediumHigh,
                            debugCallback, traceCallback,
                            "No actual changes will be made to this " +
                            "system because \"what-if\" mode is enabled.",
                            traceCategory);
                    }

                    //
                    // NOTE: If the command line has not been manually
                    //       confirmed (i.e. via the explicit command line
                    //       option), then stop processing now.  We enforce
                    //       this rule so that simply double-clicking the
                    //       executable will not result in any changes being
                    //       made to the system.
                    //
                    if (!configuration.confirm)
                    {
                        error = "Cannot continue, the \"confirm\" option is " +
                            "not enabled.";

                        return false;
                    }

                    return true;
                }
                catch (Exception e)
                {
                    TraceOps.DebugAndTrace(TracePriority.Highest,
                        debugCallback, traceCallback, e, traceCategory);

                    error = "Failed to process configuration.";
                }

                return false;
            }

            ///////////////////////////////////////////////////////////////////

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static bool CheckRuntimeVersion(
                Configuration configuration,
                bool strict,
                ref string error
                )
            {
                try
                {
                    if (configuration == null)
                    {
                        error = "Invalid configuration.";
                        return false;
                    }

                    //
                    // NOTE: What version of the runtime was the core (primary)
                    //       assembly compiled against (e.g. "v2.0.50727" or
                    //       "v4.0.30319").
                    //
                    string coreImageRuntimeVersion = GetImageRuntimeVersion(
                        configuration.coreFileName);

                    //
                    // NOTE: We allow the actual image runtime checking to be
                    //       bypassed via the "-noRuntimeVersion" command line
                    //       option.  The command line option is intended for
                    //       expert use only.
                    //
                    if (configuration.noRuntimeVersion)
                    {
                        TraceOps.DebugAndTrace(TracePriority.Medium,
                            debugCallback, traceCallback, String.Format(
                            "Assembly is compiled for the .NET Framework {0}; " +
                            "however, installation restrictions based on this " +
                            "fact have been disabled via the command line.",
                            coreImageRuntimeVersion), traceCategory);

                        return true;
                    }

                    //
                    // TODO: Restrict the configuration based on which image
                    //       runtime versions (which more-or-less correspond
                    //       to .NET Framework versions) are supported by the
                    //       versions of Visual Studio that are installed.
                    //
                    if (String.IsNullOrEmpty(coreImageRuntimeVersion))
                    {
                        error = "invalid core file image runtime version";
                        return false;
                    }
                    else if (String.Equals(
                            coreImageRuntimeVersion, CLRv2ImageRuntimeVersion,
                            StringComparison.Ordinal))
                    {
                        //
                        // NOTE: For the CLR v2.0 runtime, make sure we disable
                        //       any attempt to use it for things that require
                        //       an assembly compiled for the CLR v4.0.  It is
                        //       uncertain if this is actually a problem in
                        //       practice as the CLR v4.0 can load and use an
                        //       assembly compiled with the CLR v2.0; however,
                        //       since this project offers both configurations,
                        //       we currently disallow this mismatch.
                        //
                        configuration.noNetFx40 = true;
                        configuration.noNetFx45 = true;
                        configuration.noNetFx451 = true;
                        configuration.noVs2010 = true;
                        configuration.noVs2012 = true;
                        configuration.noVs2013 = true;

                        TraceOps.DebugAndTrace(TracePriority.Medium,
                            debugCallback, traceCallback, String.Format(
                            "Assembly is compiled for the .NET Framework {0}, " +
                            "support for the .NET Framework {1} is now disabled.",
                            CLRv2ImageRuntimeVersion, CLRv4ImageRuntimeVersion),
                            traceCategory);
                    }
                    else if (String.Equals(
                            coreImageRuntimeVersion, CLRv4ImageRuntimeVersion,
                            StringComparison.Ordinal))
                    {
                        //
                        // NOTE: For the CLR v4.0 runtime, make sure we disable
                        //       any attempt to use it for things that require
                        //       an assembly compiled for the CLR v2.0.
                        //
                        configuration.noNetFx20 = true;
                        configuration.noNetFx35 = true;
                        configuration.noVs2005 = true;
                        configuration.noVs2008 = true;

                        TraceOps.DebugAndTrace(TracePriority.Medium,
                            debugCallback, traceCallback, String.Format(
                            "Assembly is compiled for the .NET Framework {0}, " +
                            "support for the .NET Framework {1} is now disabled.",
                            ForDisplay(CLRv4ImageRuntimeVersion),
                            ForDisplay(CLRv2ImageRuntimeVersion)),
                            traceCategory);
                    }
                    else
                    {
                        error = String.Format(
                            "unsupported core file image runtime version " +
                            "{0}, must be {1} or {2}",
                            ForDisplay(coreImageRuntimeVersion),
                            ForDisplay(CLRv2ImageRuntimeVersion),
                            ForDisplay(CLRv4ImageRuntimeVersion));

                        return false;
                    }

                    return true;
                }
                catch (Exception e)
                {
                    TraceOps.DebugAndTrace(TracePriority.Highest,
                        debugCallback, traceCallback, e, traceCategory);

                    error = "Failed to check image runtime version.";
                }

                return false;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Methods
            public bool HasFlags(
                InstallFlags hasFlags,
                bool all
                )
            {
                if (all)
                    return ((installFlags & hasFlags) == hasFlags);
                else
                    return ((installFlags & hasFlags) != InstallFlags.None);
            }

            ///////////////////////////////////////////////////////////////////

            public bool IsLinqSupported()
            {
                //
                // NOTE: Return non-zero if the System.Data.SQLite.Linq
                //       assembly should be processed during the install.
                //       If the target is Visual Studio 2005, this must
                //       return zero.
                //
                return !noNetFx35 || !noNetFx40 || !noNetFx45 || !noNetFx451;
            }

            ///////////////////////////////////////////////////////////////////

            public bool IsEf6Supported()
            {
                //
                // NOTE: Return non-zero if the System.Data.SQLite.EF6
                //       assembly should be processed during the install.
                //       If the target is Visual Studio 2005 or Visual
                //       Studio 2008, this must return zero.  Also, if
                //       the EF6 core assembly is unavailable, this must
                //       return zero.
                //
                if (noNetFx40 && noNetFx45 && noNetFx451)
                    return false;

                return IsEf6AssemblyAvailable();
            }

            ///////////////////////////////////////////////////////////////////

            public void Dump(
                TraceCallback traceCallback
                )
            {
                if (traceCallback != null)
                {
                    traceCallback(String.Format(NameAndValueFormat,
                        "Assembly", ForDisplay(assembly)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "LogFileName", ForDisplay(logFileName)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "Directory", ForDisplay(directory)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "CoreFileName", ForDisplay(coreFileName)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "LinqFileName", ForDisplay(linqFileName)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "Ef6FileName", ForDisplay(ef6FileName)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "DesignerFileName", ForDisplay(designerFileName)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "RegistryVersion", ForDisplay(registryVersion)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "ConfigVersion", ForDisplay(configVersion)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "VsVersionSuffix", ForDisplay(vsVersionSuffix)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "DebugFormat", ForDisplay(debugFormat)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "TraceFormat", ForDisplay(traceFormat)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "InstallFlags", ForDisplay(installFlags)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "DebugPriority", ForDisplay(debugPriority)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "TracePriority", ForDisplay(tracePriority)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "PerUser", ForDisplay(perUser)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "Install", ForDisplay(install)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "Wow64", ForDisplay(wow64)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "NoRuntimeVersion", ForDisplay(noRuntimeVersion)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "NoDesktop", ForDisplay(noDesktop)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "NoCompact", ForDisplay(noCompact)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "NoNetFx20", ForDisplay(noNetFx20)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "NoNetFx35", ForDisplay(noNetFx35)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "NoNetFx40", ForDisplay(noNetFx40)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "NoNetFx45", ForDisplay(noNetFx45)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "NoNetFx451", ForDisplay(noNetFx451)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "NoVs2005", ForDisplay(noVs2005)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "NoVs2008", ForDisplay(noVs2008)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "NoVs2010", ForDisplay(noVs2010)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "NoVs2012", ForDisplay(noVs2012)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "NoVs2013", ForDisplay(noVs2013)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "NoTrace", ForDisplay(noTrace)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "NoConsole", ForDisplay(noConsole)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "NoLog", ForDisplay(noLog)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "ThrowOnMissing", ForDisplay(throwOnMissing)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "WhatIf", ForDisplay(whatIf)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "Debug", ForDisplay(debug)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "Verbose", ForDisplay(verbose)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "Confirm", ForDisplay(confirm)),
                        traceCategory);

                    ///////////////////////////////////////////////////////////

                    if (assembly != null)
                    {
                        traceCallback(String.Format(NameAndValueFormat,
                            "AssemblyTitle",
                            ForDisplay(GetAssemblyTitle(assembly))),
                            traceCategory);

                        traceCallback(String.Format(NameAndValueFormat,
                            "AssemblyConfiguration",
                            ForDisplay(GetAssemblyConfiguration(assembly))),
                            traceCategory);
                    }
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Properties
            private Assembly assembly;
            public Assembly Assembly
            {
                get { return assembly; }
                set { assembly = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private string logFileName;
            public string LogFileName
            {
                get { return logFileName; }
                set { logFileName = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private string directory;
            public string Directory
            {
                get { return directory; }
                set { directory = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private string coreFileName;
            public string CoreFileName
            {
                get { return coreFileName; }
                set { coreFileName = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private string linqFileName;
            public string LinqFileName
            {
                get { return linqFileName; }
                set { linqFileName = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private string ef6FileName;
            public string Ef6FileName
            {
                get { return ef6FileName; }
                set { ef6FileName = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private string designerFileName;
            public string DesignerFileName
            {
                get { return designerFileName; }
                set { designerFileName = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private string registryVersion;
            public string RegistryVersion
            {
                get { return registryVersion; }
                set { registryVersion = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private string configVersion;
            public string ConfigVersion
            {
                get { return configVersion; }
                set { configVersion = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private string vsVersionSuffix;
            public string VsVersionSuffix
            {
                get { return vsVersionSuffix; }
                set { vsVersionSuffix = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private string debugFormat;
            public string DebugFormat
            {
                get { return debugFormat; }
                set { debugFormat = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private string traceFormat;
            public string TraceFormat
            {
                get { return traceFormat; }
                set { traceFormat = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private InstallFlags installFlags;
            public InstallFlags InstallFlags
            {
                get { return installFlags; }
                set { installFlags = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private TracePriority debugPriority;
            public TracePriority DebugPriority
            {
                get { return debugPriority; }
                set { debugPriority = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private TracePriority tracePriority;
            public TracePriority TracePriority
            {
                get { return tracePriority; }
                set { tracePriority = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool perUser;
            public bool PerUser
            {
                get { return perUser; }
                set { perUser = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool install;
            public bool Install
            {
                get { return install; }
                set { install = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool wow64;
            public bool Wow64
            {
                get { return wow64; }
                set { wow64 = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool noRuntimeVersion;
            public bool NoRuntimeVersion
            {
                get { return noRuntimeVersion; }
                set { noRuntimeVersion = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool noDesktop;
            public bool NoDesktop
            {
                get { return noDesktop; }
                set { noDesktop = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool noCompact;
            public bool NoCompact
            {
                get { return noCompact; }
                set { noCompact = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool noNetFx20;
            public bool NoNetFx20
            {
                get { return noNetFx20; }
                set { noNetFx20 = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool noNetFx35;
            public bool NoNetFx35
            {
                get { return noNetFx35; }
                set { noNetFx35 = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool noNetFx40;
            public bool NoNetFx40
            {
                get { return noNetFx40; }
                set { noNetFx40 = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool noNetFx45;
            public bool NoNetFx45
            {
                get { return noNetFx45; }
                set { noNetFx45 = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool noNetFx451;
            public bool NoNetFx451
            {
                get { return noNetFx451; }
                set { noNetFx451 = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool noVs2005;
            public bool NoVs2005
            {
                get { return noVs2005; }
                set { noVs2005 = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool noVs2008;
            public bool NoVs2008
            {
                get { return noVs2008; }
                set { noVs2008 = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool noVs2010;
            public bool NoVs2010
            {
                get { return noVs2010; }
                set { noVs2010 = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool noVs2012;
            public bool NoVs2012
            {
                get { return noVs2012; }
                set { noVs2012 = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool noVs2013;
            public bool NoVs2013
            {
                get { return noVs2013; }
                set { noVs2013 = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool noTrace;
            public bool NoTrace
            {
                get { return noTrace; }
                set { noTrace = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool noConsole;
            public bool NoConsole
            {
                get { return noConsole; }
                set { noConsole = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool noLog;
            public bool NoLog
            {
                get { return noLog; }
                set { noLog = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool throwOnMissing;
            public bool ThrowOnMissing
            {
                get { return throwOnMissing; }
                set { throwOnMissing = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool whatIf;
            public bool WhatIf
            {
                get { return whatIf; }
                set { whatIf = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool debug;
            public bool Debug
            {
                get { return debug; }
                set { debug = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool verbose;
            public bool Verbose
            {
                get { return verbose; }
                set { verbose = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool confirm;
            public bool Confirm
            {
                get { return confirm; }
                set { confirm = value; }
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region FrameworkList Class
        private sealed class FrameworkList
        {
            #region Public Constructors
            public FrameworkList()
            {
                // do nothing.
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Methods
            private MockRegistryKey rootKey;
            public MockRegistryKey RootKey
            {
                get { return rootKey; }
                set { rootKey = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private StringList names;
            public StringList Names
            {
                get { return names; }
                set { names = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private VersionMap versions;
            public VersionMap Versions
            {
                get { return versions; }
                set { versions = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private StringList platformNames;
            public StringList PlatformNames
            {
                get { return platformNames; }
                set { platformNames = value; }
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region VsList Class
        private sealed class VsList
        {
            #region Public Constructors
            public VsList()
            {
                // do nothing.
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Properties
            private MockRegistryKey rootKey;
            public MockRegistryKey RootKey
            {
                get { return rootKey; }
                set { rootKey = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private VersionList versions;
            public VersionList Versions
            {
                get { return versions; }
                set { versions = value; }
            }
            #endregion
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constant Data
        private const string CoreFileName = "System.Data.SQLite.dll";
        private const string LinqFileName = "System.Data.SQLite.Linq.dll";
        private const string Ef6FileName = "System.Data.SQLite.EF6.dll";
        private const string DesignerFileName = "SQLite.Designer.dll";
        private const string ProviderName = "SQLite Data Provider";
        private const string ProjectName = "System.Data.SQLite";
        private const string LegacyProjectName = "SQLite";
        private const string InvariantName = "System.Data.SQLite";
        private const string FactoryTypeName = "System.Data.SQLite.SQLiteFactory";
        private const string Description = ".NET Framework Data Provider for SQLite";

        ///////////////////////////////////////////////////////////////////////

        private const string CLRv2ImageRuntimeVersion = "v2.0.50727";
        private const string CLRv4ImageRuntimeVersion = "v4.0.30319";

        ///////////////////////////////////////////////////////////////////////

        private const string Ef6AssemblyName = "EntityFramework, " +
            "Version=6.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";

        ///////////////////////////////////////////////////////////////////////

        private const string NameAndValueFormat = "{0}: {1}";
        private const string LogFileSuffix = ".log";

        ///////////////////////////////////////////////////////////////////////

        private const string VsDevEnvSetupFormat = "{0}: {1}";

        ///////////////////////////////////////////////////////////////////////

        private const string RootKeyName = "Software";
        private const string Wow64SubKeyName = "Wow6432Node";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The .NET Framework has both 32-bit and 64-bit editions.
        //
        private static readonly bool NetFxIs32BitOnly = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: For now, Visual Studio is always a 32-bit application.
        //
        private static readonly bool VsIs32BitOnly = true;

        ///////////////////////////////////////////////////////////////////////

        private static readonly string VsIdFormat = "B";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string XPathForAddElement =
            "configuration/system.data/DbProviderFactories/add[@invariant=\"{0}\"]";

        private static readonly string XPathForRemoveElement =
            "configuration/system.data/DbProviderFactories/remove[@invariant=\"{0}\"]";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Data
        private static Assembly thisAssembly = Assembly.GetExecutingAssembly();

        private static string traceCategory = Path.GetFileName(
            thisAssembly.Location); /* NOTE: Same for debug and trace. */

        private static TraceCallback debugCallback = AppDebug;
        private static TraceCallback traceCallback = AppTrace;

        ///////////////////////////////////////////////////////////////////////

        private static string systemDirectory = null;

#if WINDOWS
        private static string systemDirectoryWow64 = null;
#endif

        ///////////////////////////////////////////////////////////////////////

        private static int filesCreated = 0;
        private static int filesModified = 0;
        private static int filesDeleted = 0;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trace Handling
        private static string GetLogFileName(
            string typeName
            ) /* throw */
        {
            string fileName = Path.GetTempFileName();
            string directory = Path.GetDirectoryName(fileName);
            string fileNameOnly = Path.GetFileNameWithoutExtension(fileName);

            string newFileName = Path.Combine(directory, String.Format(
                "{0}{1}{2}", traceCategory, !String.IsNullOrEmpty(typeName) ?
                    "." + typeName : String.Empty, "." + fileNameOnly +
                    LogFileSuffix));

            File.Move(fileName, newFileName);

            return newFileName;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void AppDebug(
            string message,
            string category
            )
        {
            TraceOps.DebugCore(String.Format(
                TraceOps.DebugFormat, TraceOps.NextDebugId(),
                TraceOps.TimeStamp(DateTime.UtcNow), message), category);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void AppTrace(
            string message,
            string category
            )
        {
            TraceOps.TraceCore(String.Format(
                TraceOps.TraceFormat, TraceOps.NextTraceId(),
                TraceOps.TimeStamp(DateTime.UtcNow), message), category);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Generic Platform Handling
        private static bool Is64BitProcess()
        {
            //
            // NOTE: Returns true if the current process is 64-bit.  If this
            //       is true, we *know* that we must be running on a 64-bit
            //       operating system as well.  However, if this is false, we
            //       do not necessarily know that we are running on a 32-bit
            //       operating system, due to WoW64 (Win32-on-Win64), etc.
            //
            return (IntPtr.Size == sizeof(long)); // NOTE: Pointer is 64-bits?
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsSupportedRootKey(
            MockRegistry registry,
            MockRegistryKey rootKey
            )
        {
            return Object.ReferenceEquals(rootKey, registry.CurrentUser) ||
                Object.ReferenceEquals(rootKey, registry.LocalMachine);
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetRootKeyName(
            bool perUser,
            bool wow64
            )
        {
            //
            // BUGFIX: Apparently, the per-user registry hive does not use
            //         the "Wow6432Node" node to store settings for 32-bit
            //         applications running on a 64-bit operating system.
            //         Ticket [a0677309f0] has further details.
            //
            return String.Format("{0}{1}", RootKeyName,
                !perUser && wow64 && Is64BitProcess() ?
                    "\\" + Wow64SubKeyName : String.Empty);
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetSystemDirectory(
            bool wow64
            )
        {
#if WINDOWS
            if (wow64)
            {
                if (systemDirectoryWow64 == null)
                {
                    systemDirectoryWow64 =
                        UnsafeNativeMethods.GetSystemDirectory();
                }

                return systemDirectoryWow64;
            }
            else
#endif
            {
                if (systemDirectory == null)
                    systemDirectory = Environment.SystemDirectory;

                return systemDirectory;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Generic String Handling
        private static string ForDisplay(
            object value
            )
        {
            if (value == null)
                return "<null>";

            string result;
            Type type = value.GetType();

            if (type == typeof(XmlElement))
            {
                XmlElement element = (XmlElement)value;

                result = element.OuterXml;
            }
            else if (type == typeof(Version))
            {
                Version version = (Version)value;

                result = String.Format("v{0}", version);
            }
            else if (type == typeof(ProcessStartInfo))
            {
                ProcessStartInfo startInfo = (ProcessStartInfo)value;

                result = String.Format(
                    "fileName = {0}, arguments = {1}, workingDirectory = {2}, " +
                    "useShellExecute = {3}, redirectStandardOutput = {4}, " +
                    "redirectStandardError = {5}", ForDisplay(
                    startInfo.FileName), ForDisplay(startInfo.Arguments),
                    ForDisplay(startInfo.WorkingDirectory), ForDisplay(
                    startInfo.UseShellExecute), ForDisplay(
                    startInfo.RedirectStandardOutput), ForDisplay(
                    startInfo.RedirectStandardError)); /* RECURSIVE */
            }
            else if (type == typeof(Process))
            {
                Process process = (Process)value;

                result = process.Id.ToString();
            }
            else if (type == typeof(DataReceivedEventArgs))
            {
                DataReceivedEventArgs eventArgs = (DataReceivedEventArgs)value;

                result = ForDisplay(eventArgs.Data); /* RECURSIVE */
            }
            else
            {
                result = value.ToString();

                if (result.Length == 0)
                    return "<empty>";

                result = String.Format(
                    type.IsSubclassOf(typeof(ValueType)) ? "{0}" : "\"{0}\"",
                    result);
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Assembly Attribute Handling
        public static string GetAssemblyConfiguration(
            Assembly assembly
            )
        {
            if (assembly != null)
            {
                try
                {
                    if (assembly.IsDefined(
                            typeof(AssemblyConfigurationAttribute), false))
                    {
                        AssemblyConfigurationAttribute configuration =
                            (AssemblyConfigurationAttribute)
                            assembly.GetCustomAttributes(
                                typeof(AssemblyConfigurationAttribute),
                                false)[0];

                        return configuration.Configuration;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblyTitle(
            Assembly assembly
            )
        {
            if (assembly != null)
            {
                try
                {
                    if (assembly.IsDefined(
                            typeof(AssemblyTitleAttribute), false))
                    {
                        AssemblyTitleAttribute title =
                            (AssemblyTitleAttribute)
                            assembly.GetCustomAttributes(
                                typeof(AssemblyTitleAttribute), false)[0];

                        return title.Title;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region .NET Framework Handling
        private static string GetFrameworkRootKeyName(
            bool perUser,
            bool wow64
            )
        {
            return String.Format("{0}\\Microsoft\\.NETFramework",
                GetRootKeyName(perUser, wow64));
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetFrameworkKeyName(
            string frameworkName,
            Version frameworkVersion,
            string platformName,
            bool perUser,
            bool wow64
            )
        {
            string format = !String.IsNullOrEmpty(platformName) ?
                "{0}\\Microsoft\\{1}\\v{2}\\{3}" :
                "{0}\\Microsoft\\{1}\\v{2}";

            return String.Format(format, GetRootKeyName(perUser, wow64),
                frameworkName, frameworkVersion, platformName);
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetImageRuntimeVersion(
            string fileName
            )
        {
            try
            {
                Assembly assembly =
                    Assembly.ReflectionOnlyLoadFrom(fileName); /* throw */

                if (assembly != null)
                    return assembly.ImageRuntimeVersion;
            }
            catch
            {
                // do nothing.
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetFrameworkDirectory(
            MockRegistryKey rootKey,
            Version frameworkVersion,
            bool perUser,
            bool wow64,
            bool whatIf,
            bool verbose
            )
        {
            using (MockRegistryKey key = RegistryHelper.OpenSubKey(
                    rootKey, GetFrameworkRootKeyName(perUser, wow64),
                    false, whatIf, verbose))
            {
                if (key == null)
                    return null;

                object value = RegistryHelper.GetValue(
                    key, "InstallRoot", null, whatIf, verbose);

                if (!(value is string))
                    return null;

                return Path.Combine(
                    (string)value, String.Format("v{0}", frameworkVersion));
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Per-Framework/Platform Handling
        private static void InitializeFrameworkList(
            MockRegistryKey rootKey,
            Configuration configuration,
            ref FrameworkList frameworkList
            )
        {
            if (frameworkList == null)
                frameworkList = new FrameworkList();

            if (frameworkList.RootKey == null)
                frameworkList.RootKey = rootKey;

            ///////////////////////////////////////////////////////////////////

            if (frameworkList.Names == null)
            {
                frameworkList.Names = new StringList();

                if ((configuration == null) || !configuration.NoDesktop)
                    frameworkList.Names.Add(".NETFramework");

                if ((configuration == null) || !configuration.NoCompact)
                {
                    frameworkList.Names.Add(".NETCompactFramework");
                    frameworkList.Names.Add(".NETCompactFramework");
                    frameworkList.Names.Add(".NETCompactFramework");
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (frameworkList.Versions == null)
            {
                frameworkList.Versions = new VersionMap();

                if ((configuration == null) || !configuration.NoDesktop)
                {
                    VersionList desktopVersionList = new VersionList();

                    if ((configuration == null) || !configuration.NoNetFx20)
                        desktopVersionList.Add(new Version(2, 0, 50727));

                    //
                    // NOTE: The .NET Framework 3.5 does not have its own
                    //       directory; however, it still may have assembly
                    //       folders for use in Visual Studio, etc.
                    //
                    if ((configuration == null) || !configuration.NoNetFx35)
                        desktopVersionList.Add(new Version(3, 5));

                    if ((configuration == null) || !configuration.NoNetFx40)
                        desktopVersionList.Add(new Version(4, 0, 30319));

                    //
                    // NOTE: The .NET Framework 4.5 does not have its own
                    //       directory; however, it still may have assembly
                    //       folders for use in Visual Studio, etc.
                    //
                    if ((configuration == null) || !configuration.NoNetFx45)
                        desktopVersionList.Add(new Version(4, 5, 50709));

                    //
                    // NOTE: The .NET Framework 4.5.1 does not have its own
                    //       directory; however, it still may have assembly
                    //       folders for use in Visual Studio, etc.
                    //
                    if ((configuration == null) || !configuration.NoNetFx451)
                        desktopVersionList.Add(new Version(4, 5, 1));

                    frameworkList.Versions.Add(".NETFramework",
                        desktopVersionList);
                }

                if ((configuration == null) || !configuration.NoCompact)
                {
                    frameworkList.Versions.Add(".NETCompactFramework",
                        new VersionList(new Version[] {
                        new Version(2, 0, 0, 0), new Version(3, 5, 0, 0)
                    }));
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (frameworkList.PlatformNames == null)
            {
                frameworkList.PlatformNames = new StringList();

                if ((configuration == null) || !configuration.NoDesktop)
                    frameworkList.PlatformNames.Add(null);

                if ((configuration == null) || !configuration.NoCompact)
                {
                    frameworkList.PlatformNames.Add("PocketPC");
                    frameworkList.PlatformNames.Add("Smartphone");
                    frameworkList.PlatformNames.Add("WindowsCE");
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool HaveFrameworkDirectory(
            MockRegistryKey rootKey,
            string frameworkName,
            Version frameworkVersion,
            string platformName,
            bool perUser,
            bool wow64,
            bool whatIf,
            bool verbose,
            ref string installDirectory
            )
        {
            string keyName = GetFrameworkKeyName(
                frameworkName, frameworkVersion, platformName, perUser,
                wow64);

            using (MockRegistryKey key = RegistryHelper.OpenSubKey(
                    rootKey, keyName, false, whatIf, verbose))
            {
                if (key == null)
                    return false;

                if (platformName != null) // NOTE: Skip non-desktop.
                    return true;

                string directory = GetFrameworkDirectory(
                    rootKey, frameworkVersion, perUser, wow64, whatIf,
                    verbose);

                if (String.IsNullOrEmpty(directory))
                    return false;

                if (!Directory.Exists(directory))
                    return false;

                TraceOps.DebugAndTrace(TracePriority.Lower,
                    debugCallback, traceCallback, String.Format(
                    ".NET Framework {0} found via directory {1}.",
                    ForDisplay(frameworkVersion), ForDisplay(directory)),
                    traceCategory);

                installDirectory = directory;
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool HaveFrameworkRegistry(
            MockRegistryKey rootKey,
            string frameworkName,
            Version frameworkVersion,
            string platformName,
            bool perUser,
            bool wow64,
            bool whatIf,
            bool verbose
            )
        {
            string keyName = GetFrameworkKeyName(
                frameworkName, frameworkVersion, platformName, perUser,
                wow64);

            using (MockRegistryKey key = RegistryHelper.OpenSubKey(
                    rootKey, keyName, false, whatIf, verbose))
            {
                if (key == null)
                    return false;

                if (platformName != null) // NOTE: Skip non-desktop.
                    return true;

                TraceOps.DebugAndTrace(TracePriority.Lower,
                    debugCallback, traceCallback, String.Format(
                    ".NET Framework {0} found via registry {1}.",
                    ForDisplay(frameworkVersion), ForDisplay(keyName)),
                    traceCategory);

                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool ForEachFrameworkConfig(
            MockRegistry registry,
            FrameworkList frameworkList,
            FrameworkConfigCallback callback,
            string version, /* NOTE: Optional. */
            string invariant,
            string name,
            string description,
            string typeName,
            AssemblyName assemblyName,
            object clientData,
            bool perUser,
            bool wow64,
            bool throwOnMissing,
            bool whatIf,
            bool verbose,
            ref bool saved,
            ref string error
            )
        {
            if (registry == null)
            {
                error = "invalid registry";
                return false;
            }

            if (frameworkList == null)
            {
                error = "invalid framework list";
                return false;
            }

            MockRegistryKey rootKey = frameworkList.RootKey;

            if (rootKey == null)
            {
                error = "invalid root key";
                return false;
            }

            if (!IsSupportedRootKey(registry, rootKey))
            {
                error = "root key must be per-user or per-machine";
                return false;
            }

            if (frameworkList.Names == null)
            {
                error = "no framework names found";
                return false;
            }

            if (frameworkList.Versions == null)
            {
                error = "no framework versions found";
                return false;
            }

            if (frameworkList.PlatformNames == null)
            {
                error = "no platform names found";
                return false;
            }

            if (frameworkList.Names.Count != frameworkList.PlatformNames.Count)
            {
                error = String.Format("framework name count {0} does not " +
                    "match platform name count {1}", frameworkList.Names.Count,
                    frameworkList.PlatformNames.Count);

                return false;
            }

            for (int index = 0; index < frameworkList.Names.Count; index++)
            {
                //
                // NOTE: Grab the name of the framework (e.g. ".NETFramework")
                //       and the name of the platform (e.g. "WindowsCE").
                //
                string frameworkName = frameworkList.Names[index];
                string platformName = frameworkList.PlatformNames[index];

                //
                // NOTE: Skip all non-desktop frameworks (i.e. if the platform
                //       name is not null).
                //
                if (platformName != null)
                    continue;

                //
                // NOTE: Grab the supported versions of this particular
                //       framework.
                //
                VersionList frameworkVersionList;

                if (version != null)
                {
                    //
                    // NOTE: Manual override of the *ONE* framework version
                    //       to process.
                    //
                    frameworkVersionList = new VersionList();
                    frameworkVersionList.Add(new Version(version));
                }
                else
                {
                    if (!frameworkList.Versions.TryGetValue(
                            frameworkName, out frameworkVersionList) ||
                        (frameworkVersionList == null))
                    {
                        continue;
                    }
                }

                foreach (Version frameworkVersion in frameworkVersionList)
                {
                    TraceOps.DebugAndTrace(TracePriority.Lower,
                        debugCallback, traceCallback, String.Format(
                        "frameworkName = {0}, frameworkVersion = {1}, " +
                        "platformName = {2}", ForDisplay(frameworkName),
                        ForDisplay(frameworkVersion),
                        ForDisplay(platformName)), traceCategory);

                    string installDirectory = null;

                    if (!HaveFrameworkDirectory(
                            rootKey, frameworkName, frameworkVersion,
                            platformName, perUser, wow64, whatIf, verbose,
                            ref installDirectory))
                    {
                        TraceOps.DebugAndTrace(TracePriority.Low,
                            debugCallback, traceCallback, String.Format(
                            ".NET Framework {0} directory not found, " +
                            "skipping...", ForDisplay(frameworkVersion)),
                            traceCategory);

                        continue;
                    }

                    if (callback == null)
                        continue;

                    string directory = installDirectory;

                    if (String.IsNullOrEmpty(directory))
                    {
                        TraceOps.DebugAndTrace(TracePriority.Low,
                            debugCallback, traceCallback, String.Format(
                            ".NET Framework {0} directory is invalid, " +
                            "skipping...", ForDisplay(frameworkVersion)),
                            traceCategory);

                        continue;
                    }

                    directory = Path.Combine(directory, "Config");

                    if (!Directory.Exists(directory))
                    {
                        TraceOps.DebugAndTrace(TracePriority.Low,
                            debugCallback, traceCallback, String.Format(
                            ".NET Framework {0} directory {1} does not " +
                            "exist, skipping...", ForDisplay(frameworkVersion),
                            ForDisplay(directory)), traceCategory);

                        continue;
                    }

                    string fileName = Path.Combine(directory, "machine.config");

                    if (!File.Exists(fileName))
                    {
                        TraceOps.DebugAndTrace(TracePriority.Low,
                            debugCallback, traceCallback, String.Format(
                            ".NET Framework {0} file {1} does not exist, " +
                            "skipping...", ForDisplay(frameworkVersion),
                            ForDisplay(fileName)), traceCategory);

                        continue;
                    }

                    bool localSaved = false;

                    if (!callback(
                            fileName, invariant, name, description, typeName,
                            assemblyName, installDirectory, clientData,
                            perUser, wow64, throwOnMissing, whatIf, verbose,
                            ref localSaved, ref error))
                    {
                        return false;
                    }
                    else
                    {
                        if (localSaved && !saved)
                            saved = true;

                        if (verbose)
                            TraceOps.DebugAndTrace(TracePriority.Lowest,
                                debugCallback, traceCallback, String.Format(
                                "localSaved = {0}, saved = {1}",
                                ForDisplay(localSaved), ForDisplay(saved)),
                                traceCategory);
                    }
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool ForEachFrameworkRegistry(
            MockRegistry registry,
            FrameworkList frameworkList,
            FrameworkRegistryCallback callback,
            string version, /* NOTE: Optional. */
            object clientData,
            bool perUser,
            bool wow64,
            bool throwOnMissing,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            if (registry == null)
            {
                error = "invalid registry";
                return false;
            }

            if (frameworkList == null)
            {
                error = "invalid framework list";
                return false;
            }

            MockRegistryKey rootKey = frameworkList.RootKey;

            if (rootKey == null)
            {
                error = "invalid root key";
                return false;
            }

            if (!IsSupportedRootKey(registry, rootKey))
            {
                error = "root key must be per-user or per-machine";
                return false;
            }

            if (frameworkList.Names == null)
            {
                error = "no framework names found";
                return false;
            }

            if (frameworkList.Versions == null)
            {
                error = "no framework versions found";
                return false;
            }

            if (frameworkList.PlatformNames == null)
            {
                error = "no platform names found";
                return false;
            }

            if (frameworkList.Names.Count != frameworkList.PlatformNames.Count)
            {
                error = String.Format("framework name count {0} does not " +
                    "match platform name count {1}", frameworkList.Names.Count,
                    frameworkList.PlatformNames.Count);

                return false;
            }

            for (int index = 0; index < frameworkList.Names.Count; index++)
            {
                //
                // NOTE: Grab the name of the framework (e.g. ".NETFramework")
                //       and the name of the platform (e.g. "WindowsCE").
                //
                string frameworkName = frameworkList.Names[index];
                string platformName = frameworkList.PlatformNames[index];

                //
                // NOTE: Grab the supported versions of this particular
                //       framework.
                //
                VersionList frameworkVersionList;

                if (version != null)
                {
                    //
                    // NOTE: Manual override of the *ONE* framework version
                    //       to process.
                    //
                    frameworkVersionList = new VersionList();
                    frameworkVersionList.Add(new Version(version));
                }
                else
                {
                    if (!frameworkList.Versions.TryGetValue(
                            frameworkName, out frameworkVersionList) ||
                        (frameworkVersionList == null))
                    {
                        continue;
                    }
                }

                foreach (Version frameworkVersion in frameworkVersionList)
                {
                    TraceOps.DebugAndTrace(TracePriority.Lower,
                        debugCallback, traceCallback, String.Format(
                        "frameworkName = {0}, frameworkVersion = {1}, " +
                        "platformName = {2}", ForDisplay(frameworkName),
                        ForDisplay(frameworkVersion),
                        ForDisplay(platformName)), traceCategory);

                    if (!HaveFrameworkRegistry(
                            rootKey, frameworkName, frameworkVersion,
                            platformName, perUser, wow64, whatIf, verbose))
                    {
                        TraceOps.DebugAndTrace(TracePriority.Low,
                            debugCallback, traceCallback, String.Format(
                            ".NET Framework {0} registry not found, " +
                            "skipping...", ForDisplay(frameworkVersion)),
                            traceCategory);

                        continue;
                    }

                    if (callback == null)
                        continue;

                    if (!callback(
                            rootKey, frameworkName, frameworkVersion,
                            platformName, null, clientData, perUser,
                            wow64, throwOnMissing, whatIf, verbose,
                            ref error))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Per-Visual Studio Version Handling
        private static void InitializeVsList(
            MockRegistryKey rootKey,
            Configuration configuration,
            ref VsList vsList
            )
        {
            if (vsList == null)
                vsList = new VsList();

            if (vsList.RootKey == null)
                vsList.RootKey = rootKey;

            if (vsList.Versions == null)
            {
                vsList.Versions = new VersionList();

                if ((configuration == null) || !configuration.NoVs2005)
                    vsList.Versions.Add(new Version(8, 0)); // 2005

                if ((configuration == null) || !configuration.NoVs2008)
                    vsList.Versions.Add(new Version(9, 0)); // 2008

                if ((configuration == null) || !configuration.NoVs2010)
                    vsList.Versions.Add(new Version(10, 0));// 2010

                if ((configuration == null) || !configuration.NoVs2012)
                    vsList.Versions.Add(new Version(11, 0));// 2012

                if ((configuration == null) || !configuration.NoVs2013)
                    vsList.Versions.Add(new Version(12, 0));// 2013
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool HaveVsVersionDirectory(
            MockRegistryKey rootKey,
            Version vsVersion,
            string suffix,
            bool perUser,
            bool wow64,
            bool whatIf,
            bool verbose,
            ref string installDirectory
            )
        {
            if (vsVersion == null)
                return false;

            string keyName = GetVsKeyName(vsVersion, suffix, perUser, wow64);

            using (MockRegistryKey key = RegistryHelper.OpenSubKey(
                    rootKey, keyName, false, whatIf, verbose))
            {
                if (key == null)
                    return false;

                object value = RegistryHelper.GetValue(
                    key, "InstallDir", null, whatIf, verbose);

                if (!(value is string))
                    return false;

                string directory = (string)value;

                if (String.IsNullOrEmpty(directory))
                    return false;

                if (!Directory.Exists(directory))
                    return false;

                TraceOps.DebugAndTrace(TracePriority.Lower,
                    debugCallback, traceCallback, String.Format(
                    "Visual Studio {0} found in directory {1}.",
                    ForDisplay(vsVersion), ForDisplay(directory)),
                    traceCategory);

                installDirectory = directory;
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool ForEachVsVersionRegistry(
            MockRegistry registry,
            VsList vsList,
            VisualStudioRegistryCallback callback,
            string suffix,
            Package package,
            object clientData,
            bool perUser,
            bool wow64,
            bool throwOnMissing,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            if (registry == null)
            {
                error = "invalid registry";
                return false;
            }

            if (vsList == null)
            {
                error = "invalid VS list";
                return false;
            }

            MockRegistryKey rootKey = vsList.RootKey;

            if (rootKey == null)
            {
                error = "invalid root key";
                return false;
            }

            if (!IsSupportedRootKey(registry, rootKey))
            {
                error = "root key must be per-user or per-machine";
                return false;
            }

            if (vsList.Versions == null)
            {
                error = "no VS versions found";
                return false;
            }

            foreach (Version vsVersion in vsList.Versions)
            {
                TraceOps.DebugAndTrace(TracePriority.Lower,
                    debugCallback, traceCallback, String.Format(
                    "vsVersion = {0}", ForDisplay(vsVersion)),
                    traceCategory);

                string installDirectory = null;

                if (!HaveVsVersionDirectory(
                        rootKey, vsVersion, suffix, perUser, wow64, whatIf,
                        verbose, ref installDirectory))
                {
                    TraceOps.DebugAndTrace(TracePriority.Low,
                        debugCallback, traceCallback, String.Format(
                        "Visual Studio {0} not found, skipping...",
                        ForDisplay(vsVersion)), traceCategory);

                    continue;
                }

                if (callback == null)
                    continue;

                if (!callback(
                        rootKey, vsVersion, suffix, package, installDirectory,
                        clientData, perUser, wow64, throwOnMissing, whatIf,
                        verbose, ref error))
                {
                    return false;
                }
            }

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Configuration File Handling
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool AddDbProviderFactory(
            string fileName,
            string invariant,
            string name,
            string description,
            string typeName,
            AssemblyName assemblyName,
            bool whatIf,
            bool verbose,
            ref bool saved,
            ref string error
            )
        {
            bool dirty = false;
            XmlDocument document = new XmlDocument();

            document.PreserveWhitespace = true;
            document.Load(fileName);

            XmlElement addElement = document.SelectSingleNode(
                String.Format(XPathForAddElement, invariant)) as XmlElement;

            if (addElement == null)
            {
                string[] elementNames = {
                    "system.data", "DbProviderFactories"
                };

                XmlElement previousElement =
                    document.DocumentElement; /* configuration */

                foreach (string elementName in elementNames)
                {
                    addElement = previousElement.SelectSingleNode(
                        elementName) as XmlElement;

                    if (addElement == null)
                    {
                        addElement = document.CreateElement(
                            elementName, String.Empty);

                        previousElement.AppendChild(addElement);
                    }

                    previousElement = addElement;
                }

                addElement = document.CreateElement(
                    "add", String.Empty);

                previousElement.AppendChild(addElement);

                dirty = true;
            }

            if (!String.Equals(addElement.GetAttribute("name"),
                    name, StringComparison.Ordinal))
            {
                addElement.SetAttribute("name", name);
                dirty = true;
            }

            if (!String.Equals(addElement.GetAttribute("invariant"),
                    invariant, StringComparison.Ordinal))
            {
                addElement.SetAttribute("invariant", invariant);
                dirty = true;
            }

            if (!String.Equals(addElement.GetAttribute("description"),
                    description, StringComparison.Ordinal))
            {
                addElement.SetAttribute("description", description);
                dirty = true;
            }

            string fullTypeName = String.Format("{0}, {1}",
                typeName, assemblyName);

            if (!String.Equals(addElement.GetAttribute("type"),
                    fullTypeName, StringComparison.Ordinal))
            {
                addElement.SetAttribute("type", fullTypeName);
                dirty = true;
            }

            if (dirty || whatIf)
            {
                if (verbose)
                    TraceOps.DebugAndTrace(TracePriority.Highest,
                        debugCallback, traceCallback, String.Format(
                        "addElement = {0}", ForDisplay(addElement)),
                        traceCategory);

                if (!whatIf)
                    document.Save(fileName);

                filesModified++;

                saved = true;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool RemoveDbProviderFactory(
            string fileName,
            string invariant,
            bool whatIf,
            bool verbose,
            ref bool saved,
            ref string error
            )
        {
            bool dirty = false;
            XmlDocument document = new XmlDocument();

            document.PreserveWhitespace = true;
            document.Load(fileName);

            XmlElement addElement = document.SelectSingleNode(
                String.Format(XPathForAddElement, invariant)) as XmlElement;

            if (addElement != null)
            {
                addElement.ParentNode.RemoveChild(addElement);
                dirty = true;
            }

            XmlElement removeElement = document.SelectSingleNode(
                String.Format(XPathForRemoveElement, invariant)) as XmlElement;

            if (removeElement != null)
            {
                removeElement.ParentNode.RemoveChild(removeElement);
                dirty = true;
            }

            if (dirty || whatIf)
            {
                if (verbose)
                    TraceOps.DebugAndTrace(TracePriority.Highest,
                        debugCallback, traceCallback, String.Format(
                        "addElement = {0}, removeElement = {1}",
                        ForDisplay(addElement), ForDisplay(removeElement)),
                        traceCategory);

                if (!whatIf)
                    document.Save(fileName);

                filesModified++;

                saved = true;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ProcessDbProviderFactory(
            string fileName,
            string invariant,
            string name,
            string description,
            string typeName,
            AssemblyName assemblyName,
            string directory,
            object clientData,
            bool perUser,
            bool wow64,
            bool throwOnMissing,
            bool whatIf,
            bool verbose,
            ref bool saved,
            ref string error
            )
        {
            AnyPair<string, bool> pair = clientData as AnyPair<string, bool>;

            if (pair == null)
            {
                error = "invalid framework config callback data";
                return false;
            }

            if (pair.Y)
            {
                return RemoveDbProviderFactory(
                    fileName, invariant, whatIf, verbose, ref saved,
                    ref error) &&
                AddDbProviderFactory(
                    fileName, invariant, name, description, typeName,
                    assemblyName, whatIf, verbose, ref saved, ref error);
            }
            else
            {
                return RemoveDbProviderFactory(
                    fileName, invariant, whatIf, verbose, ref saved,
                    ref error);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Assembly Folders Handling
        private static string GetAssemblyFoldersKeyName(
            string frameworkName,
            Version frameworkVersion,
            string platformName,
            bool perUser,
            bool wow64
            )
        {
            //
            // NOTE: This registry key appears to always be 32-bit only
            //       (i.e. probably because it is only used by Visual
            //       Studio, which is currently always 32-bit only).
            //
            string format = !String.IsNullOrEmpty(platformName) ?
                "{0}\\Microsoft\\{1}\\v{2}\\{3}\\AssemblyFoldersEx" :
                "{0}\\Microsoft\\{1}\\v{2}\\AssemblyFoldersEx";

            return String.Format(format, GetRootKeyName(perUser, wow64),
                frameworkName, frameworkVersion, platformName);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool AddToAssemblyFolders(
            MockRegistryKey rootKey,
            string frameworkName,
            Version frameworkVersion,
            string platformName,
            string subKeyName,
            string directory,
            bool perUser,
            bool wow64,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            string keyName = GetAssemblyFoldersKeyName(
                frameworkName, frameworkVersion, platformName, perUser,
                wow64);

            using (MockRegistryKey key = RegistryHelper.OpenSubKey(
                    rootKey, keyName, true, whatIf, verbose))
            {
                if (key == null)
                {
                    error = String.Format(
                        "could not open registry key: {0}\\{1}",
                        rootKey, keyName);

                    return false;
                }

                using (MockRegistryKey subKey = RegistryHelper.CreateSubKey(
                        key, subKeyName, whatIf, verbose))
                {
                    if (subKey == null)
                    {
                        error = String.Format(
                            "could not create registry key: {0}\\{1}",
                            key, subKeyName);

                        return false;
                    }

                    RegistryHelper.SetValue(
                        subKey, null, directory, whatIf, verbose);
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool RemoveFromAssemblyFolders(
            MockRegistryKey rootKey,
            string frameworkName,
            Version frameworkVersion,
            string platformName,
            string subKeyName,
            bool perUser,
            bool wow64,
            bool throwOnMissing,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            string keyName = GetAssemblyFoldersKeyName(
                frameworkName, frameworkVersion, platformName, perUser,
                wow64);

            using (MockRegistryKey key = RegistryHelper.OpenSubKey(
                    rootKey, keyName, true, whatIf, verbose))
            {
                if (key == null)
                {
                    error = String.Format(
                        "could not open registry key: {0}\\{1}",
                        rootKey, keyName);

                    return false;
                }

                RegistryHelper.DeleteSubKey(
                    key, subKeyName, throwOnMissing, whatIf, verbose);
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ProcessAssemblyFolders(
            MockRegistryKey rootKey,
            string frameworkName,
            Version frameworkVersion,
            string platformName,
            string directory,
            object clientData,
            bool perUser,
            bool wow64,
            bool throwOnMissing,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            AnyPair<string, bool> pair = clientData as AnyPair<string, bool>;

            if (pair == null)
            {
                error = "invalid framework callback data";
                return false;
            }

            if (pair.Y)
            {
                return RemoveFromAssemblyFolders(
                    rootKey, frameworkName, frameworkVersion, platformName,
                    LegacyProjectName, perUser, wow64, false, whatIf, verbose,
                    ref error) &&
                AddToAssemblyFolders(
                    rootKey, frameworkName, frameworkVersion, platformName,
                    ProjectName, pair.X, perUser, wow64, whatIf, verbose,
                    ref error);
            }
            else
            {
                return RemoveFromAssemblyFolders(
                    rootKey, frameworkName, frameworkVersion, platformName,
                    ProjectName, perUser, wow64, throwOnMissing, whatIf,
                    verbose, ref error);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Visual Studio Handling
        private static string GetVsRootKeyName(
            bool perUser,
            bool wow64
            )
        {
            return String.Format("{0}\\Microsoft\\VisualStudio",
                GetRootKeyName(perUser, wow64));
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetVsKeyName(
            Version vsVersion,
            string suffix,
            bool perUser,
            bool wow64
            )
        {
            if (vsVersion == null)
                return null;

            return String.Format(
                "{0}\\{1}{2}", GetVsRootKeyName(perUser, wow64), vsVersion,
                suffix);
        }

        ///////////////////////////////////////////////////////////////////////

        #region Visual Studio Data Source Handling
        private static bool AddVsDataSource(
            MockRegistryKey rootKey,
            Version vsVersion,
            string suffix,
            Package package,
            bool perUser,
            bool wow64,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            if (vsVersion == null)
            {
                error = "invalid VS version";
                return false;
            }

            if (package == null)
            {
                error = "invalid VS package";
                return false;
            }

            string keyName = GetVsKeyName(vsVersion, suffix, perUser, wow64);

            using (MockRegistryKey key = RegistryHelper.OpenSubKey(
                    rootKey, keyName, false, whatIf, verbose))
            {
                if (key == null)
                {
                    error = String.Format(
                        "could not open registry key: {0}\\{1}",
                        rootKey, keyName);

                    return false;
                }

                using (MockRegistryKey subKey = RegistryHelper.OpenSubKey(
                        key, "DataSources", true, whatIf, verbose))
                {
                    if (subKey == null)
                    {
                        error = String.Format(
                            "could not open registry key: {0}\\DataSources",
                            key);

                        return false;
                    }

                    using (MockRegistryKey dataSourceKey =
                            RegistryHelper.CreateSubKey(subKey,
                            package.DataSourceId.ToString(VsIdFormat),
                            whatIf, verbose))
                    {
                        if (dataSourceKey == null)
                        {
                            error = String.Format(
                                "could not create registry key: {0}\\{1}", key,
                                package.DataSourceId.ToString(VsIdFormat));

                            return false;
                        }

                        RegistryHelper.SetValue(
                            dataSourceKey, null, String.Format(
                            "{0} Database File", ProjectName), whatIf,
                            verbose);

                        //
                        // NOTE: This value is new as of 1.0.83.0.
                        //
                        RegistryHelper.SetValue(
                            dataSourceKey, "DefaultProvider",
                            package.DataProviderId.ToString(VsIdFormat),
                            whatIf, verbose);

                        RegistryHelper.CreateSubKey(dataSourceKey,
                            String.Format("SupportingProviders\\{0}",
                            package.DataProviderId.ToString(VsIdFormat)),
                            whatIf, verbose);
                    }
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool RemoveVsDataSource(
            MockRegistryKey rootKey,
            Version vsVersion,
            string suffix,
            Package package,
            bool perUser,
            bool wow64,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            if (vsVersion == null)
            {
                error = "invalid VS version";
                return false;
            }

            if (package == null)
            {
                error = "invalid VS package";
                return false;
            }

            string keyName = GetVsKeyName(vsVersion, suffix, perUser, wow64);

            using (MockRegistryKey key = RegistryHelper.OpenSubKey(
                    rootKey, keyName, false, whatIf, verbose))
            {
                if (key == null)
                {
                    error = String.Format(
                        "could not open registry key: {0}\\{1}",
                        rootKey, keyName);

                    return false;
                }

                using (MockRegistryKey subKey = RegistryHelper.OpenSubKey(
                        key, "DataSources", true, whatIf, verbose))
                {
                    if (subKey == null)
                    {
                        error = String.Format(
                            "could not open registry key: {0}\\DataSources",
                            key);

                        return false;
                    }

                    RegistryHelper.DeleteSubKeyTree(
                        subKey, package.DataSourceId.ToString(VsIdFormat),
                        whatIf, verbose);
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ProcessVsDataSource(
            MockRegistryKey rootKey,
            Version vsVersion,
            string suffix,
            Package package,
            string directory,
            object clientData,
            bool perUser,
            bool wow64,
            bool throwOnMissing,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            if (package == null)
            {
                error = "invalid VS package";
                return false;
            }

            AnyPair<string, bool> pair = clientData as AnyPair<string, bool>;

            if (pair == null)
            {
                error = "invalid VS callback data";
                return false;
            }

            if (pair.Y)
            {
                return AddVsDataSource(
                    rootKey, vsVersion, suffix, package, perUser, wow64,
                    whatIf, verbose, ref error);
            }
            else
            {
                return RemoveVsDataSource(
                    rootKey, vsVersion, suffix, package, perUser, wow64,
                    whatIf, verbose, ref error);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Visual Studio Data Provider Handling
        private static bool AddVsDataProvider(
            MockRegistryKey rootKey,
            Version vsVersion,
            string suffix,
            Package package,
            string fileName,
            bool perUser,
            bool wow64,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            if (vsVersion == null)
            {
                error = "invalid VS version";
                return false;
            }

            if (package == null)
            {
                error = "invalid VS package";
                return false;
            }

            string keyName = GetVsKeyName(vsVersion, suffix, perUser, wow64);

            using (MockRegistryKey key = RegistryHelper.OpenSubKey(
                    rootKey, keyName, false, whatIf, verbose))
            {
                if (key == null)
                {
                    error = String.Format(
                        "could not open registry key: {0}\\{1}",
                        rootKey, keyName);

                    return false;
                }

                using (MockRegistryKey subKey = RegistryHelper.OpenSubKey(
                        key, "DataProviders", true, whatIf, verbose))
                {
                    if (subKey == null)
                    {
                        error = String.Format(
                            "could not open registry key: {0}\\DataProviders",
                            key);

                        return false;
                    }

                    using (MockRegistryKey dataProviderKey =
                            RegistryHelper.CreateSubKey(subKey,
                            package.DataProviderId.ToString(VsIdFormat),
                            whatIf, verbose))
                    {
                        if (dataProviderKey == null)
                        {
                            error = String.Format(
                                "could not create registry key: {0}\\{1}", key,
                                package.DataProviderId.ToString(VsIdFormat));

                            return false;
                        }

                        RegistryHelper.SetValue(
                            dataProviderKey, null, Description, whatIf,
                            verbose);

                        //
                        // NOTE: This value is new as of 1.0.83.0.  However,
                        //       it should only be set if the package assembly
                        //       and all the assemblies it refers to are being
                        //       placed into the global assembly cache.
                        //
                        if (package.GlobalAssemblyCache)
                        {
                            RegistryHelper.SetValue(
                                dataProviderKey, "Assembly",
                                package.AssemblyName.ToString(),
                                whatIf, verbose);
                        }

                        //
                        // NOTE: This value is new as of 1.0.83.0.
                        //
                        RegistryHelper.SetValue(
                            dataProviderKey, "AssociatedSource",
                            package.DataSourceId.ToString(VsIdFormat),
                            whatIf, verbose);

                        RegistryHelper.SetValue(
                            dataProviderKey, "InvariantName", InvariantName,
                            whatIf, verbose);

                        RegistryHelper.SetValue(
                            dataProviderKey, "Technology",
                            package.AdoNetTechnologyId.ToString(VsIdFormat),
                            whatIf, verbose);

                        RegistryHelper.SetValue(
                            dataProviderKey, "CodeBase", fileName, whatIf,
                            verbose);

                        RegistryHelper.SetValue(
                            dataProviderKey, "FactoryService",
                            package.ServiceId.ToString(VsIdFormat), whatIf,
                            verbose);

                        RegistryHelper.CreateSubKey(dataProviderKey,
                            "SupportedObjects\\DataConnectionUIControl",
                            whatIf, verbose);

                        RegistryHelper.CreateSubKey(dataProviderKey,
                            "SupportedObjects\\DataConnectionProperties",
                            whatIf, verbose);

                        RegistryHelper.CreateSubKey(dataProviderKey,
                            "SupportedObjects\\DataConnectionSupport", whatIf,
                            verbose);

                        RegistryHelper.CreateSubKey(dataProviderKey,
                            "SupportedObjects\\DataObjectSupport", whatIf,
                            verbose);

                        RegistryHelper.CreateSubKey(dataProviderKey,
                            "SupportedObjects\\DataViewSupport", whatIf,
                            verbose);
                    }
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool RemoveVsDataProvider(
            MockRegistryKey rootKey,
            Version vsVersion,
            string suffix,
            Package package,
            bool perUser,
            bool wow64,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            if (vsVersion == null)
            {
                error = "invalid VS version";
                return false;
            }

            string keyName = GetVsKeyName(vsVersion, suffix, perUser, wow64);

            using (MockRegistryKey key = RegistryHelper.OpenSubKey(
                    rootKey, keyName, false, whatIf, verbose))
            {
                if (key == null)
                {
                    error = String.Format(
                        "could not open registry key: {0}\\{1}",
                        rootKey, keyName);

                    return false;
                }

                using (MockRegistryKey subKey = RegistryHelper.OpenSubKey(
                        key, "DataProviders", true, whatIf, verbose))
                {
                    if (subKey == null)
                    {
                        error = String.Format(
                            "could not open registry key: {0}\\DataProviders",
                            key);

                        return false;
                    }

                    RegistryHelper.DeleteSubKeyTree(
                        subKey, package.DataProviderId.ToString(VsIdFormat),
                        whatIf, verbose);
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ProcessVsDataProvider(
            MockRegistryKey rootKey,
            Version vsVersion,
            string suffix,
            Package package,
            string directory,
            object clientData,
            bool perUser,
            bool wow64,
            bool throwOnMissing,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            AnyPair<string, bool> pair = clientData as AnyPair<string, bool>;

            if (pair == null)
            {
                error = "invalid VS callback data";
                return false;
            }

            if (pair.Y)
            {
                return AddVsDataProvider(
                    rootKey, vsVersion, suffix, package, pair.X, perUser,
                    wow64, whatIf, verbose, ref error);
            }
            else
            {
                return RemoveVsDataProvider(
                    rootKey, vsVersion, suffix, package, perUser, wow64,
                    whatIf, verbose, ref error);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Visual Studio Package Handling
        private static void InitializeVsPackage(
            AssemblyName assemblyName,
            bool globalAssemblyCache,
            ref Package package
            )
        {
            if (package == null)
            {
                package = new Package();

                package.AssemblyName = assemblyName;
                package.GlobalAssemblyCache = globalAssemblyCache;

                package.AdoNetTechnologyId = new Guid(
                    "77AB9A9D-78B9-4BA7-91AC-873F5338F1D2");

                package.PackageId = new Guid(
                    "DCBE6C8D-0E57-4099-A183-98FF74C64D9C");

                package.ServiceId = new Guid(
                    "DCBE6C8D-0E57-4099-A183-98FF74C64D9D");

                package.DataSourceId = new Guid(
                    "0EBAAB6E-CA80-4B4A-8DDF-CBE6BF058C71");

                package.DataProviderId = new Guid(
                    "0EBAAB6E-CA80-4B4A-8DDF-CBE6BF058C70");
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool AddVsPackage(
            MockRegistryKey rootKey,
            Version vsVersion,
            string suffix,
            Package package,
            string fileName,
            bool perUser,
            bool wow64,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            if (vsVersion == null)
            {
                error = "invalid VS version";
                return false;
            }

            if (package == null)
            {
                error = "invalid VS package";
                return false;
            }

            string keyName = GetVsKeyName(vsVersion, suffix, perUser, wow64);

            using (MockRegistryKey key = RegistryHelper.OpenSubKey(
                    rootKey, keyName, false, whatIf, verbose))
            {
                if (key == null)
                {
                    error = String.Format(
                        "could not open registry key: {0}\\{1}",
                        rootKey, keyName);

                    return false;
                }

                using (MockRegistryKey subKey = RegistryHelper.OpenSubKey(
                        key, "Packages", true, whatIf, verbose))
                {
                    if (subKey == null)
                    {
                        error = String.Format(
                            "could not open registry key: {0}\\Packages",
                            key);

                        return false;
                    }

                    //
                    // NOTE: *WARNING* Changing any of these values will likely
                    //       require a new "package load key" (PLK) to be
                    //       generated in order to properly support loading the
                    //       package into Visual Studio 2008 and earlier without
                    //       the matching Visual Studio SDK being installed.
                    //       Please refer to the "SQLite.Designer\plk.txt" file
                    //       for the existing official values and update them if
                    //       necessary.  Also, the newly generated package load
                    //       key itself, which is a 128 character alphanumeric
                    //       string, must be placed in the resource string named
                    //       "400" in the "SQLite.Designer\VSPackage.resx" file
                    //       and then the designer assembly itself must be
                    //       recompiled.  As of this writing (in February 2012),
                    //       the following URL is the proper place to generate
                    //       package load keys:
                    //
                    //       http://msdn.microsoft.com/en-us/vstudio/cc655795
                    //
                    using (MockRegistryKey packageKey =
                            RegistryHelper.CreateSubKey(subKey,
                            package.PackageId.ToString(VsIdFormat), whatIf,
                            verbose))
                    {
                        if (packageKey == null)
                        {
                            error = String.Format(
                                "could not create registry key: {0}\\{1}",
                                key, package.PackageId.ToString(VsIdFormat));

                            return false;
                        }

                        RegistryHelper.SetValue(packageKey, null,
                            String.Format("{0} Designer Package", ProjectName),
                            whatIf, verbose);

                        RegistryHelper.SetValue(packageKey, "Class",
                            "SQLite.Designer.SQLitePackage", whatIf, verbose);

                        RegistryHelper.SetValue(packageKey, "CodeBase",
                            fileName, whatIf, verbose);

                        RegistryHelper.SetValue(packageKey, "ID", 400, whatIf,
                            verbose);

                        string directory = GetSystemDirectory(wow64);

                        if (directory == null)
                            directory = String.Empty;

                        RegistryHelper.SetValue(packageKey, "InprocServer32",
                            Path.Combine(directory, "mscoree.dll"),
                            whatIf, verbose);

                        RegistryHelper.SetValue(packageKey, "CompanyName",
                            "http://system.data.sqlite.org/", whatIf, verbose);

                        RegistryHelper.SetValue(packageKey, "MinEdition",
                            "standard", whatIf, verbose);

                        RegistryHelper.SetValue(packageKey, "ProductName",
                            String.Format("{0} Designer Package", ProjectName),
                            whatIf, verbose);

                        RegistryHelper.SetValue(packageKey, "ProductVersion",
                            "1.0", whatIf, verbose);

                        using (MockRegistryKey toolboxKey =
                                RegistryHelper.CreateSubKey(packageKey,
                                "Toolbox", whatIf, verbose))
                        {
                            if (toolboxKey == null)
                            {
                                error = String.Format(
                                    "could not create registry key: " +
                                    "{0}\\Toolbox", packageKey);

                                return false;
                            }

                            RegistryHelper.SetValue(
                                toolboxKey, "Default Items", 3, whatIf,
                                verbose);
                        }
                    }
                }

                using (MockRegistryKey subKey = RegistryHelper.OpenSubKey(
                        key, "Menus", true, whatIf, verbose))
                {
                    if (subKey == null)
                    {
                        error = String.Format(
                            "could not open registry key: {0}\\Menus",
                            key);

                        return false;
                    }

                    RegistryHelper.SetValue(
                        subKey, package.PackageId.ToString(VsIdFormat),
                        ", 1000, 3", whatIf, verbose);
                }

                using (MockRegistryKey subKey = RegistryHelper.OpenSubKey(
                        key, "Services", true, whatIf, verbose))
                {
                    if (subKey == null)
                    {
                        error = String.Format(
                            "could not open registry key: {0}\\Services",
                            key);

                        return false;
                    }

                    using (MockRegistryKey serviceKey =
                            RegistryHelper.CreateSubKey(subKey,
                            package.ServiceId.ToString(VsIdFormat), whatIf,
                            verbose))
                    {
                        if (serviceKey == null)
                        {
                            error = String.Format(
                                "could not create registry key: {0}\\{1}",
                                key, package.ServiceId.ToString(VsIdFormat));

                            return false;
                        }

                        RegistryHelper.SetValue(serviceKey, null,
                            package.PackageId.ToString(VsIdFormat), whatIf,
                            verbose);

                        RegistryHelper.SetValue(serviceKey, "Name",
                            String.Format("{0} Designer Service", ProjectName),
                            whatIf, verbose);
                    }
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool RemoveVsPackage(
            MockRegistryKey rootKey,
            Version vsVersion,
            string suffix,
            Package package,
            bool perUser,
            bool wow64,
            bool throwOnMissing,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            if (vsVersion == null)
            {
                error = "invalid VS version";
                return false;
            }

            if (package == null)
            {
                error = "invalid VS package";
                return false;
            }

            string keyName = GetVsKeyName(vsVersion, suffix, perUser, wow64);

            using (MockRegistryKey key = RegistryHelper.OpenSubKey(
                    rootKey, keyName, false, whatIf, verbose))
            {
                if (key == null)
                {
                    error = String.Format(
                        "could not open registry key: {0}\\{1}",
                        rootKey, keyName);

                    return false;
                }

                using (MockRegistryKey subKey = RegistryHelper.OpenSubKey(
                        key, "Packages", true, whatIf, verbose))
                {
                    if (subKey == null)
                    {
                        error = String.Format(
                            "could not open registry key: {0}\\Packages",
                            key);

                        return false;
                    }

                    RegistryHelper.DeleteSubKeyTree(
                        subKey, package.PackageId.ToString(VsIdFormat),
                        whatIf, verbose);
                }

                using (MockRegistryKey subKey = RegistryHelper.OpenSubKey(
                        key, "Menus", true, whatIf, verbose))
                {
                    if (subKey == null)
                    {
                        error = String.Format(
                            "could not open registry key: {0}\\Menus",
                            key);

                        return false;
                    }

                    RegistryHelper.DeleteValue(
                        subKey, package.PackageId.ToString(VsIdFormat),
                        throwOnMissing, whatIf, verbose);
                }

                using (MockRegistryKey subKey = RegistryHelper.OpenSubKey(
                        key, "Services", true, whatIf, verbose))
                {
                    if (subKey == null)
                    {
                        error = String.Format(
                            "could not open registry key: {0}\\Services",
                            key);

                        return false;
                    }

                    RegistryHelper.DeleteSubKeyTree(
                        subKey, package.ServiceId.ToString(VsIdFormat),
                        whatIf, verbose);
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ProcessVsPackage(
            MockRegistryKey rootKey,
            Version vsVersion,
            string suffix,
            Package package,
            string directory,
            object clientData,
            bool perUser,
            bool wow64,
            bool throwOnMissing,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            AnyPair<string, bool> pair = clientData as AnyPair<string, bool>;

            if (pair == null)
            {
                error = "invalid VS callback data";
                return false;
            }

            if (pair.Y)
            {
                return AddVsPackage(
                    rootKey, vsVersion, suffix, package, pair.X, perUser,
                    wow64, whatIf, verbose, ref error);
            }
            else
            {
                return RemoveVsPackage(
                    rootKey, vsVersion, suffix, package, perUser, wow64,
                    throwOnMissing, whatIf, verbose, ref error);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Visual Studio Setup Handling
        private static void VsDevEnvSetupOutputDataReceived(
            object sender,
            DataReceivedEventArgs e
            )
        {
            Process process = sender as Process;

            TraceOps.DebugAndTrace(TracePriority.Medium,
                debugCallback, traceCallback, String.Format(
                VsDevEnvSetupFormat, ForDisplay(process),
                ForDisplay(e)), traceCategory);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void VsDevEnvSetupErrorDataReceived(
            object sender,
            DataReceivedEventArgs e
            )
        {
            Process process = sender as Process;

            TraceOps.DebugAndTrace(TracePriority.Medium,
                debugCallback, traceCallback, String.Format(
                VsDevEnvSetupFormat, ForDisplay(process),
                ForDisplay(e)), traceCategory);
        }

        ///////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool AddVsDevEnvSetup(
            Version vsVersion,
            string directory,
            bool perUser,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            if (perUser)
            {
                //
                // NOTE: Visual Studio does not support running in 'setup'
                //       mode on a per-user basis; therefore, skip running
                //       it in that case.
                //
                TraceOps.DebugAndTrace(TracePriority.Medium,
                    debugCallback, traceCallback, String.Format(
                    "Visual Studio {0} 'setup' mode is per-machine only, " +
                    "skipping...", ForDisplay(vsVersion)), traceCategory);

                return true;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo();

            //
            // NOTE: Set the core properties for the process to start.  In this
            //       case, we are starting the primary Visual Studio executable
            //       (devenv.exe) in "setup" mode, so that it can refresh its
            //       list of installed packages and their associated resources.
            //
            startInfo.FileName = Path.Combine(directory, "devenv.exe");
            startInfo.Arguments = "/setup";
            startInfo.WorkingDirectory = directory;

            //
            // NOTE: Set the boolean flag properties that require non-default
            //       values for the process to start.  In this case, we do not
            //       want the shell to be used for starting the process.  In
            //       addition, both standard output and error data should be
            //       redirected, so it can be logged properly.
            //
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            Process process = new Process();

            process.StartInfo = startInfo;

            process.OutputDataReceived += new DataReceivedEventHandler(
                VsDevEnvSetupOutputDataReceived);

            process.ErrorDataReceived += new DataReceivedEventHandler(
                VsDevEnvSetupErrorDataReceived);

            if (verbose)
                TraceOps.DebugAndTrace(TracePriority.Highest,
                    debugCallback, traceCallback, ForDisplay(startInfo),
                    traceCategory);

            //
            // NOTE: In "what-if" mode, do not actually start the process.
            //
            if (!whatIf)
            {
                process.Start();

                if (verbose)
                    TraceOps.DebugAndTrace(TracePriority.Highest,
                        debugCallback, traceCallback, String.Format(
                        "process = {0}", ForDisplay(process)),
                        traceCategory);

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool RemoveVsDevEnvSetup(
            Version vsVersion,
            string directory,
            bool perUser,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            //
            // NOTE: Since Visual Studio does not have an 'undo' operation for
            //       its 'setup' mode, simply execute the same command again.
            //       This should force it to refresh its list of installed
            //       packages and their associated resources (i.e. this will
            //       effectively 'remove' the package being processed since
            //       this is being done after all the other changes for the
            //       package removal have been completed).
            //
            return AddVsDevEnvSetup(
                vsVersion, directory, perUser, whatIf, verbose, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ProcessVsDevEnvSetup(
            MockRegistryKey rootKey,
            Version vsVersion,
            string suffix,
            Package package,
            string directory,
            object clientData,
            bool perUser,
            bool wow64,
            bool throwOnMissing,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            AnyPair<string, bool> pair = clientData as AnyPair<string, bool>;

            if (pair == null)
            {
                error = "invalid VS callback data";
                return false;
            }

            if (pair.Y)
            {
                return AddVsDevEnvSetup(
                    vsVersion, directory, perUser, whatIf, verbose, ref error);
            }
            else
            {
                return RemoveVsDevEnvSetup(
                    vsVersion, directory, perUser, whatIf, verbose, ref error);
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Application Entry Point
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int Main(
            string[] args
            )
        {
            try
            {
                Configuration configuration = null;
                string error = null;

                ///////////////////////////////////////////////////////////////

                #region Debugger Hook
                if (Environment.GetEnvironmentVariable("Break") != null)
                {
                    Console.WriteLine(
                        "Attach a debugger to process {0} and " +
                        "press any key to continue.",
                        Process.GetCurrentProcess().Id);

                    try
                    {
                        Console.ReadKey(true); /* throw */
                    }
                    catch (InvalidOperationException) // Console.ReadKey
                    {
                        // do nothing.
                    }

                    Debugger.Break();
                }
                #endregion

                ///////////////////////////////////////////////////////////////

                #region Command Line Processing
                if (!Configuration.FromArgs(
                        args, true, ref configuration, ref error) ||
                    !Configuration.Process(
                        args, configuration, true, ref error) ||
                    !Configuration.CheckRuntimeVersion(
                        configuration, true, ref error))
                {
                    TraceOps.ShowMessage(TracePriority.Highest,
                        debugCallback, traceCallback, thisAssembly,
                        error, traceCategory, MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    TraceOps.DebugAndTrace(TracePriority.MediumHigh,
                        debugCallback, traceCallback, "Failure.",
                        traceCategory);

                    return 1; /* FAILURE */
                }
                #endregion

                ///////////////////////////////////////////////////////////////

                using (MockRegistry registry = new MockRegistry(
                        configuration.WhatIf, false, false))
                {
                    #region Core Assembly Name Check
                    //
                    // NOTE: Do this first, before making any changes to the
                    //       system, because it will throw an exception if the
                    //       file name does not represent a valid managed
                    //       assembly.
                    //
                    AssemblyName coreAssemblyName =
                        AssemblyName.GetAssemblyName(
                            configuration.CoreFileName); /* throw */

                    AssemblyName designerAssemblyName =
                        AssemblyName.GetAssemblyName(
                            configuration.DesignerFileName); /* throw */
                    #endregion

                    ///////////////////////////////////////////////////////////

                    #region System Directory Check
                    //
                    // NOTE: Getting the system directory value here serves
                    //       two purposes:
                    //
                    //       1. It enables us to log the system directory
                    //          value very early in the installation process
                    //          (i.e. even though the value itself is not
                    //          needed until much later).
                    //
                    //       2. Since the value is cached, it prevents an
                    //          exception from being thrown much later during
                    //          the install when the value is queried again
                    //          (i.e. with the same value for the "wow64"
                    //          parameter).
                    //
                    TraceOps.DebugAndTrace(TracePriority.MediumLow,
                        debugCallback, traceCallback, String.Format(
                        "System directory is {0}.", ForDisplay(
                        GetSystemDirectory(configuration.Wow64))),
                        traceCategory); /* throw */
                    #endregion

                    ///////////////////////////////////////////////////////////

                    #region .NET Framework / Visual Studio Data
                    Package package = null;
                    FrameworkList frameworkList = null;
                    VsList vsList = null;

                    ///////////////////////////////////////////////////////////

                    InitializeVsPackage(designerAssemblyName,
                        configuration.HasFlags(
                            InstallFlags.GlobalAssemblyCache, true) &&
                        configuration.HasFlags(
                            InstallFlags.VsPackageGlobalAssemblyCache, true),
                        ref package);

                    ///////////////////////////////////////////////////////////

                    InitializeFrameworkList(configuration.PerUser ?
                        registry.CurrentUser : registry.LocalMachine,
                        configuration, ref frameworkList);

                    InitializeVsList(configuration.PerUser ?
                        registry.CurrentUser : registry.LocalMachine,
                        configuration, ref vsList);
                    #endregion

                    ///////////////////////////////////////////////////////////

                    #region Shared Client Data Creation
                    object directoryData = new AnyPair<string, bool>(
                        configuration.Directory, configuration.Install);

                    object fileNameData = new AnyPair<string, bool>(
                        configuration.DesignerFileName, configuration.Install);
                    #endregion

                    ///////////////////////////////////////////////////////////

                    #region .NET GAC Install/Remove
                    if (configuration.HasFlags(
                            InstallFlags.GlobalAssemblyCache, true))
                    {
                        Publish publish = null;

                        if (!configuration.WhatIf)
                            publish = new Publish();

                        if (configuration.Install)
                        {
                            if (!configuration.WhatIf)
                                /* throw */
                                publish.GacInstall(configuration.CoreFileName);

                            TraceOps.DebugAndTrace(TracePriority.Highest,
                                debugCallback, traceCallback, String.Format(
                                "GacInstall: assemblyPath = {0}",
                                ForDisplay(configuration.CoreFileName)),
                                traceCategory);

                            if (configuration.IsLinqSupported())
                            {
                                if (!configuration.WhatIf)
                                    /* throw */
                                    publish.GacInstall(configuration.LinqFileName);

                                TraceOps.DebugAndTrace(TracePriority.Highest,
                                    debugCallback, traceCallback, String.Format(
                                    "GacInstall: assemblyPath = {0}",
                                    ForDisplay(configuration.LinqFileName)),
                                    traceCategory);
                            }

                            if (configuration.IsEf6Supported())
                            {
                                if (!configuration.WhatIf)
                                    /* throw */
                                    publish.GacInstall(configuration.Ef6FileName);

                                TraceOps.DebugAndTrace(TracePriority.Highest,
                                    debugCallback, traceCallback, String.Format(
                                    "GacInstall: assemblyPath = {0}",
                                    ForDisplay(configuration.Ef6FileName)),
                                    traceCategory);
                            }

                            if (configuration.HasFlags(
                                    InstallFlags.VsPackageGlobalAssemblyCache, true))
                            {
                                if (!configuration.WhatIf)
                                    /* throw */
                                    publish.GacInstall(configuration.DesignerFileName);

                                TraceOps.DebugAndTrace(TracePriority.Highest,
                                    debugCallback, traceCallback, String.Format(
                                    "GacInstall: assemblyPath = {0}",
                                    ForDisplay(configuration.DesignerFileName)),
                                    traceCategory);
                            }
                        }
                        else
                        {
                            if (configuration.HasFlags(
                                    InstallFlags.VsPackageGlobalAssemblyCache, true))
                            {
                                if (!configuration.WhatIf)
                                    /* throw */
                                    publish.GacRemove(configuration.DesignerFileName);

                                TraceOps.DebugAndTrace(TracePriority.Highest,
                                    debugCallback, traceCallback, String.Format(
                                    "GacRemove: assemblyPath = {0}",
                                    ForDisplay(configuration.DesignerFileName)),
                                    traceCategory);
                            }

                            if (configuration.IsEf6Supported())
                            {
                                if (!configuration.WhatIf)
                                    /* throw */
                                    publish.GacRemove(configuration.Ef6FileName);

                                TraceOps.DebugAndTrace(TracePriority.Highest,
                                    debugCallback, traceCallback, String.Format(
                                    "GacRemove: assemblyPath = {0}",
                                    ForDisplay(configuration.Ef6FileName)),
                                    traceCategory);
                            }

                            if (configuration.IsLinqSupported())
                            {
                                if (!configuration.WhatIf)
                                    /* throw */
                                    publish.GacRemove(configuration.LinqFileName);

                                TraceOps.DebugAndTrace(TracePriority.Highest,
                                    debugCallback, traceCallback, String.Format(
                                    "GacRemove: assemblyPath = {0}",
                                    ForDisplay(configuration.LinqFileName)),
                                    traceCategory);
                            }

                            if (!configuration.WhatIf)
                                /* throw */
                                publish.GacRemove(configuration.CoreFileName);

                            TraceOps.DebugAndTrace(TracePriority.Highest,
                                debugCallback, traceCallback, String.Format(
                                "GacRemove: assemblyPath = {0}",
                                ForDisplay(configuration.CoreFileName)),
                                traceCategory);
                        }
                    }
                    #endregion

                    ///////////////////////////////////////////////////////////

                    #region .NET AssemblyFolders
                    if (configuration.HasFlags(
                            InstallFlags.AssemblyFolders, true))
                    {
                        if (!ForEachFrameworkRegistry(registry,
                                frameworkList, ProcessAssemblyFolders,
                                configuration.RegistryVersion, directoryData,
                                configuration.PerUser,
                                NetFxIs32BitOnly || configuration.Wow64,
                                configuration.ThrowOnMissing,
                                configuration.WhatIf, configuration.Verbose,
                                ref error))
                        {
                            TraceOps.ShowMessage(TracePriority.Highest,
                                debugCallback, traceCallback, thisAssembly,
                                error, traceCategory, MessageBoxButtons.OK,
                                MessageBoxIcon.Error);

                            TraceOps.DebugAndTrace(TracePriority.MediumHigh,
                                debugCallback, traceCallback, "Failure.",
                                traceCategory);

                            return 1; /* FAILURE */
                        }
                    }
                    #endregion

                    ///////////////////////////////////////////////////////////

                    #region .NET DbProviderFactory
                    if (configuration.HasFlags(
                            InstallFlags.DbProviderFactory, true))
                    {
                        bool saved = false;

                        if (!ForEachFrameworkConfig(registry,
                                frameworkList, ProcessDbProviderFactory,
                                configuration.ConfigVersion, InvariantName,
                                ProviderName, Description, FactoryTypeName,
                                coreAssemblyName, directoryData,
                                configuration.PerUser,
                                NetFxIs32BitOnly || configuration.Wow64,
                                configuration.ThrowOnMissing,
                                configuration.WhatIf, configuration.Verbose,
                                ref saved, ref error))
                        {
                            TraceOps.ShowMessage(TracePriority.Highest,
                                debugCallback, traceCallback, thisAssembly,
                                error, traceCategory, MessageBoxButtons.OK,
                                MessageBoxIcon.Error);

                            TraceOps.DebugAndTrace(TracePriority.MediumHigh,
                                debugCallback, traceCallback, "Failure.",
                                traceCategory);

                            return 1; /* FAILURE */
                        }
                    }
                    #endregion

                    ///////////////////////////////////////////////////////////

                    #region VS Package
                    if (configuration.HasFlags(
                            InstallFlags.VsPackage, true))
                    {
                        if (!ForEachVsVersionRegistry(registry,
                                vsList, ProcessVsPackage,
                                configuration.VsVersionSuffix, package,
                                fileNameData, configuration.PerUser,
                                VsIs32BitOnly || configuration.Wow64,
                                configuration.ThrowOnMissing,
                                configuration.WhatIf, configuration.Verbose,
                                ref error))
                        {
                            TraceOps.ShowMessage(TracePriority.Highest,
                                debugCallback, traceCallback, thisAssembly,
                                error, traceCategory, MessageBoxButtons.OK,
                                MessageBoxIcon.Error);

                            TraceOps.DebugAndTrace(TracePriority.MediumHigh,
                                debugCallback, traceCallback, "Failure.",
                                traceCategory);

                            return 1; /* FAILURE */
                        }
                    }
                    #endregion

                    ///////////////////////////////////////////////////////////

                    #region VS DataSource
                    if (configuration.HasFlags(
                            InstallFlags.VsDataSource, true))
                    {
                        if (!ForEachVsVersionRegistry(registry,
                                vsList, ProcessVsDataSource,
                                configuration.VsVersionSuffix, package,
                                fileNameData, configuration.PerUser,
                                VsIs32BitOnly || configuration.Wow64,
                                configuration.ThrowOnMissing,
                                configuration.WhatIf, configuration.Verbose,
                                ref error))
                        {
                            TraceOps.ShowMessage(TracePriority.Highest,
                                debugCallback, traceCallback, thisAssembly,
                                error, traceCategory, MessageBoxButtons.OK,
                                MessageBoxIcon.Error);

                            TraceOps.DebugAndTrace(TracePriority.MediumHigh,
                                debugCallback, traceCallback, "Failure.",
                                traceCategory);

                            return 1; /* FAILURE */
                        }
                    }
                    #endregion

                    ///////////////////////////////////////////////////////////

                    #region VS DataProvider
                    if (configuration.HasFlags(
                            InstallFlags.VsDataProvider, true))
                    {
                        if (!ForEachVsVersionRegistry(registry,
                                vsList, ProcessVsDataProvider,
                                configuration.VsVersionSuffix, package,
                                fileNameData, configuration.PerUser,
                                VsIs32BitOnly || configuration.Wow64,
                                configuration.ThrowOnMissing,
                                configuration.WhatIf, configuration.Verbose,
                                ref error))
                        {
                            TraceOps.ShowMessage(TracePriority.Highest,
                                debugCallback, traceCallback, thisAssembly,
                                error, traceCategory, MessageBoxButtons.OK,
                                MessageBoxIcon.Error);

                            TraceOps.DebugAndTrace(TracePriority.MediumHigh,
                                debugCallback, traceCallback, "Failure.",
                                traceCategory);

                            return 1; /* FAILURE */
                        }
                    }
                    #endregion

                    ///////////////////////////////////////////////////////////

                    #region VS DevEnv Setup
                    if (configuration.HasFlags(
                            InstallFlags.VsDevEnvSetup, true))
                    {
                        if (!ForEachVsVersionRegistry(registry,
                                vsList, ProcessVsDevEnvSetup,
                                configuration.VsVersionSuffix, package,
                                fileNameData, configuration.PerUser,
                                VsIs32BitOnly || configuration.Wow64,
                                configuration.ThrowOnMissing,
                                configuration.WhatIf, configuration.Verbose,
                                ref error))
                        {
                            TraceOps.ShowMessage(TracePriority.Highest,
                                debugCallback, traceCallback, thisAssembly,
                                error, traceCategory, MessageBoxButtons.OK,
                                MessageBoxIcon.Error);

                            TraceOps.DebugAndTrace(TracePriority.MediumHigh,
                                debugCallback, traceCallback, "Failure.",
                                traceCategory);

                            return 1; /* FAILURE */
                        }
                    }
                    #endregion

                    ///////////////////////////////////////////////////////////

                    #region Log Summary
                    TraceOps.DebugAndTrace(TracePriority.MediumHigh,
                        debugCallback, traceCallback, String.Format(
                        "subKeysCreated = {0}, subKeysDeleted = {1}, " +
                        "keyValuesSet = {2}, keyValuesDeleted = {3}",
                        ForDisplay(RegistryHelper.SubKeysCreated),
                        ForDisplay(RegistryHelper.SubKeysDeleted),
                        ForDisplay(RegistryHelper.KeyValuesSet),
                        ForDisplay(RegistryHelper.KeyValuesDeleted)),
                        traceCategory);

                    TraceOps.DebugAndTrace(TracePriority.MediumHigh,
                        debugCallback, traceCallback, String.Format(
                        "filesCreated = {0}, filesModified = {1}, " +
                        "filesDeleted = {2}", ForDisplay(filesCreated),
                        ForDisplay(filesModified), ForDisplay(filesDeleted)),
                        traceCategory);
                    #endregion

                    ///////////////////////////////////////////////////////////

                    TraceOps.DebugAndTrace(TracePriority.MediumHigh,
                        debugCallback, traceCallback, "Success.",
                        traceCategory);

                    return 0; /* SUCCESS */
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugAndTrace(TracePriority.Highest,
                    debugCallback, traceCallback, e, traceCategory);

                throw;
            }
        }
        #endregion
    }
    #endregion
}
