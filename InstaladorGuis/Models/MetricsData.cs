using System.Text.Json.Serialization;

namespace InstaladorGuis.Models;

internal sealed class MetricsData
{
    [JsonPropertyName("identificador")]
    public string Identificador { get; set; } = "";

    [JsonPropertyName("ultima_actualizacion")]
    public string UltimaActualizacion { get; set; } = "";

    [JsonPropertyName("metricas_generales")]
    public MetricasGenerales MetricasGenerales { get; set; } = new();

    [JsonPropertyName("instalaciones_por_gui")]
    public Dictionary<string, int> InstalacionesPorGui { get; set; } = new();

    [JsonPropertyName("desinstalaciones_por_gui")]
    public Dictionary<string, int> DesinstalacionesPorGui { get; set; } = new();

    [JsonPropertyName("fallos_por_aplicacion")]
    public Dictionary<string, int> FallosPorAplicacion { get; set; } = new();

    [JsonPropertyName("uso_chatbot_secciones")]
    public Dictionary<string, int> UsoChatbotSecciones { get; set; } = new();

    [JsonPropertyName("historial_eventos")]
    public List<EventoHistorial> HistorialEventos { get; set; } = [];
}

internal sealed class MetricasGenerales
{
    [JsonPropertyName("aperturas_app")]
    public int AperturasApp { get; set; }

    [JsonPropertyName("total_guis_instaladas")]
    public int TotalGuisInstaladas { get; set; }

    [JsonPropertyName("total_desinstalaciones")]
    public int TotalDesinstalaciones { get; set; }

    [JsonPropertyName("total_fallos_instalacion")]
    public int TotalFallosInstalacion { get; set; }

    [JsonPropertyName("total_uso_chatbot")]
    public int TotalUsoChatbot { get; set; }
}

internal sealed class EventoHistorial
{
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = "";

    [JsonPropertyName("tipo")]
    public string Tipo { get; set; } = "";

    [JsonPropertyName("descripcion")]
    public string Descripcion { get; set; } = "";

    [JsonPropertyName("usuario")]
    public string Usuario { get; set; } = "";
}
