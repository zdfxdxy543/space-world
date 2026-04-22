using Godot;

public partial class RayleighAtmosphereController : Node3D
{
    [Export] public NodePath PlanetPath = default!;
    [Export] public NodePath SunLightPath = default!;

    [ExportGroup("Atmosphere Shape")]
    [Export] public float AtmosphereHeight = 52.0f;
    [Export] public int RadialSegments = 96;
    [Export] public int Rings = 48;

    [ExportGroup("Rayleigh")]
    [Export] public float RayleighStrength = 4.6f;
    [Export] public float DensityFalloff = 4.6f;
    [Export] public float AtmosphereAlpha = 0.9f;
    [Export] public float PressureScaleHeight = 0.24f;
    [Export] public float SeaLevelPressure = 1.0f;
    [Export] public float TwilightRedStrength = 1.9f;
    [Export] public float HorizonPower = 1.9f;

    // Bug 修复：将 scatter_exposure 和 ambient_scatter 暴露出来
    [ExportGroup("Tuning")]
    [Export] public float ScatterScale = 1.8f;
    [Export] public float ScatterExposure = 1.0f;
    [Export] public float AmbientScatter = 0.24f;

    [ExportGroup("Pressure")]
    [Export] public bool UseCustomSeaLevelPressure = true;
    [Export] public bool UsePlanetSeedForRandomPressure = true;
    [Export] public int RandomPressureSeed = 0;
    [Export] public Vector2 SeaLevelPressureRange = new Vector2(0.75f, 1.45f);
    [Export] public Vector2 PressureScaleHeightRange = new Vector2(0.14f, 0.3f);
    [Export] public Vector2 DensityFalloffRange = new Vector2(4.2f, 7.0f);
    [Export] public Vector2 RayleighStrengthRange = new Vector2(2.2f, 3.8f);
    [Export] public Vector2 AtmosphereAlphaRange = new Vector2(0.56f, 0.78f);

    [ExportGroup("Composition (0-1, auto-normalized)")]
    [Export] public bool UseCustomComposition = true;
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

    private const float EarthMeanMolecularMass = 28.97f;
    private const float NitrogenMolecularMass = 28.0134f;
    private const float OxygenMolecularMass = 31.998f;
    private const float ArgonMolecularMass = 39.948f;
    private const float CarbonDioxideMolecularMass = 44.0095f;
    private const float MethaneMolecularMass = 16.043f;
    private const float HydrogenMolecularMass = 2.016f;
    private const float HeliumMolecularMass = 4.0026f;

    private static readonly Vector3 N2Spectrum  = new Vector3(1.00f, 1.22f, 1.64f);
    private static readonly Vector3 O2Spectrum  = new Vector3(1.02f, 1.25f, 1.68f);
    private static readonly Vector3 ArSpectrum  = new Vector3(0.94f, 1.10f, 1.38f);
    private static readonly Vector3 CO2Spectrum = new Vector3(1.08f, 1.34f, 1.84f);
    private static readonly Vector3 CH4Spectrum = new Vector3(0.88f, 1.04f, 1.24f);
    private static readonly Vector3 H2Spectrum  = new Vector3(0.24f, 0.34f, 0.56f);
    private static readonly Vector3 HeSpectrum  = new Vector3(0.16f, 0.22f, 0.36f);

    private PlanetBody _planet;
    private DirectionalLight3D _sun;
    private MeshInstance3D _shell;
    private ShaderMaterial _material;

    // Bug 修复：缓存几何参数，避免每帧重建 SphereMesh
    private float _cachedShellRadius = -1f;
    private int   _cachedRadialSegs  = -1;
    private int   _cachedRings       = -1;

    public string GetCompositionSummary()
    {
        GetNormalizedComposition(out float n2, out float o2, out float ar,
                                 out float co2, out float ch4, out float h2, out float he);
        float meanMass = ComputeMeanMolecularMass(n2, o2, ar, co2, ch4, h2, he);
        float scaleMultiplier = GetCompositionScaleHeightMultiplier(meanMass);

        return "Atmosphere Composition\n"
            + $"Sea level pressure {SeaLevelPressure:0.00} atm\n"
            + $"Mean molecular mass {meanMass:0.00} g/mol\n"
            + $"Scale height x{scaleMultiplier:0.00}\n"
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

        if (RandomizeCompositionOnReady && !UseCustomComposition)
            RandomizeComposition();

        if (!UseCustomSeaLevelPressure)
            RandomizeSeaLevelPressure();

        EnsureShell();
        RefreshAtmosphere();
    }

    public override void _Process(double delta)
    {
        ResolveNodes();

        if (_planet == null || _material == null)
            return;

        RefreshAtmosphere();
    }

    private void ResolveNodes()
    {
        if (_planet == null)
        {
            if (PlanetPath != null && !PlanetPath.IsEmpty)
                _planet = GetNodeOrNull<PlanetBody>(PlanetPath);
            else if (GetParent() is PlanetBody parentPlanet)
                _planet = parentPlanet;
        }

        if (_sun == null && SunLightPath != null && !SunLightPath.IsEmpty)
            _sun = GetNodeOrNull<DirectionalLight3D>(SunLightPath);
    }

