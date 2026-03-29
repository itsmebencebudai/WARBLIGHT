using System.Drawing;
namespace WARBLIGHT;

public enum EnemyShape { Circle, Square, Triangle, Diamond, Hexagon, Star, Capsule, Pentagon }

public abstract class Enemy
{
    public float X, Y;
    public float HP, MaxHP;
    public float Speed;
    public float Damage;
    public int XPValue;
    public Color EnemyColor;
    public float Size = 10f;
    public EnemyShape Shape = EnemyShape.Circle;
    public string? SpriteName = null;
    public bool IsAlive = true;
    public bool IsElite = false;
    private float _contactTimer = 0f;

    public List<(float damage, float timer)> PoisonEffects = new();
    public float SlowMultiplier = 1f;
    public bool IsDebuffed = false;
    public float DebuffTimer = 0f;

    public virtual void Update(GameState gs, float dt)
    {
        for (int i = PoisonEffects.Count - 1; i >= 0; i--)
        {
            var (dmg, t) = PoisonEffects[i];
            HP -= dmg * dt;
            t -= dt;
            if (t <= 0) PoisonEffects.RemoveAt(i);
            else PoisonEffects[i] = (dmg, t);
        }
        if (HP <= 0) { IsAlive = false; OnDeath(gs); return; }

        if (DebuffTimer > 0) DebuffTimer -= dt;
        else IsDebuffed = false;

        if (_contactTimer > 0) _contactTimer -= dt;

        SlowMultiplier = 1f;
    }

    public void TakeDamage(float amount, GameState gs)
    {
        if (IsDebuffed) amount *= 1.5f;
        HP -= amount;
        if (HP <= 0) { IsAlive = false; OnDeath(gs); }
    }

    public virtual void OnDeath(GameState gs)
    {
        // spawn simple particles on death
        try { gs.SpawnParticles(X, Y, EnemyColor, 12); } catch { }
    }

    public void AddPoison(float dmgPerSec, float duration)
        => PoisonEffects.Add((dmgPerSec, duration));

    public void MakeElite()
    {
        IsElite = true;
        MaxHP *= 2; HP = MaxHP;
        Speed *= 1.2f;
        EnemyColor = Color.FromArgb(
            Math.Min(255, EnemyColor.R + 60),
            Math.Min(255, EnemyColor.G + 60),
            Math.Min(255, EnemyColor.B + 60));
    }

    protected void MoveTowardPlayer(GameState gs, float dt, float speedOverride = -1f)
    {
        float spd = (speedOverride < 0 ? Speed : speedOverride) * SlowMultiplier;
        float dx = gs.Player.X - X;
        float dy = gs.Player.Y - Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        if (dist > 0.1f)
        {
            X += (dx / dist) * spd * dt;
            Y += (dy / dist) * spd * dt;
        }
        X = Math.Clamp(X, 10f, 1990f);
        Y = Math.Clamp(Y, 10f, 1990f);
    }

