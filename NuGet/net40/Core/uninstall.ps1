###############################################################################
#
# uninstall.ps1 --
#
# Written by Joe Mistachkin, updates by David Archer.
# Released to the public domain, use at your own risk!
#
###############################################################################

param($installPath, $toolsPath, $package, $project)

$platformNames = "x86", "x64"
$fileName = "SQLite.Interop.dll"
$isLinkPropertyName = "IsLink"

foreach($platformName in $platformNames) {
  $folder = $project.ProjectItems | where {
    $_.Name -eq $platformName
  }

  if ($folder -eq $null) {
    continue
  }

  $item = $folder.ProjectItems | where { $_.Name -eq $fileName }

  if ($item -ne $null) {
    $isLinkProperty = $item.Properties | where {
      $_.Name -eq $isLinkPropertyName
    }

    if (($isLinkProperty -ne $null) -and ($isLinkProperty.Value)) {
      $item.Delete()
    }
  }

  #
  # NOTE: Great care is needed here.  If the folder contains items other
  #       than the items this script is responsible for, it must be left
  #       alone.  Furthermore, the directory (on the file system) should
  #       be deleted if it ends up empty due to this script.
  #
  if ($folder.ProjectItems.Count -eq 0) {
    $folderItems = Get-ChildItem -Path $folder.FileNames(1) -Recurse

    if ($folderItems -eq $null) {
      $folder.Delete()
    } else {
      $folder.Remove()
    }
  }
}
