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
$copyToOutputDirectoryPropertyName = "CopyToOutputDirectory"

foreach($platformName in $platformNames) {
  $folder = $project.ProjectItems | where {
    $_.Name -eq $platformName
  }

  if ($folder -eq $null) {
    $projectPath = Split-Path $project.FullName
    $folderPath = Join-Path $projectPath $platformName

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
      $folder = $project.ProjectItems.AddFolder($platformName)
    }
  }

  $item = $folder.ProjectItems | where {
    $_.Name -eq $fileName
  }

  if ($item -ne $null) {
    continue
  }

  $itemSourceDirectory = Join-Path $toolsPath $platformName
  $itemSourceFileName = Join-Path $itemSourceDirectory $fileName

  $item = $folder.ProjectItems.AddFromFile($itemSourceFileName)
  $item.Properties.Item($copyToOutputDirectoryPropertyName).Value = 1
}
