using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using InstaladorGuis.Models;

namespace InstaladorGuis.Controls;

public partial class AppDialogHost : UserControl
{
    private TaskCompletionSource<bool?>? _tcs;
    private string _verbo = "instalados";
    private bool _allowClose = true;
    public bool TrabajoIniciado { get; set; }

    public AppDialogHost()
    {
        InitializeComponent();
        IsVisibleChanged += (_, _) =>
        {
            if (Visibility == Visibility.Visible)
                Focus();
        };
    }

    public Task ShowMessageAsync(string titulo, string mensaje, string boton = "Cerrar")
    {
        ResetUi();
        TitleText.Text = titulo;
        TitleText.Foreground = Brush("BrushBlackberry500");
        MessageText.Text = mensaje;
        MessageText.Visibility = Visibility.Visible;
        ShowPrimary(boton);
        _allowClose = true;
        TrabajoIniciado = true;
        return OpenAsync();
    }

    internal void MostrarProgreso(bool instalacion, int total)
    {
        ResetUi();
        _verbo = instalacion ? "instalados" : "desinstalados";
        TitleText.Text = instalacion ? "Instalación en progreso" : "Desinstalación en progreso";
        TitleText.Foreground = Brush("BrushBlackberry500");
        ProgressPanel.Visibility = Visibility.Visible;
        Bar.Value = 0;
        StatusText.Text = instalacion ? "Iniciando instalación…" : "Iniciando desinstalación…";
        CountText.Text = "0 de " + total + " GUIs " + _verbo;
        PrimaryBtn.Visibility = Visibility.Collapsed;
        SecondaryBtn.Visibility = Visibility.Collapsed;
        _allowClose = false;
        TrabajoIniciado = false;
        Visibility = Visibility.Visible;
        _tcs = new TaskCompletionSource<bool?>();
    }

