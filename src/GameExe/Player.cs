using System;
using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace GameExe;

/// <summary>
/// Le petit perso : physique de platformer "qui repond bien"
/// (coyote time, buffer de saut, saut variable) et animation
/// entierement procedurale : squash &amp; stretch, course, respiration,
/// clignements, echarpe qui flotte, pousse sur la tete qui rebondit.
/// </summary>
public class Player
{
    // --- Reglages (pixels / secondes) ---
    private const float Gravity = 2300f;
    private const float WalkSpeed = 270f;
    private const float RunSpeed = 415f;
    private const float JumpVelocity = -865f;
    private const float MaxFall = 1150f;
    private const float GroundAccel = 3000f;
    private const float GroundDecel = 3600f;
    private const float AirAccel = 1900f;

    // --- Palette du perso ---
    private static readonly Color Body = new Color(255, 243, 217, 255);
    private static readonly Color BodyShade = new Color(247, 223, 180, 255);
    private static readonly Color Belly = new Color(255, 251, 240, 255);
    private static readonly Color Boot = new Color(124, 74, 45, 255);
    private static readonly Color BootDark = new Color(99, 57, 33, 255);
    private static readonly Color Scarf = new Color(255, 122, 69, 255);
    private static readonly Color ScarfDark = new Color(240, 92, 46, 255);
    private static readonly Color Leaf = new Color(76, 195, 107, 255);
    private static readonly Color LeafDark = new Color(53, 165, 83, 255);
    private static readonly Color Eye = new Color(51, 38, 26, 255);
    private static readonly Color Cheek = new Color(255, 163, 145, 140);

    public Vector2 Pos;          // position des PIEDS (centre-bas)
    public Vector2 Vel;
    public int Facing = 1;       // 1 = droite, -1 = gauche
    public bool OnGround = true;

    private float _coyote;       // grace period apres avoir quitte le sol
    private float _jumpBuffer;   // saut appuye un peu avant de toucher le sol
    private bool _wasGround = true;

    // Animation
    private float _runPhase;
    private float _idleTime;
    private float _sx = 1f, _sy = 1f, _sxV, _syV;        // squash & stretch (ressorts)
    private float _leafAngle, _leafVel;                  // la pousse sur la tete (ressort)
    private float _blinkTimer = 2.2f, _blink;            // 0 = yeux ouverts, 1 = fermes
    private float _dustTimer;
    private readonly Random _rng = new Random(4242);

    public Player(float startX)
    {
        Pos = new Vector2(startX, World.GroundY);
    }

