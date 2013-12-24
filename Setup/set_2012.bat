@ECHO OFF

::
:: set_2012.bat --
::
:: Written by Joe Mistachkin.
:: Released to the public domain, use at your own risk!
::

SET NETFX20ONLY=
SET NETFX35ONLY=
SET NETFX40ONLY=
SET NETFX45ONLY=1
SET NETFX451ONLY=

REM
REM HACK: Evidently, installing Visual Studio 2013 breaks using MSBuild to
REM       build native projects that specify a platform toolset of "v110".
REM
SET BUILD_ARGS=/property:VisualStudioVersion=11.0

VERIFY > NUL
