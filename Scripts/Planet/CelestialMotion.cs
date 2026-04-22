using Godot;

public partial class CelestialMotion : Node3D
{
    [Export] public bool ControlParent = true;
    [Export] public float SelfRotationDegreesPerSecond = 3.0f;
    [Export] public NodePath OrbitCenterPath = default!;
    [Export] public float OrbitRadius = 0.0f;
    [Export] public float OrbitDegreesPerSecond = 0.0f;
    [Export] public Vector3 OrbitAxis = Vector3.Up;

    private Node3D _orbitCenter;
    private Node3D _target;
    private float _orbitAngle;

    public override void _Ready()
    {
        _target = this;
        if (ControlParent && GetParent() is Node3D parentNode)
        {
            _target = parentNode;
        }

        if (OrbitCenterPath != null && !OrbitCenterPath.IsEmpty)
        {
            _orbitCenter = GetNodeOrNull<Node3D>(OrbitCenterPath);
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        if (SelfRotationDegreesPerSecond != 0.0f)
        {
            _target.RotateObjectLocal(Vector3.Up, Mathf.DegToRad(SelfRotationDegreesPerSecond * dt));
        }

        if (_orbitCenter == null || OrbitRadius <= 0.0f || OrbitDegreesPerSecond == 0.0f)
        {
            return;
        }

        _orbitAngle += Mathf.DegToRad(OrbitDegreesPerSecond * dt);
        Quaternion q = new Quaternion(OrbitAxis.Normalized(), _orbitAngle);
        Vector3 offset = q * Vector3.Forward * OrbitRadius;
        _target.GlobalPosition = _orbitCenter.GlobalPosition + offset;
    }
}