    internal void Actualizar(OperationProgress p)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => Actualizar(p));
            return;
        }

        Bar.Value = Math.Clamp(p.Percent, 0, 100);
        StatusText.Text = p.Status;
        CountText.Text = p.Completed + " de " + p.Total + " GUIs " + _verbo;
    }

    internal void MostrarResumen(OperationSummary resumen)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => MostrarResumen(resumen));
            return;
        }

        var ok = resumen.Exitosos.Count;
        var fail = resumen.Fallidos.Count;
        var verboOk = resumen.EsInstalacion ? "instalada" : "desinstalada";
        var texto = "";
        if (ok > 0)
        {
            texto += ok + " GUI" + (ok > 1 ? "s" : "") + " " + verboOk + (ok > 1 ? "s" : "") + " de " + resumen.Total + Environment.NewLine;
            texto += "• " + string.Join(Environment.NewLine + "• ", resumen.Exitosos);
        }
        if (fail > 0)
        {
            if (texto.Length > 0) texto += Environment.NewLine + Environment.NewLine;
            texto += fail + " error" + (fail > 1 ? "es" : "") + ":" + Environment.NewLine;
            texto += "• " + string.Join(Environment.NewLine + "• ", resumen.Fallidos);
        }

        if (ok == resumen.Total)
        {
            TitleText.Text = resumen.EsInstalacion ? "Instalación completada" : "Desinstalación completada";
            TitleText.Foreground = Brush("BrushKiwi500");
        }
        else if (fail == resumen.Total)
        {
            TitleText.Text = resumen.EsInstalacion ? "Error en la instalación" : "Error en la desinstalación";
            TitleText.Foreground = Brush("BrushCherry400");
        }
        else
        {
            TitleText.Text = resumen.EsInstalacion ? "Completada con errores" : "Desinstalación con errores";
            TitleText.Foreground = Brush("BrushApricot500");
        }

        ProgressPanel.Visibility = Visibility.Collapsed;
        MessageText.Visibility = Visibility.Visible;
        MessageText.Text = texto;
        ShowPrimary("Cerrar");
        _allowClose = true;
    }

    internal void MostrarMensaje(string titulo, string mensaje)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => MostrarMensaje(titulo, mensaje));
            return;
        }

        ProgressPanel.Visibility = Visibility.Collapsed;
        RegistroPanel.Visibility = Visibility.Collapsed;
        TitleText.Text = titulo;
        TitleText.Foreground = Brush("BrushBlackberry500");
        MessageText.Text = mensaje;
        MessageText.Visibility = Visibility.Visible;
        ShowPrimary("Cerrar");
        _allowClose = true;
        TrabajoIniciado = true;
        if (Visibility != Visibility.Visible)
        {
            _tcs = new TaskCompletionSource<bool?>();
            Visibility = Visibility.Visible;
        }
    }

    internal Task<bool?> ShowUpdateAsync(VersionInfo version)
    {
        ResetUi();
        TitleText.Text = "Actualización disponible";
        TitleText.Foreground = Brush("BrushBlackberry500");

        var texto = "Nueva versión  " + version.Version + Environment.NewLine +
                    "Fecha  " + (version.Fecha ?? "No especificada");
        if (version.Cambios is { Count: > 0 })
        {
            texto += Environment.NewLine + Environment.NewLine + "Cambios" + Environment.NewLine +
                     string.Join(Environment.NewLine, version.Cambios.Select(c => "  ·  " + c));
        }

        MessageText.Text = texto;
        MessageText.Visibility = Visibility.Visible;
        ShowPrimary("Actualizar ahora");
        ShowSecondary("Más tarde");
        _allowClose = true;
        return OpenAsync();
    }

    public Task<string?> ShowRegistroLibreAsync()
    {
        ResetUi();
        TitleText.Text = "Registro libre de GUI";
        TitleText.Foreground = Brush("BrushBlackberry500");
        RegistroPanel.Visibility = Visibility.Visible;
        UrlBox.Text = "";
        ShowPrimary("Instalar");
        ShowSecondary("Cancelar");
        _allowClose = true;

        var tcs = new TaskCompletionSource<string?>();
        _tcs = new TaskCompletionSource<bool?>();
        Visibility = Visibility.Visible;

        _ = _tcs.Task.ContinueWith(t =>
        {
            Dispatcher.Invoke(() =>
            {
                if (t.Result == true)
                    tcs.TrySetResult(UrlBox.Text.Trim());
                else
                    tcs.TrySetResult(null);
            });
        }, TaskScheduler.Default);

        Dispatcher.BeginInvoke(() =>
        {
            UrlBox.Focus();
            KeyboardFocus(UrlBox);
        });

        return tcs.Task;
    }

    public Task WaitCloseAsync() => _tcs?.Task ?? Task.CompletedTask;

    private Task<bool?> OpenAsync()
    {
        _tcs = new TaskCompletionSource<bool?>();
        Visibility = Visibility.Visible;
        return _tcs.Task;
    }

    private void Close(bool? result)
    {
        if (!_allowClose && result != true) return;
        Visibility = Visibility.Collapsed;
        _tcs?.TrySetResult(result);
        _tcs = null;
    }

    private void OnPrimary(object sender, RoutedEventArgs e) => Close(true);
    private void OnSecondary(object sender, RoutedEventArgs e) => Close(false);

    private void ResetUi()
    {
        MessageText.Visibility = Visibility.Collapsed;
        ProgressPanel.Visibility = Visibility.Collapsed;
        RegistroPanel.Visibility = Visibility.Collapsed;
        PrimaryBtn.Visibility = Visibility.Collapsed;
        SecondaryBtn.Visibility = Visibility.Collapsed;
        PrimaryBtn.Content = null;
        SecondaryBtn.Content = null;
        MessageText.Text = "";
        TitleText.Text = "";
    }

    private void ShowPrimary(string text)
    {
        PrimaryBtn.Content = text;
        PrimaryBtn.Visibility = Visibility.Visible;
    }

    private void ShowSecondary(string text)
    {
        SecondaryBtn.Content = text;
        SecondaryBtn.Visibility = Visibility.Visible;
    }

    private Brush Brush(string key) => (Brush)FindResource(key);

    private static void KeyboardFocus(UIElement element)
    {
        System.Windows.Input.Keyboard.Focus(element);
    }
}
