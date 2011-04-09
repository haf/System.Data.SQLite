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

SET PATH=%ProgramFiles%\Inno Setup 5;%PATH%

%_VECHO% Path = '%PATH%'

%_ECHO% ISCC.exe SQLite.iss "/dAppId=%APPID%" "/dAppVersion=%VERSION%" "/dAppPublicKey=%PUBLICKEY%" "/dAppURL=%URL%" "/dIsNetFx2=%ISNETFX2%" "/dVcRuntime=%VCRUNTIME%" "/dAppPlatform=%PLATFORM%" "/dAppProcessor=%PROCESSOR%" "/dYear=%YEAR%"

IF %ERRORLEVEL% NEQ 0 (
  ECHO Failed to compile setup.
  GOTO errors
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
