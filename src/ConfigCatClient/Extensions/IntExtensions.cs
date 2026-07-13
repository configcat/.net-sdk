namespace System;

internal static class IntExtensions
{
    /// <summary>
    /// The number of digits in a non-negative number.
    /// </summary>
    /// <remarks>
    /// Returns 1 for negative numbers.
    /// </remarks>
    public static int Digits(this int n)
    {
        // Based on: https://stackoverflow.com/a/51099524/268898

        if (n < 10) return 1;
        if (n < 100) return 2;
        if (n < 1_000) return 3;
        if (n < 10_000) return 4;
        if (n < 100_000) return 5;
        if (n < 1_000_000) return 6;
        if (n < 10_000_000) return 7;
        if (n < 100_000_000) return 8;
        if (n < 1_000_000_000) return 9;
        return 10;
    }
}
