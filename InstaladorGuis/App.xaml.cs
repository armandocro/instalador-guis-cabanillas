using System.Windows;
using System.Windows.Threading;
using InstaladorGuis.Services;

namespace InstaladorGuis;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        ErrorLog.Escribir("Arranque " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        base.OnStartup(e);

        try
        {
            ErrorLog.Escribir("Cargando marca. Args: " + string.Join(" ", e.Args));
            var brand = BrandService.Load(e.Args);
            ErrorLog.Escribir("Marca: " + brand.Id);
            var metrics = new MetricsService(brand);
            var installer = new InstallerService(brand, metrics);
            ErrorLog.Escribir("Abriendo ventana");
            var window = new MainWindow(brand, metrics, installer);
            MainWindow = window;
            window.Show();
        }
        catch (Exception ex)
        {
            ErrorLog.MostrarError("No se pudo iniciar el instalador", ex);
            Shutdown(-1);
        }
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ErrorLog.MostrarError("Error de la interfaz", e.Exception);
        e.Handled = true;
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            ErrorLog.MostrarError("Error no controlado", ex);
        else
            ErrorLog.Escribir("Error nativo: " + e.ExceptionObject);
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        ErrorLog.MostrarError("Error en segundo plano", e.Exception);
        e.SetObserved();
    }
}
