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

SET PROCESSORS=x86 x64
SET YEARS=2008

SET TOOLS=%~dp0
SET TOOLS=%TOOLS:~0,-1%

CALL "%TOOLS%\set_common.bat"

IF ERRORLEVEL 1 (
  ECHO Could not set common variables.
  GOTO errors
)

FOR %%P IN (%PROCESSORS%) DO (
  FOR %%Y IN (%YEARS%) DO (
    CALL "%TOOLS%\set_%%P_%%Y.bat"

    IF ERRORLEVEL 1 (
      ECHO Could not set variables for %%P/%%Y.
      GOTO errors
    )

    CALL "%TOOLS%\bake.bat"

    IF ERRORLEVEL 1 (
      ECHO Could not bake setup for %%P/%%Y.
      GOTO errors
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
EXIT /B %ERRORLEVEL%
