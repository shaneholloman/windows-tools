// main.js - Video Titles chat tool
// Reads OPENROUTER_API_KEY and VIDEO_TITLES_PATH from environment (set by video-titles.ps1)

const MODEL         = 'google/gemini-2.5-flash';
const API_URL       = 'https://openrouter.ai/api/v1/chat/completions';
const SYSTEM_PROMPT = `You are an expert YouTube title ideation assistant for developer-focused content.
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
- Focus on accuracy - avoid misleading claims.`;

// -----------------------------------------------------------------------
// State
// -----------------------------------------------------------------------
let apiKey          = '';
let videoPath       = '';
let conversationHistory = [];

// -----------------------------------------------------------------------
// DOM refs
// -----------------------------------------------------------------------
const chatHistory      = document.getElementById('chat-history');
const messageInput     = document.getElementById('message-input');
const sendBtn          = document.getElementById('send-btn');
const statusBar        = document.getElementById('status-bar');
const transcriptInput  = document.getElementById('transcript-input');
const videoPathDisplay = document.getElementById('video-path-display');
const srtStatusDisplay = document.getElementById('srt-status-display');
const srtRow           = document.getElementById('srt-row');

// -----------------------------------------------------------------------
// Init
// -----------------------------------------------------------------------
Neutralino.init();

Neutralino.events.on('ready', async () => {
  // Read API key and video path from environment
  try { apiKey    = await Neutralino.os.getEnv('OPENROUTER_API_KEY'); } catch (e) {}
  try { videoPath = await Neutralino.os.getEnv('VIDEO_TITLES_PATH');  } catch (e) {}

  if (!apiKey) {
    setStatus(
      'OPENROUTER_API_KEY is not set. Edit the .env file at the repo root and re-launch.',
      true
    );
  }

  if (videoPath) {
    videoPathDisplay.textContent = videoPath;
    const filename = videoPath.replace(/\\/g, '/').split('/').pop();
    try { await Neutralino.window.setTitle(`Video Titles - ${filename}`); } catch (e) {}
    await tryLoadSrt(videoPath);
  }

  messageInput.select();
  messageInput.focus();
});

// -----------------------------------------------------------------------
// Sidebar navigation
// -----------------------------------------------------------------------
document.querySelectorAll('.nav-btn').forEach(btn => {
  btn.addEventListener('click', () => showSection(btn.dataset.section));
});

function showSection(name) {
  document.querySelectorAll('.nav-btn').forEach(b => b.classList.toggle('active', b.dataset.section === name));
  document.querySelectorAll('.section').forEach(s => s.classList.toggle('active', s.id === `section-${name}`));
  if (name === 'chat') messageInput.focus();
}

// Activate Chat on load
showSection('chat');

// -----------------------------------------------------------------------
// Transcript
// -----------------------------------------------------------------------
document.getElementById('load-srt-btn').addEventListener('click', async () => {
  try {
    const paths = await Neutralino.os.showOpenDialog('Select SRT transcript', {
      filters: [{ name: 'SRT files', extensions: ['srt', 'txt'] }]
    });
    if (paths && paths.length > 0) {
      const content = await Neutralino.filesystem.readFile(paths[0]);
      transcriptInput.value = parseSrt(content);
    }
  } catch (e) {
    if (e.code !== 'NE_OS_INVKNPT') { // user cancelled
      console.error('Failed to load SRT:', e);
    }
  }
});

document.getElementById('clear-transcript-btn').addEventListener('click', () => {
  transcriptInput.value = '';
});

async function tryLoadSrt(path) {
  const srtPath = path.replace(/\.[^.\\]+$/, '.srt');
  try {
    const content = await Neutralino.filesystem.readFile(srtPath);
    transcriptInput.value = parseSrt(content);
    const srtName = srtPath.replace(/\\/g, '/').split('/').pop();
    srtStatusDisplay.textContent = `Auto-loaded from ${srtName}`;
    srtRow.style.display = '';
  } catch (e) {
    // No SRT found - fine
  }
}

function parseSrt(text) {
  return text
    .replace(/\r\n/g, '\n')
    .split('\n')
    .filter(line => {
      const t = line.trim();
      return t && !/^\d+$/.test(t) && !/\d{2}:\d{2}:\d{2},\d{3}/.test(t);
    })
    .join('\n')
    .trim();
}

