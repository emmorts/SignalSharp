using System.Numerics;
using SignalSharp.Utilities;

namespace SignalSharp.Tests;

public static class AssertionUtils
{
    public static void AssertEqualWithin<T>(T[] expected, T[] actual)
        where T : IFloatingPoint<T>
    {
        Assert.That(actual, Has.Length.EqualTo(expected.Length), "Array lengths differ.");
        Assert.Multiple(() =>
        {
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That(actual[i], Is.EqualTo(expected[i]).Within(NumericUtils.GetDefaultEpsilon<T>()), $"Mismatch at index {i}");
            }
        });
    }
}
