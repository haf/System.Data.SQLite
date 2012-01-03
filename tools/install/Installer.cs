/*
 * Installer.cs --
 *
 * Written by Joe Mistachkin.
 * Released to the public domain, use at your own risk!
 */

using System.Collections.Generic;
using System.Diagnostics;
using System.EnterpriseServices.Internal;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using Microsoft.Win32;

namespace System.Data.SQLite
{
    #region Public Delegates
    internal delegate void TraceCallback(
        string message,
        string category
    );

    ///////////////////////////////////////////////////////////////////////////

    internal delegate bool FrameworkConfigCallback(
        string fileName,
        string invariant,
        string name,
        string description,
        string typeName,
        AssemblyName assemblyName,
        object clientData,
        bool whatIf,
        bool verbose,
        ref bool saved,
        ref string error
    );

    ///////////////////////////////////////////////////////////////////////////

    internal delegate bool FrameworkRegistryCallback(
        RegistryKey rootKey,
        string frameworkName,
        Version frameworkVersion,
        string platformName,
        object clientData,
        bool whatIf,
        bool verbose,
        ref string error
    );

    ///////////////////////////////////////////////////////////////////////////

    internal delegate bool VisualStudioRegistryCallback(
        RegistryKey rootKey,
        Version vsVersion,
        Installer.Package package,
        object clientData,
        bool whatIf,
        bool verbose,
        ref string error
    );
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Public Enumerations
    [Flags()]
    public enum InstallFlags
    {
        None = 0x0,
        GAC = 0x1,
        AssemblyFolders = 0x2,
        DbProviderFactory = 0x4,
        VsPackage = 0x8,
        VsDataSource = 0x10,
        VsDataProvider = 0x20,
        Framework = GAC | AssemblyFolders | DbProviderFactory,
        Vs = VsPackage | VsDataSource | VsDataProvider,
        All = Framework | Vs,
        AllNoGAC = All & ~GAC,
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
        Medium = 0x8,
        High = 0x10,
        Higher = 0x20,
        Highest = 0x40,
        Default = Medium
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Installer Class
    internal static class Installer
    {
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
            private const string DefaultTraceFormat = "#{0} @ {1}: {2}";

            private const string Iso8601DateTimeOutputFormat =
                "yyyy.MM.ddTHH:mm:ss.fffffff";
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Static Data
            private static object syncRoot = new object();
            private static long nextId;
            private static TracePriority tracePriority = TracePriority.Default;
            private static string traceFormat = DefaultTraceFormat;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Static Properties
            public static TracePriority TracePriority
            {
                get { lock (syncRoot) { return tracePriority; } }
                set { lock (syncRoot) { tracePriority = value; } }
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

            ///////////////////////////////////////////////////////////////////

            public static DialogResult ShowMessage(
                TracePriority tracePriority,
                TraceCallback traceCallback,
                Assembly assembly,
                string message,
                string category,
                MessageBoxButtons buttons,
                MessageBoxIcon icon
                )
            {
                DialogResult result = DialogResult.OK;

                Trace(tracePriority, traceCallback, message, category);

                if (SystemInformation.UserInteractive)
                {
                    string title = GetAssemblyTitle(assembly);

                    if (title == null)
                        title = Application.ProductName;

                    result = MessageBox.Show(message, title, buttons, icon);

                    Trace(tracePriority, traceCallback, String.Format(
                        "User choice of {0}.", ForDisplay(result)),
                        category);

                    return result;
                }

                Trace(tracePriority, traceCallback, String.Format(
                    "Default choice of {0}.", ForDisplay(result)),
                    category);

                return result;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Tracing Support Methods
            public static long NextId()
            {
                return Interlocked.Increment(ref nextId);
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

            public static void TraceCore(
                string message,
                string category
                )
            {
                lock (syncRoot)
                {
                    System.Diagnostics.Trace.WriteLine(message, category);
                    System.Diagnostics.Trace.Flush();
                }
            }

            ///////////////////////////////////////////////////////////////////

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static string Trace(
                TracePriority tracePriority,
                TraceCallback traceCallback,
                Exception exception,
                string category
                )
            {
                if (exception != null)
                    return Trace(tracePriority, traceCallback,
                        new StackTrace(exception, true), 0,
                        exception.ToString(), category);

                return null;
            }

            ///////////////////////////////////////////////////////////////////

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static string Trace(
                TracePriority tracePriority,
                TraceCallback traceCallback,
                string message,
                string category
                )
            {
                return Trace(
                    tracePriority, traceCallback, null, 1, message, category);
            }

            ///////////////////////////////////////////////////////////////////

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static string Trace(
                TracePriority tracePriority,
                TraceCallback traceCallback,
                StackTrace stackTrace,
                int level,
                string message,
                string category
                )
            {
                //
                // NOTE: If the priority of this message is less than what
                //       we currently care about, just return now.
                //
                if (tracePriority < TracePriority)
                    return message;

                //
                // NOTE: Always skip this call frame if the stack trace is
                //       going to be captured by GetMethodName.
                //
                if (stackTrace == null)
                    level++;

                if (traceCallback == null)
                    traceCallback = TraceCore;

                traceCallback(String.Format("{0}: {1}",
                    GetMethodName(stackTrace, level), message), category);

                return message;
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region MockRegistryKey Class
        private sealed class MockRegistryKey : IDisposable
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
                    //       itself since no writes are allowed in 'what-if'
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
                string subKeyName
                )
            {
                CheckDisposed();
                CheckReadOnly();

                if (key == null)
                    return;

                if (!whatIf)
                    key.DeleteSubKey(subKeyName);
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
                string name
                )
            {
                CheckDisposed();
                CheckReadOnly();

                if (key == null)
                    return;

                if (!whatIf)
                    key.DeleteValue(name);
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
            // BUGBUG: The 'what-if' mode setting here should probably be based
            //         on some static property, not hard-coded to true?
            //
            public static implicit operator MockRegistryKey(
                RegistryKey key
                )
            {
                return new MockRegistryKey(key, null, true, false, false);
            }

            ///////////////////////////////////////////////////////////////////

            //
            // BUGBUG: Remove me?  This should be safe because in 'what-if'
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

                throw new ObjectDisposedException(typeof(MockRegistryKey).Name);
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
            public static RegistryKey GetRootKeyByName(
                string keyName
                )
            {
                if (String.IsNullOrEmpty(keyName))
                    return null;

                switch (keyName.ToUpperInvariant())
                {
                    case "HKCR":
                    case "HKEY_CLASSES_ROOT":
                        return Registry.ClassesRoot;
                    case "HKCC":
                    case "HKEY_CURRENT_CONFIG":
                        return Registry.CurrentConfig;
                    case "HKCU":
                    case "HKEY_CURRENT_USER":
                        return Registry.CurrentUser;
                    case "HKDD":
                    case "HKEY_DYN_DATA":
                        return Registry.DynData;
                    case "HKLM":
                    case "HKEY_LOCAL_MACHINE":
                        return Registry.LocalMachine;
                    case "HKPD":
                    case "HKEY_PERFORMANCE_DATA":
                        return Registry.PerformanceData;
                    case "HKU":
                    case "HKEY_USERS":
                        return Registry.Users;
                }

                return null;
            }

            ///////////////////////////////////////////////////////////////////

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
                    TraceOps.Trace(
                        writable ? TracePriority.Highest : TracePriority.Higher,
                        traceCallback, String.Format("rootKey = {0}, " +
                        "subKeyName = {1}, writable = {2}", ForDisplay(rootKey),
                        ForDisplay(subKeyName), ForDisplay(writable)),
                        traceCategory);

                //
                // HACK: Always forbid writable access when operating in
                //       'what-if' mode.
                //
                MockRegistryKey key = rootKey.OpenSubKey(
                    subKeyName, whatIf ? false : writable);

                return (key != null) ?
                    new MockRegistryKey(key, whatIf, false, false) : null;
            }

            ///////////////////////////////////////////////////////////////////

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
                    TraceOps.Trace(
                        TracePriority.Highest, traceCallback, String.Format(
                        "rootKey = {0}, subKeyName = {1}", ForDisplay(rootKey),
                        ForDisplay(subKeyName)), traceCategory);

                try
                {
                    //
                    // HACK: Always open a key, rather than creating one when
                    //       operating in 'what-if' mode.
                    //
                    if (whatIf)
                    {
                        //
                        // HACK: Attempt to open the specified sub-key.  If
                        //       this fails, we will simply return the root key
                        //       itself since no writes are allowed in
                        //       'what-if' mode anyhow.
                        //
                        MockRegistryKey key = rootKey.OpenSubKey(subKeyName);

                        return (key != null) ?
                            key : new MockRegistryKey(
                                rootKey, subKeyName, true, false, false);
                    }
                    else
                    {
                        return new MockRegistryKey(
                            rootKey.CreateSubKey(subKeyName), false, false,
                            false);
                    }
                }
                finally
                {
                    subKeysCreated++;
                }
            }

            ///////////////////////////////////////////////////////////////////

            public static void DeleteSubKey(
                MockRegistryKey rootKey,
                string subKeyName,
                bool whatIf,
                bool verbose
                )
            {
                if (rootKey == null)
                    return;

                if (verbose)
                    TraceOps.Trace(
                        TracePriority.Highest, traceCallback, String.Format(
                        "rootKey = {0}, subKeyName = {1}", ForDisplay(rootKey),
                        ForDisplay(subKeyName)), traceCategory);

                if (!whatIf)
                    rootKey.DeleteSubKey(subKeyName);

                subKeysDeleted++;
            }

            ///////////////////////////////////////////////////////////////////

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
                    TraceOps.Trace(
                        TracePriority.Highest, traceCallback, String.Format(
                        "rootKey = {0}, subKeyName = {1}", ForDisplay(rootKey),
                        ForDisplay(subKeyName)), traceCategory);

                if (!whatIf)
                    rootKey.DeleteSubKeyTree(subKeyName);

                subKeysDeleted++;
            }

            ///////////////////////////////////////////////////////////////////

            public static string[] GetSubKeyNames(
                MockRegistryKey key,
                bool whatIf,
                bool verbose
                )
            {
                if (key == null)
                    return null;

                if (verbose)
                    TraceOps.Trace(
                        TracePriority.High, traceCallback, String.Format(
                        "key = {0}", ForDisplay(key)), traceCategory);

                return key.GetSubKeyNames();
            }

            ///////////////////////////////////////////////////////////////////

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
                    TraceOps.Trace(
                        TracePriority.High, traceCallback, String.Format(
                        "key = {0}, name = {1}, defaultValue = {2}",
                        ForDisplay(key), ForDisplay(name),
                        ForDisplay(defaultValue)), traceCategory);

                return key.GetValue(name, defaultValue);
            }

