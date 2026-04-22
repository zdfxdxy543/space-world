using Godot;

public partial class AtmosphereCompositionHud : CanvasLayer
{
    [Export] public NodePath AtmospherePath = default!;
    [Export] public NodePath LabelPath = default!;
    [Export] public float RefreshInterval = 0.4f;

    private RayleighAtmosphereController _atmosphere;
    private Label _label;
    private float _timer;

    public override void _Ready()
    {
        _label = LabelPath != null && !LabelPath.IsEmpty
            ? GetNodeOrNull<Label>(LabelPath)
            : GetNodeOrNull<Label>("CompositionLabel");

        if (AtmospherePath != null && !AtmospherePath.IsEmpty)
        {
            _atmosphere = GetNodeOrNull<RayleighAtmosphereController>(AtmospherePath);
        }

        if (_label != null)
        {
            _label.Text = "Atmosphere Composition\nloading...";
        }

        UpdateLabel();
    }

    public override void _Process(double delta)
    {
        _timer += (float)delta;
        if (_timer < RefreshInterval)
        {
            return;
        }

        _timer = 0.0f;
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        if (_label == null)
        {
            return;
        }

        if (_atmosphere == null && AtmospherePath != null && !AtmospherePath.IsEmpty)
        {
            _atmosphere = GetNodeOrNull<RayleighAtmosphereController>(AtmospherePath);
        }

        if (_atmosphere == null)
        {
            _label.Text = "Atmosphere Composition\n(not found)";
            return;
        }

        _label.Text = _atmosphere.GetCompositionSummary();
    }
}
