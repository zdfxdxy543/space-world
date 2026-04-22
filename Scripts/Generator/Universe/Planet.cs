using System;
using System.Collections.Generic;
using System.Linq;

public class BasePlanetData
{
    public string name;
    public float orbit;
    public float mass;
    public float radius;
    public float density;
    public float surfaceGravity;
    // ���²���������̬������
    public float orbitPeriod;
    public float rotationPeriod;
    public float obliquity;
    public float blackBodyTemperature;
    public int M_number;
    public WaterType waterType;
    public float waterCoverage;
    public GreenHouse greenHouse = GreenHouse.None;
    public LithosphereType lithosphereType;
    public PlateTectonics plateTectonics = PlateTectonics.None;
    public MagneticField magneticField;
    public float atmosphereRetentionFactor = 0f;
    public float atmosphereH2 = 0f;
    public float atmosphereHe = 0f;
    public float atmosphereN2 = 0f;
    public AtmosphereType atmosphereType;
    public float albedo;
    public float atmosphereCO2 = 0f;
    public bool activeCarbonateSilicateCycle = false;
    public Dictionary<Life, float> lifeHistory = new Dictionary<Life, float>();
    public float atmosphereMO2 = 0f;
    public float atmosphereCH4 = 0f;
    public float atmosphereO3 = 0f;
    public float atmosphereH2O = 0f;
    public float averageTemperature = 0f;
    public float totalAtmosphereMass = 0f;
    public float atmospherePressure = 0f;
    public bool availableBreath = false;
    public float scaleHeight = 0f;
}

public class PlanetData : BasePlanetData
{
    public PlanetFormation formation;
    public PlanetType type;
    public float eccentricity;
    public List<MoonData> moons = new List<MoonData>();

    public float dayLength;
}

public class MoonData : BasePlanetData
{
    public MoonType moonType;

    public float moonDayLength;
    public float monthLength;
}

public class PlanetPart
{
    private static readonly Random _random = new Random();
    public float diskMassFactor;
    public int diskMassModifier;
    public float planetesimalMass;
    public float planetaryMassFactor;
    public float innerEdge;
    public float iceLine;
    public float slowLine;
    public int firstOrbit = 0;
    public List<float> orbitLine = new List<float>();
    public List<PlanetFormation> planetFormations = new List<PlanetFormation>();
    public List<PlanetData> planets = new List<PlanetData>();
    public int haveTack = 0;

    public PlanetPart(string name, StarPart starPart)
    {
        GeneratePlanetaryDisk(starPart);
        GenerateDiskInstability();
        DealCoreAccretion();
        DealOligarchicCollision();
        PlanetaryMass(starPart);
        GetOrbitalEccentricity();
        GetPhysicalParameters();
        GetNaturalSatellites(starPart);
        GetOrbitalPeriod(starPart);
        GetRotationPeriod(starPart);
        GetObliquity();
        GetLocalCalendar();
        GetBlackBodyTemperature(starPart);
        GetWater();
        GetGeophysics(starPart);
        GetMagneticField();
        GetEarlyAtmosphere();
        Albedo();
        GetCarbonDioxide();
        GetLife(starPart);
        GetAverageSurfaceTemperature();
        GetFinalAtmosphere();
    }

    /// <summary>
    ///  ��������ԭʼ������
    /// </summary>
    /// <param name="starPart"></param>
    private void GeneratePlanetaryDisk(StarPart starPart)
    {
        float[] diskMassFactors = new float[16] { 0.25f, 0.32f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 1.0f, 1.0f, 1.2f, 1.4f, 1.7f, 2.0f, 2.5f, 3.2f, 4.0f };
        int[] diskMassModifiers = new int[16] { -6, -5, -4, -3, -2, -1, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6 };
        float[] orbitFactors = new float[16] { 0.6f, 0.8f, 1.2f, 1.8f, 2.7f, 4.0f, 6.0f, 9.0f, 13.5f, 20.0f, 30.0f, 45.0f, 68.0f, 100.0f, 150.0f, 220.0f };
        int diskMassFactorIndex = _random.Next(3, 19) - 3;
        this.diskMassFactor = diskMassFactors[diskMassFactorIndex];
        this.diskMassModifier = diskMassModifiers[diskMassFactorIndex];
        this.planetesimalMass = diskMassFactors[diskMassFactorIndex] * starPart.allStars[0].mass * starPart.metallicity;
        this.innerEdge = _random.Next(2, 13) * 0.005f * MathX.Pow(starPart.allStars[0].mass, 0.33f);
        this.iceLine = 4 * MathX.Sqrt(starPart.allStars[0].initialLuminosity);
        this.slowLine = 20 * MathX.Pow(starPart.allStars[0].mass, 0.33f);
        this.orbitLine.Add(this.innerEdge);
        this.planetFormations.Add(PlanetFormation.Normal);
        foreach (float orbitFactor in orbitFactors)
        {
            float orbit = orbitFactor * MathX.Sqrt(starPart.allStars[0].initialLuminosity);
            if (orbit <= this.orbitLine[0])
            {
                this.firstOrbit++;
            }
            this.orbitLine.Add(orbit);
            if (orbit > this.slowLine)
            {
                this.planetFormations.Add(PlanetFormation.SlowAccretion);
            }
            else
            {
                this.planetFormations.Add(PlanetFormation.Normal);
            }
        }
    }

