using Godot;
using System.Collections.Generic;

public static class AntiCellCraterGenerator
{
    public readonly struct Options
    {
        public readonly int Seed;
        public readonly int TargetCount;
        public readonly float MinRadiusDeg;
        public readonly float MaxRadiusDeg;
        public readonly float LargeCraterRatio;
        public readonly float DepthScale;
        public readonly float RimHeightScale;
        public readonly float AntiCellFrequency;
        public readonly float AntiCellThreshold;
        public readonly float AntiCellJitter;
        public readonly int CandidateMultiplier;
        public readonly float MinAngularSeparationDeg;

        public Options(
            int seed,
            int targetCount,
            float minRadiusDeg,
            float maxRadiusDeg,
            float largeCraterRatio,
            float depthScale,
            float rimHeightScale,
            float antiCellFrequency,
            float antiCellThreshold,
            float antiCellJitter,
            int candidateMultiplier,
            float minAngularSeparationDeg)
        {
            Seed = seed;
            TargetCount = targetCount;
            MinRadiusDeg = minRadiusDeg;
            MaxRadiusDeg = maxRadiusDeg;
            LargeCraterRatio = largeCraterRatio;
            DepthScale = depthScale;
            RimHeightScale = rimHeightScale;
            AntiCellFrequency = antiCellFrequency;
            AntiCellThreshold = antiCellThreshold;
            AntiCellJitter = antiCellJitter;
            CandidateMultiplier = candidateMultiplier;
            MinAngularSeparationDeg = minAngularSeparationDeg;
        }
    }

    private struct Candidate
    {
        public Vector3 Center;
        public float Score;
        public float RadiusDeg;
        public float Depth;
        public float RimHeight;
    }

