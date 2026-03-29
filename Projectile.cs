namespace WARBLIGHT;

[Flags]
public enum ProjectileTag { None = 0, Poison = 1, Explosion = 2, Chain = 4, Piercing = 8, Bounce = 16, Linger = 32 }
public enum ProjectileOwner { Player, Enemy }

public class Projectile
{
    public float X, Y, VX, VY;
    public float Damage;
    public float Size = 5f;
    public System.Drawing.Color Color;
    public ProjectileOwner Owner;
    public int PierceCount = 0;
    public int BounceCount = 0;
    public bool IsAlive = true;
    public ProjectileTag Tags = ProjectileTag.None;
    public float PoisonDamage = 0f, PoisonDuration = 0f;
    public bool HasExploded = false;
    public float ExplosionRadius = 40f, ExplosionDamage = 20f;
    public float LifeTimer = 5f;
    public bool IsExplosionEffect = false;
    public float ExplosionTimer = 0.3f;
    public HashSet<Enemy> HitEnemies = new();
    public int ChainCount = 0;
    public float LingerTimer = 0f;

    public void Update(GameState gs, float dt)
    {
        if (!IsAlive) return;
        if (IsExplosionEffect)
        {
            ExplosionTimer -= dt;
            if (ExplosionTimer <= 0) IsAlive = false;
            return;
        }
        X += VX * dt;
        Y += VY * dt;
        LifeTimer -= dt;
        if (LifeTimer <= 0) IsAlive = false;

        if (BounceCount > 0)
        {
            bool bounced = false;
            if (X < 10 || X > 1990) { VX = -VX; X = Math.Clamp(X, 10, 1990); bounced = true; }
            if (Y < 10 || Y > 1990) { VY = -VY; Y = Math.Clamp(Y, 10, 1990); bounced = true; }
            if (bounced) { BounceCount--; HitEnemies.Clear(); }
        }
        else if (!Tags.HasFlag(ProjectileTag.Bounce))
        {
            if (X < 0 || X > 2000 || Y < 0 || Y > 2000) IsAlive = false;
        }
    }
}
