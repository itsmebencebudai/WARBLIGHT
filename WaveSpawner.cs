namespace WARBLIGHT;
public class WaveSpawner
{
    private float _gruntTimer = 0f;
    private float _rushTimer = 0f;
    private float _tankTimer = 0f;
    private float _splitterTimer = 0f;
    private float _ghostTimer = 0f;
    private float _shooterTimer = 0f;
    private float _swarmerTimer = 0f;
    private float _leechTimer = 0f;
    private float _bomberTimer = 0f;
    private bool _hollowKingSpawned = false;
    private bool _plagueMotherSpawned = false;
    private static readonly Random _rng = new();

    public void Update(GameState gs, float dt)
    {
        float t = gs.GameTime;

        float gruntInterval = Math.Max(0.8f, 2f - t / 120f);
        _gruntTimer += dt;
        if (_gruntTimer >= gruntInterval)
        {
            _gruntTimer = 0;
            int count = 3 + (int)(t / 60f);
            for (int i = 0; i < count; i++) gs.SpawnEnemy(SpawnAtEdge(new Grunt(), gs));
        }

        if (t >= 30f) { _rushTimer += dt; if (_rushTimer >= Math.Max(2f, 5f - t / 120f)) { _rushTimer = 0; gs.SpawnEnemy(SpawnAtEdge(new Rusher(), gs)); } }

        if (t >= 120f) { _tankTimer += dt; if (_tankTimer >= 8f) { _tankTimer = 0; gs.SpawnEnemy(SpawnAtEdge(new Tank(), gs)); } }

        if (t >= 120f && !_hollowKingSpawned) { _hollowKingSpawned = true; gs.SpawnEnemy(new HollowKing(gs.Player.X, gs.Player.Y)); }

        if (t >= 180f) { _splitterTimer += dt; if (_splitterTimer >= 4f) { _splitterTimer = 0; gs.SpawnEnemy(SpawnAtEdge(new Splitter(), gs)); } }

        if (t >= 240f) { _ghostTimer += dt; if (_ghostTimer >= 5f) { _ghostTimer = 0; gs.SpawnEnemy(SpawnAtEdge(new Ghost(), gs)); } }

        if (t >= 240f) { _shooterTimer += dt; if (_shooterTimer >= 6f) { _shooterTimer = 0; gs.SpawnEnemy(SpawnAtEdge(new Shooter(), gs)); } }

        if (t >= 240f && !_plagueMotherSpawned) { _plagueMotherSpawned = true; gs.SpawnEnemy(new PlagueMother(gs.Player.X, gs.Player.Y)); }

        if (t >= 300f) { _swarmerTimer += dt; if (_swarmerTimer >= 3f) { _swarmerTimer = 0; for (int i = 0; i < 8; i++) gs.SpawnEnemy(SpawnAtEdge(new Swarmer(), gs)); } }

        if (t >= 300f) { _leechTimer += dt; if (_leechTimer >= 7f) { _leechTimer = 0; gs.SpawnEnemy(SpawnAtEdge(new Leech(), gs)); } }

        if (t >= 360f) { _bomberTimer += dt; if (_bomberTimer >= 5f) { _bomberTimer = 0; gs.SpawnEnemy(SpawnAtEdge(new Bomber(), gs)); } }
    }

    private Enemy SpawnAtEdge(Enemy e, GameState gs)
    {
        float camX = gs.Player.X - 400, camY = gs.Player.Y - 300;
        float margin = 60f;
        int edge = _rng.Next(4);
        float x, y;
        switch (edge)
        {
            case 0: x = camX + _rng.NextSingle() * 800; y = camY - margin; break;
            case 1: x = camX + 800 + margin; y = camY + _rng.NextSingle() * 600; break;
            case 2: x = camX + _rng.NextSingle() * 800; y = camY + 600 + margin; break;
            default: x = camX - margin; y = camY + _rng.NextSingle() * 600; break;
        }
        e.X = Math.Clamp(x, 50f, 1950f);
        e.Y = Math.Clamp(y, 50f, 1950f);
        if (gs.GameTime >= 300f && _rng.NextDouble() < 0.15) e.MakeElite();
        return e;
    }
}
