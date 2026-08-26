using System.IO;
using InstaladorGuis.Services;

namespace InstaladorGuis.Tests;

public class PathServiceTests
{
    [Theory]
    [InlineData("1.0.0", "1.0.0", 0)]
    [InlineData("1.0.0", "1.0.1", -1)]
    [InlineData("1.0.1", "1.0.0", 1)]
    [InlineData("1.0", "1.0.0", 0)]
    [InlineData("2.0.0", "1.9.9", 1)]
    [InlineData("1.9.9", "2.0.0", -1)]
    [InlineData("1.0.0", "1.0", 0)]
    [InlineData("10.0.0", "2.0.0", 1)]
    [InlineData("0.1.0", "0.0.9", 1)]
    public void CompareVersions_ReturnsExpected(string v1, string v2, int expected)
    {
        var result = PathService.CompareVersions(v1, v2);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void CompareVersions_HandlesEmptyAndNull(string? v1, string? v2)
    {
        if (v1 == null || v2 == null)
        {
            var result = PathService.CompareVersions(v1 ?? "", v2 ?? "");
            Assert.IsType<int>(result);
        }
        else
        {
            var result = PathService.CompareVersions(v1, v2);
            Assert.Equal(0, result);
        }
    }

    [Fact]
    public void DecodeSpecialCharacters_FixesMojibake()
    {
        var input = "acciÃ³n";
        var result = PathService.DecodeSpecialCharacters(input);
        Assert.Contains("ó", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void DecodeSpecialCharacters_ReturnsInputWhenEmpty(string? input)
    {
        var result = PathService.DecodeSpecialCharacters(input!);
        Assert.Equal(input, result);
    }

    [Fact]
    public void ResolverArchivo_ReturnsNullWhenNoFilesExist()
    {
        var bases = new[] { @"C:\nonexistent_path_12345", @"C:\another_nonexistent_67890" };
        var result = PathService.ResolverArchivo(bases, "nonexistent.json");
        Assert.Null(result);
    }

    [Fact]
    public void ResolverArchivo_ReturnsFirstExistingFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "test_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);
        try
        {
            var testFile = Path.Combine(tempDir, "test.json");
            File.WriteAllText(testFile, "{}");

            var bases = new[] { @"C:\nonexistent", tempDir };
            var result = PathService.ResolverArchivo(bases, "test.json");
            Assert.Equal(testFile, result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ResolverArchivo_ReturnsNullForEmptyBases()
    {
        var result = PathService.ResolverArchivo(Array.Empty<string>(), "test.json");
        Assert.Null(result);
    }
}
