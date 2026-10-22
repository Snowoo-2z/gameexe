using System;
using System.Reflection;

namespace GameExe;

/// <summary>Infos generales du jeu (nom, version, depot GitHub).</summary>
public static class GameInfo
{
    public const string Name = "Pousse !";
    public const string ExeName = "GameExe.exe";

    /// <summary>Depot GitHub utilise pour les mises a jour automatiques.</summary>
    public const string GitHubRepo = "Snowoo-2z/gameexe";

    /// <summary>
    /// Version actuelle, lue depuis l'exe lui-meme.
    /// Elle vient de version.txt via build.bat / GitHub Actions.
    /// En mode dev (dotnet run), ca affichera juste la version du .csproj.
    /// </summary>
    public static string Version
    {
        get
        {
            var v = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (string.IsNullOrWhiteSpace(v)) return "0.0.0-dev";
            return v.Split('+')[0].Trim(); // retire un eventuel hash de commit
        }
    }
}
