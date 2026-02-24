using System;
using System.Windows.Forms;

namespace VideoTitles {

// Reads configuration from the process environment.
// The .env file at the repo root is loaded by video-titles.ps1 before the DLL
// is started, so by the time this runs the env var is already set.
public static class Settings {

    public static string OpenRouterApiKey {
        get { return Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? ""; }
    }

    // Call once at startup. Shows a MessageBox and returns false if the key is missing.
    public static bool Validate() {
        if (!string.IsNullOrEmpty(OpenRouterApiKey)) return true;

        MessageBox.Show(
            "OPENROUTER_API_KEY is not set.\n\n" +
            "1. Open the repo root and copy .env.example to .env\n" +
            "2. Set OPENROUTER_API_KEY=sk-or-...\n" +
            "3. Re-run install.ps1 or restart video-titles\n\n" +
            "Get a key at: https://openrouter.ai/keys",
            "video-titles - Missing API key",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);

        return false;
    }
}

}
