using System.Text.Json.Serialization;

namespace InstaladorGuis.Models;

internal sealed class GuiItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("label")]
    public string Label { get; set; } = "";

    [JsonPropertyName("tipo")]
    public string Tipo { get; set; } = "package";

    public bool EsPrendas => string.Equals(Tipo, "clothing", StringComparison.OrdinalIgnoreCase);
}

internal sealed class BrandConfig
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = BrandIds.PullBear;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("fechaVersion")]
    public string FechaVersion { get; set; } = "";

    [JsonPropertyName("archivoMapping")]
    public string ArchivoMapping { get; set; } = "";

    [JsonPropertyName("archivoVersion")]
    public string ArchivoVersion { get; set; } = "";

    [JsonPropertyName("rutasRedBase")]
    public List<string> RutasRedBase { get; set; } = [];

    [JsonPropertyName("rutaMetricasBase")]
    public List<string> RutaMetricasBase { get; set; } = [];

    [JsonPropertyName("archivoMetricas")]
    public string ArchivoMetricas { get; set; } = "";

    [JsonPropertyName("rutaActualizadorBase")]
    public List<string> RutaActualizadorBase { get; set; } = [];

    [JsonPropertyName("archivoActualizador")]
    public string ArchivoActualizador { get; set; } = "";

    [JsonPropertyName("guis")]
    public List<GuiItem> Guis { get; set; } = [];

    [JsonPropertyName("guiToFolderMapping")]
    public Dictionary<string, List<string>> GuiToFolderMapping { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("guiToShortcutMapping")]
    public Dictionary<string, List<string>> GuiToShortcutMapping { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string GetLabel(string guiId)
    {
        var gui = Guis.FirstOrDefault(g => g.Id.Equals(guiId, StringComparison.OrdinalIgnoreCase));
        return gui?.Label ?? guiId;
    }
}

internal sealed class UrlMappingFile
{
    [JsonPropertyName("urlMapping")]
    public Dictionary<string, string> UrlMapping { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

internal sealed class VersionInfo
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("fecha")]
    public string? Fecha { get; set; }

    [JsonPropertyName("cambios")]
    public List<string>? Cambios { get; set; }
}

internal sealed class OperationProgress
{
    public int Percent { get; init; }
    public string Status { get; init; } = "";
    public int Completed { get; init; }
    public int Total { get; init; }
    public string? GuiId { get; init; }
    public GuiStatus? StatusVisual { get; init; }
}

internal static class BrandIds
{
    public const string PullBear = "PB";
    public const string ZaraHome = "ZH";
}

internal enum GuiStatus
{
    Unknown,
    Installed,
    NotInstalled,
    Checking
}

internal sealed class OperationSummary
{
    public List<string> Exitosos { get; } = [];
    public List<string> Fallidos { get; } = [];
    public int Total { get; set; }
    public bool EsInstalacion { get; set; } = true;
}
