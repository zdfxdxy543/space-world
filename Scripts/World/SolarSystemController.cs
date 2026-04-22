using Godot;

public partial class SolarSystemController : Node3D
{
    [Export] public NodePath PlanetPath = default!;
    [Export] public NodePath PlayerPath = default!;

    private PlanetBody _planet;
    private SpaceExplorerController _player;

    public override void _Ready()
    {
        if (PlanetPath != null && !PlanetPath.IsEmpty)
        {
            _planet = GetNodeOrNull<PlanetBody>(PlanetPath);
        }

        if (PlayerPath != null && !PlayerPath.IsEmpty)
        {
            _player = GetNodeOrNull<SpaceExplorerController>(PlayerPath);
        }

        if (_player != null && _planet != null)
        {
            Vector3 spawn = _planet.GlobalPosition + Vector3.Up * (_planet.Radius + _planet.MaxHeight + 55.0f);
            _player.GlobalPosition = spawn;
            _player.Velocity = Vector3.Zero;

            Vector3 up = (spawn - _planet.GlobalPosition).Normalized();
            _player.UpDirection = up;
            _player.LookAt(_planet.GlobalPosition, up);
        }
    }
}
