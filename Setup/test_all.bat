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

REM SET __ECHO=ECHO
REM SET __ECHO2=ECHO
REM SET __ECHO3=ECHO
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

IF NOT DEFINED TEST_CONFIGURATIONS (
  SET TEST_CONFIGURATIONS=Release
)

%_VECHO% TestConfigurations = '%TEST_CONFIGURATIONS%'

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

IF NOT DEFINED YEARS (
  SET YEARS=2008
)

%_VECHO% Years = '%YEARS%'

IF NOT DEFINED TEST_FILE (
  SET TEST_FILE=Tests\all.eagle
)

%_VECHO% TestFile = '%TEST_FILE%'

%__ECHO2% PUSHD "%ROOT%"

IF ERRORLEVEL 1 (
  ECHO Could not change directory to "%ROOT%".
  GOTO errors
)

FOR %%C IN (%TEST_CONFIGURATIONS%) DO (
  FOR %%Y IN (%YEARS%) DO (
    IF NOT DEFINED NOMANAGEDONLY (
      %__ECHO% Externals\Eagle\bin\EagleShell.exe -preInitialize "set test_year {%%Y}; set test_configuration {%%C}" -file "%TEST_FILE%"

      IF ERRORLEVEL 1 (
        ECHO Testing of "%%Y/%%C" managed-only assembly failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NOMIXEDMODE (
      IF NOT DEFINED NOXCOPY (
        CALL :fn_CheckForLinq %%Y

        %__ECHO% XCOPY "bin\%%Y\%%C\bin\test.*" "bin\%%Y\%PLATFORM%\%%C" %FFLAGS% %DFLAGS%

        IF ERRORLEVEL 1 (
          ECHO Failed to copy "bin\%%Y\%%C\bin\test.*" to "bin\%%Y\%PLATFORM%\%%C".
          GOTO errors
        )

        IF DEFINED HAVE_LINQ (
          %__ECHO% XCOPY "bin\%%Y\%%C\bin\System.Data.SQLite.Linq.*" "bin\%%Y\%PLATFORM%\%%C" %FFLAGS% %DFLAGS%

          IF ERRORLEVEL 1 (
            ECHO Failed to copy "bin\%%Y\%%C\bin\System.Data.SQLite.Linq.*" to "bin\%%Y\%PLATFORM%\%%C".
            GOTO errors
          )

          %__ECHO% XCOPY "bin\%%Y\%%C\bin\testlinq.*" "bin\%%Y\%PLATFORM%\%%C" %FFLAGS% %DFLAGS%

          IF ERRORLEVEL 1 (
            ECHO Failed to copy "bin\%%Y\%%C\bin\testlinq.*" to "bin\%%Y\%PLATFORM%\%%C".
            GOTO errors
          )

          %__ECHO% XCOPY "bin\%%Y\%%C\bin\northwindEF.db" "bin\%%Y\%PLATFORM%\%%C" %FFLAGS% %DFLAGS%

          IF ERRORLEVEL 1 (
            ECHO Failed to copy "bin\%%Y\%%C\bin\northwindEF.db" to "bin\%%Y\%PLATFORM%\%%C".
            GOTO errors
          )
        )

        %__ECHO% XCOPY "bin\%%Y\%%C\bin\SQLite.Designer.*" "bin\%%Y\%PLATFORM%\%%C" %FFLAGS% %DFLAGS%

        IF ERRORLEVEL 1 (
          ECHO Failed to copy "bin\%%Y\%%C\bin\SQLite.Designer.*" to "bin\%%Y\%PLATFORM%\%%C".
          GOTO errors
        )

        %__ECHO% XCOPY "bin\%%Y\%%C\bin\Installer.*" "bin\%%Y\%PLATFORM%\%%C" %FFLAGS% %DFLAGS%

        IF ERRORLEVEL 1 (
          ECHO Failed to copy "bin\%%Y\%%C\bin\Installer.*" to "bin\%%Y\%PLATFORM%\%%C".
          GOTO errors
        )
      )

      %__ECHO% Externals\Eagle\bin\EagleShell.exe -preInitialize "set test_year {%%Y}; set test_configuration {%%C}" -initialize -runtimeOption native -file "%TEST_FILE%"

      IF ERRORLEVEL 1 (
        ECHO Testing of "%%Y/%%C" mixed-mode assembly failed.
        GOTO errors
      )
    )
  )
)

%__ECHO2% POPD

IF ERRORLEVEL 1 (
  ECHO Could not restore directory.
  GOTO errors
)

GOTO no_errors

:fn_CheckForLinq
  CALL :fn_UnsetVariable HAVE_LINQ
  IF /I "%1" == "2008" (
    SET HAVE_LINQ=1
  )
  IF /I "%1" == "2010" (
    SET HAVE_LINQ=1
  )
  IF /I "%1" == "2012" (
    SET HAVE_LINQ=1
  )
  GOTO :EOF

:fn_UnsetVariable
  IF NOT "%1" == "" (
    SET %1=
    CALL :fn_ResetErrorLevel
  )
  GOTO :EOF

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
%__ECHO% EXIT /B %ERRORLEVEL%
