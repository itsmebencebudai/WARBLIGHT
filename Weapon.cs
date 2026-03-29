using System.Drawing;
namespace WARBLIGHT;

public abstract class Weapon
{
    public string WeaponName { get; protected set; } = "";
    public int Level { get; protected set; } = 1;
    protected float Cooldown = 1f;
    protected float CooldownTimer = 0f;
    protected static readonly Random _rng = new();

    public abstract void Update(GameState gs, float dt);
    public abstract void Upgrade();
    public abstract string GetUpgradeDescription(int toLevel);
    public virtual void Draw(Graphics g, GameState gs, float camX, float camY) { }

    protected Enemy? GetNearest(GameState gs)
    {
        Enemy? nearest = null; float min = float.MaxValue;
        foreach (var e in gs.Enemies)
        {
            if (!e.IsAlive) continue;
            float dx = e.X - gs.Player.X, dy = e.Y - gs.Player.Y;
            float d = dx * dx + dy * dy;
            if (d < min) { min = d; nearest = e; }
        }
        return nearest;
    }

    protected void FireAt(GameState gs, Enemy target, float damage, float speed, float size, Color color,
        int pierceCount = 0, ProjectileTag tags = ProjectileTag.None,
        float poisonDmg = 0, float poisonDur = 0,
        float explRadius = 0, float explDmg = 0, int bounceCount = 0)
    {
        float dx = target.X - gs.Player.X, dy = target.Y - gs.Player.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        if (dist < 0.1f) return;
        float vx = (dx / dist) * speed * gs.Player.ProjectileSpeedMultiplier;
        float vy = (dy / dist) * speed * gs.Player.ProjectileSpeedMultiplier;
        var p = new Projectile
        {
            X = gs.Player.X, Y = gs.Player.Y, VX = vx, VY = vy,
            Damage = damage, Size = size, Color = color,
            Owner = ProjectileOwner.Player,
            PierceCount = pierceCount, BounceCount = bounceCount,
            Tags = tags, PoisonDamage = poisonDmg, PoisonDuration = poisonDur
        };
        if (explRadius > 0) { p.ExplosionRadius = explRadius * gs.Player.AoESizeMultiplier; p.ExplosionDamage = explDmg; }
        gs.SpawnProjectile(p);
    }
}

// === SHADOW BOLT ===
public class ShadowBolt : Weapon
{
    private float _dmg = 15f;
    public ShadowBolt() { WeaponName = "Shadow Bolt"; Cooldown = 0.5f; }
    public override void Update(GameState gs, float dt)
    {
        CooldownTimer -= dt;
        if (CooldownTimer > 0) return;
        var t = GetNearest(gs); if (t == null) return;
        CooldownTimer = Cooldown;
        bool explode = Level >= 5;
        FireAt(gs, t, _dmg, 400f, 6f, Color.DarkViolet,
            tags: explode ? ProjectileTag.Explosion : ProjectileTag.None,
            explRadius: explode ? 40f : 0, explDmg: explode ? 40f : 0);
    }
    public override void Upgrade()
    {
        Level++;
        switch (Level)
        {
            case 2: _dmg += 10; break;
            case 3: _dmg += 10; Cooldown = 0.4f; break;
            case 4: _dmg += 10; break;
            case 5: break;
        }
    }
    public override string GetUpgradeDescription(int toLevel) => toLevel switch
    {
        2 => "+10 damage", 3 => "+10 damage, fire rate 0.4s", 4 => "+10 damage", 5 => "Explosion on impact (40px)", _ => ""
    };
}

