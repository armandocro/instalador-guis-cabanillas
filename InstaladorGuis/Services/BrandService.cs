using System.IO;
using System.Text.Json;
using InstaladorGuis.Models;

namespace InstaladorGuis.Services;

internal static class BrandService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static BrandConfig Load(string[] args)
    {
        var marca = DetectarMarca(args);
        var ruta = System.IO.Path.Combine(AppContext.BaseDirectory, "Brands", marca.ToLowerInvariant() + ".json");
        if (!File.Exists(ruta))
        {
            throw new FileNotFoundException("No se encontró la configuración de marca: " + ruta);
        }

        var json = File.ReadAllText(ruta);
        var config = JsonSerializer.Deserialize<BrandConfig>(json, JsonOptions)
            ?? throw new InvalidOperationException("Configuración de marca inválida.");

        config.GuiToFolderMapping = new Dictionary<string, List<string>>(
            config.GuiToFolderMapping, StringComparer.OrdinalIgnoreCase);
        config.GuiToShortcutMapping = new Dictionary<string, List<string>>(
            config.GuiToShortcutMapping, StringComparer.OrdinalIgnoreCase);
        return config;
    }

    public static string DetectarMarca(string[] args)
    {
        var argBrand = args.FirstOrDefault(a => a.StartsWith("--brand=", StringComparison.OrdinalIgnoreCase));
        if (argBrand != null)
        {
            var valor = argBrand.Split('=')[1].ToUpperInvariant();
            if (valor is BrandIds.PullBear or BrandIds.ZaraHome) return valor;
        }

        var env = Environment.GetEnvironmentVariable("GUIS_BRAND");
        if (!string.IsNullOrWhiteSpace(env))
        {
            var valor = env.Trim().ToUpperInvariant();
            if (valor is BrandIds.PullBear or BrandIds.ZaraHome) return valor;
        }

        var exe = System.IO.Path.GetFileName(Environment.ProcessPath ?? "").ToLowerInvariant();
        if (exe.Contains("zh") || exe.Contains("zara")) return BrandIds.ZaraHome;
        if (exe.Contains("pb") || exe.Contains("p&b") || exe.Contains("pull")) return BrandIds.PullBear;

        var dir = AppContext.BaseDirectory.Replace('/', '\\').TrimEnd('\\').ToUpperInvariant();
        if (dir.EndsWith("\\ZH") || dir.Contains("\\ZH\\") || dir.Contains("\\DESPLIEGUE\\ZH") || dir.Contains("\\GUIS-ZH"))
            return BrandIds.ZaraHome;
        if (dir.EndsWith("\\PB") || dir.Contains("\\PB\\") || dir.Contains("\\DESPLIEGUE\\PB") || dir.Contains("\\GUIS-P&B") || dir.Contains("\\GUIS-PB"))
            return BrandIds.PullBear;

        var cwd = Environment.CurrentDirectory.ToLowerInvariant();
        if (cwd.Contains("guis-zh")) return BrandIds.ZaraHome;
        if (cwd.Contains("guis-p&b") || cwd.Contains("guis-pb")) return BrandIds.PullBear;

        return BrandIds.PullBear;
    }
}
