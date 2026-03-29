using System.Windows.Forms;
namespace WARBLIGHT;

public class GameState
{
    public Player Player { get; } = new();
    public List<Enemy> Enemies { get; } = new();
    public List<Projectile> Projectiles { get; } = new();
    public List<XpOrb> XpOrbs { get; } = new();
    public List<Weapon> Weapons { get; } = new();
    public List<Particle> Particles { get; } = new();
    public bool UseSprites { get; set; } = false;
    public WaveSpawner WaveSpawner { get; } = new();
    public float GameTime { get; set; } = 0f;
    public int Score { get; set; } = 0;
    public bool IsGameOver { get; set; } = false;
    public bool IsVictory { get; set; } = false;
    public bool IsUpgradePaused { get; set; } = false;
    public List<UpgradeCard> UpgradeCards { get; } = new();
    public HashSet<Keys> KeysPressed { get; set; } = new();
    private static readonly Random _rng = new();

    public GameState() { Weapons.Add(new ShadowBolt()); }

    public void Update(float dt)
    {
        if (IsGameOver || IsVictory || IsUpgradePaused) return;
        GameTime += dt;
        Player.Update(this, dt, KeysPressed);
        foreach (var w in Weapons.ToList()) w.Update(this, dt);

        // Iterate over copies to avoid "Collection was modified" if updates spawn/remove entities
        foreach (var e in Enemies.ToList()) e.Update(this, dt);

        foreach (var p in Projectiles.ToList()) p.Update(this, dt);

        CheckPlayerProjectileVsEnemies();
        CheckEnemyProjectileVsPlayer();
        CheckEnemyContactVsPlayer();
        CollectXpOrbs();
        WaveSpawner.Update(this, dt);

        // Update particles
        foreach (var part in Particles.ToList()) part.Update(dt);

        // Spawn XP orbs and update score for dead enemies
        for (int i = 0; i < Enemies.Count; i++)
        {
            var e = Enemies[i];
            if (!e.IsAlive)
            {
                Score += e.XPValue * (e.IsElite ? 3 : 1);
                XpOrbs.Add(new XpOrb { X = e.X, Y = e.Y, Value = e.XPValue * (e.IsElite ? 2 : 1) });
            }
        }

        Enemies.RemoveAll(e => !e.IsAlive);
        Projectiles.RemoveAll(p => !p.IsAlive);
        XpOrbs.RemoveAll(o => !o.IsAlive);
        Particles.RemoveAll(p => !p.IsAlive);

        if (Player.HP <= 0) IsGameOver = true;
        if (GameTime >= 1800f) IsVictory = true;
    }

    private void CheckPlayerProjectileVsEnemies()
    {
        foreach (var p in Projectiles.ToList())
        {
            if (!p.IsAlive || p.Owner != ProjectileOwner.Player || p.IsExplosionEffect) continue;
            foreach (var e in Enemies)
            {
                if (!e.IsAlive) continue;
                if (e is Ghost gh && gh.IsPhased) continue;
                if (p.HitEnemies.Contains(e)) continue;
                float dx = e.X - p.X, dy = e.Y - p.Y;
                if (dx * dx + dy * dy > (e.Size + p.Size) * (e.Size + p.Size)) continue;
                p.HitEnemies.Add(e);
                float dmg = p.Damage * Player.DamageMultiplier;
                if (_rng.NextDouble() < Player.CritChance) dmg *= 2f;
                e.TakeDamage(dmg, this);
                if (Player.LifestealPercent > 0) Player.Heal(dmg * Player.LifestealPercent);
                if (p.Tags.HasFlag(ProjectileTag.Poison)) e.AddPoison(p.PoisonDamage, p.PoisonDuration);
                if (p.Tags.HasFlag(ProjectileTag.Explosion) && !p.HasExploded)
                {
                    p.HasExploded = true;
                    DoExplosion(p.X, p.Y, p.ExplosionRadius, p.ExplosionDamage);
                }
                if (p.PierceCount > 0) p.PierceCount--;
                else p.IsAlive = false;
                if (!p.IsAlive) break;
            }
        }
    }

    private void CheckEnemyProjectileVsPlayer()
    {
        foreach (var p in Projectiles.ToList())
        {
            if (!p.IsAlive || p.Owner != ProjectileOwner.Enemy || p.IsExplosionEffect) continue;
            float dx = Player.X - p.X, dy = Player.Y - p.Y;
            if (dx * dx + dy * dy < (10f + p.Size) * (10f + p.Size))
            {
                Player.TakeDamage(p.Damage); p.IsAlive = false;
            }
        }
    }

    private void CheckEnemyContactVsPlayer()
    {
        foreach (var e in Enemies)
        {
            if (!e.IsAlive || e.Damage <= 0) continue;
            float dx = Player.X - e.X, dy = Player.Y - e.Y;
            if (dx * dx + dy * dy < (10f + e.Size) * (10f + e.Size)) Player.TakeDamage(e.Damage);
        }
    }

    private void CollectXpOrbs()
    {
        float r = Player.XPPickupRadius;
        foreach (var orb in XpOrbs)
        {
            if (!orb.IsAlive) continue;
            float dx = Player.X - orb.X, dy = Player.Y - orb.Y;
            if (dx * dx + dy * dy <= r * r)
            {
                Player.XP += orb.Value; orb.IsAlive = false; CheckLevelUp();
            }
        }
    }

    public void CheckLevelUp()
    {
        while (Player.XP >= Player.XPToNextLevel && !IsUpgradePaused)
        {
            Player.XP -= Player.XPToNextLevel;
            Player.Level++;
            Player.XPToNextLevel = 100 + (Player.Level - 1) * 50f;
            GenerateUpgradeCards();
            IsUpgradePaused = true;
        }
    }

