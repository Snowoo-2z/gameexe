using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace GameExe;

/// <summary>
/// Petits effets qui rendent le jeu vivant : poussiere sous les pieds,
/// brins d'herbe qui volent, anneau d'impact a l'atterrissage.
/// </summary>
public class ParticleSystem
{
    private enum Kind { Dust, GrassBit }

    private struct Particle
    {
        public Kind Type;
        public Vector2 Pos, Vel;
        public float Life, MaxLife, Size, Rot, RotVel, Gravity;
        public Color Col;
    }

    private struct Ring
    {
        public Vector2 Pos;
        public float Life, MaxLife, MaxRadius;
    }

    private readonly List<Particle> _particles = new List<Particle>();
    private readonly List<Ring> _rings = new List<Ring>();
    private readonly Random _rng = new Random(9876);

    /// <summary>Nuage de poussiere (course, atterrissage, derapage).</summary>
    public void SpawnDust(float x, float y, int count, float spread = 60f, float baseVx = 0f)
    {
        for (int i = 0; i < count; i++)
        {
            _particles.Add(new Particle
            {
                Type = Kind.Dust,
                Pos = new Vector2(x + U.Range(_rng, -8, 8), y + U.Range(_rng, -3, 2)),
                Vel = new Vector2(baseVx + U.Range(_rng, -spread, spread), U.Range(_rng, -55, -8)),
                Life = 0, MaxLife = U.Range(_rng, 0.35f, 0.7f),
                Size = U.Range(_rng, 3f, 6.5f),
                Gravity = -30f, // la poussiere flotte legerement
                Col = new Color(232, 214, 175, 255),
            });
        }
    }

    /// <summary>Petits bouts d'herbe arraches qui retombent en virevoltant.</summary>
    public void SpawnGrassBits(float x, float y, int count)
    {
        for (int i = 0; i < count; i++)
        {
            _particles.Add(new Particle
            {
                Type = Kind.GrassBit,
                Pos = new Vector2(x + U.Range(_rng, -10, 10), y - 2),
                Vel = new Vector2(U.Range(_rng, -120, 120), U.Range(_rng, -260, -120)),
                Life = 0, MaxLife = U.Range(_rng, 0.5f, 0.9f),
                Size = U.Range(_rng, 5f, 9f),
                Rot = U.Range(_rng, 0, 6.28f), RotVel = U.Range(_rng, -12, 12),
                Gravity = 620f,
                Col = U.LerpColor(new Color(52, 160, 82, 255), new Color(126, 217, 140, 255), (float)_rng.NextDouble()),
            });
        }
    }

    /// <summary>Onde de choc au sol quand on atterrit.</summary>
    public void SpawnRing(float x, float y, float strength = 1f)
    {
        _rings.Add(new Ring { Pos = new Vector2(x, y + 6), Life = 0, MaxLife = 0.32f, MaxRadius = 42f * strength });
    }

    public void Update(float dt)
    {
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.Life += dt;
            if (p.Life >= p.MaxLife) { _particles.RemoveAt(i); continue; }
            p.Vel.Y += p.Gravity * dt;
            p.Vel.X *= (1f - 2.2f * dt); // freinage dans l'air
            p.Pos += p.Vel * dt;
            p.Rot += p.RotVel * dt;
            _particles[i] = p;
        }
        for (int i = _rings.Count - 1; i >= 0; i--)
        {
            var r = _rings[i];
            r.Life += dt;
            if (r.Life >= r.MaxLife) { _rings.RemoveAt(i); continue; }
            _rings[i] = r;
        }
    }

    public void Draw()
    {
        foreach (var r in _rings)
        {
            float t = r.Life / r.MaxLife;
            float radius = U.Lerp(8f, r.MaxRadius, 1f - (1f - t) * (1f - t)); // ease-out
            var col = Fade(new Color(240, 226, 190, 255), (1f - t) * 0.55f);
            DrawEllipseLines((int)r.Pos.X, (int)r.Pos.Y, radius, radius * 0.28f, col);
        }

        foreach (var p in _particles)
        {
            float t = p.Life / p.MaxLife;
            if (p.Type == Kind.Dust)
            {
                float size = p.Size * (1f + t * 1.6f); // la poussiere gonfle en s'estompant
                DrawCircleV(p.Pos, size, Fade(p.Col, (1f - t) * 0.5f));
            }
            else
            {
                var dir = new Vector2(U.Cos(p.Rot), U.Sin(p.Rot)) * (p.Size * 0.5f);
                DrawLineEx(p.Pos - dir, p.Pos + dir, 2.5f, Fade(p.Col, 1f - t));
            }
        }
    }
}
