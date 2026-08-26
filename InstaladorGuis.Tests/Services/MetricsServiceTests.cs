using System.IO;
using System.Text.Json;
using InstaladorGuis.Models;
using InstaladorGuis.Services;

namespace InstaladorGuis.Tests.Services;

public class MetricsServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _metricsFile;

    public MetricsServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "InstaladorGuisTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _metricsFile = Path.Combine(_tempDir, "metricas-test.json");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch { /* cleanup best effort */ }
    }

    private MetricsService CreateService()
    {
        var brand = new BrandConfig
        {
            Id = BrandIds.PullBear,
            ArchivoMetricas = Path.GetFileName(_metricsFile),
            RutaMetricasBase = [_tempDir]
        };
        return new MetricsService(brand);
    }

    [Fact]
    public void Inicializar_CreatesMetricsFile()
    {
        var service = CreateService();
        Assert.True(service.Inicializar());
        Assert.True(service.Inicializado);
        Assert.True(File.Exists(_metricsFile));
    }

    [Fact]
    public void RegistrarAperturaApp_PersistsEvent()
    {
        var service = CreateService();
        Assert.True(service.Inicializar());
        service.RegistrarAperturaApp();

        var json = File.ReadAllText(_metricsFile);
        var data = JsonSerializer.Deserialize<MetricsData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(data);
        Assert.True(data!.MetricasGenerales.AperturasApp >= 1);
    }

    [Fact]
    public void RegistrarInstalacionGUI_IncrementsCounter()
    {
        var service = CreateService();
        Assert.True(service.Inicializar());
        service.RegistrarInstalacionGUI("cla1");
        service.RegistrarInstalacionGUI("cla1");

        var json = File.ReadAllText(_metricsFile);
        Assert.Contains("cla1", json);
    }
}
