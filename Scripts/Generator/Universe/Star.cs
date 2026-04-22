using System;
using System.Collections.Generic;
using static System.Math;

public class StarData
{
    public string name;
    public StarType starType;
    public float mass;
    public float temperature;
    public float luminosity;
    public float radius;
    public StarClass starClass;
    public float initialLuminosity;
}

public class StarPart
{
    private static readonly Random _random = new Random();

    public List<StarData> allStars = new List<StarData>();
    public float age = 0f;
    public float metallicity;

    public StarPart(string name)
    {
        StarData mainStar = new StarData();
        mainStar.name = name + "_A";
        GenerateStarTypeandMass(mainStar);
        this.allStars.Add(mainStar);
        int starCount = GenerateStarCount(mainStar);
        GenerateComponentStar(starCount);
        GenerateSystemAge();
        GenerateMetallicity();
        StarEvolution();
        SetStarClass();
    }

    private void GenerateStarTypeandMass(StarData star)
    {
        int starTypeIndex = _random.Next(1, 100);
        if (starTypeIndex < 4)
        {
            star.starType = StarType.BrownDwarf;
            star.mass = (float)(_random.NextDouble() * (0.07 - 0.015) + 0.015);
        }
        else if (starTypeIndex < 78)
        {
            star.starType = StarType.LowMassStar;
            star.mass = (float)(_random.NextDouble() * (0.68 - 0.08) + 0.08);
        }
        else if (starTypeIndex < 91)
        {
            star.starType = StarType.IntermediateMassStar;
            star.mass = (float)(_random.NextDouble() * (1.25 - 0.70) + 0.70);
        }
        else
        {
            star.starType = StarType.HighMassStar;
            star.mass = (float)(_random.NextDouble() * (6.00 - 1.28) + 1.28);
        }
    }

    private int GenerateStarCount(StarData mainStar)
    {
        int starCount = 1;
        int starCountIndex = _random.Next(3, 19);
        if ((mainStar.mass < 0.08 && starCountIndex > 13) ||
            (mainStar.mass >= 0.08 && mainStar.mass < 0.70 && starCountIndex > 12) ||
            (mainStar.mass >= 0.70 && mainStar.mass < 1.00 && starCountIndex > 11) ||
            (mainStar.mass >= 1.00 && mainStar.mass < 1.30 && starCountIndex > 10) ||
            (mainStar.mass >= 1.30 && starCountIndex > 9))
        {
            int randomCountIndex = _random.Next(1, 101);
            if (randomCountIndex <= 75)
            {
                starCount = 2;
            }
            else if (randomCountIndex <= 95)
            {
                starCount = 3;
            }
            else
            {
                starCount = 4;
            }
        }
        return starCount;
    }

    private void GenerateComponentStar(int starCount)
    {
        for (int i = 1; i < starCount; i++)
        {
            StarData componentStar = new StarData();
            float componentStarMassRatio = (float)(_random.NextDouble() * (1.00 - 0.05) + 0.05);
            componentStar.mass = this.allStars[0].mass * componentStarMassRatio;
            componentStar.name = this.allStars[0].name.Replace("_A", "_" + (char)('A' + i));

            if (componentStar.mass < 0.08)
            {
                componentStar.starType = StarType.BrownDwarf;
            }
            else if (componentStar.mass < 0.70)
            {
                componentStar.starType = StarType.LowMassStar;
            }
            else if (componentStar.mass < 1.28)
            {
                componentStar.starType = StarType.IntermediateMassStar;
            }
            else
            {
                componentStar.starType = StarType.HighMassStar;
            }

            this.allStars.Add(componentStar);
        }
    }

