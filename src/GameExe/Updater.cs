using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace GameExe;

/// <summary>
/// Mise a jour automatique depuis GitHub Releases.
/// Au demarrage (en arriere-plan, sans bloquer le jeu) on demande a
/// l'API GitHub quelle est la derniere version publiee. Si elle est
/// plus recente, on telecharge le nouvel .exe et on propose au joueur
/// d'appuyer sur ENTREE : un mini script remplace l'exe et relance le jeu.
/// Hors-ligne ou erreur ? On ne dit rien et le jeu demarre normalement.
/// </summary>
public static class Updater
{
    public static bool Enabled = true;
    public static bool UpdateReady { get; private set; }
    public static string LatestVersion { get; private set; } = "";
    public static string Status { get; private set; } = "";
    public static bool RestartRequested { get; private set; }

    private static string _downloadedExe = "";

    public static void Init(string[] args)
    {
        foreach (var a in args)
            if (a == "--no-update") Enabled = false;
    }

    public static void CheckInBackground()
    {
        if (!Enabled) return;
        _ = Task.Run(CheckAsync);
    }

    private static async Task CheckAsync()
    {
        try
        {
            Status = "Verification des mises a jour...";
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("GameExe-Updater");

            string api = $"https://api.github.com/repos/{GameInfo.GitHubRepo}/releases/latest";
            string json = await http.GetStringAsync(api);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string tag = root.GetProperty("tag_name").GetString() ?? "";
            LatestVersion = tag.TrimStart('v', 'V');

            if (!Version.TryParse(GameInfo.Version, out var current) ||
                !Version.TryParse(LatestVersion, out var latest) ||
                latest <= current)
            {
                Status = "";
                return; // deja a jour (ou build de dev)
            }

            // Trouver l'asset GameExe.exe dans la release
            string url = "";
            foreach (var asset in root.GetProperty("assets").EnumerateArray())
            {
                if ((asset.GetProperty("name").GetString() ?? "") == GameInfo.ExeName)
                    url = asset.GetProperty("browser_download_url").GetString() ?? "";
            }
            if (url.Length == 0) { Status = ""; return; }

            Status = $"Telechargement de la v{LatestVersion}...";
            _downloadedExe = Path.Combine(Path.GetTempPath(), "GameExe_update.exe");
            var bytes = await http.GetByteArrayAsync(url);
            if (bytes.Length < 1_000_000) { Status = ""; return; } // fichier suspect, on abandonne
            await File.WriteAllBytesAsync(_downloadedExe, bytes);

            UpdateReady = true;
            Status = "";
        }
        catch
        {
            Status = ""; // pas grave : le jeu continue
        }
    }

    /// <summary>Remplace l'exe actuel par le nouveau et relance le jeu.</summary>
    public static void ApplyAndRestart()
    {
        try
        {
            string currentExe = Environment.ProcessPath ?? "";
            if (!UpdateReady || _downloadedExe.Length == 0 || !currentExe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                return;

            int pid = Process.GetCurrentProcess().Id;
            string bat = Path.Combine(Path.GetTempPath(), "GameExe_apply_update.bat");
            File.WriteAllText(bat,
                "@echo off\r\n" +
                ":wait\r\n" +
                $"tasklist /FI \"PID eq {pid}\" | find \"{pid}\" >nul && (timeout /t 1 /nobreak >nul & goto wait)\r\n" +
                $"copy /Y \"{_downloadedExe}\" \"{currentExe}\" >nul\r\n" +
                $"start \"\" \"{currentExe}\" --no-update\r\n" +
                "del \"%~f0\"\r\n");

            Process.Start(new ProcessStartInfo
            {
                FileName = bat,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            });
            RestartRequested = true; // Program.cs fermera le jeu proprement
        }
        catch
        {
            // si ca echoue, le joueur peut toujours relancer Installer.bat
        }
    }
}
