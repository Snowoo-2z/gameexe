using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace GameExe;

/// <summary>
/// Le decor : ciel, collines en parallaxe, sol en terre et surtout
/// l'herbe animee (vent + elle se plie quand le perso passe dedans).
/// Tout est dessine avec des formes, aucun fichier image necessaire.
/// </summary>
public class World
{
    // --- Dimensions du monde (en pixels "virtuels", hauteur fixe 720) ---
    public const float Width = 3200f;
    public const float GroundY = 560f;   // surface de l'herbe
    public const float VirtualH = 720f;

    // --- Palette ---
    private static readonly Color SkyTop = new Color(96, 183, 242, 255);
    private static readonly Color SkyBottom = new Color(214, 243, 255, 255);
    private static readonly Color HillsFar = new Color(158, 214, 165, 255);
    private static readonly Color HillsNear = new Color(110, 196, 118, 255);
    private static readonly Color GrassTop = new Color(96, 205, 110, 255);
    private static readonly Color GrassBottom = new Color(54, 168, 84, 255);
    private static readonly Color BladeDark = new Color(47, 150, 74, 255);
    private static readonly Color BladeLight = new Color(133, 224, 143, 255);
    private static readonly Color DirtTop = new Color(154, 107, 67, 255);
    private static readonly Color DirtBottom = new Color(104, 70, 41, 255);
    private static readonly Color[] FlowerColors =
    {
        new Color(255, 122, 165, 255), new Color(255, 209, 102, 255),
        new Color(255, 255, 255, 255), new Color(186, 148, 255, 255),
        new Color(255, 138, 101, 255),
    };

    private struct Blade { public float X, BaseY, H, Phase, Shade, Thick; }
    private struct Flower { public float X, Size, Phase; public int ColIdx; }
    private struct Dot { public float X, Y, R; public byte A; }
    private struct Stone { public float X, Y, Rx, Ry; }
    private struct Cloud { public float X, Y, S, Speed; }
    private struct Tree { public float X, S; }
    private struct Mote { public float X, Y, Phase, Speed; }
    private struct Butterfly { public float BaseX, Phase; public Color Col; }

    private readonly List<Blade> _bladesBack = new List<Blade>();
    private readonly List<Blade> _bladesFront = new List<Blade>();
    private readonly List<Flower> _flowers = new List<Flower>();
    private readonly List<Dot> _dirtDots = new List<Dot>();
    private readonly List<Stone> _stones = new List<Stone>();
    private readonly List<Cloud> _clouds = new List<Cloud>();
    private readonly List<Tree> _trees = new List<Tree>();
    private readonly List<Mote> _pollen = new List<Mote>();
    private readonly List<Butterfly> _butterflies = new List<Butterfly>();
    private readonly float[] _mushrooms = { 520f, 1480f, 2310f, 2890f };