    protected float DistToPlayer(GameState gs)
    {
        float dx = gs.Player.X - X, dy = gs.Player.Y - Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
}

// ===== GRUNT =====
public class Grunt : Enemy
{
    public Grunt() { HP = MaxHP = 10; Speed = 60; Damage = 5; XPValue = 2; EnemyColor = Color.FromArgb(0, 180, 200); Size = 10f; Shape = EnemyShape.Square; SpriteName = "grunt.png"; }
    public override void Update(GameState gs, float dt) { base.Update(gs, dt); if (IsAlive) MoveTowardPlayer(gs, dt); }
}

// ===== RUSHER =====
public class Rusher : Enemy
{
    private float _pauseTimer = 0f;
    private bool _isPaused = false;
    private bool _isBursting = false;
    private float _burstTimer = 0f;
    public Rusher() { HP = MaxHP = 8; Speed = 120; Damage = 8; XPValue = 3; EnemyColor = Color.Yellow; Size = 9f; Shape = EnemyShape.Triangle; SpriteName = "rusher.png"; }
    public override void Update(GameState gs, float dt)
    {
        base.Update(gs, dt); if (!IsAlive) return;
        float dist = DistToPlayer(gs);
        if (_isBursting)
        {
            _burstTimer -= dt;
            if (_burstTimer <= 0) { _isBursting = false; }
            else MoveTowardPlayer(gs, dt, Speed * 3f);
        }
        else if (_isPaused)
        {
            _pauseTimer -= dt;
            if (_pauseTimer <= 0) { _isPaused = false; _isBursting = true; _burstTimer = 0.3f; }
        }
        else
        {
            if (dist < 200f && !_isPaused) { _isPaused = true; _pauseTimer = 0.5f; }
            else MoveTowardPlayer(gs, dt);
        }
    }
}

// ===== TANK =====
public class Tank : Enemy
{
    public Tank() { HP = MaxHP = 100; Speed = 30; Damage = 15; XPValue = 8; EnemyColor = Color.DarkRed; Size = 20f; Shape = EnemyShape.Hexagon; SpriteName = "tank.png"; }
    public override void Update(GameState gs, float dt) { base.Update(gs, dt); if (IsAlive) MoveTowardPlayer(gs, dt); }
}

// ===== SHARD (spawned by Splitter) =====
public class Shard : Enemy
{
    public Shard() { HP = MaxHP = 5; Speed = 100; Damage = 3; XPValue = 1; EnemyColor = Color.Magenta; Size = 6f; Shape = EnemyShape.Triangle; }
    public override void Update(GameState gs, float dt) { base.Update(gs, dt); if (IsAlive) MoveTowardPlayer(gs, dt); }
}

// ===== SPLITTER =====
public class Splitter : Enemy
{
    private static readonly Random _rng = new();
    public Splitter() { HP = MaxHP = 25; Speed = 80; Damage = 8; XPValue = 4; EnemyColor = Color.Magenta; Size = 12f; Shape = EnemyShape.Diamond; }
    public override void Update(GameState gs, float dt) { base.Update(gs, dt); if (IsAlive) MoveTowardPlayer(gs, dt); }
    public override void OnDeath(GameState gs)
    {
        for (int i = 0; i < 2; i++)
        {
            float angle = _rng.NextSingle() * MathF.PI * 2f;
            var shard = new Shard { X = X + MathF.Cos(angle) * 15, Y = Y + MathF.Sin(angle) * 15 };
            gs.SpawnEnemy(shard);
        }
    }
}

// ===== GHOST =====
public class Ghost : Enemy
{
    public bool IsPhased = false;
    private float _phaseTimer = 2f;
    public Ghost() { HP = MaxHP = 20; Speed = 70; Damage = 8; XPValue = 5; EnemyColor = Color.FromArgb(200, 220, 220, 220); Size = 10f; Shape = EnemyShape.Circle; }
    public override void Update(GameState gs, float dt)
    {
        _phaseTimer -= dt;
        if (_phaseTimer <= 0) { IsPhased = !IsPhased; _phaseTimer = 2f; }
        base.Update(gs, dt); if (!IsAlive) return;
        MoveTowardPlayer(gs, dt);
    }
}

// ===== SHOOTER =====
public class Shooter : Enemy
{
    private float _fireTimer = 1.5f;
    private static readonly Random _rng = new();
    public Shooter() { HP = MaxHP = 30; Speed = 80; Damage = 0; XPValue = 6; EnemyColor = Color.Orange; Size = 11f; Shape = EnemyShape.Square; SpriteName = "shooter.png"; }
    public override void Update(GameState gs, float dt)
    {
        base.Update(gs, dt); if (!IsAlive) return;
        float dist = DistToPlayer(gs);
        if (dist > 250f) MoveTowardPlayer(gs, dt);
        _fireTimer -= dt;
        if (_fireTimer <= 0)
        {
            _fireTimer = 1.5f;
            FireAtPlayer(gs);
        }
    }
    private void FireAtPlayer(GameState gs)
    {
        for (int i = 0; i < 2; i++)
        {
            float dx = gs.Player.X - X, dy = gs.Player.Y - Y;
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist < 0.1f) continue;
            float angle = MathF.Atan2(dy, dx) + (_rng.NextSingle() - 0.5f) * 0.2f;
            float spd = 180f;
            gs.SpawnProjectile(new Projectile
            {
                X = X, Y = Y, VX = MathF.Cos(angle) * spd, VY = MathF.Sin(angle) * spd,
                Damage = 8, Size = 5f, Color = Color.OrangeRed, Owner = ProjectileOwner.Enemy, LifeTimer = 4f
            });
        }
    }
}

// ===== SWARMER =====
public class Swarmer : Enemy
{
    public Swarmer() { HP = MaxHP = 3; Speed = 110; Damage = 4; XPValue = 1; EnemyColor = Color.LightGreen; Size = 5f; Shape = EnemyShape.Triangle; }
    public override void Update(GameState gs, float dt) { base.Update(gs, dt); if (IsAlive) MoveTowardPlayer(gs, dt); }
}

