@ECHO OFF

::
:: release_ce.bat --
::
:: WinCE Binary Release Tool
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

SET RELEASE_CONFIGURATIONS=Release
SET BASE_CONFIGURATIONSUFFIX=Compact
SET PLATFORMS="Pocket PC 2003 (ARMV4)"
SET PROCESSORS=arm
SET YEARS=2008
SET BASE_PLATFORM=PocketPC
SET TYPE=binary

CALL :fn_ResetErrorLevel

%__ECHO3% CALL "%TOOLS%\release_all.bat"

IF ERRORLEVEL 1 (
  ECHO Failed to build PocketPC release files.
  GOTO errors
)

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
