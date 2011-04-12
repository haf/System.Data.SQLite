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

IF NOT DEFINED VERSION (
  SET VERSION=1.0.69.0
)

IF NOT DEFINED PUBLICKEY (
  SET PUBLICKEY=db937bc2d44ff139
)

IF NOT DEFINED PROCESSORS (
  SET PROCESSORS=x86 x64
)

IF NOT DEFINED YEARS (
  SET YEARS=2008
)
