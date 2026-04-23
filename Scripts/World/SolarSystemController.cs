using Godot;
using System;
using System.Collections.Generic;

public class PlanetMotionData
{
    public PlanetBody Body;
    public PlanetData Data;
    public Node3D OrbitCenter;
    public float OrbitAngle;
    public float OrbitRadius;
    public bool IsTidallyLocked;
}

public class MoonMotionData
{
    public PlanetBody Body;
    public MoonData Data;
    public Node3D OrbitCenter;
    public float OrbitAngle;
    public float OrbitRadius;
    public bool IsTidallyLocked;
}

public partial class SolarSystemController : Node3D
{
    [Export] public NodePath PlanetPath = default!;
    [Export] public NodePath PlayerPath = default!;
    [Export] public bool GenerateSolarSystem = true;
    [Export] public float StarDistance = 50.0f;
    [Export] public float PlanetDistance = 500.0f;

    private PlanetBody _planet;
    private SpaceExplorerController _player;
    private StarSystemData? _starSystem;
    private List<PlanetMotionData> _planetMotions = new List<PlanetMotionData>();
    private Node3D? _sunNode;
    private List<MoonMotionData> _moonMotions = new List<MoonMotionData>();

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

        if (GenerateSolarSystem)
        {
            GenerateStarSystem();
        }

