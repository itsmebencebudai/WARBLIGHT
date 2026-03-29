using System.Drawing;
namespace WARBLIGHT;

public class Particle
{
    public float X, Y, VX, VY;
    public float Life = 1f;
    public Color Color = Color.White;
    public float Size = 2f;

    public bool IsAlive => Life > 0f;

    public void Update(float dt)
    {
        Life -= dt;
        X += VX * dt;
        Y += VY * dt;
        // simple friction
        VX *= 0.98f;
        VY *= 0.98f;
    }
}
