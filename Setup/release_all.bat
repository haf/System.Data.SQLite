@ECHO OFF

::
:: release_all.bat --
::
:: Multi-Binary Release Tool
::
:: Written by Joe Mistachkin.
:: Released to the public domain, use at your own risk!
::

SETLOCAL

REM SET __ECHO=ECHO
REM SET __ECHO3=ECHO
IF NOT DEFINED _AECHO (SET _AECHO=REM)
IF NOT DEFINED _CECHO (SET _CECHO=REM)
IF NOT DEFINED _VECHO (SET _VECHO=REM)

%_AECHO% Running %0 %*

SET DUMMY2=%1

IF DEFINED DUMMY2 (
  GOTO usage
)

SET TOOLS=%~dp0
SET TOOLS=%TOOLS:~0,-1%

%_VECHO% Tools = '%TOOLS%'

CALL :fn_ResetErrorLevel

%__ECHO3% CALL "%TOOLS%\vsSp.bat"

IF ERRORLEVEL 1 (
  ECHO Could not detect Visual Studio.
  GOTO errors
)

%__ECHO3% CALL "%TOOLS%\set_common.bat"

IF ERRORLEVEL 1 (
  ECHO Could not set common variables.
  GOTO errors
)

IF NOT DEFINED RELEASE_CONFIGURATIONS (
  SET RELEASE_CONFIGURATIONS=Release
)

%_VECHO% ReleaseConfigurations = '%RELEASE_CONFIGURATIONS%'

IF NOT DEFINED PLATFORMS (
  SET PLATFORMS=Win32
)

%_VECHO% Platforms = '%PLATFORMS%'

IF NOT DEFINED YEARS (
  SET YEARS=2008
)

%_VECHO% Years = '%YEARS%'

FOR %%C IN (%RELEASE_CONFIGURATIONS%) DO (
  FOR %%P IN (%PLATFORMS%) DO (
    FOR %%Y IN (%YEARS%) DO (
      %__ECHO3% CALL "%TOOLS%\release.bat" %%C %%P %%Y

      IF ERRORLEVEL 1 (
        ECHO Could not build release archive for %%C/%%P/%%Y.
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

:usage
  ECHO.
  ECHO Usage: %~nx0
  ECHO.
  GOTO errors

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
%__ECHO% EXIT /B %ERRORLEVEL%