// ===== LEECH =====
public class Leech : Enemy
{
    private float _drainTimer = 0f;
    public Leech() { HP = MaxHP = 20; Speed = 50; Damage = 0; XPValue = 4; EnemyColor = Color.Purple; Size = 9f; Shape = EnemyShape.Diamond; }
    public override void Update(GameState gs, float dt)
    {
        base.Update(gs, dt); if (!IsAlive) return;
        float dist = DistToPlayer(gs);
        if (dist < 12f)
        {
            X = gs.Player.X; Y = gs.Player.Y;
            _drainTimer += dt;
            if (_drainTimer >= 0.5f) { _drainTimer = 0; gs.Player.TakeDamage(1f); }
        }
        else { MoveTowardPlayer(gs, dt); }
    }
}

// ===== BOMBER =====
public class Bomber : Enemy
{
    private bool _exploded = false;
    public Bomber() { HP = MaxHP = 40; Speed = 90; Damage = 10; XPValue = 7; EnemyColor = Color.OrangeRed; Size = 13f; Shape = EnemyShape.Star; SpriteName = "bomber.png"; }
    public override void Update(GameState gs, float dt)
    {
        base.Update(gs, dt); if (!IsAlive) return;
        float dist = DistToPlayer(gs);
        if (dist < 50f && !_exploded)
        {
            _exploded = true;
            gs.DoExplosion(X, Y, 80f * gs.Player.AoESizeMultiplier, 50f);
            IsAlive = false; OnDeath(gs);
        }
        else MoveTowardPlayer(gs, dt);
    }
}

// ===== HOLLOW KING BOSS =====
public class HollowKing : Enemy
{
    private float _sweepDir = 1f;
    private float _summonTimer = 10f;
    private static readonly Random _rng = new();
    public HollowKing(float px, float py)
    {
        HP = MaxHP = 1000; Speed = 60; Damage = 20; XPValue = 100;
        EnemyColor = Color.FromArgb(180, 50, 50); Size = 30f; Shape = EnemyShape.Hexagon; SpriteName = "hollowking.png";
        X = px; Y = py - 200;
    }
    public override void Update(GameState gs, float dt)
    {
        base.Update(gs, dt); if (!IsAlive) return;
        X += _sweepDir * Speed * SlowMultiplier * dt;
        if (X < 100 || X > 1900) _sweepDir *= -1;
        Y += (gs.Player.Y - Y) * 0.3f * dt;
        X = Math.Clamp(X, 50f, 1950f); Y = Math.Clamp(Y, 50f, 1950f);
        _summonTimer -= dt;
        if (_summonTimer <= 0)
        {
            _summonTimer = 10f;
            for (int i = 0; i < 8; i++)
            {
                float angle = i * MathF.PI * 2 / 8;
                var g = new Grunt { X = X + MathF.Cos(angle) * 60, Y = Y + MathF.Sin(angle) * 60 };
                gs.SpawnEnemy(g);
            }
        }
    }
}

// ===== PLAGUE MOTHER BOSS =====
public class PlagueMother : Enemy
{
    private static readonly Random _rng = new();
    private float _spawnTimer = 3f;
    public PlagueMother(float px, float py)
    {
        HP = MaxHP = 1500; Speed = 0; Damage = 0; XPValue = 150;
        EnemyColor = Color.FromArgb(100, 200, 80); Size = 35f; Shape = EnemyShape.Circle; SpriteName = "plaguemother.png";
        X = px + (_rng.NextSingle() - 0.5f) * 300 + 300;
        Y = py + (_rng.NextSingle() - 0.5f) * 300;
        X = Math.Clamp(X, 100f, 1900f); Y = Math.Clamp(Y, 100f, 1900f);
    }
    public override void Update(GameState gs, float dt)
    {
        base.Update(gs, dt); if (!IsAlive) return;
        _spawnTimer -= dt;
        if (_spawnTimer <= 0)
        {
            _spawnTimer = 3f;
            for (int i = 0; i < 8; i++)
            {
                float angle = i * MathF.PI * 2 / 8;
                var s = new Swarmer { X = X + MathF.Cos(angle) * 50, Y = Y + MathF.Sin(angle) * 50 };
                gs.SpawnEnemy(s);
            }
        }
    }
}
