using System.Collections.Generic;

public class StarSystemData
{
    public string name;
    public StarPart starPart;
    public PlanetPart planetPart;

    public StarSystemData()
    {
        this.name = StarNameGenerator.GenerateUnique();
        this.starPart = new StarPart(this.name);
        this.planetPart = new PlanetPart(this.name, this.starPart);
    }
}
