using System;

namespace VStancer.Client;

public static class MathUtil
{
    public static bool WithinEpsilon(float a, float b, float epsilon) => Math.Abs(a - b) <= epsilon;
}