# video-titles.ps1
# Interactive CLI chat tool for brainstorming YouTube video titles.
#
# Usage:
#   video-titles.ps1                       (prompts for video path)
#   video-titles.ps1 "C:\path\to\video.mp4"

param([string]$VideoPath = "")

$MODEL   = 'google/gemini-3.1-pro-preview'
$API_URL = 'https://openrouter.ai/api/v1/chat/completions'

$SYSTEM_PROMPT = @"
You are an expert YouTube title ideation assistant for developer-focused content.
Use the framework below to generate and refine titles through conversation.

### The Compelling Title Matrix

A great title usually combines two or more of these four pillars: The Hook, The Conflict, The Quantitative Payoff, and The Authority.

#### 1. The "Open Loop" Hook (Curiosity Gap)
- Pattern: [Action] until [Unforeseen Consequence] or [Subject] is [Strong Opinion]
- Examples:
  - I liked this backend until I ran React Doctor
  - Claude Code has a big problem
  - Why did the world's biggest IDE disappear?

#### 2. The "Death & Survival" Conflict
- Pattern: [Technology] is DEAD or [New Tech] just KILLED [Old Tech]
- Examples:
  - Stack Overflow is Dead
  - OpenAI just dropped their Cursor killer
  - The end of the GPU era

#### 3. The Quantitative Payoff (10x Efficiency)
- Pattern: [Action] is [Number]x easier or Build [Complex Thing] in [Short Time]
- Examples:
  - Convex just made dev onboarding 10x easier
  - Build Your Own Vibe Coding Platform in 5 Minutes
  - GLM-5 is unbelievable (Opus for 20% the cost?)

#### 4. The Authority / "Top 1%" Frame
- Pattern: The Top 1% of [Role] are using [X] or The ONLY [X] you'll ever need
- Examples:
  - The Top 1% of Devs Are Using These 5 Agent Skills
  - This Is The Only Claude Code Plugin You'll EVER Need
  - 99% of Developers Are STILL Coding Wrong in 2026

### Framework: The 3-Step Title Builder
1. Identify the Antagonist - what is the problem?
2. Add a Power Word - Unbelievable, Finally, Only, Dead, Toxic, Dangerous, Game-changing.
3. Create the Gap - explain the result or feeling, not the full mechanism.

Conversation behavior:
- Ask clarifying questions when useful.
- Propose concrete title options with variety across the four pillars.
- Keep options concise and punchy.
- Avoid repeating near-identical phrasing.
- Focus on accuracy - avoid misleading claims.
"@

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

function Write-Separator {
    Write-Host ("  " + ([string][char]0x2500) * 58) -ForegroundColor DarkGray
}

function ConvertFrom-Srt([string]$Content) {
    ($Content -replace "`r`n", "`n") -split "`n" |
        Where-Object {
            $t = $_.Trim()
            $t -and $t -notmatch '^\d+$' -and $t -notmatch '\d{2}:\d{2}:\d{2},\d{3}'
        } |
        ForEach-Object { $_.Trim() } |
        Out-String
}

function Write-Reply([string]$text) {
    foreach ($line in ($text -split "`n")) {
        Write-Host "  $line"
    }
}

# ---------------------------------------------------------------------------
# Load .env from repo root
# ---------------------------------------------------------------------------

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

