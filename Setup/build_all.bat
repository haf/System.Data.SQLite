@ECHO OFF

::
:: build_all.bat --
::
:: Multi-Wrapper Tool for MSBuild
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

CALL :fn_ResetErrorLevel

%_ECHO% CALL "%TOOLS%\vsSp.bat"

IF ERRORLEVEL 1 (
  ECHO Could not detect Visual Studio.
  GOTO errors
)

%_ECHO% CALL "%TOOLS%\set_common.bat"

IF ERRORLEVEL 1 (
  ECHO Could not set common variables.
  GOTO errors
)

IF NOT DEFINED BUILD_CONFIGURATIONS (
  SET BUILD_CONFIGURATIONS=Release
)

%_VECHO% BuildConfigurations = '%BUILD_CONFIGURATIONS%'

IF NOT DEFINED PLATFORMS (
  SET PLATFORMS=Win32
)

%_VECHO% Platforms = '%PLATFORMS%'

IF NOT DEFINED YEARS (
  SET YEARS=2008
)

%_VECHO% Years = '%YEARS%'

FOR %%C IN (%BUILD_CONFIGURATIONS%) DO (
  FOR %%P IN (%PLATFORMS%) DO (
    FOR %%Y IN (%YEARS%) DO (
      %_ECHO% CALL "%TOOLS%\set_%%Y.bat"

      IF ERRORLEVEL 1 (
        ECHO Could not set variables for %%Y.
        GOTO errors
      )

      %_ECHO% CALL "%TOOLS%\build.bat" %%C %%P

      IF ERRORLEVEL 1 (
        ECHO Could not build binaries for %%C/%%P/%%Y.
        GOTO errors
      )
    )
  )
)

GOTO no_errors

:fn_ResetErrorLevel
  VERIFY > NUL
  GOTO :EOF

:fn_SetErrorLevel
  VERIFY MAYBE 2> NUL
  GOTO :EOF

:errors
  CALL :fn_SetErrorLevel
  ENDLOCAL
  ECHO.
  ECHO Failure, errors were encountered.
  GOTO end_of_file

:no_errors
  CALL :fn_ResetErrorLevel
  ENDLOCAL
  ECHO.
  ECHO Success, no errors were encountered.
  GOTO end_of_file

:end_of_file
%_ECHO% EXIT /B %ERRORLEVEL%
