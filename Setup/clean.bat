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

REM SET __ECHO=ECHO
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

SET CLEANDIRS=bin obj Doc\Output Setup\Output
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
    %__ECHO% RMDIR /S /Q "%SOURCE%\%%D"

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

IF EXIST "%SOURCE%\*.cache" (
  REM
  REM NOTE: *WARNING* Deleting from the entire source tree.
  REM
  %__ECHO% DEL /S /Q "%SOURCE%\*.cache"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\*.cache".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\*.cache".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\*.cache" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\*.ncb" (
  REM
  REM NOTE: *WARNING* Deleting from the entire source tree.
  REM
  %__ECHO% DEL /S /Q "%SOURCE%\*.ncb"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\*.ncb".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\*.ncb".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\*.ncb" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\*.psess" (
  %__ECHO% DEL /Q "%SOURCE%\*.psess"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\*.psess".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\*.psess".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\*.psess" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\*.sdf" (
  %__ECHO% DEL /Q "%SOURCE%\*.sdf"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\*.sdf".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\*.sdf".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\*.sdf" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\*.suo" (
  REM
  REM NOTE: *WARNING* Unhiding in the entire source tree.
  REM
  %__ECHO% ATTRIB -H "%SOURCE%\*.suo" /S

  IF ERRORLEVEL 1 (
    ECHO Could not make "%SOURCE%\*.suo" visible.
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Made "%SOURCE%\*.suo" visible.
    %_AECHO%.
  )

  REM
  REM NOTE: *WARNING* Deleting from the entire source tree.
  REM
  %__ECHO% DEL /S /Q "%SOURCE%\*.suo"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\*.suo".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\*.suo".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\*.suo" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\*.vsp" (
  %__ECHO% DEL /Q "%SOURCE%\*.vsp"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\*.vsp".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\*.vsp".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\*.vsp" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\*.vsps" (
  %__ECHO% DEL /Q "%SOURCE%\*.vsps"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\*.vsps".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\*.vsps".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\*.vsps" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\*.nupkg" (
  %__ECHO% DEL /Q "%SOURCE%\*.nupkg"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\*.nupkg".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\*.nupkg".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\*.nupkg" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\Doc\SQLite.NET.chw" (
  %__ECHO% DEL /Q "%SOURCE%\Doc\SQLite.NET.chw"

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

IF EXIST "%SOURCE%\Externals\Eagle\bin\EntityFramework.*" (
  %__ECHO% DEL /Q "%SOURCE%\Externals\Eagle\bin\EntityFramework.*"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\Externals\Eagle\bin\EntityFramework.*".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\Externals\Eagle\bin\EntityFramework.*".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\Externals\Eagle\bin\EntityFramework.*" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\Externals\Eagle\bin\sqlite3.*" (
  %__ECHO% DEL /Q "%SOURCE%\Externals\Eagle\bin\sqlite3.*"

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

IF EXIST "%SOURCE%\Externals\Eagle\bin\Win32\sqlite3.*" (
  %__ECHO% DEL /Q "%SOURCE%\Externals\Eagle\bin\Win32\sqlite3.*"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\Externals\Eagle\bin\Win32\sqlite3.*".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\Externals\Eagle\bin\Win32\sqlite3.*".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\Externals\Eagle\bin\Win32\sqlite3.*" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\Externals\Eagle\bin\x86\sqlite3.*" (
  %__ECHO% DEL /Q "%SOURCE%\Externals\Eagle\bin\x86\sqlite3.*"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\Externals\Eagle\bin\x86\sqlite3.*".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\Externals\Eagle\bin\x86\sqlite3.*".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\Externals\Eagle\bin\x86\sqlite3.*" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\Externals\Eagle\bin\x64\sqlite3.*" (
  %__ECHO% DEL /Q "%SOURCE%\Externals\Eagle\bin\x64\sqlite3.*"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\Externals\Eagle\bin\x64\sqlite3.*".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\Externals\Eagle\bin\x64\sqlite3.*".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\Externals\Eagle\bin\x64\sqlite3.*" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\Externals\Eagle\bin\SQLite.Interop.*" (
  %__ECHO% DEL /Q "%SOURCE%\Externals\Eagle\bin\SQLite.Interop.*"

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

IF EXIST "%SOURCE%\Externals\Eagle\bin\Win32\SQLite.Interop.*" (
  %__ECHO% DEL /Q "%SOURCE%\Externals\Eagle\bin\Win32\SQLite.Interop.*"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\Externals\Eagle\bin\Win32\SQLite.Interop.*".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\Externals\Eagle\bin\Win32\SQLite.Interop.*".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\Externals\Eagle\bin\Win32\SQLite.Interop.*" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\Externals\Eagle\bin\x86\SQLite.Interop.*" (
  %__ECHO% DEL /Q "%SOURCE%\Externals\Eagle\bin\x86\SQLite.Interop.*"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\Externals\Eagle\bin\x86\SQLite.Interop.*".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\Externals\Eagle\bin\x86\SQLite.Interop.*".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\Externals\Eagle\bin\x86\SQLite.Interop.*" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\Externals\Eagle\bin\x64\SQLite.Interop.*" (
  %__ECHO% DEL /Q "%SOURCE%\Externals\Eagle\bin\x64\SQLite.Interop.*"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\Externals\Eagle\bin\x64\SQLite.Interop.*".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\Externals\Eagle\bin\x64\SQLite.Interop.*".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\Externals\Eagle\bin\x64\SQLite.Interop.*" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\Externals\Eagle\bin\System.Data.SQLite.*" (
  %__ECHO% DEL /Q "%SOURCE%\Externals\Eagle\bin\System.Data.SQLite.*"

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
  %__ECHO% DEL /Q "%SOURCE%\Externals\Eagle\bin\System.Data.SQLite.Linq.*"

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

IF EXIST "%SOURCE%\Externals\Eagle\bin\System.Data.SQLite.EF6.*" (
  %__ECHO% DEL /Q "%SOURCE%\Externals\Eagle\bin\System.Data.SQLite.EF6.*"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\Externals\Eagle\bin\System.Data.SQLite.EF6.*".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\Externals\Eagle\bin\System.Data.SQLite.EF6.*".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\Externals\Eagle\bin\System.Data.SQLite.EF6.*" exist.
  %_AECHO%.
)

IF EXIST "%TEMP%\EagleShell.exe.test.*.log" (
  %__ECHO% DEL /Q "%TEMP%\EagleShell.exe.test.*.log"

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

IF EXIST "%TEMP%\EagleShell32.exe.test.*.log" (
  %__ECHO% DEL /Q "%TEMP%\EagleShell32.exe.test.*.log"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%TEMP%\EagleShell32.exe.test.*.log".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%TEMP%\EagleShell32.exe.test.*.log".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%TEMP%\EagleShell32.exe.test.*.log" exist.
  %_AECHO%.
)

IF EXIST "%TEMP%\mono.exe.test.*.log" (
  %__ECHO% DEL /Q "%TEMP%\mono.exe.test.*.log"

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
  %__ECHO% DEL /Q "%TEMP%\tclsh*.exe.test.*.log"

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
%__ECHO% EXIT /B %ERRORLEVEL%
