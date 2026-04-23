using Godot;
using System;
using System.Collections.Generic;

public partial class PlanetBody : Node3D
{
    [Export] public float Radius = 220.0f;
    [Export] public float MaxHeight = 14.0f;
    [Export] public int BaseResolution = 10;
    [Export] public int MaxLodLevel = 3;
    [Export] public float LodUpdateInterval = 0.35f;

    [ExportGroup("Terrain - Lunar")]
    [Export] public float HeightScale = 1.0f;
    [Export] public float BaseFrequency = 2.2f;
    [Export] public float BaseReliefStrength = 0.18f;
    [Export] public float DetailFrequency = 18.0f;
    [Export] public float DetailStrength = 0.05f;
    [Export] public float MariaFrequency = 1.1f;
    [Export] public float MariaThreshold = 0.57f;
    [Export] public float MariaDepression = 0.11f;
    [Export] public float MariaDetailSuppression = 0.65f;
    [Export] public float SpikeSoftClamp = 0.9f;
    [Export] public float AngularSmoothingStrength = 0.55f;
    [Export] public float AngularSmoothingRadiusDeg = 0.55f;

    [ExportGroup("Terrain - Craters")]
    [Export] public int CraterCount = 260;
    [Export] public float CraterMinRadiusDeg = 0.8f;
    [Export] public float CraterMaxRadiusDeg = 9.0f;
    [Export] public float LargeCraterRatio = 0.5f;
    [Export] public float CraterDepthScale = 0.17f;
    [Export] public float CraterRimHeightScale = 0.22f;
    [Export] public float CraterRimWidth = 0.36f;
    [Export] public float CraterBlendStrength = 1.0f;
    [Export] public bool UseAntiCellCraterTool = true;
    [Export] public float AntiCellFrequency = 2.2f;
    [Export] public float AntiCellThreshold = 0.58f;
    [Export] public float AntiCellJitter = 0.45f;
    [Export] public int AntiCellCandidateMultiplier = 7;
    [Export] public float CraterMinAngularSeparationDeg = 0.38f;

    [ExportGroup("Seed")]
    [Export] public int Seed = 1337;
    [Export] public bool RandomizeSeedOnReady = true;

    [ExportGroup("Material")]
    [Export] public Color SurfaceColor = new Color("4f7a5a");
    [Export] public Color HighlandColor = new Color("8f8a80");
    [Export] public Color MariaColor = new Color("595756");
    [Export] public Color CraterRimColor = new Color("c9bdaa");
    [Export] public Color CraterFloorColor = new Color("3f3d3b");
    [Export] public float ColorContrastStrength = 0.55f;

    [ExportGroup("Atmosphere")]
    [Export] public bool EnableAtmosphere = true;
    [Export(PropertyHint.Range, "1.0,1.15,0.001")] public float AtmosphereRadiusMultiplier = 0.0f;
    [Export(PropertyHint.Range, "0.0,10.0,0.1")] public float AtmosphereBaseOffset = 0.8f;
    [Export(PropertyHint.Range, "2.0,80.0,0.5")] public float AtmosphereMinThickness = 6.0f;
    [Export(PropertyHint.Range, "2.0,120.0,0.5")] public float AtmosphereMaxThickness = 26.0f;
    [Export(PropertyHint.Range, "0.1,4.0,0.01")] public float AtmosphereDensity = 1.0f;
    [Export(PropertyHint.Range, "0.0,3.0,0.01")] public float SunsetStrength = 1.0f;
    [Export] public NodePath SunLightPath = default!;