    public void Update(float dt, ParticleSystem fx)
    {
        // --- Entrees clavier (fleches + ZQSD + WASD, pense aux claviers AZERTY) ---
        bool left = IsKeyDown(KeyboardKey.Left) || IsKeyDown(KeyboardKey.A) || IsKeyDown(KeyboardKey.Q);
        bool right = IsKeyDown(KeyboardKey.Right) || IsKeyDown(KeyboardKey.D);
        bool jumpPressed = IsKeyPressed(KeyboardKey.Space) || IsKeyPressed(KeyboardKey.Up)
                        || IsKeyPressed(KeyboardKey.W) || IsKeyPressed(KeyboardKey.Z);
        bool jumpReleased = IsKeyReleased(KeyboardKey.Space) || IsKeyReleased(KeyboardKey.Up)
                         || IsKeyReleased(KeyboardKey.W) || IsKeyReleased(KeyboardKey.Z);
        bool run = IsKeyDown(KeyboardKey.LeftShift) || IsKeyDown(KeyboardKey.RightShift);

        int input = (right ? 1 : 0) - (left ? 1 : 0);
        float maxSpeed = run ? RunSpeed : WalkSpeed;

        // Derapage : on change de sens en pleine course -> poussiere !
        if (OnGround && input != 0 && Math.Sign(Vel.X) == -input && Math.Abs(Vel.X) > 230f)
        {
            _dustTimer -= dt * 4f;
            if (_dustTimer <= 0f) { fx.SpawnDust(Pos.X, Pos.Y - 2, 2, 40f, Vel.X * 0.25f); _dustTimer = 0.05f; }
        }

        // --- Deplacement horizontal (acceleration douce) ---
        float accel = OnGround ? (input == 0 ? GroundDecel : GroundAccel) : AirAccel;
        Vel.X = U.MoveToward(Vel.X, input * maxSpeed, accel * dt);
        if (input != 0) Facing = input;

        // --- Saut : buffer + coyote + hauteur variable ---
        if (jumpPressed) _jumpBuffer = 0.12f; else _jumpBuffer -= dt;
        if (OnGround) _coyote = 0.10f; else _coyote -= dt;

        if (_jumpBuffer > 0f && _coyote > 0f)
        {
            Vel.Y = JumpVelocity;
            OnGround = false;
            _coyote = 0f; _jumpBuffer = 0f;
            _sx = 0.80f; _sy = 1.24f;                 // on s'etire en sautant
            _leafVel += 5.5f;
            fx.SpawnDust(Pos.X, Pos.Y - 2, 7, 80f);
            fx.SpawnGrassBits(Pos.X, Pos.Y, 3);
        }
        if (jumpReleased && Vel.Y < -320f) Vel.Y = -320f; // relacher tot = petit saut

        // --- Gravite ---
        Vel.Y = Math.Min(Vel.Y + Gravity * dt, MaxFall);

        // --- Integration + collision avec le sol plat ---
        Pos += Vel * dt;
        Pos.X = U.Clamp(Pos.X, 34f, World.Width - 34f);

        if (Pos.Y >= World.GroundY)
        {
            Pos.Y = World.GroundY;
            if (!_wasGround) // atterrissage !
            {
                float impact = U.Clamp(Vel.Y / 1150f, 0.25f, 1f);
                _sx = 1f + 0.26f * impact;            // on s'ecrase...
                _sy = 1f - 0.28f * impact;
                _leafVel -= 6f * impact;
                fx.SpawnDust(Pos.X, Pos.Y - 2, (int)(6 + 8 * impact), 90f);
                fx.SpawnGrassBits(Pos.X, Pos.Y, (int)(3 + 5 * impact));
                fx.SpawnRing(Pos.X, Pos.Y, 0.6f + impact * 0.7f);
            }
            Vel.Y = 0f;
            OnGround = true;
        }
        else OnGround = false;
        _wasGround = OnGround;

        // --- Animation : ressorts du squash & stretch ---
        Spring(ref _sx, ref _sxV, 1f, 210f, 15f, dt);
        Spring(ref _sy, ref _syV, 1f, 210f, 15f, dt);
        float leafTarget = U.Clamp(-Vel.X * 0.0009f, -0.4f, 0.4f) + U.Sin((float)GetTime() * 2.1f) * 0.06f;
        Spring(ref _leafAngle, ref _leafVel, leafTarget, 95f, 8f, dt);

        // Cycle de course + poussiere reguliere quand on court vite
        if (OnGround && Math.Abs(Vel.X) > 12f)
        {
            _runPhase += Math.Abs(Vel.X) * dt * 0.052f;
            _idleTime = 0f;
            if (Math.Abs(Vel.X) > 290f)
            {
                _dustTimer -= dt;
                if (_dustTimer <= 0f) { fx.SpawnDust(Pos.X - Facing * 10f, Pos.Y - 2, 1, 26f, -Vel.X * 0.18f); _dustTimer = 0.09f; }
            }
        }
        else _idleTime += dt;

        // Clignement des yeux
        _blinkTimer -= dt;
        if (_blinkTimer <= 0f) { _blinkTimer = U.Range(_rng, 1.8f, 4.4f); _blink = 1f; }
        _blink = Math.Max(0f, _blink - dt * 9f);
    }

    private static void Spring(ref float x, ref float v, float target, float stiffness, float damping, float dt)
    {
        v += (-(stiffness) * (x - target) - damping * v) * dt;
        x += v * dt;
    }