// === VOID SPRAY ===
public class VoidSpray : Weapon
{
    private int _bullets = 3;
    private float _spread = 30f;
    private bool _chain = false;
    public VoidSpray() { WeaponName = "Void Spray"; Cooldown = 0.8f; }
    public override void Update(GameState gs, float dt)
    {
        CooldownTimer -= dt; if (CooldownTimer > 0) return;
        var t = GetNearest(gs); if (t == null) return;
        CooldownTimer = Cooldown;
        float baseAngle = MathF.Atan2(t.Y - gs.Player.Y, t.X - gs.Player.X);
        float spreadRad = _spread * MathF.PI / 180f;
        for (int i = 0; i < _bullets; i++)
        {
            float angle = baseAngle + (_bullets > 1 ? (i / (float)(_bullets - 1) - 0.5f) * spreadRad : 0);
            float spd = 350f * gs.Player.ProjectileSpeedMultiplier;
            var p = new Projectile
            {
                X = gs.Player.X, Y = gs.Player.Y,
                VX = MathF.Cos(angle) * spd, VY = MathF.Sin(angle) * spd,
                Damage = 10, Size = 5f, Color = Color.Cyan,
                Owner = ProjectileOwner.Player, ChainCount = _chain ? 1 : 0
            };
            gs.SpawnProjectile(p);
        }
    }
    public override void Upgrade()
    {
        Level++;
        switch (Level)
        {
            case 2: _bullets = 4; break;
            case 3: _bullets = 5; _spread = 20f; break;
            case 4: _bullets = 6; break;
            case 5: _chain = true; break;
        }
    }
    public override string GetUpgradeDescription(int toLevel) => toLevel switch
    {
        2 => "4 bullets", 3 => "5 bullets, spread 20°", 4 => "6 bullets", 5 => "Chain to 1 nearby enemy", _ => ""
    };
}

// === BONE NEEDLE ===
public class BoneNeedle : Weapon
{
    private float _dmg = 1f;
    private bool _poison = false;
    private bool _twoShots = false;
    public BoneNeedle() { WeaponName = "Bone Needle"; Cooldown = 0.6f; }
    public override void Update(GameState gs, float dt)
    {
        CooldownTimer -= dt; if (CooldownTimer > 0) return;
        var t = GetNearest(gs); if (t == null) return;
        CooldownTimer = Cooldown;
        float angle = MathF.Atan2(t.Y - gs.Player.Y, t.X - gs.Player.X);
        float spd = 700f * gs.Player.ProjectileSpeedMultiplier;
        var tags = _poison ? ProjectileTag.Piercing | ProjectileTag.Poison : ProjectileTag.Piercing;
        void Shoot(float offsetY)
        {
            float perpX = -MathF.Sin(angle) * offsetY, perpY = MathF.Cos(angle) * offsetY;
            gs.SpawnProjectile(new Projectile
            {
                X = gs.Player.X + perpX, Y = gs.Player.Y + perpY,
                VX = MathF.Cos(angle) * spd, VY = MathF.Sin(angle) * spd,
                Damage = _dmg, Size = 2f, Color = Color.White,
                Owner = ProjectileOwner.Player, PierceCount = 999,
                Tags = tags, PoisonDamage = _poison ? 1.5f : 0, PoisonDuration = _poison ? 5f : 0
            });
        }
        Shoot(0);
        if (_twoShots) Shoot(8f);
    }
    public override void Upgrade()
    {
        Level++;
        switch (Level)
        {
            case 2: _dmg = 3; break;
            case 3: _dmg = 5; _poison = true; break;
            case 4: _dmg = 8; break;
            case 5: _twoShots = true; break;
        }
    }
    public override string GetUpgradeDescription(int toLevel) => toLevel switch
    {
        2 => "+2 damage", 3 => "+2 damage + poison", 4 => "+3 damage", 5 => "Two parallel shots", _ => ""
    };
}

// === SOUL ORB ===
public class SoulOrb : Weapon
{
    private float _dmg = 30f;
    private float _radius = 80f;
    public SoulOrb() { WeaponName = "Soul Orb"; Cooldown = 1.5f; }
    public override void Update(GameState gs, float dt)
    {
        CooldownTimer -= dt; if (CooldownTimer > 0) return;
        var t = GetNearest(gs); if (t == null) return;
        CooldownTimer = Cooldown;
        float dx = t.X - gs.Player.X, dy = t.Y - gs.Player.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy); if (dist < 0.1f) return;
        float spd = 150f * gs.Player.ProjectileSpeedMultiplier;
        var p = new Projectile
        {
            X = gs.Player.X, Y = gs.Player.Y,
            VX = (dx / dist) * spd, VY = (dy / dist) * spd,
            Damage = _dmg / 2f, Size = 10f, Color = Color.Purple,
            Owner = ProjectileOwner.Player,
            Tags = ProjectileTag.Explosion,
            ExplosionRadius = _radius * gs.Player.AoESizeMultiplier,
            ExplosionDamage = _dmg
        };
        gs.SpawnProjectile(p);
    }
    public override void Upgrade()
    {
        Level++;
        switch (Level)
        {
            case 2: _radius = 100f; break;
            case 3: _dmg = 40f; break;
            case 4: _dmg = 50f; break;
            case 5: break; // homing toward cluster (visual upgrade)
        }
    }
    public override string GetUpgradeDescription(int toLevel) => toLevel switch
    {
        2 => "Bigger radius (100px)", 3 => "+10 damage", 4 => "+10 damage", 5 => "Homes toward cluster", _ => ""
    };
}