    /// <summary>
    /// ���������̲��ȶ���������
    /// </summary>
    private void GenerateDiskInstability()
    {
        if (_random.Next(3, 19) + this.diskMassModifier >= 12)
        {
            // �̲��ȶ���
            int instabilityIndex = _random.Next(3, 19) + this.diskMassModifier;
            if (instabilityIndex < 16)
            {
                int firstRoll = _random.Next(3, 19) + this.diskMassModifier;
                int secondRoll = _random.Next(3, 19) + this.diskMassModifier;
                int instabilityCount = 0;
                if (secondRoll < 12)
                {
                    instabilityCount = 1;
                }
                else if (secondRoll < 16)
                {
                    instabilityCount = (secondRoll - 8) / 2;
                }
                else
                {
                    instabilityCount = 4;
                }

                int firstFormationOrbit = 0;
                if (firstRoll < 16)
                {
                    firstFormationOrbit = 13 - MathX.Max((firstRoll - 4), 0) / 2;
                }
                else
                {
                    firstFormationOrbit = 7;
                }
                for (int i = 0; i < instabilityCount; i++)
                {
                    if (firstFormationOrbit - i - this.firstOrbit >= 0)
                    {
                        this.planetFormations[firstFormationOrbit - i - this.firstOrbit] = PlanetFormation.DiskInstability;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// ��������������������
    /// </summary>
    private void DealCoreAccretion()
    {
        int formationCount = 0;
        if (this.planetesimalMass > 0.11 && this.planetesimalMass < 0.17)
        {
            formationCount = 1;
        }
        else if (this.planetesimalMass >= 0.17 && this.planetesimalMass < 0.24)
        {
            formationCount = 2;
        }
        else if (this.planetesimalMass >= 0.23 && this.planetesimalMass < 0.29)
        {
            formationCount = 16;
        }
        int availableFormations = 0;
        for (int i = 6; i < 17; i++)
        {
            if (this.planetFormations[i] == PlanetFormation.DiskInstability || this.planetFormations[i] == PlanetFormation.SlowAccretion)
            {
                break;
            }
            availableFormations++;
        }
        int formationIndex = _random.Next(3, 19);
        if (formationCount < 6)
        {
            availableFormations = MathX.Max(availableFormations - 2, 0);
        }
        else if (formationCount < 9)
        {
            availableFormations = MathX.Max(availableFormations - 1, 0);
        }
        else if (formationCount < 13)
        {
            // pass
        }
        else if (formationCount < 16)
        {
            availableFormations = availableFormations + 1;
        }
        else
        {
            availableFormations = availableFormations + 2;
        }
        int finalFormationCount = MathX.Min(formationCount, availableFormations);
        int migrateIndex = _random.Next(3, 19) + this.diskMassModifier;
        int finalOrbit = 6;
        if (migrateIndex >= 9 && migrateIndex < 12)
        {
            finalOrbit = 5;
            this.planetaryMassFactor = 0.75f;
        }
        else if (migrateIndex < 14)
        {
            finalOrbit = 4;
            this.planetaryMassFactor = 0.5f;
        }
        else if (migrateIndex == 14)
        {
            finalOrbit = 3;
            this.planetaryMassFactor = 0.25f;
        }
        else if (migrateIndex == 15)
        {
            finalOrbit = 2;
            this.planetaryMassFactor = 0.25f;
        }
        else if (migrateIndex == 16)
        {
            finalOrbit = 1;
            this.planetaryMassFactor = 0.25f;
        }
        else
        {
            finalOrbit = 0;
            this.planetaryMassFactor = 0.25f;
        }
        if (finalOrbit <= this.firstOrbit)
        {
            finalOrbit = 0;
        }
        for (int i = finalOrbit; i < 7; i++)
        {
            this.planetFormations[i] = PlanetFormation.Depleted;
        }
        if (finalFormationCount >= 2)
        {
            int grandTackCheckIndex = _random.Next(3, 19) + this.diskMassModifier;
            if (grandTackCheckIndex >= 12)
            {
                int grandTackIndex = _random.Next(3, 19);
                int moveCount = 0;
                if (grandTackIndex < 9)
                {
                    moveCount = 1;
                }
                else if (grandTackIndex < 17)
                {
                    moveCount = 2;
                }
                else
                {
                    moveCount = 3;
                }
                for (int i = 0; i < moveCount; i++)
                {
                    if (finalOrbit + 1 <= 7 && this.planetFormations[finalOrbit + 1] != PlanetFormation.DiskInstability)
                    {
                        finalOrbit++;
                    }
                    else
                    {
                        break;
                    }
                }
                this.haveTack = 1;
            }
        }
        for (int i = 0; i < finalFormationCount; i++)
        {
            if (this.planetFormations[i] != PlanetFormation.DiskInstability)
            {
                if (this.orbitLine[i] > this.slowLine)
                {
                    int slowAccretionCheck = _random.Next(3, 19);
                    if (slowAccretionCheck < 12)
                    {
                        continue;
                    }
                }
                this.planetFormations[i] = PlanetFormation.CoreAccretion;
            }
        }
    }

    /// <summary>
    /// ������ͷ��ײ��������
    /// </summary>
    private void DealOligarchicCollision()
    {
        if (this.planetFormations[0] == PlanetFormation.CoreAccretion || this.planetFormations[this.firstOrbit + 1] == PlanetFormation.CoreAccretion)
        {
            return;
        }
        int collisionCount = 0;
        for (int i = this.firstOrbit + 1; i < 6; i++)
        {
            if (this.planetFormations[i] == PlanetFormation.CoreAccretion)
            {
                break;
            }
            collisionCount++;
        }
        int collisionIndex = _random.Next(3, 19);
        int tightenPlanet = 0;
        if (collisionIndex < 6)
        {
            collisionCount = MathX.Max(collisionCount - 2, 0);
        }
        else if (collisionIndex < 9)
        {
            collisionCount = MathX.Max(collisionCount - 1, 0);
        }
        else if (collisionIndex < 13)
        {
            // pass
        }
        else if (collisionIndex < 16)
        {
            collisionCount = collisionCount + 1;
            tightenPlanet = 1;
        }
        else
        {
            collisionCount = collisionCount + 2;
            tightenPlanet = 1;
        }
        if (tightenPlanet == 1)
        {
            this.planetFormations[0] = PlanetFormation.OligarchicCollision;
            collisionCount--;
        }
        for (int i = 0; i < collisionCount; i++)
        {
            if (this.planetFormations[this.firstOrbit + 1 + i] != PlanetFormation.CoreAccretion)
            {
                if (this.planetFormations[this.firstOrbit + 1 + i] == PlanetFormation.Depleted)
                {
                    this.planetFormations[this.firstOrbit + 1 + i] = PlanetFormation.OligarchicCollisionAndDepleted;
                }
                else
                {
                    this.planetFormations[this.firstOrbit + 1 + i] = PlanetFormation.OligarchicCollision;
                }
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// ������������������
    /// </summary>
    /// <param name="starPart"></param>
    private void PlanetaryMass(StarPart starPart)
    {
        for (int i = 0; i < 17; i++)
        {
            if (this.planetFormations[i] == PlanetFormation.OligarchicCollisionAndDepleted || this.planetFormations[i] == PlanetFormation.DiskInstability ||
                this.planetFormations[i] == PlanetFormation.CoreAccretion || this.planetFormations[i] == PlanetFormation.OligarchicCollision)
            {
                PlanetData newPlanet = new PlanetData();
                newPlanet.name = starPart.allStars[0].name + "_" + (char)('a' + this.planets.Count);
                newPlanet.orbit = this.orbitLine[i];
                newPlanet.formation = this.planetFormations[i];
                if (this.planetFormations[i] == PlanetFormation.DiskInstability)
                {
                    if (starPart.allStars[0].mass <= 0.5)
                    {
                        newPlanet.mass = _random.Next(3, 19) * starPart.allStars[0].mass * this.diskMassFactor;
                    }
                    else
                    {
                        newPlanet.mass = _random.Next(3, 19) * (100 * starPart.allStars[0].mass * this.diskMassFactor - 38);
                    }
                    newPlanet.type = PlanetType.Gas;
                }
                else if (this.planetFormations[i] == PlanetFormation.CoreAccretion)
                {
                    int K = 0;
                    if (i > 0 && this.planetFormations[i - 1] != PlanetFormation.CoreAccretion) K = 20;
                    else if (i > 1 && this.planetFormations[i - 1] == PlanetFormation.CoreAccretion && this.planetFormations[i - 2] != PlanetFormation.CoreAccretion) K = 10;
                    else K = 4;
                    float coreMass = (_random.Next(3, 19) - K) * this.planetesimalMass;
                    if (coreMass < 5)
                    {
                        newPlanet.mass = coreMass;
                        newPlanet.type = PlanetType.Terra;
                    }
                    else
                    {
                        float[] innerPlanetMass = { 5.0f, 6.0f, 7.5f, 10.0f, 10.0f, 12.0f, 15.0f, 20.0f };
                        float[] nextInnerPlanetMass = { 2.5f, 3.0f, 4.0f, 5.0f, 5.0f, 6.0f, 7.5f, 10.0f };
                        float[] otherPlanetMass = { 1.1f, 1.1f, 1.1f, 1.2f, 1.2f, 1.3f, 1.5f, 2.0f };
                        int planetMassindex = _random.Next(3, 19);
                        if (K == 20)
                        {
                            newPlanet.mass = coreMass * innerPlanetMass[(planetMassindex - 1) / 2 - 1];
                        }
                        else if (K == 10)
                        {
                            newPlanet.mass = coreMass * nextInnerPlanetMass[(planetMassindex - 1) / 2 - 1];
                        }
                        else
                        {
                            newPlanet.mass = coreMass * otherPlanetMass[(planetMassindex - 1) / 2 - 1];
                        }
                        newPlanet.type = PlanetType.Gas;
                    }
                }
                else if (this.planetFormations[i] == PlanetFormation.OligarchicCollision || this.planetFormations[i] == PlanetFormation.OligarchicCollisionAndDepleted)
                {
                    int count = this.planetFormations.Count(num => num == PlanetFormation.OligarchicCollision || num == PlanetFormation.OligarchicCollisionAndDepleted);
                    int check = _random.Next(3, 19);
                    if (count > 1 && check >= 12 && (i == 0 || (i > 0 && (this.planetFormations[i - 1] != PlanetFormation.OligarchicCollision || this.planetFormations[i - 1] != PlanetFormation.OligarchicCollisionAndDepleted))))
                    {
                        newPlanet.type = PlanetType.LeftoverTerra;
                    }
                    else
                    {
                        newPlanet.type = PlanetType.Terra;
                    }
                    if (newPlanet.type != PlanetType.LeftoverTerra)
                    {
                        newPlanet.mass = _random.Next(3, 19) / 5.0f * this.planetesimalMass;
                        if (this.planetFormations.Count(num => num == PlanetFormation.CoreAccretion) > 0)
                        {
                            newPlanet.mass = newPlanet.mass * this.planetaryMassFactor;
                        }
                        if (this.planetFormations[i] == PlanetFormation.OligarchicCollisionAndDepleted)
                        {
                            newPlanet.mass = newPlanet.mass * 0.1f;
                        }
                        if (newPlanet.mass > 0.18)
                        {
                            newPlanet.type = PlanetType.Terra;
                        }
                        else if (i + 1 < this.planetFormations.Count && this.planetFormations[i + 1] == PlanetFormation.CoreAccretion)
                        {
                            newPlanet.type = PlanetType.PlanetoidBelt;
                            newPlanet.mass = 0;
                        }
                    }
                    else
                    {
                        newPlanet.mass = _random.Next(3, 19) * 0.01f;
                    }
                }
                this.planets.Add(newPlanet);
            }
        }
    }

    /// <summary>
    /// �������ǹ��ƫ����?
    /// </summary>
    private void GetOrbitalEccentricity()
    {
        int planetCount = this.planets.Count;
        float eccentricityFactor = 0;
        if (planetCount < 3)
        {
            eccentricityFactor = 0.23f;
        }
        else if (planetCount < 4)
        {
            eccentricityFactor = 0.15f;
        }
        else if (planetCount < 5)
        {
            eccentricityFactor = 0.12f;
        }
        else if (planetCount < 6)
        {
            eccentricityFactor = 0.1f;
        }
        else if (planetCount < 7)
        {
            eccentricityFactor = 0.08f;
        }
        else if (planetCount < 8)
        {
            eccentricityFactor = 0.07f;
        }
        else if (planetCount < 10)
        {
            eccentricityFactor = 0.06f;
        }
        else
        {
            eccentricityFactor = 0.05f;
        }
        for (int i = 0; i < this.planets.Count; i++)
        {
            if (this.planets[i].type == PlanetType.PlanetoidBelt)
            {
                this.planets[i].eccentricity = 0;
            }
            else
            {
                this.planets[i].eccentricity = eccentricityFactor;
            }
        }
    }

    /// <summary>
    /// ����������������
    /// </summary>
    private void GetPhysicalParameters()
    {
        // �ܶ�
        for (int i = 0; i < this.planets.Count; i++)
        {
            if (this.planets[i].type == PlanetType.Gas)
            {
                if (this.planets[i].mass <= 200)
                {
                    this.planets[i].density = 1 / MathX.Sqrt(this.planets[i].mass);
                }
                else
                {
                    this.planets[i].density = MathX.Pow(this.planets[i].mass, 1.27f) / 11800;
                }
            }
            else if (this.planets[i].type == PlanetType.Terra || this.planets[i].type == PlanetType.LeftoverTerra)
            {
                this.planets[i].density = MathX.Pow(this.planets[i].mass, 0.2f);
                this.planets[i].density = (_random.Next(3, 19) - 10) * 0.01f + this.planets[i].density;
                if (this.planets[i].type == PlanetType.LeftoverTerra)
                {
                    int impactIndex = _random.Next(1, 7);
                    if (impactIndex == 5 || impactIndex == 6)
                    {
                        this.planets[i].density += 0.4f;
                    }
                }
                if (this.planets[i].type == PlanetType.Terra && this.planets[i].formation == PlanetFormation.CoreAccretion)
                {
                    this.planets[i].density -= 0.1f;
                }
                if (this.planets[i].density < 0.18f) this.planets[i].density = 0.18f;
                if (this.planets[i].density > 1.43f) this.planets[i].density = 1.43f;
            }
            else
            {
                this.planets[i].density = 0;
            }

            // Radius & Surface Gravity
            if (this.planets[i].type != PlanetType.PlanetoidBelt)
            {
                this.planets[i].radius = 6370 * MathX.Pow((this.planets[i].mass / this.planets[i].density), 0.33f);
                this.planets[i].surfaceGravity = MathX.Pow((this.planets[i].mass * this.planets[i].density * this.planets[i].density), 0.33f);
            }
            else
            {
                this.planets[i].radius = 0;
            }
        }
    }

    /// <summary>
    /// ������Ȼ����
    /// </summary>
    /// <param name="starPart">������Ϣ</param>
    private void GetNaturalSatellites(StarPart starPart)
    {
        float[] orbitalRatio = { 1.406f, 1.432f, 1.452f, 1.480f, 1.50f, 1.55f, 1.587f, 1.587f, 1.587f, 1.587f, 1.60f, 1.65f, 1.70f, 1.75f, 1.80f, 1.85f };
        for (int i = 0; i < this.planets.Count; i++)
        {
            float hillRadius = 2.17f * MathX.Pow(10, 6) * (this.planets[i].orbit * (1 - this.planets[i].eccentricity)) * MathX.Pow((this.planets[i].mass / starPart.allStars[0].mass), 0.33f);
            // 1. ����
            float M = 2 * MathX.Pow(10, -15) * hillRadius * hillRadius / MathX.Sqrt(this.planets[i].orbit);
            int N = (int)MathX.Floor(2 * MathX.Pow(10, -15) * hillRadius * hillRadius / MathX.Sqrt(this.planets[i].orbit));
            N = MathX.Min(N, 8);
            //Debug.Log(M);
            //Debug.Log(N);
            for (int j = 0; j < N; j++)
            {
                MoonData newMoon = new MoonData();
                if (j == 0)
                {
                    newMoon.orbit = (_random.Next(1, 7) + 2) * this.planets[i].radius;
                }
                else
                {
                    newMoon.orbit = orbitalRatio[_random.Next(0, 16)] * this.planets[i].moons[j - 1].orbit;
                }
                newMoon.mass = MathX.Pow(10, -5) * _random.Next(3, 19) * this.planets[i].mass / N;
                newMoon.density = MathX.Pow(newMoon.mass, 0.2f);
                if (this.planets[i].orbit < this.iceLine || (this.planets[i].type == PlanetType.Gas && this.planets[i].mass >= 200 && newMoon.orbit <= 600000))
                {
                    newMoon.moonType = MoonType.Rocky;
                    newMoon.density = (_random.Next(3, 19) + 10) * 0.01f + newMoon.density;
                }
                else
                {
                    newMoon.moonType = MoonType.Icy;
                    newMoon.density = (_random.Next(3, 19) - 20) * 0.01f + newMoon.density;
                }
                if (newMoon.density < 0.18f) newMoon.density = 0.18f;
                else if (newMoon.density > 1.43f) newMoon.density = 1.43f;
                newMoon.radius = 6370 * MathX.Pow((newMoon.mass / newMoon.density), 0.33f);
                newMoon.surfaceGravity = MathX.Pow((newMoon.mass * newMoon.density * newMoon.density), 0.33f);
                newMoon.name = this.planets[i].name + "_" + (this.planets[i].moons.Count + 1);
                this.planets[i].moons.Add(newMoon);
            }

            // 2. ײ��
            if (hillRadius / this.planets[i].radius >= 300)
            {
                int impactindex = _random.Next(1, 7);
                if (impactindex == 5 || impactindex == 6)
                {
                    MoonData newMoon = new MoonData();
                    newMoon.orbit = (_random.Next(3, 19) + 7) * 4 * this.planets[i].radius;
                    newMoon.mass = 0.001f * _random.Next(3, 19) * this.planets[i].mass;
                    newMoon.moonType = MoonType.Rocky;
                    newMoon.density = (_random.Next(3, 19) + 10) * 0.01f + MathX.Pow(newMoon.mass, 0.2f);
                    if (newMoon.density < 0.18f) newMoon.density = 0.18f;
                    else if (newMoon.density > 1.43f) newMoon.density = 1.43f;
                    newMoon.radius = 6370 * MathX.Pow((newMoon.mass / newMoon.density), 0.33f);
                    newMoon.surfaceGravity = MathX.Pow((newMoon.mass * newMoon.density * newMoon.density), 0.33f);
                    newMoon.name = this.planets[i].name + "_" + (this.planets[i].moons.Count + 1);
                    this.planets[i].moons.Add(newMoon);
                }
            }

            // 3. С����
            if (this.planets[i].moons.Count == 0 && hillRadius / this.planets[i].radius >= 300)
            {
                int moonletIndex = _random.Next(1, 7);
                if (moonletIndex >= 4)
                {
                    int moonletCount = _random.Next(1, 7) - 3;
                    if (moonletCount < 1) moonletCount = 1;
                    for (int j = 0; j < moonletCount; j++)
                    {
                        MoonData newMoon = new MoonData();
                        if (j == 0)
                        {
                            newMoon.orbit = (_random.Next(1, 7) + 2) * this.planets[i].radius;
                        }
                        else
                        {
                            newMoon.orbit = orbitalRatio[_random.Next(0, 16)] * this.planets[i].moons[j - 1].orbit;
                        }
                        newMoon.mass = MathX.Pow(10, -9) * _random.Next(3, 19) * this.planets[i].mass / moonletCount;
                        newMoon.density = MathX.Pow(newMoon.mass, 0.2f);
                        if (this.planets[i].orbit < this.iceLine || (this.planets[i].type == PlanetType.Gas && this.planets[i].mass >= 200 && newMoon.orbit <= 600000))
                        {
                            newMoon.moonType = MoonType.Rocky;
                            newMoon.density = (_random.Next(3, 19) + 10) * 0.01f + newMoon.density;
                        }
                        else
                        {
                            newMoon.moonType = MoonType.Icy;
                            newMoon.density = (_random.Next(3, 19) - 20) * 0.01f + newMoon.density;
                        }
                        if (newMoon.density < 0.18f) newMoon.density = 0.18f;
                        else if (newMoon.density > 1.43f) newMoon.density = 1.43f;
                        newMoon.radius = 6370 * MathX.Pow((newMoon.mass / newMoon.density), 0.33f);
                        newMoon.surfaceGravity = MathX.Pow((newMoon.mass * newMoon.density * newMoon.density), 0.33f);
                        newMoon.name = this.planets[i].name + "_" + (this.planets[i].moons.Count + 1);
                        this.planets[i].moons.Add(newMoon);
                    }
                }
            }
        }
    }


    /// <summary>
    /// ����������
    /// </summary>
    /// <param name="starPart">���ǲ���</param>
    private void GetOrbitalPeriod(StarPart starPart)
    {
        for (int i = 0; i < this.planets.Count; i++)
        {
            for (int j = 0; j < this.planets[i].moons.Count; j++)
            {
                this.planets[i].moons[j].orbitPeriod = 2.77f * MathX.Pow(10, -6) * MathX.Sqrt(MathX.Pow(this.planets[i].moons[j].orbit, 3) / (this.planets[i].mass + this.planets[i].moons[j].mass));
            }
            this.planets[i].orbitPeriod = 8770f * MathX.Sqrt(MathX.Pow(this.planets[i].orbit, 3) / starPart.allStars[0].mass);
        }
    }

    /// <summary>
    /// ������ת����
    /// </summary>
    /// <param name="starPart">���ǲ���</param>
    private void GetRotationPeriod(StarPart starPart)
    {
        for (int i = 0; i < this.planets.Count; i++)
        {
            for (int j = 0; j < this.planets[i].moons.Count; j++)
            {
                this.planets[i].moons[j].rotationPeriod = this.planets[i].moons[j].orbitPeriod;
            }
            this.planets[i].rotationPeriod = _random.Next(4, 385);
        }
    }

    /// <summary>
    /// ���������?
    /// </summary>
    private void GetObliquity()
    {
        for (int i = 0; i < this.planets.Count; i++)
        {
            for (int j = 0; j < this.planets[i].moons.Count; j++)
            {
                this.planets[i].moons[j].obliquity = _random.Next(3, 19) - 8;
                if (this.planets[i].moons[j].obliquity < 0) this.planets[i].moons[j].obliquity = 0;
            }
            this.planets[i].obliquity = _random.Next(10, 49);
        }
    }

    /// <summary>
    /// ���㱾������
    /// </summary>
    private void GetLocalCalendar()
    {
        for (int i = 0; i < this.planets.Count; i++)
        {
            for (int j = 0; j < this.planets[i].moons.Count; j++)
            {
                this.planets[i].moons[j].moonDayLength = (this.planets[i].moons[j].orbitPeriod * this.planets[i].rotationPeriod) / (this.planets[i].moons[j].orbitPeriod - this.planets[i].rotationPeriod);
                this.planets[i].moons[j].monthLength = (this.planets[i].orbitPeriod * this.planets[i].moons[j].orbitPeriod) / (this.planets[i].orbitPeriod - this.planets[i].moons[j].orbitPeriod);
            }
            this.planets[i].dayLength = (this.planets[i].orbitPeriod * this.planets[i].rotationPeriod) / (this.planets[i].orbitPeriod - this.planets[i].rotationPeriod);
        }
    }

    /// <summary>
    /// ��������¶Ⱥ�M��
    /// </summary>
    /// <param name="starPart">���ǲ���</param>
    private void GetBlackBodyTemperature(StarPart starPart)
    {
        for (int i = 0; i < this.planets.Count; i++)
        {
            for (int j = 0; j < this.planets[i].moons.Count; j++)
            {
                this.planets[i].moons[j].blackBodyTemperature = 278 * MathX.Pow(starPart.allStars[0].luminosity, 0.25f) / MathX.Sqrt(this.planets[i].orbit);
                float M_number = 700000 * this.planets[i].moons[j].blackBodyTemperature / this.planets[i].moons[j].density / this.planets[i].moons[j].radius / this.planets[i].moons[j].radius;
                if (M_number > 1 && M_number < 4 && starPart.allStars[0].starType != StarType.BrownDwarf)
                {
                    M_number = 5;
                }
                this.planets[i].moons[j].M_number = (int)MathX.Ceiling(M_number);
            }
            this.planets[i].blackBodyTemperature = 278 * MathX.Pow(starPart.allStars[0].luminosity, 0.25f) / MathX.Sqrt(this.planets[i].orbit);
            float PlanetM_number = 700000 * this.planets[i].blackBodyTemperature / this.planets[i].density / this.planets[i].radius / this.planets[i].radius;
            if (PlanetM_number > 1 && PlanetM_number < 4 && starPart.allStars[0].starType != StarType.BrownDwarf)
            {
                PlanetM_number = 5;
            }
            this.planets[i].M_number = (int)MathX.Ceiling(PlanetM_number);
        }
    }

    /// <summary>
    /// ����ˮ������
    /// </summary>
    private void GetWater()
    {
        float[] waterCoverages = { 0.01f, 0.02f, 0.03f, 0.05f, 0.075f,
                                   0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.55f,
                                   0.6f, 0.65f, 0.7f, 0.75f, 0.8f,
                                   0.85f, 0.9f, 0.95f, 0.975f };
        for (int i = 0; i < this.planets.Count; i++)
        {
            for (int j = 0; j < this.planets[i].moons.Count; j++)
            {
                if (this.planets[i].moons[j].M_number <= 2)
                {
                    this.planets[i].moons[j].waterCoverage = 1;
                    this.planets[i].moons[j].waterType = WaterType.Massive;
                }
                else if (this.planets[i].moons[j].M_number <= 28)
                {
                    if (this.planets[i].orbit > this.iceLine)
                    {
                        this.planets[i].moons[j].waterType = WaterType.Massive;
                        this.planets[i].moons[j].waterCoverage = 1;
                    }
                    else
                    {
                        int waterIndex = _random.Next(3, 19) - this.planets[i].moons[j].M_number;
                        if (this.planets.Count(planet => planet.formation == PlanetFormation.CoreAccretion && planet.orbit > this.iceLine) > 0 && this.haveTack == 1)
                        {
                            waterIndex += 6;
                        }
                        if (this.planets.Count(planet => planet.orbit > this.slowLine) > 0)
                        {
                            waterIndex += 3;
                        }
                        if (waterIndex < -4)
                        {
                            this.planets[i].moons[j].waterType = WaterType.Trace;
                            this.planets[i].moons[j].waterCoverage = 0;
                        }
                        else if (waterIndex < 0)
                        {
                            this.planets[i].moons[j].waterType = WaterType.Minimal;
                            this.planets[i].moons[j].waterCoverage = 0;
                        }
                        else if (waterIndex < 4)
                        {
                            this.planets[i].moons[j].waterType = WaterType.Minimal;
                            this.planets[i].moons[j].waterCoverage = waterCoverages[waterIndex];
                        }
                        else if (waterIndex < 12)
                        {
                            this.planets[i].moons[j].waterType = WaterType.Moderate;
                            this.planets[i].moons[j].waterCoverage = waterCoverages[waterIndex];
                        }
                        else if (waterIndex < 20)
                        {
                            this.planets[i].moons[j].waterType = WaterType.Extensive;
                            this.planets[i].moons[j].waterCoverage = waterCoverages[waterIndex];
                        }
                        else
                        {
                            this.planets[i].moons[j].waterType = WaterType.Massive;
                            this.planets[i].moons[j].waterCoverage = 1;
                        }
                    }
                }
                else
                {
                    if (this.planets[i].moons[j].blackBodyTemperature >= 125 ||
                        (this.planets[i].type == PlanetType.Gas && this.planets[i].moons[j].moonType == MoonType.Rocky))
                    {
                        this.planets[i].moons[j].waterType = WaterType.Trace;
                        this.planets[i].moons[j].waterCoverage = 0;
                    }
                    else
                    {
                        this.planets[i].moons[j].waterType = WaterType.Massive;
                        this.planets[i].moons[j].waterCoverage = 1;
                    }
                }

                if (this.planets[i].moons[j].M_number > 2 && this.planets[i].moons[j].waterType == WaterType.Minimal &&
                    this.planets[i].moons[j].blackBodyTemperature > 300)
                {
                    float evaporationIndex = _random.Next(3, 19) + this.planets[i].moons[j].blackBodyTemperature;
                    if (evaporationIndex >= 318)
                    {
                        this.planets[i].moons[j].waterType = WaterType.Trace;
                        this.planets[i].moons[j].waterCoverage = 0;
                    }
                }
                if (this.planets[i].moons[j].M_number > 2 && this.planets[i].moons[j].waterType >= WaterType.Moderate &&
                    this.planets[i].moons[j].blackBodyTemperature > 300)
                {
                    float evaporationIndex = _random.Next(3, 19) + this.planets[i].moons[j].blackBodyTemperature;
                    if (evaporationIndex >= 318)
                    {
                        this.planets[i].moons[j].waterType = WaterType.Trace;
                        this.planets[i].moons[j].waterCoverage = 0;
                        this.planets[i].moons[j].greenHouse = GreenHouse.Dry;
                    }
                }
                if (this.planets[i].moons[j].M_number <= 2 && this.planets[i].moons[j].blackBodyTemperature > 140)
                {
                    float evaporationIndex = _random.Next(3, 19) + this.planets[i].moons[j].blackBodyTemperature;
                    if (evaporationIndex >= 158)
                    {
                        this.planets[i].moons[j].greenHouse = GreenHouse.Wet;
                    }
                }
            }
            if (this.planets[i].M_number <= 2)
            {
                this.planets[i].waterCoverage = 1;
                this.planets[i].waterType = WaterType.Massive;
            }
            else if (this.planets[i].M_number <= 28)
            {
                if (this.planets[i].orbit > this.iceLine)
                {
                    this.planets[i].waterType = WaterType.Massive;
                    this.planets[i].waterCoverage = 1;
                }
                else
                {
                    int waterIndex = _random.Next(3, 19) - this.planets[i].M_number;
                    if (this.planets.Count(planet => planet.formation == PlanetFormation.CoreAccretion && planet.orbit > this.iceLine) > 0 && this.haveTack == 1)
                    {
                        waterIndex += 6;
                    }
                    if (this.planets.Count(planet => planet.orbit > this.slowLine) > 0)
                    {
                        waterIndex += 3;
                    }
                    if (waterIndex < -4)
                    {
                        this.planets[i].waterType = WaterType.Trace;
                        this.planets[i].waterCoverage = 0;
                    }
                    else if (waterIndex < 0)
                    {
                        this.planets[i].waterType = WaterType.Minimal;
                        this.planets[i].waterCoverage = 0;
                    }
                    else if (waterIndex < 4)
                    {
                        this.planets[i].waterType = WaterType.Minimal;
                        this.planets[i].waterCoverage = waterCoverages[waterIndex];
                    }
                    else if (waterIndex < 12)
                    {
                        this.planets[i].waterType = WaterType.Moderate;
                        this.planets[i].waterCoverage = waterCoverages[waterIndex];
                    }
                    else if (waterIndex < 20)
                    {
                        this.planets[i].waterType = WaterType.Extensive;
                        this.planets[i].waterCoverage = waterCoverages[waterIndex];
                    }
                    else
                    {
                        this.planets[i].waterType = WaterType.Massive;
                        this.planets[i].waterCoverage = 1;
                    }
                }
            }
            else
            {
                if (this.planets[i].blackBodyTemperature >= 125)
                {
                    this.planets[i].waterType = WaterType.Trace;
                    this.planets[i].waterCoverage = 0;
                }
                else
                {
                    this.planets[i].waterType = WaterType.Massive;
                    this.planets[i].waterCoverage = 1;
                }
            }

            if (this.planets[i].M_number > 2 && this.planets[i].waterType == WaterType.Minimal &&
                this.planets[i].blackBodyTemperature > 300)
            {
                float evaporationIndex = _random.Next(3, 19) + this.planets[i].blackBodyTemperature;
                if (evaporationIndex >= 318)
                {
                    this.planets[i].waterType = WaterType.Trace;
                    this.planets[i].waterCoverage = 0;
                }
            }
            if (this.planets[i].M_number > 2 && this.planets[i].waterType >= WaterType.Moderate &&
                this.planets[i].blackBodyTemperature > 300)
            {
                float evaporationIndex = _random.Next(3, 19) + this.planets[i].blackBodyTemperature;
                if (evaporationIndex >= 318)
                {
                    this.planets[i].waterType = WaterType.Trace;
                    this.planets[i].waterCoverage = 0;
                    this.planets[i].greenHouse = GreenHouse.Dry;
                }
            }
            if (this.planets[i].M_number <= 2 && this.planets[i].blackBodyTemperature > 140)
            {
                float evaporationIndex = _random.Next(3, 19) + this.planets[i].blackBodyTemperature;
                if (evaporationIndex >= 158)
                {
                    this.planets[i].greenHouse = GreenHouse.Wet;
                }
            }
        }
    }

    /// <summary>
    /// ������ʲ���?
    /// </summary>
    /// <param name="starPart">���ǲ���</param>
    private void GetGeophysics(StarPart starPart)
    {
        for (int i = 0; i < this.planets.Count; i++)
        {
            int primordialIndex;
            for (int j = 0; j < this.planets[i].moons.Count; j++)
            {
                primordialIndex = _random.Next(3, 19);
                primordialIndex += (int)MathX.Round(starPart.age / 8);
                primordialIndex += (int)MathX.Round(-60 * MathX.Log10(this.planets[i].moons[j].surfaceGravity));
                primordialIndex += (int)MathX.Round(-10 * MathX.Log10(starPart.metallicity));
                if (primordialIndex < 16)
                {
                    this.planets[i].moons[j].lithosphereType = LithosphereType.Molten;
                }
                else if (primordialIndex < 24)
                {
                    this.planets[i].moons[j].lithosphereType = LithosphereType.Soft;
                }
                else if (primordialIndex < 32)
                {
                    this.planets[i].moons[j].lithosphereType = LithosphereType.EarlyPlate;
                }
                else if (primordialIndex < 64)
                {
                    this.planets[i].moons[j].lithosphereType = LithosphereType.MaturePlate;
                }
                else if (primordialIndex < 88)
                {
                    this.planets[i].moons[j].lithosphereType = LithosphereType.AncientPlate;
                }
                else
                {
                    this.planets[i].moons[j].lithosphereType = LithosphereType.Solid;
                }
                if (this.planets[i].moons[j].lithosphereType == LithosphereType.EarlyPlate || this.planets[i].moons[j].lithosphereType == LithosphereType.MaturePlate ||
                    this.planets[i].moons[j].lithosphereType == LithosphereType.AncientPlate)
                {
                    int tectonicIndex = _random.Next(3, 19);
                    if (this.planets[i].moons[j].waterType == WaterType.Extensive || this.planets[i].moons[j].waterType == WaterType.Massive) tectonicIndex += 6;
                    else if (this.planets[i].moons[j].waterType == WaterType.Minimal || this.planets[i].moons[j].waterType == WaterType.Trace) tectonicIndex -= 6;
                    if (this.planets[i].moons[j].lithosphereType == LithosphereType.EarlyPlate) tectonicIndex += 2;
                    else if (this.planets[i].moons[j].lithosphereType == LithosphereType.AncientPlate) tectonicIndex -= 2;
                    if (tectonicIndex > 10) this.planets[i].moons[j].plateTectonics = PlateTectonics.Mobile;
                    else this.planets[i].moons[j].plateTectonics = PlateTectonics.Fixed;
                }
                if (this.planets[i].moons[j].lithosphereType == LithosphereType.Molten && this.planets[i].moons[j].waterType != WaterType.Massive)
                {
                    this.planets[i].moons[j].waterType = WaterType.Trace;
                    this.planets[i].moons[j].waterCoverage = 0;
                }
                if (this.planets[i].moons[j].waterType == WaterType.Extensive)
                {
                    if (this.planets[i].moons[j].lithosphereType == LithosphereType.Soft || this.planets[i].moons[j].lithosphereType == LithosphereType.Solid)
                    {
                        this.planets[i].moons[j].waterCoverage += (_random.Next(3, 19) + 10) / 100f;
                        if (this.planets[i].moons[j].waterCoverage > 1) this.planets[i].moons[j].waterCoverage = 1;
                    }
                    else if (this.planets[i].moons[j].lithosphereType == LithosphereType.EarlyPlate || this.planets[i].moons[j].lithosphereType == LithosphereType.AncientPlate)
                    {
                        this.planets[i].moons[j].waterCoverage += (_random.Next(3, 19)) / 100f;
                        if (this.planets[i].moons[j].waterCoverage > 1) this.planets[i].moons[j].waterCoverage = 1;
                    }
                }
            }
            primordialIndex = _random.Next(3, 19);
            primordialIndex += (int)MathX.Round(starPart.age / 8);
            primordialIndex += (int)MathX.Round(-60 * MathX.Log10(this.planets[i].surfaceGravity));
            primordialIndex += (int)MathX.Round(-10 * MathX.Log10(starPart.metallicity));
            if (primordialIndex < 16)
            {
                this.planets[i].lithosphereType = LithosphereType.Molten;
            }
            else if (primordialIndex < 24)
            {
                this.planets[i].lithosphereType = LithosphereType.Soft;
            }
            else if (primordialIndex < 32)
            {
                this.planets[i].lithosphereType = LithosphereType.EarlyPlate;
            }
            else if (primordialIndex < 64)
            {
                this.planets[i].lithosphereType = LithosphereType.MaturePlate;
            }
            else if (primordialIndex < 88)
            {
                this.planets[i].lithosphereType = LithosphereType.AncientPlate;
            }
            else
            {
                this.planets[i].lithosphereType = LithosphereType.Solid;
            }
            if (this.planets[i].lithosphereType == LithosphereType.EarlyPlate || this.planets[i].lithosphereType == LithosphereType.MaturePlate ||
                this.planets[i].lithosphereType == LithosphereType.AncientPlate)
            {
                int tectonicIndex = _random.Next(3, 19);
                if (this.planets[i].waterType == WaterType.Extensive || this.planets[i].waterType == WaterType.Massive) tectonicIndex += 6;
                else if (this.planets[i].waterType == WaterType.Minimal || this.planets[i].waterType == WaterType.Trace) tectonicIndex -= 6;
                if (this.planets[i].lithosphereType == LithosphereType.EarlyPlate) tectonicIndex += 2;
                else if (this.planets[i].lithosphereType == LithosphereType.AncientPlate) tectonicIndex -= 2;
                if (tectonicIndex > 10) this.planets[i].plateTectonics = PlateTectonics.Mobile;
                else this.planets[i].plateTectonics = PlateTectonics.Fixed;
            }
            if (this.planets[i].lithosphereType == LithosphereType.Molten && this.planets[i].waterType != WaterType.Massive)
            {
                this.planets[i].waterType = WaterType.Trace;
                this.planets[i].waterCoverage = 0;
            }
            if (this.planets[i].waterType == WaterType.Extensive)
            {
                if (this.planets[i].lithosphereType == LithosphereType.Soft || this.planets[i].lithosphereType == LithosphereType.Solid)
                {
                    this.planets[i].waterCoverage += (_random.Next(3, 19) + 10) / 100f;
                    if (this.planets[i].waterCoverage > 1) this.planets[i].waterCoverage = 1;
                }
                else if (this.planets[i].lithosphereType == LithosphereType.EarlyPlate || this.planets[i].lithosphereType == LithosphereType.AncientPlate)
                {
                    this.planets[i].waterCoverage += (_random.Next(3, 19)) / 100f;
                    if (this.planets[i].waterCoverage > 1) this.planets[i].waterCoverage = 1;
                }
            }
        }
    }

    /// <summary>
    /// ����ų�?
    /// </summary>
    private void GetMagneticField()
    {
        for (int i = 0; i < this.planets.Count; i++)
        {
            int magneticIndex;
            for (int j = 0; j < this.planets[i].moons.Count; j++)
            {
                magneticIndex = _random.Next(3, 19);
                if (this.planets[i].moons[j].lithosphereType == LithosphereType.Soft) magneticIndex += 4;
                else if ((this.planets[i].moons[j].lithosphereType == LithosphereType.EarlyPlate || this.planets[i].moons[j].lithosphereType == LithosphereType.AncientPlate) &&
                          this.planets[i].moons[j].plateTectonics == PlateTectonics.Mobile) magneticIndex += 8;
                else if (this.planets[i].moons[j].lithosphereType == LithosphereType.MaturePlate && this.planets[i].moons[j].plateTectonics == PlateTectonics.Mobile) magneticIndex += 12;
                if (magneticIndex < 15)
                {
                    this.planets[i].moons[j].magneticField = MagneticField.None;
                }
                else if (magneticIndex < 18)
                {
                    this.planets[i].moons[j].magneticField = MagneticField.Weak;
                }
                else if (magneticIndex < 20)
                {
                    this.planets[i].moons[j].magneticField = MagneticField.Moderate;
                }
                else
                {
                    this.planets[i].moons[j].magneticField = MagneticField.Strong;
                }
            }
            magneticIndex = _random.Next(3, 19);
            if (this.planets[i].lithosphereType == LithosphereType.Soft) magneticIndex += 4;
            else if ((this.planets[i].lithosphereType == LithosphereType.EarlyPlate || this.planets[i].lithosphereType == LithosphereType.AncientPlate) &&
                      this.planets[i].plateTectonics == PlateTectonics.Mobile) magneticIndex += 8;
            else if (this.planets[i].lithosphereType == LithosphereType.MaturePlate && this.planets[i].plateTectonics == PlateTectonics.Mobile) magneticIndex += 12;
            if (magneticIndex < 15)
            {
                this.planets[i].magneticField = MagneticField.None;
            }
            else if (magneticIndex < 18)
            {
                this.planets[i].magneticField = MagneticField.Weak;
            }
            else if (magneticIndex < 20)
            {
                this.planets[i].magneticField = MagneticField.Moderate;
            }
            else
            {
                this.planets[i].magneticField = MagneticField.Strong;
            }
        }
    }

    /// <summary>
    /// �����ʼ������?
    /// </summary>
    private void GetEarlyAtmosphere()
    {
        for (int i = 0; i < this.planets.Count; i++)
        {
            int atmosphereIndex;
            for (int j = 0; j < this.planets[i].moons.Count; j++)
            {
                atmosphereIndex = _random.Next(3, 19);
                if (this.planets[i].moons[j].waterType == WaterType.Massive || this.planets[i].moons[j].greenHouse != GreenHouse.None) atmosphereIndex += 6;
                if (this.planets[i].moons[j].lithosphereType == LithosphereType.Molten) atmosphereIndex += 6;
                else if (this.planets[i].moons[j].lithosphereType == LithosphereType.Soft) atmosphereIndex += 4;
                else if (this.planets[i].moons[j].lithosphereType == LithosphereType.EarlyPlate) atmosphereIndex += 2;
                else if (this.planets[i].moons[j].lithosphereType == LithosphereType.AncientPlate) atmosphereIndex -= 2;
                else if (this.planets[i].moons[j].lithosphereType == LithosphereType.Solid) atmosphereIndex -= 4;
                if (this.planets[i].moons[j].magneticField == MagneticField.Moderate) atmosphereIndex -= 2;
                else if (this.planets[i].moons[j].magneticField == MagneticField.Weak) atmosphereIndex -= 4;
                else if (this.planets[i].moons[j].magneticField == MagneticField.None) atmosphereIndex -= 6;
                this.planets[i].moons[j].atmosphereRetentionFactor = atmosphereIndex * 0.1f;

                if (this.planets[i].moons[j].atmosphereRetentionFactor > 0 && this.planets[i].moons[j].M_number <= 2)
                {
                    this.planets[i].moons[j].atmosphereH2 = 7.5f * this.planets[i].moons[j].atmosphereRetentionFactor;
                }
                if (this.planets[i].moons[j].atmosphereRetentionFactor > 0 && this.planets[i].moons[j].M_number <= 4)
                {
                    this.planets[i].moons[j].atmosphereHe = 2.5f * this.planets[i].moons[j].atmosphereRetentionFactor;
                }
                if (this.planets[i].moons[j].atmosphereRetentionFactor > 0 && this.planets[i].moons[j].M_number <= 28 && this.planets[i].moons[j].blackBodyTemperature >= 80)
                {
                    this.planets[i].moons[j].atmosphereN2 = 0.7f * this.planets[i].moons[j].atmosphereRetentionFactor;
                    if (this.planets[i].moons[j].blackBodyTemperature <= 125 && this.planets[i].moons[j].waterType == WaterType.Massive)
                    {
                        this.planets[i].moons[j].atmosphereN2 *= 15;
                    }
                }

                if (this.planets[i].moons[j].greenHouse == GreenHouse.Dry)
                {
                    this.planets[i].moons[j].atmosphereType = AtmosphereType.Venus;
                }
                else if (this.planets[i].moons[j].atmosphereH2 != 0)
                {
                    this.planets[i].moons[j].atmosphereType = AtmosphereType.Dulcinea;
                }
                else if (this.planets[i].moons[j].atmosphereH2 == 0 && this.planets[i].moons[j].atmosphereN2 != 0 &&
                         this.planets[i].moons[j].blackBodyTemperature >= 80 && this.planets[i].moons[j].blackBodyTemperature <= 125)
                {
                    this.planets[i].moons[j].atmosphereType = AtmosphereType.Titan;
                }
                else if (this.planets[i].moons[j].atmosphereH2 == 0 && this.planets[i].moons[j].atmosphereN2 != 0 &&
                         this.planets[i].moons[j].blackBodyTemperature >= 125)
                {
                    this.planets[i].moons[j].atmosphereType = AtmosphereType.Earth;
                }
                else if (this.planets[i].moons[j].atmosphereH2 == 0 && this.planets[i].moons[j].atmosphereHe == 0 &&
                         this.planets[i].moons[j].atmosphereN2 == 0 && this.planets[i].moons[j].M_number <= 44 &&
                         this.planets[i].moons[j].blackBodyTemperature > 195)
                {
                    this.planets[i].moons[j].atmosphereType = AtmosphereType.Mars;
                }
                else
                {
                    this.planets[i].moons[j].atmosphereType = AtmosphereType.Luna;
                }
            }
            atmosphereIndex = _random.Next(3, 19);
            if (this.planets[i].waterType == WaterType.Massive || this.planets[i].greenHouse != GreenHouse.None) atmosphereIndex += 6;
            if (this.planets[i].lithosphereType == LithosphereType.Molten) atmosphereIndex += 6;
            else if (this.planets[i].lithosphereType == LithosphereType.Soft) atmosphereIndex += 4;
            else if (this.planets[i].lithosphereType == LithosphereType.EarlyPlate) atmosphereIndex += 2;
            else if (this.planets[i].lithosphereType == LithosphereType.AncientPlate) atmosphereIndex -= 2;
            else if (this.planets[i].lithosphereType == LithosphereType.Solid) atmosphereIndex -= 4;
            if (this.planets[i].magneticField == MagneticField.Moderate) atmosphereIndex -= 2;
            else if (this.planets[i].magneticField == MagneticField.Weak) atmosphereIndex -= 4;
            else if (this.planets[i].magneticField == MagneticField.None) atmosphereIndex -= 6;
            this.planets[i].atmosphereRetentionFactor = atmosphereIndex * 0.1f;

            if (this.planets[i].atmosphereRetentionFactor > 0 && this.planets[i].M_number <= 2)
            {
                this.planets[i].atmosphereH2 = 7.5f * this.planets[i].atmosphereRetentionFactor;
            }
            if (this.planets[i].atmosphereRetentionFactor > 0 && this.planets[i].M_number <= 4)
            {
                this.planets[i].atmosphereHe = 2.5f * this.planets[i].atmosphereRetentionFactor;
            }
            if (this.planets[i].atmosphereRetentionFactor > 0 && this.planets[i].M_number <= 28 && this.planets[i].blackBodyTemperature >= 80)
            {
                this.planets[i].atmosphereN2 = 0.7f * this.planets[i].atmosphereRetentionFactor;
                if (this.planets[i].blackBodyTemperature <= 125 && this.planets[i].waterType == WaterType.Massive)
                {
                    this.planets[i].atmosphereN2 *= 15;
                }
            }

            if (this.planets[i].greenHouse == GreenHouse.Dry)
            {
                this.planets[i].atmosphereType = AtmosphereType.Venus;
            }
            else if (this.planets[i].atmosphereH2 != 0)
            {
                this.planets[i].atmosphereType = AtmosphereType.Dulcinea;
            }
            else if (this.planets[i].atmosphereH2 == 0 && this.planets[i].atmosphereN2 != 0 &&
                     this.planets[i].blackBodyTemperature >= 80 && this.planets[i].blackBodyTemperature <= 125)
            {
                this.planets[i].atmosphereType = AtmosphereType.Titan;
            }
            else if (this.planets[i].atmosphereH2 == 0 && this.planets[i].atmosphereN2 != 0 &&
                     this.planets[i].blackBodyTemperature >= 125)
            {
                this.planets[i].atmosphereType = AtmosphereType.Earth;
            }
            else if (this.planets[i].atmosphereH2 == 0 && this.planets[i].atmosphereHe == 0 &&
                     this.planets[i].atmosphereN2 == 0 && this.planets[i].M_number <= 44 &&
                     this.planets[i].blackBodyTemperature > 195)
            {
                this.planets[i].atmosphereType = AtmosphereType.Mars;
            }
            else
            {
                this.planets[i].atmosphereType = AtmosphereType.Luna;
            }
        }
    }

    /// <summary>
    /// ���㷴����
    /// </summary>
    private void Albedo()
    {
        for (int i = 0; i < this.planets.Count; i++)
        {
            for (int j = 0; j < this.planets[i].moons.Count; j++)
            {
                switch (this.planets[i].moons[j].atmosphereType)
                {
                    case AtmosphereType.Venus:
                        this.planets[i].moons[j].albedo = 0.65f; break;
                    case AtmosphereType.Dulcinea:
                        this.planets[i].moons[j].albedo = 0.20f; break;
                    case AtmosphereType.Titan:
                        this.planets[i].moons[j].albedo = 0.10f; break;
                    case AtmosphereType.Earth:case AtmosphereType.Mars:
                        switch (this.planets[i].moons[j].waterType)
                        {
                            case WaterType.Trace:
                                this.planets[i].moons[j].albedo = 0.15f; break;
                            case WaterType.Minimal:
                                this.planets[i].moons[j].albedo = 0.16f; break;
                            case WaterType.Moderate:
                                this.planets[i].moons[j].albedo = 0.19f; break;
                            case WaterType.Extensive:
                                this.planets[i].moons[j].albedo = 0.22f; break;
                            case WaterType.Massive:
                                this.planets[i].moons[j].albedo = 0.25f; break;
                            default:
                                this.planets[i].moons[j].albedo = 0f; break;
                        }
                        break;
                    case AtmosphereType.Luna:
                        switch (this.planets[i].moons[j].waterType)
                        {
                            case WaterType.Trace:
                                this.planets[i].moons[j].albedo = 0.01f; break;
                            case WaterType.Minimal:
                                this.planets[i].moons[j].albedo = 0.02f; break;
                            case WaterType.Moderate:
                                this.planets[i].moons[j].albedo = 0.08f; break;
                            case WaterType.Extensive:
                                this.planets[i].moons[j].albedo = 0.14f; break;
                            case WaterType.Massive:
                                this.planets[i].moons[j].albedo = 0.20f; break;
                            default:
                                this.planets[i].moons[j].albedo = 0f; break;
                        }
                        break;
                    default:
                        this.planets[i].moons[j].albedo = 0f; break;
                }
                this.planets[i].moons[j].albedo += _random.Next(3, 19) * 0.01f;
                if (this.planets[i].moons[j].atmosphereType == AtmosphereType.Luna)
                {
                    if (this.planets[i].moons[j].lithosphereType == LithosphereType.Molten || this.planets[i].moons[j].lithosphereType == LithosphereType.Soft) this.planets[i].moons[j].albedo += 0.5f;
                    else if (this.planets[i].moons[j].lithosphereType == LithosphereType.EarlyPlate || this.planets[i].moons[j].lithosphereType == LithosphereType.MaturePlate ||
                             (this.planets[i].moons[j].lithosphereType == LithosphereType.AncientPlate && this.planets[i].moons[j].plateTectonics == PlateTectonics.Mobile)) this.planets[i].moons[j].albedo += 0.3f;
                    else if (((this.planets[i].moons[j].lithosphereType == LithosphereType.AncientPlate && this.planets[i].moons[j].plateTectonics == PlateTectonics.Fixed) ||
                               this.planets[i].moons[j].lithosphereType == LithosphereType.Solid) && this.planets[i].moons[j].blackBodyTemperature < 80) this.planets[i].moons[j].albedo += 0.3f;
                }
            }
            switch (this.planets[i].atmosphereType)
            {
                case AtmosphereType.Venus:
                    this.planets[i].albedo = 0.65f; break;
                case AtmosphereType.Dulcinea:
                    this.planets[i].albedo = 0.20f; break;
                case AtmosphereType.Titan:
                    this.planets[i].albedo = 0.10f; break;
                case AtmosphereType.Earth:
                case AtmosphereType.Mars:
                    switch (this.planets[i].waterType)
                    {
                        case WaterType.Trace:
                            this.planets[i].albedo = 0.15f; break;
                        case WaterType.Minimal:
                            this.planets[i].albedo = 0.16f; break;
                        case WaterType.Moderate:
                            this.planets[i].albedo = 0.19f; break;
                        case WaterType.Extensive:
                            this.planets[i].albedo = 0.22f; break;
                        case WaterType.Massive:
                            this.planets[i].albedo = 0.25f; break;
                        default:
                            this.planets[i].albedo = 0f; break;
                    }
                    break;
                case AtmosphereType.Luna:
                    switch (this.planets[i].waterType)
                    {
                        case WaterType.Trace:
                            this.planets[i].albedo = 0.01f; break;
                        case WaterType.Minimal:
                            this.planets[i].albedo = 0.02f; break;
                        case WaterType.Moderate:
                            this.planets[i].albedo = 0.08f; break;
                        case WaterType.Extensive:
                            this.planets[i].albedo = 0.14f; break;
                        case WaterType.Massive:
                            this.planets[i].albedo = 0.20f; break;
                        default:
                            this.planets[i].albedo = 0f; break;
                    }
                    break;
                default:
                    this.planets[i].albedo = 0f; break;
            }
            this.planets[i].albedo += _random.Next(3, 19) * 0.01f;
            if (this.planets[i].atmosphereType == AtmosphereType.Luna)
            {
                if (this.planets[i].lithosphereType == LithosphereType.Molten || this.planets[i].lithosphereType == LithosphereType.Soft) this.planets[i].albedo += 0.5f;
                else if (this.planets[i].lithosphereType == LithosphereType.EarlyPlate || this.planets[i].lithosphereType == LithosphereType.MaturePlate ||
                         (this.planets[i].lithosphereType == LithosphereType.AncientPlate && this.planets[i].plateTectonics == PlateTectonics.Mobile)) this.planets[i].albedo += 0.3f;
                else if (((this.planets[i].lithosphereType == LithosphereType.AncientPlate && this.planets[i].plateTectonics == PlateTectonics.Fixed) ||
                           this.planets[i].lithosphereType == LithosphereType.Solid) && this.planets[i].blackBodyTemperature < 80) this.planets[i].albedo += 0.3f;
            }
        }
    }

    /// <summary>
    /// ���������̼����?
    /// </summary>
    private void GetCarbonDioxide()
    {
        for (int i = 0; i < this.planets.Count; i++)
        {
            for (int j = 0; j < this.planets[i].moons.Count; j++)
            {
                if (this.planets[i].moons[j].atmosphereType != AtmosphereType.Luna)
                {
                    if (this.planets[i].moons[j].atmosphereType == AtmosphereType.Venus)
                    {
                        this.planets[i].moons[j].atmosphereCO2 = 100 * this.planets[i].moons[j].atmosphereRetentionFactor;
                    }
                    else if ((this.planets[i].moons[j].atmosphereType == AtmosphereType.Earth && this.planets[i].moons[j].greenHouse != GreenHouse.Dry) ||
                        this.planets[i].moons[j].atmosphereType == AtmosphereType.Mars)
                    {
                        if (this.planets[i].moons[j].M_number <= 44 && this.planets[i].moons[j].blackBodyTemperature >= 195 &&
                            this.planets[i].moons[j].atmosphereRetentionFactor > 0)
                        {
                            this.planets[i].moons[j].atmosphereCO2 = this.planets[i].moons[j].atmosphereRetentionFactor;
                        }
                        if (this.planets[i].moons[j].M_number <= 44 && this.planets[i].moons[j].blackBodyTemperature >= 195 &&
                           this.planets[i].moons[j].atmosphereRetentionFactor == 0 && this.planets[i].moons[j].atmosphereType == AtmosphereType.Mars)
                        {
                            this.planets[i].moons[j].atmosphereCO2 = _random.Next(1, 7) * 0.01f;
                        }
                    }
                    if (this.planets[i].moons[j].atmosphereCO2 > 0 && (this.planets[i].moons[j].waterType == WaterType.Moderate || this.planets[i].moons[j].waterType == WaterType.Extensive))
                    {
                        float T = (this.planets[i].moons[j].blackBodyTemperature * MathX.Pow((1 - this.planets[i].moons[j].albedo), 0.25f)) + (8.0f * MathX.Log10(this.planets[i].moons[j].atmosphereCO2) + 36.0f);
                        if (T >= 260)
                        {
                            this.planets[i].moons[j].activeCarbonateSilicateCycle = true;
                        }
                    }
                }
            }
            if (this.planets[i].atmosphereType != AtmosphereType.Luna)
            {
                if (this.planets[i].atmosphereType == AtmosphereType.Venus)
                {
                    this.planets[i].atmosphereCO2 = 100 * this.planets[i].atmosphereRetentionFactor;
                }
                else if ((this.planets[i].atmosphereType == AtmosphereType.Earth && this.planets[i].greenHouse != GreenHouse.Dry) ||
                    this.planets[i].atmosphereType == AtmosphereType.Mars)
                {
                    if (this.planets[i].M_number <= 44 && this.planets[i].blackBodyTemperature >= 195 &&
                        this.planets[i].atmosphereRetentionFactor > 0)
                    {
                        this.planets[i].atmosphereCO2 = this.planets[i].atmosphereRetentionFactor;
                    }
                    if (this.planets[i].M_number <= 44 && this.planets[i].blackBodyTemperature >= 195 &&
                       this.planets[i].atmosphereRetentionFactor == 0 && this.planets[i].atmosphereType == AtmosphereType.Mars)
                    {
                        this.planets[i].atmosphereCO2 = _random.Next(1, 7) * 0.01f;
                    }
                }
                if (this.planets[i].atmosphereCO2 > 0 && (this.planets[i].waterType == WaterType.Moderate || this.planets[i].waterType == WaterType.Extensive))
                {
                    float T = (this.planets[i].blackBodyTemperature * MathX.Pow((1 - this.planets[i].albedo), 0.25f)) + (8.0f * MathX.Log10(this.planets[i].atmosphereCO2) + 36.0f);
                    if (T >= 260)
                    {
                        this.planets[i].activeCarbonateSilicateCycle = true;
                    }
                }
            }
        }
    }

    /// <summary>
    /// ��������
    /// </summary>
    /// <param name="starPart">���ǲ���</param>
    private void GetLife(StarPart starPart)
    {
        for (int i = 0; i < this.planets.Count; i++)
        {
            float lifeTime = 0f;
            for (int j = 0; j < this.planets[i].moons.Count; j++)
            {
                if (this.planets[i].moons[j].greenHouse == GreenHouse.None && this.planets[i].moons[j].waterType >= WaterType.Moderate &&
                    this.planets[i].moons[j].lithosphereType >= LithosphereType.Soft && this.planets[i].moons[j].lithosphereType <= LithosphereType.AncientPlate &&
                    this.planets[i].moons[j].plateTectonics != PlateTectonics.Fixed)
                {
                    lifeTime = _random.Next(3, 19) * 0.03f;
                    if (starPart.age > lifeTime)
                    {
                        this.planets[i].moons[j].lifeHistory.Add(Life.DeepHydrothermalVents, lifeTime);
                    }
                }
                if (this.planets[i].moons[j].atmosphereType == AtmosphereType.Earth && this.planets[i].moons[j].activeCarbonateSilicateCycle == true &&
                    this.planets[i].moons[j].lithosphereType >= LithosphereType.Soft && this.planets[i].moons[j].lithosphereType <= LithosphereType.AncientPlate)
                {
                    lifeTime = _random.Next(3, 19);
                    if (this.planets[i].moons[j].lithosphereType == LithosphereType.Soft || this.planets[i].moons[j].plateTectonics == PlateTectonics.Mobile)
                    {
                        lifeTime = lifeTime * 0.1f;
                    }
                    else
                    {
                        lifeTime = lifeTime * 0.2f;
                    }
                    if (this.planets[i].moons[j].lifeHistory.ContainsKey(Life.DeepHydrothermalVents))
                    {
                        float anotherLife = _random.Next(3, 19) * 0.075f + this.planets[i].moons[j].lifeHistory[Life.DeepHydrothermalVents];
                        lifeTime = MathX.Min(lifeTime, anotherLife);
                    }
                    if (starPart.age > lifeTime)
                    {
                        this.planets[i].moons[j].lifeHistory.Add(Life.SurfaceRefugia, lifeTime);
                    }
                }
                if (this.planets[i].moons[j].lifeHistory.Keys.Count == 0)
                {
                    break;
                }
                lifeTime = _random.Next(3, 19) * 0.075f;
                if (this.planets[i].moons[j].lifeHistory.ContainsKey(Life.DeepHydrothermalVents))
                {
                    if (this.planets[i].moons[j].lifeHistory.ContainsKey(Life.SurfaceRefugia))
                    {
                        lifeTime = lifeTime + MathX.Min(this.planets[i].moons[j].lifeHistory[Life.DeepHydrothermalVents], this.planets[i].moons[j].lifeHistory[Life.SurfaceRefugia]);
                    }
                    else
                    {
                        lifeTime = lifeTime + this.planets[i].moons[j].lifeHistory[Life.DeepHydrothermalVents];
                    }
                }
                else
                {
                    lifeTime = lifeTime + this.planets[i].moons[j].lifeHistory[Life.SurfaceRefugia];
                }
                if (starPart.age > lifeTime)
                {
                    this.planets[i].moons[j].lifeHistory.Add(Life.Multicellular, lifeTime);
                }

                if (this.planets[i].moons[j].lifeHistory.ContainsKey(Life.SurfaceRefugia) && starPart.allStars[0].starType != StarType.BrownDwarf)
                {
                    float photoFactor = 0f;
                    switch (starPart.allStars[0].starClass)
                    {
                        case StarClass.A: case StarClass.F: case StarClass.G:
                            photoFactor =  0.1f; break;
                        case StarClass.K:
                            photoFactor = 0.19f; break;
                        case StarClass.M:
                            photoFactor = 0.3f; break;
                        default:
                            photoFactor = 999; break;
                    }
                    lifeTime = _random.Next(3, 19) * photoFactor + this.planets[i].moons[j].lifeHistory[Life.SurfaceRefugia];
                    if (starPart.age > lifeTime)
                    {
                        this.planets[i].moons[j].lifeHistory.Add(Life.Photosynthesis, lifeTime);
                    }

                    if (this.planets[i].moons[j].lifeHistory.ContainsKey(Life.Photosynthesis))
                    {
                        lifeTime = _random.Next(3, 19) * 1.5f * photoFactor + this.planets[i].moons[j].lifeHistory[Life.Photosynthesis];
                        if (starPart.age > lifeTime)
                        {
                            this.planets[i].moons[j].lifeHistory.Add(Life.OxygenCatastrophe, lifeTime);
                        }
                    }
                }

                if (this.planets[i].moons[j].lifeHistory.ContainsKey(Life.Multicellular))
                {
                    lifeTime = _random.Next(3, 19) * 0.3f + this.planets[i].moons[j].lifeHistory[Life.Multicellular];
                    if (this.planets[i].moons[j].lifeHistory.ContainsKey(Life.OxygenCatastrophe) && lifeTime > this.planets[i].moons[j].lifeHistory[Life.OxygenCatastrophe])
                    {
                        lifeTime = lifeTime - 0.5f * (lifeTime - this.planets[i].moons[j].lifeHistory[Life.OxygenCatastrophe]);
                    }
                    if (starPart.age > lifeTime)
                    {
                        this.planets[i].moons[j].lifeHistory.Add(Life.Animal, lifeTime);
                    }
                }

                if (this.planets[i].moons[j].lifeHistory.ContainsKey(Life.Animal))
                {
                    if (this.planets[i].moons[j].waterType == WaterType.Massive)
                    {
                        lifeTime = _random.Next(3, 19) * 0.1f + this.planets[i].moons[j].lifeHistory[Life.Animal];
                    }
                    else
                    {
                        lifeTime = _random.Next(3, 19) * 0.05f + this.planets[i].moons[j].lifeHistory[Life.Animal];
                    }
                    if (starPart.age > lifeTime)
                    {
                        this.planets[i].moons[j].lifeHistory.Add(Life.PreSapientLife, lifeTime);
                    }
                }
                if (this.planets[i].moons[j].lifeHistory.ContainsKey(Life.Photosynthesis) && !this.planets[i].moons[j].lifeHistory.ContainsKey(Life.OxygenCatastrophe))
                {
                    this.planets[i].moons[j].atmosphereMO2 = _random.Next(3, 19) * 0.002f;
                }
                else if (this.planets[i].moons[j].lifeHistory.ContainsKey(Life.OxygenCatastrophe))
                {
                    this.planets[i].moons[j].atmosphereMO2 = (_random.Next(3, 19) + 10) * 0.01f * this.planets[i].moons[j].atmosphereRetentionFactor;
                }
            }
            if (this.planets[i].greenHouse == GreenHouse.None && this.planets[i].waterType >= WaterType.Moderate &&
                    this.planets[i].lithosphereType >= LithosphereType.Soft && this.planets[i].lithosphereType <= LithosphereType.AncientPlate &&
                    this.planets[i].plateTectonics != PlateTectonics.Fixed)
            {
                lifeTime = _random.Next(3, 19) * 0.03f;
                if (starPart.age > lifeTime)
                {
                    this.planets[i].lifeHistory.Add(Life.DeepHydrothermalVents, lifeTime);
                }
            }
            if (this.planets[i].atmosphereType == AtmosphereType.Earth && this.planets[i].activeCarbonateSilicateCycle == true &&
                this.planets[i].lithosphereType >= LithosphereType.Soft && this.planets[i].lithosphereType <= LithosphereType.AncientPlate)
            {
                lifeTime = _random.Next(3, 19);
                if (this.planets[i].lithosphereType == LithosphereType.Soft || this.planets[i].plateTectonics == PlateTectonics.Mobile)
                {
                    lifeTime = lifeTime * 0.1f;
                }
                else
                {
                    lifeTime = lifeTime * 0.2f;
                }
                if (this.planets[i].lifeHistory.ContainsKey(Life.DeepHydrothermalVents))
                {
                    float anotherLife = _random.Next(3, 19) * 0.075f + this.planets[i].lifeHistory[Life.DeepHydrothermalVents];
                    lifeTime = MathX.Min(lifeTime, anotherLife);
                }
                if (starPart.age > lifeTime)
                {
                    this.planets[i].lifeHistory.Add(Life.SurfaceRefugia, lifeTime);
                }
            }
            if (this.planets[i].lifeHistory.Keys.Count == 0)
            {
                break;
            }
            lifeTime = _random.Next(3, 19) * 0.075f;
            if (this.planets[i].lifeHistory.ContainsKey(Life.DeepHydrothermalVents))
            {
                if (this.planets[i].lifeHistory.ContainsKey(Life.SurfaceRefugia))
                {
                    lifeTime = lifeTime + MathX.Min(this.planets[i].lifeHistory[Life.DeepHydrothermalVents], this.planets[i].lifeHistory[Life.SurfaceRefugia]);
                }
                else
                {
                    lifeTime = lifeTime + this.planets[i].lifeHistory[Life.DeepHydrothermalVents];
                }
            }
            else
            {
                lifeTime = lifeTime + this.planets[i].lifeHistory[Life.SurfaceRefugia];
            }
            if (starPart.age > lifeTime)
            {
                this.planets[i].lifeHistory.Add(Life.Multicellular, lifeTime);
            }

            if (this.planets[i].lifeHistory.ContainsKey(Life.SurfaceRefugia) && starPart.allStars[0].starType != StarType.BrownDwarf)
            {
                float photoFactor = 0f;
                switch (starPart.allStars[0].starClass)
                {
                    case StarClass.A:
                    case StarClass.F:
                    case StarClass.G:
                        photoFactor = 0.1f; break;
                    case StarClass.K:
                        photoFactor = 0.19f; break;
                    case StarClass.M:
                        photoFactor = 0.3f; break;
                    default:
                        photoFactor = 999; break;
                }
                lifeTime = _random.Next(3, 19) * photoFactor + this.planets[i].lifeHistory[Life.SurfaceRefugia];
                if (starPart.age > lifeTime)
                {
                    this.planets[i].lifeHistory.Add(Life.Photosynthesis, lifeTime);
                }

                if (this.planets[i].lifeHistory.ContainsKey(Life.Photosynthesis))
                {
                    lifeTime = _random.Next(3, 19) * 1.5f * photoFactor + this.planets[i].lifeHistory[Life.Photosynthesis];
                    if (starPart.age > lifeTime)
                    {
                        this.planets[i].lifeHistory.Add(Life.OxygenCatastrophe, lifeTime);
                    }
                }
            }

            if (this.planets[i].lifeHistory.ContainsKey(Life.Multicellular))
            {
                lifeTime = _random.Next(3, 19) * 0.3f + this.planets[i].lifeHistory[Life.Multicellular];
                if (this.planets[i].lifeHistory.ContainsKey(Life.OxygenCatastrophe) && lifeTime > this.planets[i].lifeHistory[Life.OxygenCatastrophe])
                {
                    lifeTime = lifeTime - 0.5f * (lifeTime - this.planets[i].lifeHistory[Life.OxygenCatastrophe]);
                }
                if (starPart.age > lifeTime)
                {
                    this.planets[i].lifeHistory.Add(Life.Animal, lifeTime);
                }
            }

            if (this.planets[i].lifeHistory.ContainsKey(Life.Animal))
            {
                if (this.planets[i].waterType == WaterType.Massive)
                {
                    lifeTime = _random.Next(3, 19) * 0.1f + this.planets[i].lifeHistory[Life.Animal];
                }
                else
                {
                    lifeTime = _random.Next(3, 19) * 0.05f + this.planets[i].lifeHistory[Life.Animal];
                }
                if (starPart.age > lifeTime)
                {
                    this.planets[i].lifeHistory.Add(Life.PreSapientLife, lifeTime);
                }
            }
            if (this.planets[i].lifeHistory.ContainsKey(Life.Photosynthesis) && !this.planets[i].lifeHistory.ContainsKey(Life.OxygenCatastrophe))
            {
                this.planets[i].atmosphereMO2 = _random.Next(3, 19) * 0.002f;
            }
            else if (this.planets[i].lifeHistory.ContainsKey(Life.OxygenCatastrophe))
            {
                this.planets[i].atmosphereMO2 = (_random.Next(3, 19) + 10) * 0.01f * this.planets[i].atmosphereRetentionFactor;
            }
        }
    }

    /// <summary>
    /// ����ƽ�������¶�
    /// </summary>
    private void GetAverageSurfaceTemperature()
    {
        for (int i = 0; i < this.planets.Count; i++)
        {
            float averageTemperature = 0;
            for (int j = 0; j < this.planets[i].moons.Count; j++)
            {
                if (this.planets[i].moons[j].atmosphereType == AtmosphereType.Venus)
                {
                    averageTemperature = (this.planets[i].moons[j].blackBodyTemperature * MathX.Pow((1 - this.planets[i].moons[j].albedo), 0.25f)) + (250 * MathX.Log10(this.planets[i].moons[j].atmosphereCO2));
                }
                else if (this.planets[i].moons[j].atmosphereType == AtmosphereType.Dulcinea)
                {
                    int K;
                    if (this.planets[i].moons[j].greenHouse != GreenHouse.Wet) K = 180;
                    else K = 500;
                    averageTemperature = (this.planets[i].moons[j].blackBodyTemperature * MathX.Pow((1 - this.planets[i].moons[j].albedo), 0.25f)) + (K * MathX.Log10(this.planets[i].moons[j].atmosphereH2));
                }
                else if (this.planets[i].moons[j].atmosphereType == AtmosphereType.Luna)
                {
                    averageTemperature = (this.planets[i].moons[j].blackBodyTemperature * MathX.Pow((1 - this.planets[i].moons[j].albedo), 0.25f));
                }
                else
                {
                    float baseSurfaceTemperature = this.planets[i].moons[j].blackBodyTemperature * MathX.Pow((1 - this.planets[i].moons[j].albedo), 0.25f);
                    if (this.planets[i].moons[j].M_number <= 16 && this.planets[i].moons[j].blackBodyTemperature >= 110)
                    {
                        if (this.planets[i].moons[j].atmosphereType == AtmosphereType.Titan ||
                            (this.planets[i].moons[j].atmosphereType == AtmosphereType.Earth && (this.planets[i].moons[j].lifeHistory.ContainsKey(Life.DeepHydrothermalVents) || this.planets[i].moons[j].lifeHistory.ContainsKey(Life.SurfaceRefugia))))
                        {
                            this.planets[i].moons[j].atmosphereCH4 = 2.1f + 8 * MathX.Log10(this.planets[i].moons[j].atmosphereRetentionFactor);
                        }
                    }
                    if (this.planets[i].moons[j].lifeHistory.ContainsKey(Life.OxygenCatastrophe))
                    {
                        this.planets[i].moons[j].atmosphereO3 = 1.7f + 8 * MathX.Log10(this.planets[i].moons[j].atmosphereRetentionFactor);
                    }
                    float GCO2;
                    baseSurfaceTemperature += this.planets[i].moons[j].atmosphereCH4 + this.planets[i].moons[j].atmosphereO3;
                    if (this.planets[i].moons[j].activeCarbonateSilicateCycle == true)
                    {
                        float C = 260 - baseSurfaceTemperature;
                        GCO2 = MathX.Max(C, 8) + _random.Next(2, 13) - 7;
                        this.planets[i].moons[j].atmosphereCO2 = (3.16f * MathX.Pow(10, -5) * MathX.Pow(1.333f, GCO2));
                    }
                    else
                    {
                        if (this.planets[i].moons[j].atmosphereCO2 != 0) GCO2 = 36 + (8 * MathX.Log10(this.planets[i].moons[j].atmosphereCO2));
                        else GCO2 = 0;
                    }
                    baseSurfaceTemperature += GCO2;
                    float GH2O = 0;
                    if (this.planets[i].moons[j].M_number <= 18 && this.planets[i].moons[j].blackBodyTemperature >= 260 &&
                        this.planets[i].moons[j].waterType >= WaterType.Moderate)
                    {
                        if (baseSurfaceTemperature < 260)
                        {
                            GH2O = 0;
                        }
                        else
                        {
                            GH2O = 16 + (baseSurfaceTemperature - 260) / 5f;
                        }
                        float K = 4 + 10 * MathX.Log10(this.planets[i].moons[j].waterCoverage);
                        GH2O = GH2O * K;
                        this.planets[i].moons[j].atmosphereH2O = 1.78f * MathX.Pow(10, -5) * MathX.Pow(1.333f, GH2O);
                    }
                    averageTemperature = baseSurfaceTemperature + GH2O;
                }
                this.planets[i].moons[j].averageTemperature = averageTemperature;
            }
            if (this.planets[i].atmosphereType == AtmosphereType.Venus)
            {
                averageTemperature = (this.planets[i].blackBodyTemperature * MathX.Pow((1 - this.planets[i].albedo), 0.25f)) + (250 * MathX.Log10(this.planets[i].atmosphereCO2));
            }
            else if (this.planets[i].atmosphereType == AtmosphereType.Dulcinea)
            {
                int K;
                if (this.planets[i].greenHouse != GreenHouse.Wet) K = 180;
                else K = 500;
                averageTemperature = (this.planets[i].blackBodyTemperature * MathX.Pow((1 - this.planets[i].albedo), 0.25f)) + (K * MathX.Log10(this.planets[i].atmosphereH2));
            }
            else if (this.planets[i].atmosphereType == AtmosphereType.Luna)
            {
                averageTemperature = (this.planets[i].blackBodyTemperature * MathX.Pow((1 - this.planets[i].albedo), 0.25f));
            }
            else
            {
                float baseSurfaceTemperature = this.planets[i].blackBodyTemperature * MathX.Pow((1 - this.planets[i].albedo), 0.25f);
                //Debug.Log(baseSurfaceTemperature);
                if (this.planets[i].M_number <= 16 && this.planets[i].blackBodyTemperature >= 110)
                {
                    if (this.planets[i].atmosphereType == AtmosphereType.Titan ||
                        (this.planets[i].atmosphereType == AtmosphereType.Earth && (this.planets[i].lifeHistory.ContainsKey(Life.DeepHydrothermalVents) || this.planets[i].lifeHistory.ContainsKey(Life.SurfaceRefugia))))
                    {
                        this.planets[i].atmosphereCH4 = 2.1f + 8 * MathX.Log10(this.planets[i].atmosphereRetentionFactor);
                    }
                }
                if (this.planets[i].lifeHistory.ContainsKey(Life.OxygenCatastrophe))
                {
                    this.planets[i].atmosphereO3 = 1.7f + 8 * MathX.Log10(this.planets[i].atmosphereRetentionFactor);
                }
                float GCO2 = 0f;
                baseSurfaceTemperature += this.planets[i].atmosphereCH4 + this.planets[i].atmosphereO3;
                //Debug.Log(this.planets[i].atmosphereCH4);
                //Debug.Log(this.planets[i].atmosphereO3);
                if (this.planets[i].activeCarbonateSilicateCycle == true)
                {
                    float C = 260 - baseSurfaceTemperature;
                    GCO2 = MathX.Max(C, 8) + _random.Next(2, 13) - 7;
                    this.planets[i].atmosphereCO2 = (3.16f * MathX.Pow(10, -5) * MathX.Pow(1.333f, GCO2));
                }
                else
                {
                    if (this.planets[i].atmosphereCO2 != 0) GCO2 = 36 + (8 * MathX.Log10(this.planets[i].atmosphereCO2));
                    else GCO2 = 0;
                }
                //Debug.Log(GCO2);
                baseSurfaceTemperature += GCO2;
                float GH2O = 0;
                if (this.planets[i].M_number <= 18 && this.planets[i].blackBodyTemperature >= 260 &&
                    this.planets[i].waterType >= WaterType.Moderate)
                {
                    if (baseSurfaceTemperature < 260)
                    {
                        GH2O = 0;
                    }
                    else
                    {
                        GH2O = 16 + (baseSurfaceTemperature - 260) / 5f;
                    }
                    float K = 4 + 10 * MathX.Log10(this.planets[i].waterCoverage);
                    GH2O = GH2O * K;
                    this.planets[i].atmosphereH2O = 1.78f * MathX.Pow(10, -5) * MathX.Pow(1.333f, GH2O);
                }
                averageTemperature = baseSurfaceTemperature + GH2O;
            }
            this.planets[i].averageTemperature = averageTemperature;
        }
    }

    /// <summary>
    /// ��ȡ���մ���
    /// </summary>
    private void GetFinalAtmosphere()
    {
        for (int i = 0; i < this.planets.Count; i++)
        {
            float oxygenPressure, carbonDioxidePressure, nitrogenPressure, averageMass;
            for (int j = 0; j < this.planets[i].moons.Count; j++)
            {
                this.planets[i].moons[j].totalAtmosphereMass = this.planets[i].moons[j].atmosphereH2 + this.planets[i].moons[j].atmosphereHe +
                    this.planets[i].moons[j].atmosphereN2 + this.planets[i].moons[j].atmosphereCO2 +
                    this.planets[i].moons[j].atmosphereMO2 + this.planets[i].moons[j].atmosphereH2O;
                this.planets[i].moons[j].atmospherePressure = this.planets[i].moons[j].totalAtmosphereMass * this.planets[i].moons[j].surfaceGravity;
                oxygenPressure = this.planets[i].moons[j].atmosphereMO2 * this.planets[i].moons[j].surfaceGravity;
                carbonDioxidePressure = this.planets[i].moons[j].atmosphereCO2 * this.planets[i].moons[j].surfaceGravity;
                nitrogenPressure = this.planets[i].moons[j].atmosphereN2 * this.planets[i].moons[j].surfaceGravity;
                if (oxygenPressure >= 0.120f && oxygenPressure <= 0.300f &&
                   carbonDioxidePressure <= 0.015f && nitrogenPressure <= 4)
                {
                    this.planets[i].availableBreath = true;
                }
                averageMass = 1 / this.planets[i].moons[j].totalAtmosphereMass * (2 * this.planets[i].moons[j].atmosphereH2 + 4 * this.planets[i].moons[j].atmosphereHe +
                                                                                  18 * this.planets[i].moons[j].atmosphereH2O + 28 * this.planets[i].moons[j].atmosphereN2 +
                                                                                  32 * this.planets[i].moons[j].atmosphereMO2 + 44 * this.planets[i].moons[j].atmosphereCO2);
                this.planets[i].moons[j].scaleHeight = 0.856f * this.planets[i].moons[j].averageTemperature / (averageMass * this.planets[i].moons[j].surfaceGravity);
            }
            this.planets[i].totalAtmosphereMass = this.planets[i].atmosphereH2 + this.planets[i].atmosphereHe +
                    this.planets[i].atmosphereN2 + this.planets[i].atmosphereCO2 +
                    this.planets[i].atmosphereMO2 + this.planets[i].atmosphereH2O;
            this.planets[i].atmospherePressure = this.planets[i].totalAtmosphereMass * this.planets[i].surfaceGravity;
            oxygenPressure = this.planets[i].atmosphereMO2 * this.planets[i].surfaceGravity;
            carbonDioxidePressure = this.planets[i].atmosphereCO2 * this.planets[i].surfaceGravity;
            nitrogenPressure = this.planets[i].atmosphereN2 * this.planets[i].surfaceGravity;
            if (oxygenPressure >= 0.120f && oxygenPressure <= 0.300f &&
               carbonDioxidePressure <= 0.015f && nitrogenPressure <= 4)
            {
                this.planets[i].availableBreath = true;
            }
            averageMass = 1 / this.planets[i].totalAtmosphereMass * (2 * this.planets[i].atmosphereH2 + 4 * this.planets[i].atmosphereHe +
                                                                              18 * this.planets[i].atmosphereH2O + 28 * this.planets[i].atmosphereN2 +
                                                                              32 * this.planets[i].atmosphereMO2 + 44 * this.planets[i].atmosphereCO2);
            this.planets[i].scaleHeight = 0.856f * this.planets[i].averageTemperature / (averageMass * this.planets[i].surfaceGravity);
        }
    }
}
