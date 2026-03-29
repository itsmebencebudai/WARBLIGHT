using System.Drawing;
using System.Drawing.Drawing2D;
namespace WARBLIGHT;

public static class Renderer
{
    private static readonly Font _fontSmall = new Font("Consolas", 10f);
    private static readonly Font _fontMed = new Font("Consolas", 14f, FontStyle.Bold);
    private static readonly Font _fontLarge = new Font("Consolas", 28f, FontStyle.Bold);
    private static readonly Font _fontCard = new Font("Consolas", 11f, FontStyle.Bold);
    private static readonly Font _fontCardDesc = new Font("Consolas", 9f);

    public static void Draw(GameState gs, Graphics g)
    {
        g.Clear(Color.Black);
        float camX = gs.Player.X - 400, camY = gs.Player.Y - 300;

        DrawGrid(g, camX, camY);
        DrawWorldBounds(g, camX, camY);
        foreach (var orb in gs.XpOrbs) DrawXpOrb(g, orb, camX, camY);
        foreach (var p in gs.Projectiles) DrawProjectile(g, p, camX, camY);
        // particles
        foreach (var part in gs.Particles) DrawParticle(g, part, camX, camY);
        foreach (var e in gs.Enemies) DrawEnemy(g, e, camX, camY, gs);
        foreach (var w in gs.Weapons) w.Draw(g, gs, camX, camY);
        DrawPlayer(g, gs.Player, camX, camY);
        DrawHUD(g, gs);
        if (gs.IsUpgradePaused) DrawUpgradeCards(g, gs);
        if (gs.IsGameOver) DrawGameOver(g, gs);
        if (gs.IsVictory) DrawVictory(g, gs);
    }

    private static void DrawParticle(Graphics g, Particle p, float camX, float camY)
    {
        float sx = p.X - camX, sy = p.Y - camY;
        if (sx < -20 || sx > 820 || sy < -20 || sy > 620) return;
        using var b = new SolidBrush(p.Color);
        float r = p.Size;
        g.FillEllipse(b, sx - r, sy - r, r * 2, r * 2);
    }

    private static void DrawGrid(Graphics g, float camX, float camY)
    {
        using var pen = new Pen(Color.FromArgb(20, 40, 40, 40), 1f);
        int gridSize = 100;
        int startX = ((int)camX / gridSize) * gridSize;
        int startY = ((int)camY / gridSize) * gridSize;
        for (int x = startX; x < camX + 800; x += gridSize)
            g.DrawLine(pen, x - camX, 0, x - camX, 600);
        for (int y = startY; y < camY + 600; y += gridSize)
            g.DrawLine(pen, 0, y - camY, 800, y - camY);
    }

    private static void DrawWorldBounds(Graphics g, float camX, float camY)
    {
        using var pen = new Pen(Color.FromArgb(80, 255, 50, 50), 2f);
        float bx = 0 - camX, by = 0 - camY;
        g.DrawRectangle(pen, bx, by, 2000, 2000);
    }

    private static void DrawXpOrb(Graphics g, XpOrb orb, float camX, float camY)
    {
        float sx = orb.X - camX, sy = orb.Y - camY;
        if (sx < -10 || sx > 810 || sy < -10 || sy > 610) return;
        DrawGlow(g, Color.Cyan, sx, sy, 5f, 2);
        using var b = new SolidBrush(Color.Cyan);
        g.FillEllipse(b, sx - 3, sy - 3, 6, 6);
    }

    private static void DrawProjectile(Graphics g, Projectile p, float camX, float camY)
    {
        float sx = p.X - camX, sy = p.Y - camY;
        if (sx < -30 || sx > 830 || sy < -30 || sy > 630) return;
        if (p.IsExplosionEffect)
        {
            float alpha = (float)(p.ExplosionTimer / 0.35f) * 200f;
            using var b = new SolidBrush(Color.FromArgb((int)Math.Clamp(alpha, 0, 200), p.Color));
            float r = p.Size;
            g.FillEllipse(b, sx - r, sy - r, r * 2, r * 2);
            return;
        }
        float sr = p.Size;
        DrawGlow(g, p.Color, sx, sy, sr, 2);
        using var pb = new SolidBrush(p.Color);
        g.FillEllipse(pb, sx - sr, sy - sr, sr * 2, sr * 2);
    }

    private static readonly System.Collections.Generic.Dictionary<string, Image> _imageCache = new();
    private static Image? TryGetImage(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (_imageCache.TryGetValue(name, out var img)) return img;
        try
        {
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", name);
            if (!System.IO.File.Exists(path)) return null;
            img = Image.FromFile(path);
            _imageCache[name] = img;
            return img;
        }
        catch { return null; }
    }