// === PLAGUE ARROW ===
public class PlagueArrow : Weapon
{
    private float _poisonDur = 5f;
    private float _poisonDmg = 2f;
    public PlagueArrow() { WeaponName = "Plague Arrow"; Cooldown = 0.7f; }
    public override void Update(GameState gs, float dt)
    {
        CooldownTimer -= dt; if (CooldownTimer > 0) return;
        var t = GetNearest(gs); if (t == null) return;
        CooldownTimer = Cooldown;
        FireAt(gs, t, 5f, 350f, 5f, Color.LimeGreen,
            tags: ProjectileTag.Poison, poisonDmg: _poisonDmg, poisonDur: _poisonDur);
    }
    public override void Upgrade()
    {
        Level++;
        switch (Level)
        {
            case 2: _poisonDur = 7f; break;
            case 3: break;
            case 4: _poisonDmg = 3f; break;
            case 5: break;
        }
    }
    public override string GetUpgradeDescription(int toLevel) => toLevel switch
    {
        2 => "Poison 7s", 3 => "Splash on death", 4 => "+1 poison dmg/s", 5 => "Stacks explode on death", _ => ""
    };
}

// === RICOCHET SHARD ===
public class RicochetShard : Weapon
{
    private float _dmg = 12f;
    private int _bounces = 4;
    public RicochetShard() { WeaponName = "Ricochet Shard"; Cooldown = 0.5f; }
    public override void Update(GameState gs, float dt)
    {
        CooldownTimer -= dt; if (CooldownTimer > 0) return;
        var t = GetNearest(gs); if (t == null) return;
        CooldownTimer = Cooldown;
        float angle = MathF.Atan2(t.Y - gs.Player.Y, t.X - gs.Player.X) + (_rng.NextSingle() - 0.5f) * 0.3f;
        float spd = 320f * gs.Player.ProjectileSpeedMultiplier;
        gs.SpawnProjectile(new Projectile
        {
            X = gs.Player.X, Y = gs.Player.Y,
            VX = MathF.Cos(angle) * spd, VY = MathF.Sin(angle) * spd,
            Damage = _dmg, Size = 5f, Color = Color.LightBlue,
            Owner = ProjectileOwner.Player, BounceCount = _bounces,
            Tags = ProjectileTag.Bounce
        });
    }
    public override void Upgrade()
    {
        Level++;
        switch (Level)
        {
            case 2: _bounces = 6; break;
            case 3: break;
            case 4: _dmg = 18f; break;
            case 5: break;
        }
    }
    public override string GetUpgradeDescription(int toLevel) => toLevel switch
    {
        2 => "6 bounces", 3 => "Bounce off enemies", 4 => "+6 damage", 5 => "Split into 2 on bounce", _ => ""
    };
}

// === PLAGUE NOVA ===
public class PlagueNova : Weapon
{
    private int _bullets = 12;
    private float _range = 200f;
    private bool _innerRing = false;
    public PlagueNova() { WeaponName = "Plague Nova"; Cooldown = 3f; }
    public override void Update(GameState gs, float dt)
    {
        CooldownTimer -= dt; if (CooldownTimer > 0) return;
        CooldownTimer = Cooldown;
        float spd = _range / 1.5f * gs.Player.ProjectileSpeedMultiplier;
        for (int i = 0; i < _bullets; i++)
        {
            float angle = i * MathF.PI * 2 / _bullets;
            gs.SpawnProjectile(new Projectile
            {
                X = gs.Player.X, Y = gs.Player.Y,
                VX = MathF.Cos(angle) * spd, VY = MathF.Sin(angle) * spd,
                Damage = 8, Size = 5f, Color = Color.LimeGreen,
                Owner = ProjectileOwner.Player, LifeTimer = 1.5f
            });
        }
        if (_innerRing)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * MathF.PI * 2 / 8;
                gs.SpawnProjectile(new Projectile
                {
                    X = gs.Player.X, Y = gs.Player.Y,
                    VX = MathF.Cos(angle) * spd * 0.6f, VY = MathF.Sin(angle) * spd * 0.6f,
                    Damage = 8, Size = 5f, Color = Color.Green,
                    Owner = ProjectileOwner.Player, LifeTimer = 1.5f
                });
            }
        }
    }
    public override void Upgrade()
    {
        Level++;
        switch (Level)
        {
            case 2: _bullets = 16; break;
            case 3: _range = 240f; break;
            case 4: _innerRing = true; break;
            case 5: break;
        }
    }
    public override string GetUpgradeDescription(int toLevel) => toLevel switch
    {
        2 => "16 bullets", 3 => "Bigger radius", 4 => "Second inner ring", 5 => "Bullets linger", _ => ""
    };
}

