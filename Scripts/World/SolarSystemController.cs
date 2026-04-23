using Godot;
using System;

public partial class SolarSystemController : Node3D
{
    [Export] public NodePath PlanetPath = default!;
    [Export] public NodePath PlayerPath = default!;
    [Export] public bool GenerateSolarSystem = true;
    [Export] public float StarDistance = 50.0f;
    [Export] public float PlanetDistance = 50.0f;

    private PlanetBody _planet;
    private SpaceExplorerController _player;
    private StarSystemData? _starSystem;

    public override void _Ready()
    {
        GD.PrintErr("========== SolarSystemController._Ready() START ==========");
        
        if (PlanetPath != null && !PlanetPath.IsEmpty)
        {
            _planet = GetNodeOrNull<PlanetBody>(PlanetPath);
        }

        if (PlayerPath != null && !PlayerPath.IsEmpty)
        {
            _player = GetNodeOrNull<SpaceExplorerController>(PlayerPath);
        }

        try
        {
            if (GenerateSolarSystem)
            {
                GD.Print("Generating solar system...");
                GenerateStarSystem();
            }
            else
            {
                GD.Print("Solar system generation disabled");
            }
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"Exception in GenerateStarSystem: {ex.Message}");
            GD.PrintErr(ex.StackTrace);
        }

        try
        {
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
        catch (System.Exception ex)
        {
            GD.PrintErr($"Exception in player/planet setup: {ex.Message}");
            GD.PrintErr(ex.StackTrace);
        }
        
        GD.Print($"Main node children count: {GetChildCount()}");
        GD.Print("Children list:");
        foreach (var child in GetChildren())
        {
            GD.Print($"  - {child.Name} ({child.GetType().Name})");
        }
        GD.PrintErr("========== SolarSystemController._Ready() END ==========");
    }

    private void GenerateStarSystem()
    {
        _starSystem = new StarSystemData();
        
        GD.Print($"Generating star system: {_starSystem.starPart.allStars[0].name}");
        GD.Print($"Number of planets: {_starSystem.planetPart.planets.Count}");

        CreateSun();
        CreatePlanetsAndMoons();
    }

    private void CreateSun()
    {
        if (_starSystem == null || _starSystem.starPart == null) return;

        var star = _starSystem.starPart.allStars[0];
        
        GD.Print($"Creating sun: {star.name}, temp: {star.temperature}, luminosity: {star.luminosity}, radius: {star.radius}");

        var sunNode = new Node3D
        {
            Name = star.name
        };
        AddChild(sunNode);

        float sunDistance = StarDistance;
        sunNode.GlobalPosition = new Vector3(sunDistance, sunDistance * 0.3f, 0);

        float luminosity = star.luminosity;
        float energy = Math.Clamp(luminosity * 0.1f, 0.5f, 5.0f);

        var omniLight = new OmniLight3D
        {
            Name = "SunLight",
            LightEnergy = energy * 3.0f,
            OmniRange = sunDistance * 5,
            OmniAttenuation = 0.3f,
            ShadowEnabled = true
        };
        omniLight.SetColor(GetStarColor(star.temperature));
        sunNode.AddChild(omniLight);

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
        sunNode.AddChild(meshInstance);

        var directionalLight = new DirectionalLight3D
        {
            Name = "DirectionalSun",
            LightEnergy = energy * 0.5f,
            ShadowEnabled = true
        };
        directionalLight.SetColor(GetStarColor(star.temperature));
        directionalLight.RotationDegrees = new Vector3(-30, 45, 0);
        AddChild(directionalLight);
    }

    private void CreatePlanetsAndMoons()
    {
        if (_starSystem == null || _starSystem.planetPart == null) return;

        var planetPart = _starSystem.planetPart;

        for (int i = 0; i < planetPart.planets.Count; i++)
        {
            var planet = planetPart.planets[i];

            if (planet.type == PlanetType.PlanetoidBelt) continue;

            var planetNode = new Node3D
            {
                Name = planet.name
            };
            AddChild(planetNode);

            float planetOrbitDistance = PlanetDistance * (i + 1);
            planetNode.GlobalPosition = new Vector3(planetOrbitDistance, 0, 0);

            if (planet.moons.Count > 0)
            {
                CreateMoons(planetNode, planet, planetOrbitDistance);
            }
        }
    }

    private void CreateMoons(Node3D parentPlanet, PlanetData planet, float planetDistance)
    {
        for (int i = 0; i < planet.moons.Count; i++)
        {
            var moon = planet.moons[i];

            bool isTideLocked = Math.Abs(moon.rotationPeriod - moon.orbitPeriod) < 0.001f || 
                               (moon.orbitPeriod > 0 && Math.Abs(moon.rotationPeriod / moon.orbitPeriod - 1.0f) < 0.001f);

            var moonNode = new Node3D
            {
                Name = moon.name + (isTideLocked ? "_TidallyLocked" : "")
            };
            parentPlanet.AddChild(moonNode);

            float moonDistance = planet.radius * 3 + (i + 1) * 20f;
            moonNode.Position = new Vector3(moonDistance, 0, 0);

            float moonRadius = Math.Clamp(moon.radius * 0.01f, 0.5f, 20f);
            var meshInstance = new MeshInstance3D
            {
                Name = "MoonMesh"
            };
            var sphere = new SphereMesh
            {
                Radius = moonRadius,
                Height = moonRadius * 2
            };
            meshInstance.Mesh = sphere;

            var moonMaterial = new StandardMaterial3D
            {
                AlbedoColor = moon.moonType == MoonType.Rocky 
                    ? new Color(0.6f, 0.5f, 0.4f) 
                    : new Color(0.7f, 0.8f, 0.9f)
            };
            meshInstance.MaterialOverride = moonMaterial;
            moonNode.AddChild(meshInstance);

            if (isTideLocked)
            {
                var tidallyLockedLabel = new Label3D
                {
                    Text = "Tidally Locked",
                    Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                    Position = new Vector3(0, moonRadius * 2, 0),
                    FontSize = 32,
                    Modulate = new Color(1, 0.5f, 0)
                };
                moonNode.AddChild(tidallyLockedLabel);
            }

            if (moon.orbitPeriod > 0)
            {
                var orbitMotion = new Node3D
                {
                    Name = "OrbitMotion"
                };
                parentPlanet.AddChild(orbitMotion);
                
                moonNode.Reparent(orbitMotion);

                float orbitSpeed = 1.0f / (float)moon.orbitPeriod;
                orbitMotion.SetMeta("orbit_speed", orbitSpeed);
                orbitMotion.SetMeta("orbit_distance", moonDistance);
                orbitMotion.SetMeta("moon_node", moonNode);

                orbitMotion.AddToGroup("orbit_motions");
            }
        }
    }

    public override void _Process(double delta)
    {
        foreach (var node in GetChildren())
        {
            if (node is Node3D orbitMotion && orbitMotion.HasMeta("orbit_speed"))
            {
                float orbitSpeed = (float)orbitMotion.GetMeta("orbit_speed");
                float orbitDistance = (float)orbitMotion.GetMeta("orbit_distance");
                
                float currentAngle = orbitMotion.RotationDegrees.Y;
                currentAngle += orbitSpeed * (float)delta * 0.1f;
                orbitMotion.RotationDegrees = new Vector3(0, currentAngle, 0);

                foreach (var child in orbitMotion.GetChildren())
                {
                    if (child is Node3D moon)
                    {
                        moon.Position = new Vector3(orbitDistance, 0, 0);
                    }
                }
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
