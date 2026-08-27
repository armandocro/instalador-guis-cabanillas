using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using InstaladorGuis.Helpers;
using InstaladorGuis.Models;
using InstaladorGuis.Services;
using InstaladorGuis.ViewModels;

namespace InstaladorGuis;

public partial class MainWindow : Window
{
    private readonly BrandConfig _brand;
    private readonly MetricsService _metrics;
    private readonly InstallerService _installer;
    private readonly List<GuiVm> _items = [];
    private bool _busy;
    private bool _filterUpdating;

    private const double ContentMaxWidth = 1180;
    private const double ContentMaxHeight = 820;
    private const double ContentMinWidth = 700;
    private const double ContentMinHeight = 500;

    internal MainWindow(BrandConfig brand, MetricsService metrics, InstallerService installer)
    {
        _brand = brand;
        _metrics = metrics;
        _installer = installer;
        InitializeComponent();

        Title = "Instalador de GUIs — " + brand.Nombre;
        BrandNameText.Text = brand.Nombre;
        FooterText.Text = "Versión " + brand.Version + "  ·  actualizada el " + brand.FechaVersion;
        StatInstalledSuffix.Text = "/ " + brand.Guis.Count;

        var hasPackage = brand.Guis.Any(g => !g.EsPrendas);
        var hasClothing = brand.Guis.Any(g => g.EsPrendas);
        FilterPaq.Visibility = hasPackage && hasClothing ? Visibility.Visible : Visibility.Collapsed;
        FilterPrc.Visibility = hasPackage && hasClothing ? Visibility.Visible : Visibility.Collapsed;
        PrcLegend.Visibility = hasClothing ? Visibility.Visible : Visibility.Collapsed;

        foreach (var gui in brand.Guis
                     .OrderBy(g => g.EsPrendas)
                     .ThenBy(g => g.Label, StringComparer.CurrentCultureIgnoreCase))
        {
            var vm = new GuiVm { Id = gui.Id, Label = gui.Label, EsPrendas = gui.EsPrendas };
            vm.PropertyChanged += OnGuiPropertyChanged;
            _items.Add(vm);
        }

        RefrescarLista();
        Chatbot.Inicializar(_metrics);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) => await InicializarAsync();