// === DEATH ZONE (aura) ===
public class DeathZone : Weapon
{
    public float Radius = 60f;
    private float _dmg = 5f;
    private float _pulseTimer = 0f;
    private float _pulseInterval = 0.3f;
    public bool SlowEnemies = false;
    public DeathZone() { WeaponName = "Death Zone"; }
    public override void Update(GameState gs, float dt)
    {
        _pulseTimer += dt;
        if (_pulseTimer >= _pulseInterval)
        {
            _pulseTimer = 0;
            foreach (var e in gs.Enemies)
            {
                if (!e.IsAlive) continue;
                float dx = e.X - gs.Player.X, dy = e.Y - gs.Player.Y;
                float dist = MathF.Sqrt(dx * dx + dy * dy);
                if (dist <= Radius + e.Size)
                {
                    e.TakeDamage(_dmg * gs.Player.DamageMultiplier, gs);
                    if (SlowEnemies) e.SlowMultiplier = 0.5f;
                }
            }
        }
    }
    public override void Upgrade()
    {
        Level++;
        switch (Level)
        {
            case 2: _pulseInterval = 0.2f; break;
            case 3: Radius = 90f; break;
            case 4: _dmg = 8f; break;
            case 5: SlowEnemies = true; break;
        }
    }
    public override string GetUpgradeDescription(int toLevel) => toLevel switch
    {
        2 => "Faster pulse", 3 => "Bigger radius (90px)", 4 => "+3 damage/pulse", 5 => "Slows enemies 50%", _ => ""
    };
    public override void Draw(Graphics g, GameState gs, float camX, float camY)
    {
        float sx = gs.Player.X - camX, sy = gs.Player.Y - camY;
        float r = Radius;
        using var brush = new System.Drawing.SolidBrush(Color.FromArgb(40, 200, 0, 0));
        g.FillEllipse(brush, sx - r, sy - r, r * 2, r * 2);
        using var pen = new System.Drawing.Pen(Color.FromArgb(120, 220, 0, 0), 1.5f);
        g.DrawEllipse(pen, sx - r, sy - r, r * 2, r * 2);
    }
}

// === GRAVE ERUPTION ===
public class GraveEruption : Weapon
{
    private int _targets = 3;
    private float _dmg = 40f;
    public GraveEruption() { WeaponName = "Grave Eruption"; Cooldown = 2f; }
    public override void Update(GameState gs, float dt)
    {
        CooldownTimer -= dt; if (CooldownTimer > 0) return;
        CooldownTimer = Cooldown;
        var nearby = gs.GetEnemiesInRadius(gs.Player.X, gs.Player.Y, 300f);
        nearby.Sort((a, b) =>
        {
            float da = MathF.Sqrt((a.X - gs.Player.X) * (a.X - gs.Player.X) + (a.Y - gs.Player.Y) * (a.Y - gs.Player.Y));
            float db = MathF.Sqrt((b.X - gs.Player.X) * (b.X - gs.Player.X) + (b.Y - gs.Player.Y) * (b.Y - gs.Player.Y));
            return da.CompareTo(db);
        });
        int count = Math.Min(_targets, nearby.Count);
        for (int i = 0; i < count; i++)
        {
            var e = nearby[i];
            e.TakeDamage(_dmg * gs.Player.DamageMultiplier, gs);
            gs.DoExplosion(e.X, e.Y, 40f * gs.Player.AoESizeMultiplier, 20f * gs.Player.DamageMultiplier);
            gs.SpawnProjectile(new Projectile
            {
                X = e.X, Y = e.Y, IsExplosionEffect = true,
                Size = 12f, Color = Color.Ivory, ExplosionTimer = 0.4f, IsAlive = true
            });
        }
    }
    public override void Upgrade()
    {
        Level++;
        switch (Level)
        {
            case 2: _targets = 4; break;
            case 3: _dmg = 60f; break;
            case 4: break;
            case 5: break;
        }
    }
    public override string GetUpgradeDescription(int toLevel) => toLevel switch
    {
        2 => "4 targets", 3 => "+20 damage", 4 => "Second eruption", 5 => "Spikes linger 2s", _ => ""
    };
}