    private void GenerateUpgradeCards()
    {
        UpgradeCards.Clear();
        var choices = CreateUpgradeChoices();
        for (int i = choices.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (choices[i], choices[j]) = (choices[j], choices[i]);
        }
        UpgradeCards.AddRange(choices.Take(3));
    }

    private List<UpgradeCard> CreateUpgradeChoices()
    {
        var choices = new List<UpgradeCard>();
        foreach (var w in Weapons)
        {
            if (w.Level < 5)
            {
                var wType = w.GetType();
                int nextLvl = w.Level + 1;
                choices.Add(new UpgradeCard(
                    w.WeaponName + " Lv" + nextLvl,
                    w.GetUpgradeDescription(nextLvl),
                    gs => { gs.Weapons.FirstOrDefault(x => x.GetType() == wType)?.Upgrade(); }));
            }
        }
        if (Weapons.Count < 6)
        {
            var allTypes = new Type[]
            {
                typeof(VoidSpray), typeof(BoneNeedle), typeof(SoulOrb), typeof(PlagueArrow),
                typeof(RicochetShard), typeof(PlagueNova), typeof(DeathZone), typeof(GraveEruption),
                typeof(SoulWhip), typeof(BoneShield), typeof(WraithChain), typeof(DeathBeam),
                typeof(HexRay), typeof(VoidEye)
            };
            foreach (var type in allTypes)
            {
                if (Weapons.Any(w => w.GetType() == type)) continue;
                var wInst = (Weapon)Activator.CreateInstance(type)!;
                choices.Add(new UpgradeCard(
                    wInst.WeaponName + " (New)",
                    wInst.GetUpgradeDescription(1),
                    gs => { if (gs.Weapons.Count < 6) gs.Weapons.Add((Weapon)Activator.CreateInstance(type)!); }));
            }
        }
        choices.AddRange(new[]
        {
            new UpgradeCard("+Max HP", "+20 Max HP", gs => { gs.Player.MaxHP += 20; gs.Player.HP = Math.Min(gs.Player.HP + 20, gs.Player.MaxHP); }),
            new UpgradeCard("+Speed", "+20 Move Speed", gs => gs.Player.Speed += 20),
            new UpgradeCard("+Damage%", "+10% All Damage", gs => gs.Player.DamageMultiplier += 0.1f),
            new UpgradeCard("+Crit Chance", "+5% Critical Hit", gs => gs.Player.CritChance += 0.05f),
            new UpgradeCard("+Lifesteal", "+2% Lifesteal", gs => gs.Player.LifestealPercent += 0.02f),
            new UpgradeCard("+AoE Size", "+10% AoE Radius", gs => gs.Player.AoESizeMultiplier += 0.1f),
            new UpgradeCard("+Proj Speed", "+10% Projectile Speed", gs => gs.Player.ProjectileSpeedMultiplier += 0.1f),
            new UpgradeCard("+XP Range", "+20px XP Pickup Radius", gs => gs.Player.XPPickupRadius += 20f),
        });
        return choices;
    }

    public void SelectUpgradeCard(int index)
    {
        if (index < 0 || index >= UpgradeCards.Count) return;
        UpgradeCards[index].Apply(this);
        UpgradeCards.Clear();
        IsUpgradePaused = false;
    }

    public void DoExplosion(float x, float y, float radius, float damage)
    {
        foreach (var e in Enemies)
        {
            if (!e.IsAlive) continue;
            float dx = e.X - x, dy = e.Y - y;
            if (dx * dx + dy * dy <= (radius + e.Size) * (radius + e.Size))
                e.TakeDamage(damage * Player.DamageMultiplier, this);
        }
        Projectiles.Add(new Projectile
        {
            X = x, Y = y, IsExplosionEffect = true,
            Size = radius, Color = System.Drawing.Color.OrangeRed, ExplosionTimer = 0.35f
        });
    }

    public Enemy? GetNearestEnemy(float x, float y)
    {
        Enemy? n = null; float min = float.MaxValue;
        foreach (var e in Enemies)
        {
            if (!e.IsAlive) continue;
            float dx = e.X - x, dy = e.Y - y, d = dx * dx + dy * dy;
            if (d < min) { min = d; n = e; }
        }
        return n;
    }

    public List<Enemy> GetEnemiesInRadius(float x, float y, float radius)
    {
        var result = new List<Enemy>();
        float r2 = radius * radius;
        foreach (var e in Enemies)
        {
            if (!e.IsAlive) continue;
            float dx = e.X - x, dy = e.Y - y;
            if (dx * dx + dy * dy <= r2) result.Add(e);
        }
        return result;
    }

    public void SpawnEnemy(Enemy e) => Enemies.Add(e);
    public void SpawnProjectile(Projectile p) => Projectiles.Add(p);
    public void SpawnXpOrb(float x, float y, int value) => XpOrbs.Add(new XpOrb { X = x, Y = y, Value = value });
    public void SpawnParticles(float x, float y, Color color, int count)
    {
        var rnd = new Random();
        for (int i = 0; i < count; i++)
        {
            float ang = (float)(rnd.NextDouble() * Math.PI * 2);
            float spd = 20f + (float)rnd.NextDouble() * 120f;
            var p = new Particle
            {
                X = x,
                Y = y,
                VX = MathF.Cos(ang) * spd,
                VY = MathF.Sin(ang) * spd,
                Life = 0.3f + (float)rnd.NextDouble() * 0.7f,
                Color = color,
                Size = 2f + (float)rnd.NextDouble() * 3f
            };
            Particles.Add(p);
        }
    }
}
