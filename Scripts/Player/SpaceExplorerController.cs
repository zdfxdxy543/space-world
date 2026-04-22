using Godot;

public partial class SpaceExplorerController : CharacterBody3D
{
    public enum MovementMode
    {
        GroundPlane,
        SpaceFlight,
    }

    [Export] public NodePath PlanetPath = default!;
    [Export] public MovementMode MoveMode = MovementMode.SpaceFlight;
    [Export] public float MoveSpeed = 28.0f;
    [Export] public float BoostMultiplier = 2.0f;
    [Export] public float FlightAcceleration = 8.0f;
    [Export] public float FlightDamping = 5.0f;
    [Export] public float MouseSensitivity = 0.0022f;
    [Export] public NodePath HeadlightPath = default!;
    [Export] public Key ToggleHeadlightKey = Key.F;
    [Export] public bool StartHeadlightEnabled = false;

    private PlanetBody _planet;
    private Camera3D _camera;
    private OmniLight3D _headlight;
    private float _pitch;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _camera = GetNodeOrNull<Camera3D>("Camera3D");
        _headlight = (HeadlightPath != null && !HeadlightPath.IsEmpty)
            ? GetNodeOrNull<OmniLight3D>(HeadlightPath)
            : GetNodeOrNull<OmniLight3D>("PlayerFillLight");
        if (_headlight != null)
        {
            _headlight.Visible = StartHeadlightEnabled;
        }

        if (PlanetPath != null && !PlanetPath.IsEmpty)
        {
            _planet = GetNodeOrNull<PlanetBody>(PlanetPath);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion)
        {
            RotateY(-mouseMotion.Relative.X * MouseSensitivity);
            _pitch = Mathf.Clamp(_pitch - mouseMotion.Relative.Y * MouseSensitivity, -1.3f, 1.3f);
            if (_camera != null)
            {
                _camera.Rotation = new Vector3(_pitch, 0.0f, 0.0f);
            }
        }

        if (@event.IsActionPressed("ui_cancel"))
        {
            Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
                ? Input.MouseModeEnum.Visible
                : Input.MouseModeEnum.Captured;
        }

        if (@event is InputEventKey keyEvent
            && keyEvent.Pressed
            && !keyEvent.Echo
            && keyEvent.Keycode == ToggleHeadlightKey)
        {
            ToggleHeadlight();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        Vector2 input = GetMoveInput();
        Basis basis = _camera != null ? _camera.GlobalTransform.Basis : GlobalTransform.Basis;

        if (MoveMode == MovementMode.SpaceFlight)
        {
            ApplySpaceFlight(input, basis, dt);
            MoveAndSlide();
            return;
        }

        ApplyGroundPlane(input, basis);
        MoveAndSlide();
    }

    private void ApplyGroundPlane(Vector2 input, Basis basis)
    {
        Vector3 forward = FlattenXZ(-basis.Z).Normalized();
        Vector3 right = FlattenXZ(basis.X).Normalized();

        if (forward.LengthSquared() < 0.0001f || right.LengthSquared() < 0.0001f)
        {
            forward = FlattenXZ(-GlobalTransform.Basis.Z).Normalized();
            right = FlattenXZ(GlobalTransform.Basis.X).Normalized();
        }

        Vector3 desired = right * input.X + forward * input.Y;
        desired = desired.Normalized();
        Velocity = desired * MoveSpeed;
    }

    private void ApplySpaceFlight(Vector2 input, Basis basis, float dt)
    {
        Vector3 forward = -basis.Z;
        Vector3 right = basis.X;
        Vector3 up = basis.Y;

        Vector3 direction = right * input.X + forward * input.Y;
        if (Input.IsKeyPressed(Key.Space))
        {
            direction += up;
        }
        if (Input.IsKeyPressed(Key.Ctrl))
        {
            direction -= up;
        }

        if (direction.LengthSquared() > 1.0f)
        {
            direction = direction.Normalized();
        }

        float targetSpeed = MoveSpeed;
        if (Input.IsKeyPressed(Key.Shift))
        {
            targetSpeed *= BoostMultiplier;
        }

        Vector3 targetVelocity = direction * targetSpeed;
        float accelT = Mathf.Clamp(dt * FlightAcceleration, 0.0f, 1.0f);
        Velocity = Velocity.Lerp(targetVelocity, accelT);

        if (direction.LengthSquared() < 0.0001f)
        {
            float dampT = Mathf.Clamp(dt * FlightDamping, 0.0f, 1.0f);
            Velocity = Velocity.Lerp(Vector3.Zero, dampT);
        }
    }

    private static Vector2 GetMoveInput()
    {
        Vector2 input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

        if (Input.IsKeyPressed(Key.A))
        {
            input.X -= 1.0f;
        }
        if (Input.IsKeyPressed(Key.D))
        {
            input.X += 1.0f;
        }
        if (Input.IsKeyPressed(Key.W))
        {
            input.Y += 1.0f;
        }
        if (Input.IsKeyPressed(Key.S))
        {
            input.Y -= 1.0f;
        }

        return input.LengthSquared() > 1.0f ? input.Normalized() : input;
    }

    private static Vector3 FlattenXZ(Vector3 value)
    {
        return new Vector3(value.X, 0.0f, value.Z);
    }

    private void ToggleHeadlight()
    {
        if (_headlight == null)
        {
            _headlight = GetNodeOrNull<OmniLight3D>("PlayerFillLight");
            if (_headlight == null)
            {
                return;
            }
        }

        _headlight.Visible = !_headlight.Visible;
    }
}
