###############################################################################
#
# buildChm.tcl -- CHM Build Wrapper & Post-Procssing Tool
#
# WARNING: This tool requires that the "HTML Help Workshop" and "NDoc3"
#          applications are installed to their default locations.
#
# Written by Joe Mistachkin.
# Released to the public domain, use at your own risk!
#
###############################################################################

proc readFile { fileName } {
  set file_id [open $fileName RDONLY]
  fconfigure $file_id -encoding binary -translation binary
  set result [read $file_id]
  close $file_id
  return $result
}

proc writeFile { fileName data } {
  set file_id [open $fileName {WRONLY CREAT TRUNC}]
  fconfigure $file_id -encoding binary -translation binary
  puts -nonewline $file_id $data
  close $file_id
  return ""
}

proc readFileAsSubSpec { fileName } {
  set data [readFile $fileName]
  regsub -all -- {&} $data {\\\&} data
  regsub -all -- {\\(\d+)} $data {\\\\\1} data
  return $data
}

set path [file dirname [info script]]

set nDocPath [file join $env(ProgramFiles) NDoc3]

if {![file isdirectory $nDocPath]} then {
  puts stdout "NDoc3 must be installed to: $nDocPath"; exit 1
}

set hhcPath [file join $env(ProgramFiles) "HTML Help Workshop"]

if {![file isdirectory $hhcPath]} then {
  puts stdout "HTML Help Workshop must be installed to: $hhcPath"; exit 1
}

#
# TODO: If the NDoc version number ever changes, the next line of code will
#       probably need to be updated.
#
set outputPath [file join Output ndoc3_msdn_temp]

set code [catch {
  exec [file join $nDocPath bin NDoc3Console.exe] \
      "-project=[file nativename [file join $path SQLite.NET.ndoc]]"
} result]

puts stdout $result; if {$code != 0} then {exit $code}

set fileNames [list SQLite.NET.hhp SQLite.NET.hhc]

set exps(.hhc,1) {<!--This document contains Table of Contents information for\
the HtmlHelp compiler\.--><UL>}

set exps(.hhp,1) {Default topic=~System\.Data\.SQLite\.html}
set exps(.hhp,2) {"~System\.Data\.SQLite\.html","~System\.Data\.SQLite\.html",,,,,}

set subSpecs(.hhc,1) [readFileAsSubSpec [file join $path SQLite.NET.hhc]]
set subSpecs(.hhp,1) {Default topic=welcome.html}
set subSpecs(.hhp,2) {"welcome.html","welcome.html",,,,,}

foreach fileName $fileNames {
  set fileName [file join $path $outputPath $fileName]

  #
  # NOTE: Make sure the file we need actually exists.
  #
  if {![file isfile $fileName]} then {
    puts stdout "Cannot find file: $fileName"; exit 1
  }

  #
  # NOTE: Read the entire file into memory.
  #
  set data [readFile $fileName]

  #
  # NOTE: No replacements have been performed yet.
  #
  set count 0

  foreach name [lsort [array names exps [file extension $fileName],*]] {
    set exp $exps($name)
    set subSpec ""

    if {[info exists subSpecs($name)]} then {
      set subSpec $subSpecs($name)
    }

    set expCount [regsub -- $exp $data $subSpec data]

    if {$expCount > 0} then {
      incr count $expCount
    } else {
      puts stdout "*WARNING* File \"$fileName\" does not match: $exp"
    }
  }

  #
  # NOTE: If we actually performed some replacements, rewrite the file.
  #
  if {$count > 0} then {
    writeFile $fileName $data
  }
}

set code [catch {exec [file join $hhcPath hhc.exe] [file nativename [file join $path $outputPath SQLite.NET.hhp]]} result]

#
# NOTE: For hhc.exe, zero means failure.
#
puts stdout $result; if {$code == 0} then {exit 1}

file copy -force [file join $path $outputPath SQLite.NET.chm] \
    [file join $path SQLite.NET.chm]

puts stdout SUCCESS; exit 0
