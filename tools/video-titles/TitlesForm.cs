using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace VideoTitles {

public class TitlesForm : Form {

    // -----------------------------------------------------------------------
    // Controls
    // -----------------------------------------------------------------------
    TableLayoutPanel _layout;

    // Transcript row
    Panel      _transcriptPanel;
    Label      _transcriptToggle;
    TextBox    _txtTranscript;
    Button     _btnLoadSrt;
    Button     _btnClearTranscript;

    // Notes row
    Panel      _notesPanel;
    TextBox    _txtNotes;

    // Chat history
    RichTextBox _rtbChat;

    // Input row
    Panel   _inputPanel;
    TextBox _txtInput;
    Button  _btnSend;
    Label   _lblStatus;

    // -----------------------------------------------------------------------
    // State
    // -----------------------------------------------------------------------
    readonly List<ChatMessage> _history = new List<ChatMessage>();
    bool _transcriptExpanded = true;
    string _videoPath;

    // -----------------------------------------------------------------------
    // Colours
    // -----------------------------------------------------------------------
    static readonly Color BgPage     = Color.FromArgb(245, 245, 248);
    static readonly Color BgPanel    = Color.FromArgb(255, 255, 255);
    static readonly Color Accent     = Color.FromArgb(0, 112, 224);
    static readonly Color AccentHov  = Color.FromArgb(0,  90, 190);
    static readonly Color TextDark   = Color.FromArgb( 30,  30,  30);
    static readonly Color TextMuted  = Color.FromArgb(100, 100, 110);
    static readonly Color Border     = Color.FromArgb(210, 210, 215);
    static readonly Color UserBubble = Color.FromArgb(230, 242, 255);
    static readonly Color AiBubble   = Color.FromArgb(242, 242, 247);

    // -----------------------------------------------------------------------
    // Constructor
    // -----------------------------------------------------------------------
    public TitlesForm(string videoPath) {
        _videoPath = videoPath;
        InitForm();
        BuildLayout();

        if (!string.IsNullOrEmpty(videoPath)) {
            Text = "Video Titles - " + Path.GetFileName(videoPath);
            TryLoadSrt(videoPath);
        } else {
            Text = "Video Titles";
        }
    }

    // -----------------------------------------------------------------------
    // Form setup
    // -----------------------------------------------------------------------
    void InitForm() {
        Font          = new Font("Segoe UI", 10f);
        BackColor     = BgPage;
        ClientSize    = new Size(720, 780);
        MinimumSize   = new Size(560, 500);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;

        // Try to load icon from LOCALAPPDATA
        string icoPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"mikerosoft.app\icons\video-titles.ico");
        if (File.Exists(icoPath)) {
            try { Icon = new Icon(icoPath); } catch { }
        }
    }

    // -----------------------------------------------------------------------
    // Build UI
    // -----------------------------------------------------------------------
    void BuildLayout() {
        _layout = new TableLayoutPanel {
            Dock        = DockStyle.Fill,
            ColumnCount = 1,
            Padding     = new Padding(12),
            BackColor   = BgPage,
        };
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // Row 0 - transcript panel (collapsible)
        _transcriptPanel = BuildTranscriptPanel();
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.Controls.Add(_transcriptPanel, 0, 0);

        // Row 1 - notes panel
        _notesPanel = BuildNotesPanel();
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.Controls.Add(_notesPanel, 0, 1);

        // Row 2 - chat history (fills remaining space)
        _rtbChat = new RichTextBox {
            Dock         = DockStyle.Fill,
            ReadOnly     = true,
            BackColor    = BgPanel,
            ForeColor    = TextDark,
            Font         = new Font("Segoe UI", 10f),
            BorderStyle  = BorderStyle.FixedSingle,
            ScrollBars   = RichTextBoxScrollBars.Vertical,
            WordWrap     = true,
            DetectUrls   = false,
            Margin       = new Padding(0, 4, 0, 4),
        };
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _layout.Controls.Add(_rtbChat, 0, 2);

        // Row 3 - input + send
        _inputPanel = BuildInputPanel();
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.Controls.Add(_inputPanel, 0, 3);

        Controls.Add(_layout);

        // Set initial focus to the input box
        Shown += (s, e) => _txtInput.Focus();
    }

    Panel BuildTranscriptPanel() {
        var panel = new Panel {
            Dock      = DockStyle.Fill,
            BackColor = BgPanel,
            Padding   = new Padding(8, 6, 8, 6),
            Margin    = new Padding(0, 0, 0, 4),
        };
        panel.Paint += PaintBorder;

        // Header row
        _transcriptToggle = new Label {
            Text      = "Transcript (auto-loaded from .srt)  [hide]",
            Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = TextMuted,
            AutoSize  = true,
            Cursor    = Cursors.Hand,
            Location  = new Point(0, 0),
        };
        _transcriptToggle.Click += ToggleTranscript;

        // Buttons
        _btnLoadSrt = MakeSmallButton("Load .srt file...", 2);
        _btnLoadSrt.Click += LoadSrtFile;

        _btnClearTranscript = MakeSmallButton("Clear", 3);
        _btnClearTranscript.Click += (s, e) => _txtTranscript.Clear();

        // Transcript textbox
        _txtTranscript = new TextBox {
            Multiline   = true,
            ScrollBars  = ScrollBars.Vertical,
            Font        = new Font("Segoe UI", 9f),
            ForeColor   = TextDark,
            BackColor   = Color.FromArgb(250, 250, 253),
            BorderStyle = BorderStyle.FixedSingle,
            Height      = 90,
        };

        // Layout header + buttons in a FlowLayoutPanel
        var header = new FlowLayoutPanel {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize      = true,
            WrapContents  = false,
            Margin        = new Padding(0),
        };
        header.Controls.Add(_transcriptToggle);
        header.Controls.Add(_btnLoadSrt);
        header.Controls.Add(_btnClearTranscript);

        panel.Controls.Add(header);
        panel.Controls.Add(_txtTranscript);

        panel.Layout += (s, e) => {
            header.Location        = new Point(0, 0);
            header.Width           = panel.ClientSize.Width;
            int txtTop             = header.Bottom + 4;
            _txtTranscript.Location = new Point(0, txtTop);
            _txtTranscript.Width   = panel.ClientSize.Width;
            panel.Height = _transcriptExpanded
                ? txtTop + _txtTranscript.Height + 6
                : header.Bottom + 6;
        };

        return panel;
    }

    Panel BuildNotesPanel() {
        var panel = new Panel {
            Dock      = DockStyle.Fill,
            BackColor = BgPanel,
            Padding   = new Padding(8, 6, 8, 6),
            Margin    = new Padding(0, 0, 0, 4),
            Height    = 56,
        };
        panel.Paint += PaintBorder;

        var lbl = new Label {
            Text      = "Notes / creator context:",
            Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = TextMuted,
            AutoSize  = true,
            Location  = new Point(0, 0),
        };

        _txtNotes = new TextBox {
            Font        = new Font("Segoe UI", 10f),
            ForeColor   = TextDark,
            BackColor   = Color.FromArgb(250, 250, 253),
            BorderStyle = BorderStyle.FixedSingle,
        };
        SetPlaceholder(_txtNotes, "Audience, key points, style notes, any thoughts...");

        panel.Controls.Add(lbl);
        panel.Controls.Add(_txtNotes);

        panel.Layout += (s, e) => {
            lbl.Location   = new Point(0, 2);
            int txtTop     = lbl.Bottom + 2;
            _txtNotes.Location = new Point(0, txtTop);
            _txtNotes.Width    = panel.ClientSize.Width;
            panel.Height   = txtTop + _txtNotes.Height + 6;
        };

        return panel;
    }

    Panel BuildInputPanel() {
        var panel = new Panel {
            Dock      = DockStyle.Fill,
            BackColor = BgPage,
            Margin    = new Padding(0, 4, 0, 0),
        };

        _txtInput = new TextBox {
            Multiline   = true,
            Height      = 56,
            Font        = new Font("Segoe UI", 10f),
            ForeColor   = TextDark,
            BackColor   = BgPanel,
            BorderStyle = BorderStyle.FixedSingle,
            ScrollBars  = ScrollBars.Vertical,
        };
        SetPlaceholder(_txtInput, "Message...");

        // Ctrl+Enter sends
        _txtInput.KeyDown += (s, e) => {
            if (e.KeyCode == Keys.Enter && e.Control) {
                e.SuppressKeyPress = true;
                SendMessage();
            }
        };

        _btnSend = new Button {
            Text      = "Send",
            Width     = 80,
            Height    = 56,
            BackColor = Accent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
            Cursor    = Cursors.Hand,
        };
        _btnSend.FlatAppearance.BorderSize = 0;
        _btnSend.Click += (s, e) => SendMessage();
        _btnSend.MouseEnter += (s, e) => { if (_btnSend.Enabled) _btnSend.BackColor = AccentHov; };
        _btnSend.MouseLeave += (s, e) => { if (_btnSend.Enabled) _btnSend.BackColor = Accent; };

        _lblStatus = new Label {
            AutoSize  = true,
            ForeColor = TextMuted,
            Font      = new Font("Segoe UI", 9f),
            Text      = "Ctrl+Enter to send",
            Location  = new Point(0, 60),
        };

        panel.Controls.Add(_txtInput);
        panel.Controls.Add(_btnSend);
        panel.Controls.Add(_lblStatus);

        panel.Layout += (s, e) => {
            int btnW = _btnSend.Width + 6;
            _txtInput.Location = new Point(0, 0);
            _txtInput.Width    = panel.ClientSize.Width - btnW;
            _btnSend.Location  = new Point(panel.ClientSize.Width - _btnSend.Width, 0);
            _lblStatus.Location = new Point(0, _txtInput.Bottom + 3);
            panel.Height = _txtInput.Bottom + _lblStatus.Height + 4;
        };

        return panel;
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------
    static Button MakeSmallButton(string text, int marginLeft) {
        var b = new Button {
            Text      = text,
            AutoSize  = true,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(0, 100, 200),
            BackColor = Color.Transparent,
            Cursor    = Cursors.Hand,
            Margin    = new Padding(marginLeft, 0, 0, 0),
        };
        b.FlatAppearance.BorderColor = Color.FromArgb(0, 100, 200);
        b.FlatAppearance.BorderSize  = 1;
        return b;
    }

    static void PaintBorder(object sender, PaintEventArgs e) {
        var ctrl = (Control)sender;
        ControlPaint.DrawBorder(e.Graphics, ctrl.ClientRectangle,
            Border, 1, ButtonBorderStyle.Solid,
            Border, 1, ButtonBorderStyle.Solid,
            Border, 1, ButtonBorderStyle.Solid,
            Border, 1, ButtonBorderStyle.Solid);
    }

    // Win32 placeholder text (EM_SETCUEBANNER)
    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    static extern IntPtr SendMessage(IntPtr hWnd, int msg, bool wParam, string lParam);

    static void SetPlaceholder(TextBox tb, string text) {
        tb.HandleCreated += (s, e) => SendMessage(tb.Handle, 0x1501, false, text);
    }

    // -----------------------------------------------------------------------
    // Transcript toggle
    // -----------------------------------------------------------------------
    void ToggleTranscript(object sender, EventArgs e) {
        _transcriptExpanded = !_transcriptExpanded;
        _txtTranscript.Visible      = _transcriptExpanded;
        _btnLoadSrt.Visible         = _transcriptExpanded;
        _btnClearTranscript.Visible = _transcriptExpanded;
        _transcriptToggle.Text = _transcriptExpanded
            ? "Transcript (auto-loaded from .srt)  [hide]"
            : "Transcript  [show]";
        _transcriptPanel.PerformLayout();
    }

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
            _lblStatus.Text = "Loaded transcript from " + Path.GetFileName(srtPath);
        }
    }

    void LoadSrtPath(string path) {
        try {
            string raw  = File.ReadAllText(path);
            string text = ParseSrt(raw);
            _txtTranscript.Text = text;
        } catch (Exception ex) {
            MessageBox.Show("Could not read file:\n" + ex.Message, "video-titles",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // Strip SRT sequence numbers and timestamp lines, return clean text
    static string ParseSrt(string srt) {
        var lines  = srt.Replace("\r\n", "\n").Split('\n');
        var result = new System.Text.StringBuilder();
        foreach (string line in lines) {
            string t = line.Trim();
            if (t == "") continue;
            if (Regex.IsMatch(t, @"^\d+$")) continue;                  // sequence number
            if (Regex.IsMatch(t, @"\d{2}:\d{2}:\d{2},\d{3}")) continue; // timestamp line
            result.AppendLine(t);
        }
        return result.ToString().Trim();
    }

    // -----------------------------------------------------------------------
    // Chat
    // -----------------------------------------------------------------------
    async void SendMessage() {
        string text = _txtInput.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        string apiKey = Settings.OpenRouterApiKey;
        if (string.IsNullOrEmpty(apiKey)) {
            Settings.Validate();
            return;
        }

        // Add user message to history and display
        _history.Add(new ChatMessage { Role = "user", Content = text });
        AppendChatBubble("You", text, UserBubble, TextDark);

        _txtInput.Clear();
        _btnSend.Enabled = false;
        _lblStatus.Text  = "Thinking...";
        _lblStatus.ForeColor = TextMuted;

        try {
            string reply = await OpenRouterClient.ChatAsync(
                _txtTranscript.Text,
                _txtNotes.Text,
                _history,
                apiKey);

            _history.Add(new ChatMessage { Role = "assistant", Content = reply });
            AppendChatBubble("Gemini", reply, AiBubble, TextDark);
            _lblStatus.Text = "";
        } catch (Exception ex) {
            _lblStatus.Text      = "Error: " + ex.Message;
            _lblStatus.ForeColor = Color.FromArgb(180, 30, 30);
            // Remove the user message from history so they can retry
            if (_history.Count > 0 && _history[_history.Count - 1].Role == "user")
                _history.RemoveAt(_history.Count - 1);
        } finally {
            _btnSend.Enabled = true;
            _txtInput.Focus();
        }
    }

    void AppendChatBubble(string speaker, string message, Color bubbleBg, Color textColor) {
        // Scroll to end before appending
        _rtbChat.SuspendLayout();

        int start = _rtbChat.TextLength;

        // Separator line between messages
        if (start > 0) {
            _rtbChat.AppendText("\n");
        }

        // Speaker label
        int labelStart = _rtbChat.TextLength;
        _rtbChat.AppendText(speaker + "\n");
        _rtbChat.Select(labelStart, speaker.Length);
        _rtbChat.SelectionFont  = new Font("Segoe UI", 9f, FontStyle.Bold);
        _rtbChat.SelectionColor = speaker == "You" ? Accent : TextMuted;

        // Message body
        int bodyStart = _rtbChat.TextLength;
        _rtbChat.AppendText(message + "\n");
        _rtbChat.Select(bodyStart, message.Length);
        _rtbChat.SelectionFont  = new Font("Segoe UI", 10f);
        _rtbChat.SelectionColor = textColor;

        // Divider
        _rtbChat.AppendText("\n");

        // Reset selection and scroll to end
        _rtbChat.SelectionStart  = _rtbChat.TextLength;
        _rtbChat.SelectionLength = 0;
        _rtbChat.ResumeLayout();
        _rtbChat.ScrollToCaret();
    }
}

}
