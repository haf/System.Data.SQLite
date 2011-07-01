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

SET TOOLS=%~dp0
SET TOOLS=%TOOLS:~0,-1%

%_VECHO% Tools = '%TOOLS%'

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

%_ECHO% CALL "%TOOLS%\set_common.bat"

IF ERRORLEVEL 1 (
  ECHO Could not set common variables.
  GOTO errors
)

IF NOT DEFINED FRAMEWORK (
  IF DEFINED YEAR (
    CALL :fn_SetVariable FRAMEWORK FRAMEWORK%YEAR%
  ) ELSE (
    SET FRAMEWORK=netFx20
  )
)

%_VECHO% Framework = '%FRAMEWORK%'

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

FOR /F "delims=" %%V IN ('find.exe "AssemblyVersion" System.Data.SQLite\AssemblyInfo.cs') DO (
  SET VERSION=%%V
)

IF NOT DEFINED VERSION (
  SET VERSION=1.0.0.0
  GOTO skip_mungeVersion
)

REM
REM NOTE: Strip off all the extra stuff from the AssemblyVersion line we found
REM       in the AssemblyInfo.cs file that we do not need (i.e. everything
REM       except the raw version number itself).
REM
SET VERSION=%VERSION:(=%
SET VERSION=%VERSION:)=%
SET VERSION=%VERSION:[=%
SET VERSION=%VERSION:]=%
SET VERSION=%VERSION: =%
SET VERSION=%VERSION:assembly:=%
SET VERSION=%VERSION:AssemblyVersion=%
SET VERSION=%VERSION:"=%
REM "

:skip_mungeVersion

%_VECHO% Version = '%VERSION%'

CALL :fn_ResetErrorLevel

%_ECHO% zip.exe -j -r "Setup\Output\sqlite-%FRAMEWORK%-%TYPE%-%PLATFORM%-%YEAR%-%VERSION%.zip" "bin\%YEAR%\%BASE_CONFIGURATION%\bin" -x@exclude_bin.txt
%_ECHO% zip.exe -j -r "Setup\Output\sqlite-%FRAMEWORK%-%TYPE%-%PLATFORM%-%YEAR%-%VERSION%.zip" "bin\%YEAR%\%PLATFORM%\%CONFIGURATION%" -x@exclude_bin.txt

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

:fn_SetVariable
  SETLOCAL
  SET _ECHO_CMD=ECHO %%%2%%
  FOR /F %%V IN ('%_ECHO_CMD%') DO (
    SET VALUE=%%V
  )
  ENDLOCAL && (
    SET %1=%VALUE%
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