    private void OnContentHostSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var availW = Math.Max(0, ContentHost.ActualWidth - 32);
        var availH = Math.Max(0, ContentHost.ActualHeight - 32);
        MainContent.Width = Math.Clamp(availW, ContentMinWidth, ContentMaxWidth);
        MainContent.Height = Math.Clamp(availH, ContentMinHeight, ContentMaxHeight);
    }

    private void OnGuiPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GuiVm.IsChecked))
            ActualizarContadores();
    }

    private void OnGuiCardClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source && EstaDentroDeCheckBox(source))
            return;
        if (sender is FrameworkElement fe && fe.DataContext is GuiVm gui)
            gui.IsChecked = !gui.IsChecked;
    }

    private static bool EstaDentroDeCheckBox(DependencyObject? source)
    {
        while (source != null)
        {
            if (source is CheckBox) return true;
            source = VisualTreeHelper.GetParent(source);
        }
        return false;
    }

    private void OnFilterChanged(object sender, RoutedEventArgs e)
    {
        if (_filterUpdating) return;
        _filterUpdating = true;
        if (ReferenceEquals(sender, FilterPaq) && FilterPaq.IsChecked == true)
            FilterPrc.IsChecked = false;
        else if (ReferenceEquals(sender, FilterPrc) && FilterPrc.IsChecked == true)
            FilterPaq.IsChecked = false;
        _filterUpdating = false;
        AplicarFiltro();
    }

    private void OnSeleccionarTodo(object sender, RoutedEventArgs e) => SeleccionarTodas(true);
    private void OnDeseleccionarTodo(object sender, RoutedEventArgs e) => SeleccionarTodas(false);
    private async void OnInstalar(object sender, RoutedEventArgs e) => await InstalarAsync();
    private async void OnDesinstalar(object sender, RoutedEventArgs e) => await DesinstalarAsync();
    private async void OnRegistroLibre(object sender, RoutedEventArgs e) => await RegistroLibreAsync();

    private async Task InicializarAsync()
    {
        if (_metrics.Inicializar())
            _metrics.RegistrarAperturaApp();

        if (!_installer.CargarUrlMapping(out var error))
            MostrarEstadoSistema(error, BannerKind.Error);
        else
            MostrarEstadoSistema(_installer.UrlMapping.Count + " GUIs detectadas", BannerKind.Success);

        var update = await Task.Run(() => _installer.BuscarActualizacion());
        if (update != null)
        {
            if (await DialogHost.ShowUpdateAsync(update) == true)
            {
                var actualizador = _installer.ResolverActualizador();
                if (actualizador == null)
                {
                    await MostrarAvisoAsync("Error: No se pudo encontrar el actualizador. La actualización no puede continuar.");
                }
                else
                {
                    CommandService.AbrirRuta(actualizador, "--brand=" + _brand.Id);
                    await Task.Delay(1000);
                    Close();
                    return;
                }
            }
        }

        await Task.Delay(400);
        await VerificarTodasAsync();
    }

    private async Task VerificarTodasAsync()
    {
        var instaladas = 0;
        foreach (var item in _items)
        {
            var ok = await Task.Run(() => _installer.EstaInstalada(item.Id));
            item.Status = ok ? GuiStatus.Installed : GuiStatus.NotInstalled;
            if (ok) instaladas++;
        }

        StatInstalled.Text = instaladas.ToString();
        string mensaje;
        var kind = BannerKind.Info;
        if (instaladas == 0) mensaje = "No hay GUIs instaladas actualmente.";
        else if (instaladas == _items.Count)
        {
            mensaje = "Las " + _items.Count + " GUIs están instaladas.";
            kind = BannerKind.Success;
        }
        else mensaje = instaladas + " de " + _items.Count + " GUIs están instaladas.";

        PintarBanner(VerificationBanner, VerificationText, kind, "Verificación completada. " + mensaje);
        VerificationBanner.Visibility = Visibility.Visible;
    }

    private async Task InstalarAsync()
    {
        if (_busy) return;
        if (_installer.UrlMapping.Count == 0)
        {
            await MostrarAvisoAsync("Error: No se han cargado las URLs de configuración." + Environment.NewLine + Environment.NewLine +
                         "Por favor, verifica que el archivo " + _brand.ArchivoMapping + " existe en la ruta de red y reinicia la aplicación.");
            return;
        }

        if (!_installer.VerificarAmigaLauncher())
        {
            await MostrarAvisoAsync("Error: Amiga Java Launcher no encontrado" + Environment.NewLine + Environment.NewLine +
                         "No se puede instalar la(s) GUI(s) seleccionada(s) porque el ordenador no dispone de Amiga Java Launcher." +
                         Environment.NewLine + Environment.NewLine +
                         "Ruta esperada: C:\\Program Files\\AmigaLauncher\\amglauncher.exe" + Environment.NewLine +
                         Environment.NewLine + "Por favor, instala Amiga Java Launcher antes de continuar.");
            return;
        }

        var seleccionadas = _items.Where(i => i.IsChecked).Select(i => i.Id).ToList();
        if (seleccionadas.Count == 0)
        {
            await MostrarAvisoAsync("Por favor, selecciona al menos un GUI para instalar.");
            return;
        }

        await EjecutarOperacionAsync(true, seleccionadas);
    }

    private async Task DesinstalarAsync()
    {
        if (_busy) return;
        var seleccionadas = _items.Where(i => i.IsChecked).ToList();
        if (seleccionadas.Count == 0)
        {
            await MostrarAvisoAsync("Por favor, selecciona al menos un GUI instalado para desinstalar.");
            return;
        }

        var noInstaladas = new List<string>();
        var instaladas = new List<string>();
        foreach (var item in seleccionadas)
        {
            var ok = await Task.Run(() => _installer.EstaInstalada(item.Id));
            if (ok) instaladas.Add(item.Id);
            else noInstaladas.Add(item.Id);
        }

        if (noInstaladas.Count > 0)
        {
            await MostrarAvisoAsync("Las siguientes GUIs no se pueden desinstalar porque no están instaladas:" +
                         Environment.NewLine + Environment.NewLine + "• " + string.Join(Environment.NewLine + "• ", noInstaladas));
            return;
        }

        await EjecutarOperacionAsync(false, instaladas);
    }

    private async Task EjecutarOperacionAsync(bool instalar, List<string> ids)
    {
        _busy = true;
        SetBusy(true);
        DialogHost.MostrarProgreso(instalar, ids.Count);
        var progreso = new Progress<OperationProgress>(p =>
        {
            DialogHost.Actualizar(p);
            if (p.GuiId != null && p.StatusVisual != null)
            {
                var item = _items.FirstOrDefault(i => i.Id == p.GuiId);
                if (item != null) item.Status = p.StatusVisual.Value;
            }
        });

        // fire-and-forget: excepciones manejadas internamente
        _ = Task.Run(async () =>
        {
            try
            {
                var resumen = instalar
                    ? _installer.Instalar(ids, progreso)
                    : _installer.Desinstalar(ids, progreso);
                await Dispatcher.InvokeAsync(() =>
                {
                    DialogHost.MostrarResumen(resumen);
                    foreach (var item in _items) item.IsChecked = false;
                    ActualizarContadores();
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() => DialogHost.MostrarMensaje("Error", ex.Message));
            }
        });

        await DialogHost.WaitCloseAsync();
        await VerificarTodasAsync();
        SetBusy(false);
        _busy = false;
    }

    private async Task RegistroLibreAsync()
    {
        if (_busy) return;
        var url = await DialogHost.ShowRegistroLibreAsync();
        if (url == null) return;

        if (string.IsNullOrWhiteSpace(url))
        {
            await MostrarAvisoAsync("Por favor, ingresa una URL válida para el JNLP.");
            return;
        }

        if (!InstallerService.IsValidUrl(url))
        {
            await MostrarAvisoAsync("La URL no es valida. Debe ser HTTP/HTTPS, maximo 2048 caracteres, y contener solo caracteres seguros.");
            return;
        }

        _busy = true;
        SetBusy(true);
        DialogHost.MostrarProgreso(true, 1);
        var progreso = new Progress<OperationProgress>(DialogHost.Actualizar);

        // fire-and-forget: excepciones manejadas internamente
        _ = Task.Run(async () =>
        {
            try
            {
                _installer.InstalarLibre(url, progreso);
                await Dispatcher.InvokeAsync(() =>
                    DialogHost.MostrarMensaje("Instalación completada", "GUI instalada correctamente desde la URL proporcionada"));
            }
            catch (Exception ex)
            {
                if (_metrics.Inicializado) _metrics.RegistrarFalloInstalacion("GUI_Personalizada", ex.Message);
                await Dispatcher.InvokeAsync(() =>
                    DialogHost.MostrarMensaje("Error", "Error al instalar la GUI: " + ex.Message));
            }
        });

        await DialogHost.WaitCloseAsync();
        SetBusy(false);
        _busy = false;
    }

    private void AplicarFiltro()
    {
        var soloPaq = FilterPaq.IsChecked == true && FilterPrc.IsChecked != true;
        var soloPrc = FilterPrc.IsChecked == true && FilterPaq.IsChecked != true;
        var visibles = 0;
        foreach (var item in _items)
        {
            var visible = true;
            if (soloPaq) visible = !item.EsPrendas;
            else if (soloPrc) visible = item.EsPrendas;
            item.IsVisible = visible;
            if (visible) visibles++;
        }

        FilterEmpty.Visibility = visibles == 0 ? Visibility.Visible : Visibility.Collapsed;
        RefrescarLista();
    }

    private void RefrescarLista()
    {
        var paq = _items.Where(i => i.IsVisible && !i.EsPrendas)
            .OrderBy(i => i.Label, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        var prc = _items.Where(i => i.IsVisible && i.EsPrendas)
            .OrderBy(i => i.Label, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        GuisPaqItems.ItemsSource = paq;
        GuisPrcItems.ItemsSource = prc;
        GuisPaqItems.Visibility = paq.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        GuisPrcItems.Visibility = prc.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        GuisPrcItems.Margin = paq.Count > 0 && prc.Count > 0 ? new Thickness(0, 8, 0, 0) : new Thickness(0);
    }

    private void SeleccionarTodas(bool valor)
    {
        foreach (var item in _items.Where(i => i.IsVisible))
            item.IsChecked = valor;
        ActualizarContadores();
    }

    private void ActualizarContadores()
    {
        var n = _items.Count(i => i.IsChecked);
        StatSelected.Text = n.ToString();
        InstallLabel.Text = "Instalar seleccionadas (" + n + ")";
        UninstallBtn.IsEnabled = n > 0 && !_busy;
    }

    private void SetBusy(bool busy)
    {
        InstallBtn.IsEnabled = !busy;
        UninstallBtn.IsEnabled = !busy && _items.Any(i => i.IsChecked);
        SelectAllBtn.IsEnabled = !busy;
        DeselectAllBtn.IsEnabled = !busy;
        RegistroLibreBtn.IsEnabled = !busy;
        FilterPaq.IsEnabled = !busy;
        FilterPrc.IsEnabled = !busy;
    }

    private Task MostrarAvisoAsync(string mensaje) =>
        DialogHost.ShowMessageAsync("Notificación", mensaje);

    private void MostrarEstadoSistema(string mensaje, BannerKind kind)
    {
        SystemBannerTitle.Text = kind == BannerKind.Success ? "Configuración cargada" : "Estado del sistema";
        SystemBannerText.Text = mensaje;
        PintarBanner(SystemBanner, SystemBannerText, kind, mensaje);
        SystemBanner.Visibility = Visibility.Visible;
    }

    private static void PintarBanner(Border banner, TextBlock body, BannerKind kind, string mensaje)
    {
        var (bg, accent) = kind switch
        {
            BannerKind.Success => ("#EEF7EF", "#2F9E44"),
            BannerKind.Warning => ("#FDF5EC", "#E8862C"),
            BannerKind.Error => ("#FDF1F2", "#E12D3C"),
            _ => ("#EEF4FF", "#2563EB")
        };
        banner.Background = BrushHelper.FromHex(bg);
        banner.BorderBrush = BrushHelper.FromHex(accent);
        banner.BorderThickness = new Thickness(3, 1, 1, 1);
        body.Text = mensaje;
    }
}