    [ExportGroup("Atmosphere Composition")]
    [Export(PropertyHint.Range, "0.0,100.0,0.0001")] public float AtmosphereO2 = 20.95f;
    [Export(PropertyHint.Range, "0.0,100.0,0.0001")] public float AtmosphereCH4 = 0.00018f;
    [Export(PropertyHint.Range, "0.0,100.0,0.0001")] public float AtmosphereO3 = 0.000007f;
    [Export(PropertyHint.Range, "0.0,100.0,0.0001")] public float AtmosphereH2O = 1.0f;
    [Export(PropertyHint.Range, "0.0,100.0,0.0001")] public float AtmosphereCO2 = 0.042f;
    [Export(PropertyHint.Range, "0.0,100.0,0.0001")] public float AtmosphereH2 = 0.0f;
    [Export(PropertyHint.Range, "0.0,100.0,0.0001")] public float AtmosphereHe = 0.0f;
    [Export(PropertyHint.Range, "0.0,100.0,0.0001")] public float AtmosphereN2 = 78.0f;

    private static readonly Vector3[] FaceDirections =
    {
        Vector3.Up,
        Vector3.Down,
        Vector3.Left,
        Vector3.Right,
        Vector3.Forward,
        Vector3.Back,
    };

    private readonly struct CraterData
    {
        public readonly Vector3 Center;
        public readonly float RadiusChord;
        public readonly float Depth;
        public readonly float RimHeight;

        public CraterData(Vector3 center, float radiusChord, float depth, float rimHeight)
        {
            Center = center;
            RadiusChord = radiusChord;
            Depth = depth;
            RimHeight = rimHeight;
        }
    }

    private readonly FaceRuntime[] _faces = new FaceRuntime[6];
    private readonly List<CraterData> _craters = new List<CraterData>();
    private FastNoiseLite _baseNoise = new FastNoiseLite();
    private FastNoiseLite _detailNoise = new FastNoiseLite();
    private FastNoiseLite _mariaNoise = new FastNoiseLite();
    private Camera3D _camera;
    private float _lodTimer;
    private int _currentLod = -1;
    private StandardMaterial3D _material;
    private float _observedMaxElevation;
    private float _observedPositiveElevationSum;
    private int _observedPositiveElevationCount;
    private MeshInstance3D _atmosphereMesh;
    private ShaderMaterial _atmosphereMaterial;
    private DirectionalLight3D _sunLight;

    private sealed class FaceRuntime
    {
        public MeshInstance3D MeshInstance = default!;
        public CollisionShape3D CollisionShape = default!;
    }

    public override void _Ready()
    {
        if (RandomizeSeedOnReady)
        {
            RandomNumberGenerator rng = new RandomNumberGenerator();
            rng.Randomize();
            Seed = (int)rng.RandiRange(1, int.MaxValue);
        }

        _camera = GetViewport().GetCamera3D();
        ConfigureNoise();
        BuildCraterField();
        CreateFaceNodes();

        _material = new StandardMaterial3D
        {
            AlbedoColor = Colors.White,
            Metallic = 0.0f,
            Roughness = 1.0f,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.PerVertex,
            VertexColorUseAsAlbedo = true,
        };

        EnsureAtmosphereNode();
        UpdateAtmosphereFromComposition();

        RebuildPlanet(MaxLodLevel);
    }

    public override void _Process(double delta)
    {
        _lodTimer += (float)delta;
        if (_lodTimer < LodUpdateInterval)
        {
            return;
        }

        _lodTimer = 0.0f;
        if (_camera == null)
        {
            _camera = GetViewport().GetCamera3D();
            if (_camera == null)
            {
                return;
            }
        }

        int targetLod = ComputeTargetLod(_camera.GlobalPosition.DistanceTo(GlobalPosition));
        if (targetLod != _currentLod)
        {
            RebuildPlanet(targetLod);
        }

        UpdateAtmosphereRuntime();
    }

    public float GetSurfaceHeight(Vector3 worldPosition)
    {
        Vector3 local = ToLocal(worldPosition);
        Vector3 direction = local.Normalized();
        return Radius + SampleHeight(direction);
    }

    public Vector3 GetUpDirection(Vector3 worldPosition)
    {
        return (worldPosition - GlobalPosition).Normalized();
    }

