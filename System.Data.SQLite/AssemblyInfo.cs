﻿using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Permissions;

#if !PLATFORM_COMPACTFRAMEWORK
using System.Runtime.ConstrainedExecution;
#endif

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("System.Data.SQLite")]
[assembly: AssemblyDescription("ADO.NET 2.0 Data Provider for SQLite")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("http://sqlite.phxsoftware.com")]
[assembly: AssemblyProduct("System.Data.SQLite")]
[assembly: AssemblyCopyright("Public Domain")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

#if PLATFORM_COMPACTFRAMEWORK
[assembly: AssemblyFlags(AssemblyNameFlags.Retargetable)]
#endif

//  Setting ComVisible to false makes the types in this assembly not visible 
//  to COM componenets.  If you need to access a type in this assembly from 
//  COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

[assembly: CLSCompliant(true)]

#if !PLATFORM_COMPACTFRAMEWORK
[assembly: ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers 
// by using the '*' as shown below:
[assembly: AssemblyVersion("1.0.28.0")]
#if !PLATFORM_COMPACTFRAMEWORK
[assembly: AssemblyFileVersion("1.0.28.0")]
#endif
