using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Resources;

#if !PLATFORM_COMPACTFRAMEWORK
using System.Security;
using System.Runtime.ConstrainedExecution;
#endif

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("System.Data.SQLite Core")]
[assembly: AssemblyDescription("ADO.NET Data Provider for SQLite")]
[assembly: AssemblyCompany("http://system.data.sqlite.org/")]
[assembly: AssemblyProduct("System.Data.SQLite")]
[assembly: AssemblyCopyright("Public Domain")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

#if PLATFORM_COMPACTFRAMEWORK && RETARGETABLE
[assembly: AssemblyFlags(AssemblyNameFlags.Retargetable)]
#endif

//  Setting ComVisible to false makes the types in this assembly not visible 
//  to COM componenets.  If you need to access a type in this assembly from 
//  COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]
[assembly: InternalsVisibleTo("System.Data.SQLite.Linq, PublicKey=002400000480000094000000060200000024000052534131000400000100010005a288de5687c4e1b621ddff5d844727418956997f475eb829429e411aff3e93f97b70de698b972640925bdd44280df0a25a843266973704137cbb0e7441c1fe7cae4e2440ae91ab8cde3933febcb1ac48dd33b40e13c421d8215c18a4349a436dd499e3c385cc683015f886f6c10bd90115eb2bd61b67750839e3a19941dc9c")]
[assembly: NeutralResourcesLanguage("en")]

#if !PLATFORM_COMPACTFRAMEWORK
[assembly: AllowPartiallyTrustedCallers]
[assembly: ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]

#if !NET_20
//
// NOTE: This attribute is only available in .NET Framework 4.0 or higher.
//
[assembly: SecurityRules(System.Security.SecurityRuleSet.Level1)]
#endif
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
[assembly: AssemblyVersion("1.0.77.0")]
#if !PLATFORM_COMPACTFRAMEWORK
[assembly: AssemblyFileVersion("1.0.77.0")]
#endif