    public void ApplyAtmosphereComposition(BasePlanetData data)
    {
        if (data == null)
        {
            return;
        }

        AtmosphereO2 = Mathf.Max(0.0f, data.atmosphereMO2);
        AtmosphereCH4 = Mathf.Max(0.0f, data.atmosphereCH4);
        AtmosphereO3 = Mathf.Max(0.0f, data.atmosphereO3);
        AtmosphereH2O = Mathf.Max(0.0f, data.atmosphereH2O);
        AtmosphereCO2 = Mathf.Max(0.0f, data.atmosphereCO2);
        AtmosphereH2 = Mathf.Max(0.0f, data.atmosphereH2);
        AtmosphereHe = Mathf.Max(0.0f, data.atmosphereHe);
        AtmosphereN2 = Mathf.Max(0.0f, data.atmosphereN2);

        UpdateAtmosphereFromComposition();
    }

    private void ConfigureNoise()
    {
        _baseNoise = new FastNoiseLite
        {
            Seed = Seed,
            NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
            FractalType = FastNoiseLite.FractalTypeEnum.Fbm,
            FractalOctaves = 3,
            FractalLacunarity = 2.1f,
            FractalGain = 0.55f,
        };

        _detailNoise = new FastNoiseLite
        {
            Seed = Seed + 911,
            NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
            FractalType = FastNoiseLite.FractalTypeEnum.Fbm,
            FractalOctaves = 2,
            FractalLacunarity = 2.0f,
            FractalGain = 0.5f,
        };

        _mariaNoise = new FastNoiseLite
        {
            Seed = Seed + 271,
            NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
            FractalType = FastNoiseLite.FractalTypeEnum.Fbm,
            FractalOctaves = 2,
            FractalLacunarity = 2.0f,
            FractalGain = 0.5f,
        };
    }

    private void BuildCraterField()
    {
        _craters.Clear();

        if (UseAntiCellCraterTool)
        {
            var options = new AntiCellCraterGenerator.Options(
                seed: Seed,
                targetCount: CraterCount,
                minRadiusDeg: CraterMinRadiusDeg,
                maxRadiusDeg: CraterMaxRadiusDeg,
                largeCraterRatio: LargeCraterRatio,
                depthScale: CraterDepthScale,
                rimHeightScale: CraterRimHeightScale,
                antiCellFrequency: AntiCellFrequency,
                antiCellThreshold: AntiCellThreshold,
                antiCellJitter: AntiCellJitter,
                candidateMultiplier: AntiCellCandidateMultiplier,
                minAngularSeparationDeg: CraterMinAngularSeparationDeg);

            List<CraterDescriptor> generated = AntiCellCraterGenerator.GenerateOnUnitSphere(options);
            for (int i = 0; i < generated.Count; i++)
            {
                CraterDescriptor c = generated[i];
                _craters.Add(new CraterData(c.Center, c.RadiusChord, c.Depth, c.RimHeight));
            }

            return;
        }

        RandomNumberGenerator rng = new RandomNumberGenerator
        {
            Seed = (ulong)Mathf.Max(1, Seed),
        };

        int count = Mathf.Max(0, CraterCount);
        for (int i = 0; i < count; i++)
        {
            bool large = rng.Randf() < Mathf.Clamp(LargeCraterRatio, 0.0f, 1.0f);
            float pick = rng.Randf();
            float bias = large ? Mathf.Pow(pick, 0.45f) : Mathf.Pow(pick, 1.35f);
            float radiusDeg = Mathf.Lerp(CraterMinRadiusDeg, CraterMaxRadiusDeg, bias);
            float radiusRad = Mathf.DegToRad(Mathf.Max(0.01f, radiusDeg));
            float radiusChord = 2.0f * Mathf.Sin(radiusRad * 0.5f);

            float sizeT = Mathf.Clamp((radiusDeg - CraterMinRadiusDeg) / Mathf.Max(0.001f, CraterMaxRadiusDeg - CraterMinRadiusDeg), 0.0f, 1.0f);
            float depth = Mathf.Lerp(0.03f, 0.18f, sizeT) * CraterDepthScale;
            float rimHeight = depth * Mathf.Lerp(0.6f, 1.3f, sizeT) * CraterRimHeightScale;

            Vector3 center = RandomUnitVector(rng);
            _craters.Add(new CraterData(center, radiusChord, depth, rimHeight));
        }
    }

