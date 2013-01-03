/*
 * AssemblyInfo.cpp -
 *
 * Written by Joe Mistachkin.
 * Released to the public domain, use at your own risk!
 */

using namespace System::Reflection;
using namespace System::Resources;
using namespace System::Runtime::InteropServices;

#include "interop.h"

[assembly:AssemblyTitleAttribute("SQLite.Interop")];
[assembly:AssemblyCompanyAttribute("http://system.data.sqlite.org/")];
[assembly:AssemblyDescriptionAttribute("System.Data.SQLite Interop Assembly")];
[assembly:AssemblyProductAttribute("System.Data.SQLite")];
[assembly:AssemblyCopyrightAttribute("Public Domain")];
[assembly:AssemblyVersionAttribute(INTEROP_VERSION)];
[assembly:AssemblyFileVersionAttribute(INTEROP_VERSION)];

#if DEBUG
[assembly:AssemblyConfiguration("Debug")];
#else
[assembly:AssemblyConfiguration("Release")];
#endif

[assembly:NeutralResourcesLanguage("en-US")];
[assembly:ComVisible(false)];
