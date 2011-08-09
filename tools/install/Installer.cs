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
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using Microsoft.Win32;

namespace System.Data.SQLite
{
    #region Public Delegates
    public delegate void TraceCallback(
        string message,
        string category
    );

    ///////////////////////////////////////////////////////////////////////////

    public delegate bool FrameworkConfigCallback(
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

    public delegate bool FrameworkRegistryCallback(
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

    public delegate bool VisualStudioRegistryCallback(
        RegistryKey rootKey,
        Version vsVersion,
        Guid packageId,
        Guid serviceId,
        Guid dataSourceId,
        Guid dataProviderId,
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
        Default = All
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Installer Class
    internal static class Installer
    {
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
            private const string Iso8601DateTimeOutputFormat =
                "yyyy.MM.ddTHH:mm:ss.fffffff";
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Data
            private static object syncRoot = new object();
            private static long nextId;
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
                TraceCallback traceCallback,
                Assembly assembly,
                string message,
                string category,
                MessageBoxButtons buttons,
                MessageBoxIcon icon
                )
            {
                DialogResult result = DialogResult.OK;

                Trace(traceCallback, message, category);

                if (SystemInformation.UserInteractive)
                {
                    string title = GetAssemblyTitle(assembly);

                    if (title == null)
                        title = Application.ProductName;

                    result = MessageBox.Show(message, title, buttons, icon);

                    Trace(traceCallback, String.Format(
                        "User choice of \"{0}\".", result), category);

                    return result;
                }

                Trace(traceCallback, String.Format(
                    "Default choice of \"{0}\".", result), category);

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

            public static string Trace(
                TraceCallback traceCallback,
                Exception exception,
                string category
                )
            {
                if (exception != null)
                    return Trace(traceCallback,
                        new StackTrace(exception, true), 0,
                        exception.ToString(), category);

                return null;
            }

            ///////////////////////////////////////////////////////////////////

            public static string Trace(
                TraceCallback traceCallback,
                string message,
                string category
                )
            {
                return Trace(traceCallback, null, 1, message, category);
            }

            ///////////////////////////////////////////////////////////////////

            private static string Trace(
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
                bool whatIf
                )
                : this(key, null, whatIf)
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
                        new MockRegistryKey(subKey) :
                        new MockRegistryKey(key, subKeyName);
                }
                else
                {
                    return new MockRegistryKey(
                        key.CreateSubKey(subKeyName), false);
                }
            }

            ///////////////////////////////////////////////////////////////////

            public void DeleteSubKey(
                string subKeyName
                )
            {
                CheckDisposed();

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

                if (key == null)
                    return null;

                RegistryKey subKey = key.OpenSubKey(
                    subKeyName, whatIf ? false : writable);

                return (subKey != null) ?
                    new MockRegistryKey(subKey, whatIf) : null;
            }

            ///////////////////////////////////////////////////////////////////

            public void SetValue(
                string name,
                object value
                )
            {
                CheckDisposed();

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
                get { CheckDisposed(); return key; }
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
                return new MockRegistryKey(key, null, true);
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

        private const string NameAndValueFormat = "{0}: {1}";
        private const string TraceFormat = "#{0} @ {1}: {2}";
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

        ///////////////////////////////////////////////////////////////////////

        private static RegistryKey frameworkRootKey;
        private static StringList frameworkNameList;
        private static VersionMap frameworkVersionMap;
        private static StringList platformNameList;

        ///////////////////////////////////////////////////////////////////////

        private static RegistryKey vsRootKey;
        private static VersionList vsVersionList;
        private static Guid? vsPackageId;
        private static Guid? vsServiceId;
        private static Guid? vsDataSourcesId;
        private static Guid? vsDataProviderId;
        private static Guid? vsAdoNetTechnologyId;
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
                TraceFormat, TraceOps.NextId(),
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
        private static string GetFrameworkDirectory(
            RegistryKey rootKey,
            Version frameworkVersion,
            bool whatIf,
            bool verbose
            )
        {
            using (MockRegistryKey key = OpenSubKey(
                    rootKey, FrameworkKeyName, false, whatIf, verbose))
            {
                if (key == null)
                    return null;

                object value = GetValue(
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
                using (MockRegistryKey key = OpenSubKey(
                        rootKey, keyNames[index], false, whatIf, verbose))
                {
                    if (key == null)
                        continue;

                    if (useSubKeys[index])
                    {
                        foreach (string subKeyName in GetSubKeyNames(
                                key, whatIf, verbose))
                        {
                            using (MockRegistryKey subKey = OpenSubKey(
                                    key, subKeyName, false, whatIf, verbose))
                            {
                                if (subKey == null)
                                    continue;

                                object value = GetValue(
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
                        object value = GetValue(
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
        private static void InitializeAllFrameworks(
            Configuration configuration
            )
        {
            if (frameworkRootKey == null)
                frameworkRootKey = Registry.LocalMachine;

            if (frameworkNameList == null)
            {
                frameworkNameList = new StringList();

                if ((configuration == null) || !configuration.NoDesktop)
                    frameworkNameList.Add(".NETFramework");

                if ((configuration == null) || !configuration.NoCompact)
                {
                    frameworkNameList.Add(".NETCompactFramework");
                    frameworkNameList.Add(".NETCompactFramework");
                    frameworkNameList.Add(".NETCompactFramework");
                }
            }

            if (frameworkVersionMap == null)
                frameworkVersionMap = new VersionMap();

            if ((configuration == null) || !configuration.NoDesktop)
            {
                VersionList desktopVersionList = new VersionList();

                if ((configuration == null) || !configuration.NoNetFx20)
                    desktopVersionList.Add(new Version(2, 0, 50727));

                if ((configuration == null) || !configuration.NoNetFx40)
                    desktopVersionList.Add(new Version(4, 0, 30319));

                frameworkVersionMap.Add(".NETFramework", desktopVersionList);
            }

            if ((configuration == null) || !configuration.NoCompact)
            {
                frameworkVersionMap.Add(".NETCompactFramework", new VersionList(
                    new Version[] {
                    new Version(2, 0, 0, 0), new Version(3, 5, 0, 0)
                }));
            }

            if (platformNameList == null)
            {
                platformNameList = new StringList();

                if ((configuration == null) || !configuration.NoDesktop)
                    platformNameList.Add(null);

                if ((configuration == null) || !configuration.NoCompact)
                {
                    platformNameList.Add("PocketPC");
                    platformNameList.Add("Smartphone");
                    platformNameList.Add("WindowsCE");
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

            using (MockRegistryKey key = OpenSubKey(
                    rootKey, keyName, false, whatIf, verbose))
            {
                if (key == null)
                    return false;

                if (platformName != null) // NOTE: Skip non-desktop frameworks.
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
            RegistryKey rootKey = frameworkRootKey;

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

            if (frameworkNameList == null)
            {
                error = "no framework names found";
                return false;
            }

            if (frameworkVersionMap == null)
            {
                error = "no framework versions found";
                return false;
            }

            if (platformNameList == null)
            {
                error = "no platform names found";
                return false;
            }

            if (frameworkNameList.Count != platformNameList.Count)
            {
                error = String.Format("framework name count {0} does not " +
                    "match platform name count {1}", frameworkNameList.Count,
                    platformNameList.Count);

                return false;
            }

            for (int index = 0; index < frameworkNameList.Count; index++)
            {
                //
                // NOTE: Grab the name of the framework (e.g. ".NETFramework")
                //       and the name of the platform (e.g. "WindowsCE").
                //
                string frameworkName = frameworkNameList[index];
                string platformName = platformNameList[index];

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

                if (!frameworkVersionMap.TryGetValue(
                        frameworkName, out frameworkVersionList) ||
                    (frameworkVersionList == null))
                {
                    continue;
                }

                foreach (Version frameworkVersion in frameworkVersionList)
                {
                    TraceOps.Trace(traceCallback, String.Format(
                        "frameworkName = {0}, frameworkVersion = {1}, " +
                        "platformName = {2}", ForDisplay(frameworkName),
                        ForDisplay(frameworkVersion),
                        ForDisplay(platformName)), traceCategory);

                    if (!HaveFramework(
                            rootKey, frameworkName, frameworkVersion,
                            platformName, whatIf, verbose))
                    {
                        TraceOps.Trace(traceCallback,
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
                        TraceOps.Trace(traceCallback, String.Format(
                            ".NET Framework v{0} directory is invalid, " +
                            "skipping...", frameworkVersion), traceCategory);

                        continue;
                    }

                    directory = Path.Combine(directory, "Config");

                    if (!Directory.Exists(directory))
                    {
                        TraceOps.Trace(traceCallback, String.Format(
                            ".NET Framework v{0} directory \"{1}\" does not " +
                            "exist, skipping...", frameworkVersion, directory),
                            traceCategory);

                        continue;
                    }

                    string fileName = Path.Combine(directory, "machine.config");

                    if (!File.Exists(fileName))
                    {
                        TraceOps.Trace(traceCallback, String.Format(
                            ".NET Framework v{0} file \"{1}\" does not exist, " +
                            "skipping...", frameworkVersion, fileName),
                            traceCategory);

                        continue;
                    }

                    if (!callback(
                            fileName, invariant, name, description, typeName,
                            assemblyName, clientData, whatIf, verbose,
                            ref saved, ref error))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ForEachFrameworkRegistry(
            FrameworkRegistryCallback callback,
            object clientData,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            RegistryKey rootKey = frameworkRootKey;

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

            if (frameworkNameList == null)
            {
                error = "no framework names found";
                return false;
            }

            if (frameworkVersionMap == null)
            {
                error = "no framework versions found";
                return false;
            }

            if (platformNameList == null)
            {
                error = "no platform names found";
                return false;
            }

            if (frameworkNameList.Count != platformNameList.Count)
            {
                error = String.Format("framework name count {0} does not " +
                    "match platform name count {1}", frameworkNameList.Count,
                    platformNameList.Count);

                return false;
            }

            for (int index = 0; index < frameworkNameList.Count; index++)
            {
                //
                // NOTE: Grab the name of the framework (e.g. ".NETFramework")
                //       and the name of the platform (e.g. "WindowsCE").
                //
                string frameworkName = frameworkNameList[index];
                string platformName = platformNameList[index];

                //
                // NOTE: Grab the supported versions of this particular
                //       framework.
                //
                VersionList frameworkVersionList;

                if (!frameworkVersionMap.TryGetValue(
                        frameworkName, out frameworkVersionList) ||
                    (frameworkVersionList == null))
                {
                    continue;
                }

                foreach (Version frameworkVersion in frameworkVersionList)
                {
                    TraceOps.Trace(traceCallback, String.Format(
                        "frameworkName = {0}, frameworkVersion = {1}, " +
                        "platformName = {2}", ForDisplay(frameworkName),
                        ForDisplay(frameworkVersion),
                        ForDisplay(platformName)), traceCategory);

                    if (!HaveFramework(
                            rootKey, frameworkName, frameworkVersion,
                            platformName, whatIf, verbose))
                    {
                        TraceOps.Trace(traceCallback,
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
        private static void InitializeAllVsVersions(
            Configuration configuration
            )
        {
            if (vsRootKey == null)
                vsRootKey = Registry.LocalMachine;

            if (vsAdoNetTechnologyId == null)
                vsAdoNetTechnologyId = new Guid(
                    "77AB9A9D-78B9-4BA7-91AC-873F5338F1D2");

            if (vsPackageId == null)
                vsPackageId = new Guid(
                    "DCBE6C8D-0E57-4099-A183-98FF74C64D9C");

            if (vsServiceId == null)
                vsServiceId = new Guid(
                    "DCBE6C8D-0E57-4099-A183-98FF74C64D9D");

            if (vsDataSourcesId == null)
                vsDataSourcesId = new Guid(
                    "0EBAAB6E-CA80-4B4A-8DDF-CBE6BF058C71");

            if (vsDataProviderId == null)
                vsDataProviderId = new Guid(
                    "0EBAAB6E-CA80-4B4A-8DDF-CBE6BF058C70");

            if (vsVersionList == null)
            {
                vsVersionList = new VersionList();

                // vsVersionList.Add(new Version(8, 0)); // Visual Studio 2005

                if ((configuration == null) || !configuration.NoVs2008)
                    vsVersionList.Add(new Version(9, 0)); // Visual Studio 2008

                if ((configuration == null) || !configuration.NoVs2010)
                    vsVersionList.Add(new Version(10, 0));// Visual Studio 2010
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
            string format = "Software\\Microsoft\\VisualStudio\\{0}";
            string keyName = String.Format(format, vsVersion);

            using (MockRegistryKey key = OpenSubKey(
                    rootKey, keyName, false, whatIf, verbose))
            {
                if (key == null)
                    return false;

                object value = GetValue(
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
            VisualStudioRegistryCallback callback,
            Guid packageId,
            Guid serviceId,
            Guid dataSourceId,
            Guid dataProviderId,
            object clientData,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            RegistryKey rootKey = vsRootKey;

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

            if (vsVersionList == null)
            {
                error = "no VS versions found";
                return false;
            }

            foreach (Version vsVersion in vsVersionList)
            {
                TraceOps.Trace(traceCallback, String.Format(
                    "vsVersion = {0}", ForDisplay(vsVersion)),
                    traceCategory);

                if (!HaveVsVersion(rootKey, vsVersion, whatIf, verbose))
                {
                    TraceOps.Trace(traceCallback,
                        "Visual Studio version not found, skipping...",
                        traceCategory);

                    continue;
                }

                if (callback == null)
                    continue;

                if (!callback(
                        rootKey, vsVersion, packageId, serviceId,
                        dataSourceId, dataProviderId, clientData, whatIf,
                        verbose, ref error))
                {
                    return false;
                }
            }

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Generic Registry Handling
        private static RegistryKey GetRootKeyByName(
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

        ///////////////////////////////////////////////////////////////////////

        private static MockRegistryKey OpenSubKey(
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
                TraceOps.Trace(traceCallback, String.Format(
                    "rootKey = {0}, subKeyName = {1}, writable = {2}",
                    ForDisplay(rootKey), ForDisplay(subKeyName), writable),
                    traceCategory);

            //
            // HACK: Always forbid writable access when operating in 'what-if'
            //       mode.
            //
            MockRegistryKey key = rootKey.OpenSubKey(
                subKeyName, whatIf ? false : writable);

            return (key != null) ?
                new MockRegistryKey(key, whatIf) : null;
        }

        ///////////////////////////////////////////////////////////////////////

        private static MockRegistryKey CreateSubKey(
            MockRegistryKey rootKey,
            string subKeyName,
            bool whatIf,
            bool verbose
            )
        {
            if (rootKey == null)
                return null;

            if (verbose)
                TraceOps.Trace(traceCallback, String.Format(
                    "rootKey = {0}, subKeyName = {1}", ForDisplay(rootKey),
                    ForDisplay(subKeyName)), traceCategory);

            //
            // HACK: Always open a key, rather than creating one when operating
            //       in 'what-if' mode.
            //
            if (whatIf)
            {
                //
                // HACK: Attempt to open the specified sub-key.  If this
                //       fails, we will simply return the root key itself
                //       since no writes are allowed in 'what-if' mode
                //       anyhow.
                //
                MockRegistryKey key = rootKey.OpenSubKey(subKeyName);

                return (key != null) ?
                    key : new MockRegistryKey(rootKey, subKeyName);
            }
            else
            {
                return new MockRegistryKey(
                    rootKey.CreateSubKey(subKeyName), false);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void DeleteSubKey(
            MockRegistryKey rootKey,
            string subKeyName,
            bool whatIf,
            bool verbose
            )
        {
            if (rootKey == null)
                return;

            if (verbose)
                TraceOps.Trace(traceCallback, String.Format(
                    "rootKey = {0}, subKeyName = {1}", ForDisplay(rootKey),
                    ForDisplay(subKeyName)), traceCategory);

            if (!whatIf)
                rootKey.DeleteSubKey(subKeyName);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void DeleteSubKeyTree(
            MockRegistryKey rootKey,
            string subKeyName,
            bool whatIf,
            bool verbose
            )
        {
            if (rootKey == null)
                return;

            if (verbose)
                TraceOps.Trace(traceCallback, String.Format(
                    "rootKey = {0}, subKeyName = {1}", ForDisplay(rootKey),
                    ForDisplay(subKeyName)), traceCategory);

            if (!whatIf)
                rootKey.DeleteSubKeyTree(subKeyName);
        }

        ///////////////////////////////////////////////////////////////////////

        private static string[] GetSubKeyNames(
            MockRegistryKey key,
            bool whatIf,
            bool verbose
            )
        {
            if (key == null)
                return null;

            if (verbose)
                TraceOps.Trace(traceCallback, String.Format(
                    "key = {0}", ForDisplay(key)), traceCategory);

            return key.GetSubKeyNames();
        }

        ///////////////////////////////////////////////////////////////////////

        private static object GetValue(
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
                TraceOps.Trace(traceCallback, String.Format(
                    "key = {0}, name = {1}, defaultValue = {2}",
                    ForDisplay(key), ForDisplay(name),
                    ForDisplay(defaultValue)), traceCategory);

            return key.GetValue(name, defaultValue);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void SetValue(
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
                TraceOps.Trace(traceCallback, String.Format(
                    "key = {0}, name = {1}, value = {2}", ForDisplay(key),
                    ForDisplay(name), ForDisplay(value)), traceCategory);

            if (!whatIf)
                key.SetValue(name, value);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void DeleteValue(
            MockRegistryKey key,
            string name,
            bool whatIf,
            bool verbose
            )
        {
            if (key == null)
                return;

            if (verbose)
                TraceOps.Trace(traceCallback, String.Format(
                    "key = {0}, name = {1}", ForDisplay(key),
                    ForDisplay(name)), traceCategory);

            if (!whatIf)
                key.DeleteValue(name);
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

            if (dirty)
            {
                if (verbose)
                    TraceOps.Trace(traceCallback, String.Format(
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

            if (dirty)
            {
                if (verbose)
                    TraceOps.Trace(traceCallback, String.Format(
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

            using (MockRegistryKey key = OpenSubKey(
                    rootKey, keyName, true, whatIf, verbose))
            {
                if (key == null)
                {
                    error = String.Format(
                        "could not open registry key: {0}\\{1}",
                        rootKey, keyName);

                    return false;
                }

                using (MockRegistryKey subKey = CreateSubKey(
                        key, subKeyName, whatIf, verbose))
                {
                    if (subKey == null)
                    {
                        error = String.Format(
                            "could not create registry key: {0}\\{1}",
                            key, subKeyName);

                        return false;
                    }

                    SetValue(subKey, null, directory, whatIf, verbose);
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

            using (MockRegistryKey key = OpenSubKey(
                    rootKey, keyName, false, whatIf, verbose))
            {
                if (key == null)
                {
                    error = String.Format(
                        "could not open registry key: {0}\\{1}",
                        rootKey, keyName);

                    return false;
                }

                DeleteSubKey(key, subKeyName, whatIf, verbose);
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
            return String.Format("Software\\Microsoft\\VisualStudio\\{0}",
                vsVersion);
        }

        ///////////////////////////////////////////////////////////////////////

        #region Visual Studio Data Source Handling
        private static bool AddVsDataSource(
            RegistryKey rootKey,
            Version vsVersion,
            Guid dataSourceId,
            Guid dataProviderId,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            string keyName = GetVsKeyName(vsVersion);

            using (MockRegistryKey key = OpenSubKey(
                    rootKey, keyName, false, whatIf, verbose))
            {
                if (key == null)
                {
                    error = String.Format(
                        "could not open registry key: {0}\\{1}",
                        rootKey, keyName);

                    return false;
                }

                using (MockRegistryKey subKey = OpenSubKey(
                        key, "DataSources", true, whatIf, verbose))
                {
                    if (subKey == null)
                    {
                        error = String.Format(
                            "could not open registry key: {0}\\DataSources",
                            key);

                        return false;
                    }

                    using (MockRegistryKey dataSourceKey = CreateSubKey(
                            subKey, dataSourceId.ToString(VsIdFormat), whatIf,
                            verbose))
                    {
                        if (dataSourceKey == null)
                        {
                            error = String.Format(
                                "could not create registry key: {0}\\{1}",
                                key, dataSourceId.ToString(VsIdFormat));

                            return false;
                        }

                        SetValue(dataSourceKey, null, String.Format(
                            "{0} Database File", ProjectName), whatIf,
                            verbose);

                        CreateSubKey(dataSourceKey, String.Format(
                            "SupportingProviders\\{0}",
                            dataProviderId.ToString(VsIdFormat)), whatIf,
                            verbose);
                    }
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool RemoveVsDataSource(
            RegistryKey rootKey,
            Version vsVersion,
            Guid dataSourceId,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            string keyName = GetVsKeyName(vsVersion);

            using (MockRegistryKey key = OpenSubKey(
                    rootKey, keyName, false, whatIf, verbose))
            {
                if (key == null)
                {
                    error = String.Format(
                        "could not open registry key: {0}\\{1}",
                        rootKey, keyName);

                    return false;
                }

                using (MockRegistryKey subKey = OpenSubKey(
                        key, "DataSources", true, whatIf, verbose))
                {
                    if (subKey == null)
                    {
                        error = String.Format(
                            "could not open registry key: {0}\\DataSources",
                            key);

                        return false;
                    }

                    DeleteSubKeyTree(
                        subKey, dataSourceId.ToString(VsIdFormat), whatIf,
                        verbose);
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ProcessVsDataSource(
            RegistryKey rootKey,
            Version vsVersion,
            Guid packageId, /* NOT USED */
            Guid serviceId, /* NOT USED */
            Guid dataSourceId,
            Guid dataProviderId,
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
                return AddVsDataSource(
                    rootKey, vsVersion, dataSourceId, dataProviderId,
                    whatIf, verbose, ref error);
            }
            else
            {
                return RemoveVsDataSource(
                    rootKey, vsVersion, dataSourceId, whatIf, verbose,
                    ref error);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Visual Studio Data Provider Handling
        private static bool AddVsDataProvider(
            RegistryKey rootKey,
            Version vsVersion,
            Guid serviceId,
            Guid dataProviderId,
            string fileName,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            if (vsAdoNetTechnologyId == null)
            {
                error = "invalid ADO.NET technology Id";
                return false;
            }

            string keyName = GetVsKeyName(vsVersion);

            using (MockRegistryKey key = OpenSubKey(
                    rootKey, keyName, false, whatIf, verbose))
            {
                if (key == null)
                {
                    error = String.Format(
                        "could not open registry key: {0}\\{1}",
                        rootKey, keyName);

                    return false;
                }

                using (MockRegistryKey subKey = OpenSubKey(
                        key, "DataProviders", true, whatIf, verbose))
                {
                    if (subKey == null)
                    {
                        error = String.Format(
                            "could not open registry key: {0}\\DataProviders",
                            key);

                        return false;
                    }

                    using (MockRegistryKey dataProviderKey = CreateSubKey(
                            subKey, dataProviderId.ToString(VsIdFormat), whatIf,
                            verbose))
                    {
                        if (dataProviderKey == null)
                        {
                            error = String.Format(
                                "could not create registry key: {0}\\{1}",
                                key, dataProviderId.ToString(VsIdFormat));

                            return false;
                        }

                        SetValue(dataProviderKey, null, Description, whatIf,
                            verbose);

                        SetValue(dataProviderKey, "InvariantName",
                            InvariantName, whatIf, verbose);

                        SetValue(dataProviderKey, "Technology",
                            ((Guid)vsAdoNetTechnologyId).ToString(VsIdFormat),
                            whatIf, verbose);

                        SetValue(dataProviderKey, "CodeBase", fileName, whatIf,
                            verbose);

                        SetValue(dataProviderKey, "FactoryService",
                            serviceId.ToString(VsIdFormat), whatIf, verbose);

                        CreateSubKey(dataProviderKey,
                            "SupportedObjects\\DataConnectionUIControl",
                            whatIf, verbose);

                        CreateSubKey(dataProviderKey,
                            "SupportedObjects\\DataConnectionProperties",
                            whatIf, verbose);

                        CreateSubKey(dataProviderKey,
                            "SupportedObjects\\DataConnectionSupport", whatIf,
                            verbose);

                        CreateSubKey(dataProviderKey,
                            "SupportedObjects\\DataObjectSupport", whatIf,
                            verbose);

                        CreateSubKey(dataProviderKey,
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
            Guid dataProviderId,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            string keyName = GetVsKeyName(vsVersion);

            using (MockRegistryKey key = OpenSubKey(
                    rootKey, keyName, false, whatIf, verbose))
            {
                if (key == null)
                {
                    error = String.Format(
                        "could not open registry key: {0}\\{1}",
                        rootKey, keyName);

                    return false;
                }

                using (MockRegistryKey subKey = OpenSubKey(
                        key, "DataProviders", true, whatIf, verbose))
                {
                    if (subKey == null)
                    {
                        error = String.Format(
                            "could not open registry key: {0}\\DataProviders",
                            key);

                        return false;
                    }

                    DeleteSubKeyTree(
                        subKey, dataProviderId.ToString(VsIdFormat), whatIf,
                        verbose);
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ProcessVsDataProvider(
            RegistryKey rootKey,
            Version vsVersion,
            Guid packageId, /* NOT USED */
            Guid serviceId,
            Guid dataSourceId, /* NOT USED */
            Guid dataProviderId,
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
                    rootKey, vsVersion, serviceId, dataProviderId, pair.X,
                    whatIf, verbose, ref error);
            }
            else
            {
                return RemoveVsDataProvider(
                    rootKey, vsVersion, dataProviderId, whatIf, verbose,
                    ref error);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Visual Studio Package Handling
        private static bool AddVsPackage(
            RegistryKey rootKey,
            Version vsVersion,
            Guid packageId,
            Guid serviceId,
            string fileName,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            string keyName = GetVsKeyName(vsVersion);

            using (MockRegistryKey key = OpenSubKey(
                    rootKey, keyName, false, whatIf, verbose))
            {
                if (key == null)
                {
                    error = String.Format(
                        "could not open registry key: {0}\\{1}",
                        rootKey, keyName);

                    return false;
                }

                using (MockRegistryKey subKey = OpenSubKey(
                        key, "Packages", true, whatIf, verbose))
                {
                    if (subKey == null)
                    {
                        error = String.Format(
                            "could not open registry key: {0}\\Packages",
                            key);

                        return false;
                    }

                    using (MockRegistryKey packageKey = CreateSubKey(
                            subKey, packageId.ToString(VsIdFormat), whatIf,
                            verbose))
                    {
                        if (packageKey == null)
                        {
                            error = String.Format(
                                "could not create registry key: {0}\\{1}",
                                key, packageId.ToString(VsIdFormat));

                            return false;
                        }

                        SetValue(packageKey, null, String.Format(
                            "{0} Designer Package", ProjectName), whatIf,
                            verbose);

                        SetValue(packageKey, "Class",
                            "SQLite.Designer.SQLitePackage", whatIf, verbose);

                        SetValue(packageKey, "CodeBase", fileName, whatIf,
                            verbose);

                        SetValue(packageKey, "ID", 400, whatIf, verbose);

                        SetValue(packageKey, "InprocServer32",
                            Path.Combine(Environment.SystemDirectory,
                                "mscoree.dll"), whatIf, verbose);

                        SetValue(packageKey, "CompanyName",
                            "http://system.data.sqlite.org/", whatIf, verbose);

                        SetValue(packageKey, "MinEdition", "standard", whatIf,
                            verbose);

                        SetValue(packageKey, "ProductName", String.Format(
                            "{0} Designer Package", ProjectName), whatIf,
                            verbose);

                        SetValue(packageKey, "ProductVersion", "1.0", whatIf,
                            verbose);

                        using (MockRegistryKey toolboxKey = CreateSubKey(
                                packageKey, "Toolbox", whatIf, verbose))
                        {
                            if (toolboxKey == null)
                            {
                                error = String.Format(
                                    "could not create registry key: {0}\\Toolbox",
                                    packageKey);

                                return false;
                            }

                            SetValue(toolboxKey, "Default Items", 3, whatIf,
                                verbose);
                        }
                    }
                }

                using (MockRegistryKey subKey = OpenSubKey(
                        key, "Menus", true, whatIf, verbose))
                {
                    if (subKey == null)
                    {
                        error = String.Format(
                            "could not open registry key: {0}\\Menus",
                            key);

                        return false;
                    }

                    SetValue(subKey, packageId.ToString(VsIdFormat),
                        ", 1000, 3", whatIf, verbose);
                }

                using (MockRegistryKey subKey = OpenSubKey(
                        key, "Services", true, whatIf, verbose))
                {
                    if (subKey == null)
                    {
                        error = String.Format(
                            "could not open registry key: {0}\\Services",
                            key);

                        return false;
                    }

                    using (MockRegistryKey serviceKey = CreateSubKey(
                            subKey, serviceId.ToString(VsIdFormat), whatIf,
                            verbose))
                    {
                        if (serviceKey == null)
                        {
                            error = String.Format(
                                "could not create registry key: {0}\\{1}",
                                key, serviceId.ToString(VsIdFormat));

                            return false;
                        }

                        SetValue(serviceKey, null,
                            packageId.ToString(VsIdFormat), whatIf, verbose);

                        SetValue(serviceKey, "Name", String.Format(
                            "{0} Designer Service", ProjectName), whatIf,
                            verbose);
                    }
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool RemoveVsPackage(
            RegistryKey rootKey,
            Version vsVersion,
            Guid packageId,
            Guid serviceId,
            bool whatIf,
            bool verbose,
            ref string error
            )
        {
            string keyName = GetVsKeyName(vsVersion);

            using (MockRegistryKey key = OpenSubKey(
                    rootKey, keyName, false, whatIf, verbose))
            {
                if (key == null)
                {
                    error = String.Format(
                        "could not open registry key: {0}\\{1}",
                        rootKey, keyName);

                    return false;
                }

                using (MockRegistryKey subKey = OpenSubKey(
                        key, "Packages", true, whatIf, verbose))
                {
                    if (subKey == null)
                    {
                        error = String.Format(
                            "could not open registry key: {0}\\Packages",
                            key);

                        return false;
                    }

                    DeleteSubKeyTree(
                        key, packageId.ToString(VsIdFormat), whatIf, verbose);
                }

                using (MockRegistryKey subKey = OpenSubKey(
                        key, "Menus", true, whatIf, verbose))
                {
                    if (subKey == null)
                    {
                        error = String.Format(
                            "could not open registry key: {0}\\Menus",
                            key);

                        return false;
                    }

                    DeleteValue(subKey, packageId.ToString(VsIdFormat), whatIf,
                        verbose);
                }

                using (MockRegistryKey subKey = OpenSubKey(
                        key, "Services", true, whatIf, verbose))
                {
                    if (subKey == null)
                    {
                        error = String.Format(
                            "could not open registry key: {0}\\Services",
                            key);

                        return false;
                    }

                    DeleteSubKeyTree(
                        subKey, serviceId.ToString(VsIdFormat), whatIf,
                        verbose);
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ProcessVsPackage(
            RegistryKey rootKey,
            Version vsVersion,
            Guid packageId,
            Guid serviceId,
            Guid dataSourceId,
            Guid dataProviderId,
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
                    rootKey, vsVersion, packageId, serviceId, pair.X, whatIf,
                    verbose, ref error);
            }
            else
            {
                return RemoveVsPackage(
                    rootKey, vsVersion, packageId, serviceId, whatIf, verbose,
                    ref error);
            }
        }
        #endregion
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
                InstallFlags installFlags,
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
                bool verbose
                )
            {
                this.assembly = assembly;
                this.logFileName = logFileName;
                this.directory = directory;
                this.coreFileName = coreFileName;
                this.linqFileName = linqFileName;
                this.designerFileName = designerFileName;
                this.installFlags = installFlags;
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

                return new Configuration(
                    thisAssembly, null, directory, coreFileName, linqFileName,
                    designerFileName, InstallFlags.Default, true, false, true,
                    false, false, false, false, false, false, false, true,
                    true);
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
                                    error = TraceOps.Trace(traceCallback, String.Format(
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
                            else if (MatchOption(newArg, "installFlags"))
                            {
                                object value = ParseEnum(
                                    typeof(InstallFlags), text, true);

                                if (value == null)
                                {
                                    error = TraceOps.Trace(traceCallback, String.Format(
                                        "Invalid install flags value: {0}",
                                        ForDisplay(text)), traceCategory);

                                    if (strict)
                                        return false;

                                    continue;
                                }

                                configuration.installFlags = (InstallFlags)value;
                            }
                            else if (MatchOption(newArg, "install"))
                            {
                                bool? value = ParseBoolean(text);

                                if (value == null)
                                {
                                    error = TraceOps.Trace(traceCallback, String.Format(
                                        "Invalid {0} boolean value: {1}",
                                        ForDisplay(arg), ForDisplay(text)),
                                        traceCategory);

                                    if (strict)
                                        return false;

                                    continue;
                                }

                                configuration.install = (bool)value;
                            }
                            else if (MatchOption(newArg, "whatIf"))
                            {
                                bool? value = ParseBoolean(text);

                                if (value == null)
                                {
                                    error = TraceOps.Trace(traceCallback, String.Format(
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
                                    error = TraceOps.Trace(traceCallback, String.Format(
                                        "Invalid {0} boolean value: {1}",
                                        ForDisplay(arg), ForDisplay(text)),
                                        traceCategory);

                                    if (strict)
                                        return false;

                                    continue;
                                }

                                configuration.verbose = (bool)value;
                            }
                            else if (MatchOption(newArg, "noDesktop"))
                            {
                                bool? value = ParseBoolean(text);

                                if (value == null)
                                {
                                    error = TraceOps.Trace(traceCallback, String.Format(
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
                                    error = TraceOps.Trace(traceCallback, String.Format(
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
                                    error = TraceOps.Trace(traceCallback, String.Format(
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
                                    error = TraceOps.Trace(traceCallback, String.Format(
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
                                    error = TraceOps.Trace(traceCallback, String.Format(
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
                                    error = TraceOps.Trace(traceCallback, String.Format(
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
                                    error = TraceOps.Trace(traceCallback, String.Format(
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
                                    error = TraceOps.Trace(traceCallback, String.Format(
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
                                    error = TraceOps.Trace(traceCallback, String.Format(
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
                                error = TraceOps.Trace(traceCallback, String.Format(
                                    "Unsupported command line option: {0}",
                                    ForDisplay(arg)), traceCategory);

                                if (strict)
                                    return false;
                            }
                        }
                        else
                        {
                            error = TraceOps.Trace(traceCallback, String.Format(
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
                    TraceOps.Trace(traceCallback, e, traceCategory);

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
                    configuration.Dump();

                    //
                    // NOTE: Show where we are running from and how we were
                    //       invoked.
                    //
                    string location = assembly.Location;

                    TraceOps.Trace(traceCallback, String.Format(
                        "Original command line is: {0}",
                        Environment.CommandLine), traceCategory);

                    //
                    // NOTE: If the debugger is attached and What-If mode is
                    //       [now] disabled, issue a warning.
                    //
                    if (!configuration.whatIf && Debugger.IsAttached)
                    {
                        TraceOps.Trace(traceCallback,
                            "Forced to disable \"what-if\" mode with " +
                            "debugger attached.", traceCategory);
                    }

                    return true;
                }
                catch (Exception e)
                {
                    TraceOps.Trace(traceCallback, e, traceCategory);

                    error = "Failed to process configuration.";
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

            ///////////////////////////////////////////////////////////////////////

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
                        "InstallFlags", ForDisplay(installFlags)),
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

            private InstallFlags installFlags;
            public InstallFlags InstallFlags
            {
                get { return installFlags; }
                set { installFlags = value; }
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
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Application Entry Point
        private static int Main(string[] args)
        {
            Configuration configuration = null;
            string error = null;

            ///////////////////////////////////////////////////////////////////

            #region Command Line Processing
            if (!Configuration.FromArgs(
                    args, true, ref configuration, ref error) ||
                !Configuration.Process(
                    args, configuration, true, ref error))
            {
                TraceOps.ShowMessage(
                    traceCallback, thisAssembly, error, traceCategory,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

                return 1;
            }

            ///////////////////////////////////////////////////////////////////

            InitializeAllFrameworks(configuration);
            InitializeAllVsVersions(configuration);
            #endregion

            ///////////////////////////////////////////////////////////////////

            AnyPair<string, bool> directoryPair = new AnyPair<string, bool>(
                configuration.Directory, configuration.Install);

            AnyPair<string, bool> fileNamePair = new AnyPair<string, bool>(
                configuration.DesignerFileName, configuration.Install);

            ///////////////////////////////////////////////////////////////////

            #region .NET GAC Install/Remove
            if (configuration.HasFlags(InstallFlags.GAC, true))
            {
                Publish publish = new Publish();

                if (configuration.Install)
                {
                    if (!configuration.WhatIf)
                    {
                        publish.GacInstall(configuration.CoreFileName); /* throw */
                        publish.GacInstall(configuration.LinqFileName); /* throw */
                    }
                    else
                    {
                        TraceOps.Trace(traceCallback, String.Format(
                            "GacInstall: assemblyPath = {0}",
                            configuration.CoreFileName), traceCategory);

                        TraceOps.Trace(traceCallback, String.Format(
                            "GacInstall: assemblyPath = {0}",
                            configuration.LinqFileName), traceCategory);
                    }
                }
                else
                {
                    if (!configuration.WhatIf)
                    {
                        publish.GacRemove(configuration.LinqFileName); /* throw */
                        publish.GacRemove(configuration.CoreFileName); /* throw */
                    }
                    else
                    {
                        TraceOps.Trace(traceCallback, String.Format(
                            "GacRemove: assemblyPath = {0}",
                            configuration.LinqFileName), traceCategory);

                        TraceOps.Trace(traceCallback, String.Format(
                            "GacRemove: assemblyPath = {0}",
                            configuration.CoreFileName), traceCategory);
                    }
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region .NET AssemblyFolders
            if (configuration.HasFlags(InstallFlags.AssemblyFolders, true))
            {
                if (!ForEachFrameworkRegistry(ProcessAssemblyFolders,
                        directoryPair, configuration.WhatIf,
                        configuration.Verbose, ref error))
                {
                    TraceOps.ShowMessage(
                        traceCallback, null, error, traceCategory,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return 1;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region .NET DbProviderFactory
            if (configuration.HasFlags(InstallFlags.DbProviderFactory, true))
            {
                bool saved = false;

                if (!ForEachFrameworkConfig(ProcessDbProviderFactory,
                        InvariantName, ProviderName, Description,
                        FactoryTypeName, AssemblyName.GetAssemblyName(
                            configuration.CoreFileName), directoryPair,
                        configuration.WhatIf, configuration.Verbose,
                        ref saved, ref error))
                {
                    TraceOps.ShowMessage(
                        traceCallback, null, error, traceCategory,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return 1;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region VS Package
            if (configuration.HasFlags(InstallFlags.VsPackage, true))
            {
                if (!ForEachVsVersionRegistry(ProcessVsPackage,
                        (Guid)vsPackageId, (Guid)vsServiceId,
                        (Guid)vsDataSourcesId, (Guid)vsDataProviderId,
                        fileNamePair, configuration.WhatIf,
                        configuration.Verbose, ref error))
                {
                    TraceOps.ShowMessage(
                        traceCallback, null, error, traceCategory,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return 1;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region VS DataSource
            if (configuration.HasFlags(InstallFlags.VsDataSource, true))
            {
                if (!ForEachVsVersionRegistry(ProcessVsDataSource,
                        (Guid)vsPackageId, (Guid)vsServiceId,
                        (Guid)vsDataSourcesId, (Guid)vsDataProviderId,
                        fileNamePair, configuration.WhatIf,
                        configuration.Verbose, ref error))
                {
                    TraceOps.ShowMessage(
                        traceCallback, null, error, traceCategory,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return 1;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region VS DataProvider
            if (configuration.HasFlags(InstallFlags.VsDataProvider, true))
            {
                if (!ForEachVsVersionRegistry(ProcessVsDataProvider,
                        (Guid)vsPackageId, (Guid)vsServiceId,
                        (Guid)vsDataSourcesId, (Guid)vsDataProviderId,
                        fileNamePair, configuration.WhatIf,
                        configuration.Verbose, ref error))
                {
                    TraceOps.ShowMessage(
                        traceCallback, null, error, traceCategory,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return 1;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            return 0;
        }
        #endregion
    }
    #endregion
}
