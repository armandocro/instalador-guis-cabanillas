using InstaladorGuis.Models;
using InstaladorGuis.Services;

namespace InstaladorGuis.Tests.Services;

public class BrandServiceTests
{
    [Theory]
    [InlineData("--brand=PB", BrandIds.PullBear)]
    [InlineData("--brand=pb", BrandIds.PullBear)]
    [InlineData("--brand=ZH", BrandIds.ZaraHome)]
    [InlineData("--brand=zh", BrandIds.ZaraHome)]
    public void DetectarMarca_FromArgs(string arg, string expected)
    {
        Assert.Equal(expected, BrandService.DetectarMarca([arg]));
    }

    [Fact]
    public void DetectarMarca_ArgsTakePrecedenceOverEnv()
    {
        var previous = Environment.GetEnvironmentVariable("GUIS_BRAND");
        try
        {
            Environment.SetEnvironmentVariable("GUIS_BRAND", BrandIds.ZaraHome);
            Assert.Equal(BrandIds.PullBear, BrandService.DetectarMarca(["--brand=PB"]));
        }
        finally
        {
            Environment.SetEnvironmentVariable("GUIS_BRAND", previous);
        }
    }

    [Fact]
    public void DetectarMarca_FromEnvironment()
    {
        var previous = Environment.GetEnvironmentVariable("GUIS_BRAND");
        try
        {
            Environment.SetEnvironmentVariable("GUIS_BRAND", "ZH");
            Assert.Equal(BrandIds.ZaraHome, BrandService.DetectarMarca([]));
        }
        finally
        {
            Environment.SetEnvironmentVariable("GUIS_BRAND", previous);
        }
    }

    [Fact]
    public void DetectarMarca_DefaultsToPullBearWithoutHints()
    {
        var previous = Environment.GetEnvironmentVariable("GUIS_BRAND");
        try
        {
            Environment.SetEnvironmentVariable("GUIS_BRAND", null);
            var marca = BrandService.DetectarMarca([]);
            Assert.True(marca is BrandIds.PullBear or BrandIds.ZaraHome);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GUIS_BRAND", previous);
        }
    }
}
