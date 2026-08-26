using InstaladorGuis.Models;

namespace InstaladorGuis.Tests;

public class MetricsDataTests
{
    [Fact]
    public void MetricsData_DefaultValues()
    {
        var data = new MetricsData();
        Assert.Equal("", data.Identificador);
        Assert.Equal("", data.UltimaActualizacion);
        Assert.NotNull(data.MetricasGenerales);
        Assert.NotNull(data.InstalacionesPorGui);
        Assert.Empty(data.InstalacionesPorGui);
        Assert.NotNull(data.DesinstalacionesPorGui);
        Assert.Empty(data.DesinstalacionesPorGui);
        Assert.NotNull(data.HistorialEventos);
        Assert.Empty(data.HistorialEventos);
    }

    [Fact]
    public void OperationSummary_TracksSuccessesAndFailures()
    {
        var summary = new OperationSummary { Total = 3, EsInstalacion = true };
        summary.Exitosos.Add("gui1");
        summary.Exitosos.Add("gui2");
        summary.Fallidos.Add("gui3");

        Assert.Equal(3, summary.Total);
        Assert.True(summary.EsInstalacion);
        Assert.Equal(2, summary.Exitosos.Count);
        Assert.Single(summary.Fallidos);
    }

    [Fact]
    public void GuiStatus_Enum_HasAllValues()
    {
        Assert.Equal(0, (int)GuiStatus.Unknown);
        Assert.Equal(1, (int)GuiStatus.Installed);
        Assert.Equal(2, (int)GuiStatus.NotInstalled);
        Assert.Equal(3, (int)GuiStatus.Checking);
    }
}
