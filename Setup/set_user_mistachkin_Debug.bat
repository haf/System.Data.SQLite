@ECHO OFF

::
:: set_user_mistachkin_Debug.bat --
::
:: Custom Per-User Debug Build Settings
::
:: Written by Joe Mistachkin.
:: Released to the public domain, use at your own risk!
::

REM
REM NOTE: Enables the extra debug code helpful in troubleshooting issues.
REM
SET MSBUILD_ARGS=/p:CheckState=true
SET MSBUILD_ARGS=%MSBUILD_ARGS% /p:CountHandle=true
SET MSBUILD_ARGS=%MSBUILD_ARGS% /p:TraceConnection=true
SET MSBUILD_ARGS=%MSBUILD_ARGS% /p:TraceDetection=true
SET MSBUILD_ARGS=%MSBUILD_ARGS% /p:TraceHandle=true
SET MSBUILD_ARGS=%MSBUILD_ARGS% /p:TraceStatement=true
SET MSBUILD_ARGS=%MSBUILD_ARGS% /p:TrackMemoryBytes=true