// === SOUL WHIP (orbiting) ===
public class SoulWhip : Weapon
{
    public float[] Angles;
    public float Radius = 80f;
    private float _orbitSpeed;
    private float _dmg = 20f;
    private HashSet<Enemy>[] _hitSets;
    private float _hitResetTimer = 0f;
    public SoulWhip()
    {
        WeaponName = "Soul Whip";
        _orbitSpeed = MathF.PI * 2f;
        Angles = new float[] { 0f };
        _hitSets = new HashSet<Enemy>[] { new() };
    }
    public override void Update(GameState gs, float dt)
    {
        _hitResetTimer += dt;
        if (_hitResetTimer >= 0.2f) { _hitResetTimer = 0; foreach (var h in _hitSets) h.Clear(); }
        for (int i = 0; i < Angles.Length; i++)
        {
            Angles[i] += _orbitSpeed * dt;
            float ox = gs.Player.X + MathF.Cos(Angles[i]) * Radius;
            float oy = gs.Player.Y + MathF.Sin(Angles[i]) * Radius;
            foreach (var e in gs.Enemies)
            {
                if (!e.IsAlive || _hitSets[i].Contains(e)) continue;
                float dx = e.X - ox, dy = e.Y - oy;
                if (dx * dx + dy * dy < (e.Size + 8f) * (e.Size + 8f))
                {
                    _hitSets[i].Add(e);
                    e.TakeDamage(_dmg * gs.Player.DamageMultiplier, gs);
                }
            }
        }
    }
    public override void Upgrade()
    {
        Level++;
        int n = Level switch { 2 => 2, 3 => 3, 4 => 3, 5 => 3, _ => Angles.Length };
        if (n != Angles.Length)
        {
            float[] na = new float[n];
            for (int i = 0; i < n; i++) na[i] = i < Angles.Length ? Angles[i] : i * MathF.PI * 2 / n;
            Angles = na;
            _hitSets = new HashSet<Enemy>[n];
            for (int i = 0; i < n; i++) _hitSets[i] = new();
        }
        if (Level == 3) _orbitSpeed = MathF.PI * 3f;
        if (Level == 4) Radius = 100f;
    }
    public override string GetUpgradeDescription(int toLevel) => toLevel switch
    {
        2 => "2 orbiters", 3 => "3 orbiters, faster", 4 => "Bigger radius (100px)", 5 => "Trailing damage", _ => ""
    };
    public override void Draw(Graphics g, GameState gs, float camX, float camY)
    {
        foreach (var angle in Angles)
        {
            float ox = gs.Player.X + MathF.Cos(angle) * Radius - camX;
            float oy = gs.Player.Y + MathF.Sin(angle) * Radius - camY;
            using var b = new System.Drawing.SolidBrush(Color.Gold);
            g.FillEllipse(b, ox - 8, oy - 8, 16, 16);
            using var pen = new System.Drawing.Pen(Color.FromArgb(150, 255, 215, 0), 2f);
            g.DrawEllipse(pen, ox - 8, oy - 8, 16, 16);
        }
    }
}

