using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace WARBLIGHT;

public class GameForm : Form
{
    private GameState _gs;
    private System.Windows.Forms.Timer _timer;
    private readonly HashSet<Keys> _keys = new();
    private DateTime _lastTick = DateTime.UtcNow;
    private const int CARD_W = 200, CARD_H = 140, CARD_GAP = 20;

    public GameForm()
    {
        Text = "WARBLIGHT";
        ClientSize = new Size(800, 600);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.Black;
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        _gs = new GameState();
        _timer = new System.Windows.Forms.Timer { Interval = 16 };
        _timer.Tick += OnTick;
        _timer.Start();
        KeyDown += OnKeyDown;
        KeyUp += (s, e) => _keys.Remove(e.KeyCode);
        MouseClick += OnMouseClick;
    }

    protected override void OnFormClosing(FormClosingEventArgs e) { _timer.Stop(); base.OnFormClosing(e); }

    private void OnTick(object? sender, EventArgs e)
    {
        var now = DateTime.UtcNow;
        float dt = (float)(now - _lastTick).TotalSeconds;
        _lastTick = now;
        dt = Math.Clamp(dt, 0f, 0.05f);
        _gs.KeysPressed = _keys;
        _gs.Update(dt);
        Invalidate();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        _keys.Add(e.KeyCode);
        if (e.KeyCode == Keys.R && (_gs.IsGameOver || _gs.IsVictory))
        {
            _gs = new GameState();
            _lastTick = DateTime.UtcNow;
        }
    }

    private void OnMouseClick(object? sender, MouseEventArgs e)
    {
        if (!_gs.IsUpgradePaused) return;
        int totalW = 3 * CARD_W + 2 * CARD_GAP;
        int sx = (800 - totalW) / 2;
        int sy = (600 - CARD_H) / 2;
        for (int i = 0; i < Math.Min(3, _gs.UpgradeCards.Count); i++)
        {
            int cx = sx + i * (CARD_W + CARD_GAP);
            if (e.X >= cx && e.X <= cx + CARD_W && e.Y >= sy && e.Y <= sy + CARD_H)
            {
                _gs.SelectUpgradeCard(i);
                break;
            }
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Renderer.Draw(_gs, e.Graphics);
    }
}
