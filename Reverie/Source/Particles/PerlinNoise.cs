using System;

namespace Reverie.Source;

using Microsoft.Xna.Framework;

public static class PerlinNoise
{
    private readonly static int[] Permutation = GeneratePermutation();
    
    private static int[] GeneratePermutation()
    {
        var p = new int[512];
        var perm = new int[256];
        var random = new Random(0); // Fixed seed for consistent noise
        
        for (var i = 0; i < 256; i++)
            perm[i] = i;
        
        // Shuffle (Fisher-Yates)
        for (var i = 0; i < 256; i++)
        {
            var j = random.Next(256);
            (perm[i], perm[j]) = (perm[j], perm[i]);
        }
        
        // Duplicate for wrapping
        for (var i = 0; i < 512; i++)
            p[i] = perm[i % 256];
        
        return p;
    }
    
    public static float Noise(float x, float y, float z = 0f)
    {
        // Find unit cube that contains point
        var X = (int)MathF.Floor(x) & 255;
        var Y = (int)MathF.Floor(y) & 255;
        var Z = (int)MathF.Floor(z) & 255;
        
        // Find relative position in cube
        x -= MathF.Floor(x);
        y -= MathF.Floor(y);
        z -= MathF.Floor(z);
        
        // Compute fade curves
        var u = Fade(x);
        var v = Fade(y);
        var w = Fade(z);
        
        // Hash coordinates of cube corners
        var A = Permutation[X] + Y;
        var AA = Permutation[A] + Z;
        var AB = Permutation[A + 1] + Z;
        var B = Permutation[X + 1] + Y;
        var BA = Permutation[B] + Z;
        var BB = Permutation[B + 1] + Z;
        
        // Blend results from 8 corners
        return Lerp(w,
            Lerp(v,
                Lerp(u, Grad(Permutation[AA], x, y, z), Grad(Permutation[BA], x - 1, y, z)),
                Lerp(u, Grad(Permutation[AB], x, y - 1, z), Grad(Permutation[BB], x - 1, y - 1, z))),
            Lerp(v,
                Lerp(u, Grad(Permutation[AA + 1], x, y, z - 1), Grad(Permutation[BA + 1], x - 1, y, z - 1)),
                Lerp(u, Grad(Permutation[AB + 1], x, y - 1, z - 1), Grad(Permutation[BB + 1], x - 1, y - 1, z - 1))));
    }
    
    // Fade function: 6t^5 - 15t^4 + 10t^3
    private static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);
    
    // Linear interpolation
    private static float Lerp(float t, float a, float b) => a + t * (b - a);
    
    // Gradient function
    private static float Grad(int hash, float x, float y, float z)
    {
        var h = hash & 15;
        var u = h < 8 ? x : y;
        var v = h < 4 ? y : h == 12 || h == 14 ? x : z;
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }
    
    // Get flow direction for particles (2D)
    public static Vector2 GetFlowDirection(Vector2 position, float time, float scale = 0.01f)
    {
        var angle = Noise(position.X * scale, position.Y * scale, time) * MathF.PI * 2f;
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle));
    }
}