    private void EnsureShell()
    {
        if (_shell != null)
            return;

        _shell = new MeshInstance3D { Name = "AtmosphereShell" };
        AddChild(_shell);

        Shader shader = GD.Load<Shader>("res://Shaders/rayleigh_atmosphere.gdshader");
        _material = new ShaderMaterial { Shader = shader };
        _shell.MaterialOverride = _material;
    }

    private void RefreshAtmosphere()
    {
        UpdateAtmosphereGeometry();
        UpdateAtmosphereMaterial();
    }

    private void UpdateAtmosphereGeometry()
    {
        if (_planet == null || _shell == null)
            return;

        float shellRadius = _planet.Radius + Mathf.Max(1.0f, AtmosphereHeight);
        int   radSegs     = Mathf.Max(24, RadialSegments);
        int   rings       = Mathf.Max(12, Rings);

        // Bug 修复：只在几何参数实际变化时重建 Mesh，不再每帧 new SphereMesh
        if (!Mathf.IsEqualApprox(shellRadius, _cachedShellRadius)
            || radSegs != _cachedRadialSegs
            || rings   != _cachedRings)
        {
            _shell.Mesh = new SphereMesh
            {
                Radius         = shellRadius,
                Height         = shellRadius * 2.0f,
                RadialSegments = radSegs,
                Rings          = rings,
            };
            _cachedShellRadius = shellRadius;
            _cachedRadialSegs  = radSegs;
            _cachedRings       = rings;
        }

        _shell.GlobalPosition = _planet.GlobalPosition;
    }

    private void UpdateAtmosphereMaterial()
    {
        if (_planet == null || _material == null)
            return;

        GetNormalizedComposition(out float n2, out float o2, out float ar,
                                 out float co2, out float ch4, out float h2, out float he);

        float meanMolecularMass   = ComputeMeanMolecularMass(n2, o2, ar, co2, ch4, h2, he);
        float compositionScaleHeight = GetCompositionScaleHeightMultiplier(meanMolecularMass);
        float atmosphereThickness = Mathf.Max(1.0f, AtmosphereHeight);
        float effectiveScaleHeight = Mathf.Max(0.001f, PressureScaleHeight)
                                   * atmosphereThickness
                                   * compositionScaleHeight;

        Vector3 rayleighCoeff = BuildRayleighCoefficient(n2, o2, ar, co2, ch4, h2, he);
        rayleighCoeff *= Mathf.Max(0.0f, RayleighStrength) * Mathf.Max(0.0f, ScatterScale);

        float visualPressure = Mathf.Log(1.0f + Mathf.Max(0.0f, SeaLevelPressure))
                             / Mathf.Log(2.0f);

        Vector3 sunDirection = _sun != null
            ? -_sun.GlobalTransform.Basis.Z.Normalized()
            : Vector3.Down;

        _material.SetShaderParameter("planet_center",             _planet.GlobalPosition);
        _material.SetShaderParameter("planet_radius",             _planet.Radius);
        _material.SetShaderParameter("atmosphere_radius",         _planet.Radius + atmosphereThickness);
        _material.SetShaderParameter("rayleigh_scattering_coeff", rayleighCoeff);
        _material.SetShaderParameter("sun_direction",             sunDirection);
        _material.SetShaderParameter("sea_level_pressure",        Mathf.Max(0.0f, SeaLevelPressure));
        _material.SetShaderParameter("visual_pressure",           Mathf.Clamp(visualPressure, 0.15f, 3.0f));
        _material.SetShaderParameter("density_scale_height",      effectiveScaleHeight);
        _material.SetShaderParameter("density_falloff",           Mathf.Max(0.0f, DensityFalloff));
        _material.SetShaderParameter("atmosphere_alpha",          Mathf.Clamp(AtmosphereAlpha, 0.0f, 1.0f));
        _material.SetShaderParameter("twilight_red_strength",     Mathf.Max(0.0f, TwilightRedStrength));
        _material.SetShaderParameter("horizon_power",             Mathf.Max(0.01f, HorizonPower));
        // Bug 修复：之前缺失这两个参数的传递
        _material.SetShaderParameter("scatter_exposure",          Mathf.Max(0.0f, ScatterExposure));
        _material.SetShaderParameter("ambient_scatter",           Mathf.Max(0.0f, AmbientScatter));
    }

    private Vector3 BuildRayleighCoefficient(
        float n2, float o2, float ar, float co2, float ch4, float h2, float he)
    {
        Vector3 coefficient =
            n2  * N2Spectrum  +
            o2  * O2Spectrum  +
            ar  * ArSpectrum  +
            co2 * CO2Spectrum +
            ch4 * CH4Spectrum +
            h2  * H2Spectrum  +
            he  * HeSpectrum;

        float maxChannel = Mathf.Max(0.001f,
            Mathf.Max(coefficient.X, Mathf.Max(coefficient.Y, coefficient.Z)));
        return coefficient / maxChannel;
    }