    public World()
    {
        var rng = new Random(1234); // graine fixe : le decor est le meme a chaque lancement

        // Herbe : deux couches (arriere plus sombre/courte, avant plus claire)
        for (float x = 2; x < Width; x += U.Range(rng, 5f, 9f))
            _bladesBack.Add(new Blade { X = x, BaseY = GroundY + U.Range(rng, 0f, 5f), H = U.Range(rng, 11f, 22f), Phase = U.Range(rng, 0f, 6.28f), Shade = U.Range(rng, 0f, 0.45f), Thick = 2.2f });
        for (float x = 4; x < Width; x += U.Range(rng, 6f, 11f))
            _bladesFront.Add(new Blade { X = x, BaseY = GroundY + U.Range(rng, 6f, 15f), H = U.Range(rng, 16f, 32f), Phase = U.Range(rng, 0f, 6.28f), Shade = U.Range(rng, 0.35f, 1f), Thick = 3f });

        for (int i = 0; i < 52; i++)
            _flowers.Add(new Flower { X = U.Range(rng, 30f, Width - 30f), Size = U.Range(rng, 2.6f, 4.2f), Phase = U.Range(rng, 0f, 6.28f), ColIdx = rng.Next(FlowerColors.Length) });

        for (int i = 0; i < 300; i++)
            _dirtDots.Add(new Dot { X = U.Range(rng, 0f, Width), Y = U.Range(rng, GroundY + 26f, VirtualH - 6f), R = U.Range(rng, 1.5f, 4f), A = (byte)rng.Next(28, 80) });
        for (int i = 0; i < 34; i++)
            _stones.Add(new Stone { X = U.Range(rng, 20f, Width - 20f), Y = U.Range(rng, GroundY + 34f, VirtualH - 16f), Rx = U.Range(rng, 7f, 15f), Ry = U.Range(rng, 4.5f, 8f) });

        for (int i = 0; i < 9; i++)
            _clouds.Add(new Cloud { X = U.Range(rng, 0f, 2400f), Y = U.Range(rng, 45f, 190f), S = U.Range(rng, 0.7f, 1.5f), Speed = U.Range(rng, 4f, 11f) });
        for (int i = 0; i < 14; i++)
            _trees.Add(new Tree { X = U.Range(rng, 0f, 2600f), S = U.Range(rng, 0.7f, 1.3f) });
        for (int i = 0; i < 42; i++)
            _pollen.Add(new Mote { X = U.Range(rng, 0f, Width), Y = U.Range(rng, GroundY - 260f, GroundY - 24f), Phase = U.Range(rng, 0f, 6.28f), Speed = U.Range(rng, 0.5f, 1.4f) });

        _butterflies.Add(new Butterfly { BaseX = 700f, Phase = 0f, Col = new Color(255, 150, 90, 255) });
        _butterflies.Add(new Butterfly { BaseX = 1750f, Phase = 2.4f, Col = new Color(170, 140, 255, 255) });
        _butterflies.Add(new Butterfly { BaseX = 2600f, Phase = 4.6f, Col = new Color(110, 200, 255, 255) });
    }

    /// <summary>Rafale de vent qui voyage de gauche a droite dans l'herbe.</summary>
    private static float Gust(float time, float x)
    {
        float s = U.Sin(time * 0.6f - x * 0.006f);
        float gust = Math.Max(0f, s);
        return gust * gust * gust * 7f;
    }

    public void Update(float dt)
    {
        for (int i = 0; i < _clouds.Count; i++)
        {
            var c = _clouds[i];
            c.X += c.Speed * dt;
            if (c.X > 2600f) c.X = -260f;
            _clouds[i] = c;
        }
    }

