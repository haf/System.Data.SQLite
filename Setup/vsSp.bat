@ECHO OFF

::
:: vsSp.bat --
::
:: Visual Studio 2008/2010/2012 Service Pack Detection Tool
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

REM
REM NOTE: Build the command that we will use to query for Visual Studio 2008.
REM       Visual Studio 2008 is 32-bit only; therefore, when not running on an
REM       x86 platform, look in the WoW64 registry hive.
REM
IF "%PROCESSOR_ARCHITECTURE%" == "x86" (
  SET GET_SP_CMD=reg.exe QUERY "HKLM\SOFTWARE\Microsoft\DevDiv\VS\Servicing\9.0" /v SP
) ELSE (
  SET GET_SP_CMD=reg.exe QUERY "HKLM\SOFTWARE\Wow6432Node\Microsoft\DevDiv\VS\Servicing\9.0" /v SP
)

FOR /F "eol=; tokens=1,2,3*" %%I IN ('%GET_SP_CMD% 2^> NUL') DO (
  IF {%%I} == {SP} (
    IF {%%J} == {REG_DWORD} (
      %_AECHO% Found Visual Studio 2008 Service Pack "%%K".
      SET VS2008SP=%%K
    )
  )
)

REM
REM NOTE: Build the command that we will use to query for Visual Studio 2010.
REM       Visual Studio 2010 is 32-bit only; therefore, when not running on an
REM       x86 platform, look in the WoW64 registry hive.
REM
IF "%PROCESSOR_ARCHITECTURE%" == "x86" (
  SET GET_SP_CMD=reg.exe QUERY "HKLM\SOFTWARE\Microsoft\DevDiv\VS\Servicing\10.0" /v SP
) ELSE (
  SET GET_SP_CMD=reg.exe QUERY "HKLM\SOFTWARE\Wow6432Node\Microsoft\DevDiv\VS\Servicing\10.0" /v SP
)

FOR /F "eol=; tokens=1,2,3*" %%I IN ('%GET_SP_CMD% 2^> NUL') DO (
  IF {%%I} == {SP} (
    IF {%%J} == {REG_DWORD} (
      %_AECHO% Found Visual Studio 2010 Service Pack "%%K".
      SET VS2010SP=%%K
    )
  )
)

REM
REM NOTE: Build the command that we will use to query for Visual Studio 2012.
REM       Visual Studio 2012 is 32-bit only; therefore, when not running on an
REM       x86 platform, look in the WoW64 registry hive.
REM
IF "%PROCESSOR_ARCHITECTURE%" == "x86" (
  SET GET_SP_CMD=reg.exe QUERY "HKLM\SOFTWARE\Microsoft\DevDiv\VS\Servicing\11.0" /v SP
) ELSE (
  SET GET_SP_CMD=reg.exe QUERY "HKLM\SOFTWARE\Wow6432Node\Microsoft\DevDiv\VS\Servicing\11.0" /v SP
)

FOR /F "eol=; tokens=1,2,3*" %%I IN ('%GET_SP_CMD% 2^> NUL') DO (
  IF {%%I} == {SP} (
    IF {%%J} == {REG_DWORD} (
      %_AECHO% Found Visual Studio 2012 Service Pack "%%K".
      SET VS2012SP=%%K
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

:usage
  ECHO.
  ECHO Usage: %~nx0
  ECHO.
  GOTO errors

:errors
  CALL :fn_SetErrorLevel
  ENDLOCAL
  GOTO end_of_file

:no_errors
  ENDLOCAL && (
    SET VS2008SP=%VS2008SP%
    SET VS2010SP=%VS2010SP%
    SET VS2012SP=%VS2012SP%
  )
  CALL :fn_ResetErrorLevel
  GOTO end_of_file

:end_of_file
EXIT /B %ERRORLEVEL%
