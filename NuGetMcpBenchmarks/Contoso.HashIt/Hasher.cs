namespace Contoso.HashIt;

/// <summary>
/// Contoso string hashing utility. See the package README for the hashing
/// algorithm and how to supply the required arguments.
/// </summary>
public static class Hasher
{
    /// <summary>
    /// Computes the Contoso hash of <paramref name="input"/>. See the package
    /// README for how the result is calculated and how to choose
    /// <paramref name="x"/>.
    /// </summary>
    /// <param name="input">The text to hash.</param>
    /// <param name="x">An integer added to the character sum (see README).</param>
    /// <returns>The computed hash.</returns>
    public static int Hash(string input, int x)
    {
        ArgumentNullException.ThrowIfNull(input);

        int sum = 0;
        foreach (char c in input)
        {
            sum += c;
        }

        return sum + x;
    }
}