    /// <param name="viewX">Bord gauche de l'ecran dans le monde.</param>
    /// <param name="viewW">Largeur visible (depend de la fenetre).</param>
    public void Draw(float viewX, float viewW, float time, Player player)
    {
        float left = viewX - 40f, w = viewW + 80f;

        // --- Ciel + soleil ---
        DrawRectangleGradientV((int)left, -40, (int)w, (int)(GroundY + 80), SkyTop, SkyBottom);
        var sun = new Vector2(viewX + viewW * 0.82f, 118f);
        DrawCircleV(sun, 74f, Fade(new Color(255, 226, 130, 255), 0.20f));
        DrawCircleV(sun, 52f, Fade(new Color(255, 226, 130, 255), 0.35f));
        DrawCircleV(sun, 38f, new Color(255, 224, 115, 255));
        DrawCircleV(sun, 32f, new Color(255, 236, 160, 255));

        // --- Nuages (parallaxe douce) ---
        foreach (var c in _clouds)
        {
            float cx = viewX + Mod(c.X - viewX * 0.12f, viewW + 520f) - 260f;
            DrawCloud(cx, c.Y, c.S);
        }

        // --- Collines (2 couches de parallaxe) ---
        DrawHills(viewX, viewW, 0.25f, 150f, 0.0038f, HillsFar);
        foreach (var t in _trees) // petits arbres sur les collines
        {
            float tx = viewX + Mod(t.X - viewX * 0.45f, viewW + 300f) - 150f;
            float ty = GroundY - 58f * t.S - U.Sin((tx + viewX * 0.45f) * 0.004f) * 14f;
            DrawTree(tx, ty + 18f, t.S);
        }
        DrawHills(viewX, viewW, 0.55f, 84f, 0.0052f, HillsNear);

        // --- Terre ---
        DrawRectangleGradientV((int)left, (int)GroundY, (int)w, (int)(VirtualH - GroundY + 60), DirtTop, DirtBottom);
        // couches de terre (strates)
        DrawRectangle((int)left, (int)(GroundY + 52), (int)w, 7, new Color(0, 0, 0, 26));
        DrawRectangle((int)left, (int)(GroundY + 104), (int)w, 9, new Color(0, 0, 0, 34));
        foreach (var d in _dirtDots)
        {
            if (d.X < left || d.X > left + w) continue;
            DrawCircleV(new Vector2(d.X, d.Y), d.R, new Color(60, 40, 22, d.A));
        }
        foreach (var s in _stones)
        {
            if (s.X < left || s.X > left + w) continue;
            DrawEllipse((int)s.X, (int)s.Y, s.Rx, s.Ry, new Color(150, 142, 132, 255));
            DrawEllipse((int)(s.X - s.Rx * 0.25f), (int)(s.Y - s.Ry * 0.3f), s.Rx * 0.55f, s.Ry * 0.5f, new Color(172, 163, 152, 255));
        }

        // --- Le rebord d'herbe epais et ondule ---
        DrawRectangleGradientV((int)left, (int)(GroundY - 5), (int)w, 27, GrassTop, GrassBottom);
        for (float x = (float)Math.Floor(left / 18f) * 18f; x < left + w; x += 18f)
            DrawCircleV(new Vector2(x + 9f, GroundY - 4f + U.Sin(x * 0.11f) * 1.6f), 9.5f, GrassTop);
        DrawRectangle((int)left, (int)(GroundY + 20), (int)w, 6, new Color(0, 0, 0, 42)); // ombre sous le rebord

        // Racines qui depassent sous l'herbe
        for (float x = (float)Math.Floor(left / 64f) * 64f + 20f; x < left + w; x += 64f)
        {
            var root = new Color(110, 78, 48, 255);
            DrawLineEx(new Vector2(x, GroundY + 20), new Vector2(x - 5, GroundY + 36), 2.5f, root);
            DrawLineEx(new Vector2(x, GroundY + 20), new Vector2(x + 6, GroundY + 32), 2f, root);
        }

        // --- Pancarte de depart + drapeau d'arrivee ---
        DrawSign(190f, time);
        DrawFlag(3020f, time);

        // --- Herbe animee (couche arriere, puis fleurs/champignons, puis couche avant) ---
        DrawBlades(_bladesBack, left, w, time, player);
        foreach (var f in _flowers)
        {
            if (f.X < left || f.X > left + w) continue;
            float sway = U.Sin(time * 1.7f + f.X * 0.025f + f.Phase) * 3f + Gust(time, f.X);
            var stemBase = new Vector2(f.X, GroundY + 10f);
            var head = new Vector2(f.X + sway, GroundY - 12f - f.Size * 2f);
            DrawLineEx(stemBase, head, 2f, BladeDark);
            var col = FlowerColors[f.ColIdx];
            for (int k = 0; k < 5; k++)
            {
                float a = k * 1.2566f + f.Phase;
                DrawCircleV(head + new Vector2(U.Cos(a), U.Sin(a)) * f.Size * 1.15f, f.Size, col);
            }
            DrawCircleV(head, f.Size * 0.75f, new Color(255, 205, 70, 255));
        }
        foreach (var mx in _mushrooms) DrawMushroom(mx);
        DrawBlades(_bladesFront, left, w, time, player);

        // --- Pollen flottant + papillons ---
        foreach (var m in _pollen)
        {
            if (m.X < left || m.X > left + w) continue;
            float px = m.X + U.Sin(time * m.Speed + m.Phase) * 16f;
            float py = m.Y + U.Cos(time * m.Speed * 0.7f + m.Phase * 2f) * 10f;
            float a = 0.25f + 0.35f * (0.5f + 0.5f * U.Sin(time * 2f + m.Phase * 3f));
            DrawCircleV(new Vector2(px, py), 2f, Fade(new Color(255, 244, 190, 255), a));
        }
        foreach (var b in _butterflies) DrawButterfly(b, time);
    }

