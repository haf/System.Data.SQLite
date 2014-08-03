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
  SET PREARGS=-interactive -noExit -initialize

  IF DEFINED NOAUTOSELECT (
    %_AECHO% Skipping automatic build selection...
  ) ELSE (
    %_AECHO% Enabling automatic build selection...
    CALL :fn_AppendVariable PREARGS " -runtimeOption autoSelect"
  )
)

%_VECHO% PreArgs = '%PREARGS%'

IF NOT DEFINED TEST_FILE (
  %_AECHO% No test file specified, using default...
  SET TEST_FILE=Tests\empty.eagle
)

%_VECHO% TestFile = '%TEST_FILE%'

IF NOT DEFINED POSTARGS (
  %_AECHO% No post-arguments specified, using default...
  SET POSTARGS=-file "%TEST_FILE%"
)

%_VECHO% PostArgs = '%POSTARGS%'

IF NOT DEFINED 32BITONLY (
  SET EAGLESHELL=EagleShell.exe
) ELSE (
  SET EAGLESHELL=EagleShell32.exe
)

%_VECHO% EagleShell = '%EAGLESHELL%'

%_CECHO% "Externals\Eagle\bin\%EAGLESHELL%" %PREARGS% %* %POSTARGS%
%__ECHO% "Externals\Eagle\bin\%EAGLESHELL%" %PREARGS% %* %POSTARGS%

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

:fn_AppendVariable
  SET __ECHO_CMD=ECHO %%%1%%
  IF DEFINED %1 (
    FOR /F "delims=" %%V IN ('%__ECHO_CMD%') DO (
      SET %1=%%V%~2
    )
  ) ELSE (
    SET %1=%~2
  )
  SET __ECHO_CMD=
  CALL :fn_ResetErrorLevel
  GOTO :EOF

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
