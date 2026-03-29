using System.Windows.Forms;
namespace WARBLIGHT;
public class Player
{
    public float X = 1000f, Y = 1000f;
    public float HP = 100f, MaxHP = 100f;
    public float XP = 0f, XPToNextLevel = 100f;
    public int Level = 1;
    public float Speed = 180f;
    public float InvulTimer = 0f;
    public float LifestealPercent = 0f;
    public float DamageMultiplier = 1f;
    public float CritChance = 0f;
    public float AoESizeMultiplier = 1f;
    public float ProjectileSpeedMultiplier = 1f;
    public float XPPickupRadius = 40f;

    public void Update(GameState gs, float dt, HashSet<Keys> keys)
    {
        float dx = 0, dy = 0;
        if (keys.Contains(Keys.W) || keys.Contains(Keys.Up)) dy -= 1;
        if (keys.Contains(Keys.S) || keys.Contains(Keys.Down)) dy += 1;
        if (keys.Contains(Keys.A) || keys.Contains(Keys.Left)) dx -= 1;
        if (keys.Contains(Keys.D) || keys.Contains(Keys.Right)) dx += 1;
        float len = MathF.Sqrt(dx * dx + dy * dy);
        if (len > 0) { dx /= len; dy /= len; }
        X = Math.Clamp(X + dx * Speed * dt, 10f, 1990f);
        Y = Math.Clamp(Y + dy * Speed * dt, 10f, 1990f);
        if (InvulTimer > 0) InvulTimer -= dt;
    }

    public void TakeDamage(float amount)
    {
        if (InvulTimer > 0) return;
        HP = Math.Max(0, HP - amount);
        InvulTimer = 0.5f;
    }

    public void Heal(float amount)
        => HP = Math.Min(MaxHP, HP + amount);
}
