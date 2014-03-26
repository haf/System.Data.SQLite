/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

using System.Reflection;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("System.Data.SQLite Tester")]
[assembly: AssemblyDescription("ADO.NET Data Provider for SQLite")]
[assembly: AssemblyCompany("http://system.data.sqlite.org/")]
[assembly: AssemblyProduct("System.Data.SQLite")]
[assembly: AssemblyCopyright("Public Domain")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

//  Setting ComVisible to false makes the types in this assembly not visible 
//  to COM componenets.  If you need to access a type in this assembly from 
//  COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("a6adbb45-c95f-44fb-bd47-f073e79c1d4b")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
[assembly: AssemblyVersion("1.0.93.0")]
[assembly: AssemblyFileVersion("1.0.93.0")]
