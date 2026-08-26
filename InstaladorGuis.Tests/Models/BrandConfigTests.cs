using InstaladorGuis.Models;

namespace InstaladorGuis.Tests;

public class BrandConfigTests
{
    [Fact]
    public void GetLabel_ReturnsLabelWhenGuiExists()
    {
        var config = new BrandConfig
        {
            Guis =
            [
                new GuiItem { Id = "cla1", Label = "Clasificador 1" },
                new GuiItem { Id = "car", Label = "CAR" }
            ]
        };

        Assert.Equal("Clasificador 1", config.GetLabel("cla1"));
        Assert.Equal("CAR", config.GetLabel("car"));
    }

    [Fact]
    public void GetLabel_ReturnsIdWhenGuiNotFound()
    {
        var config = new BrandConfig
        {
            Guis = [new GuiItem { Id = "cla1", Label = "Clasificador 1" }]
        };

        Assert.Equal("nonexistent", config.GetLabel("nonexistent"));
    }

    [Fact]
    public void GetLabel_IsCaseInsensitive()
    {
        var config = new BrandConfig
        {
            Guis = [new GuiItem { Id = "CLA1", Label = "Clasificador 1" }]
        };

        Assert.Equal("Clasificador 1", config.GetLabel("cla1"));
    }

    [Fact]
    public void GetLabel_ReturnsIdForEmptyGuisList()
    {
        var config = new BrandConfig { Guis = [] };
        Assert.Equal("anything", config.GetLabel("anything"));
    }

    [Theory]
    [InlineData("package", false)]
    [InlineData("clothing", true)]
    [InlineData("Package", false)]
    [InlineData("CLOTHING", true)]
    [InlineData("other", false)]
    [InlineData("", false)]
    public void GuiItem_EsPrendas_DetectsClothingType(string tipo, bool expected)
    {
        var gui = new GuiItem { Tipo = tipo };
        Assert.Equal(expected, gui.EsPrendas);
    }

    [Fact]
    public void BrandConfig_DefaultValues()
    {
        var config = new BrandConfig();
        Assert.Equal(BrandIds.PullBear, config.Id);
        Assert.Equal("", config.Nombre);
        Assert.Equal("", config.Version);
        Assert.NotNull(config.Guis);
        Assert.Empty(config.Guis);
    }
}