    private static Vector3 RandomUnitVector(RandomNumberGenerator rng)
    {
        float z = rng.RandfRange(-1.0f, 1.0f);
        float a = rng.RandfRange(0.0f, Mathf.Tau);
        float r = Mathf.Sqrt(Mathf.Max(0.0f, 1.0f - z * z));
        return new Vector3(r * Mathf.Cos(a), z, r * Mathf.Sin(a));
    }

    private int ComputeTargetLod(float distanceToCenter)
    {
        float altitude = Mathf.Max(0.0f, distanceToCenter - Radius);
        float ratio = altitude / Mathf.Max(Radius, 0.001f);

        int lod;
        if (ratio > 4.0f)
        {
            lod = 0;
        }
        else if (ratio > 2.0f)
        {
            lod = 1;
        }
        else if (ratio > 0.9f)
        {
            lod = 2;
        }
        else
        {
            lod = 3;
        }

        return Mathf.Clamp(lod, 0, MaxLodLevel);
    }

    private void RebuildPlanet(int lodLevel)
    {
        _currentLod = lodLevel;
        int resolution = BaseResolution << lodLevel;
        _observedMaxElevation = 0.0f;
        _observedPositiveElevationSum = 0.0f;
        _observedPositiveElevationCount = 0;

        for (int i = 0; i < FaceDirections.Length; i++)
        {
            ArrayMesh mesh = BuildFaceMesh(FaceDirections[i], resolution);
            _faces[i].MeshInstance.Mesh = mesh;
            _faces[i].MeshInstance.MaterialOverride = _material;

            ConcavePolygonShape3D shape = new ConcavePolygonShape3D();
            shape.Data = mesh.GetFaces();
            _faces[i].CollisionShape.Shape = shape;
        }

        UpdateAtmosphereFromComposition();
    }