    public void Draw(float time)
    {
        float speedNorm = U.Clamp(Math.Abs(Vel.X) / RunSpeed, 0f, 1f);
        bool moving = OnGround && Math.Abs(Vel.X) > 12f;

        // Respiration au repos + rebond de course
        float breathe = (moving || !OnGround) ? 0f : U.Sin(time * 2.3f) * 0.022f;
        float bob = moving ? Math.Abs(U.Sin(_runPhase)) * 4.2f : Math.Max(0f, U.Sin(time * 2.3f)) * 1.1f;

        float sx = _sx * (1f - breathe * 0.6f);
        float sy = _sy * (1f + breathe);
        if (!OnGround) // en l'air, le corps s'etire avec la vitesse verticale
        {
            float stretch = U.Clamp(Math.Abs(Vel.Y) / 5200f, 0f, 0.16f);
            sy *= 1f + stretch; sx *= 1f - stretch * 0.55f;
        }

        float bodyW = 48f * sx, bodyH = 54f * sy;
        float lean = U.Clamp(Vel.X * 0.00038f, -0.20f, 0.20f); // penche dans le sens du mouvement
        float cx = Pos.X + lean * 26f;
        float bottomY = Pos.Y - 5f - bob;
        float cy = bottomY - bodyH * 0.5f;

        // --- Ombre au sol (retrecit quand on saute haut) ---
        float heightAbove = World.GroundY - Pos.Y;
        float shadowScale = U.Clamp(1f - heightAbove / 460f, 0.3f, 1f);
        DrawEllipse((int)Pos.X, (int)(World.GroundY + 9), 24f * shadowScale, 6.5f * shadowScale,
            new Color(40, 90, 40, (int)(90 * shadowScale)));

        // --- Pieds (petites bottes animees) ---
        Vector2 footA, footB;
        if (moving)
        {
            footA = new Vector2(Pos.X + U.Cos(_runPhase) * 12f * Facing - 2f, Pos.Y - 3f - Math.Max(0f, U.Sin(_runPhase)) * 8f);
            footB = new Vector2(Pos.X + U.Cos(_runPhase + 3.14159f) * 12f * Facing + 2f, Pos.Y - 3f - Math.Max(0f, U.Sin(_runPhase + 3.14159f)) * 8f);
        }
        else if (!OnGround)
        {
            float tuck = Vel.Y < 0 ? 13f : 6f; // pieds replies en montee, tendus en chute
            footA = new Vector2(Pos.X - 9f, Pos.Y - tuck);
            footB = new Vector2(Pos.X + 10f, Pos.Y - tuck * 0.6f);
        }
        else
        {
            footA = new Vector2(Pos.X - 10f, Pos.Y - 3f);
            footB = new Vector2(Pos.X + 10f, Pos.Y - 3f);
        }
        DrawFoot(footB, Facing, BootDark); // pied arriere d'abord
        DrawFoot(footA, Facing, Boot);

        // --- Bras arriere + queue d'echarpe (derriere le corps) ---
        DrawArm(cx, cy, bodyW, bodyH, back: true, moving, time, speedNorm);
        DrawScarfTail(cx, cy, bodyH, time, speedNorm);

        // --- Corps (grosse goutte cremeuse) ---
        DrawEllipse((int)cx, (int)(cy + 2.5f), bodyW * 0.5f, bodyH * 0.5f, BodyShade); // leger contour bas
        DrawEllipse((int)cx, (int)cy, bodyW * 0.5f, bodyH * 0.5f, Body);
        DrawEllipse((int)(cx + Facing * 3f), (int)(cy + bodyH * 0.17f), bodyW * 0.29f, bodyH * 0.24f, Belly);

        // --- Bras avant ---
        DrawArm(cx, cy, bodyW, bodyH, back: false, moving, time, speedNorm);

        // --- Echarpe (le bandeau autour du cou) ---
        var scarfBand = new Rectangle(cx - bodyW * 0.34f + Facing * 2f, cy + bodyH * 0.04f, bodyW * 0.68f, 11f);
        DrawRectangleRounded(scarfBand, 0.9f, 10, Scarf);
        DrawRectangleRounded(new Rectangle(scarfBand.X, scarfBand.Y + 6f, scarfBand.Width, 5f), 0.9f, 8, ScarfDark);

        // --- Visage ---
        float faceX = cx + Facing * 9f;
        float lookY = OnGround ? 0f : U.Clamp(Vel.Y * 0.004f, -2.5f, 3f);
        float eyeY = cy - bodyH * 0.20f + lookY;
        float eyeOpen = 1f - _blink;
        if (!OnGround && Vel.Y > 720f) eyeOpen *= 0.45f; // il plisse les yeux en chute rapide

        DrawEllipse((int)(faceX - 15f), (int)(eyeY + 8f), 5.5f, 3.2f, Cheek);
        DrawEllipse((int)(faceX + 15f), (int)(eyeY + 8f), 5.5f, 3.2f, Cheek);
        DrawEye(faceX - 8.5f, eyeY, eyeOpen);
        DrawEye(faceX + 8.5f, eyeY, eyeOpen);

        if (!OnGround && Vel.Y > 500f) // bouche "o" de surprise en chute
            DrawCircleV(new Vector2(faceX, eyeY + 11f), 3.4f, Eye);
        else // sourire
            DrawRing(new Vector2(faceX, eyeY + 5f), 4.6f, 6.4f, 25f, 155f, 12, Eye);

        // --- La pousse sur la tete (suivante, avec ressort) ---
        float topX = cx + lean * 34f;
        float topY = cy - bodyH * 0.48f;
        var stemTop = new Vector2(topX + U.Sin(_leafAngle) * 12f, topY - 11f + (1f - U.Cos(_leafAngle)) * 3f);
        DrawLineEx(new Vector2(topX, topY + 2f), stemTop, 3f, LeafDark);
        // deux feuilles
        DrawEllipse((int)(stemTop.X - 8f), (int)(stemTop.Y - 3f), 9f, 5f, LeafDark);
        DrawEllipse((int)(stemTop.X - 8f), (int)(stemTop.Y - 4f), 8f, 4.2f, Leaf);
        DrawEllipse((int)(stemTop.X + 8f), (int)(stemTop.Y - 5f), 10f, 5.6f, LeafDark);
        DrawEllipse((int)(stemTop.X + 8f), (int)(stemTop.Y - 6f), 9f, 4.8f, Leaf);
        DrawCircleV(stemTop, 2.6f, LeafDark);
    }