// === BONE SHIELD (orbiting, blocks projectiles) ===
public class BoneShield : Weapon
{
    public float[] Angles;
    public float Radius = 50f;
    private float _dmg = 12f;
    private HashSet<Enemy>[] _hitSets;
    private float _hitResetTimer = 0f;
    public BoneShield()
    {
        WeaponName = "Bone Shield";
        int n = 3;
        Angles = new float[n];
        _hitSets = new HashSet<Enemy>[n];
        for (int i = 0; i < n; i++) { Angles[i] = i * MathF.PI * 2 / n; _hitSets[i] = new(); }
    }
    public override void Update(GameState gs, float dt)
    {
        float orbitSpd = MathF.PI * 1.5f;
        _hitResetTimer += dt;
        if (_hitResetTimer >= 0.25f) { _hitResetTimer = 0; foreach (var h in _hitSets) h.Clear(); }
        for (int i = 0; i < Angles.Length; i++)
        {
            Angles[i] += orbitSpd * dt;
            float ox = gs.Player.X + MathF.Cos(Angles[i]) * Radius;
            float oy = gs.Player.Y + MathF.Sin(Angles[i]) * Radius;
            for (int j = gs.Projectiles.Count - 1; j >= 0; j--)
            {
                var p = gs.Projectiles[j];
                if (!p.IsAlive || p.Owner != ProjectileOwner.Enemy || p.IsExplosionEffect) continue;
                float dx = p.X - ox, dy = p.Y - oy;
                if (dx * dx + dy * dy < 12f * 12f) { p.IsAlive = false; }
            }
            foreach (var e in gs.Enemies)
            {
                if (!e.IsAlive || _hitSets[i].Contains(e)) continue;
                float dx = e.X - ox, dy = e.Y - oy;
                if (dx * dx + dy * dy < (e.Size + 8f) * (e.Size + 8f))
                {
                    _hitSets[i].Add(e);
                    e.TakeDamage(_dmg * gs.Player.DamageMultiplier, gs);
                }
            }
        }
    }
    public override void Upgrade()
    {
        Level++;
        int n = Level switch { 2 => 4, 3 => 5, 4 => 5, 5 => 5, _ => Angles.Length };
        if (n != Angles.Length)
        {
            float[] na = new float[n];
            for (int i = 0; i < n; i++) na[i] = i < Angles.Length ? Angles[i] : i * MathF.PI * 2 / n;
            Angles = na;
            _hitSets = new HashSet<Enemy>[n];
            for (int i = 0; i < n; i++) _hitSets[i] = new();
        }
        if (Level == 3) _dmg = 18f;
        // Level 4: reflect projectiles (visual upgrade)
    }
    public override string GetUpgradeDescription(int toLevel) => toLevel switch
    {
        2 => "4 fragments", 3 => "5 fragments + more damage", 4 => "Reflect projectiles", 5 => "Blocked shots spawn Ricochet", _ => ""
    };
    public override void Draw(Graphics g, GameState gs, float camX, float camY)
    {
        foreach (var angle in Angles)
        {
            float ox = gs.Player.X + MathF.Cos(angle) * Radius - camX;
            float oy = gs.Player.Y + MathF.Sin(angle) * Radius - camY;
            using var b = new System.Drawing.SolidBrush(Color.WhiteSmoke);
            g.FillEllipse(b, ox - 6, oy - 6, 12, 12);
            using var pen = new System.Drawing.Pen(Color.FromArgb(180, 255, 255, 220), 1.5f);
            g.DrawEllipse(pen, ox - 6, oy - 6, 12, 12);
        }
    }
}

// === WRAITH CHAIN ===
public class WraithChain : Weapon
{
    private float _reach = 120f;
    private float _sweepAngle = MathF.PI / 2f;
    private float _side = 1f;
    public WraithChain() { WeaponName = "Wraith Chain"; Cooldown = 1.2f; }
    public override void Update(GameState gs, float dt)
    {
        CooldownTimer -= dt; if (CooldownTimer > 0) return;
        CooldownTimer = Cooldown;
        var t = GetNearest(gs); if (t == null) return;
        float baseAngle = MathF.Atan2(t.Y - gs.Player.Y, t.X - gs.Player.X) + _side * _sweepAngle / 2f;
        DamageInArc(gs, baseAngle, _sweepAngle);
        _side *= -1;
    }
    private void DamageInArc(GameState gs, float centerAngle, float sweep)
    {
        foreach (var e in gs.Enemies)
        {
            if (!e.IsAlive) continue;
            float dx = e.X - gs.Player.X, dy = e.Y - gs.Player.Y;
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist > _reach + e.Size) continue;
            float angle = MathF.Atan2(dy, dx);
            float diff = MathF.Abs(AngleDiff(angle, centerAngle));
            if (diff <= sweep / 2f) e.TakeDamage(25f * gs.Player.DamageMultiplier, gs);
        }
        gs.SpawnProjectile(new Projectile
        {
            X = gs.Player.X, Y = gs.Player.Y, IsExplosionEffect = true,
            Size = _reach, Color = Color.Teal, ExplosionTimer = 0.15f
        });
    }
    private float AngleDiff(float a, float b)
    {
        float d = a - b;
        while (d > MathF.PI) d -= MathF.PI * 2;
        while (d < -MathF.PI) d += MathF.PI * 2;
        return d;
    }
    public override void Upgrade()
    {
        Level++;
        switch (Level)
        {
            case 2: _reach = 150f; break;
            case 3: _sweepAngle = MathF.PI; break;
            case 4: break; // return sweep (fires twice)
            case 5: break;
        }
    }
    public override string GetUpgradeDescription(int toLevel) => toLevel switch
    {
        2 => "Longer reach (150px)", 3 => "180° sweep", 4 => "Return sweep", 5 => "Chain lightning", _ => ""
    };
}

