@ECHO OFF

::
:: set_ReleaseNativeOnly.bat --
::
:: Written by Joe Mistachkin.
:: Released to the public domain, use at your own risk!
::

REM
REM NOTE: Force usage of the Visual Studio 2008 (.NET Framework 3.5) build
REM       system.  This is very important because we want to ship binaries
REM       that only rely upon the .NET Framework 2.0 which is very widely
REM       deployed and because those binaries will also work with projects
REM       using the .NET Framework 4.0.
REM
SET NETFX35ONLY=1
SET YEAR=2008
SET YEARS=%YEAR%

ECHO WARNING: Forcing the use of the .NET Framework 3.5...
