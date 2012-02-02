@ECHO OFF

::
:: bake_all.bat --
::
:: Multi-Setup Preparation & Baking Tool
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

CALL :fn_ResetErrorLevel

%__ECHO% CALL "%TOOLS%\vsSp.bat"

IF ERRORLEVEL 1 (
  ECHO Could not detect Visual Studio.
  GOTO errors
)

%__ECHO% CALL "%TOOLS%\set_common.bat"

IF ERRORLEVEL 1 (
  ECHO Could not set common variables.
  GOTO errors
)

IF NOT DEFINED BAKE_CONFIGURATIONS (
  SET BAKE_CONFIGURATIONS=Release
)

%_VECHO% BakeConfigurations = '%BAKE_CONFIGURATIONS%'

IF NOT DEFINED PROCESSORS (
  SET PROCESSORS=x86
)

%_VECHO% Processors = '%PROCESSORS%'

IF NOT DEFINED YEARS (
  SET YEARS=2008
)

%_VECHO% Years = '%YEARS%'

FOR %%C IN (%BAKE_CONFIGURATIONS%) DO (
  FOR %%P IN (%PROCESSORS%) DO (
    FOR %%Y IN (%YEARS%) DO (
      %__ECHO% CALL "%TOOLS%\set_%%C_%%P_%%Y.bat"

      IF ERRORLEVEL 1 (
        ECHO Could not set variables for %%C/%%P/%%Y.
        GOTO errors
      )

      %__ECHO% CALL "%TOOLS%\bake.bat"

      IF ERRORLEVEL 1 (
        ECHO Could not bake setup for %%C/%%P/%%Y.
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
%__ECHO% EXIT /B %ERRORLEVEL%
