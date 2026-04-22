using Godot;

public partial class RayleighAtmosphereController : Node3D
{
    [Export] public NodePath PlanetPath = default!;
    [Export] public NodePath SunLightPath = default!;

    [ExportGroup("Atmosphere Shape")]
    [Export] public float AtmosphereHeight = 34.0f;
    [Export] public int RadialSegments = 96;
    [Export] public int Rings = 48;

    [ExportGroup("Rayleigh")]
    [Export] public float RayleighStrength = 1.9f;
    [Export] public float DensityFalloff = 4.6f;
    [Export] public float AtmosphereAlpha = 0.65f;
    [Export] public float PressureScaleHeight = 0.28f;
    [Export] public float TwilightRedStrength = 1.2f;
    [Export] public float HorizonPower = 2.0f;

    [ExportGroup("Composition (0-1, auto-normalized)")]
    [Export] public bool RandomizeCompositionOnReady = false;
    [Export] public bool UsePlanetSeedForRandomComposition = true;
    [Export] public int RandomCompositionSeed = 0;
    [Export] public float Nitrogen = 0.78f;
    [Export] public float Oxygen = 0.21f;
    [Export] public float Argon = 0.01f;
    [Export] public float CarbonDioxide = 0.0004f;
    [Export] public float Methane = 0.0f;
    [Export] public float Hydrogen = 0.0f;
    [Export] public float Helium = 0.0f;

    [ExportGroup("Tuning")]
    [Export] public float ScatterScale = 1.0f;

    private PlanetBody _planet;
    private DirectionalLight3D _sun;
    private MeshInstance3D _shell;
    private ShaderMaterial _material;

    public string GetCompositionSummary()
    {
        GetNormalizedComposition(
            out float n2,
            out float o2,
            out float ar,
            out float co2,
            out float ch4,
            out float h2,
            out float he);

        return "Atmosphere Composition\n"
            + $"N2  {n2 * 100.0f:0.0}%\n"
            + $"O2  {o2 * 100.0f:0.0}%\n"
            + $"Ar  {ar * 100.0f:0.0}%\n"
            + $"CO2 {co2 * 100.0f:0.00}%\n"
            + $"CH4 {ch4 * 100.0f:0.00}%\n"
            + $"H2  {h2 * 100.0f:0.0}%\n"
            + $"He  {he * 100.0f:0.0}%";
    }

    public override void _Ready()
    {
        ResolveNodes();
        if (RandomizeCompositionOnReady)
        {
            RandomizeComposition();
        }
        EnsureShell();
        UpdateAtmosphereGeometry();
        UpdateAtmosphereMaterial();
    }

    public override void _Process(double delta)
    {
        if (_planet == null || _material == null)
        {
            ResolveNodes();
            if (_planet == null || _material == null)
            {
                return;
            }
        }

        UpdateAtmosphereGeometry();
        UpdateAtmosphereMaterial();
    }

    private void ResolveNodes()
    {
        if (_planet == null)
        {
            if (PlanetPath != null && !PlanetPath.IsEmpty)
            {
                _planet = GetNodeOrNull<PlanetBody>(PlanetPath);
            }
            else if (GetParent() is PlanetBody parentPlanet)
            {
                _planet = parentPlanet;
            }
        }

        if (_sun == null && SunLightPath != null && !SunLightPath.IsEmpty)
        {
            _sun = GetNodeOrNull<DirectionalLight3D>(SunLightPath);
        }
    }

    private void EnsureShell()
    {
        if (_shell != null)
        {
            return;
        }

        _shell = new MeshInstance3D { Name = "AtmosphereShell" };
        AddChild(_shell);

        Shader shader = GD.Load<Shader>("res://Shaders/rayleigh_atmosphere.gdshader");
        _material = new ShaderMaterial
        {
            Shader = shader,
        };

        _shell.MaterialOverride = _material;
    }

    private void UpdateAtmosphereGeometry()
    {
        if (_planet == null || _shell == null)
        {
            return;
        }

        float shellRadius = _planet.Radius + Mathf.Max(1.0f, AtmosphereHeight);
        SphereMesh sphere = new SphereMesh
        {
            Radius = shellRadius,
            Height = shellRadius * 2.0f,
            RadialSegments = Mathf.Max(24, RadialSegments),
            Rings = Mathf.Max(12, Rings),
        };

        _shell.Mesh = sphere;
        _shell.GlobalPosition = _planet.GlobalPosition;
    }

    private void UpdateAtmosphereMaterial()
    {
        if (_planet == null || _material == null)
        {
            return;
        }

        Vector3 sunDirection = _sun != null ? -_sun.GlobalTransform.Basis.Z.Normalized() : Vector3.Down;

        _material.SetShaderParameter("planet_center", _planet.GlobalPosition);
        _material.SetShaderParameter("planet_radius", _planet.Radius);
        _material.SetShaderParameter("atmosphere_radius", _planet.Radius + Mathf.Max(1.0f, AtmosphereHeight));
        _material.SetShaderParameter("inv_wavelength4", BuildCompositionWavelength());
        _material.SetShaderParameter("sun_direction", sunDirection);
        _material.SetShaderParameter("rayleigh_strength", RayleighStrength);
        _material.SetShaderParameter("density_falloff", DensityFalloff);
        _material.SetShaderParameter("atmosphere_alpha", AtmosphereAlpha);
        _material.SetShaderParameter("pressure_scale_height", PressureScaleHeight);
        _material.SetShaderParameter("twilight_red_strength", TwilightRedStrength);
        _material.SetShaderParameter("horizon_power", HorizonPower);
    }

