using System;
using Raylib_cs;

namespace GameExe;

/// <summary>Petites fonctions maths utilisees partout dans le jeu.</summary>
public static class U
{
    public static float Lerp(float a, float b, float t) => a + (b - a) * t;

    public static float Clamp(float v, float min, float max)
        => v < min ? min : (v > max ? max : v);

    public static float MoveToward(float v, float target, float maxDelta)
    {
        if (v < target) return Math.Min(v + maxDelta, target);
        return Math.Max(v - maxDelta, target);
    }

    /// <summary>Amortissement fluide independant des FPS (camera, etc.).</summary>
    public static float Damp(float a, float b, float speed, float dt)
        => Lerp(a, b, 1f - (float)Math.Exp(-speed * dt));

    public static float Sin(float x) => (float)Math.Sin(x);
    public static float Cos(float x) => (float)Math.Cos(x);

    public static float Range(Random rng, float min, float max)
        => min + (float)rng.NextDouble() * (max - min);

    /// <summary>Melange de deux couleurs (le Color.Lerp de Raylib-cs 7.0.2 est bogue).</summary>
    public static Color LerpColor(Color a, Color b, float t)
        => new Color(
            (int)Lerp(a.R, b.R, t),
            (int)Lerp(a.G, b.G, t),
            (int)Lerp(a.B, b.B, t),
            (int)Lerp(a.A, b.A, t));
}
