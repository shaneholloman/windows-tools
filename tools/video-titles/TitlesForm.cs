using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace VideoTitles {

public class TitlesForm : Form {

    // -----------------------------------------------------------------------
    // Colours
    // -----------------------------------------------------------------------
    static readonly Color SidebarBg    = Color.FromArgb(36, 36, 44);
    static readonly Color SidebarText  = Color.FromArgb(180, 180, 195);
    static readonly Color NavActive    = Color.FromArgb(0, 112, 224);
    static readonly Color NavHover     = Color.FromArgb(55, 55, 70);
    static readonly Color BgContent    = Color.FromArgb(250, 250, 253);
    static readonly Color BgInput      = Color.FromArgb(255, 255, 255);
    static readonly Color Accent       = Color.FromArgb(0, 112, 224);
    static readonly Color AccentHov    = Color.FromArgb(0,  90, 190);
    static readonly Color TextDark     = Color.FromArgb( 28,  28,  30);
    static readonly Color TextMuted    = Color.FromArgb(100, 100, 112);
    static readonly Color Border       = Color.FromArgb(218, 218, 224);

    // -----------------------------------------------------------------------
    // Nav items
    // -----------------------------------------------------------------------
    const string SEC_VIDEO      = "Video";
    const string SEC_TRANSCRIPT = "Transcript";
    const string SEC_CHAT       = "Chat";

    // -----------------------------------------------------------------------
    // Controls
    // -----------------------------------------------------------------------
    Panel   _sidebar;
    Panel   _contentArea;

    // Nav buttons
    Button  _navVideo;
    Button  _navTranscript;
    Button  _navChat;
    string  _activeSection = SEC_CHAT;

    // Video section
    Panel   _videoSection;
    Label   _lblVideoPath;
    Label   _lblSrtStatus;

    // Transcript section
    Panel   _transcriptSection;
    TextBox _txtTranscript;

    // Chat section
    Panel       _chatSection;
    RichTextBox _rtbChat;
    TextBox     _txtInput;
    Button      _btnSend;
    Label       _lblStatus;

    // -----------------------------------------------------------------------
    // State
    // -----------------------------------------------------------------------
    readonly List<ChatMessage> _history = new List<ChatMessage>();
    string _videoPath;

    // -----------------------------------------------------------------------
    // Constructor
    // -----------------------------------------------------------------------
    public TitlesForm(string videoPath) {
        _videoPath = videoPath;
        InitForm();
        BuildLayout();

        // Populate video section
        if (!string.IsNullOrEmpty(videoPath)) {
            Text = "Video Titles - " + Path.GetFileName(videoPath);
            _lblVideoPath.Text = videoPath;
            TryLoadSrt(videoPath);
        } else {
            Text = "Video Titles";
            _lblVideoPath.Text = "(no video selected)";
        }

        ShowSection(SEC_CHAT);
    }

    // -----------------------------------------------------------------------
    // Form setup
    // -----------------------------------------------------------------------
    void InitForm() {
        Font            = new Font("Segoe UI", 10f);
        BackColor       = SidebarBg;
        ClientSize      = new Size(780, 680);
        MinimumSize     = new Size(620, 480);
        StartPosition   = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;

        string icoPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"mikerosoft.app\icons\video-titles.ico");
        if (File.Exists(icoPath)) {
            try { Icon = new Icon(icoPath); } catch { }
        }
    }

    // -----------------------------------------------------------------------
    // Layout - sidebar + content area side by side
    // -----------------------------------------------------------------------
    void BuildLayout() {
        var root = new TableLayoutPanel {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 1,
            Padding     = new Padding(0),
            Margin      = new Padding(0),
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _sidebar     = BuildSidebar();
        _contentArea = new Panel {
            Dock      = DockStyle.Fill,
            BackColor = BgContent,
            Padding   = new Padding(0),
        };

        root.Controls.Add(_sidebar,     0, 0);
        root.Controls.Add(_contentArea, 1, 0);

        // Pre-build all section panels
        _videoSection      = BuildVideoSection();
        _transcriptSection = BuildTranscriptSection();
        _chatSection       = BuildChatSection();

        Controls.Add(root);
        Shown += (s, e) => _txtInput.Focus();
    }

    // -----------------------------------------------------------------------
    // Sidebar
    // -----------------------------------------------------------------------
    Panel BuildSidebar() {
        var panel = new Panel {
            Dock      = DockStyle.Fill,
            BackColor = SidebarBg,
        };

        // App label at top
        var title = new Label {
            Text      = "Video\nTitles",
            Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock      = DockStyle.Top,
            Height    = 68,
            Padding   = new Padding(0, 14, 0, 0),
        };

        // Thin separator
        var sep = new Panel {
            Dock      = DockStyle.Top,
            Height    = 1,
            BackColor = Color.FromArgb(60, 60, 75),
        };

        // Nav buttons
        _navVideo      = MakeNavButton(SEC_VIDEO);
        _navTranscript = MakeNavButton(SEC_TRANSCRIPT);
        _navChat       = MakeNavButton(SEC_CHAT);

        _navVideo.Click      += (s, e) => ShowSection(SEC_VIDEO);
        _navTranscript.Click += (s, e) => ShowSection(SEC_TRANSCRIPT);
        _navChat.Click       += (s, e) => ShowSection(SEC_CHAT);

        // Stack from bottom of title downward using a flow panel
        var nav = new FlowLayoutPanel {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents  = false,
            BackColor     = SidebarBg,
            Padding       = new Padding(0, 8, 0, 0),
        };
        nav.Controls.Add(_navVideo);
        nav.Controls.Add(_navTranscript);
        nav.Controls.Add(_navChat);

        panel.Controls.Add(nav);
        panel.Controls.Add(sep);
        panel.Controls.Add(title);

        return panel;
    }

    Button MakeNavButton(string label) {
        var btn = new Button {
            Text      = label,
            Width     = 110,
            Height    = 44,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 10f),
            ForeColor = SidebarText,
            BackColor = SidebarBg,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(16, 0, 0, 0),
            Cursor    = Cursors.Hand,
            Margin    = new Padding(0, 0, 0, 2),
        };
        btn.FlatAppearance.BorderSize  = 0;
        btn.FlatAppearance.MouseOverBackColor = NavHover;
        btn.FlatAppearance.MouseDownBackColor = NavActive;
        return btn;
    }

    void ShowSection(string section) {
        _activeSection = section;

        // Swap content
        _contentArea.Controls.Clear();
        Panel active = section == SEC_VIDEO      ? _videoSection :
                       section == SEC_TRANSCRIPT ? _transcriptSection :
                                                   _chatSection;
        active.Dock = DockStyle.Fill;
        _contentArea.Controls.Add(active);

        // Update nav button appearance
        foreach (Button btn in new[] { _navVideo, _navTranscript, _navChat }) {
            bool isActive = btn.Text == section;
            btn.BackColor = isActive ? NavActive : SidebarBg;
            btn.ForeColor = isActive ? Color.White : SidebarText;
            btn.Font      = new Font("Segoe UI", 10f, isActive ? FontStyle.Bold : FontStyle.Regular);
        }

        if (section == SEC_CHAT)
            _txtInput.Focus();
    }

    // -----------------------------------------------------------------------
    // Video section
    // -----------------------------------------------------------------------
    Panel BuildVideoSection() {
        var panel = new Panel { BackColor = BgContent };

        var titleLbl = new Label {
            Text      = "Video",
            Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize  = true,
            Location  = new Point(20, 20),
        };

        var pathLbl = new Label {
            Text      = "File:",
            Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = TextMuted,
            AutoSize  = true,
            Location  = new Point(20, 60),
        };

        _lblVideoPath = new Label {
            Font      = new Font("Segoe UI", 9.5f),
            ForeColor = TextDark,
            AutoSize  = false,
            Height    = 18,
            Location  = new Point(20, 80),
        };

        _lblSrtStatus = new Label {
            Font      = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(40, 140, 40),
            AutoSize  = true,
            Location  = new Point(20, 106),
        };

        var btnChange = new Button {
            Text      = "Change video...",
            Location  = new Point(20, 140),
            AutoSize  = true,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9.5f),
            ForeColor = Accent,
            BackColor = BgContent,
            Cursor    = Cursors.Hand,
            Padding   = new Padding(6, 3, 6, 3),
        };
        btnChange.FlatAppearance.BorderColor = Border;
        btnChange.FlatAppearance.BorderSize  = 1;
        btnChange.Click += ChangeVideo;

        panel.Controls.AddRange(new Control[] { titleLbl, pathLbl, _lblVideoPath, _lblSrtStatus, btnChange });

        panel.Layout += (s, e) => {
            _lblVideoPath.Width = panel.ClientSize.Width - 40;
        };

        return panel;
    }

    void ChangeVideo(object sender, EventArgs e) {
        using (var dlg = new OpenFileDialog()) {
            dlg.Title  = "Select video file";
            dlg.Filter = "Video files|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.webm;*.m4v;*.mpg;*.mpeg;*.ts;*.flv|All files|*.*";
            if (!string.IsNullOrEmpty(_videoPath))
                dlg.InitialDirectory = Path.GetDirectoryName(_videoPath);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            _videoPath = dlg.FileName;
            Text = "Video Titles - " + Path.GetFileName(_videoPath);
            _lblVideoPath.Text = _videoPath;
            _lblSrtStatus.Text = "";
            TryLoadSrt(_videoPath);
        }
    }

    // -----------------------------------------------------------------------
    // Transcript section
    // -----------------------------------------------------------------------
    Panel BuildTranscriptSection() {
        var panel = new Panel {
            BackColor = BgContent,
            Padding   = new Padding(0),
        };

        _txtTranscript = new TextBox {
            Multiline   = true,
            ScrollBars  = ScrollBars.Vertical,
            Font        = new Font("Segoe UI", 9.5f),
            ForeColor   = TextDark,
            BackColor   = BgInput,
            BorderStyle = BorderStyle.None,
            WordWrap    = true,
        };

        // Bottom toolbar
        var toolbar = new Panel {
            Dock      = DockStyle.Bottom,
            Height    = 40,
            BackColor = Color.FromArgb(240, 240, 244),
            Padding   = new Padding(8, 0, 8, 0),
        };
        toolbar.Paint += (s, e) => {
            e.Graphics.DrawLine(new Pen(Border), 0, 0, toolbar.Width, 0);
        };

        var btnLoad = MakeToolbarButton("Load .srt file...");
        btnLoad.Click += LoadSrtFile;

        var btnClear = MakeToolbarButton("Clear");
        btnClear.Click += (s, e) => _txtTranscript.Clear();

        toolbar.Controls.Add(btnLoad);
        toolbar.Controls.Add(btnClear);

        toolbar.Layout += (s, e) => {
            btnLoad.Location  = new Point(0, (toolbar.ClientSize.Height - btnLoad.Height) / 2);
            btnClear.Location = new Point(btnLoad.Right + 8, (toolbar.ClientSize.Height - btnClear.Height) / 2);
        };

        panel.Controls.Add(_txtTranscript);
        panel.Controls.Add(toolbar);

        panel.Layout += (s, e) => {
            _txtTranscript.Location = new Point(12, 12);
            _txtTranscript.Size     = new Size(
                panel.ClientSize.Width  - 24,
                panel.ClientSize.Height - toolbar.Height - 20);
        };

        return panel;
    }

    // -----------------------------------------------------------------------
    // Chat section
    // -----------------------------------------------------------------------
    Panel BuildChatSection() {
        var panel = new Panel {
            BackColor = BgContent,
            Padding   = new Padding(0),
        };

        _rtbChat = new RichTextBox {
            ReadOnly    = true,
            BackColor   = BgContent,
            ForeColor   = TextDark,
            Font        = new Font("Segoe UI", 10f),
            BorderStyle = BorderStyle.None,
            ScrollBars  = RichTextBoxScrollBars.Vertical,
            WordWrap    = true,
            DetectUrls  = false,
            Padding     = new Padding(12, 8, 12, 8),
        };

        // Input area at bottom
        var inputBar = new Panel {
            Dock      = DockStyle.Bottom,
            Height    = 72,
            BackColor = Color.FromArgb(242, 242, 247),
            Padding   = new Padding(10, 8, 10, 8),
        };
        inputBar.Paint += (s, e) => {
            e.Graphics.DrawLine(new Pen(Border), 0, 0, inputBar.Width, 0);
        };

        _txtInput = new TextBox {
            Multiline   = true,
            Font        = new Font("Segoe UI", 10f),
            ForeColor   = TextDark,
            BackColor   = BgInput,
            BorderStyle = BorderStyle.FixedSingle,
            ScrollBars  = ScrollBars.Vertical,
            Text        = "Please give me 10 options",
        };
        _txtInput.KeyDown += (s, e) => {
            if (e.KeyCode == Keys.Enter && e.Control) {
                e.SuppressKeyPress = true;
                DoSend();
            }
        };
        // Select all on focus so the pre-filled text is easy to replace
        _txtInput.GotFocus += (s, e) => _txtInput.SelectAll();

        _btnSend = new Button {
            Text      = "Send",
            Width     = 72,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
            BackColor = Accent,
            ForeColor = Color.White,
            Cursor    = Cursors.Hand,
        };
        _btnSend.FlatAppearance.BorderSize = 0;
        _btnSend.Click += (s, e) => DoSend();
        _btnSend.MouseEnter += (s, e) => { if (_btnSend.Enabled) _btnSend.BackColor = AccentHov; };
        _btnSend.MouseLeave += (s, e) => { if (_btnSend.Enabled) _btnSend.BackColor = Accent; };

        _lblStatus = new Label {
            Font      = new Font("Segoe UI", 8.5f),
            ForeColor = TextMuted,
            AutoSize  = true,
            Text      = "Ctrl+Enter to send",
        };

        inputBar.Controls.AddRange(new Control[] { _txtInput, _btnSend, _lblStatus });
        inputBar.Layout += (s, e) => {
            int pad     = 10;
            int btnW    = _btnSend.Width;
            int inputW  = inputBar.ClientSize.Width - btnW - pad * 3;
            int inputH  = inputBar.ClientSize.Height - pad * 2 - _lblStatus.Height - 2;

            _txtInput.Location = new Point(pad, pad);
            _txtInput.Size     = new Size(inputW, inputH);
            _btnSend.Location  = new Point(pad + inputW + pad, pad);
            _btnSend.Height    = inputH;
            _lblStatus.Location = new Point(pad, _txtInput.Bottom + 2);
        };

        panel.Controls.Add(_rtbChat);
        panel.Controls.Add(inputBar);

        panel.Layout += (s, e) => {
            _rtbChat.Location = new Point(0, 0);
            _rtbChat.Size     = new Size(panel.ClientSize.Width, panel.ClientSize.Height - inputBar.Height);
        };

        return panel;
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------
    static Button MakeToolbarButton(string text) {
        var b = new Button {
            Text      = text,
            AutoSize  = true,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9f),
            ForeColor = Accent,
            BackColor = Color.Transparent,
            Cursor    = Cursors.Hand,
            Padding   = new Padding(6, 2, 6, 2),
        };
        b.FlatAppearance.BorderColor = Border;
        b.FlatAppearance.BorderSize  = 1;
        return b;
    }

    // Win32 placeholder text (EM_SETCUEBANNER) - kept for reference but unused
    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    static extern IntPtr SendMessage(IntPtr hWnd, int msg, bool wParam, string lParam);

    // -----------------------------------------------------------------------
    // Load .srt
    // -----------------------------------------------------------------------
    void LoadSrtFile(object sender, EventArgs e) {
        using (var dlg = new OpenFileDialog()) {
            dlg.Title  = "Select SRT transcript file";
            dlg.Filter = "SRT files (*.srt)|*.srt|Text files (*.txt)|*.txt|All files (*.*)|*.*";
            if (!string.IsNullOrEmpty(_videoPath))
                dlg.InitialDirectory = Path.GetDirectoryName(_videoPath);
            if (dlg.ShowDialog(this) == DialogResult.OK)
                LoadSrtPath(dlg.FileName);
        }
    }

    void TryLoadSrt(string videoPath) {
        string srtPath = Path.ChangeExtension(videoPath, ".srt");
        if (File.Exists(srtPath)) {
            LoadSrtPath(srtPath);
            _lblSrtStatus.Text = "Transcript auto-loaded from " + Path.GetFileName(srtPath);
        }
    }

    void LoadSrtPath(string path) {
        try {
            string text = ParseSrt(File.ReadAllText(path));
            _txtTranscript.Text = text;
        } catch (Exception ex) {
            MessageBox.Show("Could not read file:\n" + ex.Message, "video-titles",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    static string ParseSrt(string srt) {
        var lines  = srt.Replace("\r\n", "\n").Split('\n');
        var result = new System.Text.StringBuilder();
        foreach (string line in lines) {
            string t = line.Trim();
            if (t == "") continue;
            if (Regex.IsMatch(t, @"^\d+$")) continue;
            if (Regex.IsMatch(t, @"\d{2}:\d{2}:\d{2},\d{3}")) continue;
            result.AppendLine(t);
        }
        return result.ToString().Trim();
    }

    // -----------------------------------------------------------------------
    // Chat
    // -----------------------------------------------------------------------
    async void DoSend() {
        string text = _txtInput.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        string apiKey = Settings.OpenRouterApiKey;
        if (string.IsNullOrEmpty(apiKey)) { Settings.Validate(); return; }

        _history.Add(new ChatMessage { Role = "user", Content = text });
        AppendBubble("You", text, Accent);

        _txtInput.Clear();
        _btnSend.Enabled     = false;
        _lblStatus.Text      = "Thinking...";
        _lblStatus.ForeColor = TextMuted;

        // Switch to chat panel so the user can watch the reply come in
        ShowSection(SEC_CHAT);

        try {
            string reply = await OpenRouterClient.ChatAsync(
                _txtTranscript.Text,
                _history,
                apiKey);

            _history.Add(new ChatMessage { Role = "assistant", Content = reply });
            AppendBubble("Gemini", reply, TextMuted);
            _lblStatus.Text = "";
        } catch (Exception ex) {
            _lblStatus.Text      = "Error: " + ex.Message;
            _lblStatus.ForeColor = Color.FromArgb(180, 30, 30);
            if (_history.Count > 0 && _history[_history.Count - 1].Role == "user")
                _history.RemoveAt(_history.Count - 1);
        } finally {
            _btnSend.Enabled = true;
            _txtInput.Focus();
        }
    }

    void AppendBubble(string speaker, string message, Color speakerColor) {
        _rtbChat.SuspendLayout();

        if (_rtbChat.TextLength > 0)
            _rtbChat.AppendText("\n");

        // Speaker label
        int labelStart = _rtbChat.TextLength;
        _rtbChat.AppendText(speaker + "\n");
        _rtbChat.Select(labelStart, speaker.Length);
        _rtbChat.SelectionFont  = new Font("Segoe UI", 9f, FontStyle.Bold);
        _rtbChat.SelectionColor = speakerColor;

        // Body
        int bodyStart = _rtbChat.TextLength;
        _rtbChat.AppendText(message + "\n");
        _rtbChat.Select(bodyStart, message.Length);
        _rtbChat.SelectionFont  = new Font("Segoe UI", 10f);
        _rtbChat.SelectionColor = TextDark;

        _rtbChat.AppendText("\n");
        _rtbChat.SelectionStart  = _rtbChat.TextLength;
        _rtbChat.SelectionLength = 0;
        _rtbChat.ResumeLayout();
        _rtbChat.ScrollToCaret();
    }
}

}
