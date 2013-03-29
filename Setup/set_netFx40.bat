@ECHO OFF

::
:: set_netFx40.bat --
::
:: Written by Joe Mistachkin.
:: Released to the public domain, use at your own risk!
::

IF NOT DEFINED ISNETFX2 (
  SET ISNETFX2=False
)

IF NOT DEFINED VCRUNTIME (
  SET VCRUNTIME=2010_SP1_MFC
)

IF NOT DEFINED CONFIGURATION (
  SET CONFIGURATION=Release
)

IF NOT DEFINED PLATFORM (
  SET PLATFORM=Win32
)

IF NOT DEFINED PROCESSOR (
  SET PROCESSOR=x86
)

IF NOT DEFINED YEAR (
  SET YEAR=2010
)

IF NOT DEFINED FRAMEWORK (
  SET FRAMEWORK=netFx40
)

:end_of_file
