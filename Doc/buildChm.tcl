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

proc getFileHash { fileName } {
  if {[catch {
    exec fossil.exe sha1sum [file nativename $fileName]
  } result] == 0} then {
    return [string trim [lindex [split $result " "] 0]]
  }
  return ""
}

#
# HACK: Copy our local [fixed] copy of the MSDN documenter assembly into the
#       installed location of NDoc3, if necessary.  Actually copying the file
#       will require elevated administrator privileges; otherwise, it would
#       fail.  Any errors encountered while copying the file are reported via
#       the console; however, they will not halt processing.
#
proc copyMsdnDocumenter { sourceDirectory destinationDirectory } {
  set fileNameOnly NDoc3.Documenter.Msdn.dll

  set sourceFileName [file join $sourceDirectory bin $fileNameOnly]
  set destinationFileName [file join $destinationDirectory bin $fileNameOnly]

  set sourceFileHash [getFileHash $sourceFileName]
  # puts stdout "Hashed \"$sourceFileName\" ==> \"$sourceFileHash\""

  set destinationFileHash [getFileHash $destinationFileName]
  # puts stdout "Hashed \"$destinationFileName\" ==> \"$destinationFileHash\""

  if {[string length $sourceFileHash] > 0 && \
      [string length $destinationFileHash] > 0 && \
      $sourceFileHash ne $destinationFileHash} then {
    if {[catch {
      file copy -force $destinationFileName $destinationFileName.bak
      file copy -force $sourceFileName $destinationFileName
    } result] == 0} then {
      puts stdout \
          "finished copying \"$sourceFileName\" to \"$destinationFileName\""
    } else {
      puts stdout $result
    }
  } else {
    puts stdout \
        "skipped copying \"$sourceFileName\" to \"$destinationFileName\""
  }
}

set path [file normalize [file dirname [info script]]]

set nDocExtPath [file join [file dirname $path] Externals NDoc3]
set nDocInstPath [file join $env(ProgramFiles) NDoc3]

if {![file isdirectory $nDocInstPath]} then {
  puts stdout "NDoc3 must be installed to: $nDocInstPath"
  exit 1
}

set hhcPath [file join $env(ProgramFiles) "HTML Help Workshop"]

if {![file isdirectory $hhcPath]} then {
  puts stdout "HTML Help Workshop must be installed to: $hhcPath"
  exit 1
}

#
# NOTE: Build the name of the NDoc project file.
#
set projectFile [file join $path SQLite.NET.ndoc]

if {![file exists $projectFile]} then {
  puts stdout "Cannot find NDoc3 project file: $projectFile"
  exit 1
}

#
# NOTE: Extract the name of the XML doc file that will be used to build
#       the final CHM file from the NDoc project file.
#
set data [readFile $projectFile]

if {[string length $data] == 0} then {
  puts stdout "NDoc3 project file contains no data: $projectFile"
  exit 1
}

if {![regexp -- { documentation="(.*?)" } $data dummy xmlDocFile]} then {
  puts stdout "Cannot find XML doc file name in NDoc3 project file:\
               $projectFile"
  exit 1
}

if {[string length $xmlDocFile] == 0 || ![file exists $xmlDocFile]} then {
  puts stdout "Cannot find XML doc file: $xmlDocFile"
  exit 1
}

set data [readFile $xmlDocFile]
set count 0

