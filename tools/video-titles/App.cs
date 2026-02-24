using System;
using System.Windows.Forms;

namespace VideoTitles {

public static class App {

    // Entry point called by video-titles.ps1 after loading the DLL.
    // videoPath may be empty when launched without a file argument.
    public static void Run(string videoPath = "", string scriptDir = "") {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        if (!Settings.Validate()) return;

        using (var form = new TitlesForm(videoPath ?? "")) {
            Application.Run(form);
        }
    }
}

}
