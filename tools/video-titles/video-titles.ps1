# video-titles.ps1 - Title ideation chat tool launcher
#
# Loads the pre-built DLL and opens the chat UI.
# Accepts an optional video file path; if found, auto-loads the matching .srt.
#
# First-time setup:
#   1. Run build.bat to compile the DLL
#   2. Run install.ps1 to register the context menu entry
#   3. Right-click a video file -> Mike's Tools -> Video Titles
#
# After any code change to *.cs files:
#   1. Run build.bat (or build-and-run.bat for a one-shot dev cycle)

param([string]$VideoPath = "")

# Load .env from repo root (two levels up: tools\video-titles\ -> tools\ -> repo\)
$repoRoot = Split-Path (Split-Path $PSScriptRoot)
$dotEnv   = Join-Path $repoRoot ".env"
if (Test-Path $dotEnv) {
    Get-Content $dotEnv | ForEach-Object {
        if ($_ -match '^\s*([^#][^=]*?)\s*=\s*(.*)\s*$') {
            $val = $Matches[2].Trim().Trim('"').Trim("'")
            [System.Environment]::SetEnvironmentVariable($Matches[1].Trim(), $val, 'Process')
        }
    }
}

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$dll = Join-Path $env:LOCALAPPDATA 'video-titles\video-titles.dll'

if (-not (Test-Path $dll)) {
    $buildBat = Join-Path $PSScriptRoot 'build.bat'
    [System.Windows.Forms.MessageBox]::Show(
        "video-titles has not been built yet.`n`nPlease run build.bat first:`n`n  $buildBat`n`nThis only needs to be done once (and again after code changes).",
        'video-titles - not built',
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Warning) | Out-Null
    exit
}

[System.Reflection.Assembly]::LoadFrom($dll) | Out-Null
[VideoTitles.App]::Run($VideoPath, $PSScriptRoot)
