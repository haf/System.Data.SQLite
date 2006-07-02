' VBScript source code
Main

Sub Main()
  Dim WshShell
  Set WshShell = WScript.CreateObject("WScript.Shell")
  
  Dim fso
  Set fso = WScript.CreateObject("Scripting.FileSystemObject")
  
  Dim srcFile
  Dim srcFileContents
  dim newFileContents
  
  ' In order to support encryption, we need to know when the pager is being destroyed so we can destroy our encryption
  ' objects.  This modification adds code to support that.
  '
  Set srcFile = fso.OpenTextFile("src\pager.c", 1)  
  srcFileContents = srcFile.ReadAll()
  srcFile.Close()
  If InStr(1, srcFileContents, "sqlite3pager_free_codecarg", 1) = 0 Then
    newFileContents = Replace(srcFileContents, Chr(10) & "  sqliteFree(pPager);", Chr(10) & "#ifdef SQLITE_HAS_CODEC" & Chr(10) & "  sqlite3pager_free_codecarg(pPager->pCodecArg);" & Chr(10) & "#endif" & Chr(10) & "  sqliteFree(pPager);")
    If (newFileContents <> srcFileContents) Then
      WScript.StdOut.WriteLine "Updating pager.c"
      Set srcFile = fso.CreateTextFile("src\pager.c", true)
      srcFile.Write(newFileContents)
      srcFile.Close()
    End If
  End If

  Set srcFile = fso.OpenTextFile("src\sqlite3.def", 1)
  srcFileContents = srcFile.ReadAll()
  srcFile.Close()
  
  If InStr(1, srcFileContents, "sqlite3_key", 1) = 0 Then
    newFileContents = srcFileContents & Chr(10) & "sqlite3_key" & Chr(10) & "sqlite3_rekey" & Chr(10)
    WScript.StdOut.WriteLine "Updating sqlite3.def"
    set srcFile = fso.CreateTextFile("src\sqlite3.def", true)
    srcFile.Write(newFileContents)
    srcFile.Close()
  End If
  
End Sub
  