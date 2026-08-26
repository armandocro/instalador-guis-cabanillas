using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using InstaladorGuis.Services;

namespace InstaladorGuis.Controls;

public partial class ChatbotControl : UserControl
{
    private MetricsService? _metrics;
    private readonly ObservableCollection<ChatBubble> _messages = [];

    public ChatbotControl()
    {
        InitializeComponent();
        MessagesList.ItemsSource = _messages;
    }

    internal void Inicializar(MetricsService metrics)
    {
        _metrics = metrics;
        ReiniciarConversacion();
    }

    private void OnToggle(object sender, RoutedEventArgs e)
    {
        if (ChatWindow.Visibility == Visibility.Visible)
        {
            Cerrar();
            return;
        }

        ChatWindow.Visibility = Visibility.Visible;
    }

    private void OnCerrar(object sender, RoutedEventArgs e) => Cerrar();

    public void Cerrar() => ChatWindow.Visibility = Visibility.Collapsed;

    private void OnOptionClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ChatOption option })
            option.Accion();
    }

    private void ReiniciarConversacion()
    {
        _messages.Clear();
        AgregarBot(ObtenerSaludo() + Environment.NewLine + Environment.NewLine + "Estoy aquí para echarte una mano.");
        MostrarMenuPrincipal();
    }

    private void MostrarMenuPrincipal()
    {
        AgregarBot("¿En qué puedo ayudarte?",
        [
            new ChatOption("1 · Instalar o desinstalar una GUI", () => Seleccionar(1)),
            new ChatOption("2 · Problemas al instalar o desinstalar", () => Seleccionar(2)),
            new ChatOption("3 · Hay una actualización disponible", () => Seleccionar(3)),
            new ChatOption("4 · Otro tema", () => Seleccionar(4))
        ]);
    }

    private void Seleccionar(int option)
    {
        if (_metrics?.Inicializado == true)
        {
            _metrics.RegistrarUsoChatbot();
            var seccion = option switch
            {
                1 => "instalacion_desinstalacion",
                2 => "solucion_problemas",
                3 => "proceso_actualizacion",
                4 => "otros_temas",
                _ => ""
            };
            if (seccion.Length > 0) _metrics.RegistrarUsoSeccionChatbot(seccion);
        }

        AgregarUsuario("Opción " + option);
        AgregarBot(ObtenerRespuesta(option));
        if (option == 4)
        {
            AgregarBot("Otros temas:",
            [
                new ChatOption("Problemas de permisos", () => AyudaAdicional("permissions")),
                new ChatOption("Problemas de red", () => AyudaAdicional("network")),
                new ChatOption("Problemas de rendimiento", () => AyudaAdicional("performance")),
                new ChatOption("Contactar soporte", () => AyudaAdicional("contact"))
            ]);
        }
        else
        {
            PreguntarOtraAyuda();
        }
    }

    private void AyudaAdicional(string topic)
    {
        if (_metrics?.Inicializado == true)
        {
            _metrics.RegistrarUsoChatbot();
            var seccion = topic switch
            {
                "permissions" => "problemas_permisos",
                "network" => "problemas_red",
                "performance" => "problemas_rendimiento",
                "contact" => "contactar_soporte",
                _ => ""
            };
            if (seccion.Length > 0) _metrics.RegistrarUsoSeccionChatbot(seccion);
        }

        var nombre = topic switch
        {
            "permissions" => "Problemas de permisos",
            "network" => "Problemas de red",
            "performance" => "Problemas de rendimiento",
            "contact" => "Contactar soporte",
            _ => "Ayuda adicional"
        };
        AgregarUsuario(nombre);
        AgregarBot(ObtenerAyudaAdicional(topic));
        PreguntarOtraAyuda();
    }

    private void PreguntarOtraAyuda()
    {
        AgregarBot("¿Necesitas ayuda con otro tema?",
        [
            new ChatOption("Sí", MostrarMenuPrincipal),
            new ChatOption("No", Cerrar)
        ]);
    }

    private void AgregarUsuario(string texto) => AgregarBurbuja(texto, true, null);
    private void AgregarBot(string texto, ChatOption[]? opciones = null) => AgregarBurbuja(texto, false, opciones);

    private void AgregarBurbuja(string texto, bool usuario, ChatOption[]? opciones)
    {
        _messages.Add(new ChatBubble(texto, usuario, opciones));
        _ = Dispatcher.InvokeAsync(() => MessagesScroll.ScrollToEnd(), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private static string ObtenerSaludo()
    {
        var hora = DateTime.Now.Hour;
        if (hora is >= 5 and < 12) return "Buenos días. Bienvenido al asistente de soporte.";
        if (hora is >= 12 and < 20) return "Buenas tardes. Bienvenido al asistente de soporte.";
        return "Buenas noches. Bienvenido al asistente de soporte.";
    }

    private static string ObtenerRespuesta(int option) => option switch
    {
        1 => "Para instalar: marca las GUIs, pulsa Instalar seleccionadas y espera al progreso.\n\nPara desinstalar: selecciónalas y pulsa Desinstalar seleccionadas.\n\nConsejo: guarda el trabajo si tienes otras GUIs de SGA abiertas.",
        2 => "Problemas habituales:\n• Amiga Java Launcher no instalado\n• Sin conexión de red\n• El proceso se queda colgado: reinicia la aplicación\n\nSi persiste, llama a soporte: 52100.",
        3 => "Puedes actualizar ahora o más tarde. Hasta que actualices, el aviso aparecerá al abrir la app.\n\nSe abrirá el actualizador y se cerrará esta ventana. Pulsa Comenzar actualización y, al terminar, vuelve a abrir el instalador.",
        4 => "Si nada de esto te ayuda, contacta con Soporte IT.\nTeléfono: 52100\nCorreo: mttousuarioscabanillas@inditex.com",
        _ => "Selecciona una opción válida del menú."
    };

    private static string ObtenerAyudaAdicional(string topic) => topic switch
    {
        "permissions" => "Si tras instalar no ves las opciones que necesitas, puede faltar permiso. Habla con tu responsable para canalizarlo con Informática.",
        "network" => "Comprueba el acceso a carpetas compartidas o a la INET. Si hay corte de red, llama al 52100.",
        "performance" => "Repórtalo a tu responsable indicando el nombre del ordenador y la GUI afectada.",
        "contact" => "Soporte IT\nCorreo: mttousuarioscabanillas@inditex.com\nTeléfono: 52100",
        _ => "Contacta con soporte técnico."
    };
}

internal sealed class ChatBubble
{
    public ChatBubble(string texto, bool isUser, ChatOption[]? opciones)
    {
        Texto = texto;
        IsUser = isUser;
        Opciones = opciones ?? [];
    }

    public string Texto { get; }
    public bool IsUser { get; }
    public ChatOption[] Opciones { get; }
    public bool TieneOpciones => Opciones.Length > 0;
}

internal sealed class ChatOption
{
    public ChatOption(string texto, Action accion)
    {
        Texto = texto;
        Accion = accion;
    }

    public string Texto { get; }
    public Action Accion { get; }
}
