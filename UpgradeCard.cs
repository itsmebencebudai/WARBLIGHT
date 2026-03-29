namespace WARBLIGHT;
public class UpgradeCard
{
    public string Title { get; }
    public string Description { get; }
    private readonly Action<GameState> _apply;
    public UpgradeCard(string title, string description, Action<GameState> apply)
    {
        Title = title; Description = description; _apply = apply;
    }
    public void Apply(GameState gs) => _apply(gs);
}
