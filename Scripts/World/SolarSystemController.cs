using Godot;
using System;
using System.Collections.Generic;

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
    private List<PlanetBody> _generatedPlanets = new List<PlanetBody>();
    private Node3D? _sunNode;

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
            else if (_generatedPlanets.Count > 0)
            {
                _planet = _generatedPlanets[0];
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

            _generatedPlanets.Add(planetBody);

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
            planetBody.EnableAtmosphere = true;
            planetBody.AtmosphereRadiusMultiplier = 1.05f;
            planetBody.AtmosphereMaxThickness = 30f;
            planetBody.AtmosphereDensity = 4f;
        }
        else if (planetType == PlanetType.Gas)
        {
            planetBody.SurfaceColor = new Color(0.9f, 0.7f, 0.5f);
            planetBody.HighlandColor = new Color(0.8f, 0.6f, 0.4f);
            planetBody.MariaColor = new Color(0.7f, 0.5f, 0.4f);
            planetBody.EnableAtmosphere = true;
            planetBody.AtmosphereRadiusMultiplier = 1.1f;
            planetBody.AtmosphereMaxThickness = 80f;
            planetBody.AtmosphereDensity = 8f;
        }
        // else if (planetType == PlanetType.Ocean)
        // {
        //     planetBody.SurfaceColor = new Color(0.2f, 0.4f, 0.8f);
        //     planetBody.HighlandColor = new Color(0.3f, 0.5f, 0.4f);
        //     planetBody.MariaColor = new Color(0.1f, 0.2f, 0.5f);
        //     planetBody.EnableAtmosphere = true;
        //     planetBody.AtmosphereRadiusMultiplier = 1.03f;
        //     planetBody.AtmosphereMaxThickness = 20f;
        //     planetBody.AtmosphereDensity = 3f;
        // }
        else
        {
            planetBody.SurfaceColor = new Color(0.6f, 0.6f, 0.6f);
            planetBody.HighlandColor = new Color(0.7f, 0.7f, 0.7f);
            planetBody.MariaColor = new Color(0.5f, 0.5f, 0.5f);
            planetBody.EnableAtmosphere = false;
        }

        planetBody.HeightScale = Math.Clamp(planet.radius * 0.01f, 0.5f, 5f);
        planetBody.Seed = (int)(planet.mass * 1000);
    }

    private void CreateMoons(Node3D parentPlanet, PlanetData planet, float planetDistance)
    {
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
            moonBody.GlobalPosition = new Vector3(moonDistance, 0, 0);

            moonBody.SurfaceColor = moon.moonType == MoonType.Rocky 
                ? new Color(0.6f, 0.5f, 0.4f) 
                : new Color(0.7f, 0.8f, 0.9f);
            moonBody.HighlandColor = moonBody.SurfaceColor * 1.2f;
            moonBody.MariaColor = moonBody.SurfaceColor * 0.8f;
            moonBody.EnableAtmosphere = false;
            moonBody.HeightScale = 0.5f;
            moonBody.Seed = (int)(moon.mass * 10000);

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
}