    private static void DrawEnemy(Graphics g, Enemy e, float camX, float camY, GameState gs)
    {
        float sx = e.X - camX, sy = e.Y - camY;
        if (sx < -50 || sx > 850 || sy < -50 || sy > 650) return;
        float r = e.Size;
        Color c = e.EnemyColor;
        if (e is Ghost ghost && ghost.IsPhased) c = Color.FromArgb(60, c);
        // Draw glow removed; keep solid color
        using var b = new SolidBrush(c);
        // If sprites are enabled and the enemy has a sprite, draw it
        if (gs.UseSprites && !string.IsNullOrEmpty(e.SpriteName))
        {
            var img = TryGetImage(e.SpriteName!);
            if (img != null)
            {
                float w = r * 2f * (img.Width / (float)img.Height);
                float h = r * 2f;
                g.DrawImage(img, sx - w / 2f, sy - h / 2f, w, h);
                // outline for elite
                if (e.IsElite) { using var pen = new Pen(Color.White, 2f); g.DrawEllipse(pen, sx - r, sy - r, r * 2, r * 2); }
                // HP bar
                if (e.HP < e.MaxHP)
                {
                    float bw = r * 2 + 4, bh = 3f;
                    float bx = sx - r - 2, by = sy - r - 7;
                    using var bgBrush = new SolidBrush(Color.FromArgb(150, 50, 0, 0));
                    g.FillRectangle(bgBrush, bx, by, bw, bh);
                    using var hpBrush = new SolidBrush(Color.Red);
                    g.FillRectangle(hpBrush, bx, by, bw * (e.HP / e.MaxHP), bh);
                }
                return;
            }
        }

        switch (e.Shape)
        {
            case EnemyShape.Circle:
                g.FillEllipse(b, sx - r, sy - r, r * 2, r * 2);
                if (e.IsElite) { using var pen = new Pen(Color.White, 2f); g.DrawEllipse(pen, sx - r, sy - r, r * 2, r * 2); }
                break;
            case EnemyShape.Square:
                g.FillRectangle(b, sx - r, sy - r, r * 2, r * 2);
                if (e.IsElite) { using var pen = new Pen(Color.White, 2f); g.DrawRectangle(pen, sx - r, sy - r, r * 2, r * 2); }
                break;
            case EnemyShape.Triangle:
                {
                    float angle = MathF.Atan2(gs.Player.Y - e.Y, gs.Player.X - e.X);
                    float tipX = sx + MathF.Cos(angle) * r * 1.8f;
                    float tipY = sy + MathF.Sin(angle) * r * 1.8f;
                    float baseCenterX = sx - MathF.Cos(angle) * r * 0.6f;
                    float baseCenterY = sy - MathF.Sin(angle) * r * 0.6f;
                    float perpX = -MathF.Sin(angle), perpY = MathF.Cos(angle);
                    float leftX = baseCenterX + perpX * r;
                    float leftY = baseCenterY + perpY * r;
                    float rightX = baseCenterX - perpX * r;
                    float rightY = baseCenterY - perpY * r;
                    var pts = new PointF[] { new PointF(tipX, tipY), new PointF(leftX, leftY), new PointF(rightX, rightY) };
                    g.FillPolygon(b, pts);
                    if (e.IsElite) { using var pen = new Pen(Color.White, 2f); g.DrawPolygon(pen, pts); }
                }
                break;
            case EnemyShape.Diamond:
                {
                    var pts = new PointF[] {
                        new PointF(sx, sy - r),
                        new PointF(sx + r, sy),
                        new PointF(sx, sy + r),
                        new PointF(sx - r, sy)
                    };
                    g.FillPolygon(b, pts);
                    if (e.IsElite) { using var pen = new Pen(Color.White, 2f); g.DrawPolygon(pen, pts); }
                }
                break;
            case EnemyShape.Capsule:
                {
                    // capsule: rectangle with semicircle ends
                    float w = r * 2f;
                    float h = r * 1.2f;
                    // draw center rectangle
                    g.FillRectangle(b, sx - w / 2f, sy - h / 2f, w, h);
                    // draw end caps
                    g.FillEllipse(b, sx - w / 2f - h / 2f, sy - h / 2f, h, h);
                    g.FillEllipse(b, sx + w / 2f - h / 2f, sy - h / 2f, h, h);
                    if (e.IsElite) { using var pen = new Pen(Color.White, 2f); g.DrawRectangle(pen, sx - w / 2f, sy - h / 2f, w, h); }
                }
                break;
            case EnemyShape.Pentagon:
                {
                    var pts = new PointF[5];
                    for (int i = 0; i < 5; i++)
                    {
                        float a = (float)(i * MathF.PI * 2 / 5 - MathF.PI / 2);
                        pts[i] = new PointF(sx + MathF.Cos(a) * r, sy + MathF.Sin(a) * r);
                    }
                    g.FillPolygon(b, pts);
                    if (e.IsElite) { using var pen = new Pen(Color.White, 2f); g.DrawPolygon(pen, pts); }
                }
                break;
            case EnemyShape.Hexagon:
                {
                    var pts = new PointF[6];
                    for (int i = 0; i < 6; i++)
                    {
                        float a = (float)(i * MathF.PI * 2 / 6 - MathF.PI / 6);
                        pts[i] = new PointF(sx + MathF.Cos(a) * r, sy + MathF.Sin(a) * r);
                    }
                    g.FillPolygon(b, pts);
                    if (e.IsElite) { using var pen = new Pen(Color.White, 2f); g.DrawPolygon(pen, pts); }
                }
                break;
            case EnemyShape.Star:
                {
                    int spikes = 5;
                    var pts = new List<PointF>();
                    float outer = r * 1.6f, inner = r * 0.7f;
                    for (int i = 0; i < spikes * 2; i++)
                    {
                        float a = i * MathF.PI / spikes - MathF.PI / 2;
                        float rad = (i % 2 == 0) ? outer : inner;
                        pts.Add(new PointF(sx + MathF.Cos(a) * rad, sy + MathF.Sin(a) * rad));
                    }
                    g.FillPolygon(b, pts.ToArray());
                    if (e.IsElite) { using var pen = new Pen(Color.White, 2f); g.DrawPolygon(pen, pts.ToArray()); }
                }
                break;
            default:
                g.FillEllipse(b, sx - r, sy - r, r * 2, r * 2);
                if (e.IsElite) { using var pen = new Pen(Color.White, 2f); g.DrawEllipse(pen, sx - r, sy - r, r * 2, r * 2); }
                break;
        }
        if (e.HP < e.MaxHP)
        {
            float bw = r * 2 + 4, bh = 3f;
            float bx = sx - r - 2, by = sy - r - 7;
            using var bgBrush = new SolidBrush(Color.FromArgb(150, 50, 0, 0));
            g.FillRectangle(bgBrush, bx, by, bw, bh);
            using var hpBrush = new SolidBrush(Color.Red);
            g.FillRectangle(hpBrush, bx, by, bw * (e.HP / e.MaxHP), bh);
        }
    }