if (-not $env:OPENROUTER_API_KEY) {
    Write-Host ""
    Write-Host "  ERROR: OPENROUTER_API_KEY is not set." -ForegroundColor Red
    Write-Host "  Copy .env.example to .env in the repo root and set the key." -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

# ---------------------------------------------------------------------------
# Resolve video path
# ---------------------------------------------------------------------------

if (-not $VideoPath) {
    Write-Host ""
    Write-Host "  No video path provided." -ForegroundColor Yellow
    $VideoPath = (Read-Host "  Enter path to video file").Trim().Trim('"')
}

if (-not (Test-Path $VideoPath)) {
    Write-Host ""
    Write-Host "  ERROR: File not found: $VideoPath" -ForegroundColor Red
    Write-Host ""
    exit 1
}

$VideoPath = (Resolve-Path $VideoPath).Path
$videoDir  = Split-Path $VideoPath
$videoName = [System.IO.Path]::GetFileNameWithoutExtension($VideoPath)
$logPath   = Join-Path $videoDir "$videoName-titles.jsonl"

# ---------------------------------------------------------------------------
# Header
# ---------------------------------------------------------------------------

Clear-Host
Write-Host ""
Write-Host "  VIDEO TITLES" -ForegroundColor Cyan
Write-Separator
Write-Host "  Video : $videoName" -ForegroundColor White
Write-Host "  Log   : $logPath" -ForegroundColor DarkGray

# ---------------------------------------------------------------------------
# Auto-detect or offer to generate SRT
# ---------------------------------------------------------------------------

$transcript = ""
$srtPath    = [System.IO.Path]::ChangeExtension($VideoPath, ".srt")

if (Test-Path $srtPath) {
    $raw        = Get-Content $srtPath -Raw -Encoding UTF8
    $transcript = ConvertFrom-Srt $raw
    $wordCount  = ($transcript -split '\s+' | Where-Object { $_ }).Count
    Write-Host "  SRT   : $(Split-Path $srtPath -Leaf) ($wordCount words loaded)" -ForegroundColor Green
} else {
    Write-Host "  SRT   : not found" -ForegroundColor Yellow
    Write-Host ""
    $choice = Read-Host "  Run transcribe to generate a transcript? (y/n)"
    if ($choice -match '^[Yy]') {
        Write-Host ""
        Write-Separator
        Write-Host ""
        cmd /c "`"C:\dev\tools\transcribe.bat`" `"$VideoPath`""
        Write-Host ""
        Write-Separator
        if (Test-Path $srtPath) {
            $raw        = Get-Content $srtPath -Raw -Encoding UTF8
            $transcript = ConvertFrom-Srt $raw
            $wordCount  = ($transcript -split '\s+' | Where-Object { $_ }).Count
            Write-Host "  SRT   : loaded ($wordCount words)" -ForegroundColor Green
        } else {
            Write-Host "  SRT   : transcript not found after running transcribe - continuing without." -ForegroundColor Yellow
        }
    } else {
        Write-Host "  SRT   : skipped - you can paste context directly in chat." -ForegroundColor DarkGray
    }
}

Write-Separator
Write-Host "  Type your message and press Enter. Ctrl+Enter sends multi-line." -ForegroundColor DarkGray
Write-Host "  Type  quit  to exit." -ForegroundColor DarkGray
Write-Separator
Write-Host ""

# ---------------------------------------------------------------------------
# Chat loop
# ---------------------------------------------------------------------------

$history   = [System.Collections.Generic.List[hashtable]]::new()
$sessionId = [System.DateTime]::Now.ToString("yyyyMMdd-HHmmss")

while ($true) {
    Write-Host "  You: " -ForegroundColor Cyan -NoNewline
    $userInput = (Read-Host).Trim()

    if (-not $userInput) { continue }
    if ($userInput -in @('quit', 'exit', 'q', ':q')) {
        Write-Host ""
        Write-Host "  Bye." -ForegroundColor DarkGray
        Write-Host ""
        break
    }

    # Build messages array
    $messages = [System.Collections.Generic.List[object]]::new()
    $messages.Add([pscustomobject]@{ role = "system"; content = $SYSTEM_PROMPT })

    if ($transcript) {
        $messages.Add([pscustomobject]@{
            role    = "user"
            content = "Use this transcript as context for the full conversation:`n`n$transcript"
        })
    }

    foreach ($m in $history) {
        $messages.Add([pscustomobject]@{ role = $m.role; content = $m.content })
    }
    $messages.Add([pscustomobject]@{ role = "user"; content = $userInput })

    Write-Host ""
    Write-Host "  Thinking..." -ForegroundColor DarkGray

    try {
        $body = [pscustomobject]@{
            model       = $MODEL
            messages    = @($messages)
            temperature = 0.8
            max_tokens  = 1200
        } | ConvertTo-Json -Depth 10 -Compress

        $response = Invoke-RestMethod `
            -Uri     $API_URL `
            -Method  POST `
            -Headers @{
                Authorization  = "Bearer $($env:OPENROUTER_API_KEY)"
                'Content-Type' = 'application/json'
                'HTTP-Referer' = 'https://github.com/mikecann/mikerosoft'
                'X-Title'      = 'mikerosoft/video-titles'
            } `
            -Body        $body `
            -ContentType 'application/json'

        $reply = $response.choices[0].message.content

        $history.Add(@{ role = "user";      content = $userInput })
        $history.Add(@{ role = "assistant"; content = $reply     })

        Write-Host ""
        Write-Host "  Gemini:" -ForegroundColor Green
        Write-Host ""
        Write-Reply $reply
        Write-Host ""
        Write-Separator
        Write-Host ""

        # Append exchange to JSONL log
        $logEntry = [pscustomobject]@{
            session   = $sessionId
            timestamp = [System.DateTime]::UtcNow.ToString("o")
            video     = $VideoPath
            user      = $userInput
            assistant = $reply
        } | ConvertTo-Json -Compress
        Add-Content -Path $logPath -Value $logEntry -Encoding UTF8

    } catch {
        Write-Host ""
        Write-Host "  ERROR: $_" -ForegroundColor Red
        Write-Host ""
    }
}
