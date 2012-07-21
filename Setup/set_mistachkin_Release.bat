@ECHO OFF

::
:: set_mistachkin_Release.bat --
::
:: Custom Per-User Release Build Settings
::
:: Written by Joe Mistachkin.
:: Released to the public domain, use at your own risk!
::

REM
REM NOTE: Unsets any extra MSBuild arguments that may be present to force the
REM       use of all the default settings.
REM
SET MSBUILD_ARGS=

VERIFY > NUL
