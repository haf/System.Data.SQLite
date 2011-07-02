@ECHO OFF

::
:: bake.bat --
::
:: Setup Preparation & Baking Tool
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

SET TOOLS=%~dp0
SET TOOLS=%TOOLS:~0,-1%

%_VECHO% Tools = '%TOOLS%'

IF NOT DEFINED FRAMEWORK (
  IF DEFINED YEAR (
    CALL :fn_SetVariable FRAMEWORK FRAMEWORK%YEAR%
  ) ELSE (
    SET FRAMEWORK=netFx20
  )
)

%_VECHO% Framework = '%FRAMEWORK%'

IF "%PROCESSOR_ARCHITECTURE%"=="x86" GOTO set_path_32
SET PATH=%ProgramFiles(x86)%\Inno Setup 5;%PATH%
GOTO set_path_done
:set_path_32
SET PATH=%ProgramFiles%\Inno Setup 5;%PATH%
:set_path_done

%_VECHO% Path = '%PATH%'

%_ECHO% ISCC.exe "%TOOLS%\SQLite.iss" "/dAppId=%APPID%" "/dAppPublicKey=%PUBLICKEY%" "/dAppURL=%URL%" "/dIsNetFx2=%ISNETFX2%" "/dVcRuntime=%VCRUNTIME%" "/dAppConfiguration=%CONFIGURATION%" "/dAppPlatform=%PLATFORM%" "/dAppProcessor=%PROCESSOR%" "/dFramework=%FRAMEWORK%" "/dYear=%YEAR%"

IF %ERRORLEVEL% NEQ 0 (
  ECHO Failed to compile setup.
  GOTO errors
)

GOTO no_errors

:fn_SetVariable
  SETLOCAL
  SET _ECHO_CMD=ECHO %%%2%%
  FOR /F "delims=" %%V IN ('%_ECHO_CMD%') DO (
    SET VALUE=%%V
  )
  ENDLOCAL && (
    SET %1=%VALUE%
  )
  GOTO :EOF

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
  ECHO Bake failure, errors were encountered.
  GOTO end_of_file

:no_errors
  CALL :fn_ResetErrorLevel
  ENDLOCAL
  ECHO.
  ECHO Bake success, no errors were encountered.
  GOTO end_of_file

:end_of_file
%_ECHO% EXIT /B %ERRORLEVEL%
