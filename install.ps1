# install.ps1 — wires up all tools so they are available on the PATH.
#
# Run once after cloning, and re-run whenever a new tool is added.
# Updating an existing tool only requires a `git pull` — no reinstall needed.
#
# What it does:
#   - Writes thin stub .bat files into $ToolsDir (which should be on your PATH)
#   - Stubs simply forward to the real scripts inside this repo
#   - Recreates the "Scale Monitor 4" taskbar shortcut
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File install.ps1

$RepoDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$ToolsDir = "C:\dev\tools"

if (-not (Test-Path $ToolsDir)) {
    Write-Host "Error: ToolsDir '$ToolsDir' not found. Edit the `$ToolsDir variable in install.ps1." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Installing mikes-windows-tools -> $ToolsDir" -ForegroundColor Cyan
Write-Host ""

# ---------------------------------------------------------------------------
# Helper: write a stub .bat that calls a target, preserving all arguments
# ---------------------------------------------------------------------------
function Write-BatStub($toolName, $content) {
    $dest = Join-Path $ToolsDir "$toolName.bat"
    Set-Content -Path $dest -Value $content -Encoding ASCII
    Write-Host "  [bat]  $dest" -ForegroundColor Green
}

# ---------------------------------------------------------------------------
# transcribe  — needs EXEDIR so the exe files in c:\dev\tools are found
# ---------------------------------------------------------------------------
Write-BatStub "transcribe" @"
@echo off
set "EXEDIR=%~dp0"
call "$RepoDir\transcribe\transcribe.bat" %*
"@

# ---------------------------------------------------------------------------
# removebg
# ---------------------------------------------------------------------------
Write-BatStub "removebg" @"
@echo off
call "$RepoDir\removebg\removebg.bat" %*
"@

# ---------------------------------------------------------------------------
# all-hands
# ---------------------------------------------------------------------------
Write-BatStub "all-hands" @"
@echo off
call "$RepoDir\all-hands\all-hands.bat" %*
"@

# ---------------------------------------------------------------------------
# backup-phone
# ---------------------------------------------------------------------------
Write-BatStub "backup-phone" @"
@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "$RepoDir\backup-phone\backup-phone.ps1" %*
"@

# ---------------------------------------------------------------------------
# scale-monitor4 — taskbar shortcut (no bat stub needed; launched via shortcut)
# ---------------------------------------------------------------------------
$vbsPath      = "$RepoDir\scale-monitor4\scale-monitor4.vbs"
$shortcutPath = Join-Path $ToolsDir "Scale Monitor 4.lnk"
$wsh          = New-Object -ComObject WScript.Shell
$sc           = $wsh.CreateShortcut($shortcutPath)
$sc.TargetPath       = "wscript.exe"
$sc.Arguments        = "`"$vbsPath`""
$sc.WorkingDirectory = "$RepoDir\scale-monitor4"
$sc.Description      = "Toggle Monitor 4 scale between 200% (normal) and 300% (filming)"
$sc.IconLocation     = "%SystemRoot%\System32\imageres.dll,109"
$sc.Save()
Write-Host "  [lnk]  $shortcutPath" -ForegroundColor Green

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "Done. To update tools in future: git pull (no reinstall needed)." -ForegroundColor Yellow
Write-Host "To add a new tool: create its subfolder, then re-run install.ps1." -ForegroundColor Yellow
Write-Host ""
Write-Host "Reminder: right-click 'Scale Monitor 4.lnk' in $ToolsDir and pin to taskbar." -ForegroundColor Cyan
