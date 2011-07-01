@ECHO OFF

::
:: release.bat --
::
:: Binary Release Tool
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

SET YEAR=%3

IF DEFINED YEAR (
  SET YEAR=%YEAR:"=%
  REM "
) ELSE (
  %_AECHO% No year specified, using default...
  SET YEAR=2008
)

%_VECHO% Year = '%YEAR%'

SET BASE_CONFIGURATION=%CONFIGURATION:NativeOnly=%

%_VECHO% BaseConfiguration = '%BASE_CONFIGURATION%'

IF "%CONFIGURATION%" == "%BASE_CONFIGURATION%" (
  SET TYPE=binary-bundle
) ELSE (
  SET TYPE=binary
)

%_VECHO% Type = '%TYPE%'

SET ROOT=%~dp0\..
SET ROOT=%ROOT:\\=\%

SET TOOLS=%~dp0
SET TOOLS=%TOOLS:~0,-1%

%_VECHO% Root = '%ROOT%'
%_VECHO% Tools = '%TOOLS%'

CALL :fn_ResetErrorLevel

%_ECHO% PUSHD "%ROOT%"

IF ERRORLEVEL 1 (
  ECHO Could not change directory to "%ROOT%".
  GOTO errors
)

CALL :fn_ResetErrorLevel

%_ECHO% zip.exe -r Setup\Output\sqlite-dotnet-%TYPE%-%PLATFORM%-%YEAR%-%VERSION%.zip * -x@exclude_bin.txt

IF ERRORLEVEL 1 (
  ECHO Failed to archive binary files.
  GOTO errors
)

%_ECHO% POPD

IF ERRORLEVEL 1 (
  ECHO Could not restore directory.
  GOTO errors
)

GOTO no_errors

:fn_ResetErrorLevel
  VERIFY > NUL
  GOTO :EOF

:fn_SetErrorLevel
  VERIFY MAYBE 2> NUL
  GOTO :EOF

:usage
  ECHO.
  ECHO Usage: %~nx0 [configuration] [platform] [year]
  ECHO.
  GOTO errors

:errors
  CALL :fn_SetErrorLevel
  ENDLOCAL
  ECHO.
  ECHO Release failure, errors were encountered.
  GOTO end_of_file

:no_errors
  CALL :fn_ResetErrorLevel
  ENDLOCAL
  ECHO.
  ECHO Release success, no errors were encountered.
  GOTO end_of_file

:end_of_file
%_ECHO% EXIT /B %ERRORLEVEL%