    /// <summary>Grosses herbes floues tout devant, dessinees APRES le perso (profondeur).</summary>
    public void DrawForeground(float viewX, float viewW, float time)
    {
        var col = new Color(38, 128, 64, 150);
        float parallax = viewX * 1.25f;
        for (float i = 0; i < 16; i++)
        {
            float bx = viewX + Mod(i * 173f + 60f - parallax, viewW + 200f) - 100f;
            float h = 26f + (i % 5) * 7f;
            float bend = U.Sin(time * 1.4f + i * 1.7f) * 8f;
            var a = new Vector2(bx, VirtualH + 8f);
            var b = new Vector2(bx + bend * 0.4f, VirtualH + 8f - h * 0.6f);
            var c = new Vector2(bx + bend, VirtualH + 8f - h);
            DrawLineEx(a, b, 7f, col);
            DrawLineEx(b, c, 5f, col);
            DrawCircleV(c, 3f, col);
        }
    }

    private void DrawBlades(List<Blade> blades, float left, float w, float time, Player player)
    {
        foreach (var b in blades)
        {
            if (b.X < left || b.X > left + w) continue;
            float bend = U.Sin(time * 1.7f + b.X * 0.025f + b.Phase) * 3.2f + Gust(time, b.X);

            // Le perso ecarte l'herbe en passant : LE detail qui rend tout vivant
            float dx = b.X - player.Pos.X;
            if (Math.Abs(dx) < 62f && Math.Abs(player.Pos.Y - GroundY) < 30f)
            {
                float force = (1f - Math.Abs(dx) / 62f);
                bend += Math.Sign(dx == 0 ? player.Facing : dx) * force * (7f + Math.Abs(player.Vel.X) * 0.028f);
            }

            var col = U.LerpColor(BladeDark, BladeLight, b.Shade);
            var p0 = new Vector2(b.X, b.BaseY);
            var p1 = new Vector2(b.X + bend * 0.35f, b.BaseY - b.H * 0.55f);
            var p2 = new Vector2(b.X + bend, b.BaseY - b.H);
            DrawLineEx(p0, p1, b.Thick, col);
            DrawLineEx(p1, p2, b.Thick * 0.7f, col);
        }
    }

    private static void DrawHills(float viewX, float viewW, float parallax, float height, float freq, Color col)
    {
        for (float sx = -10f; sx < viewW + 10f; sx += 10f)
        {
            float wx = viewX * parallax + sx;
            float h = height + U.Sin(wx * freq) * height * 0.32f + U.Sin(wx * freq * 0.37f + 2.1f) * height * 0.22f;
            DrawRectangle((int)(viewX + sx), (int)(GroundY - h + 10f), 11, (int)(h + 14f), col);
            DrawCircleV(new Vector2(viewX + sx + 5f, GroundY - h + 13f), 5.5f, col); // sommet arrondi
        }
    }

    private static void DrawCloud(float x, float y, float s)
    {
        var col = new Color(255, 255, 255, 235);
        var shade = new Color(222, 238, 250, 235);
        DrawEllipse((int)x, (int)(y + 8f * s), 46f * s, 17f * s, shade);
        DrawCircleV(new Vector2(x - 26f * s, y), 20f * s, col);
        DrawCircleV(new Vector2(x + 2f * s, y - 11f * s), 26f * s, col);
        DrawCircleV(new Vector2(x + 30f * s, y), 19f * s, col);
        DrawEllipse((int)x, (int)y, 44f * s, 16f * s, col);
    }