            ///////////////////////////////////////////////////////////////////

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
                    TraceOps.Trace(
                        TracePriority.Highest, traceCallback, String.Format(
                        "key = {0}, name = {1}, value = {2}", ForDisplay(key),
                        ForDisplay(name), ForDisplay(value)), traceCategory);

                if (!whatIf)
                    key.SetValue(name, value);

                keyValuesSet++;
            }

            ///////////////////////////////////////////////////////////////////

            public static void DeleteValue(
                MockRegistryKey key,
                string name,
                bool whatIf,
                bool verbose
                )
            {
                if (key == null)
                    return;

                if (verbose)
                    TraceOps.Trace(
                        TracePriority.Highest, traceCallback, String.Format(
                        "key = {0}, name = {1}", ForDisplay(key),
                        ForDisplay(name)), traceCategory);

                if (!whatIf)
                    key.DeleteValue(name);

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

        #region VersionListMap Class
        private sealed class VersionListMap : Dictionary<Version, VersionList>
        {
            public VersionListMap()
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
                string designerFileName,
                string traceFormat,
                InstallFlags installFlags,
                TracePriority tracePriority,
                bool install,
                bool noDesktop,
                bool noCompact,
                bool noNetFx20,
                bool noNetFx40,
                bool noVs2008,
                bool noVs2010,
                bool noTrace,
                bool noConsole,
                bool noLog,
                bool whatIf,
                bool verbose,
                bool confirm
                )
            {
                this.assembly = assembly;
                this.logFileName = logFileName;
                this.directory = directory;
                this.coreFileName = coreFileName;
                this.linqFileName = linqFileName;
                this.designerFileName = designerFileName;
                this.traceFormat = traceFormat;
                this.installFlags = installFlags;
                this.tracePriority = tracePriority;
                this.install = install;
                this.noDesktop = noDesktop;
                this.noCompact = noCompact;
                this.noNetFx20 = noNetFx20;
                this.noNetFx40 = noNetFx40;
                this.noVs2008 = noVs2008;
                this.noVs2010 = noVs2010;
                this.noTrace = noTrace;
                this.noConsole = noConsole;
                this.noLog = noLog;
                this.whatIf = whatIf;
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
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Static Methods
            public static Configuration CreateDefault()
            {
                string directory = null;
                string coreFileName = null;
                string linqFileName = null;
                string designerFileName = null;

                GetDefaultFileNames(
                    ref directory, ref coreFileName, ref linqFileName,
                    ref designerFileName);

                return new Configuration(thisAssembly, null, directory,
                    coreFileName, linqFileName, designerFileName,
                    TraceOps.TraceFormat, InstallFlags.Default,
                    TracePriority.Default, true, false, true, false, false,
                    false, false, false, false, false, true, true, false);
            }

            ///////////////////////////////////////////////////////////////////

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

                        if (String.IsNullOrEmpty(arg))
                            continue;

                        string newArg = arg;

                        if (CheckOption(ref newArg))
                        {
                            //
                            // NOTE: All the supported command line options must
                            //       have a value; therefore, attempt to advance
                            //       to it now.  If we fail, we are done.
                            //
                            index++;

                            if (index >= length)
                            {
                                error = TraceOps.Trace(
                                    TracePriority.Lowest,
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
                            if (MatchOption(newArg, "strict"))
                            {
                                bool? value = ParseBoolean(text);

                                if (value == null)
                                {
                                    error = TraceOps.Trace(
                                        TracePriority.Lowest,
                                        traceCallback, String.Format(
                                        "Invalid {0} boolean value: {1}",
                                        ForDisplay(arg), ForDisplay(text)),
                                        traceCategory);

                                    if (strict)
                                        return false;

                                    continue;
                                }

                                //
                                // NOTE: Allow the command line arguments to override
                                //       the "strictness" setting provided by our caller.
                                //
                                strict = (bool)value;
                            }
                            else if (MatchOption(newArg, "logFileName"))
                            {
                                configuration.logFileName = text;
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

                                string designerFileName = configuration.designerFileName;

                                if (!String.IsNullOrEmpty(designerFileName))
                                    designerFileName = Path.GetFileName(designerFileName);

                                if (String.IsNullOrEmpty(designerFileName))
                                    designerFileName = Installer.DesignerFileName;

                                configuration.designerFileName = Path.Combine(
                                    configuration.directory, designerFileName);
                            }
                            else if (MatchOption(newArg, "coreFileName"))
                            {
                                configuration.coreFileName = text;
                            }
                            else if (MatchOption(newArg, "linqFileName"))
                            {
                                configuration.linqFileName = text;
                            }
                            else if (MatchOption(newArg, "designerFileName"))
                            {
                                configuration.designerFileName = text;
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
                                    error = TraceOps.Trace(
                                        TracePriority.Lowest,
                                        traceCallback, String.Format(
                                        "Invalid trace priority value: {0}",
                                        ForDisplay(text)), traceCategory);

                                    if (strict)
                                        return false;

                                    continue;
                                }

                                configuration.tracePriority = (TracePriority)value;
                                TraceOps.TracePriority = configuration.tracePriority;
                            }
                            else if (MatchOption(newArg, "install"))
                            {
                                bool? value = ParseBoolean(text);

                                if (value == null)
                                {
                                    error = TraceOps.Trace(
                                        TracePriority.Lowest,
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
                                    error = TraceOps.Trace(
                                        TracePriority.Lowest,
                                        traceCallback, String.Format(
                                        "Invalid install flags value: {0}",
                                        ForDisplay(text)), traceCategory);

                                    if (strict)
                                        return false;

                                    continue;
                                }

                                configuration.installFlags = (InstallFlags)value;
                            }
                            else if (MatchOption(newArg, "whatIf"))
                            {
                                bool? value = ParseBoolean(text);

                                if (value == null)
                                {
                                    error = TraceOps.Trace(
                                        TracePriority.Lowest,
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
                            else if (MatchOption(newArg, "verbose"))
                            {
                                bool? value = ParseBoolean(text);

                                if (value == null)
                                {
                                    error = TraceOps.Trace(
                                        TracePriority.Lowest,
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
                            else if (MatchOption(newArg, "confirm"))
                            {
                                bool? value = ParseBoolean(text);

                                if (value == null)
                                {
                                    error = TraceOps.Trace(
                                        TracePriority.Lowest,
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
                            else if (MatchOption(newArg, "noDesktop"))
                            {
                                bool? value = ParseBoolean(text);

                                if (value == null)
                                {
                                    error = TraceOps.Trace(
                                        TracePriority.Lowest,
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
                            else if (MatchOption(newArg, "noCompact"))
                            {
                                bool? value = ParseBoolean(text);

                                if (value == null)
                                {
                                    error = TraceOps.Trace(
                                        TracePriority.Lowest,
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
                            else if (MatchOption(newArg, "noNetFx20"))
                            {
                                bool? value = ParseBoolean(text);

                                if (value == null)
                                {
                                    error = TraceOps.Trace(
                                        TracePriority.Lowest,
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
                            else if (MatchOption(newArg, "noNetFx40"))
                            {
                                bool? value = ParseBoolean(text);

                                if (value == null)
                                {
                                    error = TraceOps.Trace(
                                        TracePriority.Lowest,
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
                            else if (MatchOption(newArg, "noVs2008"))
                            {
                                bool? value = ParseBoolean(text);

                                if (value == null)
                                {
                                    error = TraceOps.Trace(
                                        TracePriority.Lowest,
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
                                    error = TraceOps.Trace(
                                        TracePriority.Lowest,
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
                            else if (MatchOption(newArg, "noTrace"))
                            {
                                bool? value = ParseBoolean(text);

                                if (value == null)
                                {
                                    error = TraceOps.Trace(
                                        TracePriority.Lowest,
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
                            else if (MatchOption(newArg, "noConsole"))
                            {
                                bool? value = ParseBoolean(text);

                                if (value == null)
                                {
                                    error = TraceOps.Trace(
                                        TracePriority.Lowest,
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
                            else if (MatchOption(newArg, "noLog"))
                            {
                                bool? value = ParseBoolean(text);

                                if (value == null)
                                {
                                    error = TraceOps.Trace(
                                        TracePriority.Lowest,
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
                            else
                            {
                                error = TraceOps.Trace(
                                    TracePriority.Lowest,
                                    traceCallback, String.Format(
                                    "Unsupported command line option: {0}",
                                    ForDisplay(arg)), traceCategory);

                                if (strict)
                                    return false;
                            }
                        }
                        else
                        {
                            error = TraceOps.Trace(
                                TracePriority.Lowest,
                                traceCallback, String.Format(
                                "Unsupported command line argument: {0}",
                                ForDisplay(arg)), traceCategory);

                            if (strict)
                                return false;
                        }
                    }

                    return true;
                }
                catch (Exception e)
                {
                    TraceOps.Trace(
                        TracePriority.Highest, traceCallback, e,
                        traceCategory);

                    error = "Failed to modify configuration.";
                }

                return false;
            }

            ///////////////////////////////////////////////////////////////////

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
                            configuration.logFileName = GetLogFileName();
                        }

                        ///////////////////////////////////////////////////////

                        if (!configuration.noConsole)
                        {
                            Trace.Listeners.Add(new ConsoleTraceListener());
                        }

                        if (!configuration.noLog &&
                            !String.IsNullOrEmpty(configuration.logFileName))
                        {
                            Trace.Listeners.Add(new TextWriterTraceListener(
                                configuration.logFileName));
                        }
                    }

                    //
                    // NOTE: Dump the configuration now in case we need to
                    //       troubleshoot any issues.
                    //
                    if (configuration.tracePriority <= TracePriority.Medium)
                        configuration.Dump();

                    //
                    // NOTE: Show where we are running from and how we were
                    //       invoked.
                    //
                    string location = assembly.Location;

                    TraceOps.Trace(
                        TracePriority.Medium, traceCallback, String.Format(
                        "Running executable is: {0}", ForDisplay(location)),
                        traceCategory);

                    TraceOps.Trace(
                        TracePriority.Medium, traceCallback, String.Format(
                        "Original command line is: {0}",
                        Environment.CommandLine), traceCategory);

                    if (!configuration.whatIf)
                    {
                        //
                        // NOTE: If the debugger is attached and What-If mode
                        //       is [now] disabled, issue a warning.
                        //
                        if (Debugger.IsAttached)
                            TraceOps.Trace(
                                TracePriority.Medium, traceCallback,
                                "Forced to disable \"what-if\" mode with " +
                                "debugger attached.", traceCategory);
                    }
                    else
                    {
                        TraceOps.Trace(
                            TracePriority.Higher, traceCallback,
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
                    TraceOps.Trace(
                        TracePriority.Highest, traceCallback, e,
                        traceCategory);

                    error = "Failed to process configuration.";
                }

                return false;
            }

            ///////////////////////////////////////////////////////////////////

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
                            coreImageRuntimeVersion, CLR2ImageRuntimeVersion,
                            StringComparison.InvariantCulture))
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
                        configuration.noVs2010 = true;

                        TraceOps.Trace(
                            TracePriority.Medium, traceCallback, String.Format(
                            "Assembly is compiled for the .NET Framework {0}, " +
                            "support for .NET Framework {1} is now disabled.",
                            CLR2ImageRuntimeVersion, CLR4ImageRuntimeVersion),
                            traceCategory);
                    }
                    else if (String.Equals(
                            coreImageRuntimeVersion, CLR4ImageRuntimeVersion,
                            StringComparison.InvariantCulture))
                    {
                        //
                        // NOTE: For the CLR v4.0 runtime, make sure we disable
                        //       any attempt to use it for things that require
                        //       an assembly compiled for the CLR v2.0.
                        //
                        configuration.noNetFx20 = true;
                        configuration.noVs2008 = true;

                        TraceOps.Trace(
                            TracePriority.Medium, traceCallback, String.Format(
                            "Assembly is compiled for the .NET Framework {0}, " +
                            "support for .NET Framework {1} is now disabled.",
                            ForDisplay(CLR4ImageRuntimeVersion),
                            ForDisplay(CLR2ImageRuntimeVersion)),
                            traceCategory);
                    }
                    else
                    {
                        error = String.Format(
                            "unsupported core file image runtime version " +
                            "{0}, must be {1} or {2}",
                            ForDisplay(coreImageRuntimeVersion),
                            ForDisplay(CLR2ImageRuntimeVersion),
                            ForDisplay(CLR4ImageRuntimeVersion));

                        return false;
                    }

                    return true;
                }
                catch (Exception e)
                {
                    TraceOps.Trace(
                        TracePriority.Highest, traceCallback, e,
                        traceCategory);

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

            public void Dump()
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
                        "DesignerFileName", ForDisplay(designerFileName)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "TraceFormat", ForDisplay(traceFormat)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "InstallFlags", ForDisplay(installFlags)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "TracePriority", ForDisplay(tracePriority)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "Install", ForDisplay(install)),
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
                        "NoNetFx40", ForDisplay(noNetFx40)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "NoVs2008", ForDisplay(noVs2008)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "NoVs2010", ForDisplay(noVs2010)),
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
                        "WhatIf", ForDisplay(whatIf)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "Verbose", ForDisplay(verbose)),
                        traceCategory);

                    traceCallback(String.Format(NameAndValueFormat,
                        "Confirm", ForDisplay(confirm)),
                        traceCategory);
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

            private string designerFileName;
            public string DesignerFileName
            {
                get { return designerFileName; }
                set { designerFileName = value; }
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

            private TracePriority tracePriority;
            public TracePriority TracePriority
            {
                get { return tracePriority; }
                set { tracePriority = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool install;
            public bool Install
            {
                get { return install; }
                set { install = value; }
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

            private bool noNetFx40;
            public bool NoNetFx40
            {
                get { return noNetFx40; }
                set { noNetFx40 = value; }
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

            private bool whatIf;
            public bool WhatIf
            {
                get { return whatIf; }
                set { whatIf = value; }
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
            private RegistryKey rootKey;
            public RegistryKey RootKey
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
            private RegistryKey rootKey;
            public RegistryKey RootKey
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

            ///////////////////////////////////////////////////////////////////

            private VersionListMap frameworkVersions;
            public VersionListMap FrameworkVersions
            {
                get { return frameworkVersions; }
                set { frameworkVersions = value; }
            }

            ///////////////////////////////////////////////////////////////////

            private VersionList installedVersions;
            public VersionList InstalledVersions
            {
                get { return installedVersions; }
                set { installedVersions = value; }
            }
            #endregion
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constant Data
        private const string CoreFileName = "System.Data.SQLite.dll";
        private const string LinqFileName = "System.Data.SQLite.Linq.dll";
        private const string DesignerFileName = "SQLite.Designer.dll";
        private const string ProviderName = "SQLite Data Provider";
        private const string ProjectName = "System.Data.SQLite";
        private const string LegacyProjectName = "SQLite";
        private const string InvariantName = "System.Data.SQLite";
        private const string FactoryTypeName = "System.Data.SQLite.SQLiteFactory";
        private const string Description = ".NET Framework Data Provider for SQLite";

        ///////////////////////////////////////////////////////////////////////

        private const string CLR2ImageRuntimeVersion = "v2.0.50727";
        private const string CLR4ImageRuntimeVersion = "v4.0.30319";

        ///////////////////////////////////////////////////////////////////////

        private const string NameAndValueFormat = "{0}: {1}";
        private const string LogFileSuffix = ".log";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string VsIdFormat = "B";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string FrameworkKeyName =
            "Software\\Microsoft\\.NETFramework";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string FrameworkSdkKeyName =
            "Software\\Microsoft\\Microsoft SDKs\\.NETFramework";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string WindowsSdkKeyName =
            "Software\\Microsoft\\Microsoft SDKs\\Windows";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string XPathForAddElement =
            "configuration/system.data/DbProviderFactories/add[@invariant=\"{0}\"]";

        private static readonly string XPathForRemoveElement =
            "configuration/system.data/DbProviderFactories/remove[@invariant=\"{0}\"]";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private static Assembly thisAssembly = Assembly.GetExecutingAssembly();

        private static string traceCategory = Path.GetFileName(
            thisAssembly.Location);

        private static TraceCallback traceCallback = AppTrace;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trace Handling
        private static string GetLogFileName()
        {
            string result = Path.GetTempFileName(); /* throw */

            File.Move(result, result + LogFileSuffix); /* throw */
            result += LogFileSuffix;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void AppTrace(
            string message,
            string category
            )
        {
            TraceOps.TraceCore(String.Format(
                TraceOps.TraceFormat, TraceOps.NextId(),
                TraceOps.TimeStamp(DateTime.UtcNow), message), category);
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

        #region .NET Framework Handling
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
            RegistryKey rootKey,
            Version frameworkVersion,
            bool whatIf,
            bool verbose
            )
        {
            using (MockRegistryKey key = RegistryHelper.OpenSubKey(
                    rootKey, FrameworkKeyName, false, whatIf, verbose))
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

        ///////////////////////////////////////////////////////////////////////

        private static string GetSdkBinaryFileName(
            RegistryKey rootKey,
            string fileName,
            bool whatIf,
            bool verbose
            )
        {
            StringDictionary results = new StringDictionary();

            string[] keyNames = {
                FrameworkKeyName,
                FrameworkSdkKeyName,
                WindowsSdkKeyName
            };

            string[] valueNames = {
                "sdkInstallRootv2.0",
                "InstallationFolder",
                "InstallationFolder"
            };

            bool[] useSubKeys = {
                false,
                true,
                true
            };

            for (int index = 0; index < keyNames.Length; index++)
            {
                using (MockRegistryKey key = RegistryHelper.OpenSubKey(
                        rootKey, keyNames[index], false, whatIf, verbose))
                {
                    if (key == null)
                        continue;

                    if (useSubKeys[index])
                    {
                        foreach (string subKeyName in RegistryHelper.GetSubKeyNames(
                                key, whatIf, verbose))
                        {
                            using (MockRegistryKey subKey = RegistryHelper.OpenSubKey(
                                    key, subKeyName, false, whatIf, verbose))
                            {
                                if (subKey == null)
                                    continue;

                                object value = RegistryHelper.GetValue(
                                    subKey, valueNames[index], null, whatIf,
                                    verbose);

                                if (!(value is string))
                                    continue;

                                string path = (string)value;

                                if (!Directory.Exists(path))
                                    continue;

                                path = Path.Combine(path, "bin");

                                if (!Directory.Exists(path))
                                    continue;

                                if (String.IsNullOrEmpty(fileName))
                                {
                                    results.Add(subKey.Name, path);
                                    continue;
                                }

                                path = Path.Combine(path, fileName);

                                if (File.Exists(path))
                                    results.Add(subKey.Name, path);
                            }
                        }
                    }
                    else
                    {
                        object value = RegistryHelper.GetValue(
                            key, valueNames[index], null, whatIf, verbose);

                        if (!(value is string))
                            continue;

                        string path = (string)value;

                        if (!Directory.Exists(path))
                            continue;

                        path = Path.Combine(path, "bin");

                        if (!Directory.Exists(path))
                            continue;

                        if (String.IsNullOrEmpty(fileName))
                        {
                            results.Add(key.Name, path);
                            continue;
                        }

                        path = Path.Combine(path, fileName);

                        if (File.Exists(path))
                            results.Add(key.Name, path);
                    }
                }
            }

            //
            // NOTE: If we found some results, return the last (latest) one.
            //
            if (results.Count > 0)
                return results[new StringList(results.Keys)[results.Count - 1]];

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Per-Framework/Platform Handling
        private static void InitializeFrameworkList(
            Configuration configuration,
            ref FrameworkList frameworkList
            )
        {
            if (frameworkList == null)
                frameworkList = new FrameworkList();

            if (frameworkList.RootKey == null)
                frameworkList.RootKey = Registry.LocalMachine;

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

                    if ((configuration == null) || !configuration.NoNetFx40)
                        desktopVersionList.Add(new Version(4, 0, 30319));

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

        private static bool HaveFramework(
            RegistryKey rootKey,
            string frameworkName,
            Version frameworkVersion,
            string platformName,
            bool whatIf,
            bool verbose
            )
        {
            string format = !String.IsNullOrEmpty(platformName) ?
                "Software\\Microsoft\\{0}\\v{1}\\{2}" :
                "Software\\Microsoft\\{0}\\v{1}";

            string keyName = String.Format(
                format, frameworkName, frameworkVersion, platformName);

            using (MockRegistryKey key = RegistryHelper.OpenSubKey(
                    rootKey, keyName, false, whatIf, verbose))
            {
                if (key == null)
                    return false;

                if (platformName != null) // NOTE: Skip non-desktop.
                    return true;

                string directory = GetFrameworkDirectory(
                    rootKey, frameworkVersion, whatIf, verbose);

                if (String.IsNullOrEmpty(directory))
                    return false;

                if (!Directory.Exists(directory))
                    return false;

                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ForEachFrameworkConfig(
            FrameworkList frameworkList,
            FrameworkConfigCallback callback,
            string invariant,
            string name,
            string description,
            string typeName,
            AssemblyName assemblyName,
            object clientData,
            bool whatIf,
            bool verbose,
            ref bool saved,
            ref string error
            )
        {
            if (frameworkList == null)
            {
                error = "invalid framework list";
                return false;
            }

            RegistryKey rootKey = frameworkList.RootKey;

            if (rootKey == null)
            {
                error = "invalid root key";
                return false;
            }

            if (!Object.ReferenceEquals(rootKey, Registry.CurrentUser) &&
                !Object.ReferenceEquals(rootKey, Registry.LocalMachine))
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

                if (!frameworkList.Versions.TryGetValue(
                        frameworkName, out frameworkVersionList) ||
                    (frameworkVersionList == null))
                {
                    continue;
                }

                foreach (Version frameworkVersion in frameworkVersionList)
                {
                    TraceOps.Trace(
                        TracePriority.Lower, traceCallback, String.Format(
                        "frameworkName = {0}, frameworkVersion = {1}, " +
                        "platformName = {2}", ForDisplay(frameworkName),
                        ForDisplay(frameworkVersion),
                        ForDisplay(platformName)), traceCategory);

                    if (!HaveFramework(
                            rootKey, frameworkName, frameworkVersion,
                            platformName, whatIf, verbose))
                    {
                        TraceOps.Trace(
                            TracePriority.Low, traceCallback,
                            ".NET Framework not found, skipping...",
                            traceCategory);

                        continue;
                    }

                    if (callback == null)
                        continue;

                    string directory = GetFrameworkDirectory(
                        rootKey, frameworkVersion, whatIf, verbose);

                    if (String.IsNullOrEmpty(directory))
                    {
                        TraceOps.Trace(
                            TracePriority.Low, traceCallback, String.Format(
                            ".NET Framework {0} directory is invalid, " +
                            "skipping...", ForDisplay(frameworkVersion)),
                            traceCategory);

                        continue;
                    }

                    directory = Path.Combine(directory, "Config");

                    if (!Directory.Exists(directory))
                    {
                        TraceOps.Trace(
                            TracePriority.Low, traceCallback, String.Format(
                            ".NET Framework {0} directory {1} does not exist, " +
                            "skipping...", ForDisplay(frameworkVersion),
                            ForDisplay(directory)), traceCategory);

                        continue;
                    }

                    string fileName = Path.Combine(directory, "machine.config");

                    if (!File.Exists(fileName))
                    {
                        TraceOps.Trace(
                            TracePriority.Low, traceCallback, String.Format(
                            ".NET Framework {0} file {1} does not exist, " +
                            "skipping...", ForDisplay(frameworkVersion),
                            ForDisplay(fileName)), traceCategory);

                        continue;
                    }

                    bool localSaved = false;

                    if (!callback(
                            fileName, invariant, name, description, typeName,
                            assemblyName, clientData, whatIf, verbose,
                            ref localSaved, ref error))
                    {
                        return false;
                    }
                    else
                    {
                        if (localSaved && !saved)
                            saved = true;

                        if (verbose)
                            TraceOps.Trace(
                                TracePriority.Lowest,
                                traceCallback, String.Format(
                                "localSaved = {0}, saved = {1}",
                                ForDisplay(localSaved), ForDisplay(saved)),
                                traceCategory);
                    }
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ForEachFrameworkRegistry(
            FrameworkList frameworkList,
            FrameworkRegistryCallback callback,
            object clientData,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            if (frameworkList == null)
            {
                error = "invalid framework list";
                return false;
            }

            RegistryKey rootKey = frameworkList.RootKey;

            if (rootKey == null)
            {
                error = "invalid root key";
                return false;
            }

            if (!Object.ReferenceEquals(rootKey, Registry.CurrentUser) &&
                !Object.ReferenceEquals(rootKey, Registry.LocalMachine))
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

                if (!frameworkList.Versions.TryGetValue(
                        frameworkName, out frameworkVersionList) ||
                    (frameworkVersionList == null))
                {
                    continue;
                }

                foreach (Version frameworkVersion in frameworkVersionList)
                {
                    TraceOps.Trace(
                        TracePriority.Lower, traceCallback, String.Format(
                        "frameworkName = {0}, frameworkVersion = {1}, " +
                        "platformName = {2}", ForDisplay(frameworkName),
                        ForDisplay(frameworkVersion),
                        ForDisplay(platformName)), traceCategory);

                    if (!HaveFramework(
                            rootKey, frameworkName, frameworkVersion,
                            platformName, whatIf, verbose))
                    {
                        TraceOps.Trace(TracePriority.Low, traceCallback,
                            ".NET Framework not found, skipping...",
                            traceCategory);

                        continue;
                    }

                    if (callback == null)
                        continue;

                    if (!callback(
                            rootKey, frameworkName, frameworkVersion,
                            platformName, clientData, whatIf, verbose,
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
            Configuration configuration,
            ref VsList vsList
            )
        {
            if (vsList == null)
                vsList = new VsList();

            if (vsList.RootKey == null)
                vsList.RootKey = Registry.LocalMachine;

            if (vsList.Versions == null)
            {
                vsList.Versions = new VersionList();

                // vsList.Versions.Add(new Version(8, 0)); // Visual Studio 2005

                if ((configuration == null) || !configuration.NoVs2008)
                    vsList.Versions.Add(new Version(9, 0)); // Visual Studio 2008

                if ((configuration == null) || !configuration.NoVs2010)
                    vsList.Versions.Add(new Version(10, 0));// Visual Studio 2010
            }

            if (vsList.FrameworkVersions == null)
            {
                vsList.FrameworkVersions = new VersionListMap();

                // vsList.FrameworkVersions.Add(new Version(8, 0),
                //     new VersionList(new Version[] {
                //         new Version(2, 0, 50727) }));

                if ((configuration == null) || !configuration.NoVs2008)
                    vsList.FrameworkVersions.Add(new Version(9, 0),
                        new VersionList(new Version[] {
                            new Version(2, 0, 50727) }));

                if ((configuration == null) || !configuration.NoVs2010)
                    vsList.FrameworkVersions.Add(new Version(10, 0),
                        new VersionList(new Version[] {
                            new Version(2, 0, 50727),
                                new Version(4, 0, 30319) }));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool HaveVsVersion(
            RegistryKey rootKey,
            Version vsVersion,
            bool whatIf,
            bool verbose
            )
        {
            if (vsVersion == null)
                return false;

            string format = "Software\\Microsoft\\VisualStudio\\{0}";
            string keyName = String.Format(format, vsVersion);

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

                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ForEachVsVersionRegistry(
            VsList vsList,
            VisualStudioRegistryCallback callback,
            Package package,
            object clientData,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            if (vsList == null)
            {
                error = "invalid VS list";
                return false;
            }

            RegistryKey rootKey = vsList.RootKey;

            if (rootKey == null)
            {
                error = "invalid root key";
                return false;
            }

            if (!Object.ReferenceEquals(rootKey, Registry.CurrentUser) &&
                !Object.ReferenceEquals(rootKey, Registry.LocalMachine))
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
                TraceOps.Trace(
                    TracePriority.Lower, traceCallback, String.Format(
                    "vsVersion = {0}", ForDisplay(vsVersion)),
                    traceCategory);

                if (!HaveVsVersion(rootKey, vsVersion, whatIf, verbose))
                {
                    TraceOps.Trace(TracePriority.Low, traceCallback,
                        "Visual Studio version not found, skipping...",
                        traceCategory);

                    continue;
                }

                if (callback == null)
                    continue;

                if (!callback(
                        rootKey, vsVersion, package, clientData, whatIf,
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

            XmlElement element = document.SelectSingleNode(String.Format(
                XPathForAddElement, invariant)) as XmlElement;

            if (element == null)
            {
                string[] elementNames = {
                        "system.data", "DbProviderFactories"
                    };

                XmlElement previousElement =
                    document.DocumentElement; /* configuration */

                foreach (string elementName in elementNames)
                {
                    element = previousElement.SelectSingleNode(
                        elementName) as XmlElement;

                    if (element == null)
                    {
                        element = document.CreateElement(
                            elementName, String.Empty);

                        previousElement.AppendChild(element);
                    }

                    previousElement = element;
                }

                element = document.CreateElement(
                    "add", String.Empty);

                previousElement.AppendChild(element);

                dirty = true;
            }

            if (!String.Equals(element.GetAttribute("name"),
                    name, StringComparison.InvariantCulture))
            {
                element.SetAttribute("name", name);
                dirty = true;
            }

            if (!String.Equals(element.GetAttribute("invariant"),
                    invariant, StringComparison.InvariantCulture))
            {
                element.SetAttribute("invariant", invariant);
                dirty = true;
            }

            if (!String.Equals(element.GetAttribute("description"),
                    description, StringComparison.InvariantCulture))
            {
                element.SetAttribute("description", description);
                dirty = true;
            }

            string fullTypeName = String.Format("{0}, {1}",
                typeName, assemblyName);

            if (!String.Equals(element.GetAttribute("type"),
                    fullTypeName, StringComparison.InvariantCulture))
            {
                element.SetAttribute("type", fullTypeName);
                dirty = true;
            }

            if (dirty || whatIf)
            {
                if (verbose)
                    TraceOps.Trace(
                        TracePriority.Highest, traceCallback, String.Format(
                        "element = {0}", ForDisplay(element)), traceCategory);

                if (!whatIf)
                    document.Save(fileName);

                saved = true;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

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

            XmlElement element = document.SelectSingleNode(String.Format(
                XPathForAddElement, invariant)) as XmlElement;

            if (element != null)
            {
                element.ParentNode.RemoveChild(element);
                dirty = true;
            }

            element = document.SelectSingleNode(String.Format(
                XPathForRemoveElement, invariant)) as XmlElement;

            if (element != null)
            {
                element.ParentNode.RemoveChild(element);
                dirty = true;
            }

            if (dirty || whatIf)
            {
                if (verbose)
                    TraceOps.Trace(
                        TracePriority.Highest, traceCallback, String.Format(
                        "element = {0}", ForDisplay(element)), traceCategory);

                if (!whatIf)
                    document.Save(fileName);

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
            object clientData,
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
            string platformName
            )
        {
            string format = !String.IsNullOrEmpty(platformName) ?
                "Software\\Microsoft\\{0}\\v{1}\\{2}\\AssemblyFoldersEx" :
                "Software\\Microsoft\\{0}\\v{1}\\AssemblyFoldersEx";

            return String.Format(format, frameworkName, frameworkVersion,
                platformName);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool AddToAssemblyFolders(
            RegistryKey rootKey,
            string frameworkName,
            Version frameworkVersion,
            string platformName,
            string subKeyName,
            string directory,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            string keyName = GetAssemblyFoldersKeyName(
                frameworkName, frameworkVersion, platformName);

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
            RegistryKey rootKey,
            string frameworkName,
            Version frameworkVersion,
            string platformName,
            string subKeyName,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            string keyName = GetAssemblyFoldersKeyName(
                frameworkName, frameworkVersion, platformName);

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

                RegistryHelper.DeleteSubKey(
                    key, subKeyName, whatIf, verbose);
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ProcessAssemblyFolders(
            RegistryKey rootKey,
            string frameworkName,
            Version frameworkVersion,
            string platformName,
            object clientData,
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
                    LegacyProjectName, whatIf, verbose, ref error) &&
                AddToAssemblyFolders(
                    rootKey, frameworkName, frameworkVersion, platformName,
                    ProjectName, pair.X, whatIf, verbose, ref error);
            }
            else
            {
                return RemoveFromAssemblyFolders(
                    rootKey, frameworkName, frameworkVersion, platformName,
                    ProjectName, whatIf, verbose, ref error);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Visual Studio Handling
        private static string GetVsKeyName(
            Version vsVersion
            )
        {
            if (vsVersion == null)
                return null;

            return String.Format("Software\\Microsoft\\VisualStudio\\{0}",
                vsVersion);
        }

        ///////////////////////////////////////////////////////////////////////

        #region Visual Studio Version Handling
        private static bool AddVsVersion(
            RegistryKey rootKey,
            Version vsVersion,
            Package package,
            object clientData,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            if (vsVersion != null)
            {
                VsList vsList = clientData as VsList;

                if (vsList != null)
                {
                    if (vsList.InstalledVersions == null)
                        vsList.InstalledVersions = new VersionList();

                    if (!vsList.InstalledVersions.Contains(vsVersion))
                        vsList.InstalledVersions.Add(vsVersion);
                }
            }

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Visual Studio Data Source Handling
        private static bool AddVsDataSource(
            RegistryKey rootKey,
            Version vsVersion,
            Package package,
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

            string keyName = GetVsKeyName(vsVersion);

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
            RegistryKey rootKey,
            Version vsVersion,
            Package package,
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

            string keyName = GetVsKeyName(vsVersion);

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
            RegistryKey rootKey,
            Version vsVersion,
            Package package,
            object clientData,
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
                    rootKey, vsVersion, package, whatIf, verbose, ref error);
            }
            else
            {
                return RemoveVsDataSource(
                    rootKey, vsVersion, package, whatIf, verbose, ref error);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Visual Studio Data Provider Handling
        private static bool AddVsDataProvider(
            RegistryKey rootKey,
            Version vsVersion,
            Package package,
            string fileName,
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

            string keyName = GetVsKeyName(vsVersion);

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
            RegistryKey rootKey,
            Version vsVersion,
            Package package,
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

            string keyName = GetVsKeyName(vsVersion);

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
            RegistryKey rootKey,
            Version vsVersion,
            Package package,
            object clientData,
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
                    rootKey, vsVersion, package, pair.X,
                    whatIf, verbose, ref error);
            }
            else
            {
                return RemoveVsDataProvider(
                    rootKey, vsVersion, package, whatIf,
                    verbose, ref error);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Visual Studio Package Handling
        private static void InitializeVsPackage(
            ref Package package
            )
        {
            if (package == null)
            {
                package = new Package();

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
            RegistryKey rootKey,
            Version vsVersion,
            Package package,
            string fileName,
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

            string keyName = GetVsKeyName(vsVersion);

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

                    using (MockRegistryKey packageKey = RegistryHelper.CreateSubKey(
                            subKey, package.PackageId.ToString(VsIdFormat), whatIf,
                            verbose))
                    {
                        if (packageKey == null)
                        {
                            error = String.Format(
                                "could not create registry key: {0}\\{1}",
                                key, package.PackageId.ToString(VsIdFormat));

                            return false;
                        }

                        RegistryHelper.SetValue(packageKey, null, String.Format(
                            "{0} Designer Package", ProjectName), whatIf,
                            verbose);

                        RegistryHelper.SetValue(packageKey, "Class",
                            "SQLite.Designer.SQLitePackage", whatIf, verbose);

                        RegistryHelper.SetValue(packageKey, "CodeBase",
                            fileName, whatIf, verbose);

                        RegistryHelper.SetValue(packageKey, "ID", 400, whatIf,
                            verbose);

                        RegistryHelper.SetValue(packageKey, "InprocServer32",
                            Path.Combine(Environment.SystemDirectory,
                                "mscoree.dll"), whatIf, verbose);

                        RegistryHelper.SetValue(packageKey, "CompanyName",
                            "http://system.data.sqlite.org/", whatIf, verbose);

                        RegistryHelper.SetValue(packageKey, "MinEdition",
                            "standard", whatIf, verbose);

                        RegistryHelper.SetValue(packageKey, "ProductName",
                            String.Format("{0} Designer Package", ProjectName),
                            whatIf, verbose);

                        RegistryHelper.SetValue(packageKey, "ProductVersion",
                            "1.0", whatIf, verbose);

                        using (MockRegistryKey toolboxKey = RegistryHelper.CreateSubKey(
                                packageKey, "Toolbox", whatIf, verbose))
                        {
                            if (toolboxKey == null)
                            {
                                error = String.Format(
                                    "could not create registry key: {0}\\Toolbox",
                                    packageKey);

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

                    using (MockRegistryKey serviceKey = RegistryHelper.CreateSubKey(
                            subKey, package.ServiceId.ToString(VsIdFormat), whatIf,
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
            RegistryKey rootKey,
            Version vsVersion,
            Package package,
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

            string keyName = GetVsKeyName(vsVersion);

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
                        key, package.PackageId.ToString(VsIdFormat), whatIf,
                        verbose);
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
                        subKey, package.PackageId.ToString(VsIdFormat), whatIf,
                        verbose);
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
                        subKey, package.ServiceId.ToString(VsIdFormat), whatIf,
                        verbose);
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ProcessVsPackage(
            RegistryKey rootKey,
            Version vsVersion,
            Package package,
            object clientData,
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
                    rootKey, vsVersion, package, pair.X, whatIf,
                    verbose, ref error);
            }
            else
            {
                return RemoveVsPackage(
                    rootKey, vsVersion, package, whatIf, verbose,
                    ref error);
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Application Entry Point
        private static int Main(
            string[] args
            )
        {
            Configuration configuration = null;
            string error = null;

            ///////////////////////////////////////////////////////////////////

            #region Command Line Processing
            if (!Configuration.FromArgs(
                    args, true, ref configuration, ref error) ||
                !Configuration.Process(
                    args, configuration, true, ref error) ||
                !Configuration.CheckRuntimeVersion(
                    configuration, true, ref error))
            {
                TraceOps.ShowMessage(
                    TracePriority.Highest, traceCallback, thisAssembly,
                    error, traceCategory, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return 1; /* FAILURE */
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region .NET Framework / Visual Studio Data
            Package package = null;
            FrameworkList frameworkList = null;
            VsList vsList = null;

            ///////////////////////////////////////////////////////////////////

            InitializeVsPackage(ref package);
            InitializeFrameworkList(configuration, ref frameworkList);
            InitializeVsList(configuration, ref vsList);
            #endregion

            ///////////////////////////////////////////////////////////////////

            AssemblyName assemblyName = AssemblyName.GetAssemblyName(
                configuration.CoreFileName); /* throw */

            ///////////////////////////////////////////////////////////////////

            AnyPair<string, bool> directoryPair = new AnyPair<string, bool>(
                configuration.Directory, configuration.Install);

            AnyPair<string, bool> fileNamePair = new AnyPair<string, bool>(
                configuration.DesignerFileName, configuration.Install);

            ///////////////////////////////////////////////////////////////////

            #region .NET GAC Install/Remove
            if (configuration.HasFlags(InstallFlags.GAC, true))
            {
                Publish publish = null;

                if (!configuration.WhatIf)
                    publish = new Publish();

                if (configuration.Install)
                {
                    if (!configuration.WhatIf)
                        publish.GacInstall(configuration.CoreFileName); /* throw */

                    TraceOps.Trace(
                        TracePriority.Highest, traceCallback, String.Format(
                        "GacInstall: assemblyPath = {0}",
                        ForDisplay(configuration.CoreFileName)), traceCategory);

                    if (!configuration.WhatIf)
                        publish.GacInstall(configuration.LinqFileName); /* throw */

                    TraceOps.Trace(
                        TracePriority.Highest, traceCallback, String.Format(
                        "GacInstall: assemblyPath = {0}",
                        ForDisplay(configuration.LinqFileName)), traceCategory);
                }
                else
                {
                    if (!configuration.WhatIf)
                        publish.GacRemove(configuration.LinqFileName); /* throw */

                    TraceOps.Trace(
                        TracePriority.Highest, traceCallback, String.Format(
                        "GacRemove: assemblyPath = {0}",
                        ForDisplay(configuration.LinqFileName)), traceCategory);

                    if (!configuration.WhatIf)
                        publish.GacRemove(configuration.CoreFileName); /* throw */

                    TraceOps.Trace(
                        TracePriority.Highest, traceCallback, String.Format(
                        "GacRemove: assemblyPath = {0}",
                        ForDisplay(configuration.CoreFileName)), traceCategory);
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region .NET AssemblyFolders
            if (configuration.HasFlags(InstallFlags.AssemblyFolders, true))
            {
                if (!ForEachFrameworkRegistry(
                        frameworkList, ProcessAssemblyFolders,
                        directoryPair, configuration.WhatIf,
                        configuration.Verbose, ref error))
                {
                    TraceOps.ShowMessage(
                        TracePriority.Highest, traceCallback, null,
                        error, traceCategory, MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return 1; /* FAILURE */
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region .NET DbProviderFactory
            if (configuration.HasFlags(InstallFlags.DbProviderFactory, true))
            {
                bool saved = false;

                if (!ForEachFrameworkConfig(
                        frameworkList, ProcessDbProviderFactory,
                        InvariantName, ProviderName, Description,
                        FactoryTypeName, assemblyName, directoryPair,
                        configuration.WhatIf, configuration.Verbose,
                        ref saved, ref error))
                {
                    TraceOps.ShowMessage(
                        TracePriority.Highest, traceCallback, null,
                        error, traceCategory, MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return 1; /* FAILURE */
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region VS Package
            if (configuration.HasFlags(InstallFlags.VsPackage, true))
            {
                if (!ForEachVsVersionRegistry(
                        vsList, ProcessVsPackage, package, fileNamePair,
                        configuration.WhatIf, configuration.Verbose,
                        ref error))
                {
                    TraceOps.ShowMessage(
                        TracePriority.Highest, traceCallback, null,
                        error, traceCategory, MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return 1; /* FAILURE */
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region VS DataSource
            if (configuration.HasFlags(InstallFlags.VsDataSource, true))
            {
                if (!ForEachVsVersionRegistry(
                        vsList, ProcessVsDataSource, package, fileNamePair,
                        configuration.WhatIf, configuration.Verbose,
                        ref error))
                {
                    TraceOps.ShowMessage(
                        TracePriority.Highest, traceCallback, null,
                        error, traceCategory, MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return 1; /* FAILURE */
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region VS DataProvider
            if (configuration.HasFlags(InstallFlags.VsDataProvider, true))
            {
                if (!ForEachVsVersionRegistry(
                        vsList, ProcessVsDataProvider, package, fileNamePair,
                        configuration.WhatIf, configuration.Verbose,
                        ref error))
                {
                    TraceOps.ShowMessage(
                        TracePriority.Highest, traceCallback, null,
                        error, traceCategory, MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return 1; /* FAILURE */
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Log Summary
            TraceOps.Trace(
                TracePriority.Higher, traceCallback, String.Format(
                "subKeysCreated = {0}, subKeysDeleted = {1}, " +
                "keyValuesSet = {2}, keyValuesDeleted = {3}",
                ForDisplay(RegistryHelper.SubKeysCreated),
                ForDisplay(RegistryHelper.SubKeysDeleted),
                ForDisplay(RegistryHelper.KeyValuesSet),
                ForDisplay(RegistryHelper.KeyValuesDeleted)),
                traceCategory);
            #endregion

            ///////////////////////////////////////////////////////////////////

            return 0; /* SUCCESS */
        }
        #endregion
    }
    #endregion
}
