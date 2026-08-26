using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using InstaladorGuis.Models;

namespace InstaladorGuis.Services;

internal sealed class InstallerService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly Regex UrlPattern = new(@"^https?://[\w\-._~:/?#\[\]@!$&'()*+,;=%]+$", RegexOptions.Compiled);
    private readonly BrandConfig _brand;
    private readonly MetricsService _metrics;
    public Dictionary<string, string> UrlMapping { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

    public InstallerService(BrandConfig brand, MetricsService metrics)
    {
        _brand = brand;
        _metrics = metrics;
    }

    internal static bool IsValidUrl(string url) =>
        !string.IsNullOrWhiteSpace(url) && url.Length <= 2048 && UrlPattern.IsMatch(url);

    internal static string SanitizeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("La URL no puede estar vacia.");
        var sanitized = url.Trim();
        sanitized = sanitized.Replace("\"", "").Replace("'", "").Replace("`", "")
                             .Replace("\\", "").Replace("&", "").Replace(";", "")
                             .Replace("|", "").Replace(">", "").Replace("<", "");
        return sanitized;
    }

    public bool CargarUrlMapping(out string mensajeError)
    {
        mensajeError = "";
        var ruta = PathService.ResolverArchivo(_brand.RutasRedBase, _brand.ArchivoMapping);
        if (ruta == null)
        {
            mensajeError = "No se pudo encontrar el archivo de configuración.\n\nRutas probadas:\n" +
                string.Join("\n", _brand.RutasRedBase.Select(r => "• " + System.IO.Path.Combine(r, _brand.ArchivoMapping)));
            return false;
        }
        try
        {
            var data = JsonSerializer.Deserialize<UrlMappingFile>(File.ReadAllText(ruta), JsonOptions);
            if (data?.UrlMapping == null || data.UrlMapping.Count == 0)
            {
                mensajeError = "El archivo JSON no tiene la estructura esperada (urlMapping).";
                return false;
            }
            UrlMapping = new Dictionary<string, string>(data.UrlMapping, StringComparer.OrdinalIgnoreCase);
            return true;
        }
        catch (Exception ex)
        {
            mensajeError = "Error al cargar la configuración:\n\n" + ex.Message;
            return false;
        }
    }

    public VersionInfo? BuscarActualizacion()
    {
        var ruta = PathService.ResolverArchivo(_brand.RutasRedBase, _brand.ArchivoVersion);
        if (ruta == null) return null;
        try
        {
            var data = JsonSerializer.Deserialize<VersionInfo>(PathService.DecodeSpecialCharacters(File.ReadAllText(ruta)), JsonOptions);
            if (data == null || string.IsNullOrWhiteSpace(data.Version)) return null;
            return PathService.CompareVersions(_brand.Version, data.Version) < 0 ? data : null;
        }
        catch { return null; }
    }

    public bool VerificarAmigaLauncher() => File.Exists(PathService.AmigaLauncherPath);
    public string? ResolverActualizador() => PathService.ResolverArchivo(_brand.RutaActualizadorBase, _brand.ArchivoActualizador);

    public bool EstaInstalada(string guiId)
    {
        try
        {
            if (!Directory.Exists(PathService.AmigaAppsPath)) return false;
            return _brand.GuiToFolderMapping.TryGetValue(guiId, out var folders) &&
                   folders.Any(f => Directory.Exists(System.IO.Path.Combine(PathService.AmigaAppsPath, f)));
        }
        catch { return false; }
    }

    public OperationSummary Instalar(IReadOnlyList<string> componentes, IProgress<OperationProgress> progreso)
    {
        var resumen = new OperationSummary { Total = componentes.Count, EsInstalacion = true };
        var completed = 0;
        for (var index = 0; index < componentes.Count; index++)
        {
            var id = componentes[index];
            var label = _brand.GetLabel(id);
            if (!UrlMapping.TryGetValue(id, out var url) || string.IsNullOrWhiteSpace(url))
            {
                completed++;
                resumen.Fallidos.Add(id + " (sin URL)");
                Reportar(progreso, completed, componentes.Count, label + " (sin URL disponible)", id, GuiStatus.NotInstalled);
                Thread.Sleep(500);
                continue;
            }
            ReportarPaso(progreso, index, componentes.Count, completed, 2, "Desinstalando " + label + "...", id, GuiStatus.Checking);
            try
            {
                if (!IsValidUrl(url)) throw new InvalidOperationException("URL inválida para " + label);
                var safeUrl = SanitizeUrl(url);
                CommandService.Ejecutar("javaws", ["-uninstall", safeUrl], true);
                ReportarPaso(progreso, index, componentes.Count, completed, 3, "Instalando " + label + "...", id, GuiStatus.Checking);
                var install = CommandService.Ejecutar(PathService.AmigaLauncherShortPath, ["-install", "-silent", safeUrl], true);
                if (!install.Ok) throw new InvalidOperationException(install.Error ?? "Error en AmigaLauncher");
                completed++;
                resumen.Exitosos.Add(id);
                if (_metrics.Inicializado) _metrics.RegistrarInstalacionGUI(id);
                Reportar(progreso, completed, componentes.Count, label + " instalado correctamente", id, GuiStatus.Installed);
                Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                completed++;
                resumen.Fallidos.Add(id);
                if (_metrics.Inicializado) _metrics.RegistrarFalloInstalacion(id, ex.Message);
                Reportar(progreso, completed, componentes.Count, "Error instalando " + label, id, GuiStatus.NotInstalled);
                Thread.Sleep(1000);
            }
        }
        return resumen;
    }

    public OperationSummary Desinstalar(IReadOnlyList<string> componentes, IProgress<OperationProgress> progreso)
    {
        var resumen = new OperationSummary { Total = componentes.Count, EsInstalacion = false };
        var completed = 0;
        foreach (var id in componentes)
        {
            var label = _brand.GetLabel(id);
            if (!UrlMapping.TryGetValue(id, out var url) || string.IsNullOrWhiteSpace(url))
            {
                completed++;
                resumen.Fallidos.Add(id + " (sin URL)");
                Reportar(progreso, completed, componentes.Count, label + " (sin URL)", id, null);
                continue;
            }
            try
            {
                if (!IsValidUrl(url)) throw new InvalidOperationException("URL inválida para " + label);
                var safeUrl = SanitizeUrl(url);
                Reportar(progreso, completed, componentes.Count, "Desinstalando " + label + "...", id, GuiStatus.Checking);
                CommandService.Ejecutar(PathService.AmigaLauncherShortPath, ["-uninstall", "-silent", safeUrl], false);
                Thread.Sleep(250);
                _brand.GuiToFolderMapping.TryGetValue(id, out var folders);
                _brand.GuiToShortcutMapping.TryGetValue(id, out var shortcuts);
                if (folders != null)
                {
                    foreach (var folder in folders)
                    {
                        var folderPath = System.IO.Path.Combine(PathService.AmigaAppsPath, folder);
                        if (Directory.Exists(folderPath)) { Directory.Delete(folderPath, true); break; }
                    }
                }
                CommandService.Ejecutar("taskkill", ["/IM", "amglauncher.exe", "/F"], true);
                if (shortcuts != null)
                {
                    foreach (var shortcut in shortcuts)
                    {
                        var shortcutPath = System.IO.Path.Combine(PathService.DesktopPath, shortcut);
                        if (File.Exists(shortcutPath)) File.Delete(shortcutPath);
                    }
                }
                completed++;
                resumen.Exitosos.Add(id);
                if (_metrics.Inicializado) _metrics.RegistrarDesinstalacionGUI(id);
                Reportar(progreso, completed, componentes.Count, label + " desinstalado correctamente", id, GuiStatus.NotInstalled);
                Thread.Sleep(400);
            }
            catch (Exception ex)
            {
                completed++;
                resumen.Fallidos.Add(id);
                if (_metrics.Inicializado) _metrics.RegistrarFalloInstalacion(id, "Error desinstalando: " + ex.Message);
                Reportar(progreso, completed, componentes.Count, "Error desinstalando " + label, id, GuiStatus.Installed);
            }
        }
        return resumen;
    }

    public void InstalarLibre(string url, IProgress<OperationProgress> progreso)
    {
        if (!IsValidUrl(url)) throw new InvalidOperationException("La URL proporcionada no es válida.");
        var safeUrl = SanitizeUrl(url);
        Reportar(progreso, 0, 1, "Desinstalando versión anterior...", null, null);
        CommandService.Ejecutar("javaws", ["-uninstall", safeUrl], true);
        Reportar(progreso, 0, 1, "Instalando GUI...", null, null);
        var install = CommandService.Ejecutar(PathService.AmigaLauncherShortPath, ["-install", "-silent", safeUrl], true);
        if (!install.Ok) throw new InvalidOperationException(install.Error ?? "Error al instalar la GUI");
        Reportar(progreso, 1, 1, "GUI instalada correctamente", null, null);
        if (_metrics.Inicializado)
            _metrics.RegistrarInstalacionGUI("GUI_Personalizada_" + url[(url.LastIndexOf('/') + 1)..]);
    }

    private static void Reportar(IProgress<OperationProgress> p, int completed, int total, string status, string? guiId, GuiStatus? visual)
    {
        p.Report(new OperationProgress
        {
            Percent = total == 0 ? 100 : (int)Math.Round(completed * 100.0 / total),
            Status = status,
            Completed = completed,
            Total = total,
            GuiId = guiId,
            StatusVisual = visual
        });
    }

    private static void ReportarPaso(IProgress<OperationProgress> p, int index, int total, int completed, int step, string status, string guiId, GuiStatus visual)
    {
        var baseProgress = (index * 4.0) / (total * 4.0) * 100.0;
        var stepProgress = (step / 4.0) * (100.0 / total);
        p.Report(new OperationProgress
        {
            Percent = (int)Math.Round(baseProgress + stepProgress),
            Status = status,
            Completed = completed,
            Total = total,
            GuiId = guiId,
            StatusVisual = visual
        });
    }
}