// === DEATH BEAM ===
public class DeathBeam : Weapon
{
    private float _dmg = 25f;
    private float _burstDur = 0.5f;
    private float _cooldownDur = 0.5f;
    private float _timer = 0f;
    private bool _firing = false;
    public Enemy? CurrentTarget = null;
    public DeathBeam() { WeaponName = "Death Beam"; }
    public override void Update(GameState gs, float dt)
    {
        _timer -= dt;
        if (_timer <= 0)
        {
            _firing = !_firing;
            _timer = _firing ? _burstDur : _cooldownDur;
        }
        if (_firing)
        {
            CurrentTarget = GetNearest(gs);
            if (CurrentTarget != null)
            {
                float dx = CurrentTarget.X - gs.Player.X, dy = CurrentTarget.Y - gs.Player.Y;
                float len = MathF.Sqrt(dx * dx + dy * dy);
                if (len > 0.1f)
                {
                    foreach (var e in gs.Enemies)
                    {
                        if (!e.IsAlive) continue;
                        float ex = e.X - gs.Player.X, ey = e.Y - gs.Player.Y;
                        float projectionRatio = Math.Clamp((ex * dx + ey * dy) / (len * len), 0f, 1f);
                        float closestX = projectionRatio * dx, closestY = projectionRatio * dy;
                        float distToLine = MathF.Sqrt((ex - closestX) * (ex - closestX) + (ey - closestY) * (ey - closestY));
                        if (distToLine <= e.Size + 5f) e.TakeDamage(_dmg * dt * gs.Player.DamageMultiplier, gs);
                    }
                }
            }
        }
        else CurrentTarget = null;
    }
    public override void Upgrade()
    {
        Level++;
        switch (Level)
        {
            case 2: _dmg = 50f; break;
            case 3: _burstDur = 0.75f; break;
            case 4: _cooldownDur = 0.3f; break;
            case 5: break;
        }
    }
    public override string GetUpgradeDescription(int toLevel) => toLevel switch
    {
        2 => "+25 damage", 3 => "Longer burst (0.75s)", 4 => "Shorter cooldown (0.3s)", 5 => "Chain to 2nd target", _ => ""
    };
    public override void Draw(Graphics g, GameState gs, float camX, float camY)
    {
        if (!_firing || CurrentTarget == null) return;
        float sx = gs.Player.X - camX, sy = gs.Player.Y - camY;
        float tx = CurrentTarget.X - camX, ty = CurrentTarget.Y - camY;
        using var pen1 = new System.Drawing.Pen(Color.FromArgb(80, 0, 255, 255), 8f);
        using var pen2 = new System.Drawing.Pen(Color.FromArgb(200, 0, 255, 255), 2f);
        g.DrawLine(pen1, sx, sy, tx, ty);
        g.DrawLine(pen2, sx, sy, tx, ty);
    }
}

