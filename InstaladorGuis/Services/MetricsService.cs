using System.IO;
using System.Text.Json;
using InstaladorGuis.Models;

namespace InstaladorGuis.Services;

internal sealed class MetricsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly BrandConfig _brand;
    private MetricsData? _metricas;
    private readonly string? _ruta;
    private readonly object _lock = new();
    public bool Inicializado { get; private set; }

    public MetricsService(BrandConfig brand)
    {
        _brand = brand;
        _ruta = EncontrarRuta(brand.RutaMetricasBase, brand.ArchivoMetricas);
    }

    public bool Inicializar()
    {
        try { Cargar(); Inicializado = true; return true; }
        catch { return false; }
    }

    public void RegistrarAperturaApp()
    {
        if (!AsegurarCarga()) return;
        _metricas!.MetricasGenerales.AperturasApp++;
        AgregarEvento("apertura_app", "Aplicación abierta");
        Guardar();
    }

    public void RegistrarInstalacionGUI(string nombreGui)
    {
        if (!AsegurarCarga()) return;
        _metricas!.MetricasGenerales.TotalGuisInstaladas++;
        Incrementar(_metricas.InstalacionesPorGui, nombreGui);
        AgregarEvento("instalacion_exitosa", "GUI " + nombreGui + " instalada exitosamente");
        Guardar();
    }

    public void RegistrarDesinstalacionGUI(string nombreGui)
    {
        if (!AsegurarCarga()) return;
        _metricas!.MetricasGenerales.TotalDesinstalaciones++;
        Incrementar(_metricas.DesinstalacionesPorGui, nombreGui);
        AgregarEvento("desinstalacion_exitosa", "GUI " + nombreGui + " desinstalada exitosamente");
        Guardar();
    }

    public void RegistrarFalloInstalacion(string nombreGui, string? error)
    {
        if (!AsegurarCarga()) return;
        _metricas!.MetricasGenerales.TotalFallosInstalacion++;
        Incrementar(_metricas.FallosPorAplicacion, nombreGui);
        AgregarEvento("fallo_instalacion", "Fallo instalando " + nombreGui + ": " + (error ?? "desconocido"));
        Guardar();
    }

    public void RegistrarUsoChatbot()
    {
        if (!AsegurarCarga()) return;
        _metricas!.MetricasGenerales.TotalUsoChatbot++;
        AgregarEvento("uso_chatbot", "Chatbot utilizado");
        Guardar();
    }

    public void RegistrarUsoSeccionChatbot(string seccion)
    {
        if (!AsegurarCarga()) return;
        if (!_metricas!.UsoChatbotSecciones.ContainsKey(seccion)) return;
        _metricas.UsoChatbotSecciones[seccion]++;
        AgregarEvento("uso_seccion_chatbot", "Sección " + seccion + " del chatbot utilizada");
        Guardar();
    }

    private static string? EncontrarRuta(IEnumerable<string> bases, string archivo)
    {
        var candidatos = bases.Select(b => System.IO.Path.Combine(b, archivo)).ToList();
        foreach (var r in candidatos)
        {
            try { if (File.Exists(r)) return r; } catch { }
        }
        if (candidatos.Count > 0)
        {
            try
            {
                var dir = System.IO.Path.GetDirectoryName(candidatos[0]);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return candidatos[0];
            }
            catch { }
        }
        return null;
    }

    private bool AsegurarCarga() => _metricas != null || Cargar();

    private bool Cargar()
    {
        if (string.IsNullOrEmpty(_ruta)) return false;
        try
        {
            if (!File.Exists(_ruta)) CrearArchivoInicial();
            var json = File.ReadAllText(_ruta);
            _metricas = JsonSerializer.Deserialize<MetricsData>(json, JsonOptions) ?? CrearDatosIniciales();
            AsegurarSecciones();
            if (string.IsNullOrEmpty(_metricas.UltimaActualizacion)) Guardar();
            return true;
        }
        catch { return false; }
    }

    private void CrearArchivoInicial()
    {
        if (string.IsNullOrEmpty(_ruta)) return;
        _metricas = CrearDatosIniciales();
        File.WriteAllText(_ruta, JsonSerializer.Serialize(_metricas, JsonOptions));
    }

    private MetricsData CrearDatosIniciales() => new()
    {
        Identificador = _brand.Id,
        UltimaActualizacion = DateTime.UtcNow.ToString("o"),
        UsoChatbotSecciones = new Dictionary<string, int>
        {
            ["instalacion_desinstalacion"] = 0,
            ["solucion_problemas"] = 0,
            ["proceso_actualizacion"] = 0,
            ["otros_temas"] = 0,
            ["problemas_permisos"] = 0,
            ["problemas_red"] = 0,
            ["problemas_rendimiento"] = 0,
            ["contactar_soporte"] = 0
        }
    };

    private void AsegurarSecciones()
    {
        if (_metricas == null) return;
        foreach (var clave in new[] { "instalacion_desinstalacion", "solucion_problemas", "proceso_actualizacion", "otros_temas", "problemas_permisos", "problemas_red", "problemas_rendimiento", "contactar_soporte" })
            _metricas.UsoChatbotSecciones.TryAdd(clave, 0);
    }

    private void AgregarEvento(string tipo, string descripcion)
    {
        if (_metricas == null) return;
        var usuario = string.IsNullOrWhiteSpace(Environment.UserName) ? "Usuario_Desconocido" : Environment.UserName;
        _metricas.HistorialEventos.Add(new EventoHistorial
        {
            Timestamp = DateTime.UtcNow.ToString("o"),
            Tipo = tipo,
            Descripcion = descripcion,
            Usuario = usuario
        });
        if (_metricas.HistorialEventos.Count > 1000)
            _metricas.HistorialEventos = _metricas.HistorialEventos.TakeLast(1000).ToList();
    }

    private static void Incrementar(Dictionary<string, int> dic, string clave)
    {
        if (!dic.ContainsKey(clave)) dic[clave] = 0;
        dic[clave]++;
    }

    private void Guardar()
    {
        if (_metricas == null || string.IsNullOrEmpty(_ruta)) return;
        lock (_lock)
        {
            try
            {
                _metricas.UltimaActualizacion = DateTime.UtcNow.ToString("o");
                File.WriteAllText(_ruta, JsonSerializer.Serialize(_metricas, JsonOptions));
            }
            catch { /* no bloquear la UI por fallos de telemetría */ }
        }
    }
}