    private float ComputeMeanMolecularMass(
        float n2, float o2, float ar, float co2, float ch4, float h2, float he)
    {
        return n2  * NitrogenMolecularMass
             + o2  * OxygenMolecularMass
             + ar  * ArgonMolecularMass
             + co2 * CarbonDioxideMolecularMass
             + ch4 * MethaneMolecularMass
             + h2  * HydrogenMolecularMass
             + he  * HeliumMolecularMass;
    }

    private float GetCompositionScaleHeightMultiplier(float meanMolecularMass)
    {
        float ratio = EarthMeanMolecularMass / Mathf.Max(1.0f, meanMolecularMass);
        return Mathf.Clamp(ratio, 0.35f, 6.0f);
    }

    private void GetNormalizedComposition(
        out float n2, out float o2, out float ar, out float co2,
        out float ch4, out float h2, out float he)
    {
        n2  = Mathf.Max(0.0f, Nitrogen);
        o2  = Mathf.Max(0.0f, Oxygen);
        ar  = Mathf.Max(0.0f, Argon);
        co2 = Mathf.Max(0.0f, CarbonDioxide);
        ch4 = Mathf.Max(0.0f, Methane);
        h2  = Mathf.Max(0.0f, Hydrogen);
        he  = Mathf.Max(0.0f, Helium);

        float sum = n2 + o2 + ar + co2 + ch4 + h2 + he;
        if (sum <= 0.000001f)
        {
            n2 = 1.0f; o2 = ar = co2 = ch4 = h2 = he = 0.0f;
            return;
        }

        float inv = 1.0f / sum;
        n2 *= inv; o2 *= inv; ar *= inv; co2 *= inv;
        ch4 *= inv; h2 *= inv; he *= inv;
    }

    private void RandomizeSeaLevelPressure()
    {
        var rng = new RandomNumberGenerator();
        if (UsePlanetSeedForRandomPressure && _planet != null)
            rng.Seed = (ulong)Mathf.Max(1, _planet.Seed + 8971);
        else if (RandomPressureSeed > 0)
            rng.Seed = (ulong)RandomPressureSeed;
        else
            rng.Randomize();

        SeaLevelPressure   = rng.RandfRange(SeaLevelPressureRange.X,   SeaLevelPressureRange.Y);
        PressureScaleHeight = rng.RandfRange(PressureScaleHeightRange.X, PressureScaleHeightRange.Y);
        DensityFalloff     = rng.RandfRange(DensityFalloffRange.X,     DensityFalloffRange.Y);
        RayleighStrength   = rng.RandfRange(RayleighStrengthRange.X,   RayleighStrengthRange.Y);
        AtmosphereAlpha    = rng.RandfRange(AtmosphereAlphaRange.X,    AtmosphereAlphaRange.Y);
    }

    private void RandomizeComposition()
    {
        var rng = new RandomNumberGenerator();
        if (UsePlanetSeedForRandomComposition && _planet != null)
            rng.Seed = (ulong)Mathf.Max(1, _planet.Seed + 5413);
        else if (RandomCompositionSeed > 0)
            rng.Seed = (ulong)RandomCompositionSeed;
        else
            rng.Randomize();

        int   profile  = rng.RandiRange(0, 3);
        float n2Base   = 0.2f,  o2Base  = 0.02f, arBase  = 0.005f,
              co2Base  = 0.02f, ch4Base = 0.005f, h2Base  = 0.02f, heBase = 0.005f;

        switch (profile)
        {
            case 0: n2Base = 0.65f; o2Base = 0.18f; arBase  = 0.02f;  co2Base = 0.01f; break;
            case 1: co2Base = 0.72f; n2Base = 0.18f; o2Base = 0.02f;  arBase  = 0.03f; break;
            case 2: h2Base  = 0.68f; heBase = 0.24f; ch4Base = 0.03f; n2Base  = 0.03f; break;
            case 3: n2Base  = 0.52f; ch4Base = 0.26f; h2Base = 0.1f;  co2Base = 0.05f; break;
        }

        Nitrogen     = n2Base  * rng.RandfRange(0.75f, 1.25f);
        Oxygen       = o2Base  * rng.RandfRange(0.6f,  1.4f);
        Argon        = arBase  * rng.RandfRange(0.5f,  1.8f);
        CarbonDioxide = co2Base * rng.RandfRange(0.6f, 1.5f);
        Methane      = ch4Base * rng.RandfRange(0.6f,  1.6f);
        Hydrogen     = h2Base  * rng.RandfRange(0.7f,  1.4f);
        Helium       = heBase  * rng.RandfRange(0.7f,  1.4f);

        GetNormalizedComposition(
            out Nitrogen, out Oxygen, out Argon, out CarbonDioxide,
            out Methane, out Hydrogen, out Helium);
    }
}