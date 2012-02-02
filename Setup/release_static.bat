@ECHO OFF

::
:: release_static.bat --
::
:: Static Binary Release Tool
::
:: Written by Joe Mistachkin.
:: Released to the public domain, use at your own risk!
::

SETLOCAL

REM SET __ECHO=ECHO
IF NOT DEFINED _AECHO (SET _AECHO=REM)
IF NOT DEFINED _CECHO (SET _CECHO=REM)
IF NOT DEFINED _VECHO (SET _VECHO=REM)

%_AECHO% Running %0 %*

SET TOOLS=%~dp0
SET TOOLS=%TOOLS:~0,-1%

%_VECHO% Tools = '%TOOLS%'

SET CONFIGURATIONSUFFIX=Static
SET TYPE_PREFIX=static-

CALL :fn_ResetErrorLevel

%__ECHO% CALL "%TOOLS%\release_all.bat"

IF ERRORLEVEL 1 (
  ECHO Failed to build static release files.
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