    private Vector3 BuildCompositionWavelength()
    {
        GetNormalizedComposition(
            out float n2,
            out float o2,
            out float ar,
            out float co2,
            out float ch4,
            out float h2,
            out float he);

        Vector3 wavelength =
            n2 * new Vector3(650.0f, 570.0f, 475.0f) +
            o2 * new Vector3(640.0f, 560.0f, 470.0f) +
            ar * new Vector3(660.0f, 580.0f, 490.0f) +
            co2 * new Vector3(680.0f, 590.0f, 505.0f) +
            ch4 * new Vector3(710.0f, 610.0f, 500.0f) +
            h2 * new Vector3(620.0f, 540.0f, 430.0f) +
            he * new Vector3(610.0f, 535.0f, 425.0f);

        wavelength.X = Mathf.Max(380.0f, wavelength.X);
        wavelength.Y = Mathf.Max(380.0f, wavelength.Y);
        wavelength.Z = Mathf.Max(380.0f, wavelength.Z);

        Vector3 inv4 = new Vector3(
            Mathf.Pow(400.0f / wavelength.X, 4.0f),
            Mathf.Pow(400.0f / wavelength.Y, 4.0f),
            Mathf.Pow(400.0f / wavelength.Z, 4.0f));

        float maxC = Mathf.Max(0.0001f, Mathf.Max(inv4.X, Mathf.Max(inv4.Y, inv4.Z)));
        inv4 /= maxC;
        return inv4 * Mathf.Max(0.0f, ScatterScale);
    }

    private void GetNormalizedComposition(
        out float n2,
        out float o2,
        out float ar,
        out float co2,
        out float ch4,
        out float h2,
        out float he)
    {
        n2 = Mathf.Max(0.0f, Nitrogen);
        o2 = Mathf.Max(0.0f, Oxygen);
        ar = Mathf.Max(0.0f, Argon);
        co2 = Mathf.Max(0.0f, CarbonDioxide);
        ch4 = Mathf.Max(0.0f, Methane);
        h2 = Mathf.Max(0.0f, Hydrogen);
        he = Mathf.Max(0.0f, Helium);

        float sum = n2 + o2 + ar + co2 + ch4 + h2 + he;
        if (sum <= 0.000001f)
        {
            n2 = 1.0f;
            o2 = 0.0f;
            ar = 0.0f;
            co2 = 0.0f;
            ch4 = 0.0f;
            h2 = 0.0f;
            he = 0.0f;
            return;
        }

        float inv = 1.0f / sum;
        n2 *= inv;
        o2 *= inv;
        ar *= inv;
        co2 *= inv;
        ch4 *= inv;
        h2 *= inv;
        he *= inv;
    }

    private void RandomizeComposition()
    {
        RandomNumberGenerator rng = new RandomNumberGenerator();
        if (UsePlanetSeedForRandomComposition && _planet != null)
        {
            rng.Seed = (ulong)Mathf.Max(1, _planet.Seed + 5413);
        }
        else if (RandomCompositionSeed > 0)
        {
            rng.Seed = (ulong)RandomCompositionSeed;
        }
        else
        {
            rng.Randomize();
        }

        // Build a biased random mixture using a dominant profile + stochastic minor gases.
        int profile = rng.RandiRange(0, 3);
        float n2Base = 0.2f;
        float o2Base = 0.02f;
        float arBase = 0.005f;
        float co2Base = 0.02f;
        float ch4Base = 0.005f;
        float h2Base = 0.02f;
        float heBase = 0.005f;

        switch (profile)
        {
            case 0: // N2-dominant terrestrial
                n2Base = 0.65f;
                o2Base = 0.18f;
                arBase = 0.02f;
                co2Base = 0.01f;
                break;
            case 1: // CO2-heavy terrestrial
                co2Base = 0.72f;
                n2Base = 0.18f;
                o2Base = 0.02f;
                arBase = 0.03f;
                break;
            case 2: // H2/He giant-like
                h2Base = 0.68f;
                heBase = 0.24f;
                ch4Base = 0.03f;
                n2Base = 0.03f;
                break;
            case 3: // methane-rich cold atmosphere
                n2Base = 0.52f;
                ch4Base = 0.26f;
                h2Base = 0.1f;
                co2Base = 0.05f;
                break;
        }

        // Add random perturbation so planets are not identical within a profile.
        Nitrogen = n2Base * rng.RandfRange(0.75f, 1.25f);
        Oxygen = o2Base * rng.RandfRange(0.6f, 1.4f);
        Argon = arBase * rng.RandfRange(0.5f, 1.8f);
        CarbonDioxide = co2Base * rng.RandfRange(0.6f, 1.5f);
        Methane = ch4Base * rng.RandfRange(0.6f, 1.6f);
        Hydrogen = h2Base * rng.RandfRange(0.7f, 1.4f);
        Helium = heBase * rng.RandfRange(0.7f, 1.4f);

        float sum = Nitrogen + Oxygen + Argon + CarbonDioxide + Methane + Hydrogen + Helium;
        if (sum <= 0.000001f)
        {
            Nitrogen = 1.0f;
            Oxygen = 0.0f;
            Argon = 0.0f;
            CarbonDioxide = 0.0f;
            Methane = 0.0f;
            Hydrogen = 0.0f;
            Helium = 0.0f;
            return;
        }

        float inv = 1.0f / sum;
        Nitrogen *= inv;
        Oxygen *= inv;
        Argon *= inv;
        CarbonDioxide *= inv;
        Methane *= inv;
        Hydrogen *= inv;
        Helium *= inv;
    }
}