    private ArrayMesh BuildFaceMesh(Vector3 localUp, int resolution)
    {
        Vector3 axisA = new Vector3(localUp.Y, localUp.Z, localUp.X);
        Vector3 axisB = localUp.Cross(axisA);

        int vertexCount = (resolution + 1) * (resolution + 1);
        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        Color[] colors = new Color[vertexCount];
        int[] indices = new int[resolution * resolution * 6];

        int triIndex = 0;
        for (int y = 0; y <= resolution; y++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                int index = x + y * (resolution + 1);
                Vector2 percent = new Vector2(x, y) / (float)resolution;
                Vector3 pointOnCube = localUp
                                      + (percent.X - 0.5f) * 2.0f * axisA
                                      + (percent.Y - 0.5f) * 2.0f * axisB;

                Vector3 unitSphere = pointOnCube.Normalized();
                float elevation = SampleHeight(unitSphere);
                if (elevation > _observedMaxElevation)
                {
                    _observedMaxElevation = elevation;
                }
                if (elevation > 0.0f)
                {
                    _observedPositiveElevationSum += elevation;
                    _observedPositiveElevationCount++;
                }
                Vector3 vertex = unitSphere * (Radius + elevation);

                vertices[index] = vertex;
                normals[index] = unitSphere;
                uvs[index] = percent;
                colors[index] = EvaluateTerrainColor(unitSphere, elevation);

                if (x == resolution || y == resolution)
                {
                    continue;
                }

                int a = index;
                int b = index + resolution + 1;
                int c = index + resolution + 2;
                int d = index + 1;

                WriteTriangleFacingOutward(indices, ref triIndex, vertices, a, b, c);
                WriteTriangleFacingOutward(indices, ref triIndex, vertices, a, c, d);
            }
        }

        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.Normal] = normals;
        arrays[(int)Mesh.ArrayType.TexUV] = uvs;
        arrays[(int)Mesh.ArrayType.Color] = colors;
        arrays[(int)Mesh.ArrayType.Index] = indices;

        ArrayMesh mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return mesh;
    }

    private static void WriteTriangleFacingOutward(int[] indices, ref int triIndex, Vector3[] vertices, int i0, int i1, int i2)
    {
        Vector3 v0 = vertices[i0];
        Vector3 v1 = vertices[i1];
        Vector3 v2 = vertices[i2];

        Vector3 normal = (v1 - v0).Cross(v2 - v0);
        Vector3 centroid = (v0 + v1 + v2) / 3.0f;

        if (normal.Dot(centroid) < 0.0f)
        {
            (i1, i2) = (i2, i1);
        }

        indices[triIndex++] = i0;
        indices[triIndex++] = i1;
        indices[triIndex++] = i2;
    }

    private float SampleHeight(Vector3 direction)
    {
        float raw = EvaluateRawHeight(direction);

        float smoothStrength = Mathf.Clamp(AngularSmoothingStrength, 0.0f, 1.0f);
        if (smoothStrength > 0.001f)
        {
            float r = Mathf.DegToRad(Mathf.Max(0.01f, AngularSmoothingRadiusDeg));
            Vector3 tangentA = GetAnyTangent(direction);
            Vector3 tangentB = direction.Cross(tangentA).Normalized();

            Vector3 d1 = (direction + tangentA * r).Normalized();
            Vector3 d2 = (direction - tangentA * r).Normalized();
            Vector3 d3 = (direction + tangentB * r).Normalized();
            Vector3 d4 = (direction - tangentB * r).Normalized();

            float neighborhood = (
                EvaluateRawHeight(d1) +
                EvaluateRawHeight(d2) +
                EvaluateRawHeight(d3) +
                EvaluateRawHeight(d4)) * 0.25f;

            raw = Mathf.Lerp(raw, neighborhood, smoothStrength);
        }

        float clampPower = Mathf.Max(0.01f, SpikeSoftClamp);
        float clamped = raw / (1.0f + Mathf.Abs(raw) / clampPower);
        return clamped * MaxHeight * Mathf.Max(0.0f, HeightScale);
    }

    private float EvaluateRawHeight(Vector3 direction)
    {
        float macro = _baseNoise.GetNoise3Dv(direction * BaseFrequency) * BaseReliefStrength;
        float detail = _detailNoise.GetNoise3Dv(direction * DetailFrequency) * DetailStrength;

        float mariaSignal = (_mariaNoise.GetNoise3Dv(direction * MariaFrequency) + 1.0f) * 0.5f;
        float mariaMask = SmoothStep(MariaThreshold, 1.0f, mariaSignal);

        float mariaSuppression = Mathf.Lerp(1.0f, 1.0f - Mathf.Clamp(MariaDetailSuppression, 0.0f, 1.0f), mariaMask);
        float mariaDepress = mariaMask * MariaDepression;

        float crater = SampleCraterContribution(direction) * CraterBlendStrength;
        float shaped = macro + detail * mariaSuppression - mariaDepress + crater;

        return Mathf.Clamp(shaped, -1.5f, 1.5f);
    }

    private Color EvaluateTerrainColor(Vector3 direction, float elevation)
    {
        float mariaSignal = (_mariaNoise.GetNoise3Dv(direction * MariaFrequency) + 1.0f) * 0.5f;
        float mariaMask = SmoothStep(MariaThreshold, 1.0f, mariaSignal);

        float crater = SampleCraterContribution(direction) * CraterBlendStrength;
        float rimNorm = Mathf.Clamp(
            crater / Mathf.Max(0.001f, CraterDepthScale * CraterRimHeightScale * 0.1f),
            0.0f,
            1.0f);
        float floorNorm = Mathf.Clamp(
            -crater / Mathf.Max(0.001f, CraterDepthScale * 0.05f),
            0.0f,
            1.0f);

        Color biome = HighlandColor.Lerp(MariaColor, mariaMask);
        Color withCraters = biome.Lerp(CraterRimColor, rimNorm * 0.9f);
        withCraters = withCraters.Lerp(CraterFloorColor, floorNorm * 0.9f);

        float range = Mathf.Max(0.001f, MaxHeight * Mathf.Max(0.0f, HeightScale));
        float h = Mathf.Clamp(elevation / range, -1.0f, 1.0f);
        float contrast = Mathf.Clamp(1.0f + h * ColorContrastStrength, 0.5f, 1.6f);

        withCraters = new Color(
            Mathf.Clamp(withCraters.R * contrast, 0.0f, 1.0f),
            Mathf.Clamp(withCraters.G * contrast, 0.0f, 1.0f),
            Mathf.Clamp(withCraters.B * contrast, 0.0f, 1.0f),
            1.0f);

        // Keep legacy tint as final grade control.
        return withCraters * SurfaceColor;
    }

    private static Vector3 GetAnyTangent(Vector3 n)
    {
        Vector3 refAxis = Mathf.Abs(n.Y) < 0.95f ? Vector3.Up : Vector3.Right;
        return n.Cross(refAxis).Normalized();
    }

    private float SampleCraterContribution(Vector3 direction)
    {
        if (_craters.Count == 0)
        {
            return 0.0f;
        }

        float total = 0.0f;
        float rimWidth = Mathf.Max(0.05f, CraterRimWidth);

        for (int i = 0; i < _craters.Count; i++)
        {
            CraterData crater = _craters[i];
            float chord = Mathf.Sqrt(Mathf.Max(0.0f, 2.0f * (1.0f - direction.Dot(crater.Center))));
            float outerRadius = crater.RadiusChord * (1.0f + rimWidth);
            if (chord > outerRadius)
            {
                continue;
            }

            float t = chord / Mathf.Max(0.0001f, crater.RadiusChord);
            if (t <= 1.0f)
            {
                float bowl = 1.0f - t * t;
                total -= crater.Depth * bowl * bowl;
            }
            else
            {
                float rt = (t - 1.0f) / rimWidth;
                float rim = 1.0f - rt;
                total += crater.RimHeight * rim * rim;
            }
        }

        return total;
    }

    private static float SmoothStep(float edge0, float edge1, float x)
    {
        if (edge1 <= edge0)
        {
            return x >= edge1 ? 1.0f : 0.0f;
        }

        float t = Mathf.Clamp((x - edge0) / (edge1 - edge0), 0.0f, 1.0f);
        return t * t * (3.0f - 2.0f * t);
    }

    private void CreateFaceNodes()
    {
        for (int i = 0; i < FaceDirections.Length; i++)
        {
            Node3D faceRoot = new Node3D { Name = $"Face_{i}" };
            AddChild(faceRoot);

            MeshInstance3D meshInstance = new MeshInstance3D();
            faceRoot.AddChild(meshInstance);

            StaticBody3D body = new StaticBody3D();
            faceRoot.AddChild(body);

            CollisionShape3D collisionShape = new CollisionShape3D();
            body.AddChild(collisionShape);

            _faces[i] = new FaceRuntime
            {
                MeshInstance = meshInstance,
                CollisionShape = collisionShape,
            };
        }
    }

    private void EnsureAtmosphereNode()
    {
        if (!EnableAtmosphere)
        {
            return;
        }

        _atmosphereMesh = GetNodeOrNull<MeshInstance3D>("Atmosphere");
        if (_atmosphereMesh == null)
        {
            _atmosphereMesh = new MeshInstance3D
            {
                Name = "Atmosphere",
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            };
            AddChild(_atmosphereMesh);
        }

        SphereMesh shell = new SphereMesh
        {
            Radius = 1.0f,
            Height = 2.0f,
            RadialSegments = 96,
            Rings = 48,
        };
        _atmosphereMesh.Mesh = shell;

        if (_atmosphereMaterial == null)
        {
            Shader shader = GD.Load<Shader>("res://Shaders/planet_atmosphere.gdshader");
            _atmosphereMaterial = new ShaderMaterial
            {
                Shader = shader,
            };
        }

        _atmosphereMesh.MaterialOverride = _atmosphereMaterial;
        _sunLight = ResolveSunLight();
    }

    private void UpdateAtmosphereFromComposition()
    {
        if (!EnableAtmosphere || _atmosphereMesh == null || _atmosphereMaterial == null)
        {
            return;
        }

        float o2 = Mathf.Max(0.0f, AtmosphereO2);
        float ch4 = Mathf.Max(0.0f, AtmosphereCH4);
        float o3 = Mathf.Max(0.0f, AtmosphereO3);
        float h2o = Mathf.Max(0.0f, AtmosphereH2O);
        float co2 = Mathf.Max(0.0f, AtmosphereCO2);
        float h2 = Mathf.Max(0.0f, AtmosphereH2);
        float he = Mathf.Max(0.0f, AtmosphereHe);
        float n2 = Mathf.Max(0.0f, AtmosphereN2);

        float total = o2 + ch4 + o3 + h2o + co2 + h2 + he + n2;
        if (total <= 0.00001f)
        {
            _atmosphereMesh.Visible = false;
            return;
        }

        _atmosphereMesh.Visible = true;

        float surfaceRadius = EstimateSurfaceOuterRadius();

        float atmosphereRadius;
        if (AtmosphereRadiusMultiplier > 1.0f)
        {
            atmosphereRadius = surfaceRadius * AtmosphereRadiusMultiplier;
        }
        else
        {
            float autoThickness = EstimateAtmosphereThickness(surfaceRadius, o2, ch4, o3, h2o, co2, h2, he, n2, total);
            atmosphereRadius = surfaceRadius + Mathf.Max(0.0f, AtmosphereBaseOffset) + autoThickness;
        }

        _atmosphereMesh.Scale = Vector3.One * atmosphereRadius;

        Vector3 rayleigh = ComputeRayleighColor(o2, ch4, o3, h2o, co2, h2, he, n2, total);
        Vector3 mie = ComputeMieColor(o2, h2o, co2, ch4, total);
        Vector3 ozoneAbsorb = ComputeOzoneAbsorption(o3, total);

        _atmosphereMaterial.SetShaderParameter("planet_radius", surfaceRadius);
        _atmosphereMaterial.SetShaderParameter("atmosphere_radius", atmosphereRadius);
        _atmosphereMaterial.SetShaderParameter("atmosphere_density", AtmosphereDensity);
        _atmosphereMaterial.SetShaderParameter("sunset_strength", SunsetStrength);
        _atmosphereMaterial.SetShaderParameter("rayleigh_rgb", rayleigh);
        _atmosphereMaterial.SetShaderParameter("mie_rgb", mie);
        _atmosphereMaterial.SetShaderParameter("ozone_absorption_rgb", ozoneAbsorb);
        _atmosphereMaterial.SetShaderParameter("planet_center_ws", GlobalPosition);
    }

    private float EstimateSurfaceOuterRadius()
    {
        if (_observedMaxElevation > 0.0f)
        {
            float meanPositive = _observedPositiveElevationCount > 0
                ? _observedPositiveElevationSum / _observedPositiveElevationCount
                : _observedMaxElevation;

            // Use a representative outer radius to keep the shell close to the terrain
            // instead of following a single outlier peak.
            float representative = Mathf.Lerp(meanPositive, _observedMaxElevation, 0.35f);
            return Radius + representative + 0.5f;
        }

        float fallback = Mathf.Max(0.0f, MaxHeight) * Mathf.Max(0.0f, HeightScale);
        return Radius + fallback + 0.5f;
    }

    private void UpdateAtmosphereRuntime()
    {
        if (!EnableAtmosphere || _atmosphereMaterial == null)
        {
            return;
        }

        if (_sunLight == null)
        {
            _sunLight = ResolveSunLight();
        }

        Vector3 sunDirection = _sunLight != null
            ? _sunLight.GlobalTransform.Basis.Z.Normalized()
            : (Vector3.Up + Vector3.Forward * 0.25f).Normalized();

        _atmosphereMaterial.SetShaderParameter("planet_center_ws", GlobalPosition);
        _atmosphereMaterial.SetShaderParameter("sun_dir_ws", sunDirection);
    }

    private DirectionalLight3D ResolveSunLight()
    {
        if (SunLightPath != null && !SunLightPath.IsEmpty)
        {
            DirectionalLight3D assigned = GetNodeOrNull<DirectionalLight3D>(SunLightPath);
            if (assigned != null)
            {
                return assigned;
            }
        }

        Node sceneRoot = GetTree()?.CurrentScene;
        if (sceneRoot == null)
        {
            return null;
        }

        return FindDirectionalLightRecursive(sceneRoot);
    }

    private static DirectionalLight3D FindDirectionalLightRecursive(Node root)
    {
        if (root is DirectionalLight3D directional)
        {
            return directional;
        }

        for (int i = 0; i < root.GetChildCount(); i++)
        {
            DirectionalLight3D found = FindDirectionalLightRecursive(root.GetChild(i));
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private float EstimateAtmosphereThickness(float surfaceRadius, float o2, float ch4, float o3, float h2o, float co2, float h2, float he, float n2, float total)
    {
        float lightGasFraction = (h2 + he) / total;
        float heavyGasFraction = (co2 + o3 + h2o) / total;

        float thicknessRatio = 0.012f;
        thicknessRatio += 0.050f * lightGasFraction;
        thicknessRatio += 0.006f * Mathf.Sqrt(Mathf.Clamp(total / 100.0f, 0.0f, 3.0f));
        thicknessRatio += 0.004f * heavyGasFraction;

        float rawThickness = surfaceRadius * thicknessRatio;
        float minThickness = Mathf.Max(0.5f, AtmosphereMinThickness);
        float maxThickness = Mathf.Max(minThickness, AtmosphereMaxThickness);

        return Mathf.Clamp(rawThickness, minThickness, maxThickness);
    }

    private static Vector3 ComputeRayleighColor(float o2, float ch4, float o3, float h2o, float co2, float h2, float he, float n2, float total)
    {
        float nO2 = o2 / total;
        float nCH4 = ch4 / total;
        float nO3 = o3 / total;
        float nH2O = h2o / total;
        float nCO2 = co2 / total;
        float nH2 = h2 / total;
        float nHe = he / total;
        float nN2 = n2 / total;

        float gasStrength =
            nN2 * 1.00f +
            nO2 * 1.12f +
            nCO2 * 1.55f +
            nH2O * 1.30f +
            nCH4 * 1.18f +
            nO3 * 1.35f +
            nH2 * 0.55f +
            nHe * 0.08f;

        float r = gasStrength / Mathf.Pow(680.0f / 550.0f, 4.0f);
        float g = gasStrength;
        float b = gasStrength / Mathf.Pow(440.0f / 550.0f, 4.0f);

        Vector3 color = new Vector3(r, g, b);
        return color * 0.55f;
    }

    private static Vector3 ComputeMieColor(float o2, float h2o, float co2, float ch4, float total)
    {
        float aerosolProxy = Mathf.Clamp((h2o * 1.2f + co2 * 0.35f + ch4 * 0.35f + o2 * 0.05f) / Mathf.Max(0.0001f, total), 0.0f, 1.0f);
        float baseStrength = 0.03f + aerosolProxy * 0.22f;
        return new Vector3(baseStrength, baseStrength * 0.98f, baseStrength * 0.92f);
    }

    private static Vector3 ComputeOzoneAbsorption(float o3, float total)
    {
        float ratio = Mathf.Clamp(o3 / Mathf.Max(total, 0.0001f), 0.0f, 1.0f);
        return new Vector3(0.08f, 0.20f, 0.55f) * ratio * 8.0f;
    }
}
