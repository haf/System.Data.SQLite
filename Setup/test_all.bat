@ECHO OFF

::
:: test_all.bat --
::
:: Multiplexing Wrapper Tool for Unit Tests
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

SET DUMMY2=%1

IF DEFINED DUMMY2 (
  GOTO usage
)

REM SET DFLAGS=/L

%_VECHO% DFlags = '%DFLAGS%'

SET FFLAGS=/V /F /G /H /I /R /Y /Z

%_VECHO% FFlags = '%FFLAGS%'

SET ROOT=%~dp0\..
SET ROOT=%ROOT:\\=\%

%_VECHO% Root = '%ROOT%'

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

IF NOT DEFINED YEARS (
  SET YEARS=2008
)

%_VECHO% Years = '%YEARS%'

IF /I "%PROCESSOR_ARCHITECTURE%" == "x86" (
  SET PLATFORM=Win32
)

IF /I "%PROCESSOR_ARCHITECTURE%" == "AMD64" (
  SET PLATFORM=x64
)

IF NOT DEFINED PLATFORM (
  ECHO Unsupported platform.
  GOTO errors
)

%_VECHO% Platform = '%PLATFORM%'

%_ECHO% PUSHD "%ROOT%"

IF ERRORLEVEL 1 (
  ECHO Could not change directory to "%ROOT%".
  GOTO errors
)

FOR %%Y IN (%YEARS%) DO (
  %_ECHO% Externals\Eagle\bin\EagleShell.exe -preInitialize "set test_year {%%Y}" -file Tests\all.eagle

  IF ERRORLEVEL 1 (
    ECHO Testing of "%%Y" managed-only assembly failed.
    GOTO errors
  )

  %_ECHO% XCOPY "bin\%%Y\Release\bin\test.*" "bin\%%Y\%PLATFORM%\Release" %FFLAGS% %DFLAGS%

  IF ERRORLEVEL 1 (
    ECHO Failed to copy "bin\%%Y\Release\bin\test.*" to "bin\%%Y\%PLATFORM%\Release".
    GOTO errors
  )

  %_ECHO% XCOPY "bin\%%Y\Release\bin\System.Data.SQLite.Linq.*" "bin\%%Y\%PLATFORM%\Release" %FFLAGS% %DFLAGS%

  IF ERRORLEVEL 1 (
    ECHO Failed to copy "bin\%%Y\Release\bin\System.Data.SQLite.Linq.*" to "bin\%%Y\%PLATFORM%\Release".
    GOTO errors
  )

  %_ECHO% XCOPY "bin\%%Y\Release\bin\testlinq.*" "bin\%%Y\%PLATFORM%\Release" %FFLAGS% %DFLAGS%

  IF ERRORLEVEL 1 (
    ECHO Failed to copy "bin\%%Y\Release\bin\testlinq.*" to "bin\%%Y\%PLATFORM%\Release".
    GOTO errors
  )

  %_ECHO% XCOPY "bin\%%Y\Release\bin\northwindEF.db" "bin\%%Y\%PLATFORM%\Release" %FFLAGS% %DFLAGS%

  IF ERRORLEVEL 1 (
    ECHO Failed to copy "bin\%%Y\Release\bin\northwindEF.db" to "bin\%%Y\%PLATFORM%\Release".
    GOTO errors
  )

  %_ECHO% Externals\Eagle\bin\EagleShell.exe -preInitialize "set test_year {%%Y}" -initialize -runtimeOption native -file Tests\all.eagle

  IF ERRORLEVEL 1 (
    ECHO Testing of "%%Y" mixed-mode assembly failed.
    GOTO errors
  )
)

%_ECHO% POPD

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
  ECHO Usage: %~nx0
  GOTO errors

:errors
  CALL :fn_SetErrorLevel
  ENDLOCAL
  ECHO.
  ECHO Test failure, errors were encountered.
  GOTO end_of_file

:no_errors
  CALL :fn_ResetErrorLevel
  ENDLOCAL
  ECHO.
  ECHO Test success, no errors were encountered.
  GOTO end_of_file

:end_of_file
%_ECHO% EXIT /B %ERRORLEVEL%