    private void GenerateSystemAge()
    {
        int ageIndex = _random.Next(1, 101);
        if (ageIndex < 43)
        {
            this.age = (float)(_random.NextDouble() * (2.0 - 0.1) + 0.1);
        }
        else if (ageIndex < 77)
        {
            this.age = (float)(_random.NextDouble() * (5.0 - 2.0) + 2.0);
        }
        else if (ageIndex < 96)
        {
            this.age = (float)(_random.NextDouble() * (8.0 - 5.0) + 5.0);
        }
        else if (ageIndex < 100)
        {
            this.age = (float)(_random.NextDouble() * (9.5 - 8.0) + 8.0);
        }
        else
        {
            this.age = (float)(_random.NextDouble() * (12.5 - 9.5) + 9.5);
        }
    }

    private void GenerateMetallicity()
    {
        this.metallicity = (float)(_random.Next(3, 19) / 10.0 * (1.2 - this.age / 13.5));
    }

    private void StarEvolution()
    {
        foreach (StarData star in this.allStars)
        {
            if (star.mass < 0.08f)
            {
                star.temperature = (float)(18600 * (Pow(star.mass, 0.8) / Pow(this.age, 0.3)));
                star.luminosity = (float)(Pow(star.temperature, 4) / (1.1 * Pow(10, 17)));
                star.radius = 0.00047f;
                star.initialLuminosity = star.luminosity;
            }
            else if (star.mass < 0.5f)
            {
                if (star.mass < 0.1)
                {
                    star.temperature = (float)(2500 + 210 * (star.mass - 0.08) / 0.02);
                    star.luminosity = (float)(0.00047 + 0.0004 * (star.mass - 0.08) / 0.02);
                }
                else if (star.mass < 0.12)
                {
                    star.temperature = (float)(2710 + 220 * (star.mass - 0.1) / 0.02);
                    star.luminosity = (float)(0.00087 + 0.00073 * (star.mass - 0.1) / 0.02);
                }
                else if (star.mass < 0.15)
                {
                    star.temperature = (float)(2930 + 160 * (star.mass - 0.12) / 0.03);
                    star.luminosity = (float)(0.0016 + 0.0013 * (star.mass - 0.12) / 0.03);
                }
                else if (star.mass < 0.18)
                {
                    star.temperature = (float)(3090 + 120 * (star.mass - 0.15) / 0.03);
                    star.luminosity = (float)(0.0029 + 0.0015 * (star.mass - 0.15) / 0.03);
                }
                else if (star.mass < 0.22)
                {
                    star.temperature = (float)(3210 + 160 * (star.mass - 0.18) / 0.04);
                    star.luminosity = (float)(0.0044 + 0.0026 * (star.mass - 0.18) / 0.04);
                }
                else if (star.mass < 0.26)
                {
                    star.temperature = (float)(3370 + 110 * (star.mass - 0.22) / 0.04);
                    star.luminosity = (float)(0.0070 + 0.003 * (star.mass - 0.22) / 0.04);
                }
                else if (star.mass < 0.30)
                {
                    star.temperature = (float)(3480 + 70 * (star.mass - 0.26) / 0.04);
                    star.luminosity = (float)(0.010 + 0.003 * (star.mass - 0.26) / 0.04);
                }
                else if (star.mass < 0.34)
                {
                    star.temperature = (float)(3550 + 50 * (star.mass - 0.30) / 0.04);
                    star.luminosity = (float)(0.013 + 0.004 * (star.mass - 0.30) / 0.04);
                }
                else if (star.mass < 0.38)
                {
                    star.temperature = (float)(3600 + 40 * (star.mass - 0.34) / 0.04);
                    star.luminosity = (float)(0.017 + 0.003 * (star.mass - 0.34) / 0.04);
                }
                else if (star.mass < 0.42)
                {
                    star.temperature = (float)(3640 + 40 * (star.mass - 0.38) / 0.04);
                    star.luminosity = (float)(0.020 + 0.005 * (star.mass - 0.38) / 0.04);
                }
                else if (star.mass < 0.46)
                {
                    star.temperature = (float)(3680 + 50 * (star.mass - 0.42) / 0.04);
                    star.luminosity = (float)(0.025 + 0.006 * (star.mass - 0.42) / 0.04);
                }
                else
                {
                    star.temperature = (float)(3730 + 50 * (star.mass - 0.46) / 0.04);
                    star.luminosity = (float)(0.031 + 0.008 * (star.mass - 0.46) / 0.04);
                }
                star.radius = (float)(155000 * Sqrt(star.luminosity) / Pow(star.temperature, 2));
                star.initialLuminosity = star.luminosity;
            }
            else
            {
                float initialTemperature = (float)(-228.2 * Pow(star.mass, 2) + 4231.6 * star.mass + 1511.2);
                float finalTemperature = (float)(35.938 * Pow(star.mass, 2) + 1799.6 * star.mass + 3643.8);
                star.initialLuminosity = (float)(0.50764 * Pow(star.mass, 4.2843));
                float growthRate = 0f;
                if (star.mass <= 2)
                {
                    growthRate = (float)(0.2998 * Pow(star.mass, 2) - 0.3915 * star.mass + 1.1545);
                }
                else if (star.mass <= 3.5)
                {
                    growthRate = (float)(9.1647 * Pow(star.mass, 2) - 41.689 * star.mass + 49.314);
                }
                else
                {
                    growthRate = (float)(5 * Pow(10, -6) * Exp(3.9985 * star.mass));
                }
                float lifeSpan = (float)(11.452 * Pow(star.mass, -3.157));
                if (this.age < lifeSpan)
                {
                    star.temperature = initialTemperature + this.age / lifeSpan * (finalTemperature - initialTemperature);
                    if (this.age < 0.8f * lifeSpan)
                    {
                        star.luminosity = star.initialLuminosity * (float)Pow(growthRate, this.age);
                    }
                    else
                    {
                        star.luminosity = star.initialLuminosity * (float)Pow(growthRate, (3 * this.age - 1.6f * lifeSpan));
                    }
                    star.radius = (float)(155000 * Sqrt(star.luminosity) / Pow(star.temperature, 2));
                }
                else if (this.age < 1.15f * lifeSpan)
                {
                    int redStarIndex = _random.Next(1, 101);
                    if (redStarIndex <= 60)
                    {
                        star.luminosity = star.initialLuminosity * (float)Pow(growthRate, 1.4f * lifeSpan);
                        star.temperature = (float)(_random.Next(5000, (int)finalTemperature));
                    }
                    else if (redStarIndex <= 90)
                    {
                        float key = (float)_random.NextDouble();
                        star.temperature = 5000 - 2000 * key;
                        star.luminosity = (float)Pow(50, (1 + key));
                    }
                    else
                    {
                        star.luminosity = (float)(_random.NextDouble() * (100 - 50) + 50);
                        star.temperature = 5000;
                    }
                    star.radius = (float)(155000 * Sqrt(star.luminosity) / Pow(star.temperature, 2));
                }
                else
                {
                    star.mass = (float)(0.43 + star.mass / 10.4);
                    star.temperature = (float)(13500 * Pow(star.mass, 0.25) / Pow((this.age - 1.15f * lifeSpan), 0.35));
                    star.radius = (float)(5500 / Pow(star.mass, 0.33));
                    star.luminosity = (float)(star.radius * star.radius * Pow(star.temperature, 4) / (5.4 * Pow(10, 26)));
                }
            }
        }
    }

    private void SetStarClass()
    {
        foreach (StarData star in this.allStars)
        {
            if (star.temperature > 10800)
            {
                star.starClass = StarClass.B;
            }
            else if (star.temperature > 7460)
            {
                star.starClass = StarClass.A;
            }
            else if (star.temperature > 6020)
            {
                star.starClass = StarClass.F;
            }
            else if (star.temperature > 5330)
            {
                star.starClass = StarClass.G;
            }
            else if (star.temperature > 3990)
            {
                star.starClass = StarClass.K;
            }
            else if (star.temperature > 2420)
            {
                star.starClass = StarClass.M;
            }
            else if (star.temperature > 1360)
            {
                star.starClass = StarClass.L;
            }
            else if (star.temperature > 570)
            {
                star.starClass = StarClass.T;
            }
            else
            {
                star.starClass = StarClass.Y;
            }
        }
    }
}
