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

:redo

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

IF DEFINED PLATFORM (
  %_AECHO% Skipping platform detection, already set...
  GOTO skip_detectPlatform
)

IF /I "%PROCESSOR_ARCHITECTURE%" == "x86" (
  SET PLATFORM=Win32
)

IF /I "%PROCESSOR_ARCHITECTURE%" == "AMD64" (
  SET PLATFORM=x64
)

:skip_detectPlatform

IF NOT DEFINED PLATFORM (
  ECHO Unsupported platform.
  GOTO errors
)

%_VECHO% Platform = '%PLATFORM%'

IF NOT DEFINED YEARS (
  SET YEARS=2008
)

%_VECHO% Years = '%YEARS%'
%_VECHO% PreArgs = '%PREARGS%'

IF NOT DEFINED TEST_FILE (
  SET TEST_FILE=Tests\all.eagle
)

%_VECHO% TestFile = '%TEST_FILE%'
%_VECHO% PostArgs = '%POSTARGS%'

IF NOT DEFINED 32BITONLY (
  SET EAGLESHELL=EagleShell.exe
) ELSE (
  SET EAGLESHELL=EagleShell32.exe
)

%_VECHO% EagleShell = '%EAGLESHELL%'

REM
REM NOTE: Set an environment variable that can be used by the test suite to
REM       determine that testing is being performed in batch mode.
REM
REM HACK: If the SKIPMAIN environment variable is set, completely skip the
REM       main loop.  This allows callers to skip 64-bit testing and proceed
REM       directly to 32-bit testing.
REM
IF NOT DEFINED SKIPMAIN (
  %__ECHO2% PUSHD "%ROOT%"

  IF ERRORLEVEL 1 (
    ECHO Could not change directory to "%ROOT%".
    GOTO errors
  )

  SET TEST_ALL=1

  FOR %%C IN (%TEST_CONFIGURATIONS%) DO (
    FOR %%Y IN (%YEARS%) DO (
      IF EXIST "bin\%%Y\%%C\bin" (
        IF DEFINED 32BITONLY (
          %__ECHO% DEL /Q "bin\%%Y\%%C\bin\SQLite.Interop.*"

          IF ERRORLEVEL 1 (
            ECHO Failed to delete "bin\%%Y\%%C\bin\SQLite.Interop.*".
            GOTO errors
          )
        )

        IF NOT DEFINED NOMANAGEDONLY (
          %__ECHO% "Externals\Eagle\bin\%EAGLESHELL%" %PREARGS% -anyInitialize "set test_year {%%Y}; set test_configuration {%%C}" -file "%TEST_FILE%" %POSTARGS%

          IF ERRORLEVEL 1 (
            ECHO Testing of "%%Y/%%C" managed-only assembly failed.
            GOTO errors
          )
        )

        IF EXIST "bin\%%Y\%PLATFORM%\%%C" (
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

              IF DEFINED HAVE_EF6 (
                %__ECHO% XCOPY "bin\%%Y\%%C\bin\EntityFramework.*" "bin\%%Y\%PLATFORM%\%%C" %FFLAGS% %DFLAGS%

                IF ERRORLEVEL 1 (
                  ECHO Failed to copy "bin\%%Y\%%C\bin\EntityFramework.*" to "bin\%%Y\%PLATFORM%\%%C".
                  GOTO errors
                )

                %__ECHO% XCOPY "bin\%%Y\%%C\bin\System.Data.SQLite.EF6.*" "bin\%%Y\%PLATFORM%\%%C" %FFLAGS% %DFLAGS%

                IF ERRORLEVEL 1 (
                  ECHO Failed to copy "bin\%%Y\%%C\bin\System.Data.SQLite.EF6.*" to "bin\%%Y\%PLATFORM%\%%C".
                  GOTO errors
                )

                %__ECHO% XCOPY "bin\%%Y\%%C\bin\testef6.*" "bin\%%Y\%PLATFORM%\%%C" %FFLAGS% %DFLAGS%

                IF ERRORLEVEL 1 (
                  ECHO Failed to copy "bin\%%Y\%%C\bin\testef6.*" to "bin\%%Y\%PLATFORM%\%%C".
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

            %__ECHO% "Externals\Eagle\bin\%EAGLESHELL%" %PREARGS% -preInitialize "set test_year {%%Y}; set test_configuration {%%C}" -initialize -runtimeOption native -file "%TEST_FILE%" %POSTARGS%

            IF ERRORLEVEL 1 (
              ECHO Testing of "%%Y/%%C" mixed-mode assembly failed.
              GOTO errors
            )
          )
        ) ELSE (
          %_AECHO% Native directory "bin\%%Y\%PLATFORM%\%%C" not found, skipped.
        )
      ) ELSE (
        %_AECHO% Managed directory "bin\%%Y\%%C\bin" not found, skipped.
      )
    )
  )

  %__ECHO2% POPD

  IF ERRORLEVEL 1 (
    ECHO Could not restore directory.
    GOTO errors
  )
)

REM
REM NOTE: If this is a 64-bit machine and we have not already run the 32-bit
REM       tests, do so now, unless we are forbidden from doing so.
REM
IF NOT DEFINED SKIP32BITONLY (
  IF NOT DEFINED 32BITONLY (
    IF /I NOT "%PROCESSOR_ARCHITECTURE%" == "x86" (
      REM
      REM HACK: Always unset the SKIPMAIN environment variable so the primary
      REM       loop will run when only the 32-bit binaries are being tested.
      REM
      CALL :fn_UnsetVariable SKIPMAIN

      SET PLATFORM=Win32
      SET 32BITONLY=1
      GOTO redo
    )
  )
)

GOTO no_errors

:fn_CheckForLinq
  CALL :fn_UnsetVariable HAVE_LINQ
  CALL :fn_UnsetVariable HAVE_EF6
  IF /I "%1" == "2008" (
    SET HAVE_LINQ=1
  )
  IF /I "%1" == "2010" (
    SET HAVE_LINQ=1
    SET HAVE_EF6=1
  )
  IF /I "%1" == "2012" (
    SET HAVE_LINQ=1
    SET HAVE_EF6=1
  )
  IF /I "%1" == "2013" (
    SET HAVE_LINQ=1
    SET HAVE_EF6=1
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