    private void DrawFoot(Vector2 f, int facing, Color col)
    {
        DrawEllipse((int)(f.X + facing * 3f), (int)f.Y, 9.5f, 5.2f, col);
        DrawEllipse((int)(f.X + facing * 4.5f), (int)(f.Y - 1.6f), 6.5f, 3.4f, col);
    }

    private void DrawEye(float x, float y, float open)
    {
        float ry = Math.Max(0.9f, 6.2f * open);
        DrawEllipse((int)x, (int)y, 4.5f, ry, Eye);
        if (open > 0.5f)
            DrawCircleV(new Vector2(x + Facing * 1.4f, y - 2.2f), 1.6f, Color.White);
    }

    private void DrawArm(float cx, float cy, float bodyW, float bodyH, bool back, bool moving, float time, float speedNorm)
    {
        var col = back ? BodyShade : Body;
        float side = back ? -1f : 1f;
        var shoulder = new Vector2(cx + side * Facing * bodyW * 0.30f - Facing * 2f, cy + bodyH * 0.02f);

        float swing;
        if (!OnGround) swing = Vel.Y < 0 ? -0.9f : 0.55f + U.Sin(time * 22f) * 0.22f; // bras en l'air / qui moulinent
        else if (moving) swing = U.Sin(_runPhase + (back ? 0f : 3.14159f)) * (0.35f + speedNorm * 0.45f);
        else swing = U.Sin(time * 2.3f) * 0.07f;

        var hand = new Vector2(
            shoulder.X + (U.Sin(swing) * 13f * Facing) + side * Facing * 3f,
            shoulder.Y + U.Cos(swing) * 13f - (OnGround ? 0f : 4f));
        DrawLineEx(shoulder, hand, 7.5f, col);
        DrawCircleV(hand, 4.4f, col);
    }

    private void DrawScarfTail(float cx, float cy, float bodyH, float time, float speedNorm)
    {
        var neck = new Vector2(cx - Facing * 12f, cy + bodyH * 0.08f);
        var prev = neck;
        float flutter = 2.5f + speedNorm * 4f + (OnGround ? 0f : 2.5f);
        float lift = U.Clamp(-Vel.Y * 0.006f, -4f, 5f) - Vel.X * Facing * 0.004f;
        for (int i = 1; i <= 4; i++)
        {
            var p = new Vector2(
                neck.X - Facing * i * 11.5f,
                neck.Y + i * 2.6f + U.Sin(time * 9f - i * 1.15f) * flutter + lift * i * 0.4f);
            var col = (i % 2 == 0) ? Scarf : ScarfDark;
            DrawLineEx(prev, p, U.Lerp(9.5f, 4.5f, i / 4f), col);
            DrawCircleV(p, U.Lerp(5f, 2.6f, i / 4f), col);
            prev = p;
        }
    }
}