    private static void DrawPlayer(Graphics g, Player p, float camX, float camY)
    {
        float sx = p.X - camX, sy = p.Y - camY;
        DrawGlow(g, Color.Cyan, sx, sy, 14f, 4);
        bool invul = p.InvulTimer > 0;
        using var b = new SolidBrush(invul ? Color.FromArgb(180, 255, 255, 255) : Color.White);
        g.FillEllipse(b, sx - 10, sy - 10, 20, 20);
        using var pen = new Pen(Color.Cyan, 1.5f);
        g.DrawEllipse(pen, sx - 10, sy - 10, 20, 20);
        using var xpPen = new Pen(Color.FromArgb(30, 0, 255, 200), 1f);
        g.DrawEllipse(xpPen, sx - p.XPPickupRadius, sy - p.XPPickupRadius, p.XPPickupRadius * 2, p.XPPickupRadius * 2);
    }

    private static void DrawHUD(Graphics g, GameState gs)
    {
        var p = gs.Player;
        float hpFrac = Math.Clamp(p.HP / p.MaxHP, 0, 1);
        using (var bg = new SolidBrush(Color.FromArgb(150, 60, 0, 0))) g.FillRectangle(bg, 10, 10, 200, 16);
        using (var hp = new SolidBrush(Color.FromArgb(220, 200, 0, 0))) g.FillRectangle(hp, 10, 10, 200 * hpFrac, 16);
        using (var pen = new Pen(Color.FromArgb(200, 255, 80, 80), 1f)) g.DrawRectangle(pen, 10, 10, 200, 16);
        g.DrawString($"HP {(int)p.HP}/{(int)p.MaxHP}", _fontSmall, Brushes.White, 12, 11);

        float xpFrac = Math.Clamp(p.XP / p.XPToNextLevel, 0, 1);
        using (var bg = new SolidBrush(Color.FromArgb(150, 0, 0, 60))) g.FillRectangle(bg, 10, 30, 200, 12);
        using (var xpB = new SolidBrush(Color.FromArgb(220, 0, 150, 255))) g.FillRectangle(xpB, 10, 30, 200 * xpFrac, 12);
        using (var pen = new Pen(Color.FromArgb(200, 0, 180, 255), 1f)) g.DrawRectangle(pen, 10, 30, 200, 12);

        int mins = (int)(gs.GameTime / 60), secs = (int)(gs.GameTime % 60);
        g.DrawString($"LVL {p.Level}  XP {(int)p.XP}/{(int)p.XPToNextLevel}", _fontSmall, Brushes.Cyan, 10, 46);
        g.DrawString($"SCORE: {gs.Score}", _fontSmall, Brushes.Yellow, 10, 62);
        g.DrawString($"TIME: {mins:00}:{secs:00}", _fontSmall, Brushes.LightGray, 10, 78);

        for (int i = 0; i < gs.Weapons.Count; i++)
        {
            var w = gs.Weapons[i];
            g.DrawString($"[{w.WeaponName} L{w.Level}]", _fontSmall, Brushes.LightGreen, 10, 100 + i * 16);
        }

        g.DrawString($"Enemies: {gs.Enemies.Count}", _fontSmall, Brushes.Orange, 690, 10);
    }