// -----------------------------------------------------------------------
// Video section - change video
// -----------------------------------------------------------------------
document.getElementById('change-video-btn').addEventListener('click', async () => {
  try {
    const paths = await Neutralino.os.showOpenDialog('Select video file', {
      filters: [{ name: 'Video files', extensions: ['mp4','mkv','avi','mov','wmv','webm','m4v','mpg','mpeg','ts','flv'] }]
    });
    if (!paths || paths.length === 0) return;
    videoPath = paths[0];
    videoPathDisplay.textContent = videoPath;
    const filename = videoPath.replace(/\\/g, '/').split('/').pop();
    srtRow.style.display = 'none';
    srtStatusDisplay.textContent = '';
    try { await Neutralino.window.setTitle(`Video Titles - ${filename}`); } catch(e) {}
    await tryLoadSrt(videoPath);
  } catch (e) {
    if (e.code !== 'NE_OS_INVKNPT') console.error(e);
  }
});

// -----------------------------------------------------------------------
// Chat - send
// -----------------------------------------------------------------------
sendBtn.addEventListener('click', sendMessage);

messageInput.addEventListener('keydown', e => {
  if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
    e.preventDefault();
    sendMessage();
  }
});

async function sendMessage() {
  const text = messageInput.value.trim();
  if (!text) return;

  if (!apiKey) {
    setStatus('OPENROUTER_API_KEY is not set. Edit .env and re-launch.', true);
    return;
  }

  // Build messages
  const transcript = transcriptInput.value.trim();
  const messages = [{ role: 'system', content: SYSTEM_PROMPT }];

  if (transcript) {
    messages.push({ role: 'user', content: `Use this transcript as context for the full conversation:\n\n${transcript}` });
  }

  messages.push(...conversationHistory);
  messages.push({ role: 'user', content: text });

  conversationHistory.push({ role: 'user', content: text });
  appendMessage('user', text);

  messageInput.value = '';
  sendBtn.disabled = true;
  setStatus('Thinking...');

  // Switch to chat so the user sees the response come in
  showSection('chat');

  try {
    const res = await fetch(API_URL, {
      method: 'POST',
      headers: {
        'Authorization':  `Bearer ${apiKey}`,
        'Content-Type':   'application/json',
        'HTTP-Referer':   'https://github.com/mikecann/mikerosoft',
        'X-Title':        'mikerosoft/video-titles',
      },
      body: JSON.stringify({
        model:       MODEL,
        messages,
        temperature: 0.8,
        max_tokens:  1200,
      }),
    });

    const data = await res.json();

    if (!res.ok) {
      const msg = data?.error?.message ?? `API error ${res.status}`;
      throw new Error(msg);
    }

    const reply = data.choices?.[0]?.message?.content ?? '';
    conversationHistory.push({ role: 'assistant', content: reply });
    appendMessage('assistant', reply);
    setStatus('');
  } catch (err) {
    setStatus(`Error: ${err.message}`, true);
    // Remove failed user message so they can retry
    conversationHistory.pop();
  } finally {
    sendBtn.disabled = false;
    messageInput.focus();
  }
}

// -----------------------------------------------------------------------
// Chat rendering
// -----------------------------------------------------------------------
function appendMessage(role, content) {
  const msg = document.createElement('div');
  msg.className = `message message-${role}`;

  const speaker = document.createElement('div');
  speaker.className = 'message-speaker';
  speaker.textContent = role === 'user' ? 'You' : 'Gemini';

  const body = document.createElement('div');
  body.className = 'message-body';

  if (role === 'assistant' && window.marked) {
    body.innerHTML = marked.parse(content);
  } else {
    body.textContent = content;
  }

  msg.appendChild(speaker);
  msg.appendChild(body);
  chatHistory.appendChild(msg);
  chatHistory.scrollTop = chatHistory.scrollHeight;
}

// -----------------------------------------------------------------------
// Status
// -----------------------------------------------------------------------
function setStatus(text, isError = false) {
  statusBar.textContent = text || 'Ctrl+Enter to send';
  statusBar.classList.toggle('error', isError);
}
