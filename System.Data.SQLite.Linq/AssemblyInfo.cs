/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Runtime.ConstrainedExecution;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
#if USE_ENTITY_FRAMEWORK_6
[assembly: AssemblyTitle("System.Data.SQLite for Entity Framework 6")]
#else
[assembly: AssemblyTitle("System.Data.SQLite for LINQ")]
#endif

[assembly: AssemblyDescription("ADO.NET Data Provider for SQLite")]
[assembly: AssemblyCompany("http://system.data.sqlite.org/")]
[assembly: AssemblyProduct("System.Data.SQLite")]
[assembly: AssemblyCopyright("Public Domain")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]

#if !NET_40 && !NET_45 && !NET_451
[assembly: AllowPartiallyTrustedCallers]
#endif

[assembly: ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.94.0")]
[assembly: AssemblyFileVersion("1.0.94.0")]
