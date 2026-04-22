using Godot;

public readonly struct CraterDescriptor
{
    public readonly Vector3 Center;
    public readonly float RadiusChord;
    public readonly float Depth;
    public readonly float RimHeight;

    public CraterDescriptor(Vector3 center, float radiusChord, float depth, float rimHeight)
    {
        Center = center;
        RadiusChord = radiusChord;
        Depth = depth;
        RimHeight = rimHeight;
    }
}