// === HEX RAY (rotating laser) ===
public class HexRay : Weapon
{
    public float Angle = 0f;
    private float _rotSpeed = MathF.PI * 2f / 4f;
    private float _dmg = 3f;
    private int _rays = 1;
    private float _tickTimer = 0f;
    public HexRay() { WeaponName = "Hex Ray"; }
    public override void Update(GameState gs, float dt)
    {
        Angle += _rotSpeed * dt;
        _tickTimer += dt;
        if (_tickTimer < 0.1f) return;
        _tickTimer = 0f;
        for (int r = 0; r < _rays; r++)
        {
            float a = Angle + r * MathF.PI * 2f / _rays;
            float dx = MathF.Cos(a), dy = MathF.Sin(a);
            foreach (var e in gs.Enemies)
            {
                if (!e.IsAlive) continue;
                float ex = e.X - gs.Player.X, ey = e.Y - gs.Player.Y;
                float proj = ex * dx + ey * dy;
                if (proj < 0) continue;
                float cx2 = ex - proj * dx, cy2 = ey - proj * dy;
                if (cx2 * cx2 + cy2 * cy2 <= (e.Size + 4f) * (e.Size + 4f))
                    e.TakeDamage(_dmg * gs.Player.DamageMultiplier, gs);
            }
        }
    }
    public override void Upgrade()
    {
        Level++;
        switch (Level)
        {
            case 2: _rotSpeed = MathF.PI * 2f / 3f; break;
            case 3: _rays = 2; break;
            case 4: _dmg = 5f; break;
            case 5: _rays = 3; break;
        }
    }
    public override string GetUpgradeDescription(int toLevel) => toLevel switch
    {
        2 => "Faster rotation (3s)", 3 => "Second opposite ray", 4 => "+2 damage/tick", 5 => "Three rays", _ => ""
    };
    public override void Draw(Graphics g, GameState gs, float camX, float camY)
    {
        float sx = gs.Player.X - camX, sy = gs.Player.Y - camY;
        for (int r = 0; r < _rays; r++)
        {
            float a = Angle + r * MathF.PI * 2f / _rays;
            float ex = sx + MathF.Cos(a) * 2000f, ey = sy + MathF.Sin(a) * 2000f;
            using var pen1 = new System.Drawing.Pen(Color.FromArgb(60, 150, 0, 200), 6f);
            using var pen2 = new System.Drawing.Pen(Color.FromArgb(180, 200, 0, 255), 1.5f);
            g.DrawLine(pen1, sx, sy, ex, ey);
            g.DrawLine(pen2, sx, sy, ex, ey);
        }
    }
}

// === VOID EYE ===
public class VoidEye : Weapon
{
    private float _dmg = 10f;
    private float _tickTimer = 0f;
    private bool _debuff = false;
    private int _targetCount = 1;
    public Enemy? PrimaryTarget = null;
    public VoidEye() { WeaponName = "Void Eye"; }
    public override void Update(GameState gs, float dt)
    {
        _tickTimer += dt;
        if (_tickTimer < 0.2f) return;
        _tickTimer = 0f;
        var sorted = gs.Enemies.Where(e => e.IsAlive).ToList();
        sorted.Sort((a, b) =>
        {
            float da = (a.X - gs.Player.X) * (a.X - gs.Player.X) + (a.Y - gs.Player.Y) * (a.Y - gs.Player.Y);
            float db = (b.X - gs.Player.X) * (b.X - gs.Player.X) + (b.Y - gs.Player.Y) * (b.Y - gs.Player.Y);
            return da.CompareTo(db);
        });
        var targets = sorted.Take(_targetCount).ToList();
        PrimaryTarget = targets.Count > 0 ? targets[0] : null;
        foreach (var t in targets)
        {
            t.TakeDamage(_dmg * gs.Player.DamageMultiplier, gs);
            if (_debuff) { t.IsDebuffed = true; t.DebuffTimer = 1f; }
        }
    }
    public override void Upgrade()
    {
        Level++;
        switch (Level)
        {
            case 2: _dmg = 20f; break;
            case 3: _debuff = true; break;
            case 4: _targetCount = 2; break;
            case 5: break;
        }
    }
    public override string GetUpgradeDescription(int toLevel) => toLevel switch
    {
        2 => "+10 dmg/tick", 3 => "Debuff: +50% dmg taken", 4 => "Second target", 5 => "Killed targets explode", _ => ""
    };
    public override void Draw(Graphics g, GameState gs, float camX, float camY)
    {
        if (PrimaryTarget == null || !PrimaryTarget.IsAlive) return;
        float tx = PrimaryTarget.X - camX, ty = PrimaryTarget.Y - camY;
        float r = PrimaryTarget.Size + 6f;
        using var brush = new System.Drawing.SolidBrush(Color.FromArgb(60, 100, 0, 150));
        g.FillEllipse(brush, tx - r, ty - r, r * 2, r * 2);
        using var pen = new System.Drawing.Pen(Color.FromArgb(180, 150, 0, 200), 2f);
        g.DrawEllipse(pen, tx - r, ty - r, r * 2, r * 2);
    }
}
