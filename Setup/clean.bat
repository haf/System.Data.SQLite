@ECHO OFF

::
:: clean.bat --
::
:: Build Cleaning Tool
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

SET SOURCE=%~dp0\..
SET SOURCE=%SOURCE:\\=\%

%_VECHO% Source = '%SOURCE%'
%_VECHO% Temp = '%TEMP%'

IF NOT DEFINED TEMP (
  ECHO The TEMP environment variable must be set first.
  GOTO usage
)

IF NOT EXIST "%TEMP%" (
  ECHO The TEMP directory, "%TEMP%", does not exist.
  GOTO usage
)

IF DEFINED CLEANDIRS GOTO skip_cleanDirs

SET CLEANDIRS=bin obj Doc\Output Membership\bin Membership\obj Setup\Output
SET CLEANDIRS=%CLEANDIRS% SQLite.Designer\bin SQLite.Designer\obj
SET CLEANDIRS=%CLEANDIRS% SQLite.Interop\bin SQLite.Interop\obj
SET CLEANDIRS=%CLEANDIRS% System.Data.SQLite\bin System.Data.SQLite\obj
SET CLEANDIRS=%CLEANDIRS% System.Data.SQLite.Linq\bin System.Data.SQLite.Linq\obj
SET CLEANDIRS=%CLEANDIRS% test\bin test\obj testce\bin testce\obj testlinq\bin
SET CLEANDIRS=%CLEANDIRS% testlinq\obj tools\install\bin tools\install\obj

:skip_cleanDirs

%_VECHO% CleanDirs = '%CLEANDIRS%'

CALL :fn_ResetErrorLevel

%_AECHO%.

FOR %%D IN (%CLEANDIRS%) DO (
  IF EXIST "%SOURCE%\%%D" (
    %_ECHO% RMDIR /S /Q "%SOURCE%\%%D"

    IF ERRORLEVEL 1 (
      ECHO Could not remove directory "%SOURCE%\%%D".
      ECHO.
      GOTO errors
    ) ELSE (
      %_AECHO% Removed directory "%SOURCE%\%%D".
      %_AECHO%.
    )
  ) ELSE (
    %_AECHO% Directory "%SOURCE%\%%D" does not exist.
    %_AECHO%.
  )
)

IF EXIST "%SOURCE%\Doc\SQLite.NET.chw" (
  %_ECHO% DEL /Q "%SOURCE%\Doc\SQLite.NET.chw"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\Doc\SQLite.NET.chw".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\Doc\SQLite.NET.chw".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\Doc\SQLite.NET.chw" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\Externals\Eagle\bin\sqlite3.*" (
  %_ECHO% DEL /Q "%SOURCE%\Externals\Eagle\bin\sqlite3.*"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\Externals\Eagle\bin\sqlite3.*".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\Externals\Eagle\bin\sqlite3.*".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\Externals\Eagle\bin\sqlite3.*" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\Externals\Eagle\bin\SQLite.Interop.*" (
  %_ECHO% DEL /Q "%SOURCE%\Externals\Eagle\bin\SQLite.Interop.*"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\Externals\Eagle\bin\SQLite.Interop.*".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\Externals\Eagle\bin\SQLite.Interop.*".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\Externals\Eagle\bin\SQLite.Interop.*" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\Externals\Eagle\bin\System.Data.SQLite.*" (
  %_ECHO% DEL /Q "%SOURCE%\Externals\Eagle\bin\System.Data.SQLite.*"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\Externals\Eagle\bin\System.Data.SQLite.*".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\Externals\Eagle\bin\System.Data.SQLite.*".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\Externals\Eagle\bin\System.Data.SQLite.*" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\Externals\Eagle\bin\System.Data.SQLite.Linq.*" (
  %_ECHO% DEL /Q "%SOURCE%\Externals\Eagle\bin\System.Data.SQLite.Linq.*"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\Externals\Eagle\bin\System.Data.SQLite.Linq.*".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\Externals\Eagle\bin\System.Data.SQLite.Linq.*".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\Externals\Eagle\bin\System.Data.SQLite.Linq.*" exist.
  %_AECHO%.
)

IF EXIST "%TEMP%\EagleShell.exe.test.*.log" (
  %_ECHO% DEL /Q "%TEMP%\EagleShell.exe.test.*.log"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%TEMP%\EagleShell.exe.test.*.log".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%TEMP%\EagleShell.exe.test.*.log".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%TEMP%\EagleShell.exe.test.*.log" exist.
  %_AECHO%.
)

IF EXIST "%TEMP%\mono.exe.test.*.log" (
  %_ECHO% DEL /Q "%TEMP%\mono.exe.test.*.log"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%TEMP%\mono.exe.test.*.log".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%TEMP%\mono.exe.test.*.log".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%TEMP%\mono.exe.test.*.log" exist.
  %_AECHO%.
)

IF EXIST "%TEMP%\tclsh*.exe.test.*.log" (
  %_ECHO% DEL /Q "%TEMP%\tclsh*.exe.test.*.log"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%TEMP%\tclsh*.exe.test.*.log".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%TEMP%\tclsh*.exe.test.*.log".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%TEMP%\tclsh*.exe.test.*.log" exist.
  %_AECHO%.
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
  ECHO The TEMP environment variable must be set to the full path of the existing
  ECHO directory used to store temporary files.
  GOTO errors

:errors
  CALL :fn_SetErrorLevel
  ENDLOCAL
  ECHO.
  ECHO Clean failure, errors were encountered.
  GOTO end_of_file

:no_errors
  CALL :fn_ResetErrorLevel
  ENDLOCAL
  ECHO.
  ECHO Clean success, no errors were encountered.
  GOTO end_of_file

:end_of_file
%_ECHO% EXIT /B %ERRORLEVEL%