        if (_player != null)
        {
            Vector3 spawnPoint;
            Vector3 lookTarget;
            
            if (_sunNode != null)
            {
                spawnPoint = _sunNode.GlobalPosition + new Vector3(10, 10, 10);
                lookTarget = _sunNode.GlobalPosition;
            }
            else if (_planetMotions.Count > 0)
            {
                _planet = _planetMotions[0].Body;
                spawnPoint = _planet.GlobalPosition + Vector3.Up * (_planet.Radius + _planet.MaxHeight + 55.0f);
                lookTarget = _planet.GlobalPosition;
            }
            else if (_planet != null)
            {
                spawnPoint = _planet.GlobalPosition + Vector3.Up * (_planet.Radius + _planet.MaxHeight + 55.0f);
                lookTarget = _planet.GlobalPosition;
            }
            else
            {
                return;
            }
            
            _player.GlobalPosition = spawnPoint;
            _player.Velocity = Vector3.Zero;

            Vector3 up = (spawnPoint - lookTarget).Normalized();
            _player.UpDirection = up;
            _player.LookAt(lookTarget, up);
        }
    }

    private void GenerateStarSystem()
    {
        _starSystem = new StarSystemData();

        CreateSun();
        CreatePlanetsAndMoons();
    }

    private void CreateSun()
    {
        if (_starSystem == null || _starSystem.starPart == null) return;

        var star = _starSystem.starPart.allStars[0];

        _sunNode = new Node3D
        {
            Name = star.name
        };
        AddChild(_sunNode);
        _sunNode.AddToGroup("generated_sun");
  
        float sunDistance = StarDistance;
        _sunNode.GlobalPosition = new Vector3(sunDistance, sunDistance * 0.3f, 0);

        float luminosity = star.luminosity;
        float energy = Math.Clamp(luminosity * 10f, 0.5f, 5.0f);

        var omniLight = new OmniLight3D
        {
            Name = "SunLight",
            LightEnergy = energy * 3.0f,
            OmniRange = 4096f,
            OmniAttenuation = 0.3f,
            ShadowEnabled = true
        };
        omniLight.SetColor(GetStarColor(star.temperature));
        _sunNode.AddChild(omniLight);

        var meshInstance = new MeshInstance3D
        {
            Name = "SunMesh"
        };
        float sunRadius = Math.Clamp(star.radius * 50000f, 5f, 200f);
        var sphere = new SphereMesh
        {
            Radius = sunRadius,
            Height = sunRadius * 2
        };
        meshInstance.Mesh = sphere;

        var sunMaterial = new StandardMaterial3D
        {
            EmissionEnabled = true,
            Emission = GetStarColor(star.temperature),
            EmissionEnergyMultiplier = energy * 2,
            AlbedoColor = GetStarColor(star.temperature)
        };
        meshInstance.MaterialOverride = sunMaterial;
        _sunNode.AddChild(meshInstance);
    }

    private void CreatePlanetsAndMoons()
    {
        if (_starSystem == null || _starSystem.planetPart == null) return;

        var planetPart = _starSystem.planetPart;

        for (int i = 0; i < planetPart.planets.Count; i++)
        {
            var planet = planetPart.planets[i];

            if (planet.type == PlanetType.PlanetoidBelt) continue;

            var planetBody = new PlanetBody
            {
                Name = planet.name,
                Radius = Math.Clamp(planet.radius * 0.001f, 10f, 500f),
                MaxHeight = Math.Clamp(planet.radius * 0.0005f, 1f, 50f)
            };
            AddChild(planetBody);

            float planetOrbitDistance = PlanetDistance * (i + 1);
            planetBody.GlobalPosition = new Vector3(planetOrbitDistance, 0, 0);

            ApplyPlanetProperties(planetBody, planet);

            var orbitCenter = new Node3D { Name = planet.name + "_OrbitCenter" };
            AddChild(orbitCenter);
            orbitCenter.GlobalPosition = _sunNode != null ? _sunNode.GlobalPosition : Vector3.Zero;

            float orbitPeriod = planet.orbitPeriod > 0 ? planet.orbitPeriod : 8760f;
            float orbitSpeedDegPerSec = 360f / (float)orbitPeriod;

            var motionData = new PlanetMotionData
            {
                Body = planetBody,
                Data = planet,
                OrbitCenter = orbitCenter,
                OrbitAngle = (float)(i * Math.PI * 2 / planetPart.planets.Count),
                OrbitRadius = planetOrbitDistance,
                IsTidallyLocked = false
            };
            _planetMotions.Add(motionData);

            if (planet.moons.Count > 0)
            {
                CreateMoons(planetBody, planet, planetOrbitDistance);
            }
        }
    }

    private void ApplyPlanetProperties(PlanetBody planetBody, PlanetData planet)
    {
        var planetType = planet.type;
        
        if (planetType == PlanetType.Terra)
        {
            planetBody.SurfaceColor = new Color(0.4f, 0.5f, 0.3f);
            planetBody.HighlandColor = new Color(0.5f, 0.45f, 0.4f);
            planetBody.MariaColor = new Color(0.3f, 0.3f, 0.35f);
        }
        else if (planetType == PlanetType.Gas)
        {
            planetBody.SurfaceColor = new Color(0.9f, 0.7f, 0.5f);
            planetBody.HighlandColor = new Color(0.8f, 0.6f, 0.4f);
            planetBody.MariaColor = new Color(0.7f, 0.5f, 0.4f);
        }
        else
        {
            planetBody.SurfaceColor = new Color(0.6f, 0.6f, 0.6f);
            planetBody.HighlandColor = new Color(0.7f, 0.7f, 0.7f);
            planetBody.MariaColor = new Color(0.5f, 0.5f, 0.5f);
        }

        planetBody.WaterCoverage = Math.Clamp(planet.waterCoverage, 0f, 1f);

        float obliquityDeg = planet.obliquity;
        if (obliquityDeg > 0)
        {
            planetBody.RotationDegrees = new Vector3(0, 0, (float)obliquityDeg);
        }

        ApplyAtmosphereProperties(planetBody, planet);

        planetBody.HeightScale = Math.Clamp(planet.radius * 0.01f, 0.5f, 5f);
        planetBody.Seed = (int)(planet.mass * 1000);
    }

    private void ApplyAtmosphereProperties(PlanetBody planetBody, BasePlanetData data)
    {
        bool hasAtmosphere = data.atmosphereRetentionFactor > 0.1f;
        planetBody.EnableAtmosphere = hasAtmosphere;

        if (!hasAtmosphere) return;

        float pressure = data.atmospherePressure;
        if (pressure <= 0) pressure = data.totalAtmosphereMass * 100f;
        pressure = Math.Clamp(pressure, 0.01f, 5000f);

        if (data.atmosphereType == AtmosphereType.Venus || data.atmosphereType == AtmosphereType.Dulcinea)
        {
            planetBody.AtmosphereRadiusMultiplier = 1.1f;
            planetBody.AtmosphereMaxThickness = Math.Clamp(pressure * 0.1f, 20f, 120f);
            planetBody.AtmosphereDensity = Math.Clamp(pressure * 0.05f, 1f, 10f);
        }
        else if (data.atmosphereType == AtmosphereType.Earth || data.atmosphereType == AtmosphereType.Titan)
        {
            planetBody.AtmosphereRadiusMultiplier = 1.05f;
            planetBody.AtmosphereMaxThickness = Math.Clamp(pressure * 0.05f, 10f, 40f);
            planetBody.AtmosphereDensity = Math.Clamp(pressure * 0.02f, 0.5f, 5f);
        }
        else if (data.atmosphereType == AtmosphereType.Mars)
        {
            planetBody.AtmosphereRadiusMultiplier = 1.03f;
            planetBody.AtmosphereMaxThickness = Math.Clamp(pressure * 0.05f, 5f, 20f);
            planetBody.AtmosphereDensity = Math.Clamp(pressure * 0.02f, 0.3f, 2f);
        }
        else
        {
            float retentionFactor = Math.Clamp(data.atmosphereRetentionFactor, 0f, 2f);
            planetBody.AtmosphereRadiusMultiplier = 1.0f + retentionFactor * 0.05f;
            planetBody.AtmosphereMaxThickness = Math.Clamp(pressure * 0.02f + 5f, 5f, 50f);
            planetBody.AtmosphereDensity = Math.Clamp(retentionFactor * 0.5f, 0.1f, 3f);
        }

        planetBody.AtmosphereH2 = Math.Clamp(data.atmosphereH2, 0f, 100f);
        planetBody.AtmosphereHe = Math.Clamp(data.atmosphereHe, 0f, 100f);
        planetBody.AtmosphereN2 = Math.Clamp(data.atmosphereN2, 0f, 100f);
        planetBody.AtmosphereCO2 = Math.Clamp(data.atmosphereCO2, 0f, 100f);
        planetBody.AtmosphereO2 = Math.Clamp(data.atmosphereMO2, 0f, 100f);
        planetBody.AtmosphereCH4 = Math.Clamp(data.atmosphereCH4, 0f, 100f);
        planetBody.AtmosphereO3 = Math.Clamp(data.atmosphereO3, 0f, 100f);
        planetBody.AtmosphereH2O = Math.Clamp(data.atmosphereH2O, 0f, 100f);

        planetBody.ApplyAtmosphereComposition(data);
    }

    private void CreateMoons(Node3D parentPlanet, PlanetData planet, float planetDistance)
    {
        if (planet.moons == null || planet.moons.Count == 0)
        {
            GD.Print($"Planet {planet.name} has no moons");
            return;
        }

        GD.Print($"Creating {planet.moons.Count} moons for planet {planet.name}");
        for (int i = 0; i < planet.moons.Count; i++)
        {
            var moon = planet.moons[i];

            bool isTideLocked = Math.Abs(moon.rotationPeriod - moon.orbitPeriod) < 0.001f || 
                               (moon.orbitPeriod > 0 && Math.Abs(moon.rotationPeriod / moon.orbitPeriod - 1.0f) < 0.001f);

            var moonBody = new PlanetBody
            {
                Name = moon.name + (isTideLocked ? "_TidallyLocked" : ""),
                Radius = Math.Clamp(moon.radius * 0.001f, 1f, 50f),
                MaxHeight = Math.Clamp(moon.radius * 0.0005f, 0.5f, 5f)
            };
            parentPlanet.AddChild(moonBody);

            float moonDistance = planet.radius * 0.01f + (i + 1) * 10f;

            var moonOrbitCenter = new Node3D { Name = moon.name + "_OrbitCenter" };
            parentPlanet.AddChild(moonOrbitCenter);

            var moonMotion = new MoonMotionData
            {
                Body = moonBody,
                Data = moon,
                OrbitCenter = moonOrbitCenter,
                OrbitAngle = (float)(i * Math.PI * 2 / planet.moons.Count),
                OrbitRadius = moonDistance,
                IsTidallyLocked = isTideLocked
            };
            _moonMotions.Add(moonMotion);

            moonBody.SurfaceColor = moon.moonType == MoonType.Rocky 
                ? new Color(0.6f, 0.5f, 0.4f) 
                : new Color(0.7f, 0.8f, 0.9f);
            moonBody.HighlandColor = moonBody.SurfaceColor * 1.2f;
            moonBody.MariaColor = moonBody.SurfaceColor * 0.8f;
            moonBody.HeightScale = 0.5f;
            moonBody.Seed = (int)(moon.mass * 10000);

            moonBody.WaterCoverage = Math.Clamp(moon.waterCoverage, 0f, 1f);

            float moonObliquityDeg = moon.obliquity;
            if (moonObliquityDeg > 0)
            {
                moonBody.RotationDegrees = new Vector3(0, 0, (float)moonObliquityDeg);
            }

            ApplyAtmosphereProperties(moonBody, moon);

            if (isTideLocked)
            {
                var tidallyLockedLabel = new Label3D
                {
                    Text = "Tidally Locked",
                    Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                    Position = new Vector3(0, moonBody.Radius + moonBody.MaxHeight + 2f, 0),
                    FontSize = 32,
                    Modulate = new Color(1, 0.5f, 0)
                };
                moonBody.AddChild(tidallyLockedLabel);
            }
        }
    }

    private Color GetStarColor(float temperature)
    {
        if (temperature > 10000)
            return new Color(0.6f, 0.7f, 1.0f);
        else if (temperature > 7500)
            return new Color(0.9f, 0.9f, 1.0f);
        else if (temperature > 6000)
            return new Color(1.0f, 1.0f, 0.9f);
        else if (temperature > 5200)
            return new Color(1.0f, 0.95f, 0.8f);
        else if (temperature > 3700)
            return new Color(1.0f, 0.8f, 0.5f);
        else
            return new Color(1.0f, 0.5f, 0.3f);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        foreach (var motion in _planetMotions)
        {
            if (motion.Data == null || motion.Body == null || motion.OrbitCenter == null)
            {
                continue;
            }

            float orbitPeriod = motion.Data.orbitPeriod > 0 ? motion.Data.orbitPeriod : 8760f;
            float orbitSpeedDegPerSec = 360f / orbitPeriod;
            motion.OrbitAngle += orbitSpeedDegPerSec * dt * 0.001f;

            float x = (float)Math.Cos(motion.OrbitAngle) * motion.OrbitRadius;
            float z = (float)Math.Sin(motion.OrbitAngle) * motion.OrbitRadius;
            motion.Body.GlobalPosition = motion.OrbitCenter.GlobalPosition + new Vector3(x, 0, z);

            if (motion.Data.rotationPeriod > 0)
            {
                float rotationSpeedDegPerSec = 360f / (float)motion.Data.rotationPeriod;
                motion.Body.Rotate(Vector3.Up, Mathf.DegToRad(rotationSpeedDegPerSec * dt));
            }
        }

        foreach (var motion in _moonMotions)
        {
            if (motion.Data == null || motion.Body == null || motion.OrbitCenter == null)
            {
                continue;
            }

            float orbitPeriod = motion.Data.orbitPeriod > 0 ? motion.Data.orbitPeriod : 720f;
            float orbitSpeedDegPerSec = 360f / orbitPeriod;
            motion.OrbitAngle += orbitSpeedDegPerSec * dt * 0.001f;

            float x = (float)Math.Cos(motion.OrbitAngle) * motion.OrbitRadius;
            float z = (float)Math.Sin(motion.OrbitAngle) * motion.OrbitRadius;
            motion.Body.GlobalPosition = motion.OrbitCenter.GlobalPosition + new Vector3(x, 0, z);

            if (motion.Data.rotationPeriod > 0 && !motion.IsTidallyLocked)
            {
                float rotationSpeedDegPerSec = 360f / (float)motion.Data.rotationPeriod;
                motion.Body.Rotate(Vector3.Up, Mathf.DegToRad(rotationSpeedDegPerSec * dt));
            }
        }
    }
}
