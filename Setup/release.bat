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

REM SET __ECHO=ECHO
REM SET __ECHO2=ECHO
REM SET __ECHO3=ECHO
IF NOT DEFINED _AECHO (SET _AECHO=REM)
IF NOT DEFINED _CECHO (SET _CECHO=REM)
IF NOT DEFINED _VECHO (SET _VECHO=REM)

%_AECHO% Running %0 %*

SET DUMMY2=%4

IF DEFINED DUMMY2 (
  GOTO usage
)

SET TOOLS=%~dp0
SET TOOLS=%TOOLS:~0,-1%

%_VECHO% Tools = '%TOOLS%'

SET CONFIGURATION=%1

IF DEFINED CONFIGURATION (
  CALL :fn_UnquoteVariable CONFIGURATION
) ELSE (
  %_AECHO% No configuration specified, using default...
  SET CONFIGURATION=Release
)

%_VECHO% Configuration = '%CONFIGURATION%'
%_VECHO% ConfigurationSuffix = '%CONFIGURATIONSUFFIX%'

SET PLATFORM=%2

IF DEFINED PLATFORM (
  CALL :fn_UnquoteVariable PLATFORM
) ELSE (
  %_AECHO% No platform specified, using default...
  SET PLATFORM=Win32
)

%_VECHO% Platform = '%PLATFORM%'

SET YEAR=%3

IF DEFINED YEAR (
  CALL :fn_UnquoteVariable YEAR
) ELSE (
  %_AECHO% No year specified, using default...
  SET YEAR=2008
)

%_VECHO% Year = '%YEAR%'

SET BASE_CONFIGURATION=%CONFIGURATION%
SET BASE_CONFIGURATION=%BASE_CONFIGURATION:ManagedOnly=%
SET BASE_CONFIGURATION=%BASE_CONFIGURATION:NativeOnly=%

%_VECHO% BaseConfiguration = '%BASE_CONFIGURATION%'
%_VECHO% BaseConfigurationSuffix = '%BASE_CONFIGURATIONSUFFIX%'

IF NOT DEFINED BASE_PLATFORM (
  CALL :fn_SetVariable BASE_PLATFORM PLATFORM
)

%_VECHO% BasePlatform = '%BASE_PLATFORM%'

IF NOT DEFINED TYPE (
  IF /I "%CONFIGURATION%" == "%BASE_CONFIGURATION%" (
    SET TYPE=%TYPE_PREFIX%binary-bundle
  ) ELSE (
    SET TYPE=%TYPE_PREFIX%binary
  )
)

%_VECHO% Type = '%TYPE%'

CALL :fn_ResetErrorLevel

%__ECHO3% CALL "%TOOLS%\set_common.bat"

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

%__ECHO2% PUSHD "%ROOT%"

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

IF NOT EXIST Setup\Output (
  %__ECHO% MKDIR Setup\Output

  IF ERRORLEVEL 1 (
    ECHO Could not create directory "Setup\Output".
    GOTO errors
  )
)

IF DEFINED BASE_CONFIGURATIONSUFFIX (
  FOR /F "delims=" %%F IN ('DIR /B /S /AD "bin\%YEAR%\%BASE_CONFIGURATION%%BASE_CONFIGURATIONSUFFIX%\bin" 2^> NUL') DO (
    %__ECHO% RMDIR /S /Q "%%F"
  )
  %__ECHO% zip.exe -v -j -r "Setup\Output\sqlite-%FRAMEWORK%-%TYPE%-%BASE_PLATFORM%-%YEAR%-%VERSION%.zip" "bin\%YEAR%\%BASE_CONFIGURATION%%BASE_CONFIGURATIONSUFFIX%\bin" -x @exclude_bin.txt
) ELSE (
  FOR /F "delims=" %%F IN ('DIR /B /S /AD "bin\%YEAR%\%BASE_CONFIGURATION%\bin" 2^> NUL') DO (
    %__ECHO% RMDIR /S /Q "%%F"
  )
  %__ECHO% zip.exe -v -j -r "Setup\Output\sqlite-%FRAMEWORK%-%TYPE%-%BASE_PLATFORM%-%YEAR%-%VERSION%.zip" "bin\%YEAR%\%BASE_CONFIGURATION%\bin" -x @exclude_bin.txt
)

IF /I "%CONFIGURATION%" == "%BASE_CONFIGURATION%" (
  IF NOT DEFINED BASE_CONFIGURATIONSUFFIX (
    %__ECHO% zip -v -d "Setup\Output\sqlite-%FRAMEWORK%-%TYPE%-%BASE_PLATFORM%-%YEAR%-%VERSION%.zip" SQLite.Interop.*
  )
)

%__ECHO% zip.exe -v -j -r "Setup\Output\sqlite-%FRAMEWORK%-%TYPE%-%BASE_PLATFORM%-%YEAR%-%VERSION%.zip" "bin\%YEAR%\%PLATFORM%\%CONFIGURATION%%CONFIGURATIONSUFFIX%" -x @exclude_bin.txt

IF ERRORLEVEL 1 (
  ECHO Failed to archive binary files.
  GOTO errors
)

%__ECHO2% POPD

IF ERRORLEVEL 1 (
  ECHO Could not restore directory.
  GOTO errors
)

GOTO no_errors

:fn_SetVariable
  SETLOCAL
  SET __ECHO_CMD=ECHO %%%2%%
  FOR /F "delims=" %%V IN ('%__ECHO_CMD%') DO (
    SET VALUE=%%V
  )
  ENDLOCAL && (
    SET %1=%VALUE%
  )
  GOTO :EOF

:fn_UnquoteVariable
  SETLOCAL
  IF NOT DEFINED %1 GOTO :EOF
  SET __ECHO_CMD=ECHO %%%1%%
  FOR /F "delims=" %%V IN ('%__ECHO_CMD%') DO (
    SET VALUE=%%V
  )
  SET VALUE=%VALUE:"=%
  REM "
  ENDLOCAL && SET %1=%VALUE%
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
%__ECHO% EXIT /B %ERRORLEVEL%
