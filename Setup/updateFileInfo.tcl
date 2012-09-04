###############################################################################
#
# updateFileInfo.tcl -- File Metadata Updating Tool
#
# WARNING: This tool requires the Fossil binary to exist somewhere along the
#          PATH.
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

proc getFileSize { fileName } {
  #
  # NOTE: Return the number of mebibytes in the file with two digits after the
  #       decimal.
  #
  return [format %.2f [expr {[file size $fileName] / 1048576.0}]]
}

proc getFileHash { fileName } {
  #
  # NOTE: Return the SHA1 hash of the file, making use of Fossil via [exec] to
  #       actually calculate it.
  #
  return [string trim [lindex [regexp -inline -nocase -- {[0-9A-F]{40} } \
      [exec fossil sha1sum $fileName]] 0]]
}

#
# NOTE: Grab the fully qualified directory name of the directory containing
#       this script file.
#
set path [file normalize [file dirname [info script]]]

#
# NOTE: *WARNING* This assumes that the root of the source check-out is one
#       directory above the directory containing this script.
#
set root [file normalize [file dirname $path]]

#
# NOTE: Grab the name of the file to be updated from the command line, if
#       available; otherwise, use the default (i.e. "../www/downloads.wiki").
#
if {[info exists argv] && [llength $argv] > 0} then {
  set updateFileName [lindex $argv 0]
} else {
  set updateFileName [file join $root www downloads.wiki]
}

#
# NOTE: Grab the directory containing the files referenced in the data of the
#       file to be updated from the command line, if available; otherwise, use
#       the default (i.e. "./Output").
#
if {[info exists argv] && [llength $argv] > 1} then {
  set directory [lindex $argv 1]
} else {
  set directory [file join $path Output]
}

#
# NOTE: Setup the regular expression pattern with the necessary captures.  This
#       pattern is mostly non-greedy; however, at the end we need to match
#       exactly 40 hexadecimal characters.  In theory, in Tcl, this could have
#       an undefined result due to the mixing of greedy and non-greedy
#       quantifiers; however, in practice, this seems to work properly.  Also,
#       this pattern assumes a particular structure for the [HTML] file to be
#       updated.
#
set pattern {<a\
    href=".*?/(.*?\.(?:exe|zip|nupkg))">.*?\((\d+?\.\d+?) MiB\).*?sha1:\
    ([0-9A-F]{40})}

#
# NOTE: Grab all the data from the file to be updated.
#
set data [readFile $updateFileName]

#
# NOTE: Process each match in the data and capture the file name, size, and
#       hash.
#
set count 0

foreach {dummy fileName fileSize fileHash} \
    [regexp -all -inline -nocase -- $pattern $data] {
  #
  # NOTE: Get the fully qualified file name based on the configured directory.
  #
  set fullFileName [file join $directory [file tail $fileName]]

  #
  # NOTE: If the file does not exist, issue a warning and skip it.
  #
  if {![file exists $fullFileName]} then {
    puts stdout "WARNING: File \"$fullFileName\" does not exist, skipped."
    continue
  }

  #
  # NOTE: Replace the captured size and hash with ones calculated from the
  #       actual file name.  This will only replace the first instance of
  #       each (literal) match.  Since we are processing the matches in the
  #       exact order they appear in the data AND we are only replacing one
  #       literal instance per match AND the size sub-pattern is nothing like
  #       the hash sub-pattern, this should be 100% reliable.
  #
  incr count [regsub -nocase -- "***=$fileSize" $data [getFileSize \
      $fullFileName] data]

  incr count [regsub -nocase -- "***=$fileHash" $data [getFileHash \
      $fullFileName] data]
}

#
# NOTE: Write the [modified] data to the file to be updated.
#
if {$count > 0} then {
  writeFile $updateFileName $data
} else {
  puts stdout "WARNING: No changes, update of \"$updateFileName\" skipped."
}