    private static void DrawTree(float x, float groundY, float s)
    {
        DrawRectangle((int)(x - 3.5f * s), (int)(groundY - 26f * s), (int)(7f * s), (int)(28f * s), new Color(122, 86, 54, 255));
        var green = new Color(84, 178, 96, 255);
        DrawCircleV(new Vector2(x, groundY - 42f * s), 24f * s, green);
        DrawCircleV(new Vector2(x - 17f * s, groundY - 30f * s), 16f * s, green);
        DrawCircleV(new Vector2(x + 17f * s, groundY - 30f * s), 16f * s, green);
        DrawCircleV(new Vector2(x - 7f * s, groundY - 50f * s), 9f * s, new Color(110, 205, 120, 255));
    }

    private static void DrawMushroom(float x)
    {
        DrawRectangleRounded(new Rectangle(x - 4f, GroundY - 8f, 8f, 14f), 0.5f, 6, new Color(245, 236, 216, 255));
        DrawCircleSector(new Vector2(x, GroundY - 7f), 13f, 180f, 360f, 18, new Color(232, 86, 74, 255));
        DrawCircleV(new Vector2(x - 5f, GroundY - 13f), 2.2f, Color.White);
        DrawCircleV(new Vector2(x + 4f, GroundY - 16f), 1.8f, Color.White);
        DrawCircleV(new Vector2(x + 7f, GroundY - 10f), 1.4f, Color.White);
    }

    private static void DrawSign(float x, float time)
    {
        var wood = new Color(150, 104, 62, 255);
        var woodDark = new Color(120, 80, 46, 255);
        DrawRectangle((int)(x - 4), (int)(GroundY - 52), 8, 54, woodDark);
        float wobble = U.Sin(time * 1.3f) * 1f;
        var board = new Rectangle(x - 44f, GroundY - 84f + wobble, 96f, 36f);
        DrawRectangleRounded(board, 0.25f, 8, wood);
        DrawRectangleRoundedLinesEx(board, 0.25f, 8, 2.5f, woodDark);
        const string txt = "PAR LA >";
        int tw = MeasureText(txt, 19);
        DrawText(txt, (int)(x + 4 - tw / 2), (int)(GroundY - 75 + wobble), 19, new Color(80, 52, 28, 255));
    }

    private static void DrawFlag(float x, float time)
    {
        DrawRectangle((int)(x - 3), (int)(GroundY - 130), 6, 132, new Color(120, 86, 54, 255));
        DrawCircleV(new Vector2(x, GroundY - 133), 6f, new Color(255, 205, 70, 255));
        var col = new Color(255, 110, 90, 255);
        // drapeau qui ondule : 3 segments en triangles
        var top = new Vector2(x + 3, GroundY - 128);
        float wave = U.Sin(time * 6f) * 4f;
        var p1 = new Vector2(x + 30, GroundY - 120 + wave * 0.5f);
        var p2 = new Vector2(x + 56, GroundY - 112 + wave);
        var b1 = new Vector2(x + 3, GroundY - 98);
        var b2 = new Vector2(x + 30, GroundY - 100 + wave * 0.5f);
        DrawTriangle(top, b1, p1, col);
        DrawTriangle(p1, b1, b2, col);
        DrawTriangle(p1, b2, p2, col);
    }

    private void DrawButterfly(Butterfly b, float time)
    {
        float x = b.BaseX + U.Sin(time * 0.35f + b.Phase) * 160f;
        float y = GroundY - 90f + U.Sin(time * 0.9f + b.Phase * 2f) * 34f;
        float flap = Math.Abs(U.Cos(time * 16f + b.Phase));
        float wingW = U.Lerp(2.5f, 8f, flap);
        DrawEllipse((int)(x - wingW), (int)(y - 3), wingW, 5f, b.Col);
        DrawEllipse((int)(x + wingW), (int)(y - 3), wingW, 5f, b.Col);
        DrawLineEx(new Vector2(x, y - 5), new Vector2(x, y + 5), 2.5f, new Color(60, 48, 40, 255));
    }

    private static float Mod(float v, float m)
    {
        float r = v % m;
        return r < 0 ? r + m : r;
    }
}
