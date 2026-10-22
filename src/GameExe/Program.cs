using System;
using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace GameExe;

/// <summary>
/// Point d'entree du jeu : fenetre, boucle principale, camera, interface.
/// Lance le jeu en dev avec :  dotnet run --project src/GameExe
/// Fabrique le .exe avec    :  build.bat  (ou GitHub Actions automatiquement)
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        Updater.Init(args);
        Updater.CheckInBackground();

        SetConfigFlags(ConfigFlags.VSyncHint | ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);
        InitWindow(1280, 720, GameInfo.Name + " - petit platformer");
        SetTargetFPS(60);
        SetExitKey(KeyboardKey.Null); // ECHAP gere a la main plus bas

        var world = new World();
        var player = new Player(320f);
        var fx = new ParticleSystem();

        float camX = 0f;
        bool quit = false;

        while (!WindowShouldClose() && !quit)
        {
            float dt = Math.Min(GetFrameTime(), 0.033f);
            float time = (float)GetTime();

            // --- Touches generales ---
            if (IsKeyPressed(KeyboardKey.Escape)) quit = true;
            if (IsKeyPressed(KeyboardKey.F11) || IsKeyPressed(KeyboardKey.F)) ToggleFullscreen();
            if (IsKeyPressed(KeyboardKey.P)) TakeScreenshot("capture.png");
            if (Updater.UpdateReady && IsKeyPressed(KeyboardKey.Enter)) Updater.ApplyAndRestart();
            if (Updater.RestartRequested) break;

            // --- Logique ---
            world.Update(dt);
            player.Update(dt, fx);
            fx.Update(dt);

            // Camera douce avec petite anticipation dans le sens du regard
            int screenW = GetScreenWidth(), screenH = GetScreenHeight();
            float zoom = screenH / World.VirtualH;
            float viewW = screenW / zoom;
            float target = player.Pos.X - viewW * 0.5f + player.Facing * 55f;
            camX = U.Damp(camX, target, 5.5f, dt);
            camX = U.Clamp(camX, 0f, Math.Max(0f, World.Width - viewW));

            var camera = new Camera2D
            {
                Offset = new Vector2(screenW * 0.5f, screenH * 0.5f),
                Target = new Vector2(camX + viewW * 0.5f, World.VirtualH * 0.5f),
                Rotation = 0f,
                Zoom = zoom,
            };

            // --- Dessin ---
            BeginDrawing();
            ClearBackground(new Color(96, 183, 242, 255));

            BeginMode2D(camera);
            world.Draw(camX, viewW, time, player);
            fx.Draw();
            player.Draw(time);
            world.DrawForeground(camX, viewW, time);
            EndMode2D();

            DrawHud(screenW, screenH, zoom, time);
            EndDrawing();
        }

        CloseWindow();
        return 0;
    }

    private static void DrawHud(int screenW, int screenH, float zoom, float time)
    {
        int m = (int)(18 * zoom);                 // marge
        int titleSize = Math.Max(16, (int)(34 * zoom));
        int smallSize = Math.Max(11, (int)(17 * zoom));
        var ink = new Color(38, 62, 44, 255);
        var inkSoft = new Color(38, 62, 44, 170);

        // Titre avec ombre
        DrawText(GameInfo.Name.ToUpperInvariant(), m + 2, m + 3, titleSize, new Color(255, 255, 255, 140));
        DrawText(GameInfo.Name.ToUpperInvariant(), m, m, titleSize, ink);
        DrawText("Fleches / ZQSD : bouger    ESPACE : sauter    MAJ : courir    F : plein ecran",
            m, m + titleSize + (int)(6 * zoom), smallSize, inkSoft);

        // Version en bas a droite (+ etat de la verification de mise a jour)
        string ver = "v" + GameInfo.Version;
        DrawText(ver, screenW - m - MeasureText(ver, smallSize), screenH - m - smallSize, smallSize, inkSoft);
        if (Updater.Status.Length > 0)
        {
            string s = Updater.Status;
            DrawText(s, screenW - m - MeasureText(s, smallSize), screenH - m - smallSize * 2 - 4, smallSize, inkSoft);
        }

        // Bandeau "mise a jour prete" (pulse doucement pour attirer l'oeil)
        if (Updater.UpdateReady)
        {
            string msg = $"Nouvelle version v{Updater.LatestVersion} prete !  Appuie sur ENTREE pour mettre a jour";
            int fs = Math.Max(13, (int)(19 * zoom));
            int tw = MeasureText(msg, fs);
            int bw = tw + (int)(36 * zoom), bh = (int)(40 * zoom);
            var rect = new Rectangle(screenW * 0.5f - bw * 0.5f, m, bw, bh);
            float pulse = 0.85f + 0.15f * U.Sin(time * 3.5f);
            DrawRectangleRounded(rect, 0.5f, 12, Fade(new Color(255, 122, 69, 255), pulse));
            DrawRectangleRoundedLinesEx(rect, 0.5f, 12, 2f, new Color(255, 255, 255, 200));
            DrawText(msg, (int)(rect.X + 18 * zoom), (int)(rect.Y + bh * 0.5f - fs * 0.5f), fs, Color.White);
        }
    }
}