    public static List<CraterDescriptor> GenerateOnUnitSphere(Options options)
    {
        var result = new List<CraterDescriptor>();
        int target = Mathf.Max(0, options.TargetCount);
        if (target == 0)
        {
            return result;
        }

        RandomNumberGenerator rng = new RandomNumberGenerator
        {
            Seed = (ulong)Mathf.Max(1, options.Seed),
        };

        FastNoiseLite cellular = new FastNoiseLite
        {
            Seed = options.Seed + 701,
            NoiseType = FastNoiseLite.NoiseTypeEnum.Cellular,
            Frequency = Mathf.Max(0.01f, options.AntiCellFrequency),
            CellularJitter = 1.0f,
        };

        int candidatesCount = Mathf.Max(target, target * Mathf.Max(1, options.CandidateMultiplier));
        var candidates = new List<Candidate>(candidatesCount);

        float minRadius = Mathf.Max(0.05f, options.MinRadiusDeg);
        float maxRadius = Mathf.Max(minRadius + 0.01f, options.MaxRadiusDeg);
        float threshold = Mathf.Clamp(options.AntiCellThreshold, 0.0f, 1.0f);
        float jitter = Mathf.Clamp(options.AntiCellJitter, 0.0f, 1.0f);

        for (int i = 0; i < candidatesCount; i++)
        {
            Vector3 direction = FibonacciSphere(i, candidatesCount);
            if (jitter > 0.0f)
            {
                direction = (direction + RandomUnitVector(rng) * jitter * 0.16f).Normalized();
            }

            float cell = cellular.GetNoise3Dv(direction);
            float cell01 = Mathf.Clamp((cell + 1.0f) * 0.5f, 0.0f, 1.0f);
            float antiCell = 1.0f - cell01;
            if (antiCell < threshold)
            {
                continue;
            }

            float score = Mathf.Pow((antiCell - threshold) / Mathf.Max(0.001f, 1.0f - threshold), 1.35f);
            bool large = rng.Randf() < Mathf.Clamp(options.LargeCraterRatio, 0.0f, 1.0f);

            float sizePick = rng.Randf();
            float sizeBias = large ? Mathf.Pow(sizePick, 0.42f) : Mathf.Pow(sizePick, 1.55f);
            float radiusDeg = Mathf.Lerp(minRadius, maxRadius, sizeBias);
            radiusDeg *= Mathf.Lerp(0.82f, 1.18f, score);

            float sizeT = Mathf.Clamp((radiusDeg - minRadius) / Mathf.Max(0.001f, maxRadius - minRadius), 0.0f, 1.0f);
            float depth = Mathf.Lerp(0.025f, 0.20f, sizeT) * options.DepthScale;
            depth *= Mathf.Lerp(0.85f, 1.2f, score);
            float rim = depth * Mathf.Lerp(0.55f, 1.35f, sizeT) * options.RimHeightScale;

            candidates.Add(new Candidate
            {
                Center = direction,
                Score = score,
                RadiusDeg = radiusDeg,
                Depth = depth,
                RimHeight = rim,
            });
        }

        candidates.Sort((a, b) => b.Score.CompareTo(a.Score));

        float minSepRad = Mathf.DegToRad(Mathf.Max(0.1f, options.MinAngularSeparationDeg));
        for (int i = 0; i < candidates.Count && result.Count < target; i++)
        {
            Candidate cand = candidates[i];
            float candRad = Mathf.DegToRad(cand.RadiusDeg);
            bool overlaps = false;

            for (int j = 0; j < result.Count; j++)
            {
                CraterDescriptor existing = result[j];
                float existingRad = 2.0f * Mathf.Asin(Mathf.Clamp(existing.RadiusChord * 0.5f, 0.0f, 1.0f));
                float sep = minSepRad + candRad * 0.45f + existingRad * 0.45f;
                float cosSep = Mathf.Cos(sep);
                float dot = cand.Center.Dot(existing.Center);
                if (dot > cosSep)
                {
                    overlaps = true;
                    break;
                }
            }

            if (overlaps)
            {
                continue;
            }

            result.Add(new CraterDescriptor(
                cand.Center,
                RadiusDegToChord(cand.RadiusDeg),
                cand.Depth,
                cand.RimHeight));
        }

        while (result.Count < target)
        {
            float radiusDeg = Mathf.Lerp(minRadius, maxRadius, rng.Randf());
            float sizeT = Mathf.Clamp((radiusDeg - minRadius) / Mathf.Max(0.001f, maxRadius - minRadius), 0.0f, 1.0f);
            float depth = Mathf.Lerp(0.025f, 0.16f, sizeT) * options.DepthScale;
            float rim = depth * Mathf.Lerp(0.6f, 1.2f, sizeT) * options.RimHeightScale;

            result.Add(new CraterDescriptor(
                RandomUnitVector(rng),
                RadiusDegToChord(radiusDeg),
                depth,
                rim));
        }

        return result;
    }

    private static float RadiusDegToChord(float radiusDeg)
    {
        float radiusRad = Mathf.DegToRad(Mathf.Max(0.01f, radiusDeg));
        return 2.0f * Mathf.Sin(radiusRad * 0.5f);
    }

    private static Vector3 FibonacciSphere(int index, int total)
    {
        if (total <= 1)
        {
            return Vector3.Up;
        }

        float t = index / (float)(total - 1);
        float y = 1.0f - 2.0f * t;
        float radius = Mathf.Sqrt(Mathf.Max(0.0f, 1.0f - y * y));
        float goldenAngle = Mathf.Pi * (3.0f - Mathf.Sqrt(5.0f));
        float theta = index * goldenAngle;
        return new Vector3(Mathf.Cos(theta) * radius, y, Mathf.Sin(theta) * radius);
    }

    private static Vector3 RandomUnitVector(RandomNumberGenerator rng)
    {
        float z = rng.RandfRange(-1.0f, 1.0f);
        float a = rng.RandfRange(0.0f, Mathf.Tau);
        float r = Mathf.Sqrt(Mathf.Max(0.0f, 1.0f - z * z));
        return new Vector3(r * Mathf.Cos(a), z, r * Mathf.Sin(a));
    }
}
