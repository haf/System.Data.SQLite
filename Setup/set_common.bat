@ECHO OFF

::
:: set_common.bat --
::
:: Written by Joe Mistachkin.
:: Released to the public domain, use at your own risk!
::

IF NOT DEFINED APPID (
  SET APPID={{02E43EC2-6B1C-45B5-9E48-941C3E1B204A}
)

IF NOT DEFINED URL (
  SET URL=http://system.data.sqlite.org/
)

IF NOT DEFINED PUBLICKEY (
  SET PUBLICKEY=db937bc2d44ff139
)

IF NOT DEFINED CONFIGURATIONS (
  SET CONFIGURATIONS=Release ReleaseNativeOnly
)

IF NOT DEFINED PLATFORMS (
  SET PLATFORMS=Win32 x64
)

IF NOT DEFINED PROCESSORS (
  SET PROCESSORS=x86 x64
)

IF DEFINED YEARS GOTO end_of_file

IF DEFINED VS2008SP (
  SET YEARS=%YEARS% 2008
)

IF DEFINED VS2010SP (
  SET YEARS=%YEARS% 2010
)

:end_of_file
