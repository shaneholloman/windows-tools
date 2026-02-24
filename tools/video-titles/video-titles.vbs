Dim shell, fso, dir, arg
Set shell = CreateObject("WScript.Shell")
Set fso   = CreateObject("Scripting.FileSystemObject")
dir = fso.GetParentFolderName(WScript.ScriptFullName)
arg = ""
If WScript.Arguments.Count > 0 Then arg = WScript.Arguments(0)
shell.Run "powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -Sta -File """ & dir & "\video-titles.ps1"" """ & arg & """", 0, False
