@ECHO OFF

::
:: test.bat --
::
:: Eagle Shell Testing Tool
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

SET ROOT=%~dp0\..
SET ROOT=%ROOT:\\=\%

%_VECHO% Root = '%ROOT%'

SET TOOLS=%~dp0
SET TOOLS=%TOOLS:~0,-1%

%_VECHO% Tools = '%TOOLS%'

CALL :fn_ResetErrorLevel

%__ECHO2% PUSHD "%ROOT%"

IF ERRORLEVEL 1 (
  ECHO Could not change directory to "%ROOT%".
  GOTO errors
)

IF NOT DEFINED PREARGS (
  %_AECHO% No pre-arguments specified, using default...
  SET PREARGS=-interactive -noExit
)

%_VECHO% PreArgs = '%PREARGS%'

IF NOT DEFINED POSTARGS (
  %_AECHO% No post-arguments specified, using default...
  SET POSTARGS=-file Tests\empty.eagle
)

%_VECHO% PostArgs = '%POSTARGS%'

%_CECHO% Externals\Eagle\bin\EagleShell.exe %PREARGS% %* %POSTARGS%
%__ECHO% Externals\Eagle\bin\EagleShell.exe %PREARGS% %* %POSTARGS%

IF ERRORLEVEL 1 (
  ECHO Received non-zero return code from the Eagle Shell.
  GOTO errors
)

%__ECHO2% POPD

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
  ECHO Usage: %~nx0 [...]
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
