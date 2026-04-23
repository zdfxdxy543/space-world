using System;
using Godot;

public static class MathX
{
    public static float Pow(float x, float y)
    {
        return (float)Math.Pow(x, y);
    }

    public static float Pow(int x, int y)
    {
        return (float)Math.Pow(x, y);
    }

    public static double Pow(double x, double y)
    {
        return Math.Pow(x, y);
    }

    public static float Sqrt(float x)
    {
        return (float)Math.Sqrt(x);
    }

    public static float Sqrt(int x)
    {
        return (float)Math.Sqrt(x);
    }

    public static double Sqrt(double x)
    {
        return Math.Sqrt(x);
    }

    public static float Max(float x, float y)
    {
        return Math.Max(x, y);
    }

    public static int Max(int x, int y)
    {
        return Math.Max(x, y);
    }

    public static double Max(double x, double y)
    {
        return Math.Max(x, y);
    }

    public static float Min(float x, float y)
    {
        return Math.Min(x, y);
    }

    public static int Min(int x, int y)
    {
        return Math.Min(x, y);
    }

    public static double Min(double x, double y)
    {
        return Math.Min(x, y);
    }

    public static float Floor(float x)
    {
        return (float)Math.Floor(x);
    }

    public static double Floor(double x)
    {
        return Math.Floor(x);
    }

    public static float Ceiling(float x)
    {
        return (float)Math.Ceiling(x);
    }

    public static double Ceiling(double x)
    {
        return Math.Ceiling(x);
    }

    public static float Round(float x)
    {
        return (float)Math.Round(x);
    }

    public static double Round(double x)
    {
        return Math.Round(x);
    }

    public static float Log10(float x)
    {
        return (float)Math.Log10(x);
    }

    public static double Log10(double x)
    {
        return Math.Log10(x);
    }

    public static float Exp(float x)
    {
        return (float)Math.Exp(x);
    }

    public static double Exp(double x)
    {
        return Math.Exp(x);
    }

    public static float Abs(float x)
    {
        return Math.Abs(x);
    }

    public static int Abs(int x)
    {
        return Math.Abs(x);
    }

    public static double Abs(double x)
    {
        return System.Math.Abs(x);
    }

    public static float DegToRad(float degrees)
    {
        return degrees * Mathf.Pi / 180f;
    }

    public static double DegToRad(double degrees)
    {
        return degrees * System.Math.PI / 180.0;
    }
}
