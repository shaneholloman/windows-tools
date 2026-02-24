# video-titles.ps1 - Title ideation chat tool launcher
#
# Loads .env from the repo root, sets environment variables, then starts
# the Neutralinojs desktop app. The JS frontend reads OPENROUTER_API_KEY
# and VIDEO_TITLES_PATH via Neutralino.os.getEnv().
#
# Usage:
#   video-titles.ps1                    # open with no video
#   video-titles.ps1 "C:\video.mp4"     # open and auto-load transcript

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

# Pass video path via environment variable so Neutralinojs can read it
if ($VideoPath) {
    [System.Environment]::SetEnvironmentVariable('VIDEO_TITLES_PATH', $VideoPath, 'Process')
}

$exe = Join-Path $PSScriptRoot 'bin\neutralino-win_x64.exe'

if (-not (Test-Path $exe)) {
    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.MessageBox]::Show(
        "Neutralinojs binary not found.`n`nRun 'neu update' in the video-titles folder to download it.`n`n  cd $PSScriptRoot`n  neu update",
        'video-titles - binary missing',
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Warning) | Out-Null
    exit
}

# Start the Neutralinojs app (--load-dir-res loads resources from disk, no build needed)
& $exe --load-dir-res --path="$PSScriptRoot"