    private const int CARD_W = 200, CARD_H = 140, CARD_GAP = 20;

    private static void DrawUpgradeCards(Graphics g, GameState gs)
    {
        using (var dim = new SolidBrush(Color.FromArgb(160, 0, 0, 0))) g.FillRectangle(dim, 0, 0, 800, 600);
        g.DrawString("LEVEL UP! Choose an upgrade:", _fontMed, Brushes.Yellow, 220, 180);

        int totalW = 3 * CARD_W + 2 * CARD_GAP;
        int sx = (800 - totalW) / 2;
        int sy = (600 - CARD_H) / 2;

        for (int i = 0; i < Math.Min(3, gs.UpgradeCards.Count); i++)
        {
            var card = gs.UpgradeCards[i];
            int cx = sx + i * (CARD_W + CARD_GAP);
            using (var bg = new SolidBrush(Color.FromArgb(200, 30, 30, 50))) g.FillRectangle(bg, cx, sy, CARD_W, CARD_H);
            using (var border = new Pen(Color.FromArgb(200, 100, 100, 200), 2f)) g.DrawRectangle(border, cx, sy, CARD_W, CARD_H);
            g.DrawString(card.Title, _fontCard, Brushes.Cyan, cx + 8, sy + 10);
            DrawWrappedText(g, card.Description, _fontCardDesc, Brushes.LightGray, cx + 8, sy + 35, CARD_W - 16);
            g.DrawString("[Click to select]", _fontSmall, Brushes.DarkGray, cx + 8, sy + CARD_H - 20);
        }
    }

    private static void DrawWrappedText(Graphics g, string text, Font font, Brush brush, float x, float y, float maxWidth)
    {
        var words = text.Split(' ');
        string line = "";
        float lineH = font.GetHeight(g);
        float cy = y;
        foreach (var w in words)
        {
            string test = line.Length > 0 ? line + " " + w : w;
            if (g.MeasureString(test, font).Width > maxWidth && line.Length > 0)
            {
                g.DrawString(line, font, brush, x, cy);
                cy += lineH;
                line = w;
            }
            else line = test;
        }
        if (line.Length > 0) g.DrawString(line, font, brush, x, cy);
    }

    private static void DrawGameOver(Graphics g, GameState gs)
    {
        using (var dim = new SolidBrush(Color.FromArgb(180, 0, 0, 0))) g.FillRectangle(dim, 0, 0, 800, 600);
        DrawGlowText(g, "GAME OVER", _fontLarge, Color.Red, 400, 220);
        g.DrawString($"Score: {gs.Score}", _fontMed, Brushes.Yellow, 330, 290);
        int mins = (int)(gs.GameTime / 60), secs = (int)(gs.GameTime % 60);
        g.DrawString($"Survived: {mins:00}:{secs:00}", _fontMed, Brushes.LightGray, 300, 320);
        g.DrawString($"Level: {gs.Player.Level}", _fontMed, Brushes.Cyan, 345, 350);
        g.DrawString("Press R to Restart", _fontMed, Brushes.White, 285, 400);
    }

    private static void DrawVictory(Graphics g, GameState gs)
    {
        using (var dim = new SolidBrush(Color.FromArgb(180, 0, 0, 0))) g.FillRectangle(dim, 0, 0, 800, 600);
        DrawGlowText(g, "VICTORY!", _fontLarge, Color.Gold, 400, 220);
        g.DrawString($"Score: {gs.Score}", _fontMed, Brushes.Yellow, 330, 290);
        g.DrawString($"Level: {gs.Player.Level}", _fontMed, Brushes.Cyan, 345, 320);
        g.DrawString("Press R to Play Again", _fontMed, Brushes.White, 270, 400);
    }

    private static void DrawGlowText(Graphics g, string text, Font font, Color color, float cx, float cy)
    {
        // Draw plain centered text without glow/shadow
        var size = g.MeasureString(text, font);
        float x = cx - size.Width / 2, y = cy - size.Height / 2;
        using var main = new SolidBrush(color);
        g.DrawString(text, font, main, x, y);
    }

    private static void DrawGlow(Graphics g, Color color, float cx, float cy, float radius, int steps)
    {
        // Glow disabled: no-op to remove shadows/halo effects
        return;
    }
}
