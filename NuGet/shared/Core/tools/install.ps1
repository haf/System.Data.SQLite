###############################################################################
#
# install.ps1 --
#
# Written by Joe Mistachkin, updates by David Archer.
# Released to the public domain, use at your own risk!
#
###############################################################################

param($installPath, $toolsPath, $package, $project)

$platformNames = "x86", "x64"
$fileName = "SQLite.Interop.dll"

$netFxPath = Split-Path $toolsPath -Leaf
$buildPath = Join-Path $toolsPath ".." -Resolve
$buildPath = Join-Path $buildPath ".." -Resolve
$buildPath = Join-Path $buildPath "build" -Resolve
$buildPath = Join-Path $buildPath $netFxPath -Resolve

foreach($platformName in $platformNames) {
  $folder = $project.ProjectItems | where {
    $_.Name -eq $platformName
  }

  if ($folder -eq $null) {
    $projectPath = Split-Path $project.FullName
    $folderPath = Join-Path $projectPath $platformName -Resolve

    if (Test-Path $folderPath) {
      $folder = $project.ProjectItems.AddFromDirectory($folderPath)

      #
      # NOTE: Since the EnvDTE.AddFromDirectory method is hard-wired to
      #       recursively add an existing folder *and* all of its files,
      #       it is necessary to remove all of its items after adding.
      #       This should be completely "safe" since the folder was just
      #       added by this script and did not exist in the project prior
      #       to that point.
      #
      foreach ($item in $folder.ProjectItems) {
        $item.Remove()
      }
    } else {
      #
      # NOTE: Apparently, there are circumstances where this call to add
      #       the folder can fail, e.g. when the user manually excludes
      #       it from the project and then deletes the directory on the
      #       file system from outside of Visual Studio.
      #
      $folder = $project.ProjectItems.AddFolder($platformName)
    }
  }

  $item = $folder.ProjectItems | where {
    $_.Name -eq $fileName
  }

  if ($item -ne $null) {
    continue
  }

  $itemSourceDirectory = Join-Path $buildPath $platformName -Resolve
  $itemSourceFileName = Join-Path $itemSourceDirectory $fileName -Resolve

  $item = $folder.ProjectItems.AddFromFile($itemSourceFileName)
}
