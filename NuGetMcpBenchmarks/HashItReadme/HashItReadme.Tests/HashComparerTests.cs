using Xunit;

namespace HashItReadme.Tests;

public sealed class HashComparerTests
{
    private readonly HashComparer _comparer = new();

    [Fact]
    public void AreEqual_ReturnsTrue_WhenOffsetsBalanceDifferentInputs()
    {
        Assert.True(_comparer.AreEqual("abc", 10, "abd", 9));
    }

    [Fact]
    public void AreEqual_ReturnsTrue_ForAnagramsWithSameOffset()
    {
        Assert.True(_comparer.AreEqual("ab", 5, "ba", 5));
    }

    [Fact]
    public void AreEqual_ReturnsTrue_ForEmptyStringsWithSameOffset()
    {
        Assert.True(_comparer.AreEqual("", 5, "", 5));
    }

    [Fact]
    public void AreEqual_ReturnsFalse_ForDifferentHashes()
    {
        Assert.False(_comparer.AreEqual("abc", 0, "xyz", 0));
    }

    [Fact]
    public void AreEqual_ReturnsFalse_WhenOffsetsDiffer()
    {
        Assert.False(_comparer.AreEqual("abc", 1, "abc", 2));
    }
}
