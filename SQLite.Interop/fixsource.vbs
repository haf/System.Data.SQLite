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
  
  ' Changes the name of the function so we can get first crack at it in interop.h to provide GetSchemaTable() support
  '
  Set srcFile = fso.OpenTextFile("src\select.c", 1) 
  srcFileContents = srcFile.ReadAll()
  srcFile.Close()  
  newFileContents = Replace(srcFileContents, "static void generateColumnNames(", "static void _generateColumnNames(")
  If (newFileContents <> srcFileContents) Then
    WScript.StdOut.WriteLine "Updating select.c"
    Set srcFile = fso.CreateTextFile("src\select.c", true)
    srcFile.Write(newFileContents)
    srcFile.Close()
  End If

  ' SQLite supports : and $ for named parameters, but we add support for @ as well here
  '  
  Set srcFile = fso.OpenTextFile("src\tokenize.c", 1)  
  srcFileContents = srcFile.ReadAll()
  srcFile.Close()
  If InStr(1, srcFileContents, "    case '@':", 1) = 0 Then
    newFileContents = Replace(srcFileContents, "    case ':': {", "    case '@':" & Chr(10) & "    case ':': {")
    If (newFileContents <> srcFileContents) Then
      WScript.StdOut.WriteLine "Updating tokenize.c"
      Set srcFile = fso.CreateTextFile("src\tokenize.c", true)
      srcFile.Write(newFileContents)
      srcFile.Close()
    End If
  End If

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

End Sub
  