set pattern { cref="([A-Z]):System\.Data\.SQLite\.}
incr count [regsub -all -- $pattern $data { cref="\1:system.Data.SQLite.} data]

if {$count > 0} then {
  writeFile $xmlDocFile $data
} else {
  puts stdout "*WARNING* File \"$xmlDocFile\" does not match: $pattern"
}

#
# TODO: If the NDoc version number ever changes, the next line of code will
#       probably need to be updated.
#
set outputPath [file join Output]
set temporaryPath [file join $outputPath ndoc3_msdn_temp]

if {[file isdirectory $nDocExtPath]} then {
  copyMsdnDocumenter $nDocExtPath $nDocInstPath
}

set code [catch {exec [file join $nDocInstPath bin NDoc3Console.exe] \
    "-project=[file nativename $projectFile]"} result]

puts stdout $result; if {$code != 0} then {exit $code}

set fileNames [list SQLite.NET.hhp SQLite.NET.hhc]

foreach fileName [glob -nocomplain [file join $temporaryPath *.html]] {
  lappend fileNames [file tail $fileName]
}

set patterns(.hhc,1) {<!--This document contains Table of Contents information\
for the HtmlHelp compiler\.--><UL>}

set patterns(.hhp,1) {Default topic=~System\.Data\.SQLite\.html}

set patterns(.hhp,2) \
    {"~System\.Data\.SQLite\.html","~System\.Data\.SQLite\.html",,,,,}

set patterns(.html,1) \
    {"http://msdn\.microsoft\.com/en-us/library/(System\.Data\.SQLite\.(?:.*?))\(VS\.\d+\)\.aspx"}

set patterns(.html,2) {System.Collections.Generic.IEnumerable`1}
set patterns(.html,3) {System.Collections.Generic.IEnumerator`1}

set patterns(.html,4) \
    {"(System\.Data\.SQLite~System\.Data\.SQLite\.SQLiteFunction\.Dispose)\.html"}

set patterns(.html,5) \
    {"(System\.Data\.SQLite~System\.Data\.SQLite\.SQLiteModule\.SetEstimatedCost)\.html"}

set patterns(.html,6) \
    {"(System\.Data\.SQLite~System\.Data\.SQLite\.SQLiteModule\.SetTableError)\.html"}

set patterns(.html,7) \
    {"(System\.Data\.SQLite~System\.Data\.SQLite\.SQLiteModule\.Dispose)\.html"}

set patterns(.html,8) \
    {"(System\.Data\.SQLite~System\.Data\.SQLite\.SQLiteVirtualTableCursor\.Dispose)\.html"}

set subSpecs(.hhc,1) [readFileAsSubSpec [file join $path SQLite.NET.hhc]]

set subSpecs(.hhp,1) {Default topic=welcome.html}
set subSpecs(.hhp,2) {"welcome.html","welcome.html",,,,,}

set subSpecs(.html,1) {"System.Data.SQLite~\1.html"}
set subSpecs(.html,2) {9eekhta0}
set subSpecs(.html,3) {78dfe2yb}
set subSpecs(.html,4) {"\1~Overloads.html"}
set subSpecs(.html,5) {"\1~Overloads.html"}
set subSpecs(.html,6) {"\1~Overloads.html"}
set subSpecs(.html,7) {"\1~Overloads.html"}
set subSpecs(.html,8) {"\1~Overloads.html"}

foreach fileName $fileNames {
  set fileName [file join $path $temporaryPath $fileName]

  #
  # NOTE: Make sure the file we need actually exists.
  #
  if {![file isfile $fileName]} then {
    puts stdout "Cannot find file: $fileName"
    exit 1
  }

  #
  # NOTE: Read the entire file into memory.
  #
  set data [readFile $fileName]

  #
  # NOTE: No replacements have been performed yet.
  #
  set count 0

  foreach name [lsort [array names patterns [file extension $fileName],*]] {
    set pattern $patterns($name)
    set subSpec ""

    if {[info exists subSpecs($name)]} then {
      set subSpec $subSpecs($name)
    }

    set patternCount [regsub -all -- $pattern $data $subSpec data]

    if {$patternCount > 0} then {
      incr count $patternCount
    } else {
      #
      # NOTE: This will emit multiple warnings for each file, making things
      #       a bit too noisy (by default).
      #
      # puts stdout "*WARNING* File \"$fileName\" does not match: $pattern"
    }
  }

  #
  # NOTE: If we actually performed some replacements, rewrite the file.
  #
  if {$count > 0} then {
    writeFile $fileName $data
  }
}

set code [catch {exec [file join $hhcPath hhc.exe] \
    [file nativename [file join $path $temporaryPath SQLite.NET.hhp]]} result]

#
# NOTE: For hhc.exe, zero means failure.
#
puts stdout $result; if {$code == 0} then {exit 1}

file copy -force [file join $path $temporaryPath SQLite.NET.chm] \
    [file join $path SQLite.NET.chm]

puts stdout SUCCESS
exit 0
