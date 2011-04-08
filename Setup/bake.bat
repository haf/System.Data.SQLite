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

SET PATH=%ProgramFiles%\Inno Setup 5;%PATH%

ISCC.exe SQLite.iss "/dAppId=%APPID%" "/dAppVersion=%VERSION%" "/dAppPublicKey=%PUBLICKEY%" "/dAppURL=%URL%" "/dIsNetFx2=%ISNETFX2%" "/dVcRuntime=%VCRUNTIME%" "/dAppPlatform=%PLATFORM%" "/dAppProcessor=%PROCESSOR%"

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
