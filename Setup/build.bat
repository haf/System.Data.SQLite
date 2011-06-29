@ECHO OFF

::
:: build.bat --
::
:: MSBuild Wrapper Tool
::
:: Written by Joe Mistachkin.
:: Released to the public domain, use at your own risk!
::

SETLOCAL

REM SET _ECHO=ECHO
IF NOT DEFINED _AECHO (SET _AECHO=REM)
IF NOT DEFINED _CECHO (SET _CECHO=REM)
IF NOT DEFINED _VECHO (SET _VECHO=REM)

%_AECHO% Running %0 %*

SET ROOT=%~dp0\..
SET ROOT=%ROOT:\\=\%

%_VECHO% Root = '%ROOT%'

SET CONFIGURATION=%1

IF DEFINED CONFIGURATION (
  SET CONFIGURATION=%CONFIGURATION:"=%
  REM "
) ELSE (
  %_AECHO% No configuration specified, using default...
  SET CONFIGURATION=Release
)

%_VECHO% Configuration = '%CONFIGURATION%'

SET PLATFORM=%2

IF DEFINED PLATFORM (
  SET PLATFORM=%PLATFORM:"=%
  REM "
) ELSE (
  %_AECHO% No platform specified, using default...
  SET PLATFORM=Win32
)

%_VECHO% Platform = '%PLATFORM%'

SET TOOLS=%~dp0
SET TOOLS=%TOOLS:\\=\%

%_VECHO% Tools = '%TOOLS%'

IF EXIST "%TOOLS%\set_%CONFIGURATION%_%PLATFORM%.bat" (
  %_AECHO% Running "%TOOLS%\set_%CONFIGURATION%_%PLATFORM%.bat"...
  %_ECHO% CALL "%TOOLS%\set_%CONFIGURATION%_%PLATFORM%.bat"

  IF ERRORLEVEL 1 (
    ECHO File "%TOOLS%\set_%CONFIGURATION%_%PLATFORM%.bat" failed.
    GOTO errors
  )
)

IF DEFINED NETFX20ONLY (
  %_AECHO% Forcing the use of the .NET Framework 2.0...
  SET YEAR=2005
  SET FRAMEWORKDIR=%windir%\Microsoft.NET\Framework\v2.0.50727
  GOTO skip_netFxCheck
)

IF DEFINED NETFX35ONLY (
  %_AECHO% Forcing the use of the .NET Framework 3.5...
  SET YEAR=2008
  SET FRAMEWORKDIR=%windir%\Microsoft.NET\Framework\v3.5
  GOTO skip_netFxCheck
)

IF DEFINED NETFX40ONLY (
  %_AECHO% Forcing the use of the .NET Framework 4.0...
  SET YEAR=2010
  SET FRAMEWORKDIR=%windir%\Microsoft.NET\Framework\v4.0.30319
  GOTO skip_netFxCheck
)

IF NOT DEFINED FRAMEWORKDIR (
  %_AECHO% Checking for the .NET Framework 4.0...
  SET YEAR=2010
  SET FRAMEWORKDIR=%windir%\Microsoft.NET\Framework\v4.0.30319
)

IF NOT EXIST "%FRAMEWORKDIR%" (
  %_AECHO% Checking for the .NET Framework 3.5...
  SET YEAR=2008
  SET FRAMEWORKDIR=%windir%\Microsoft.NET\Framework\v3.5
)

IF NOT EXIST "%FRAMEWORKDIR%" (
  %_AECHO% Checking for the .NET Framework 2.0...
  SET YEAR=2005
  SET FRAMEWORKDIR=%windir%\Microsoft.NET\Framework\v2.0.50727
)

:skip_netFxCheck

IF NOT EXIST "%FRAMEWORKDIR%" (
  ECHO.
  ECHO The .NET Framework directory "%FRAMEWORKDIR%" was not found.
  ECHO.
  ECHO Please install the .NET Framework or set the "FRAMEWORKDIR"
  ECHO environment variable to the location where it is installed.
  ECHO.
  GOTO errors
)

%_VECHO% Year = '%YEAR%'
%_VECHO% FrameworkDir = '%FRAMEWORKDIR%'

CALL :fn_ResetErrorLevel

%_ECHO% PUSHD "%ROOT%"

IF ERRORLEVEL 1 (
  ECHO Could not change directory to "%ROOT%".
  GOTO errors
)

SET PATH=%FRAMEWORKDIR%;%PATH%

%_VECHO% Path = '%PATH%'

IF NOT DEFINED SOLUTION (
  %_AECHO% Building all projects...
  SET SOLUTION=.\SQLite.NET.%YEAR%.MSBuild.sln
)

IF NOT EXIST "%SOLUTION%" (
  %_AECHO% Building all projects...
  SET SOLUTION=.\SQLite.NET.%YEAR%.sln
)

%_VECHO% Solution = '%SOLUTION%'

IF NOT DEFINED TARGET (
  SET TARGET=Rebuild
)

%_VECHO% Target = '%TARGET%'

IF NOT DEFINED TEMP (
  ECHO Temporary directory must be defined.
  GOTO errors
)

%_VECHO% Temp = '%TEMP%'

IF NOT DEFINED LOGDIR (
  SET LOGDIR=%TEMP%
)

%_VECHO% LogDir = '%LOGDIR%'

IF NOT DEFINED LOGPREFIX (
  SET LOGPREFIX=System.Data.SQLite.Build
)

%_VECHO% LogPrefix = '%LOGPREFIX%'

IF NOT DEFINED LOGSUFFIX (
  SET LOGSUFFIX=Unknown
)

%_VECHO% LogSuffix = '%LOGSUFFIX%'

IF DEFINED LOGGING GOTO skip_setLogging
IF DEFINED NOLOG GOTO skip_setLogging

SET LOGGING="/logger:FileLogger,Microsoft.Build.Engine;Logfile=%LOGDIR%\%LOGPREFIX%_%CONFIGURATION%_%PLATFORM%_%LOGSUFFIX%.log;Verbosity=diagnostic"

:skip_setLogging

%_VECHO% Logging = '%LOGGING%'

%_ECHO% MSBuild.exe "%SOLUTION%" "/target:%TARGET%" "/property:Configuration=%CONFIGURATION%" "/property:Platform=%PLATFORM%" %LOGGING% %MSBUILD_ARGS%

IF ERRORLEVEL 1 (
  ECHO Build failed.
  GOTO errors
)

%_ECHO% POPD

IF ERRORLEVEL 1 (
  ECHO Could not restore directory.
  GOTO errors
)

GOTO no_errors

:fn_UnsetVariable
  IF NOT "%1" == "" (
    SET %1=
    CALL :fn_ResetErrorLevel
  )
  GOTO :EOF

:fn_ResetErrorLevel
  VERIFY > NUL
  GOTO :EOF

:fn_SetErrorLevel
  VERIFY MAYBE 2> NUL
  GOTO :EOF

:usage
  ECHO.
  ECHO Usage: %~nx0 [configuration] [platform] [...]
  ECHO.
  GOTO errors

:errors
  CALL :fn_SetErrorLevel
  ENDLOCAL
  ECHO.
  ECHO Build failure, errors were encountered.
  GOTO end_of_file

:no_errors
  CALL :fn_ResetErrorLevel
  ENDLOCAL
  ECHO.
  ECHO Build success, no errors were encountered.
  GOTO end_of_file

:end_of_file
%_ECHO% EXIT /B %ERRORLEVEL%
